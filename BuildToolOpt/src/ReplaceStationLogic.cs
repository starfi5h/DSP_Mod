using HarmonyLib;
using System;

namespace BuildToolOpt
{
    class ReplaceStationLogic
    {
		public static bool IsReplacing { get; private set; }
		static int stationId;

		[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
		public static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool_Click __instance, ref bool __result)
		{
			stationId = 0;
			if (__instance.buildPreviews.Count == 1)
			{
				BuildPreview buildPreview = __instance.buildPreviews[0];
				if (buildPreview.desc.isStation && buildPreview.condition == EBuildCondition.Collide)
				{
					if (buildPreview.desc.isVeinCollector || buildPreview.desc.isCollectStation) //不取代大礦機和軌道採集器
						return;

					int entityId = GetOverlapStationEntityId(__instance);
					if (entityId > 0)
					{
						// Snap to the overlap station
						ref var entityData = ref __instance.factory.entityPool[entityId];
						stationId = entityData.stationId;
						buildPreview.lpos = entityData.pos;
						buildPreview.lrot = entityData.rot;

						// Warn user about station getting too close, but still allow them to build
						buildPreview.condition = EBuildCondition.Ok;
						if (!VFInput.onGUI) UICursor.SetCursor(ECursor.Default);
						__result = true;
						var realCondition = GetRealStationCondition(__instance, buildPreview);
						__instance.actionBuild.model.cursorState = realCondition == EBuildCondition.Ok ? 0 : -1;
						__instance.actionBuild.model.cursorText = BuildPreview.GetConditionText(realCondition);
						return;
					}
				}
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.ConfirmOperation))]
		public static void ConfirmOperation_Postfix(BuildTool_Click __instance, ref bool __result)
		{
			if (__result)
			{
				if (stationId > 0)
				{
					ReplaceStation(__instance, stationId);
					__result = false;
					return;
				}
			}
		}

		[HarmonyPrefix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.TakeBackItems_Station))]
		public static void TakeBackItems_Station(PlanetTransport __instance, int stationId, ref bool __runOriginal)
        {
			__runOriginal = !IsReplacing;
			if (__runOriginal) return;

			// When replacing the old station, don't put items into player package
			if (stationId == 0) return;
			StationComponent stationComponent = __instance.GetStationComponent(stationId);
			if (stationComponent == null || stationComponent.id != stationId)
			{
				return;
			}
			int storageLength = stationComponent.storage?.Length ?? 0;
			for (int i = 0; i < storageLength; i++)
			{
				if (stationComponent.storage[i].count > 0 && stationComponent.storage[i].itemId > 0)
				{
					stationComponent.storage[i].count = 0;
					stationComponent.storage[i].inc = 0;
				}
			}
			stationComponent.idleDroneCount = 0;
			stationComponent.idleShipCount = 0;
			stationComponent.warperCount = 0;
			Plugin.Log.LogDebug("TakeBackItems_Station");
        }

		[HarmonyPrefix, HarmonyPatch(typeof(UIItemup), "Up")]
		static void Up()
        {
			Plugin.Log.LogDebug(System.Environment.StackTrace);
        }

		private static int GetOverlapStationEntityId(BuildTool_Click tool)
		{
			PlanetPhysics physics = tool.player.planetData.physics;
			int i = 0;
			while (BuildTool._tmp_cols[i] != null)
			{
				if (physics.GetColliderData(BuildTool._tmp_cols[i], out ColliderData colliderData))
				{
					if (colliderData.objType == EObjectType.Entity && tool.factory.entityPool[colliderData.objId].stationId > 0)
						return colliderData.objId;
				}
				i++;
			}
			return 0;
		}

		private static EBuildCondition GetRealStationCondition(BuildTool_Click tool, BuildPreview buildPreview)
        {
			var stationPool = tool.factory.transport.stationPool;
			var stationCursor = tool.factory.transport.stationCursor;
			var prebuildPool = tool.factory.prebuildPool;
			var prebuildCursor = tool.factory.prebuildCursor;
			var entityPool = tool.factory.entityPool;
			float PLSlimit = 225f;
			float ILSlimit = 841f;
			for (int id = 1; id < stationCursor; id++)
			{
				if (id == stationId) continue; // Skip the orignal station

				if (stationPool[id] != null && stationPool[id].id == id)
				{
					float maxSqrDist = ((stationPool[id].isStellar || buildPreview.desc.isStellarStation) ? ILSlimit : PLSlimit);
					if ((entityPool[stationPool[id].entityId].pos - buildPreview.lpos).sqrMagnitude < maxSqrDist)
					{
						if (stationPool[id].isVeinCollector)
						{
							return EBuildCondition.MK2MinerTooClose;
						}
						else
						{
							return EBuildCondition.TowerTooClose;
						}
					}
				}
			}
			for (int id = 1; id < prebuildCursor; id++)
			{
				if (prebuildPool[id].id == id)
				{
					var itemProto = LDB.items.Select(prebuildPool[id].protoId);
					if (itemProto != null && itemProto.prefabDesc.isStation)
					{
						float maxSqrDist = ((itemProto.prefabDesc.isStellarStation || buildPreview.desc.isStellarStation) ? ILSlimit : PLSlimit);
						if (buildPreview.desc.isVeinCollector && itemProto.prefabDesc.isVeinCollector)
						{
							maxSqrDist = 0f;
						}
						if ((prebuildPool[id].pos - buildPreview.lpos).sqrMagnitude < maxSqrDist)
						{
							if (itemProto.prefabDesc.isVeinCollector)
							{
								return EBuildCondition.MK2MinerTooClose;
							}
							else
							{
								return EBuildCondition.TowerTooClose;
							}
						}
					}
				}
			}
			return EBuildCondition.Ok;
		}

		public static void ReplaceStation(BuildTool_Click tool, int stationId)
		{
			IsReplacing = true; // flag for other mods compat

			// Save status of old station
			StationComponent oldStation = tool.factory.transport.stationPool[stationId];
			BuildingParameters parameters = default;
			parameters.CopyFromFactoryObject(oldStation.entityId, tool.factory);
			SlotData[] slots = oldStation.slots;
			SaveState(oldStation, out StationState state);
			tool.actionBuild.DoDismantleObject(oldStation.entityId);

			// Create prebuild and apply config
			tool.buildPreviews[0].parameters = parameters.parameters;
			tool.buildPreviews[0].paramCount = parameters.parameters.Length;
			tool.CreatePrebuilds();
			int objId = tool.buildPreviews[0].objId;
			ReconnectBelts(tool.factory, objId, slots);

			// Build and apply state				
			stationId = tool.factory.transport.stationCursor;
			if (tool.factory.transport.stationRecycleCursor > 0)
				stationId = tool.factory.transport.stationRecycle[tool.factory.transport.stationRecycleCursor - 1];
			tool.factory.BuildFinally(tool.player, -objId);
			StationComponent newStation = tool.factory.transport.stationPool[stationId];
			LoadState(newStation, state);

			IsReplacing = false;
		}

		private static void ReconnectBelts(PlanetFactory factory, int objId, SlotData[] slots)
		{
			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i].beltId > 0)
				{
					int BeltEntityId = factory.cargoTraffic.beltPool[slots[i].beltId].entityId;
					if (slots[i].dir == IODir.Output)
						factory.WriteObjectConn(objId, i, true, BeltEntityId, 1);
					else
						factory.WriteObjectConn(objId, i, false, BeltEntityId, 0);
				}
			}
		}

		struct StationState
		{
			public long energy;
			public int warperCount;
			public int droneCount;
			public int shipCount;
			public StationStore[] storage;
		}

		private static void SaveState(StationComponent station, out StationState state)
		{
			state = default;
			state.energy = station.energy;
			state.warperCount = station.warperCount;
			state.droneCount = station.idleDroneCount + station.workDroneCount;
			state.shipCount = station.idleShipCount + station.workShipCount;
			state.storage = new StationStore[station.storage.Length];
			for (int i = 0; i < station.storage.Length; i++)
				state.storage[i] = station.storage[i];
		}

		private static void LoadState(StationComponent station, in StationState state)
		{
			station.energy = state.energy;

			var itemCountInStation = station.warperCount;
			station.warperCount = Math.Min(state.warperCount, station.warperMaxCount);
			RefundItem(1210, state.warperCount - station.warperMaxCount + itemCountInStation, 0, station.entityId);

			itemCountInStation = station.idleDroneCount;
			station.idleDroneCount = Math.Min(state.droneCount, station.workDroneDatas.Length);
			RefundItem(5001, state.droneCount - station.workDroneDatas.Length + itemCountInStation, 0, station.entityId);

			itemCountInStation = station.idleShipCount;
			station.idleShipCount = Math.Min(state.shipCount, station.workShipDatas.Length);
			RefundItem(5002, state.shipCount - station.workShipDatas.Length + itemCountInStation, 0, station.entityId);

			int length = Math.Min(station.storage.Length, state.storage.Length);
			for (int i = 0; i < length; i++)
			{
				station.storage[i].count = state.storage[i].count;
				station.storage[i].inc = state.storage[i].inc;
			}
			for (int i = length; i < state.storage.Length; i++)
			{
				StationStore store = state.storage[i];
				RefundItem(store.itemId, store.count, store.inc, station.entityId);
			}
		}

		private static void RefundItem(int itemId, int count, int inc, int objId)
		{
			if (count > 0)
			{
				GameMain.mainPlayer.TryAddItemToPackage(itemId, count, inc, true, objId);
				UIItemup.Up(itemId, count);
			}
		}
	}
}
