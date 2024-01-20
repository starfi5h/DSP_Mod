using HarmonyLib;
using System;

namespace BuildToolOpt
{
    class ReplaceStationLogic
    {
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
					int entityId = GetOverlapStationEntityId(__instance);
					if (entityId > 0)
					{
						// Snap to the overlap station
						EntityData entityData = __instance.factory.entityPool[entityId];
						stationId = entityData.stationId;
						buildPreview.lpos = entityData.pos;
						buildPreview.lrot = entityData.rot;
						buildPreview.condition = EBuildCondition.Ok;
						__instance.actionBuild.model.cursorText = buildPreview.conditionText;
						__instance.actionBuild.model.cursorState = 0;
						if (!VFInput.onGUI)
							UICursor.SetCursor(ECursor.Default);
						__result = true;
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

		private static int GetOverlapStationEntityId(BuildTool_Click tool)
		{
			PlanetPhysics physics = tool.player.planetData.physics;
			int i = 0;
			while (BuildTool._tmp_cols[i] != null)
			{
				if (physics.GetColliderData(BuildTool._tmp_cols[i], out ColliderData colliderData) && colliderData.isForBuild)
				{
					if (colliderData.objType == EObjectType.Entity && tool.factory.entityPool[colliderData.objId].stationId > 0)
						return colliderData.objId;
				}
				i++;
			}
			return 0;
		}

		public static void ReplaceStation(BuildTool_Click tool, int stationId)
		{
			// Save status of old station
			StationComponent oldStation = tool.factory.transport.stationPool[stationId];
			BuildingParameters parameters = default;
			parameters.CopyFromFactoryObject(oldStation.entityId, tool.factory);
			SlotData[] slots = oldStation.slots;
			SaveState(oldStation, out StationState state);
			CleanState(oldStation);
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

		private static void CleanState(StationComponent station)
		{
			station.warperCount = 0;
			station.idleDroneCount = 0;
			station.workDroneCount = 0;
			station.idleShipCount = 0;
			station.workShipCount = 0;
			for (int i = 0; i < station.storage.Length; i++)
			{
				station.storage[i].count = 0;
				station.storage[i].inc = 0;
			}
		}

		private static void LoadState(StationComponent station, in StationState state)
		{
			station.energy = state.energy;

			station.warperCount = Math.Min(state.warperCount, station.warperMaxCount);
			RefundItem(1210, state.warperCount - station.warperMaxCount, 0, station.entityId);

			station.idleDroneCount = Math.Min(state.droneCount, station.workDroneDatas.Length);
			RefundItem(5001, state.droneCount - station.workDroneDatas.Length, 0, station.entityId);

			station.idleShipCount = Math.Min(state.shipCount, station.workShipDatas.Length);
			RefundItem(5002, state.shipCount - station.workShipDatas.Length, 0, station.entityId);

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
