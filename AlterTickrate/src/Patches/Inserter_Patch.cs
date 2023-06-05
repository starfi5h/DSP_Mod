using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
	public class Inserter_Patch
	{
		[HarmonyPrefix, HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
		[HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
		static void InternalUpdatePrefix(ref float power)
		{
			if (power >= 0.1f)
			{
				// scale speed of transferring cargo by x
				power *= Parameters.InserterSpeedRate;
			}
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
		[HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdateNoAnim))]
		static IEnumerable<CodeInstruction> InputUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				var codeMatcher = new CodeMatcher(instructions);

				// Change: this.idleTick = num - 1;
				// To:     this.idleTick = num - Parameters.InserterUpdatePeriod;
				codeMatcher
					.MatchForward(false,
						new CodeMatch(i => i.IsLdloc()),
						new CodeMatch(OpCodes.Ldc_I4_1),
						new CodeMatch(OpCodes.Sub),
						new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "idleTick")
					)
					.Repeat(matcher => matcher
						.Advance(1)
						.SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(Parameters), nameof(Parameters.InserterUpdatePeriod)))
					);

				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Log.Error("Transpiler InserterComponent.InternalUpdate failed");
				Log.Error(e);
				return instructions;
			}
		}
	}
}
