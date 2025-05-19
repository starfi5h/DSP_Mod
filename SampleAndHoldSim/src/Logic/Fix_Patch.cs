using HarmonyLib;

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
				}
            }
		}
	}
}
