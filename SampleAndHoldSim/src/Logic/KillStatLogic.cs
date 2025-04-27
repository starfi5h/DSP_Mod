using HarmonyLib;

namespace SampleAndHoldSim
{
    public class KillStatLogic1
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(KillStatistics), nameof(KillStatistics.RegisterFactoryKillStat))]
		static void RegisterFactoryKillStat_Postfix(KillStatistics __instance, int factoryIndex, int modelIndex)
		{
			// Multiple factory kill for slow planets
			if (factoryIndex == MainManager.FocusFactoryIndex) return; // Focus local planet
			ref AstroKillStat ptr = ref __instance.factoryKillStatPool[factoryIndex];
			ptr.killRegister[modelIndex] += MainManager.UpdatePeriod - 1;
		}
	}

	public class KillStatLogic2
	{
        [HarmonyPrefix]
        [HarmonyPatch(typeof(KillStatistics), nameof(KillStatistics.PrepareTick))]
        static bool KillStatistics_PrepareTick()
        {
            // PrepareTick every x tick, x = MainManager.UpdatePeriod. Preserver kill stat in 1~period tick
            // Due to skill(weapon projectiles) is update every tick, the kill may happen in every tick.
            // So in this place use global UpdatePeriod instead of factory active/inactive tick
            int period = MainManager.UpdatePeriod;
            if (period <= 1) return true;
            return GameMain.gameTick % period == 1; // mod 1: Reset after GameTick is record
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KillStatistics), nameof(KillStatistics.GameTick))]
        static bool KillStatistics_GameTick(KillStatistics __instance, long time)
        {
            // GameTick update every x tick, each time update x frames of stats
            int period = MainManager.UpdatePeriod;
            if (period <= 1) return true;
            if (GameMain.gameTick % period != 0) return false; // mod 0: Record tick

            long startTime = time - period;
            if (startTime < 0) startTime = 0;
            for (int i = 0; i < __instance.starKillStatPool.Length; i++)
            {
                if (__instance.starKillStatPool[i] != null)
                {
                    for (long t = startTime; t <= time; t++)
                        __instance.starKillStatPool[i].GameTick(t);
                }
            }
            for (int j = 0; j < __instance.factoryKillStatPool.Length; j++)
            {
                if (__instance.factoryKillStatPool[j] != null)
                {
                    for (long t = startTime; t <= time; t++)
                        __instance.factoryKillStatPool[j].GameTick(t);
                }
            }
            if (__instance.mechaKillStat != null)
            {
                for (long t = startTime; t <= time; t++)
                    __instance.mechaKillStat.GameTick(t);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KillStatistics), nameof(KillStatistics.AfterTick))]
        static bool KillStatistics_AfterTick()
        {
            // Skip entirely AstroKillStat.AfterTick is just calling ClearRegister, which is already done in PrepareTick
            return false;
        }
    }
}
