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
            bool flag = !__instance.isMenuDemo && gameData.DetermineLocalPlanet();
            gameData.DetermineRelative();
            __instance.SetStarmapReferences(GameMain.data);
            if (flag)
                GameCamera.instance.FrameLogic();
            VFInput.UpdateGameStates();
            if (GameMain.mainPlayer?.controller.actionSail.fastTravelling ?? false) // Use original path for fast travel
            {
                GameMain.universeSimulator.GameTick(__instance.timef);
                gameData.DetermineRelative();
                PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);
                goto EndLogic;
            }
            else
            {
                UniverseSimulatorGameTick(); //靜止宇宙
            }
            PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);
            // Scenario的部分需要時間前進才有用, 跳過

            // Extract the part meaningful to player in GameData.GameTick():
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
            if (gameData.guideMission != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
                gameData.guideMission.GameTick(); // what does this do?
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

            if (gameData.spaceSector != null)
            {
                PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
                gameData.spaceSector.model.PostGameTick(); //refresh visaul position
                if (!DSPGame.IsMenuDemo)
                {
                    gameData.spaceSector.physics.PostGameTick(); //refresh collision box
                }
                PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
            }
            // LocalAudio 暫時不管它

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
            // this.galaxyData.UpdatePoses(time); //保留原本星球位置,只在接下來計算鏡頭相對位置
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
                        universe.planetSimulators[i].UpdateUniversalPosition(uPosition, position2);
                    }
                }
            }
        }

        private static void PlayerGameTick(long time)
        {            
            Player player = GameMain.data.mainPlayer;
            if (player == null)
                return;

            if (GameStateManager.Interactable) //允許完整互動
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

            player.controller.GameTick(time); //機甲動作
            player.gizmo.GameTick();
            player.orders.GameTick(time);
            player.mecha.forge.GameTick(time, 0.016666668f); //forge是在Mecha.GameTick中唯一沒有和外界交互的
            player.mecha.UpdateSkillColliders(); //更新玩家的護盾位置

            Monitor.Exit(player);
        }
    }
}
