using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace MimicSimulation
{
    class ManagerLogic
    {
        static int threadCount = SystemInfo.processorCount;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MultithreadSystem), "Init")]
        [HarmonyPatch(typeof(MultithreadSystem), "ResetUsedThreadCnt")]
        internal static void Record_UsedThreadCnt(MultithreadSystem __instance)
        {
            threadCount = __instance.usedThreadCnt > 0 ? __instance.usedThreadCnt : 1;
            Log.Info($"ThreadCount: {threadCount}");
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
        static IEnumerable<CodeInstruction> PrepareTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Add Threading.ForEachParallel(PrepareTick, GameMain.data.factoryCount)); return; at the begining
                var codeMatcher = new CodeMatcher(instructions)
                    .Start()
                    .InsertAndAdvance(
                        HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
                        Threading.ForEachParallel(PrepareTick, GameMain.data.factoryCount, threadCount)),
                        new CodeInstruction(OpCodes.Ret)
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.PrepareTick failed.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Remove this.factoryStatPool[i].GameTick(time); in the loop
                // Add Threading.ForEachParallel(GameTick, GameMain.data.factoryCount); at the begining
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FactoryProductionStat), "GameTick")))
                    .Advance(-5)
                    .SetOpcodeAndAdvance(OpCodes.Nop)
                    .RemoveInstructions(5)
                    .Start()
                    .Insert(
                        HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
                        Threading.ForEachParallel(GameTick, GameMain.data.factoryCount, threadCount)
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.GameTick failed.");
                return instructions;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick)), HarmonyPriority(Priority.First)]
        static void PlanetTransport_Prefix(PlanetTransport __instance)
        {
            int index = __instance.planet.factoryIndex;
            if (MainManager.TryGet(index, out FactoryManager manager))
                manager.StationBeforeTransport();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick)), HarmonyPriority(Priority.Last)]
        static void PlanetTransport_Postfix(PlanetTransport __instance)
        {
            int index = __instance.planet.factoryIndex;
            if (MainManager.TryGet(index, out FactoryManager manager))
                manager.StationAfterTransport();
        }

        static void PrepareTick(int index)
        {
            var factoryStat = GameMain.data.statistics.production.factoryStatPool[index];
            MainManager.TryGet(index, out FactoryManager manager);
            if (manager == null || manager.IsActive || GameMain.data.factories[index].planet.type == EPlanetType.Gas)
            {
                // Reset stats for idle Gas giant, so statation collectors stats won't be double
                factoryStat.PrepareTick();
                if (manager != null)
                    manager.VeinWorkBegin();
            }
            else
            {
                factoryStat.itemChanged = false;
                factoryStat.consumeRegister[11901] = 0; // Sail: Swarm.RemoveSolarSail()
                factoryStat.productRegister[11901] = 0; // Sail: Swarm.AddSolarSail()
                factoryStat.productRegister[11902] = 0; // SP: ConstructSp()
                factoryStat.productRegister[11903] = 0; // CP: Swarm.GameTick()
            }
        }

        static void GameTick(int index)
        {
            //ProductionStatistics.GameTick
            GameMain.data.statistics.production.factoryStatPool[index].GameTick(GameMain.gameTick);
            if (MainManager.TryGet(index, out FactoryManager manager) == false)
                return;

            manager.StationAfterTick();
            if (manager.IsActive)
            {
                manager.VeinWorkEnd();
                manager.DysonColletEnd();
            }
            else
            {
                manager.VeinIdleEnd();
                Lab_IdleTick(index);
                manager.DysonIdleTick();
            }
        }

        static void Lab_IdleTick(int index)
        {
            long hash = GameMain.data.statistics.production.factoryStatPool[index].hashRegister;
            if (hash > 0 && GameMain.data.history.currentTech > 0)
            {
                lock (GameMain.data.history)
                {
                    GameMain.data.history.AddTechHash(hash);
                }
            }
        }
    }
}
