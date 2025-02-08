using HarmonyLib;
using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;


namespace BulletTime.Nebula
{
    public static class NebulaPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "RealGameTick", MethodType.Getter)]
        static void RealGameTick(ref long __result)
        {
            if (GameStateManager.StoredGameTick != 0)
                __result = GameStateManager.StoredGameTick;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "RealUPS", MethodType.Getter)]
        static void RealUPS(ref float __result)
        {
            if (!GameStateManager.Pause)
                __result *= (1f - GameStateManager.SkipRatio) / 100f;
            //Log.Dev($"{1f - GameStateManager.SkipRatio:F3} UPS:{__result}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "NotifyTickDifference")]
        static void NotifyTickDifference(float delta)
        {
            if (!GameStateManager.Pause)
            {
                float ratio = Mathf.Clamp(1 + delta / (float)FPSController.currentUPS, 0.01f, 1f);
                GameStateManager.SetSpeedRatio(ratio);
                //Log.Dev($"{delta:F4} RATIO:{ratio}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "OnPlayerJoining")]
        static bool OnPlayerJoining(string username)
        {
            NebulaCompat.IsPlayerJoining = true;
            GameMain.isFullscreenPaused = true; // Prevent other players from joining
            IngameUI.ShowStatus(string.Format("{0} joining the game".Translate(), username));
            GameStateManager.SetPauseMode(true);
            GameStateManager.SetSyncingLock(true); // TODO: Lock for only on the joining player's planet
            SetProgessMode(true); // Prepare to update joining player status
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "OnAllPlayersSyncCompleted")]
        static void OnAllPlayersSyncCompleted()
        {
            NebulaCompat.IsPlayerJoining = false;
            if (!NebulaCompat.IsClient)
            {
                NebulaCompat.DetermineCurrentState();
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
            }
            SetProgessMode(false); // Resume ping status
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Prefix()
        {
            if (NebulaCompat.IsMultiplayerActive)
                NebulaCompat.SendPacket(PauseEvent.Save);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Postfix(string saveName)
        {
            if (NebulaCompat.IsMultiplayerActive && !NebulaCompat.IsClient)
            {
                if (saveName != GameSave.AutoSaveTmp)
                {
                    // if it is not autosave (trigger by manual), reset all pause states
                    NebulaCompat.LoadingPlayers.Clear();
                    NebulaCompat.IsPlayerJoining = false;
                }
                if (saveName != GameSave.saveExt)
                {
                    NebulaCompat.DetermineCurrentState();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MechaLab), nameof(MechaLab.GameTick))]
        private static bool MechaLabGameTick_Prefix()
        {
            // Disable MechaLab during player joining
            return !NebulaCompat.IsMultiplayerActive || !NebulaCompat.IsPlayerJoining;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDESphereInfo), nameof(UIDESphereInfo._OnInit)), HarmonyAfter("dsp.nebula-multiplayer")]
        private static void UIDESphereInfo__OnInit()
        {
            UIDETopFunction topFunction = UIRoot.instance.uiGame.dysonEditor.controlPanel.topFunction;
            topFunction.pauseButton.button.interactable = true;
            SetDysonSpherePasued(NebulaCompat.DysonSpherePaused);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction._OnLateUpdate))]
        private static bool UIDETopFunction__OnLateUpdate()
        {
            return !NebulaCompat.IsMultiplayerActive;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction.OnPauseButtonClick))]
        private static bool OnPauseButtonClick()
        {
            if (NebulaCompat.IsMultiplayerActive)
            {
                SetDysonSpherePasued(!NebulaCompat.DysonSpherePaused);
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
                return false;
            }
            return true;
        }

        public static void SetDysonSpherePasued(bool state)
        {
            NebulaCompat.DysonSpherePaused = state;
            UIDETopFunction topFunction = UIRoot.instance.uiGame.dysonEditor.controlPanel.topFunction;
            topFunction.pauseButton.highlighted = !NebulaCompat.DysonSpherePaused;
            topFunction.pauseImg.sprite = (NebulaCompat.DysonSpherePaused ? topFunction.pauseSprite : topFunction.playSprite);
            topFunction.pauseText.text = (NebulaCompat.DysonSpherePaused ? "Click to resume rotating" : "Click to stop rotating").Translate();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            try
            {
                // When DysonSpherePaused is on, replace this.orbitAngularSpeed with 0f
                var codeMatcher = new CodeMatcher(instructions, iL)
                    .MatchForward(true,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphereLayer), "orbitAngularSpeed"))
                    )
                    .Repeat(matcher =>
                    {
                        matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(NebulaPatch), nameof(GetOrbitAngularSpeed)));
                    });

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Error("DysonSphereLayer_GameTick Transpiler error");
                Log.Error(e);
                return instructions;
            }
        }

        static float GetOrbitAngularSpeed(DysonSphereLayer @this)
        {
            return NebulaCompat.DysonSpherePaused ? 0 : @this.orbitAngularSpeed;
        }

        #region Progress

        static int lastLength;
        static bool enablePingIndicatorUpdate = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "UpdateBufferLength")]
        static void UpdateBufferLength_Postfix(int length)
        {
            if (lastLength == length || NebulaWorld.GameStates.GameStatesManager.FragmentSize <= 0) return; // Update only when length change

            // Broadcast the downloading progress to other players
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new ProgressUpdatePacket(NebulaWorld.GameStates.GameStatesManager.FragmentSize, (float)length / NebulaWorld.GameStates.GameStatesManager.FragmentSize));
            lastLength = length;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "UpdatePingIndicator")]
        static bool UpdatePingIndicator_Prefix()
        {
            return enablePingIndicatorUpdate;
        }

        public static void SetProgessMode(bool enable)
        {
            if (enable)
            {
                enablePingIndicatorUpdate = false;
            }
            else
            {
                enablePingIndicatorUpdate = true;
                if (NebulaModAPI.MultiplayerSession.IsServer) NebulaWorld.Multiplayer.Session.World.HidePingIndicator();
            }
        }

        public static void SetProgressTest(int fragmentSize, float percentage)
        {
            var enable = enablePingIndicatorUpdate;
            enablePingIndicatorUpdate = true;
            NebulaWorld.Multiplayer.Session.World.UpdatePingIndicator($"Progress {fragmentSize / 1000:n0} KB ({percentage:P1})");
            enablePingIndicatorUpdate = enable;
        }

        #endregion
    }
}
