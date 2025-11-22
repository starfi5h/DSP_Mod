using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class GameLogic_Patch
    {
        static PlanetFactory[] idleFactories;
        static PlanetFactory[] workFactories;
        static long[] workFactoryTimes;
        static int idleFactoryCount;
        static int workFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                int length = GameMain.data.factories.Length;
                workFactories = new PlanetFactory[length];
                idleFactories = new PlanetFactory[length];
                workFactoryTimes = new long[length]; // the scale tick of the working factories
                MainManager.Init();
                UIstation.SetVeiwStation(-1, -1, 0);
                ManagerLogic.OnGameStart();
                Fix_Patch.FixMinerProductCount(); // the fix is also applied in StationComponent.UpdateVeinCollection
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void GameMain_End()
        {
            Plugin.instance.SaveConfig(MainManager.UpdatePeriod, MainManager.FocusLocalFactory);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ThreadManager), nameof(ThreadManager.ProcessFrame))]
        static void BeforeLogicFrameBegin()
        {
            // 當logic frame開始之前, 根據GameMain.gameTick決定工廠的狀態(working/idle)
            // (時機點必須在EGameLogicTask.DataPrepare(400)之前
            workFactoryCount = MainManager.SetFactories(workFactories, idleFactories, workFactoryTimes);
            idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameLogic), nameof(GameLogic.RefreshFactoryArray))]
        static bool RefreshFactoryArray_Prefix(GameLogic __instance)
        {
            if (MainManager.UpdatePeriod <= 1) return true;

            __instance.factories = workFactories;
            __instance.factoryCount = workFactoryCount;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.UniverseGameTick))]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.PlayerGameTick))]
        static void ResetLocalLoadFactory(GameLogic __instance)
        {
            // localLoadedFactory若不在workFactories中, 就將其設為null
            // 不然會引發_present_cargo_parallel或ContextCollect_FactoryComponents_MultiMain的錯誤
            bool IslocalFactoryActive = false;
            for (int i = 0; i < workFactoryCount; i++)
            {
                if (workFactories[i] == __instance.localLoadedFactory)
                {
                    IslocalFactoryActive = true;
                    break;
                }
            }
            if (!IslocalFactoryActive)
            {
                __instance.localLoadedFactory = null;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameLogic), nameof(GameLogic.OnFactoryFrameBegin))]
        static void OnFactoryBegin(GameLogic __instance)
        {
            // 將時間替換成倍率縮減後的真實時間, 以讓idle工廠可以正常運作
            // 對於focus local的本地工廠則在GameLogic_Patch特殊處理
            int scale = MainManager.UpdatePeriod;
            __instance.timef = __instance.main.timef / scale;
            __instance.timei = __instance.main.timei / scale;
            __instance.timef_once = __instance.main.timef_once / scale;
            __instance.timei_once = __instance.main.timei_once / scale;

            // 處理相關Manager邏輯
            ManagerLogic.OnFactoryFrameBegin();
        }

        // Used by mod compat 做為兼容替代保留
        public static IEnumerable<CodeInstruction> ReplaceFactories(IEnumerable<CodeInstruction> instructions)
        {
            // replace GameData.factories with workFactories
            var codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameLogic_Patch), "workFactories"))
                        .Advance(-2)
                        .SetAndAdvance(OpCodes.Nop, null)
                );

            // replace GameData.factoryCount with workFactoryCount
            codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factoryCount"))
                .Repeat(matcher => {
                    matcher
                         .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameLogic_Patch), "workFactoryCount"))
                         .Advance(-2)
                         .SetAndAdvance(OpCodes.Nop, null);

                    if (matcher.InstructionAt(1).opcode == OpCodes.Blt)
                    {
                        // replace time with workFactoryTimes[i] in the loop of for (int i = 0; i < this.factoryCount; i++) {...}
                        matcher.Advance(-6);
                        var index = matcher.Instruction;
                        //Log.Info(matcher.Pos + ": " + index);

                        while (matcher.Opcode != OpCodes.Br) 
                        {
                            //if (matcher.Opcode == OpCodes.Callvirt)
                            //    Log.Debug(matcher.Pos + ": " + matcher.Instruction);
                            if (matcher.Opcode == OpCodes.Ldarg_1)
                            {
                                matcher
                                    .RemoveInstruction()
                                    .Insert(
                                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameLogic_Patch), "workFactoryTimes")),
                                        index,
                                        new CodeInstruction(OpCodes.Ldelem_I8)
                                    );
                                matcher.Advance(-2);
                                //Log.Warn(matcher.Pos);
                            }
                            matcher.Advance(-1);
                        }                       
                    }
                });
            return codeMatcher.InstructionEnumeration();
        }
    }
}
