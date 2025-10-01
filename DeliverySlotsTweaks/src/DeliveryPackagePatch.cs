using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DeliverySlotsTweaks
{
    public class DeliveryPackagePatch
    {
		public static bool architectMode = false;
		public static int maxDeliveryGridIndex = 0; // Assign in Plugin.ParameterOverwrite()

		static readonly Dictionary<int, int> packageItemCount = new();
		static readonly Dictionary<int, int> deliveryItemCount = new ();
		static readonly Dictionary<int, int> deliveryGridindex = new ();

		public static void Count(StorageComponent package)
		{
			packageItemCount.Clear();
			StorageComponent.GRID[] grids = package.grids;
			int length = package.size < grids.Length ? package.size : grids.Length;
			for (int i = 0; i < length; i++)
			{
				int itemId = grids[i].itemId;
				if (itemId > 0)
				{
					if (packageItemCount.ContainsKey(itemId))
						packageItemCount[itemId] += grids[i].count;
					else
						packageItemCount.Add(itemId, grids[i].count);
				}
			}
		}

		public static void Count(DeliveryPackage package)
		{
			deliveryItemCount.Clear();
			deliveryGridindex.Clear();
			DeliveryPackage.GRID[] grids = package.grids;
			for (int i = 0; i <= maxDeliveryGridIndex; i++)
			{
				int itemId = grids[i].itemId;
				if (itemId > 0) // No duplicate items in delivery slots
				{
					deliveryItemCount.Add(itemId, grids[i].count);
					deliveryGridindex.Add(itemId, i);
				}
			}
		}

		// Replace StorageComponent.GetItemCount => packageUtility.GetItemCountFromAllPackages in the future?
		public static int GetItemCount(StorageComponent _, int itemId)
		{
			if (architectMode) return 999;

			packageItemCount.TryGetValue(itemId, out int itemCount1);
			deliveryItemCount.TryGetValue(itemId, out int itemCount2);
			return itemCount1 + itemCount2;
		}

        // To ingore buildbar limit
#pragma warning disable IDE0060
        public static int GetItemCountDummy(StorageComponent _, int itemId)
#pragma warning restore IDE0060
        {
			return 999;
		}

		// Replace StorageComponent.TakeItem
		public static int TakeItem(StorageComponent storage, int itemId, int count, out int inc)
		{
			if (architectMode)
			{
				inc = 0;
				return count;
			}

			if (deliveryGridindex.TryGetValue(itemId, out int gridindex))
			{
				GameMain.mainPlayer.packageUtility.TakeItemFromAllPackages(gridindex, ref itemId, ref count, out inc, false);
				return count;
			}
			else
				return storage.TakeItem(itemId, count, out inc);
		}

		// Replace StorageComponent.TakeTailItems
		public static void TakeTailItems(StorageComponent storage, ref int itemId, ref int count, out int inc, bool _)
		{
			if (architectMode)
			{
				inc = 0;
				return;
			}
			if (Compatibility.Nebula_Patch.IsActive && Compatibility.Nebula_Patch.IsOthers())
            {
				inc = 0;
				return;
            }

			if (deliveryGridindex.TryGetValue(itemId, out int gridindex))
			{
				GameMain.mainPlayer.packageUtility.TakeItemFromAllPackages(gridindex, ref itemId, ref count, out inc, false);
				if (packageItemCount.ContainsKey(itemId))
				{
					int num = packageItemCount[itemId] - count;
					packageItemCount[itemId] = num;
					if (num <= 0) packageItemCount.Remove(itemId);
				}
				else if (deliveryItemCount.ContainsKey(itemId))
                {
					int num = deliveryItemCount[itemId] - count;
					deliveryItemCount[itemId] = num;
					if (num <= 0) deliveryItemCount.Remove(itemId);
				}
				
			}
			else
			{
				storage.TakeTailItems(ref itemId, ref count, out inc, false);
				if (packageItemCount.ContainsKey(itemId))
                {
					int num = packageItemCount[itemId] - count;
					packageItemCount[itemId] = num;
					if (num <= 0) packageItemCount.Remove(itemId);
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Mecha), nameof(Mecha.GameTick))]
		public static void Prepare(Mecha __instance, long time)
		{
			if (__instance?.player != null && __instance.player == GameMain.mainPlayer)
			{
				Count(__instance.player.package);
				Count(__instance.player.deliveryPackage);

				if (time % 30 == 0 && Plugin.AutoRefillWarper.Value)
                {
					TryReillWarper(__instance);
				}
			}
		}

		static void TryReillWarper(Mecha mecha)
        {
			if (mecha.HasWarper()) return;

			var itemId = 1210;
			var itemCount = 1;
			if (GetItemCount(null, itemId) <= 0) return;
			TakeTailItems(mecha.player.package, ref itemId, ref itemCount, out var itemInc, false);
			mecha.warpStorage.AddItem(itemId, itemCount, itemInc, out _);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.Sort))]
		public static void Sort_Prefix(StorageComponent __instance)
		{
			if (!Plugin.SortToDelieverySlots.Value) return;
			if (__instance != GameMain.mainPlayer?.package) return;

			// Try to move item into delivery slots before sorting
			var packageGrids = __instance.grids;
			var deliveryPackage = GameMain.mainPlayer.deliveryPackage;
			int length = __instance.size < __instance.grids.Length ? __instance.size : __instance.grids.Length;
			for (int i = length - 1; i > 0; i--)
			{
				ref var grid = ref packageGrids[i];
				int itemId = grid.itemId;
				if (itemId > 0 && deliveryGridindex.ContainsKey(itemId))
				{
					int sentItemCount = deliveryPackage.AddItem(deliveryGridindex[itemId], itemId, grid.count, grid.inc, out int remainInc);
					grid.count -= sentItemCount;
					grid.inc = remainInc;
				}
			}
		}

		#region MechaDroneLogic

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.PlaceItems))]
		public static IEnumerable<CodeInstruction> PlaceItems_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions);

				/*
				if (this.entityId == 0)
				{
					StorageComponent package = player.package;
					for (int i = 0; i < package.size; i++) {
						...
						num += count;
					}
					AddConstructableCountsInStorage(this, player, ref num); // Insert method here
				}
				else if (this.entityId > 0)
				{
					...
				*/

				codeMacher.MatchForward(false,
						new CodeMatch(OpCodes.Br),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.entityId))),
						new CodeMatch(OpCodes.Ldc_I4_0),
						new CodeMatch(OpCodes.Ble)
					)
					.Insert(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Ldloca_S, (byte)0), // num += count; in the loop
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(AddConstructableCountsInStorage)))
					);

				// Replace player.package.TakeTailItems

				codeMacher
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldarg_2),
						new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(Player), nameof(Player.package))),
						new CodeMatch(OpCodes.Ldloca_S),
						new CodeMatch(OpCodes.Ldloca_S),
						new CodeMatch(OpCodes.Ldloca_S),
						new CodeMatch(OpCodes.Ldc_I4_0),
						new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeTailItems")
					)
					.Repeat(
						matcher => matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeTailItems)))
					);

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler PlaceItems error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		static void AddConstructableCountsInStorage(ConstructionModuleComponent constructionModule, Player player, ref int num)
        {
			if (player?.deliveryPackage == null) return;

			// Add item in deliveryPackage to constructableCountsInStorage too
			var array = constructionModule.constructableCountsInStorage;
			var grids = player.deliveryPackage.grids;
			for (int i = 0; i <= maxDeliveryGridIndex; i++)
			{
				int itemId = grids[i].itemId;
				if (itemId > 0 && ItemProto.constructableIdHash.Contains(itemId))
				{
					int count = grids[i].count;					
					int index = ItemProto.constructableIndiceById[itemId];
					array[index].haveCount += count;
					num += count;
				}
			}
		}

		#endregion

		#region MechaForge

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryAddTask))]
		[HarmonyPatch(typeof(MechaForge), nameof(MechaForge.TryTaskWithTestPackage))]
		public static IEnumerable<CodeInstruction> TryAddTask_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "TryAddTaskIterate"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TryAddTaskIterate)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler TryAddTask error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		public static bool TryAddTaskIterate(MechaForge mechaForge, RecipeProto recipe, int count, out bool useBottleneckItem, bool predictBottleneckItems)
        {
			// mechaForge._test_pack : expected products in replicator queue
			Count(mechaForge.player.deliveryPackage);
			foreach (var pair in deliveryItemCount)
				mechaForge._test_pack.Alter(pair.Key, pair.Value); // Add item in delivery slot

			return mechaForge.TryAddTaskIterate(recipe, count, out useBottleneckItem, predictBottleneckItems);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MechaForge), nameof(MechaForge.AddTaskIterate))]
		[HarmonyPatch(typeof(Mecha), nameof(Mecha.AutoReplenishAmmo))]
		[HarmonyPatch(typeof(Mecha), nameof(Mecha.AutoReplenishBomb))]
		public static IEnumerable<CodeInstruction> TakeItem_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeItem"))
					.Repeat(matcher => matcher
						.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeItem))));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler StorageComponent.TakeItem error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		#endregion

		#region BuildTool

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.RemoveBasePit))]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityAutoReplenishIfNeeded))]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
		[HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
		[HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.OnPlayerPackageChange))]
		[HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.SetComponentItem))]
		[HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
		[HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu.OnChildButtonClick))]
		[HarmonyPatch(typeof(UIHandTip), nameof(UIHandTip._OnUpdate))]
		[HarmonyPatch(typeof(UIRemoveBasePitButton), nameof(UIRemoveBasePitButton._OnUpdate))]
		[HarmonyPatch(typeof(UITurretWindow), nameof(UITurretWindow.OnHandFillAmmoButtonClick))]
		public static IEnumerable<CodeInstruction> UIBuildMenu_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetItemCount"))
					.Repeat(matcher => matcher
						.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(GetItemCount))));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler UIBuildMenu._OnUpdate error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyAfter("dsp.nebula-multiplayer")]
		[HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))] // Note: target player.package.TakeTailItems, not tmpPackage.TakeTailItems
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.RemoveBasePit))]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.EntityAutoReplenishIfNeeded))]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.StationAutoReplenishIfNeeded))]
		[HarmonyPatch(typeof(Player), nameof(Player.TakeItemFromPlayer))] // Call by EntityFastFillIn
		[HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.DoUpgradeObject))]
		[HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
		[HarmonyPatch(typeof(UITurretWindow), nameof(UITurretWindow.OnHandFillAmmoButtonClick))]
		public static IEnumerable<CodeInstruction> CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
		{
			try
			{
				int count = 1; // Match the count to target positions of TakeTailItems for each functions
				if (__originalMethod.DeclaringType.Name == "BuildTool_BlueprintPaste" && __originalMethod.Name == "CreatePrebuilds") count = 2;
				else if (__originalMethod.Name == "StationAutoReplenishIfNeeded") count = 2;
				else if (__originalMethod.Name == "EntityAutoReplenishIfNeeded") count = 3;

				var codeMacher = new CodeMatcher(instructions).End();
				for (int i = 0; i < count; i++)
				{
					codeMacher
						.MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeTailItems"))
						.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeTailItems)));
				}

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler CreatePrebuilds error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CalculateReformData))]
		[HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.DetermineReforms))]
		public static IEnumerable<CodeInstruction> TmpPackage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
		{
			try
			{
				// 因為tmpPackage被修改後比player.package更長, 任何使用Array.Copy的函式都要進行修改

				// Because tmpPackage size is changed to the size larger than player package by this mod

				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Copy"));

				// Change: Array.Copy(base.player.package.grids, this.tmpPackage.grids, this.tmpPackage.size);
				// To:     Array.Copy(base.player.package.grids, this.tmpPackage.grids, base.player.package.size);

				var operand = codeMacher.InstructionAt(-1).operand;
				if (operand != null && ((FieldInfo)operand).Name == "size")
				{
					codeMacher.Advance(-3)
					.RemoveInstructions(3);
				}
				else if (operand == null)
                {
					codeMacher.Advance(-5)
					.RemoveInstructions(5);
				}
				else
                {
					Plugin.Log.LogWarning("Transpiler TmpPackage_Transpiler unexpected operand!");
					return instructions;
				}

				codeMacher.Insert(
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(BuildTool), nameof(BuildTool.player))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Player), nameof(Player.package))),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.size)))
				);

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler TmpPackage_Transpiler error: " + __originalMethod.Name);
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.GameTick))]
		public static IEnumerable<CodeInstruction> PlayerAction_Build_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "_GameTick"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(BuildTool_GameTick)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler PlayerAction_Build.GameTick error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		public static void BuildTool_GameTick(BuildTool @this, long time)
		{
			// BuildTool._GameTick cannot be patched by prefix, so use transpiler to replace completely
			if (@this.active)
			{				
				@this.mouseRay = @this.mainCamera.ScreenPointToRay(Input.mousePosition);
				@this.SetFactoryReferences();

				// Include delivery items in tmpPackage
				int totalsize = @this.player.package.size + maxDeliveryGridIndex + 1;
				if (@this.tmpPackage == null)
				{
					@this.tmpPackage = new StorageComponent(totalsize);
				}
				if (@this.tmpPackage.size != totalsize)
				{
					@this.tmpPackage.SetSize(totalsize);
				}
				Array.Copy(@this.player.package.grids, @this.tmpPackage.grids, @this.player.package.size);
				var grids = @this.player.deliveryPackage.grids;
				for (int i = @this.player.package.size; i < totalsize; i++)
                {
					ref var ptr = ref grids[i - @this.player.package.size];
					@this.tmpPackage.grids[i].itemId = ptr.itemId;
					@this.tmpPackage.grids[i].count = ptr.count;
					//Plugin.Log.LogDebug($"[{ptr.itemId}] {ptr.count}");
				}

				@this.tmpInhandId = @this.player.inhandItemId;
				@this.tmpInhandCount = @this.player.inhandItemCount;
				try
				{
					@this._OnTick(time);
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
				@this.frame++;
			}
		}

		#endregion
	}
}
