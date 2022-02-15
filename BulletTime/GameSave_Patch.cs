﻿using BepInEx;
using HarmonyLib;
using System.IO;
using System.Threading;

namespace BulletTime
{
    class GameSave_Patch
    {
        public static bool isBlocked = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        private static void UIGame_Prefix(UIGame __instance)
        {
            if (!GameMain.isRunning || __instance.willClose)
            {
                return;
            }

            if (!BulletTime.State.Interactable && !isBlocked)
            {
                bool balcklist = __instance.dysonEditor.active || (BulletTime.State.LockFactory && __instance.isAnyFunctionWindowActive);
                bool whitelist = __instance.statWindow.active || __instance.replicator.active || __instance.mechaWindow.active || __instance.blueprintBrowser.active;
                if (balcklist && !whitelist)
                {
                    ShowMessage("Read-Only", "Can't interact with game world during auto-save\nPlease wait or press ESC to close the window");
                    isBlocked = true;
                }
            }
        }

        private static void ShowMessage(string title, string message)
        {
            UIMessageBox uimessageBox = UIDialog.CreateDialog("Prefabs/MessageBox VE") as UIMessageBox;
            uimessageBox.m_TitleText.text = title;
            uimessageBox.m_MessageText.text = message;
            uimessageBox.m_Button1.transform.parent.gameObject.SetActive(false);
            uimessageBox.m_Button2.transform.parent.gameObject.SetActive(false);
            uimessageBox.m_Button3.transform.parent.gameObject.SetActive(false);
            uimessageBox.m_IconImage.gameObject.SetActive(false);
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
            if (BulletTime.State.LockFactory)
            {
                __result.onUp = false;
                __result.onDown = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAutoSave), nameof(UIAutoSave._OnLateUpdate))]
        private static void OverwriteSaveText(UIAutoSave __instance)
        {            
            if (!BulletTime.State.Interactable)
            {
                // The game file is still saving in another thread
                __instance.saveText.text = "Saving...".Translate();
                __instance.showTime = 1.8f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.AutoSave))]
        private static bool AutoSave_Prefix()
        {
            if (BulletTime.State.Interactable)
            {                
                // Let's capture screenshot on main thread first
                GameCamera.CaptureSaveScreenShot();
                ThreadingHelper.Instance.StartAsyncInvoke(() =>
                {
                    HighStopwatch highStopwatch = new HighStopwatch();
                    highStopwatch.Begin();
                    bool tmp = BulletTime.State.Pause;
                    BulletTime.State.SetPauseMode(true);
                    BulletTime.State.SetInteractable(false);
                    // Wait a tick to let game full stop
                    Thread.Sleep((int)(1000/FPSController.currentUPS));
                    Log.Info($"Background Autosave start. Sleep: {(int)(1000/FPSController.currentUPS)}ms");
                    bool result = GameSave.AutoSave();
                    Log.Info($"Background Autosave end. Duration: {highStopwatch.duration}s");
                    BulletTime.State.SetInteractable(true);
                    BulletTime.State.SetPauseMode(tmp);
                    return () =>
                    {
                        UIRoot.instance.uiGame.autoSave.saveText.text = result ? "保存成功".Translate() : "保存失败".Translate();
                        UIRoot.instance.uiGame.autoSave.contentTweener.Play1To0();
                    };
                });
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.CaptureSaveScreenShot))]
        private static bool CaptureSaveScreenShot_Prefix()
        {
            if (!BulletTime.State.Interactable)
            {
                // We can't capture screenshot on worker thread, skip
                return false;
            }
            return true;
        }

        static readonly AutoResetEvent autoEvent = new AutoResetEvent(false);
        static bool main = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.Export))]
        private static bool Export_Prefix(DysonSwarm __instance, BinaryWriter w)
        {
            if (!BulletTime.State.Interactable && main)
            {
                main = false;
                autoEvent.Reset();
                ThreadingHelper.Instance.StartSyncInvoke(() =>
                {
                    __instance.Export(w);
                    autoEvent.Set();
                });
                Log.Debug($"Exporting DysonSwarm {__instance.starData.displayName}...");
                autoEvent.WaitOne(-1);
                main = true;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.Export))]
        private static void Player_Prefix()
        {
            if (!BulletTime.State.Interactable)
            {
                Log.Debug("Export player data start");
                Monitor.Enter(GameMain.data.mainPlayer);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Export))]
        private static void Player_Postfix()
        {
            if (!BulletTime.State.Interactable)
            {
                Monitor.Exit(GameMain.data.mainPlayer);
                Log.Debug("Export player data end");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
        private static void PlanetFactory_Prefix(PlanetFactory __instance)
        {
            if (!BulletTime.State.Interactable && __instance.planetId == GameMain.localPlanet?.id)
            {
                Log.Debug("Export local PlanetFactory start");
                BulletTime.State.SetLockFactory(true);
                Thread.Sleep((int)(1000 / FPSController.currentUPS));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
        private static void PlanetFactory_Postfix(PlanetFactory __instance)
        {
            if (!BulletTime.State.Interactable && __instance.planetId == GameMain.localPlanet?.id)
            {                
                BulletTime.State.SetLockFactory(false);
                Log.Debug("Export local PlanetFactory end");
            }
        }

    }
}
