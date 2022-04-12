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
            //string str = "";
            for (int i = 0; i < __instance.gameData.factoryCount; i++)
            {
                if (GameData_Patch.IsActive[i])
                {
                    //str += i + " ";
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
            //Log.Warn(str);
            return false;
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
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProductionStatistics_Patch), "GameTick")));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("ProductionStatistics.GameTick failed. Mod version not compatible with game version.");
                return instructions;
            }
        }


        static void Print(IEnumerable<CodeInstruction> instructions, int start, int end)
        {
            int count = -1;
            foreach (var i in instructions)
            {
                if (count++ < start)
                    continue;
                if (count >= end)
                    break;

                if (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt)
                    Log.Warn($"{count,2} {i}");
                else if (i.IsLdarg())
                    Log.Info($"{count,2} {i}");
                else
                    Log.Debug($"{count,2} {i}");
            }
        }
    }
}
