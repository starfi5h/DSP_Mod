using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class Fix_Patch // Fixes for potential errors
	{
		[HarmonyPrefix, HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.CalculateActualEnergyPerTick))]
		public static bool CalculateActualEnergyPerTick_Overwrite(ref PowerExchangerComponent __instance, bool isOutput, ref long __result)
		{
			int inc;
			if (isOutput)
			{
				inc = __instance.poolInc;
			}
			else
			{
				int emptyCount = __instance.emptyCount;
				int emptyInc = __instance.emptyInc;
				inc = __instance.split_inc(ref emptyCount, ref emptyInc, 1);
			}
			if (inc > 0)
			{
				if (inc >= Cargo.accTableMilli.Length) // Fix for abnormal inc
                {
					inc = Cargo.accTableMilli.Length - 1;
				}
				__result = __instance.energyPerTick + (long)(__instance.energyPerTick * Cargo.accTableMilli[inc] + 0.1);
				return false;
			}
			__result = __instance.energyPerTick;
			return false;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
		static IEnumerable<CodeInstruction> UpdateVeinCollection_Fix(IEnumerable<CodeInstruction> instructions)
		{
			// 修正原版遊戲中, 當大礦機儲量超過上限時會反而增加礦機緩衝礦物的bug

			// Changes:
			//		num2 = ((num2 > productCount) ? productCount : num2);
			//		if (num2 != 0) => 改成 if (num2 > 0)
			//		{
			//			StationStore[] array2 = this.storage;
			//			int num3 = 0;

			try
			{
				var codeMatcher = new CodeMatcher(instructions)
					.End()
					.MatchBack(false,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Brfalse),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "storage"),
						new CodeMatch(OpCodes.Ldc_I4_0)
					)
					.Advance(1)
					.InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
					.SetOpcodeAndAdvance(OpCodes.Ble);
				return codeMatcher.InstructionEnumeration();
			}
			catch (Exception ex)
            {
				Log.Warn("Fix_Patch.UpdateVeinCollection_Fix fail!");
				Log.Warn(ex);
				return instructions;
			}
		}

		public static void FixMinerProductCount()
        {
			int facotryCount = GameMain.data.factoryCount;
			for (int factoryIndex = 0; factoryIndex < facotryCount; factoryIndex++)
            {
				var factory = GameMain.data.factories[factoryIndex];
				if (factory == null) continue;
				var factorySystem = factory.factorySystem;

				for (int i = 0; i < factorySystem.minerCursor; i++)
                {
					ref var miner = ref factorySystem.minerPool[i];
					if (miner.productCount < 0) miner.productCount = 0; // Fix miners that have negative tmp storage
					else if (miner.productCount > 50) miner.productCount = 50; // Assume tmp storage max limit is 50
				}
            }
		}
	}
}
