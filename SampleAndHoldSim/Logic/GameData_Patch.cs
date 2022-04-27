using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class GameData_Patch
    {
        static PlanetFactory[] idleFactories;
        static PlanetFactory[] workFactories;
        static int idleFactoryCount;
        static int workFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                workFactories = new PlanetFactory[GameMain.data.factories.Length];
                idleFactories = new PlanetFactory[GameMain.data.factories.Length];
                MainManager.Init();
                workFactoryCount = MainManager.SetFactories(workFactories, idleFactories);
                idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
                Log.Debug($"factoryCount total:{GameMain.data.factoryCount} work:{workFactoryCount} idle:{idleFactoryCount}");
                UIvein.ViewFactoryIndex = -1;
                UIstation.SetVeiwStation(-1, -1, 0);
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void GameMain_End()
        {
            Plugin.instance.SaveConfig(MainManager.MaxFactoryCount);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix()
        {
            workFactoryCount = MainManager.SetFactories(workFactories, idleFactories);
            idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
        }

        public static IEnumerable<CodeInstruction> ReplaceFactories(IEnumerable<CodeInstruction> instructions)
        {
            // replace GameData.factories with workFactories
            var codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "workFactories"))
                        .Advance(-2)
                        .SetAndAdvance(OpCodes.Nop, null)
                );

            // replace GameData.factoryCount with workFactoryCount
            codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factoryCount"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "workFactoryCount"))
                        .Advance(-2)
                        .SetAndAdvance(OpCodes.Nop, null)
                );
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var result = ReplaceFactories(instructions);

                // Restore GameMain.multithreadSystem.PrepareTransportData(GameMain.localPlanet, this.factories, this.factoryCount, time);
                var codeMatcher = new CodeMatcher(result)
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
                Log.Error("Transpiler GameData.GameTick failed.");
                return instructions;
            }
        }
    }
}
