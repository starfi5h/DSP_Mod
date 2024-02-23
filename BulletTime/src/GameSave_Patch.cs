using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine.UI;

namespace BulletTime
{
    class GameSave_Patch
    {
        public static bool isBlocked = false;
        public static bool isEnabled = false;
        private static Harmony harmony = null;

        public static void Enable(bool enable)
        {
            if (NebulaCompat.NebulaIsInstalled)
            {
                isEnabled = enable;
                return;
            }

            if (enable && harmony == null)
            {
                Log.Debug("Patch GameSave_Patch");
                harmony = Harmony.CreateAndPatchAll(typeof(GameSave_Patch));
                isEnabled = true;
            }
            if (!enable && harmony != null)
            {
                Log.Debug("Unpatch GameSave_Patch");
                harmony.UnpatchSelf();
                harmony = null;
                isEnabled = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        private static void UIGame_Prefix(UIGame __instance)
        {
            if (!GameMain.isRunning || __instance.willClose)
            {
                return;
            }

            if (!GameStateManager.Interactable && !isBlocked)
            {
                bool balcklist = __instance.dysonEditor.active || (GameStateManager.LockFactory && __instance.isAnyFunctionWindowActive);
                bool whitelist = __instance.statWindow.active || __instance.replicator.active || __instance.mechaWindow.active || __instance.blueprintBrowser.active;
                if (balcklist && !whitelist)
                {
                    if (__instance.dysonEditor.active)
                    {
                        if (string.IsNullOrEmpty(IngameUI.CurrentStatus))
                        {
                            ShowMessage("Read-Only".Translate(), "Can't interact with game world during auto-save\nPlease wait or press ESC to close the window".Translate());
                        }
                        else
                        {
                            ShowMessage("Read-Only".Translate(), "Can't interact with game world when player is loading the game".Translate());
                        }
                    }
                    else
                        ShowMessage();
                    isBlocked = true;
                }
            }
        }

        private static void ShowMessage(string title = null, string message = null)
        {
            UIMessageBox uimessageBox = UIDialog.CreateDialog("Prefabs/MessageBox VE") as UIMessageBox;
            if (title!= null && message != null)
            {
                uimessageBox.m_TitleText.text = title;
                uimessageBox.m_MessageText.text = message;
                uimessageBox.m_Button1.transform.parent.gameObject.SetActive(false);
                uimessageBox.m_Button2.transform.parent.gameObject.SetActive(false);
                uimessageBox.m_Button3.transform.parent.gameObject.SetActive(false);
                uimessageBox.m_IconImage.gameObject.SetActive(false);
            }
            else
            {
                // Make block image transparent
                uimessageBox.m_WindowTrans.gameObject.SetActive(false);
                uimessageBox.gameObject.GetComponent<Image>().color = UnityEngine.Color.clear;
            }
            UIMessageBox.PushMessage(uimessageBox);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMessageBox), nameof(UIMessageBox.CloseTopMessage))]
        private static void CloseTopMessage_Postfix(ref bool __result)
        {
            // Don't eat ESC so it will close the window
            if (isBlocked)
            {
                __result = false;
                isBlocked = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), "_buildConfirm", MethodType.Getter)]
        [HarmonyPatch(typeof(VFInput), "blueprintPasteOperate0", MethodType.Getter)]
        [HarmonyPatch(typeof(VFInput), "blueprintPasteOperate1", MethodType.Getter)]
        private static void BuildConfirm_Postfix(ref VFInput.InputValue __result)
        {
            // Stop building actions
            if (GameStateManager.LockFactory)
            {
                __result.onUp = false;
                __result.onDown = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), "instantConstruct", MethodType.Getter)]
        private static void InstantConstruct_Postfix(ref bool __result)
        {
            // Stop building actions
            if (GameStateManager.LockFactory)
            {
                __result = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIAutoSave), nameof(UIAutoSave._OnLateUpdate))]
        private static bool AutoSaveGuard(UIAutoSave __instance)
        {
            if (__instance.showTime == 0 && GameStateManager.Pause)
            {
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAutoSave), nameof(UIAutoSave._OnLateUpdate))]
        private static void OverwriteSaveText(UIAutoSave __instance)
        {            
            if (GameStateManager.IsSaving)
            {
                // The game file is still saving in another thread
                __instance.saveText.text = "Saving...".Translate();
                __instance.showTime = 1.8f;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIAutoSave), nameof(UIAutoSave._OnLateUpdate))]
        public static IEnumerable<CodeInstruction> RemoveGC_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Replace GameSave.AutoSave() with AsyncAutoSave()
            var codeMacher = new CodeMatcher(instructions).End()
                .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GameSave), nameof(GameSave.AutoSave))))
                .SetOperandAndAdvance(AccessTools.Method(typeof(GameSave_Patch), nameof(AsyncAutoSave)));

            return codeMacher.InstructionEnumeration();
        }

        private static bool AsyncAutoSave()
        {
            if (!isEnabled) // Use vanilla autosave if it is disable (MP)
                return GameSave.AutoSave();

            // Let's capture screenshot on main thread first
            GameCamera.CaptureSaveScreenShot();

            // Set pause state here so the game will go to pause in the next fixupdate()
            bool tmp = GameStateManager.Pause;
            bool isFullscreenPaused = GameMain.isFullscreenPaused;
            if (NebulaCompat.IsMultiplayerActive)
            {
                GameMain.isFullscreenPaused = true; // Prevent other players from joining
                NebulaCompat.SetPacketProcessing(false); // Stop processing packet from other players
            }
            GameStateManager.SetPauseMode(true);
            GameStateManager.SetInteractable(false);
            GameStateManager.IsSaving = true;

            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                HighStopwatch highStopwatch = new HighStopwatch();
                highStopwatch.Begin();
                // Wait a tick to let game full stop?
                Thread.Sleep((int)(1000/FPSController.currentUPS));
                Log.Info($"Background Autosave start. UPS: {FPSController.currentUPS:F2}");
                bool result = GameSave.AutoSave();
                Log.Info($"Background Autosave end. Duration: {highStopwatch.duration}s");
                return () =>
                {
                    System.GC.Collect();

                    GameStateManager.IsSaving = false;
                    GameStateManager.SetInteractable(true);
                    GameStateManager.SetPauseMode(tmp);
                    GameMain.isFullscreenPaused = isFullscreenPaused;
                    if (NebulaCompat.IsMultiplayerActive)
                    {
                        NebulaCompat.SetPacketProcessing(true); // Resume processing packet from other players
                    }
                    UIRoot.instance.uiGame.autoSave.saveText.text = result ? "保存成功".Translate() : "保存失败".Translate();
                    UIRoot.instance.uiGame.autoSave.contentTweener.Play1To0();
                };
            });
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.CaptureSaveScreenShot))]
        private static bool CaptureSaveScreenShot_Prefix()
        {
            if (!GameStateManager.Interactable)
            {
                // We can't capture screenshot on worker thread, skip
                return false;
            }
            return true;
        }

        static readonly AutoResetEvent autoEvent = new AutoResetEvent(false);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.Export))]
        private static bool Export_Prefix(DysonSwarm __instance, BinaryWriter w)
        {
            if (!GameStateManager.Interactable && ThreadingHelper.Instance.InvokeRequired)
            {
                autoEvent.Reset();
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    __instance.Export(w);
                    autoEvent.Set();
                });
                //Log.Debug($"Exporting DysonSwarm {__instance.starData.displayName}...");
                autoEvent.WaitOne(-1);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.Export))]
        private static void Player_Prefix()
        {
            if (!GameStateManager.Interactable)
            {
                //Log.Debug("Export player data start");
                Monitor.Enter(GameMain.data.mainPlayer);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Export))]
        private static void Player_Postfix()
        {
            if (!GameStateManager.Interactable)
            {
                Monitor.Exit(GameMain.data.mainPlayer);
                //Log.Debug("Export player data end");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
        private static void PlanetFactory_Prefix(PlanetFactory __instance)
        {
            if (!GameStateManager.Interactable && __instance.planetId == GameMain.localPlanet?.id)
            {
                //Log.Debug("Export local PlanetFactory start");
                GameStateManager.SetLockFactory(true);
                Thread.Sleep((int)(1000 / FPSController.currentUPS));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
        private static void PlanetFactory_Postfix(PlanetFactory __instance)
        {
            if (!GameStateManager.Interactable && __instance.planetId == GameMain.localPlanet?.id)
            {                
                GameStateManager.SetLockFactory(false);
                //Log.Debug("Export local PlanetFactory end");
            }
        }

    }
}
