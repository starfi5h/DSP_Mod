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

		// Replace StorageComponent.GetItemCount
		public static int GetItemCount(StorageComponent _, int itemId)
		{
			packageItemCount.TryGetValue(itemId, out int itemCount1);
			deliveryItemCount.TryGetValue(itemId, out int itemCount2);
			return itemCount1 + itemCount2;
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

		#region MechaDroneLogic

		[HarmonyPostfix, HarmonyPatch(typeof(MechaDroneLogic), nameof(MechaDroneLogic.Prepare))]
		public static void Prepare(MechaDroneLogic __instance, bool __result)
		{
			if (__result) // this.factory != null
			{
				Count(__instance.player.package);
				Count(__instance.player.deliveryPackage);
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MechaDroneLogic), nameof(MechaDroneLogic.UpdateTargets))]
		[HarmonyPatch(typeof(MechaDroneLogic), nameof(MechaDroneLogic.FindNext))]
		public static IEnumerable<CodeInstruction> UpdateTargets_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetItemCount"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(GetItemCount)))
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeTailItems"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeTailItems)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler UpdateTargets error");
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
		public static IEnumerable<CodeInstruction> AddTaskIterate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeItem"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeItem)));

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler AddTaskIterate error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		#endregion

		#region BuildTool

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnUpdate))]
		[HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu.OnChildButtonClick))]
		[HarmonyPatch(typeof(UIHandTip), nameof(UIHandTip._OnUpdate))]
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))]
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
		[HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.DoUpgradeObject))]
		[HarmonyPatch(typeof(BuildTool_Reform), nameof(BuildTool_Reform.ReformAction))] // Note: target player.package.TakeTailItems, not tmpPackage.TakeTailItems
		public static IEnumerable<CodeInstruction> CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.End()
					.MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "TakeTailItems"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(DeliveryPackagePatch), nameof(TakeTailItems)));

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
