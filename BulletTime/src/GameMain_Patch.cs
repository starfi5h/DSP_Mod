using BulletTime.Nebula;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace BulletTime
{
    class GameMain_Patch
    {
        static bool pauseThisFrame;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void Begin_Postfix()
        {
            if (!GameMain.instance.isMenuDemo) IngameUI.Init();
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
            if (GameStateManager.StoredGameTick != 0) GameMain.gameTick = GameStateManager.StoredGameTick;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.GamePauseLogic))]
        private static bool GamePauseLogic_Prefix(PlayerAnimator __instance, ref bool __result)
        {
            if (!GameStateManager.Pause) 
                return true;

            if (GameStateManager.HotkeyPause && !GameStateManager.EnableMechaFunc)
            {
                // freeze mecha animation 
                __instance.PauseAllAnimations();
                __instance.motorBone.localPosition = Vector3.zero;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ABN_MechaPosition), nameof(ABN_MechaPosition.OnGameTick))]
        private static bool ABN_MechaPosition_Prefix()
        {
            // We will skip position check so it will not trigger when exiting pause mode
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMove_Sail), nameof(PlayerMove_Sail.StartFastTravelToPlanet))]
        [HarmonyPatch(typeof(PlayerMove_Sail), nameof(PlayerMove_Sail.StartFastTravelToUPosition))]
        private static bool StartFastTravel_Prefix()
        {
            if (GameStateManager.Pause)
            {
                UIRealtimeTip.Popup("Can't teleport to another planet during BulletTime pause!");
                return false;
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UniverseSimulator), nameof(UniverseSimulator.GameTick))]
        private static IEnumerable<CodeInstruction> UniverseSimulatorGameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Due to Galactic Scale has prefix for galaxyData.UpdatePoses, we will use transpiler to replace here
            var codeMacher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GalaxyData), nameof(GalaxyData.UpdatePoses))))
                .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(GameMain_Patch), nameof(UpdatePoses)));
            return codeMacher.InstructionEnumeration();
        }

        private static void UpdatePoses(GalaxyData galaxyData, double time)
        {
            // Guard for galaxyData.UpdatePoses in UniverseSimulator.GameTick
            // if pauseThisFrame and player is not fastTravelling, then skip updating the planets positions
            if (!pauseThisFrame || GameMain.mainPlayer == null || GameMain.mainPlayer.fastTravelling)       
                galaxyData.UpdatePoses(time);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Postfix()
        {
            pauseThisFrame = false;
        }

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

            pauseThisFrame = GameStateManager.PauseInThisFrame();
            if (!pauseThisFrame)
            {
                return true;
            }

            // 時間停止模式
            // 取代DetermineGameTickRate loop之中的內容, 只更新必要的內容(鏡頭,相對位置,機甲動作等)
            GameData data = GameMain.data;
            data.mainPlayer.ApplyGamePauseState(false);
            if (GameStateManager.AdvanceTick)
            {
                __instance.timei += 1L;
                __instance.timei_once += 1L;
            }
            __instance.timef = __instance.timei * 0.016666666666666666;
            __instance.timef_once = __instance.timei_once * 0.016666666666666666;
            DeepProfiler.BeginMajorSample(DPEntry.LogicTick, -1, __instance.timei);
            LogicFrame(GameMain.logic); //用處理時停模式的特殊邏輯取代
            DeepProfiler.EndMajorSample();
            return false;
        }


        private static void LogicFrame(GameLogic logic)
        {
            // 以單線程進行必要的運算, 函式從GameLogic.OnGameLogicFrame提取           
            logic.UniverseGameTick();   // 修改: 靜止宇宙,使星球不移動(要阻止UpdatePoses),只計算鏡頭相對位置和按鍵
            logic.LocalPlanetPhysics(); // 更新本地星球的物理, 包含碰撞和raycastLogic(游標指向的東西)
            if (logic.sector != null)
            {
                logic.SpaceSectorPhysics(); // 宇宙和載具物理邏輯
                logic.SpaceSectorPrepare(); // skillSystem.CollectTempStates
            }
            logic.LocalFactoryPrepare();// cargoTraffic.ClearStates

            GameLogicPlayerGameTick(logic); // 修改: 玩家的動作處理(要阻止和外界交互的部分),以及姿態數據收集

            // 工廠邏輯和黑霧邏輯全部跳過, 只更新物理和音效
            //logic.FactoryBeforeGameTick();// 跳過:無人機建設和戴森球創建
            //logic.FactoryConstructionSystemGameTick(); // 跳過:戰場分析站和無人機建設
            //logic.WarningSystemGameTick();// 跳過:警報系統狀態更新和廣播
            //logic.TrashSystemGameTick();  // 跳過:垃圾移動和壽命
            //logic.ScenarioGameTick();     // 跳過: 場景邏輯(需要時間前進才有用):教程,成就,目標,元數據,異常偵測,彩蛋

            SpaceSectorGameTick(logic); // 特殊處理: 投射物運動

            logic.LocalPlanetAudio();
            logic.SpaceSectorAudio();
            logic.SpaceSectorAudioPost();

            SpaceSectorPostGameTick(logic); // 特殊處理: 黑霧模型的位置更新和物理碰撞

            logic.CollectPreferences(); // 收集玩家的偏好設定
        }

        [HarmonyReversePatch(HarmonyReversePatchType.Original)]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.PlayerGameTick))]
        public static void GameLogicPlayerGameTick(GameLogic logic)
        {
            _ = Transpiler(null);
            return;

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Replace this.mainPlayer.GameTick(this.timei) with our own guard function
                var codeMacher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.GameTick))))
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(GameMain_Patch), nameof(PlayerGameTick)));
                return codeMacher.InstructionEnumeration();
            }
        }

        private static void PlayerGameTick(Player player, long time)
        {
            if (GameStateManager.HotkeyPause) //熱鍵戰術暫停模式:機甲無法移動
            {
                if (GameStateManager.EnableMechaFunc) //允許完整互動
                {
                    player.GameTick(time);
                    return;
                }
                // 擷取PlayerController.GameTick中放置藍圖和查看的部分
                player.controller.SetCommandStateHeader(); // 更新cmd的參數
                player.controller.UpdateCommandState(); // 更新cmd的狀態
                player.controller.GetInput(); // 處理按鍵輸入VFInput
                player.controller.HandleBaseInput(); // 處理手拿東西
                player.controller.actionRts.GameTick(time); 
                player.controller.actionBuild.GameTick(time);
                player.controller.actionInspect.GameTick(time); //阻止立即建造
                player.controller.ClearForce();
                player.position = GameStateManager.PlayerPosition; //鎖定機甲位置

                player.gizmo.GameTick();
                player.orders.GameTick(time);
                return;
            }

            if (GameStateManager.Interactable) //允許完整互動
            {
                player.GameTick(time);
                return;
            }
            // In auto-saving, we need to make sure mecha data is not corrupted
            Monitor.Enter(player);

            // 時停模式:機甲可以自由移動
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

        private static void SpaceSectorGameTick(GameLogic logic)
        {            
            SkillSystem_Patch.GameTick(); // 對於SpaceSectorGameTick來說,只有SkillSystem有必要更新(投射物)
            logic.RefreshFactoryArray();  // 同步logic.factories陣列
        }

        private static void SpaceSectorPostGameTick(GameLogic logic)
        {
            if (logic.sector != null)
            {
                DeepProfiler.BeginSample(DPEntry.Sector, -1, 3L);
                logic.sector.model.PostGameTick(); //refresh visaul position
                DeepProfiler.BeginSample(DPEntry.LocalPhysics, -1, 0L);
                if (!DSPGame.IsMenuDemo)
                {
                    logic.sector.physics.PostGameTick(); //refresh collision box
                }
                DeepProfiler.EndSample(-1, -2L);
                DeepProfiler.EndSample(-1, -2L);
            }
        }
    }
}
