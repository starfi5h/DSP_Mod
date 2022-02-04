using HarmonyLib;
using System;
using Unity;
using UnityEngine;

namespace Experiment
{
    public class SoftPause
    {
        static int count = 0;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Resume))]
        private static void Resume_Postfix()
        {
            //GameMain.instance._paused = true;
            GameMain.isFullscreenPaused = (count++ % 2 == 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Postfix()
        {
            if (GameMain.isFullscreenPaused)
            {
                PerformanceMonitor.BeginLogicFrame();
                PerformanceMonitor.BeginSample(ECpuWorkEntry.GameLogic);
                PerformanceMonitor.BeginSample(ECpuWorkEntry.UniverseSimulate);
                GameMain.data.DetermineRelative();
                if (GameMain.localPlanet != null)
                    GameCamera.instance.FrameLogic();
                VFInput.UpdateGameStates();
                UniverseSimulatorGameTick(GameMain.instance.timei);
                PerformanceMonitor.EndSample(ECpuWorkEntry.UniverseSimulate);

                PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
                GameMain.data.DetermineRelative();
                GameMain.data.mainPlayer.ApplyGamePauseState(true);
                PlayerGameTick(GameMain.instance.timei);
                PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
                PerformanceMonitor.EndSample(ECpuWorkEntry.GameLogic);
                PerformanceMonitor.EndLogicFrame();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.GamePauseLogic))]
        private static bool GamePauseLogic_Prefix(bool __result)
        {
            if (GameMain.isFullscreenPaused)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        private static void GameTick_Prefix(PlayerAction_Rts __instance)
        {
            if (GameMain.isFullscreenPaused)
            {
                //
                //Log.Debug($"{VFInput._rtsMove.onDown} {VFInput._rtsMove.onUp}");
                bool rtsMoveCameraConflict = VFInput.rtsMoveCameraConflict;
                bool rtsMineCameraConflict = VFInput.rtsMineCameraConflict;
                //Log.Debug(rtsMoveCameraConflict);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateCommandState))]
        private static void UpdateCommandState_Postfix(PlayerController __instance)
        {
            if (GameMain.isFullscreenPaused)
            {
                if (__instance.cmd.type == ECommand.Follow || __instance.cmd.type == ECommand.Mine)
                {
                    //Log.Debug(__instance.cmd.type);
                    __instance.cmd.type = ECommand.None;
                }
            }
        }

        private static void UniverseSimulatorGameTick(long time)
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

        static bool interactable = false;




        private static void PlayerGameTick(long time)
        {
            Player player = GameMain.data.mainPlayer;

            if (interactable)
            {
                player.controller.cmd.raycast.GameTick();
                player.GameTick(time);
                return;
            }




            player.mecha.GenerateEnergy(0.016666666666666666);


            // Raycast.GameTick() will stop, so clear it
            player.controller.cmd.raycast.castVege.id = 0;
            player.controller.cmd.raycast.castVein.id = 0;
            player.controller.cmd.raycast.castEntity.id = 0;
            player.controller.cmd.raycast.castPrebuild.id = 0;


            player.controller.GameTick(time);
            player.gizmo.GameTick();
            player.orders.GameTick(time);



            //player.mecha.GameTick(time, 0.016666668f); - disable drone, lab
            player.mecha.forge.GameTick(time, 0.016666668f);
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        public static bool CreatePrebuilds_Prefix(BuildTool __instance)
        {
            if (GameMain.isFullscreenPaused)
            {
                Log.Debug("Create Prebuild!");
                UIRealtimeTip.Popup("Cannot build in pause mode!", true);
                return false;
            }
            return true;
        }


    }
}
