using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MimicSimulation
{
    public class GameData_Patch
    {
        static PlanetFactory[] idleFactories;
        static PlanetFactory[] workFactories;
        static int idleFactoryCount;
        static int workFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void GameMain_Start()
        {
            if (GameMain.data != null)
            {
                workFactories = new PlanetFactory[GameMain.data.factories.Length];
                idleFactories = new PlanetFactory[GameMain.data.factories.Length];
                MainManager.Init();
                workFactoryCount = MainManager.SetFactories(workFactories, idleFactories);
                idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
                Log.Info($"factoryCount total:{GameMain.data.factoryCount} work:{workFactoryCount} idle:{idleFactoryCount}");
            }
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix()
        {
            workFactoryCount = MainManager.SetFactories(workFactories, idleFactories);
            idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace this.factories to workFactories
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"))
                    .Repeat(matcher => matcher
                            //.SetOperandAndAdvance(workFactories)
                            .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "workFactories"))
                            .Advance(-2)
                            .SetAndAdvance(OpCodes.Nop, null)
                    );

                // replace this.factoryCount to workFactoryCount
                codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factoryCount"))
                    .Repeat(matcher => matcher
                            //.SetOperandAndAdvance(workFactoryCount)
                            .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "workFactoryCount"))
                            .Advance(-2)
                            .SetAndAdvance(OpCodes.Nop, null)
                    );
                // Restore GameMain.multithreadSystem.PrepareTransportData(GameMain.localPlanet, this.factories, this.factoryCount, time);
                codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "PrepareTransportData"))
                    .Advance(-5)
                    .SetAndAdvance(OpCodes.Ldarg_0, null)
                    .SetAndAdvance(OpCodes.Ldfld, AccessTools.Field(typeof(GameData), "factories"))
                    .SetAndAdvance(OpCodes.Ldarg_0, null)
                    .SetAndAdvance(OpCodes.Ldfld, AccessTools.Field(typeof(GameData), "factoryCount"));

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("GameTick_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }
    }
}
