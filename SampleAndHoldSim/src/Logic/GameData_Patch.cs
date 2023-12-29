using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public partial class GameData_Patch
    {
        static PlanetFactory[] idleFactories;
        static PlanetFactory[] workFactories;
        static long[] factoryTimes;
        static int idleFactoryCount;
        static int workFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                int length = GameMain.data.factories.Length;
                workFactories = new PlanetFactory[length];
                idleFactories = new PlanetFactory[length];
                factoryTimes = new long[length];
                MainManager.Init();
                UIstation.SetVeiwStation(-1, -1, 0);
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void GameMain_End()
        {
            Plugin.instance.SaveConfig(MainManager.UpdatePeriod, MainManager.FocusLocalFactory);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix()
        {
            workFactoryCount = MainManager.SetFactories(workFactories, idleFactories, factoryTimes);
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
                .Repeat(matcher => {
                    matcher
                         .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "workFactoryCount"))
                         .Advance(-2)
                         .SetAndAdvance(OpCodes.Nop, null);

                    if (matcher.InstructionAt(1).opcode == OpCodes.Blt)
                    {
                        // replace time with factoryTimes[i] in the loop of for (int i = 0; i < this.factoryCount; i++) {...}
                        matcher.Advance(-6);
                        var index = matcher.Instruction;
                        //Log.Info(matcher.Pos + ": " + index);

                        while (matcher.Opcode != OpCodes.Br) 
                        {
                            //if (matcher.Opcode == OpCodes.Callvirt)
                            //    Log.Debug(matcher.Pos + ": " + matcher.Instruction);
                            if (matcher.Opcode == OpCodes.Ldarg_1)
                            {
                                matcher
                                    .RemoveInstruction()
                                    .Insert(
                                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), "factoryTimes")),
                                        index,
                                        new CodeInstruction(OpCodes.Ldelem_I8)
                                    );
                                matcher.Advance(-2);
                                //Log.Warn(matcher.Pos);
                            }
                            matcher.Advance(-1);
                        }                       
                    }
                });
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

                // Replace DFGroundSystem logic part in if (this.gameDesc.isCombatMode) {...}
                codeMatcher.
                    MatchForward(true,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameDesc), "get_isCombatMode")),
                        new CodeMatch(OpCodes.Brfalse),
                        new CodeMatch(OpCodes.Ldarg_0)
                    );
                if (codeMatcher.IsInvalid)
                {
                    Log.Error("GameTick_Transpiler: Can't find DFGroundSystemLogic!");
                    return codeMatcher.InstructionEnumeration();
                }
                var brfalse = codeMatcher.InstructionAt(-1);
                // call DFGroundSystemLogic_Prefix in CombatLogic.cs
                codeMatcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameData_Patch), nameof(DFGroundSystemLogic_Prefix))),
                    brfalse
                );

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
