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
		const float DELTA_TIME = 0.016666668f; // num6 in PowerSystem.GameTick

		[HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static bool GameTick(PowerSystem __instance, long time)
        {
            if (__instance.factory != GameMain.localPlanet?.factory || (__instance.factory.index + (int)time) % Parameters.PowerUpdatePeriod == 0)
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
						animData.Step2(animData.state, DELTA_TIME, animData.power, 2f);
					}
					else if (genPool[i].geothermal)
                    {
						animData.Step(animData.state, DELTA_TIME, 2f, 0f);
					}
					else
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 1f);
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
		[HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
		static bool PrepareTick(ProductionStatistics __instance)
        {
			for (int i = 0; i < __instance.gameData.factoryCount; i++)
			{
				// replace this.factoryStatPool[i].PrepareTick():
				var factoryStat = __instance.factoryStatPool[i];
				Array.Clear(factoryStat.productRegister, 0, 12000);
				Array.Clear(factoryStat.consumeRegister, 0, 12000);

				// Clear power related registers only when power will be updated in this tick
				if ((i + GameMain.gameTick) % Parameters.PowerUpdatePeriod == 0)					
				{
					factoryStat.powerGenRegister = 0L;
					factoryStat.powerConRegister = 0L;
					factoryStat.powerDisRegister = 0L;
					factoryStat.powerChaRegister = 0L;
				}
				factoryStat.hashRegister = 0L;
				__instance.factoryStatPool[i].itemChanged = false;
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
				__instance.state += 0.00557f * Parameters.PowerUpdatePeriod * 5.0f; // speed up process by 5 times
				if (__instance.state >= __instance.targetState)
				{
					__instance.state = __instance.targetState;
				}
			}
			else if (__instance.state > __instance.targetState)
			{
				__instance.state -= 0.00557f * Parameters.PowerUpdatePeriod * 5.0f; // speed up process by 5 times
				if (__instance.state <= __instance.targetState)
				{
					__instance.state = __instance.targetState;
				}
			}
			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.InputUpdate))]
		static IEnumerable<CodeInstruction> InputUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// scale enegry increase in buffer
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: if (num >= this.maxPoolEnergy - this.currPoolEnergy)
				// To:     if (num * scale >= this.maxPoolEnergy - this.currPoolEnergy)
				codeMatcher.MatchForward(false,
					new CodeMatch(i => i.IsLdloc()),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "maxPoolEnergy"),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "currPoolEnergy"),
					new CodeMatch(OpCodes.Sub),
					new CodeMatch(OpCodes.Blt)
				)
				.Advance(1)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod))),
					new CodeInstruction(OpCodes.Conv_I8),
					new CodeInstruction(OpCodes.Mul)
				);


				// Change: this.currPoolEnergy += num;
				// To:     this.currPoolEnergy = Math.Min(this.currPoolEnergy + num * scale, this.maxPoolEnergy);
				codeMatcher.End()
					.MatchBack(false,
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "currPoolEnergy"),
						new CodeMatch(i => i.IsLdloc()),
						new CodeMatch(OpCodes.Add),
						new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "currPoolEnergy")
					)
					.Advance(4)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod))),
						new CodeInstruction(OpCodes.Conv_I8),
						new CodeInstruction(OpCodes.Mul)
					)
					.Advance(1)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerExchangerComponent), "maxPoolEnergy")),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), "Min", new Type[] { typeof(long), typeof(long) }))
					);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerExchangerComponent.InputUpdate failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.OutputUpdate))]
		static IEnumerable<CodeInstruction> OutputUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// scale enegry decrease in buffer
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: if (num >= this.currPoolEnergy)
				// To:     if (num * scale >= this.currPoolEnergy)
				codeMatcher.MatchForward(false,
					new CodeMatch(i => i.IsLdloc()),
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "currPoolEnergy"),
					new CodeMatch(OpCodes.Blt)
				)
				.Advance(1)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod))),
					new CodeInstruction(OpCodes.Conv_I8),
					new CodeInstruction(OpCodes.Mul)
				);


				// Change: this.currPoolEnergy -= num;
				// To:     this.currPoolEnergy = Math.Max(this.currPoolEnergy + num * scale, 0);
				codeMatcher.End()
					.MatchBack(false,
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "currPoolEnergy"),
						new CodeMatch(i => i.IsLdloc()),
						new CodeMatch(OpCodes.Sub),
						new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "currPoolEnergy")
					)
					.Advance(4)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.FacilityUpdatePeriod))),
						new CodeInstruction(OpCodes.Conv_I8),
						new CodeInstruction(OpCodes.Mul)
					)
					.Advance(1)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldc_I4_0),
						new CodeInstruction(OpCodes.Conv_I8),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), "Max", new Type[] { typeof(long), typeof(long) }))
					);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerExchangerComponent.OutputUpdate failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma))]
		static IEnumerable<CodeInstruction> GameTick_Gamma_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// scale gravition len consumption, photon production
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
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.FacilityUpdatePeriod))),
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
					.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod)));

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
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod))),
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

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GenEnergyByFuel))]
		static IEnumerable<CodeInstruction> GenEnergyByFuel_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// scale fuel consumption
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Insert: num *= scale;
				// Before  if (this.fuelEnergy >= num)
				codeMatcher.MatchForward(false,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "fuelEnergy"),
					new CodeMatch(OpCodes.Ldloc_0),
					new CodeMatch(OpCodes.Blt)
				)
				.Insert(
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.PowerUpdatePeriod))),
					new CodeInstruction(OpCodes.Conv_I8),
					new CodeInstruction(OpCodes.Mul),
					new CodeInstruction(OpCodes.Stloc_0)
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerGeneratorComponent.GenEnergyByFuel failed");
				Log.Error(e);
				return instructions;
			}
		}
	}
}
