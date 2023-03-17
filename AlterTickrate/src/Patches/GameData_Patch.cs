using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
    public class GameData_Patch
    {
        static PlanetFactory[] facilityFactories = new PlanetFactory[0];
        static PlanetFactory[] beltFactories = new PlanetFactory[0];
        static int facilityFactoryCount;
        static int inserterFactoryCount;
        static int storageFacotryCount;
        static int beltFactoryCount;
        static int beltAddonFactoryCount; // Splitter, Monitor, Spraycoater, Piler

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                facilityFactories = new PlanetFactory[GameMain.data.factories.Length];
                beltFactories = new PlanetFactory[GameMain.data.factories.Length];
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix(GameData __instance, long time)
        {
            if (__instance.factories.Length != facilityFactories.Length)
            {
                facilityFactories = new PlanetFactory[__instance.factories.Length];
                beltFactories = new PlanetFactory[__instance.factories.Length];
            }
			PlanetFactory localFactory = GameMain.localPlanet?.factory;

            facilityFactoryCount = 0;
            for (int i = 0; i < __instance.factoryCount; i++)
            {
                if ((i + time) % Parameters.FacilityUpdatePeriod == 0)
                {
                    facilityFactories[facilityFactoryCount++] = __instance.factories[i];
                }
            }
			if (localFactory != null && (localFactory.index + time) % Parameters.FacilityUpdatePeriod != 0)
            {
				//Log.Warn($"{ConfigSettings.FacilityUpdatePeriod} {gameTick % ConfigSettings.FacilityUpdatePeriod} {(localFactory.index + gameTick) % ConfigSettings.FacilityUpdatePeriod}");
				facilityFactories[facilityFactoryCount++] = localFactory;
				Parameters.AnimOnlyFactory = localFactory;
			}
			else
            {
				Parameters.AnimOnlyFactory = null;
			}

            inserterFactoryCount = time % Parameters.InserterUpdatePeriod == 0 ? __instance.factoryCount : 0;
            storageFacotryCount = time % Parameters.StorageUpdatePeriod == 0 ? __instance.factoryCount : 0;

            beltFactoryCount = 0;
            beltAddonFactoryCount = 0;
            Parameters.LocalCargoContainer = null;
            if (time % Parameters.BeltUpdatePeriod == 0)
            {
                for (int i = 0; i < __instance.factoryCount; i++)
                {
                    beltFactories[beltFactoryCount++] = __instance.factories[i];
                }
                beltAddonFactoryCount = __instance.factoryCount;
            }
            else
            {
#if !DEBUG
                if (localFactory != null) // Make belt always update on local planet to make blue belt looks normal
                {
                    beltFactories[beltFactoryCount++] = localFactory;
                    Parameters.LocalCargoContainer = localFactory.cargoContainer;
                }
#endif
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                IEnumerable<CodeInstruction> newInstructions = instructions;

                if (true) // Facility & Power
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
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    int endPos = codeMatcher.Pos; //IL #198
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(facilityFactories), nameof(facilityFactoryCount));


                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Lab);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Facility);
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Lab), // 17
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    startPos = codeMatcher.Pos; //IL #212

                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Facility), //12
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    endPos = codeMatcher.Pos; //IL #261
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(facilityFactories), nameof(facilityFactoryCount));
                }

                if (true) // Sorter
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
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, "", nameof(inserterFactoryCount));
                }

                if (true) // Storage
                {
                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
                    var codeMatcher = new CodeMatcher(newInstructions);
                    for (int i = 0; i < 3; i++)
                    {
                        codeMatcher.MatchForward(false,
                            new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Storage), //19
                            new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                        int startPos = codeMatcher.Pos;

                        codeMatcher.MatchForward(false,
                            new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Storage), //19
                            new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                        int endPos = codeMatcher.Pos;
                        newInstructions = ReplaceFactories(newInstructions, startPos, endPos, "", nameof(storageFacotryCount));
                    }
                }

                
                if (Parameters.BeltUpdatePeriod > 1)
                {
                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
                    var codeMatcher = new CodeMatcher(newInstructions);
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt), //9
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    int startPos = codeMatcher.Pos;

                    codeMatcher
                        .MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    int endPos = codeMatcher.Pos;
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, nameof(beltFactories), nameof(beltFactoryCount));


                    // Start: PerformanceMonitor.BeginSample(ECpuWorkEntry.Splitter);
                    // End:   PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
                    startPos = codeMatcher.Pos;
                    codeMatcher
                        .MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    endPos = codeMatcher.Pos;
                    newInstructions = ReplaceFactories(newInstructions, startPos, endPos, "", nameof(beltAddonFactoryCount));
                }

                return newInstructions;
            }
            catch
            {
                Log.Error("Transpiler GameData.GameTick failed");
                return instructions;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceFactories(IEnumerable<CodeInstruction> instructions, int start, int end, string factoriesName, string factoryCountName)
        {
            var codeMatcher = new CodeMatcher(instructions);

            // replace GameData.factories with factoriesField
            if (factoriesName != "")
            {
                var factoriesField = AccessTools.Field(typeof(GameData_Patch), factoriesName);
                codeMatcher.Start().Advance(start);
                while (true)
                {
                    codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"));
                    if (codeMatcher.IsInvalid || codeMatcher.Pos >= end)
                        break;

                    codeMatcher
                        .Advance(-1)
                        .SetAndAdvance(OpCodes.Nop, null)
                        .SetAndAdvance(OpCodes.Ldsfld, factoriesField);
                }
            }

            // replace GameData.factoryCount with factoryCountField
            var factoryCountField = AccessTools.Field(typeof(GameData_Patch), factoryCountName);
            codeMatcher.Start().Advance(start);
            while (true)
            {
                codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factoryCount"));
                if (codeMatcher.IsInvalid || codeMatcher.Pos >= end)
                    break;

                codeMatcher
                    .Advance(-1)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Ldsfld, factoryCountField);
            }

            return codeMatcher.InstructionEnumeration();
        }
	}
}
