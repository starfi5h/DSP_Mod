﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        [HarmonyPatch(typeof(MultithreadSystem), nameof(MultithreadSystem.Init))]
        [HarmonyPatch(typeof(MultithreadSystem), nameof(MultithreadSystem.ResetUsedThreadCnt))]
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
            {
                if (manager.IsActive)
                {
                    manager.SetMineral(__instance, __instance.storage[0].count - __state);
                    ref var miner = ref factory.factorySystem.minerPool[__instance.minerId];
                    if (miner.productCount < 0) miner.productCount = 0; // Fix miners that have negative tmp storage
                }
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.GameTick))]
        static IEnumerable<CodeInstruction> PlanetTransport_Transpiler1(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Patch for singlethread
                // Goal: Prevent other stations's ship delivery interfare item count diff record
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetTransport), "GameTick")))
                    .Advance(1)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        HarmonyLib.Transpilers.EmitDelegate<Action<PlanetFactory>>((factory) =>
                        {
                            if (MainManager.TryGet(factory.index, out FactoryManager manager))
                                manager.StationAfterTransport();
                        }
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler PlanetTransport_Transpiler1 failed.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> PlanetTransport_Transpiler2(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Patch for multithread
                // Goal: Execute FactoryManager.StationAfterTransport() after all PlanetTransport.GameTick() is done
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(true, 
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "PrepareTransportData"),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_multithreadSystem"),
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "Schedule"),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_multithreadSystem"),
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "Complete"))
                    .Advance(1)
                    .Insert(
                        HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
                        {
                            for (int index = 0; index < GameMain.data.factoryCount; index++)
                            {
                                if (MainManager.TryGet(index, out FactoryManager manager))
                                    manager.StationAfterTransport();
                            }
                        }
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("PlanetTransport_Transpiler2 failed.");
                return instructions;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static void PowerSystem_Gametick(PowerSystem __instance, ref long time)
        {
            if (MainManager.TryGet(__instance.factory.index, out var manager))
            {
                // Fix len consumption rate in idle factory
                // bool useCata = time % 10L == 0L;
                if (manager.IsNextIdle)
                    time /= MainManager.UpdatePeriod;
            }
        }

        static void PrepareTick(int index)
        {
            var factoryStat = GameMain.data.statistics.production.factoryStatPool[index];
            MainManager.TryGet(index, out FactoryManager manager);
            if (manager == null || manager.IsActive || GameMain.data.factories[index].planet.type == EPlanetType.Gas)
            {
                // Reset stats for idle Gas giant, so statation collectors stats won't be double
                factoryStat.PrepareTick();
                //if (manager != null)
                //    manager.VeinWorkBegin();
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
                //manager.VeinWorkEnd();
                manager.DysonColletEnd();
            }
            else
            {
                //manager.VeinIdleEnd();
                Lab_IdleTick(index);
                manager.DysonIdleTick();
            }
            
            if (!manager.IsActive || manager.IsNextIdle)
            {
                // If stats is not going to reset in the next frame, remove stats add by Blackbox
                if (Compatibility.Blackbox_Patch.IsPatched)
                    Compatibility.Blackbox_Patch.Warper.RevertStats(index);
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
