using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BuildToolOpt
{
    static class GalacticTransport_Patch
    {
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.AddStation2StationRoute))]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RemoveStation2StationRoute),
			new Type[] { typeof(int) })]
		public static IEnumerable<CodeInstruction> S2S_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace: this.RefreshTraffic(0);
			// With:    RefreshTraffic_S2S(this, gid0, gid1);
			var codeMacher = new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "RefreshTraffic"))
				.Repeat(matcher => matcher
					.Advance(-1)
					.RemoveInstructions(2)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GalacticTransport_Patch), nameof(RefreshTraffic_S2S)))
					));

			return codeMacher.InstructionEnumeration();
		}

		public static void RefreshTraffic_S2S(GalacticTransport galacticTransport, int gid0, int gid1)
        {
			// Refresh only two stations and those pair with them
			galacticTransport.RefreshTraffic(gid0);
			galacticTransport.RefreshTraffic(gid1);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.AddAstro2AstroBan))]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.AddAstro2AstroRoute))]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RemoveAstro2AstroBan))]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RemoveAstro2AstroRoute))]
		public static IEnumerable<CodeInstruction> A2A_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Replace: this.RefreshTraffic(0);
			// With:    RefreshTraffic_A2A(astroId0, astroId1, itemId);
			var codeMacher = new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "RefreshTraffic"))
				.Repeat(matcher => matcher
					.Advance(-2)
					.SetAndAdvance(OpCodes.Nop, null)
					.RemoveInstructions(2)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Ldarg_3),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GalacticTransport_Patch), nameof(RefreshTraffic_A2A)))
					));

			return codeMacher.InstructionEnumeration();
		}

		public static void RefreshTraffic_A2A(int astroId0, int astroId1, int itemId)
		{
			// Refresh only stations on the target planet / system
			RefreshTraffic_Astro(astroId0, itemId);
			RefreshTraffic_Astro(astroId1, itemId);
		}

		public static void RefreshTraffic_Astro(int astroId, int itemId)
        {
			if (astroId % 100 == 0)
			{
				var star = GameMain.data.galaxy.StarById(astroId / 100);
				if (star != null)
				{
					foreach (var planet in star.planets)
					{
						if (planet?.factory != null)
						{
							RefreshTraffic_Planet(planet.factory.transport, itemId);
						}
					}
				}
			}
			else
            {
				var planet = GameMain.data.galaxy.PlanetById(astroId);
				if (planet?.factory != null)
                {
					RefreshTraffic_Planet(planet.factory.transport, itemId);
				}
            }
		}

		public static void RefreshTraffic_Planet(PlanetTransport planetTransport, int itemId)
        {
			// Refresh only stations that have the item, and those pair with them
			for (int staionId = 1; staionId < planetTransport.stationCursor; staionId++)
            {
				var station = planetTransport.stationPool[staionId];
				if (station?.storage == null || !station.isStellar) continue;

				bool hasItem = false;
				for (int i = 0; i < station.storage.Length; i++)
                {
					if (station.storage[i].itemId == itemId)
                    {
						hasItem = true;
						break;
                    }
                }
				if (!hasItem) continue;
				GameMain.data.galacticTransport.RefreshTraffic(station.gid);
            }
        }

		[HarmonyPrefix, HarmonyPriority(Priority.Low)]
		[HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RemoveStationComponent))]
		static void RemoveStationComponent_Prefix(ref bool __runOriginal, GalacticTransport __instance, int gid)
        {
			if (!__runOriginal) return;

			if (__instance.stationPool[gid] != null)
			{
				// 如果remote pair已經淨空, 那就不用呼叫refresh traffic
				int remotePairCount = __instance.stationPool[gid].remotePairOffsets?[6] ?? 0;
				Plugin.Log.LogDebug($"Remove Station[{gid}]: remote pair count = {remotePairCount}");
				if (remotePairCount > 0)
				{
					var storage = __instance.stationPool[gid].storage;
					int length = storage?.Length ?? 0;
					for (int i = 0; i < length; i++)
					{
						// 重置remoteLogic, 使AddRemotePairs不增加新的pair
						storage[i].remoteLogic = ELogisticStorage.None;
					}
					// 改用新的的RefreshTraffic, 只移除相關station的pairing
					__instance.RefreshTraffic(gid);
				}

				__instance.stationPool[gid] = null;
				int[] array = __instance.stationRecycle;
				int num = __instance.stationRecycleCursor;
				__instance.stationRecycleCursor = num + 1;
				array[num] = gid;
			}

			__instance.RemoveStation2StationRoute(gid);
			// 取代這裡的this.RefreshTraffic(gid), 這個因為station已是null所以會整個重新rematch, 大量耗時
			if (__instance.OnStellarStationRemoved != null)
			{
				__instance.OnStellarStationRemoved();
			}			
			__runOriginal = false;
		}



#if DEBUG
		static HighStopwatch stopwatch = new();

		[HarmonyPostfix, HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RefreshTraffic))]
		static void RefreshTraffic_Postfix(bool __runOriginal)
        {
			Plugin.Log.LogInfo($"RefreshTraffic time: {stopwatch.duration} runOriginal: {__runOriginal}");			
		}

		public static void Print(StationComponent station)
		{
			// Note: remotePairOffsets is monotonically non-decreasing (see AddRemotePair)
			string s = $"station{station.gid}: ";
			for (int i = 0; i <= 6; i++) s += " " + station.remotePairOffsets[i];
			Plugin.Log.LogInfo(s);
		}

#endif

		[HarmonyPrefix, HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RefreshTraffic))]
        public static bool RefreshTraffic_Prefix(GalacticTransport __instance, int keyStationGId)
        {
#if DEBUG
			stopwatch = new HighStopwatch();
			stopwatch.Begin();
#endif
			if (keyStationGId == 0) return true;
			if (keyStationGId > __instance.stationPool.Length || __instance.stationPool[keyStationGId] == null) return true;

			
			var keyStation = __instance.stationPool[keyStationGId];
			int oldPairCount = keyStation.remotePairOffsets?[6] ?? 0;
			Plugin.Log.LogDebug($"GalacticTransport.RefreshTraffic key gid = {keyStationGId} pair count = {oldPairCount}");
			var otherGIds = ClearOtherStationRemotePairs(__instance.stationPool, keyStation);
			
			keyStation.ClearRemotePairs();			
			AddRemotePairs(keyStation, __instance);
			int newPairCount = keyStation.remotePairOffsets?[6] ?? 0;
			//Plugin.Log.LogDebug($"Related station count = {otherGIds.Count}. RemotePairs count {oldPairCount} => {newPairCount}");

			// Update ship status for key station and the stations that have ships going to the key station
			int logisticShipCarries = GameMain.history.logisticShipCarries;
			UpdateShipStatus(keyStation, __instance, keyStationGId, logisticShipCarries);
			foreach (int gid in otherGIds) UpdateShipStatus(__instance.stationPool[gid], __instance, keyStationGId, logisticShipCarries);

			// Update remotePairCount for achievement checking
			__instance.remotePairCount = 0;
			for (int gid = 1; gid < __instance.stationCursor; gid++)
			{
				if (__instance.stationPool[gid] != null && __instance.stationPool[gid].gid == gid)
				{
					__instance.remotePairCount += __instance.stationPool[gid].remotePairTotalCount;
				}
			}
			__instance.remotePairCount /= 2;

			return false;
        }

        public static HashSet<int> ClearOtherStationRemotePairs(StationComponent[] stationPool, StationComponent station)
        {
			var otherGIds = new HashSet<int>();
			if (station.remotePairOffsets == null) return otherGIds;
			if (station.remotePairs == null) station.SetRemotePairCapacity(128);

			int pairCount = 0;
			for (int i = 0; i < station.remotePairOffsets[6]; i++)
            {
				ref var remotePair = ref station.remotePairs[i];
                int otherGId = remotePair.supplyId != station.gid ? remotePair.supplyId : remotePair.demandId;
				if (otherGId != 0) otherGIds.Add(otherGId);
				pairCount++;
			}

			int result = 0;
			foreach (var otherGId in otherGIds) result += ClearStationRemotePairs(stationPool[otherGId], station.gid);
			if (result != pairCount) Plugin.Log.LogWarning($"RemotePairs mismatch! local:{pairCount} remote:{result}");
			return otherGIds;
        }

		public static int ClearStationRemotePairs(StationComponent station, int keyStationGId)
        {
			int pairCount = 0;
			int offsetIndex = 6;
			for (int index = station.remotePairOffsets[6] - 1; index >= 0; index--)
			{
				ref var remotePair = ref station.remotePairs[index];
				if (remotePair.demandId != keyStationGId && remotePair.supplyId != keyStationGId) continue;

				while (index < station.remotePairOffsets[offsetIndex - 1])
				{
					if (offsetIndex == 1) break;
					offsetIndex--;
				}

				// Plugin.Log.LogDebug($"[{index}] o:{offsetIndex} s:{remotePair.supplyId} d:{remotePair.demandId}");

				// Shift array elements to fill the gap
				if (index < station.remotePairOffsets[6] - 1)
				{
					Array.Copy(station.remotePairs, index + 1, station.remotePairs, index, station.remotePairOffsets[6] - index - 1);
				}
				// Decrease offsets for this and all subsequent ranges
				for (int i = offsetIndex; i <= 6; i++)
				{
					station.remotePairOffsets[i]--;
				}
				// TODO: Does the order of remotePairs in the same group matter?
				pairCount++;
			}

			return pairCount;	
		}
    		
		public static void AddRemotePairs(StationComponent @this, GalacticTransport galacticTransport)
        {
			// Upper half part of StationComponent.RematchRemotePairs

			int num = @this.storage?.Length ?? 0;
			StationComponent[] stationPool = galacticTransport.stationPool;
			int gStationCursor = galacticTransport.stationCursor;
			for (int i = 0; i < num; i++)
			{
				if (@this.storage[i].remoteLogic == ELogisticStorage.Supply)
				{
					int itemId = @this.storage[i].itemId;
					for (int j = 1; j < gStationCursor; j++) // change: start from 1 to search for all stations
					{
						if (stationPool[j] != null && stationPool[j].gid == j && stationPool[j].planetId != @this.planetId)
						{
							StationStore[] array = stationPool[j].storage;
							for (int k = 0; k < array.Length; k++)
							{
								if (itemId == array[k].itemId && array[k].remoteLogic == ELogisticStorage.Demand)
								{
									int num2 = @this.planetId;
									int num3 = stationPool[j].planetId;
									int num4 = num2 / 100 * 100;
									int num5 = num3 / 100 * 100;
									if (!galacticTransport.IsAstro2AstroBanExist(num2, num3, itemId) && !galacticTransport.IsAstro2AstroBanExist(num4, num5, itemId))
									{
										bool flag = stationPool[j].routePriority == ERemoteRoutePriority.Designated || @this.routePriority == ERemoteRoutePriority.Designated;
										if (!flag)
										{
											@this.AddRemotePair(@this.gid, i, j, k);
											stationPool[j].AddRemotePair(@this.gid, i, j, k);
										}
										bool flag2 = false;
										if (galacticTransport.IsStation2StationRouteExist(@this.gid, stationPool[j].gid))
										{
											@this.AddRouteRemotePair(@this.gid, i, j, k, 2);
											stationPool[j].AddRouteRemotePair(@this.gid, i, j, k, 2);
											flag2 = true;
										}
										if (galacticTransport.IsAstro2AstroRouteEnable(num2, num3, itemId))
										{
											@this.AddRouteRemotePair(@this.gid, i, j, k, 3);
											stationPool[j].AddRouteRemotePair(@this.gid, i, j, k, 3);
											flag2 = true;
										}
										if (galacticTransport.IsAstro2AstroRouteEnable(num4, num5, itemId))
										{
											@this.AddRouteRemotePair(@this.gid, i, j, k, 4);
											stationPool[j].AddRouteRemotePair(@this.gid, i, j, k, 4);
											flag2 = true;
										}
										if ((@this.remoteGroupMask & stationPool[j].remoteGroupMask) > 0L)
										{
											@this.AddRouteRemotePair(@this.gid, i, j, k, 5);
											stationPool[j].AddRouteRemotePair(@this.gid, i, j, k, 5);
											flag2 = true;
										}
										if (!flag2 && !flag)
										{
											@this.AddRouteRemotePair(@this.gid, i, j, k, 6);
											stationPool[j].AddRouteRemotePair(@this.gid, i, j, k, 6);
										}
									}
								}
							}
						}
					}
				}
				else if (@this.storage[i].remoteLogic == ELogisticStorage.Demand)
				{
					int itemId2 = @this.storage[i].itemId;
					for (int l = 1; l < gStationCursor; l++) // change: start from 1 to search for all stations
					{
						if (stationPool[l] != null && stationPool[l].gid == l && stationPool[l].planetId != @this.planetId)
						{
							StationStore[] array2 = stationPool[l].storage;
							for (int m = 0; m < array2.Length; m++)
							{
								if (itemId2 == array2[m].itemId && array2[m].remoteLogic == ELogisticStorage.Supply)
								{
									int num6 = @this.planetId;
									int num7 = stationPool[l].planetId;
									int num8 = num6 / 100 * 100;
									int num9 = num7 / 100 * 100;
									if (!galacticTransport.IsAstro2AstroBanExist(num6, num7, itemId2) && !galacticTransport.IsAstro2AstroBanExist(num8, num9, itemId2))
									{
										bool flag3 = stationPool[l].routePriority == ERemoteRoutePriority.Designated || @this.routePriority == ERemoteRoutePriority.Designated;
										if (!flag3)
										{
											@this.AddRemotePair(l, m, @this.gid, i);
											stationPool[l].AddRemotePair(l, m, @this.gid, i);
										}
										bool flag4 = false;
										if (galacticTransport.IsStation2StationRouteExist(@this.gid, stationPool[l].gid))
										{
											@this.AddRouteRemotePair(l, m, @this.gid, i, 2);
											stationPool[l].AddRouteRemotePair(l, m, @this.gid, i, 2);
											flag4 = true;
										}
										if (galacticTransport.IsAstro2AstroRouteEnable(num6, num7, itemId2))
										{
											@this.AddRouteRemotePair(l, m, @this.gid, i, 3);
											stationPool[l].AddRouteRemotePair(l, m, @this.gid, i, 3);
											flag4 = true;
										}
										if (galacticTransport.IsAstro2AstroRouteEnable(num8, num9, itemId2))
										{
											@this.AddRouteRemotePair(l, m, @this.gid, i, 4);
											stationPool[l].AddRouteRemotePair(l, m, @this.gid, i, 4);
											flag4 = true;
										}
										if ((@this.remoteGroupMask & stationPool[l].remoteGroupMask) > 0L)
										{
											@this.AddRouteRemotePair(l, m, @this.gid, i, 5);
											stationPool[l].AddRouteRemotePair(l, m, @this.gid, i, 5);
											flag4 = true;
										}
										if (!flag4 && !flag3)
										{
											@this.AddRouteRemotePair(l, m, @this.gid, i, 6);
											stationPool[l].AddRouteRemotePair(l, m, @this.gid, i, 6);
										}
									}
								}
							}
						}
					}
				}
			}
		}
    
        public static void UpdateShipStatus(StationComponent @this, GalacticTransport galacticTransport, int keyStationGId, int shipCarries)
        {
			// Lower half part (if (keyStationId > 0)) of StationComponent.RematchRemotePairs

			StationComponent[] stationPool = galacticTransport.stationPool;
			if (keyStationGId > 0)
			{
				for (int n = 0; n < @this.workShipCount; n++)
				{
					StationComponent stationComponent = stationPool[@this.workShipDatas[n].otherGId];
					StationStore[] array3 = stationComponent?.storage;
					if (keyStationGId == @this.gid)
					{
						if (@this.workShipDatas[n].itemCount == 0 && @this.workShipDatas[n].direction > 0 && @this.workShipDatas[n].otherGId > 0)
						{
							int itemId3 = @this.workShipDatas[n].itemId;
							if (@this.HasRemoteDemand(itemId3, -10000000) == -1)
							{
								if (@this.workShipOrders[n].itemId > 0)
								{
									if (@this.storage[@this.workShipOrders[n].thisIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array4 = @this.storage;
										int thisIndex = @this.workShipOrders[n].thisIndex;
										array4[thisIndex].remoteOrder = array4[thisIndex].remoteOrder - @this.workShipOrders[n].thisOrdered;
									}
									if (array3 != null && array3[@this.workShipOrders[n].otherIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array5 = array3;
										int otherIndex = @this.workShipOrders[n].otherIndex;
										array5[otherIndex].remoteOrder = array5[otherIndex].remoteOrder - @this.workShipOrders[n].otherOrdered;
									}
									@this.workShipOrders[n].ClearThis();
									@this.workShipOrders[n].ClearOther();
								}
								@this.workShipDatas[n].itemId = 0;
								for (int num10 = 0; num10 < @this.storage.Length; num10++)
								{
									if (@this.storage[num10].remoteLogic == ELogisticStorage.Demand)
									{
										int num11 = stationComponent.HasRemoteSupply(@this.storage[num10].itemId, 1);
										if (num11 >= 0 && @this.storage[num10].remoteDemandCount > 0)
										{
											@this.workShipDatas[n].itemId = @this.storage[num10].itemId;
											@this.workShipDatas[n].direction = 1;
											@this.workShipOrders[n].itemId = @this.workShipDatas[n].itemId;
											@this.workShipOrders[n].otherStationGId = @this.workShipDatas[n].otherGId;
											@this.workShipOrders[n].thisIndex = num10;
											@this.workShipOrders[n].otherIndex = num11;
											@this.workShipOrders[n].thisOrdered = shipCarries;
											@this.workShipOrders[n].otherOrdered = -shipCarries;
											StationStore[] array6 = @this.storage;
											int num12 = num10;
											array6[num12].remoteOrder = array6[num12].remoteOrder + shipCarries;
											StationStore[] array7 = array3;
											int num13 = num11;
											array7[num13].remoteOrder = array7[num13].remoteOrder - shipCarries;
											break;
										}
									}
								}
								if (@this.workShipDatas[n].itemId == 0)
								{
									@this.workShipDatas[n].otherGId = 0;
									@this.workShipDatas[n].direction = -1;
									if (@this.workShipDatas[n].stage == -1)
									{
										@this.workShipDatas[n].pPosTemp = @this.shipDiskPos[@this.workShipDatas[n].shipIndex] + @this.shipDiskPos[@this.workShipDatas[n].shipIndex].normalized * 25f;
									}
								}
							}
						}
						if (@this.workShipDatas[n].itemCount != 0 && @this.workShipDatas[n].direction < 0)
						{
							int itemId4 = @this.workShipDatas[n].itemId;
							if (@this.HasRemoteDemand(itemId4, -10000000) == -1 && @this.workShipOrders[n].itemId > 0)
							{
								if (@this.storage[@this.workShipOrders[n].thisIndex].itemId == @this.workShipOrders[n].itemId)
								{
									StationStore[] array8 = @this.storage;
									int thisIndex2 = @this.workShipOrders[n].thisIndex;
									array8[thisIndex2].remoteOrder = array8[thisIndex2].remoteOrder - @this.workShipOrders[n].thisOrdered;
								}
								if (array3 != null && array3[@this.workShipOrders[n].otherIndex].itemId == @this.workShipOrders[n].itemId)
								{
									StationStore[] array9 = array3;
									int otherIndex2 = @this.workShipOrders[n].otherIndex;
									array9[otherIndex2].remoteOrder = array9[otherIndex2].remoteOrder - @this.workShipOrders[n].otherOrdered;
								}
								@this.workShipOrders[n].ClearThis();
								@this.workShipOrders[n].ClearOther();
								@this.workShipOrders[n].itemId = itemId4;
							}
						}
					}
					if (keyStationGId == @this.workShipDatas[n].otherGId)
					{
						if ((stationPool[@this.workShipDatas[n].otherGId] == null || stationPool[@this.workShipDatas[n].otherGId].gid == 0) && @this.workShipDatas[n].direction > 0)
						{
							if (@this.workShipOrders[n].itemId > 0)
							{
								if (@this.storage[@this.workShipOrders[n].thisIndex].itemId == @this.workShipOrders[n].itemId)
								{
									StationStore[] array10 = @this.storage;
									int thisIndex3 = @this.workShipOrders[n].thisIndex;
									array10[thisIndex3].remoteOrder = array10[thisIndex3].remoteOrder - @this.workShipOrders[n].thisOrdered;
								}
								@this.workShipOrders[n].ClearThis();
								@this.workShipOrders[n].ClearOther();
							}
							@this.workShipDatas[n].otherGId = 0;
							@this.workShipDatas[n].direction = -1;
						}
						else if ((stationPool[@this.workShipDatas[n].otherGId] == null || stationPool[@this.workShipDatas[n].otherGId].gid == 0) && @this.workShipDatas[n].direction < 0)
						{
							@this.workShipDatas[n].otherGId = 0;
							@this.workShipDatas[n].direction = -1;
						}
						else if (@this.workShipDatas[n].itemCount > 0 && @this.workShipDatas[n].direction > 0 && @this.workShipDatas[n].otherGId > 0)
						{
							if (stationComponent.HasRemoteDemand(@this.workShipDatas[n].itemId, 0) == -1)
							{
								if (@this.workShipOrders[n].itemId > 0)
								{
									if (@this.storage[@this.workShipOrders[n].thisIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array11 = @this.storage;
										int thisIndex4 = @this.workShipOrders[n].thisIndex;
										array11[thisIndex4].remoteOrder = array11[thisIndex4].remoteOrder - @this.workShipOrders[n].thisOrdered;
									}
									if (array3[@this.workShipOrders[n].otherIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array12 = array3;
										int otherIndex3 = @this.workShipOrders[n].otherIndex;
										array12[otherIndex3].remoteOrder = array12[otherIndex3].remoteOrder - @this.workShipOrders[n].otherOrdered;
									}
									@this.workShipOrders[n].ClearThis();
									@this.workShipOrders[n].ClearOther();
								}
								@this.workShipDatas[n].otherGId = 0;
								@this.workShipDatas[n].direction = -1;
							}
						}
						else if (@this.workShipDatas[n].itemCount == 0 && @this.workShipDatas[n].direction > 0 && @this.workShipDatas[n].otherGId > 0)
						{
							int itemId5 = @this.workShipDatas[n].itemId;
							if (stationComponent.HasRemoteSupply(itemId5, 0) == -1)
							{
								if (@this.workShipOrders[n].itemId > 0)
								{
									if (@this.storage[@this.workShipOrders[n].thisIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array13 = @this.storage;
										int thisIndex5 = @this.workShipOrders[n].thisIndex;
										array13[thisIndex5].remoteOrder = array13[thisIndex5].remoteOrder - @this.workShipOrders[n].thisOrdered;
									}
									if (array3[@this.workShipOrders[n].otherIndex].itemId == @this.workShipOrders[n].itemId)
									{
										StationStore[] array14 = array3;
										int otherIndex4 = @this.workShipOrders[n].otherIndex;
										array14[otherIndex4].remoteOrder = array14[otherIndex4].remoteOrder - @this.workShipOrders[n].otherOrdered;
									}
									@this.workShipOrders[n].ClearThis();
									@this.workShipOrders[n].ClearOther();
								}
								@this.workShipDatas[n].itemId = 0;
								for (int num14 = 0; num14 < @this.storage.Length; num14++)
								{
									if (@this.storage[num14].remoteLogic == ELogisticStorage.Demand)
									{
										int num15 = stationComponent.HasRemoteSupply(@this.storage[num14].itemId, 1);
										if (num15 >= 0 && @this.storage[num14].remoteDemandCount > 0)
										{
											@this.workShipDatas[n].itemId = @this.storage[num14].itemId;
											@this.workShipDatas[n].direction = 1;
											@this.workShipOrders[n].itemId = @this.workShipDatas[n].itemId;
											@this.workShipOrders[n].otherStationGId = @this.workShipDatas[n].otherGId;
											@this.workShipOrders[n].thisIndex = num14;
											@this.workShipOrders[n].otherIndex = num15;
											@this.workShipOrders[n].thisOrdered = shipCarries;
											@this.workShipOrders[n].otherOrdered = -shipCarries;
											StationStore[] array15 = @this.storage;
											int num16 = num14;
											array15[num16].remoteOrder = array15[num16].remoteOrder + shipCarries;
											StationStore[] array16 = array3;
											int num17 = num15;
											array16[num17].remoteOrder = array16[num17].remoteOrder - shipCarries;
											break;
										}
									}
								}
								if (@this.workShipDatas[n].itemId == 0)
								{
									@this.workShipDatas[n].otherGId = 0;
									@this.workShipDatas[n].direction = -1;
								}
							}
						}
					}
				}
			}
		}
	}
}
