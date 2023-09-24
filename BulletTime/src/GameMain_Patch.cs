using HarmonyLib;
using System.Threading;
using UnityEngine;

namespace BulletTime
{
    class GameMain_Patch
    {
        static bool pasueThisFrame;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static bool FixedUpdate_Prefix(GameMain __instance)
        {
            // If the game is paused already, we run original function
            if (!GameMain.isRunning || GameMain.isPaused)
            {
                if (!NebulaCompat.IsMultiplayerActive)
                {
                    // Don't skip in Multiplayer mode
                    return true;
                }
            }

            pasueThisFrame = GameStateManager.PauseInThisFrame();
            if (!pasueThisFrame)
            {
                return true;
            }

            if (GameStateManager.AdvanceTick)
            {
                __instance.timei += 1L;
                __instance.timei_once += 1L;
            }
            __instance.timef = __instance.timei * 0.016666666666666666;
            __instance.timef_once = __instance.timei_once * 0.016666666666666666;
            GameData gameData = GameMain.data;

            PerformanceMonitor.BeginLogicFrame();
            PerformanceMonitor.BeginSample(ECpuWorkEntry.GameLogic);            

            PerformanceMonitor.BeginSample(ECpuWorkEntry.UniverseSimulate);
            bool flag = gameData.DetermineLocalPlanet();
            gameData.DetermineRelative();
            if (flag)
                GameCamera.instance.FrameLogic();
            VFInput.UpdateGameStates();
            if (UIRoot.instance.uiGame.starmap.fastTravelling) // Use original path for fast travel
            {
                GameMain.universeSimulator.GameTick(__instance.timef);
                gameData.DetermineRelative();
                PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);
                goto EndLogic;
            }
            else
            {
                UniverseSimulatorGameTick();
            }
            PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);


            PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
            gameData.mainPlayer.packageUtility.Count();
            PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);

            if (GameMain.localPlanet != null && GameMain.localPlanet.factoryLoaded)
            {
                // update player.cmd.raycast
                PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
                GameMain.localPlanet.factory.cargoTraffic.ClearStates();
                GameMain.localPlanet.physics.GameTick();
                PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
            }
            if (GameMain.data.guideMission != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
                gameData.guideMission.GameTick();
                PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
            }
            if (GameMain.mainPlayer != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
                gameData.mainPlayer.ApplyGamePauseState(false);
                PlayerGameTick(__instance.timei);
                gameData.DetermineRelative();
                PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
            }

            EndLogic:
            PerformanceMonitor.EndSample(ECpuWorkEntry.GameLogic);
            PerformanceMonitor.EndLogicFrame();

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Postfix()
        {
            if (pasueThisFrame)
            {
                pasueThisFrame = false;
            }            
            if (BulletTimePlugin.KeyAutosave.Value.IsDown() && UIRoot.instance.uiGame.autoSave.showTime == 0)
            {
                // Initial auto save when there is no autosave in process
                UIAutoSave.lastSaveTick = 0L;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void Begin_Postfix()
        {
            if (!GameMain.instance.isMenuDemo)
            {
                IngameUI.Init();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void End_Postfix()
        {
            IngameUI.Dispose();
            GameStateManager.SetSpeedRatio(1f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Prefix()
        {
            // Save real gameTick
            if (GameStateManager.StoredGameTick != 0)
            {
                GameMain.gameTick = GameStateManager.StoredGameTick;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.GamePauseLogic))]
        private static bool GamePauseLogic_Prefix(ref bool __result)
        {
            if (GameStateManager.Pause)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ABN_MechaPosition), nameof(ABN_MechaPosition.OnGameTick))]
        private static bool ABN_MechaPosition_Prefix()
        {
            // We will skip position check so it will not trigger when exiting pause mode
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStarmap), nameof(UIStarmap.StartFastTravelToPlanet))]
        private static bool StartFastTravelToPlanet_Prefix()
        {
            if (GameMain.isFullscreenPaused)
            {
                UIRealtimeTip.Popup("Can't teleport to another planet during BulletTime pause!");
                return false;
            }
            return true;
        }

        private static void UniverseSimulatorGameTick()
        {
            UniverseSimulator universe = GameMain.universeSimulator;
            universe.backgroundStars.transform.position = Camera.main.transform.position;
            if (GameMain.localPlanet != null)
            {
                universe.backgroundStars.transform.rotation = Quaternion.Inverse(GameMain.localPlanet.runtimeRotation);
            }
            else
            {
                universe.backgroundStars.transform.rotation = Quaternion.identity;
            }
            Vector3 position = GameMain.mainPlayer.position;
            VectorLF3 uPosition = GameMain.mainPlayer.uPosition;
            Vector3 position2 = GameCamera.main.transform.position;
            Quaternion rotation = GameCamera.main.transform.rotation;
            for (int i = 0; i < universe.starSimulators.Length; i++)
            {
                universe.starSimulators[i].UpdateUniversalPosition(position, uPosition, position2, rotation);
            }
            if (universe.planetSimulators != null)
            {
                for (int i = 0; i < universe.planetSimulators.Length; i++)
                {
                    if (universe.planetSimulators[i] != null)
                    {
                        universe.planetSimulators[i].UpdateUniversalPosition(position, uPosition, position2);
                    }
                }
            }
        }

        private static void PlayerGameTick(long time)
        {            
            Player player = GameMain.data.mainPlayer;
            if (player == null)
                return;

            if (GameStateManager.Interactable)
            {
                player.GameTick(time);
                return;
            }
            // In auto-saving, we need to make sure mecha data is not corrupted
            Monitor.Enter(player);

            player.mecha.GenerateEnergy(0.016666666666666666);
            if (player.controller.cmd.raycast != null)
            {
                player.controller.cmd.raycast.castVege.id = 0;
                player.controller.cmd.raycast.castVein.id = 0;
            }

            player.controller.GameTick(time);
            player.gizmo.GameTick();
            player.orders.GameTick(time);
            player.mecha.forge.GameTick(time, 0.016666668f);

            Monitor.Exit(player);
        }
    }
}
