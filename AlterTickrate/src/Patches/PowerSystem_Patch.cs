using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AlterTickrate.Patches
{
    public class PowerSystem_Patch
    {
        public static float PowerSystem_SpeedRate = 5.0f;
        public static PlanetFactory AnimOnlyFactory = null;
		const float DELTA_TIME = 0.016666668f; // num6 in PowerSystem.GameTick

		[HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static bool GameTick(PowerSystem __instance)
        {
            if (__instance.factory != AnimOnlyFactory)
                return true;

			var entityPool = __instance.factory.entityPool;
			var entityAnimPool = __instance.factory.entityAnimPool;			
			var genPool = __instance.genPool;

			// Update animation for generator
			for (int i = 1; i < __instance.genCursor; i++)
            {
				if (genPool[i].id == i)
                {
					var entityId = genPool[i].entityId;
					ref var animData = ref entityAnimPool[entityId];
					if (genPool[i].wind)
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
					else if (genPool[i].gamma)
                    {
						animData.time += DELTA_TIME;
						if (animData.time > 1f)
							animData.time -= 1f;
					}
					else if (genPool[i].fuelMask > 1)
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
					else if (genPool[i].geothermal)
                    {
						animData.Step(animData.state, DELTA_TIME, 2f, 0f);
					}
					else
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
				}
            }

			// wireless charger for mecha & animation
			var mecha = GameMain.mainPlayer.mecha;
			for (int i = 1; i < __instance.nodeCursor; i++)
			{
				if (__instance.nodePool[i].id == i)
				{
					int entityId = __instance.nodePool[i].entityId;
					int networkId = __instance.nodePool[i].networkId;
					ref var animData = ref entityAnimPool[entityId];
					if (__instance.nodePool[i].isCharger)
					{
						int num62 = __instance.nodePool[i].requiredEnergy - __instance.nodePool[i].idleEnergyPerTick;
						float num63 = __instance.networkServes[networkId];
						if (__instance.nodePool[i].coverRadius < 15f)
						{
							animData.StepPoweredClamped(num63, DELTA_TIME, (num62 > 0) ? 2U : 1U);
						}
						else
						{
							animData.StepPoweredClamped2(num63, DELTA_TIME, (num62 > 0) ? 2U : 1U);
						}
						if (num62 > 0 && entityAnimPool[entityId].state == 2U)
						{
							num62 = (int)(num63 * num62);
							mecha.coreEnergy += num62;
							mecha.MarkEnergyChange(2, num62);
							mecha.AddChargerDevice(entityId);
							if (mecha.coreEnergy > mecha.coreEnergyCap)
							{
								mecha.coreEnergy = mecha.coreEnergyCap;
							}
						}
					}
					else if (entityPool[entityId].powerGenId == 0 && entityPool[entityId].powerAccId == 0 && entityPool[entityId].powerExcId == 0)
					{
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.4f);
					}
				}
			}

			return false;
        }


		[HarmonyPrefix]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.StateUpdate))]
		static bool StateUpdate(ref PowerExchangerComponent __instance)
		{
			// Opening / Closing speed of PowerExchangerComponent
			if (__instance.state < __instance.targetState)
			{
				__instance.state += 0.00557f * Facility_Patch.FacilitySpeedRate;
				if (__instance.state >= __instance.targetState)
				{
					__instance.state = __instance.targetState;
				}
			}
			else if (__instance.state > __instance.targetState)
			{
				__instance.state -= 0.00557f * Facility_Patch.FacilitySpeedRate;
				if (__instance.state <= __instance.targetState)
				{
					__instance.state = __instance.targetState;
				}
			}
			return false;
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.InputUpdate))]
		static bool InputUpdate(ref PowerExchangerComponent __instance, long remaining, AnimData[] animPool, int[] productRegister, int[] consumeRegister, ref long __result)
		{
			if (__instance.state != 1f)
			{
				__result = 0L;
				return false;
			}
			long num = remaining;
			num = ((num < __instance.energyPerTick) ? num : __instance.energyPerTick);
			if (num * ConfigSettings.FacilityUpdatePeriod >= __instance.maxPoolEnergy - __instance.currPoolEnergy) // Populate 
			{
				if (__instance.emptyCount > 0 && __instance.fullCount < 20) // Assume charge speed is less than 1/tick
				{
					if (num != remaining)
					{
						num = __instance.energyPerTick;
					}
					__instance.currPoolEnergy -= __instance.maxPoolEnergy;
					__instance.emptyCount -= 1;
					__instance.fullCount += 1;
					int[] obj = productRegister;
					lock (obj)
					{
						productRegister[__instance.fullId]++;
					}
					obj = consumeRegister;
					lock (obj)
					{
						consumeRegister[__instance.emptyId]++;
						goto IL_EF;
					}
				}
				num = __instance.maxPoolEnergy - __instance.currPoolEnergy;
			}
		IL_EF:
			__instance.currEnergyPerTick = num;
			__instance.currPoolEnergy += num * ConfigSettings.FacilityUpdatePeriod; // Populate
			__instance.currPoolEnergy = __instance.currPoolEnergy < __instance.maxPoolEnergy ? __instance.currPoolEnergy : __instance.maxPoolEnergy; // Clamp to limit
			animPool[__instance.entityId].state = (uint)Mathf.CeilToInt((float)num / (float)__instance.energyPerTick * 100f);
			animPool[__instance.entityId].power = (float)__instance.currPoolEnergy / (float)__instance.maxPoolEnergy;
			__result = num;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.OutputUpdate))]
		static bool OutputUpdate(ref PowerExchangerComponent __instance, long energyPay, AnimData[] animPool, int[] productRegister, int[] consumeRegister, ref long __result)
		{
			if (__instance.state != -1f)
			{
				__result = 0L;
				return false;
			}
			long num = energyPay;
			num = ((num < __instance.energyPerTick) ? num : __instance.energyPerTick);
			if (num * ConfigSettings.FacilityUpdatePeriod >= __instance.currPoolEnergy)  // Populate 
			{
				if (__instance.fullCount > 0 && __instance.emptyCount < 20)  // Assume discharge speed is less than 1/tick
				{
					if (num != energyPay)
					{
						num = __instance.energyPerTick;
					}
					__instance.currPoolEnergy += __instance.maxPoolEnergy;
					__instance.fullCount -= 1;
					__instance.emptyCount += 1;
					int[] obj = productRegister;
					lock (obj)
					{
						productRegister[__instance.emptyId]++;
					}
					obj = consumeRegister;
					lock (obj)
					{
						consumeRegister[__instance.fullId]++;
						goto IL_E1;
					}
				}
				num = __instance.currPoolEnergy;
			}
		IL_E1:
			__instance.currEnergyPerTick = -num;
			__instance.currPoolEnergy -= num * ConfigSettings.FacilityUpdatePeriod; // Populate
			__instance.currPoolEnergy = __instance.currPoolEnergy > 0 ? __instance.currPoolEnergy : 0; // Clamp to limit
			animPool[__instance.entityId].state = (uint)Mathf.CeilToInt((float)num / (float)__instance.energyPerTick * 100f);
			animPool[__instance.entityId].power = (float)__instance.currPoolEnergy / (float)__instance.maxPoolEnergy;
			__result = num;
			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma))]
		static IEnumerable<CodeInstruction> GameTick_Gamma_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: int num2 = this.catalystIncPoint / this.catalystPoint;
				// To:	   int num2 = this.catalystIncPoint * scale / this.catalystPoint;
				codeMatcher.MatchForward(false,
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "catalystIncPoint"),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "catalystPoint"),
					new CodeMatch(OpCodes.Div),
					new CodeMatch(OpCodes.Stloc_S)
				)
				.Advance(2)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ConfigSettings), "_facilityUpdatePeriod")),
					new CodeInstruction(OpCodes.Mul)
				);

				// Change: this.catalystPoint--;
				// To:	   this.catalystPoint -= scale;
				codeMatcher.MatchForward(false,
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "catalystPoint"),
						new CodeMatch(OpCodes.Ldc_I4_1),
						new CodeMatch(OpCodes.Sub),
						new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "catalystPoint")
					)
					.Advance(3)
					.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(ConfigSettings), "_facilityUpdatePeriod"));

				// Change: this.productCount += (float)((double)this.capacityCurrentTick / (double)this.productHeat);
				// To:	   this.productCount += (float)((double)this.capacityCurrentTick * (double)scale / (double)this.productHeat);
				codeMatcher.MatchForward(false,
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "productCount"),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "capacityCurrentTick"),
					new CodeMatch(OpCodes.Conv_R8)
				)
				.Advance(6)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ConfigSettings), "_facilityUpdatePeriod")),
					new CodeInstruction(OpCodes.Conv_R8),
					new CodeInstruction(OpCodes.Mul)
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerGeneratorComponent.GameTick_Gamma failed");
				Log.Error(e);
				return instructions;
			}
		}

	}




}
