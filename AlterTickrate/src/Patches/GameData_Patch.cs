using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AlterTickrate.Patches
{
    public class GameData_Patch
    {
        static PlanetFactory[] facilityFactories = new PlanetFactory[0];
        static PlanetFactory[] inserterFactories = new PlanetFactory[0];
        static PlanetFactory[] beltFactories = new PlanetFactory[0];
        static int facilityFactoryCount;
        static int inserterFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                facilityFactories = new PlanetFactory[GameMain.data.factories.Length];
                inserterFactories = new PlanetFactory[GameMain.data.factories.Length];
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        static void GameMain_End()
        {
            Plugin.plugin.SaveConfig();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix()
        {
            if (GameMain.data.factories.Length != facilityFactories.Length)
            {
                facilityFactories = new PlanetFactory[GameMain.data.factories.Length];
                inserterFactories = new PlanetFactory[GameMain.data.factories.Length];
                beltFactories = new PlanetFactory[GameMain.data.factories.Length];
            }
            int gameTick = (int)GameMain.gameTick;
			PlanetFactory localFactory = GameMain.localPlanet?.factory;

            facilityFactoryCount = 0;
            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                if ((i + gameTick) % Parameters.FacilityUpdatePeriod == 0)
                {
                    facilityFactories[facilityFactoryCount] = GameMain.data.factories[i];
                    facilityFactoryCount++;
                }
            }
			if (localFactory != null && (localFactory.index + gameTick) % Parameters.FacilityUpdatePeriod != 0)
            {
				//Log.Warn($"{ConfigSettings.FacilityUpdatePeriod} {gameTick % ConfigSettings.FacilityUpdatePeriod} {(localFactory.index + gameTick) % ConfigSettings.FacilityUpdatePeriod}");
				facilityFactories[facilityFactoryCount++] = localFactory;
				Parameters.AnimOnlyFactory = localFactory;
			}
			else
            {
				Parameters.AnimOnlyFactory = null;
			}

            inserterFactoryCount = 0;
            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                if ((i + gameTick) % Parameters.SorterUpdatePeriod == 0)
                {
                    inserterFactories[inserterFactoryCount] = GameMain.data.factories[i];
                    inserterFactoryCount++;
                }
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                IEnumerable<CodeInstruction> newInstructions = instructions;

                if (true) // EnableFacility
                {
                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Factory);
                    // End:   PerformanceMonitor.BeginSample(ECpuWorkEntry.Facility);
                    var codeMatcher = new CodeMatcher(newInstructions);
                    codeMatcher.MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_I4_5), // ECpuWorkEntry.Factory = 5
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    int startPos = codeMatcher.Pos; //IL #92
                    
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Facility), //12
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    int endPos = codeMatcher.Pos; //IL #212
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(facilityFactories), nameof(facilityFactoryCount));
                }

                if (true) // EnableSorter
                {
                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Inserter);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Inserter); 
                    var codeMatcher = new CodeMatcher(newInstructions);
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Inserter), //10
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    int startPos = codeMatcher.Pos;

                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Inserter), //10
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    int endPos = codeMatcher.Pos;
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(inserterFactories), nameof(inserterFactoryCount));
                }

                /*
                if (false) // EnableBelt
                {
                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Belt); (second)
                    var codeMatcher = new CodeMatcher(newInstructions);
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt), //9
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    int startPos = codeMatcher.Pos;

                    codeMatcher
                        .MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"))
                        .MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    int endPos = codeMatcher.Pos;
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(beltFactories), nameof(beltFactoryCount));
                }
                */

                return newInstructions;
            }
            catch
            {
                Log.Error("Transpiler GameData.GameTick failed");
                return instructions;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceFactories(IEnumerable<CodeInstruction> instructions, int start, int end, string factoriesField, string factoryCountField)
        {
            // replace GameData.factories with factoriesField
            var codeMatcher = new CodeMatcher(instructions).Advance(start);
            while (true) {
                codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"));
                if (codeMatcher.IsInvalid || codeMatcher.Pos >= end)
                    break;

                codeMatcher
                    .Advance(-1)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), factoriesField));
            }

            // replace GameData.factoryCount with factoryCountField
            codeMatcher.Start().Advance(start);
            while (true)
            {
                codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factoryCount"));
                if (codeMatcher.IsInvalid || codeMatcher.Pos >= end)
                    break;

                codeMatcher
                    .Advance(-1)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(GameData_Patch), factoryCountField));
            }

            return codeMatcher.InstructionEnumeration();
        }
	}
}
