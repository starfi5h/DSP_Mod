using HarmonyLib;
using System;

namespace BuildToolOpt
{
    class ReplaceStationLogic
    {
		public static bool IsReplacing { get; private set; }
		public static bool ReplacingGhost { get; private set; }
		static int stationId;
		static int stationPrebuildId;

		[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
		public static void BuildTool_Click_CheckBuildConditions_Postfix(BuildTool_Click __instance, ref bool __result)
		{
			stationId = 0;
			stationPrebuildId = 0;
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

					// 白模也支持替换：悬停在站点预制备（蓝图虚影/待补塔虚影）上时同样触发
					int prebuildId = GetOverlapStationPrebuildId(__instance);
					if (prebuildId > 0)
					{
						ref var prebuildData = ref __instance.factory.prebuildPool[prebuildId];
						stationPrebuildId = prebuildId;
						buildPreview.lpos = prebuildData.pos;
						buildPreview.lrot = prebuildData.rot;

						buildPreview.condition = EBuildCondition.Ok;
						if (!VFInput.onGUI) UICursor.SetCursor(ECursor.Default);
						__result = true;
						var realCondition = GetRealStationCondition(__instance, buildPreview);
						__instance.actionBuild.model.cursorState = realCondition == EBuildCondition.Ok ? 0 : -1;
						__instance.actionBuild.model.cursorText = BuildPreview.GetConditionText(realCondition);
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
				if (stationPrebuildId > 0)
				{
					ReplaceStationPrebuild(__instance, stationPrebuildId);
					__result = false;
					return;
				}
			}
		}

		[HarmonyPrefix, HarmonyPatch(typeof(PlanetTransport), nameof(PlanetTransport.TakeBackItems_Station))]
		public static void TakeBackItems_Station(PlanetTransport __instance, int stationId, ref bool __runOriginal)
        {
			// Immediate rebuild: contents are transferred via SaveState/LoadState, suppress the refund.
			// Ghost rebuild: let the vanilla refund put drones/ships/warpers/storage into the package.
			__runOriginal = !(IsReplacing && !ReplacingGhost);
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
			//Plugin.Log.LogDebug("TakeBackItems_Station");
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

		// 物流塔替换对白模也生效：悬停在站点预制备上时返回其 id
		private static int GetOverlapStationPrebuildId(BuildTool_Click tool)
		{
			PlanetPhysics physics = tool.player.planetData.physics;
			int i = 0;
			while (BuildTool._tmp_cols[i] != null)
			{
				if (physics.GetColliderData(BuildTool._tmp_cols[i], out ColliderData colliderData))
				{
					if (colliderData.objType == EObjectType.Prebuild)
					{
						int prebuildId = colliderData.objId;
						PrebuildData prebuildData = tool.factory.prebuildPool[prebuildId];
						if (prebuildData.id == prebuildId)
						{
							ItemProto proto = LDB.items.Select(prebuildData.protoId);
							if (proto != null && proto.prefabDesc.isStation
								&& !proto.prefabDesc.isVeinCollector && !proto.prefabDesc.isCollectStation)
								return prebuildId;
						}
					}
				}
				i++;
			}
			return 0;
		}

		// 白模替换：移除旧站点预制备，放置手中塔（有塔立即建成并沿用旧白模设置；无塔则放置新的白模）。
		// 旧白模未支付过物品（itemRequired = 1 未补），拆除不产生退款，因此本流程天然收支平衡。
		public static void ReplaceStationPrebuild(BuildTool_Click tool, int prebuildId)
		{
			IsReplacing = true; // flag for other mods compat
			ReplacingGhost = true; // 白模源替换没有旧站内容物需要转移

			Player player = tool.player;
			int replaceItemId = tool.buildPreviews[0].item.ID;
			int realBefore = RealItemCount(player, replaceItemId);

			// 使用 BuildingParameters 從舊 Prebuild 複製完整物流站設定。
			// 傳入負數代表來源是 Prebuild。
			BuildingParameters stationParameters = default;

			bool copied = stationParameters.CopyFromFactoryObject(
				-prebuildId,
				tool.factory,
				copyInserters: false,
				copyAllSettings: true);

			if (!copied || stationParameters.type != BuildingType.Station)
			{
				ReplacingGhost = false;
				IsReplacing = false;
				return;
			}

			// 建立獨立的 2048 長度陣列，避免拆除舊 Prebuild
			// 時影響原本的參數資料。
			int[] parameters = new int[BuildingParameters.kStationParamsLength];

			if (stationParameters.parameters != null)
			{
				Array.Copy(
					stationParameters.parameters,
					parameters,
					Math.Min(
						stationParameters.parameters.Length,
						parameters.Length));
			}

			int paramCount = parameters.Length;


			// 取得舊、新物流站的 PrefabDesc
			PlanetFactory factory = tool.factory;
			ref PrebuildData oldPrebuild = ref factory.prebuildPool[prebuildId];
			ItemProto oldProto = LDB.items.Select(oldPrebuild.protoId);
			PrefabDesc newDesc = tool.buildPreviews[0].desc;

			// 取得舊、新物流站接口數量
			int oldSlotCount = oldProto?.prefabDesc?.portPoses?.Length ?? 12;
			int newSlotCount = newDesc?.portPoses?.Length ?? 12;

			// 拆除舊 Prebuild 之前，先保存所有外部接口連線
			StationObjectConn[] connections =
				CapturePrebuildConnections(
					factory,
					prebuildId,
					oldSlotCount);


			// 移除旧白模（未支付的预制备 ObjectAssetValue = 0，无退款）
			tool.actionBuild.DoDismantleObject(-prebuildId);

			// 放置新塔（沿用旧白模参数；有真实塔则消耗并立即建成，无塔则放置新的白模）
			tool.buildPreviews[0].parameters = parameters;
			tool.buildPreviews[0].paramCount = paramCount;
			tool.CreatePrebuilds();
			int objId = tool.buildPreviews[0].objId;
			if (objId == 0)
			{
				// 无塔且未开启 EnableHologram：无法放置白模，等同手动拆除了旧白模
				ReplacingGhost = false;
				IsReplacing = false;
				return;
			}

			// 新 Prebuild 建立成功後，立即恢復外部 Belt 連線。
			// objId 保留 CreatePrebuilds() 回傳的原始正負號。
			ReconnectPrebuildConnections(
				factory,
				objId,
				connections,
				newSlotCount);

			if (realBefore >= 1)
			{
				// Paid with a real tower -> build immediately (the ghost carried no runtime state).
				tool.factory.BuildFinally(tool.player, -objId);

				// 消费对账：支付被虚拟库存类补丁假扣时收回差额，防止白赚
				if (RealItemCount(player, replaceItemId) > realBefore)
				{
					int takeId = replaceItemId;
					int takeCount = 1;
					player.package.TakeTailItems(ref takeId, ref takeCount, out _);
				}
			}
			// 无塔：保持白模状态（itemRequired = 1），等待玩家补足物品后由建造系统完成

			ReplacingGhost = false;
			IsReplacing = false;
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
				if (id == stationPrebuildId) continue; // Skip the prebuild being replaced
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

			Player player = tool.player;
			int replaceItemId = tool.buildPreviews[0].item.ID;
			int realBefore = RealItemCount(player, replaceItemId);

			// No real tower owned -> the replacement becomes a hologram prebuild
			// (the "missing item" state). The dismantle refund is taken back so the
			// hologram's own item requirement (itemRequired = 1) charges the player
			// exactly one tower on completion; the replace can never net a free tower.
			bool ghost = realBefore < 1;
			ReplacingGhost = ghost;

			// Save status of old station
			StationComponent oldStation = tool.factory.transport.stationPool[stationId];
			BuildingParameters parameters = default;
			parameters.CopyFromFactoryObject(oldStation.entityId, tool.factory, copyInserters: false, copyAllSettings: true);
			SlotData[] slots = oldStation.slots;
			SaveState(oldStation, out StationState state);
			tool.actionBuild.DoDismantleObject(oldStation.entityId);

			// Create prebuild and apply config
			tool.buildPreviews[0].parameters = parameters.parameters;
			tool.buildPreviews[0].paramCount = parameters.parameters.Length;
			tool.CreatePrebuilds();
			int objId = tool.buildPreviews[0].objId;
			if (objId == 0)
			{
				// Ghost mode requires the hologram feature (EnableHologram). Without it the
				// vanilla path refuses to create the prebuild; the old station is already
				// dismantled with a full refund (same as a manual dismantle), so just stop.
				ReplacingGhost = false;
				IsReplacing = false;
				return;
			}
			ReconnectBelts(tool.factory, objId, slots);

			if (!ghost)
			{
				// Paid with a real tower -> build immediately and transfer the old state.
				stationId = tool.factory.transport.stationCursor;
				if (tool.factory.transport.stationRecycleCursor > 0)
					stationId = tool.factory.transport.stationRecycle[tool.factory.transport.stationRecycleCursor - 1];
				tool.factory.BuildFinally(tool.player, -objId);
				StationComponent newStation = tool.factory.transport.stationPool[stationId];
				LoadState(newStation, state);

				// Consumption reconciliation: if the item payment was faked away by other
				// mods (virtual stock patching the takes to no-ops), the dismantle refund
				// would net a free tower -> take one real tower back.
				if (RealItemCount(player, replaceItemId) > realBefore)
				{
					int takeId = replaceItemId;
					int takeCount = 1;
					player.package.TakeTailItems(ref takeId, ref takeCount, out _);
				}
			}

			ReplacingGhost = false;
			IsReplacing = false;
		}

		// Read the real item count straight from storage grids and hand fields.
		// Virtual-stock / architect-mode patches hook methods like GetItemCount,
		// but they cannot change what the grids actually contain.
		private static int RealItemCount(Player player, int itemId)
		{
			if (player == null || itemId <= 0) return 0;
			int count = 0;
			StorageComponent package = player.package;
			if (package != null)
			{
				StorageComponent.GRID[] grids = package.grids;
				int length = package.size < grids.Length ? package.size : grids.Length;
				for (int i = 0; i < length; i++)
				{
					if (grids[i].itemId == itemId)
						count += grids[i].count;
				}
			}
			if (player.inhandItemId == itemId)
				count += player.inhandItemCount;
			return count;
		}

		private struct StationObjectConn
		{
			public int slot;
			public bool isOutput;
			public int otherObjId;
			public int otherSlot;
		}

		private static StationObjectConn[] CapturePrebuildConnections(
			PlanetFactory factory,
			int prebuildId,
			int slotCount)
		{
			var connections = new StationObjectConn[slotCount];
			int objId = -prebuildId;

			for (int i = 0; i < slotCount; i++)
			{

				factory.ReadObjectConn( objId, i,
					out bool isOutput,
					out int otherObjId,
					out int otherSlot);

				if (otherObjId != 0)
				{
					connections[i] = new StationObjectConn
					{
						slot = i,
						isOutput = isOutput,
						otherObjId = otherObjId,
						otherSlot = otherSlot
					};
				}
			}
			return connections;
		}

		private static void ReconnectPrebuildConnections(
			PlanetFactory factory,
			int newObjId,
			StationObjectConn[] connections,
			int newSlotCount)
		{
			if (connections == null) return;

			int count = Math.Min(connections.Length, newSlotCount);

			for (int i = 0; i < count; i++)
			{
				var conn = connections[i];

				if (conn.otherObjId == 0) continue;

				factory.WriteObjectConn(
					newObjId,
					conn.slot,
					conn.isOutput,
					conn.otherObjId,
					conn.otherSlot);
			}
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
