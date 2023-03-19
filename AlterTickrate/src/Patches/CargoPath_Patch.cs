using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
	public class CargoPath_Patch
	{
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Update))]
		static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// After : int num5 = this.chunks[i * 3 + 2]; // belt speed
				// Insert: if (__instance.cargoContainer != Parameters.LocalCargoContainer) 
				//		         num5 *= Parameters.BeltUpdatePeriod; 
				codeMatcher.MatchForward(true,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "chunks"),
					new CodeMatch(i => i.IsLdloc()),
					new CodeMatch(OpCodes.Ldc_I4_3),
					new CodeMatch(OpCodes.Mul),
					new CodeMatch(OpCodes.Ldc_I4_2),
					new CodeMatch(OpCodes.Add),
					new CodeMatch(OpCodes.Ldelem_I4),
					new CodeMatch(OpCodes.Stloc_S)
				)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.BeltUpdatePeriod))),
					new CodeInstruction(OpCodes.Mul)
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler CargoPath.Update failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(SpraycoaterComponent), nameof(SpraycoaterComponent.InternalUpdate))]
		static IEnumerable<CodeInstruction> SpraycoaterComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: this.sprayTime += (flag ? (beltComponent2.speed * (int)(1000f * num6)) : 0);
				// To    : this.sprayTime += (flag ? (beltComponent2.speed * scale * (int)(1000f * num6)) : 0);
				codeMatcher.MatchForward(true,
					new CodeMatch(i => i.IsLdloc()),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "speed")
				)
				.Advance(1)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.BeltUpdatePeriod))),
					new CodeInstruction(OpCodes.Mul)
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler SpraycoaterComponent.InternalUpdate failed");
				Log.Error(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(MonitorComponent), nameof(MonitorComponent.InternalUpdate))]
		static IEnumerable<CodeInstruction> MonitorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: double num9 = (double)this.targetCargoBytes / 10.0;
				// To    : double num9 = (double)this.targetCargoBytes / (10.0 / scale);
				codeMatcher.MatchForward(true,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "targetCargoBytes"),
					new CodeMatch(OpCodes.Conv_R8),
					new CodeMatch(OpCodes.Ldc_R8),
					new CodeMatch(OpCodes.Div),
					new CodeMatch(OpCodes.Stloc_S)
				)
				.Advance(-1)
				.Insert(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.BeltUpdatePeriod))),
					new CodeInstruction(OpCodes.Conv_R8),
					new CodeInstruction(OpCodes.Div)
				);

				// Before: this.cargoFlow += num;
				// Insert: num /= scale;
				codeMatcher.MatchForward(false,
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "cargoFlow"),
					new CodeMatch(OpCodes.Ldloc_0),
					new CodeMatch(OpCodes.Add),
					new CodeMatch(OpCodes.Stfld)
				)
				.Advance(3)
				.Insert(
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.BeltUpdatePeriod))),
					new CodeInstruction(OpCodes.Div),
					new CodeInstruction(OpCodes.Stloc_0)
				);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler MonitorComponent.InternalUpdate failed");
				Log.Error(e);
				return instructions;
			}
		}
	}
}
