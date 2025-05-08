using HarmonyLib;
using System;
using System.Collections.Generic;

namespace BuildToolOpt
{
    static class GalacticTransport_Patch
    {

#if DEBUG
		static HighStopwatch stopwatch = new();

		[HarmonyPostfix, HarmonyPatch(typeof(GalacticTransport), nameof(GalacticTransport.RefreshTraffic))]
		static void RefreshTraffic_Postfix()
        {
			Plugin.Log.LogDebug("RefreshTraffic time: " + stopwatch.duration);
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

			Plugin.Log.LogDebug("GalacticTransport.RefreshTraffic key gid = " + keyStationGId);
			var keyStation = __instance.stationPool[keyStationGId];
			int oldPairCount = keyStation.remotePairOffsets?[6] ?? 0;
			var otherGIds = ClearOtherStationRemotePairs(__instance.stationPool, keyStation);
			
			keyStation.ClearRemotePairs();			
			AddRemotePairs(keyStation, __instance);
			int newPairCount = keyStation.remotePairOffsets?[6] ?? 0;
			Plugin.Log.LogDebug($"Related station count = {otherGIds.Count}. RemotePairs count {oldPairCount} => {newPairCount}");

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

			int num = @this.storage.Length;
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
