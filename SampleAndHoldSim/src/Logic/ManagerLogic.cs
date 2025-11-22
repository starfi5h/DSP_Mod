using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace SampleAndHoldSim
{
    public class ManagerLogic
    {
        static long totalHashByIdle = 0;

        public static void OnGameStart()
        {
            totalHashByIdle = 0;
        }

        public static void OnFactoryFrameBegin()
        {
            // 計入上一幀的idle hash, 然後歸零
            GameMain.data.history.AddTechHash(totalHashByIdle);
            totalHashByIdle = 0;

            // 更新Active Manager
            var gameLogic = GameMain.logic;
            for (int i = 0; i < gameLogic.factoryCount; i++)
            {
                if (MainManager.TryGet(gameLogic.factories[i].index, out var manager))
                {
                    if (manager.IsActive)
                    {
                        manager.StationBeforeTick();
                        manager.DysonBeforeTick();
                    }
                }
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
        static bool PrepareTick_Prefix(ProductionStatistics __instance) // 單線程
        {            
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                PrepareTick(i); //替換factoryStatPool[i].PrepareTick();
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick_Parallel))]
        static bool PrepareTick_Parallel_Prefix(ProductionStatistics __instance, int threadOrdinal, int threadCount) // 多線程
        {
            if (ParallelUtils.CalculateWorkSegment(threadOrdinal, threadCount, 
                __instance.gameData.factoryCount, 0, out int workStart, out int workEnd))
            {
                for (int i = workStart; i < workEnd; i++)
                {
                    PrepareTick(i); //替換factoryStatPool[i].PrepareTick();
                }
            }
            return false;
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
        [HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick_Parallel))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Note: Due to onItemChange can only call in the declared class, it need to use transpiler

                // Replace: this.factoryStatPool[i].GameTick(time);
                // To:      GameLogic_Patch.GameTick(i);

                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FactoryProductionStat), "GameTick")))
                    .Advance(-2)
                    .RemoveInstructions(3)
                    .Insert(
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ManagerLogic), nameof(GameTick)))
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.GameTick failed.");
                return instructions;
            }
        }

        static void PrepareTick(int index)
        {
            var factoryStat = GameMain.data.statistics.production.factoryStatPool[index];
            if (factoryStat == null) return;
            MainManager.TryGet(index, out FactoryManager manager);
            if (manager == null || manager.IsActive || GameMain.data.factories[index].planet.type == EPlanetType.Gas)
            {
                // Reset stats for idle Gas giant, so statation collectors stats won't be double
                factoryStat.PrepareTick();
            }
            else
            {
                factoryStat.itemChanged = false;
                factoryStat.consumeRegister[1210]  = 0; // Warper: StationComponent.InternalTickRemote()
                factoryStat.consumeRegister[11901] = 0; // Sail: Swarm.RemoveSolarSail()
                factoryStat.productRegister[11901] = 0; // Sail: Swarm.AddSolarSail()
                factoryStat.productRegister[11902] = 0; // SP: ConstructSp()
                factoryStat.productRegister[11903] = 0; // CP: Swarm.GameTick()
            }
        }

        static void GameTick(FactoryProductionStat[] factoryStatPool, int index)
        {
            //ProductionStatistics.GameTick
            factoryStatPool[index].GameTick(GameMain.gameTick);
            if (!MainManager.TryGet(index, out FactoryManager manager))
            {
                //Log.Debug($"factoryStatPool[{index}] null!");
                return;
            }

            manager.StationAfterTick();
            if (manager.IsActive)
            {
                //manager.VeinWorkEnd();
                manager.DysonColletEnd();
            }
            else
            {
                //manager.VeinIdleEnd();
                Lab_IdleTick(index);
                manager.DysonIdleTick();
            }
        }

        static void Lab_IdleTick(int index)
        {
            long hash = GameMain.data.statistics.production.factoryStatPool[index].hashRegister;
            if (hash > 0 && GameMain.data.history.currentTech > 0)
            {
                Interlocked.Add(ref totalHashByIdle, hash);
            }
        }
    }
}
