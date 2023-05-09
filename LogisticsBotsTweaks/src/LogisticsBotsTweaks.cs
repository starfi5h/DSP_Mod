using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LogisticsBotsTweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.LogisticsBotsTweaks";
        public const string NAME = "LogisticsBotsTweaks";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
			Patch_LogisticsBot.logisticsBotSpeedScale = Config.Bind("Bot", "SpeedScale", 1f,
				"Scale of logistics bots flight speed. 物流配送机的飞行速度倍率").Value;

			Patch_Distributor.dispenserMaxCourierCount = Config.Bind("Distributor", "MaxBotCount", 10,
				"Max logistics bots count. 物流配送器的最大运输机数量").Value;

			Patch_Distributor.maxChargePowerScale = Config.Bind("Distributor", "MaxChargePowerScale", 1.0f,
				"Scale of max charge power. 物流配送器的最大充电功率倍率").Value;

            Log = Logger;
            harmony = new(GUID);

			if (Patch_LogisticsBot.logisticsBotSpeedScale != 1f)
				harmony.PatchAll(typeof(Patch_LogisticsBot));
			if (Patch_Distributor.dispenserMaxCourierCount != 10 || Patch_Distributor.maxChargePowerScale != 1f)
				harmony.PatchAll(typeof(Patch_Distributor));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }        
    }

	class Patch_LogisticsBot
	{
		public static float logisticsBotSpeedScale = 1.0f;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.Import))]
		[HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.SetForNewGame))]
		public static void LogisticCourierSpeedModified(GameHistoryData __instance)
		{
			__instance.logisticCourierSpeed = Configs.freeMode.logisticCourierSpeed * logisticsBotSpeedScale;
		}
	}

	class Patch_Distributor
	{
		public static int dispenserMaxCourierCount = 10;
		public static float maxChargePowerScale = 1.0f;

		[HarmonyPostfix, HarmonyPatch(typeof(DispenserComponent), nameof(DispenserComponent.Init))]
		public static void Init(DispenserComponent __instance, PrefabDesc _desc)
		{
			if (dispenserMaxCourierCount > _desc.dispenserMaxCourierCount) // Only expand
			{
				__instance.workCourierDatas = new CourierData[dispenserMaxCourierCount];
				__instance.orders = new DeliveryLogisticOrder[dispenserMaxCourierCount];
				__instance.holdupPackage = new DispenserStore[dispenserMaxCourierCount * 2];
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(DispenserComponent), nameof(DispenserComponent.Import))]
		public static void Import(DispenserComponent __instance)
		{
			if (dispenserMaxCourierCount > __instance.workCourierDatas.Length) // Only expand
			{
				var workCourierDatas = __instance.workCourierDatas;
				__instance.workCourierDatas = new CourierData[dispenserMaxCourierCount];
				Array.Copy(workCourierDatas, __instance.workCourierDatas, workCourierDatas.Length);

				var orders = __instance.orders;
				__instance.orders = new DeliveryLogisticOrder[dispenserMaxCourierCount];
				Array.Copy(orders, __instance.orders, orders.Length);

				var holdupPackage = __instance.holdupPackage;
				__instance.holdupPackage = new DispenserStore[dispenserMaxCourierCount * 2];
				Array.Copy(holdupPackage, __instance.holdupPackage, holdupPackage.Length);
			}
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(UIDispenserWindow), nameof(UIDispenserWindow.OnCourierIconClick))]
		public static IEnumerable<CodeInstruction> OnCourierIconClick_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// Change: int num2 = (itemProto2 != null) ? itemProto2.prefabDesc.dispenserMaxCourierCount : 10;
				// To:     int num2 = (itemProto2 != null) ? dispenserMaxCourierCount : 10;
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "dispenserMaxCourierCount"))
					.Advance(1)
					.Insert(
						new CodeInstruction(OpCodes.Pop),
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patch_Distributor), nameof(dispenserMaxCourierCount)))
					);

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogError(e);
				return instructions;
			}
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(UIDispenserWindow), nameof(UIDispenserWindow.OnDispenserIdChange))]
		public static IEnumerable<CodeInstruction> OnDispenserIdChange_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// this.maxChargePowerSlider.maxValue = (float)(num / 5000L) * maxChargePowerScale;
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "set_maxValue"))
					.Insert(
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patch_Distributor), nameof(maxChargePowerScale))),
						new CodeInstruction(OpCodes.Mul)
					);

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogError(e);
				return instructions;
			}
		}
	}
}
