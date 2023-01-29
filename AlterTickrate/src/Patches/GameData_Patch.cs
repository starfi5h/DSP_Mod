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
        static int beltFactoryCount;

        [HarmonyPrefix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        static void GameMain_Begin()
        {
            if (GameMain.data != null)
            {
                facilityFactories = new PlanetFactory[GameMain.data.factories.Length];
                inserterFactories = new PlanetFactory[GameMain.data.factories.Length];
            }
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

            if (ConfigSettings.EnableFacility)
            {
                facilityFactoryCount = 0;
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    if ((i + gameTick) % ConfigSettings.FacilityUpdatePeriod == 0)
                    {
                        facilityFactories[facilityFactoryCount] = GameMain.data.factories[i];
                        facilityFactoryCount++;
                    }
                }
            }
            if (ConfigSettings.EnableSorter)
            {
                inserterFactoryCount = 0;
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    if ((i + gameTick) % ConfigSettings.SorterUpdatePeriod == 0)
                    {
                        inserterFactories[inserterFactoryCount] = GameMain.data.factories[i];
                        inserterFactoryCount++;
                    }
                }
            }
            if (ConfigSettings.EnableBelt)
            {
                beltFactoryCount = 0;
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    if ((i + gameTick) % ConfigSettings.BeltUpdatePeriod == 0)
                    {
                        beltFactories[beltFactoryCount] = GameMain.data.factories[i];
                        beltFactoryCount++;
                    }
                }
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                IEnumerable<CodeInstruction> newInstructions = instructions;

                if (ConfigSettings.EnableFacility)
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

                if (ConfigSettings.EnableSorter)
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

                if (ConfigSettings.EnableBelt)
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

		[HarmonyPrefix, HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
		public static bool GameTick(FactorySystem __instance, long time, bool isActive, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
		{
			GameHistoryData history = GameMain.history;
			FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[__instance.factory.index];
			int[] productRegister = factoryProductionStat.productRegister;
			int[] consumeRegister = factoryProductionStat.consumeRegister;
			PowerSystem powerSystem = __instance.factory.powerSystem;
			float[] networkServes = powerSystem.networkServes;
			EntityData[] entityPool = __instance.factory.entityPool;
			VeinData[] veinPool = __instance.factory.veinPool;
			AnimData[] entityAnimPool = __instance.factory.entityAnimPool;
			SignData[] entitySignPool = __instance.factory.entitySignPool;
			int[][] entityNeeds = __instance.factory.entityNeeds;
			PowerConsumerComponent[] consumerPool = powerSystem.consumerPool;
			float num = 0.016666668f * ConfigSettings.FacilityUpdatePeriod;
			AstroData[] astroPoses = null;
			bool flag = isActive || (time + (long)__instance.factory.index) % 15L == 0L;
			int num2;
			int num3;
			if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.minerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out num2, out num3))
			{
				float num5;
				float num4 = num5 = __instance.factory.gameData.gameDesc.resourceMultiplier;
				if (num5 < 0.41666666f)
				{
					num5 = 0.41666666f;
				}
				float num6 = history.miningCostRate;
				float miningSpeedScale = history.miningSpeedScale;
				float num7 = history.miningCostRate * 0.40111667f / num5;
				if (num4 > 99.5f)
				{
					num6 = 0f;
					num7 = 0f;
				}
				bool flag2 = isActive && num6 > 0f;
				for (int i = num2; i < num3; i++)
				{
					if (__instance.minerPool[i].id == i)
					{
						int entityId = __instance.minerPool[i].entityId;
						int stationId = entityPool[entityId].stationId;
						float num8 = networkServes[consumerPool[__instance.minerPool[i].pcId].networkId];
						uint num9 = __instance.minerPool[i].InternalUpdate(__instance.factory, veinPool, num8, (__instance.minerPool[i].type == EMinerType.Oil) ? num7 : num6, miningSpeedScale, productRegister);
						int num10 = (int)Mathf.Floor(entityAnimPool[entityId].time / 10f);
						entityAnimPool[entityId].time = entityAnimPool[entityId].time % 10f;
						entityAnimPool[entityId].Step(num9, num * num8);
						entityAnimPool[entityId].power = num8;
						if (stationId > 0)
						{
							if (__instance.minerPool[i].veinCount > 0)
							{
								EVeinType veinTypeByItemId = LDB.veins.GetVeinTypeByItemId(veinPool[__instance.minerPool[i].veins[0]].productId);
								AnimData[] array = entityAnimPool;
								int num11 = entityId;
								array[num11].state = (uint)((byte)array[num11].state + ((int)veinTypeByItemId * 100));
							}
							AnimData[] array2 = entityAnimPool;
							int num12 = entityId;
							array2[num12].power = array2[num12].power + 10f;
							AnimData[] array3 = entityAnimPool;
							int num13 = entityId;
							array3[num13].power = array3[num13].power + (float)__instance.minerPool[i].speed;
							if (num9 == 1U)
							{
								num10 = 3000;
							}
							else
							{
								num10 -= (int)(num * 1000f);
								if (num10 < 0)
								{
									num10 = 0;
								}
							}
							AnimData[] array4 = entityAnimPool;
							int num14 = entityId;
							array4[num14].time = array4[num14].time + (float)(num10 * 10);
						}
						if (entitySignPool[entityId].signType == 0U || entitySignPool[entityId].signType > 3U)
						{
							entitySignPool[entityId].signType = ((__instance.minerPool[i].minimumVeinAmount < 1000) ? 7U : 0U);
						}
						if (flag2 && __instance.minerPool[i].type == EMinerType.Vein)
						{
							if ((long)i % 30L == time % 30L)
							{
								__instance.minerPool[i].GetTotalVeinAmount(veinPool);
							}
							entitySignPool[entityId].count0 = (float)__instance.minerPool[i].totalVeinAmount;
						}
						else
						{
							entitySignPool[entityId].count0 = 0f;
						}
					}
				}
			}
			if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.assemblerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out num2, out num3))
			{
				if (flag)
				{
					for (int j = num2; j < num3; j++)
					{
						if (__instance.assemblerPool[j].id == j)
						{
							ref AssemblerComponent ptr = ref __instance.assemblerPool[j];
							int entityId2 = ptr.entityId;
							uint num15 = 0U;
							float num16 = networkServes[consumerPool[ptr.pcId].networkId];
							if (ptr.recipeId != 0)
							{
								ptr.UpdateNeeds();
								num15 = ptr.InternalUpdate(num16, productRegister, consumeRegister);
							}
							if (ptr.recipeType == ERecipeType.Chemical)
							{
								entityAnimPool[entityId2].working_length = 2f;
								entityAnimPool[entityId2].Step(num15, num * num16);
								entityAnimPool[entityId2].power = num16;
								entityAnimPool[entityId2].working_length = (float)ptr.recipeId;
							}
							else
							{
								entityAnimPool[entityId2].Step(num15, num * num16);
								entityAnimPool[entityId2].power = num16;
							}
							entityNeeds[entityId2] = ptr.needs;
							if (entitySignPool[entityId2].signType == 0U || entitySignPool[entityId2].signType > 3U)
							{
								entitySignPool[entityId2].signType = ((ptr.recipeId == 0) ? 4U : ((num15 > 0U) ? 0U : 6U));
							}
						}
					}
				}
				else
				{
					for (int k = num2; k < num3; k++)
					{
						if (__instance.assemblerPool[k].id == k)
						{
							int entityId3 = __instance.assemblerPool[k].entityId;
							float power = networkServes[consumerPool[__instance.assemblerPool[k].pcId].networkId];
							if (__instance.assemblerPool[k].recipeId != 0)
							{
								__instance.assemblerPool[k].UpdateNeeds();
								__instance.assemblerPool[k].InternalUpdate(power, productRegister, consumeRegister);
							}
							entityNeeds[entityId3] = __instance.assemblerPool[k].needs;
						}
					}
				}
			}
			if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.fractionatorCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out num2, out num3))
			{
				for (int l = num2; l < num3; l++)
				{
					if (__instance.fractionatorPool[l].id == l)
					{
						int entityId4 = __instance.fractionatorPool[l].entityId;
						float power2 = networkServes[consumerPool[__instance.fractionatorPool[l].pcId].networkId];
						uint state = __instance.fractionatorPool[l].InternalUpdate(__instance.factory, power2, entitySignPool, productRegister, consumeRegister);
						entityAnimPool[entityId4].time = Mathf.Sqrt((float)__instance.fractionatorPool[l].fluidInputCount * 0.025f);
						entityAnimPool[entityId4].state = state;
						entityAnimPool[entityId4].power = power2;
					}
				}
			}
			EjectorComponent[] obj = __instance.ejectorPool;
			lock (obj)
			{
				if (__instance.ejectorCursor - __instance.ejectorRecycleCursor > 1)
				{
					astroPoses = __instance.planet.galaxy.astrosData;
				}
			}
			DysonSwarm swarm = null;
			if (__instance.factory.dysonSphere != null)
			{
				swarm = __instance.factory.dysonSphere.swarm;
			}
			if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.ejectorCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out num2, out num3))
			{
				for (int m = num2; m < num3; m++)
				{
					if (__instance.ejectorPool[m].id == m)
					{
						int entityId5 = __instance.ejectorPool[m].entityId;
						float power3 = networkServes[consumerPool[__instance.ejectorPool[m].pcId].networkId];
						uint state2 = __instance.ejectorPool[m].InternalUpdate(power3, swarm, astroPoses, entityAnimPool, consumeRegister);
						entityAnimPool[entityId5].state = state2;
						entityNeeds[entityId5] = __instance.ejectorPool[m].needs;
						if (entitySignPool[entityId5].signType == 0U || entitySignPool[entityId5].signType > 3U)
						{
							entitySignPool[entityId5].signType = ((__instance.ejectorPool[m].orbitId > 0) ? 0U : 5U);
						}
					}
				}
			}
			SiloComponent[] obj2 = __instance.siloPool;
			lock (obj2)
			{
				if (__instance.siloCursor - __instance.siloRecycleCursor > 1)
				{
					astroPoses = __instance.planet.galaxy.astrosData;
				}
			}
			DysonSphere dysonSphere = __instance.factory.dysonSphere;
			bool flag4 = dysonSphere != null && dysonSphere.autoNodeCount > 0;
			if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.siloCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out num2, out num3))
			{
				for (int n = num2; n < num3; n++)
				{
					if (__instance.siloPool[n].id == n)
					{
						int entityId6 = __instance.siloPool[n].entityId;
						float power4 = networkServes[consumerPool[__instance.siloPool[n].pcId].networkId];
						uint state3 = __instance.siloPool[n].InternalUpdate(power4, dysonSphere, entityAnimPool, consumeRegister);
						entityAnimPool[entityId6].state = state3;
						entityNeeds[entityId6] = __instance.siloPool[n].needs;
						if (entitySignPool[entityId6].signType == 0U || entitySignPool[entityId6].signType > 3U)
						{
							entitySignPool[entityId6].signType = (flag4 ? 0U : 9U);
						}
					}
				}
			}

			return false;
		}
	}
}
