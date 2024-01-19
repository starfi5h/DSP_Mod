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
			if (deliveryGridindex.TryGetValue(itemId, out int gridindex))
			{
				GameMain.mainPlayer.packageUtility.TakeItemFromAllPackages(gridindex, ref itemId, ref count, out inc);
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
				GameMain.mainPlayer.packageUtility.TakeItemFromAllPackages(gridindex, ref itemId, ref count, out inc);
				if (packageItemCount.ContainsKey(itemId))
				{
					int num = packageItemCount[itemId] - count;
					packageItemCount[itemId] = num;
					if (num == 0)
						packageItemCount.Remove(itemId);
				}
				else if (deliveryItemCount.ContainsKey(itemId))
                {
					int num = deliveryItemCount[itemId] - count;
					deliveryItemCount[itemId] = num;
					if (num == 0)
						deliveryItemCount.Remove(itemId);
				}
				
			}
			else
			{
				storage.TakeTailItems(ref itemId, ref count, out inc, false);
				if (packageItemCount.ContainsKey(itemId))
                {
					int num = packageItemCount[itemId] - count;
					packageItemCount[itemId] = num;
					if (num == 0)
						packageItemCount.Remove(itemId);
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Mecha), nameof(Mecha.GameTick))]
		public static void Prepare(Mecha __instance)
		{
			if (__instance?.player != null && __instance.player == GameMain.mainPlayer)
			{
				Count(__instance.player.package);
				Count(__instance.player.deliveryPackage);
			}
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
		[HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.SearchForNewTargets))]
		[HarmonyPatch(typeof(ConstructionSystem), nameof(ConstructionSystem.FindNextConstruct))]
		public static IEnumerable<CodeInstruction> FindNextConstruct_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// Patch only if (ownedByMecha) flag2 = ptr.itemRequired <= this.player.package.GetItemCount((int)ptr.protoId);
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetItemCount"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(GetItemCount)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler FindNextConstruct error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(ConstructionSystem), nameof(ConstructionSystem.TakeEnoughItemsFromPlayer))]
		public static IEnumerable<CodeInstruction> TakeEnoughItemsFromPlayer_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeTailItems"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeTailItems)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler TakeEnoughItemsFromPlayer error");
				Plugin.Log.LogWarning(e);
				return instructions;
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

		public static bool TryAddTaskIterate(MechaForge mechaForge, RecipeProto recipe, int count)
        {
			// mechaForge._test_pack : expected products in replicator queue
			Count(mechaForge.player.deliveryPackage);
			foreach (var pair in deliveryItemCount)
				mechaForge._test_pack.Alter(pair.Key, pair.Value); // Add item in delivery slot

			return mechaForge.TryAddTaskIterate(recipe, count);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MechaForge), nameof(MechaForge.AddTaskIterate))]
		[HarmonyPatch(typeof(Mecha), nameof(Mecha.AutoReplenishAmmo))]
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
		[HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu.OnChildButtonClick))]
		public static IEnumerable<CodeInstruction> OnBuildingButtonClick_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				if (!Plugin.EnableHologram.Value)
					return UIBuildMenu_Transpiler(instructions);

				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetItemCount"))
					.Repeat(matcher => matcher
						.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(GetItemCountDummy))));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler UIBuildMenu.OnChildButtonClick error");
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
				int count = 1;
				if (__originalMethod.Name == "StationAutoReplenishIfNeeded") count = 2;
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
