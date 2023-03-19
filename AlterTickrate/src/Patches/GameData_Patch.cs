using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
    public class GameData_Patch
    {
        static PlanetFactory[] powerFactories = new PlanetFactory[0];
        static int powerFactoryCount;
        static int researchLabFactoryCount;
        static int liftLabFactoryCount;
        static int inserterFactoryCount;
        static int storageFacotryCount;
        static int beltFactoryCount;
        static int beltAddonFactoryCount; // Splitter, Monitor, Spraycoater, Piler

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        static void GameTick_Prefix(GameData __instance, long time)
        {
            if (__instance.factories.Length != powerFactories.Length)
            {
                powerFactories = new PlanetFactory[__instance.factories.Length];
            }
			PlanetFactory localFactory = GameMain.localPlanet?.factory;

            powerFactoryCount = 0;
            researchLabFactoryCount = 0;
            for (int i = 0; i < __instance.factoryCount; i++)
            {
                int offsetIndex = __instance.factories[i].index + (int)time;
                if (offsetIndex % Parameters.PowerUpdatePeriod == 0)
                    powerFactories[powerFactoryCount++] = __instance.factories[i];
            }
            if (localFactory != null)
            {
                int offsetIndex = localFactory.index + (int)time;
                if (offsetIndex % Parameters.PowerUpdatePeriod != 0)
                    powerFactories[powerFactoryCount++] = localFactory;
            }

            researchLabFactoryCount = time % Parameters.LabResearchUpdatePeriod == 0 ? __instance.factoryCount : 0;
            liftLabFactoryCount = time % Parameters.LabLiftUpdatePeriod == 0 ? __instance.factoryCount : 0;
            inserterFactoryCount = time % Parameters.InserterUpdatePeriod == 0 ? __instance.factoryCount : 0;
            storageFacotryCount = time % Parameters.StorageUpdatePeriod == 0 ? __instance.factoryCount : 0;

            beltFactoryCount = 0;
            beltAddonFactoryCount = 0;
            if (time % Parameters.BeltUpdatePeriod == 0)
            {
                beltFactoryCount = __instance.factoryCount;
                beltAddonFactoryCount = __instance.factoryCount;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codeMatcher = new CodeMatcher(instructions);
                int startPos, endPos;

                // Power system
                {
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_8), // ECpuWorkEntry.PowerSystem = 8
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    startPos = codeMatcher.Pos; //IL #94

                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Facility), //12
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    endPos = codeMatcher.Pos; //IL #198
                    ReplaceFactories(codeMatcher, startPos, endPos, nameof(powerFactories), nameof(powerFactoryCount));
                }

                // Facility is done in Facility_Patch
                // Lab Produce is done in LabProduce_Patch

                // Lab
                if (Parameters.LabResearchUpdatePeriod > 1 || Parameters.LabLiftUpdatePeriod > 1)
                {
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Lab), // 17
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    startPos = codeMatcher.Pos; //IL #212
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_multithreadSystem"));
                    endPos = codeMatcher.Pos; //IL #247
                    ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(researchLabFactoryCount));

                    startPos = endPos;
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Lab), //17
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    endPos = codeMatcher.Pos; //IL #259
                    ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(liftLabFactoryCount));
                }

                if (Parameters.InserterUpdatePeriod > 1)
                {
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Inserter), //10
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    startPos = codeMatcher.Pos; //IL #307

                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Inserter), //10
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    endPos = codeMatcher.Pos; //IL #321
                    ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(inserterFactoryCount));
                }

                if (Parameters.StorageUpdatePeriod > 1)
                {
                    codeMatcher.Start();
                    for (int i = 0; i < 3; i++)
                    {
                        codeMatcher.MatchForward(false,
                            new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Storage), //19
                            new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                        startPos = codeMatcher.Pos;

                        codeMatcher.MatchForward(false,
                            new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Storage), //19
                            new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                        endPos = codeMatcher.Pos;
                        ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(storageFacotryCount));
                    }
                }
                
                if (Parameters.BeltUpdatePeriod > 1)
                {
                    codeMatcher.Start();
                    codeMatcher.MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt), //9
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    startPos = codeMatcher.Pos; //IL #360

                    codeMatcher.MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Belt),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndSample"));
                    endPos = codeMatcher.Pos; //IL #375
                    ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(beltFactoryCount));


                    // DspOpt remove the dup Belt sample so it needs to mark with next line BeginSample(ECpuWorkEntry.Storage)
                    startPos = codeMatcher.Pos;
                    codeMatcher.MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Ldc_I4_S && (sbyte)i.operand == (sbyte)ECpuWorkEntry.Storage),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginSample"));
                    endPos = codeMatcher.Pos; //IL #444
                    ReplaceFactories(codeMatcher, startPos, endPos, "", nameof(beltAddonFactoryCount));
                }

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler GameData.GameTick failed");
                return instructions;
            }
        }

        public static void ReplaceFactories(CodeMatcher codeMatcher, int start, int end, string factoriesName, string factoryCountName)
        {
            //Log.Debug($"{start} {end} {factoryCountName}");

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

            codeMatcher.Start().Advance(end);
        }
	}
}
