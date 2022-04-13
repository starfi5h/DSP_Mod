using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MimicSimulation
{
    class Functions_Patch
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
        static IEnumerable<CodeInstruction> PrepareTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codeMatcher = new CodeMatcher(instructions)
                    .Start()
                    .InsertAndAdvance(
                        HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
                        Threading.ForEachParallel(PrepareTick, GameMain.data.factoryCount)),
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
                // replace this.factories to workFactories
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FactoryProductionStat), "GameTick")))
                    .Advance(-5)
                    .SetOpcodeAndAdvance(OpCodes.Nop)
                    .RemoveInstructions(5)
                    .Start()
                    .Insert(
                        HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
                        Threading.ForEachParallel(GameTick, GameMain.data.factoryCount)
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.GameTick failed.");
                return instructions;
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        static void PlanetTransport_Prefix(PlanetTransport __instance)
        {
            int index = __instance.planet.factoryIndex;
            if (FactoryPool.TryGet(index, out FactoryData factoryData))
                factoryData.StationBeforeTransport();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.GameTick))]
        static void PlanetTransport_Postfix(PlanetTransport __instance)
        {
            int index = __instance.planet.factoryIndex;
            if (FactoryPool.TryGet(index, out FactoryData factoryData))
                factoryData.StationAfterTransport();
        }

        static void PrepareTick(int index)
        {
            var factoryStat = GameMain.data.statistics.production.factoryStatPool[index];
            FactoryPool.TryGet(index, out FactoryData factoryData);
            if (factoryData == null || factoryData.IsActive)
            {
                factoryStat.PrepareTick();
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
            if (FactoryPool.TryGet(index, out FactoryData factoryData) == false)
                return;

            factoryData.StationAfterTick();
            if (factoryData.IsActive)
            {
                factoryData.DysonColletEnd();
            }
            else
            {
                Lab_IdleTick(index);
                factoryData.DysonIdleTick();
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
