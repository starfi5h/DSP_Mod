using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MimicSimulation
{
    class ProductionStatistics_Patch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
        public static bool PrepareTick(ProductionStatistics __instance)
        {
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                if (GameData_Patch.IsActive[i])
                {
                    __instance.factoryStatPool[i].PrepareTick();
                }
                else
                {
                    __instance.factoryStatPool[i].itemChanged = false;
                    __instance.factoryStatPool[i].consumeRegister[11901] = 0; // Sail: Swarm.RemoveSolarSail()
                    __instance.factoryStatPool[i].productRegister[11901] = 0; // Sail: Swarm.AddSolarSail()
                    __instance.factoryStatPool[i].productRegister[11902] = 0; // SP: ConstructSp()
                    __instance.factoryStatPool[i].productRegister[11903] = 0; // CP: Swarm.GameTick()
                }
            }
            return false;
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
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProductionStatistics_Patch), "GameTick")));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler ProductionStatistics.GameTick failed.");
                return instructions;
            }
        }

        static void GameTick()
        {
            Threading.ForEachParallel(Work, GameMain.data.factoryCount);
        }

        static void Work(int index)
        {
            GameMain.data.statistics.production.factoryStatPool[index].GameTick(GameMain.gameTick);
            if (GameData_Patch.IsActive[index])
            {
            }
            else
            {
                Lab_IdleTick(index);
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
