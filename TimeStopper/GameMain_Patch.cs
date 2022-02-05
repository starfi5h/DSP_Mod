using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace TimeStopper
{
    class GameMain_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.isFullscreenPaused), MethodType.Setter)]
        private static void isFullscreenPaused_Postfix()
        {
            if (GameStateManager.Pause == true)
            {
                GameMain.instance._fullscreenPaused = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Prefix(GameMain __instance)
        {
            __instance._fullscreenPaused = GameStateManager.Pause;
            //For multiplayer
            //__instance._paused = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Postfix()
        {
            if (GameStateManager.Pause)
            {
                GameMain.gameTick++;
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

            if (Input.GetKeyDown(KeyCode.F10))
            {
                GameStateManager.SetPauseMode(!GameStateManager.Pause, ref GameMain.instance.timei);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.GamePauseLogic))]
        private static bool GamePauseLogic_Prefix(bool __result)
        {
            if (GameStateManager.Pause)
            {
                __result = false;
                return false;
            }
            return true;
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

        private static void PlayerGameTick(long time)
        {            
            Player player = GameMain.data.mainPlayer;
            Monitor.Enter(player);
            if (GameStateManager.Interactable)
            {
                player.controller.cmd.raycast.GameTick();
                player.GameTick(time);
                return;
            }
            player.mecha.GenerateEnergy(0.016666666666666666);

            // Raycast.GameTick() will stop updating, so clean the remain information
            player.controller.cmd.raycast.castVege.id = 0;
            player.controller.cmd.raycast.castVein.id = 0;
            player.controller.cmd.raycast.castEntity.id = 0;
            player.controller.cmd.raycast.castPrebuild.id = 0;

            player.controller.GameTick(time);
            player.gizmo.GameTick();
            player.orders.GameTick(time);

            //disable drone, lab
            player.mecha.forge.GameTick(time, 0.016666668f);
            Monitor.Exit(player);
        }



    }
}
