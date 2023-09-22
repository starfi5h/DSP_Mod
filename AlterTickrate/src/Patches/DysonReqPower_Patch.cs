using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
    public class DysonReqPower_Patch
    {
		static long[] energyReqs = new long[64];

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.BeforeGameTick))]
		static IEnumerable<CodeInstruction> BeforeGameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: this.energyReqCurrentTick = 0L;
				// To:     UpdateEnergyReqCurrentTick(this, times)
				codeMatcher.MatchForward(false,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldc_I4_0),
					new CodeMatch(OpCodes.Conv_I8),
					new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DysonSphere), "energyReqCurrentTick"))
				)
				.Advance(1).RemoveInstructions(3)
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonReqPower_Patch), nameof(UpdateEnergyReqCurrentTick)))
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler DysonSphere.BeforeGameTick failed");
				Log.Error(e);
				return instructions;
			}
		}

        static void UpdateEnergyReqCurrentTick(DysonSphere __instance, long times)
        {
			if (times % Parameters.PowerUpdatePeriod != 0) 
				return; // Update energyReqCurrentTick only when energyReqs[i] has collected all requests

			int starIndex = __instance.starData.index;
			if (starIndex >= energyReqs.Length)
				energyReqs = new long[GameMain.galaxy.stars.Length]; // Reset for star count changes
			__instance.energyReqCurrentTick = energyReqs[starIndex];
			energyReqs[starIndex] = 0;
		}

		static bool RequestDysonSpherePower_Guard(PowerSystem __instance)
        {
			// Do not run RequestDysonSpherePower if it is called in animation-only local planet
			return (__instance.factory.index + ((int)GameMain.gameTick)) % Parameters.PowerUpdatePeriod == 0;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.RequestDysonSpherePower))]
		static IEnumerable<CodeInstruction> RequestDysonSpherePower_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions, iLGenerator);

				// Insert the following gurad at the start:
				// if (!RequestDysonSpherePower_Guard(this)) return;
				codeMatcher.MatchForward(false,
					new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(PowerSystem), "dysonSphere"))
				)
				.Advance(1).CreateLabel(out Label start)
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonReqPower_Patch), nameof(RequestDysonSpherePower_Guard))),
					new CodeInstruction(OpCodes.Brtrue_S, start),
					new CodeInstruction(OpCodes.Ret)
				);

				// Change: this.dysonSphere.energyReqCurrentTick += num;
				// To:     UpdateEnergyReqs(this, num)
				codeMatcher.End().MatchBack(false,
					new CodeMatch(OpCodes.Dup),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), "energyReqCurrentTick"))
				)
				.RemoveInstructions(2)
				.Advance(1)
				.RemoveInstructions(2)
				.Insert(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonReqPower_Patch), nameof(CollectEnergyReqs)))
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler PowerSystem.RequestDysonSpherePower failed");
				Log.Error(e);
				return instructions;
			}
		}

		static void CollectEnergyReqs(DysonSphere dysonSphere, long value)
        {
			if (dysonSphere.starData.index < energyReqs.Length)
			{
				energyReqs[dysonSphere.starData.index] += value;
			}
        }
	}
}
