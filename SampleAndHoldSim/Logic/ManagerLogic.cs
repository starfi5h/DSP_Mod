﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace SampleAndHoldSim
{
    class ManagerLogic
    {
        static int threadCount = SystemInfo.processorCount;
        static long totalHash = 0;

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
                        {
                            totalHash = 0;
                            Threading.ForEachParallel(GameTick, GameMain.data.factoryCount, threadCount);
                            // NotifyTechUnlock() use unity api so needs to be in main thread
                            GameMain.data.history.AddTechHash(totalHash);
                        }
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.GameTick failed.");
                return instructions;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
        static void UpdateVeinCollection_Prefix(StationComponent __instance, ref int __state)
        {
            __state = __instance.storage[0].count;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
        static void UpdateVeinCollection_Postfix(StationComponent __instance, int __state, PlanetFactory factory)
        {
            if (MainManager.TryGet(factory.index, out FactoryManager manager))
                manager.SetMinearl(__instance.id, __instance.storage[0].count - __state);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick)), HarmonyPriority(Priority.VeryLow)]
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
                factoryStat.consumeRegister[1210]  = 0; // Warper: StationComponent.InternalTickRemote()
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
                Interlocked.Add(ref totalHash, hash);
            }
        }
    }
}
