using HarmonyLib;
using System.Threading;
using UnityEngine;
using Compatibility;

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

            pasueThisFrame = BulletTimePlugin.State.PauseInThisFrame();
            if (!pasueThisFrame)
            {
                return true;
            }

            if (BulletTimePlugin.State.AdvanceTick)
            {
                __instance.timei += 1L;
                __instance.timei_once += 1L;
            }
            __instance.timef = __instance.timei * 0.016666666666666666;
            __instance.timef_once = __instance.timei_once * 0.016666666666666666;

            PerformanceMonitor.BeginLogicFrame();
            PerformanceMonitor.BeginSample(ECpuWorkEntry.GameLogic);            

            PerformanceMonitor.BeginSample(ECpuWorkEntry.UniverseSimulate);
            bool flag = GameMain.data.DetermineLocalPlanet();
            GameMain.data.DetermineRelative();
            if (flag)
                GameCamera.instance.FrameLogic();
            VFInput.UpdateGameStates();
            UniverseSimulatorGameTick();
            PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);

            if (GameMain.localPlanet != null && GameMain.localPlanet.factoryLoaded)
            {
                // update player.cmd.raycast
                PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
                GameMain.localPlanet.physics.GameTick();
                PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
            }
            if (GameMain.data.guideMission != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
                GameMain.data.guideMission.GameTick();
                PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
            }
            if (GameMain.mainPlayer != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
                GameMain.data.DetermineRelative();
                GameMain.data.mainPlayer.ApplyGamePauseState(true);
                PlayerGameTick(GameMain.instance.timei);
                PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
            }
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
            if (Input.GetKeyDown(BulletTimePlugin.KeyAutosave.Value))
            {
                UIAutoSave.lastSaveTick = 0L;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void Begin_Postfix()
        {
            if (NebulaCompat.Enable)
            {
                NebulaCompat.OnGameMainBegin();
            }
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
            BulletTimePlugin.State.SetSpeedRatio(1f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Prefix()
        {
            // Save real gameTick
            if (BulletTimePlugin.State.StoredGameTick != 0)
            {
                GameMain.gameTick = BulletTimePlugin.State.StoredGameTick;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.GamePauseLogic))]
        private static bool GamePauseLogic_Prefix(ref bool __result)
        {
            if (BulletTimePlugin.State.Pause)
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

            if (BulletTimePlugin.State.Interactable)
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
