using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AlterTickrate.Patches
{
	public class UIProgress_Patch
	{		
		static int factoryIndex;
		static float progress0, progress1;
		static float extraProgress0, extraProgress1;
		static int lastTick = 0, lastExtraTick = 0; // Update progress end points when a cycle has passed

		public static float GetFillAmount(float progress)
		{
			if (Parameters.FacilityUpdatePeriod == 1) return progress;
			int t = (factoryIndex + (int)GameMain.gameTick) % Parameters.FacilityUpdatePeriod;
			if (t < lastTick)
			{
				progress0 = progress1;
				progress1 = progress;
				if (progress1 < progress0) // If progress1 reach end and product not stacking, start with 0
					progress0 -= 1f;
			}
			lastTick = t;
			return Mathf.Clamp01(Mathf.Lerp(progress0, progress1, (float)t / Parameters.FacilityUpdatePeriod));
		}

		public static float GetExtraFillAmount(float progress)
        {
			if (Parameters.FacilityUpdatePeriod == 1) return progress;
			int t = (factoryIndex + (int)GameMain.gameTick) % Parameters.FacilityUpdatePeriod;
			if (t < lastExtraTick)
			{
				extraProgress0 = extraProgress1;
				extraProgress1 = progress;
				if (extraProgress1 < extraProgress0)
					extraProgress0 -= 1f;
			}
			lastExtraTick = t;
			//Log.Debug($"{t} {extraProgress0:F3} {progress:F3}");
			return Mathf.Clamp01(Mathf.Lerp(extraProgress0, extraProgress1, (float)t / Parameters.FacilityUpdatePeriod));
		}

		public static float GetCoolDown(float progress)
        {
			if (Parameters.FacilityUpdatePeriod == 1) return progress;
			int t = (factoryIndex + (int)GameMain.gameTick) % Parameters.FacilityUpdatePeriod;
			if (t < lastTick)
			{
				progress0 = progress1;  // the ejector/silo progress will go back and forward
				progress1 = Mathf.Clamp01(progress);
			}
			lastTick = t;
			return Mathf.Lerp(progress0, progress1, (float)t / Parameters.FacilityUpdatePeriod);
		}

		public static float GetFuelAmount(float progress)
		{
			if (Parameters.PowerUpdatePeriod == 1) return progress;
			int t = (factoryIndex + (int)GameMain.gameTick) % Parameters.PowerUpdatePeriod;
			if (t < lastTick)
			{
				progress0 = progress1;
				progress1 = progress;
				if (progress1 > progress0)
					progress0 = progress1;
				//Log.Debug($"{t} {progress0:F3} {progress1:F3}");
			}
			lastTick = t;
			return Mathf.Clamp01(Mathf.Lerp(progress0, progress1, (float)t / Parameters.PowerUpdatePeriod));
		}

		public static float GetExtraFuelAmount(float progress)
		{
			if (Parameters.PowerUpdatePeriod == 1) return progress;
			int t = (factoryIndex + (int)GameMain.gameTick) % Parameters.PowerUpdatePeriod;
			if (t < lastExtraTick)
			{
				extraProgress0 = extraProgress1;
				extraProgress1 = progress;
				if (extraProgress1 > extraProgress0)
					extraProgress0 = extraProgress1;
			}
			lastExtraTick = t;
			//Log.Debug($"{t} {extraProgress0:F3} {progress:F3}");
			return Mathf.Clamp01(Mathf.Lerp(extraProgress0, extraProgress1, (float)t / Parameters.PowerUpdatePeriod));
		}

		// Note: UIMinerWindow progress is too fast to look normal

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow._OnUpdate))]
		[HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow._OnUpdate))]
		[HarmonyPatch(typeof(UIEjectorWindow), nameof(UIEjectorWindow._OnUpdate))]
		[HarmonyPatch(typeof(UISiloWindow), nameof(UISiloWindow._OnUpdate))]
		static IEnumerable<CodeInstruction> OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				if (codeMatcher.InstructionAt(2).opcode == OpCodes.Call)
                {
					string name = ((MethodInfo)codeMatcher.InstructionAt(2).operand).Name;
					if (name == "get_ejectorId" || name == "get_siloId")
                    {
						codeMatcher
							.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Clamp01"))
							.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetCoolDown)))
							.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Clamp01"))
							.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetCoolDown)));

						return codeMatcher.InstructionEnumeration();
					}
                }

				codeMatcher
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Clamp01"))
					.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetFillAmount)))
					.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Clamp01"));
				if (codeMatcher.IsValid)
					codeMatcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetExtraFillAmount)));

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler OnUpdate_Transpiler failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(UIPowerGeneratorWindow), nameof(UIPowerGeneratorWindow._OnUpdate))]
		static IEnumerable<CodeInstruction> PowerOnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				codeMatcher
					.End()
					.MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "set_fillAmount"))
					.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetExtraFuelAmount))))
					.MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "set_fillAmount"))
					.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIProgress_Patch), nameof(UIProgress_Patch.GetFuelAmount))));

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerOnUpdate_Transpiler failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow.OnServingBoxChange))]
		static void OnServingBoxChange(UIAssemblerWindow __instance)
		{
			if (__instance.assemblerId == 0 || __instance.factory == null)
				return;

			factoryIndex = __instance.factory.index;
			ref var assemblerComponent = ref __instance.factorySystem.assemblerPool[__instance.assemblerId];
			progress0 = progress1 = Mathf.Clamp01((float)assemblerComponent.time / assemblerComponent.timeSpend);
			extraProgress0 = extraProgress1 = Mathf.Clamp01((float)assemblerComponent.extraTime / assemblerComponent.extraTimeSpend);
			lastTick = lastExtraTick = 0;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UILabWindow), nameof(UILabWindow.OnLabIdChange))]
		static void OnLabIdChange(UILabWindow __instance)
		{
			if (__instance.labId == 0 || __instance.factory == null)
				return;

			factoryIndex = __instance.factory.index;
			ref var labComponent = ref __instance.factorySystem.labPool[__instance.labId];
			progress0 = progress1 = Mathf.Clamp01((float)labComponent.time / labComponent.timeSpend);
			extraProgress0 = extraProgress1 = Mathf.Clamp01((float)labComponent.extraTime / labComponent.extraTimeSpend);
			lastTick = lastExtraTick = 0;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIEjectorWindow), nameof(UIEjectorWindow.OnServingBoxChange))]
		static void OnServingBoxChange(UIEjectorWindow __instance)
		{
			if (__instance.ejectorId == 0 || __instance.factory == null)
				return;

			factoryIndex = __instance.factory.index;
			ref var component = ref __instance.factorySystem.ejectorPool[__instance.ejectorId];
			progress0 = progress1 = (component.direction > 0) ? Mathf.Clamp01((float)component.time / component.chargeSpend) : Mathf.Clamp01((float)component.time / component.coldSpend);
			lastTick = lastExtraTick = 0;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UISiloWindow), nameof(UISiloWindow.OnServingBoxChange))]
		static void OnServingBoxChange(UISiloWindow __instance)
		{
			if (__instance.siloId == 0 || __instance.factory == null)
				return;

			factoryIndex = __instance.factory.index;
			ref var component = ref __instance.factorySystem.siloPool[__instance.siloId];
			progress0 = progress1 = (component.direction > 0) ? Mathf.Clamp01((float)component.time / component.chargeSpend) : Mathf.Clamp01((float)component.time / component.coldSpend);
			lastTick = lastExtraTick = 0;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIPowerGeneratorWindow), nameof(UIPowerGeneratorWindow.OnGeneratorIdChange))]
		static void OnGeneratorIdChange(UIPowerGeneratorWindow __instance)
		{
			if (__instance.generatorId == 0 || __instance.factory == null)
				return;

			factoryIndex = __instance.factory.index;
			ref var component = ref __instance.powerSystem.genPool[__instance.generatorId];
			ItemProto itemProto = LDB.items.Select(component.curFuelId);
			if (itemProto != null)
			{
				progress0 = progress1 = component.fuelEnergy / itemProto.HeatValue;
				extraProgress0 = extraProgress1 = 0.0f;
				lastTick = lastExtraTick = 0;
			}
		}
	}
}
