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
		[HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickInserters), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int) })]
		static void GameTickInserters_Prefix(ref long time)
		{
			// bool flag = time % 60L == 0L && isActive;
			// 讓flag可以在當地星球每60 * Parameters.InserterUpdatePeriod tick觸發
			time /= Parameters.InserterUpdatePeriod;
		}

		// 已知: 在buffer的一個貨物結構中(InsertCargoDirect), 10個位置中有6個常數可以幫助定位
		// 根據鴿籠定理, TryPickItem檢查index~index+5的範圍內必能定位到貨物
		// 因為藍帶最快5buffer/s因此可以每2tick檢查1次

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
