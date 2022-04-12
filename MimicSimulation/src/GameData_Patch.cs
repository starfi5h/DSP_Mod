using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MimicSimulation
{
    class GameData_Patch
    {
        static PlanetFactory[] idleFactories;
        static PlanetFactory[] workFactories;
        static int idleFactoryCount;
        static int workFactoryCount;
        static int factoryCursor = 0;
        static int Setting = 5;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void GameMain_Start()
        {
            if (GameMain.data != null)
            {
                workFactories = new PlanetFactory[GameMain.data.factories.Length];
                idleFactories = new PlanetFactory[GameMain.data.factories.Length];
                factoryCursor = 0;
                SetFactories();
                Log.Debug($"FactoryCount total:{GameMain.data.factoryCount} work:{workFactoryCount} idle:{idleFactoryCount}");
            }
        }

        public static void SetFactories()
        {
            int workIndex = 0;
            int idleIndex = 0;
            int localId = GameMain.localPlanet?.factoryIndex ?? -1;
            if (localId != -1)
                workFactories[workIndex++] = GameMain.data.factories[localId];

            int i = factoryCursor;
            do
            {
                i = (++i) % GameMain.data.factoryCount;
                if (i == localId)
                    continue;
                if (workIndex < Setting)
                    workFactories[workIndex++] = GameMain.data.factories[i];
                else
                    idleFactories[idleIndex++] = GameMain.data.factories[i];
            } while (i != factoryCursor);
            workFactoryCount = workIndex;
            idleFactoryCount = idleIndex;
            factoryCursor = (++factoryCursor) % GameMain.data.factoryCount;
        }

        
        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix(GameData __instance)
        {
            SetFactories();
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
