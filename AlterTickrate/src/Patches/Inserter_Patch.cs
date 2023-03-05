using HarmonyLib;
using System;

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
				power *= Parameters.InserterSpeedRate;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
		static bool InsertInto(PlanetFactory __instance, int entityId, byte itemInc, out byte remainInc, ref int __result)
		{
			remainInc = itemInc;
			int beltId = __instance.entityPool[entityId].beltId;
			if (beltId > 0)
				return true;
			// When insert into building, update period is LCM(FacilityUpdatePeriod, InserterUpdatePeriod)
			__result = 0;
			return (GameMain.gameTick + __instance.index) % Parameters.FacilityUpdatePeriod == 0;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.PickFrom))]
		static bool PickFrom(PlanetFactory __instance, int entityId, out byte stack, out byte inc, ref int __result)
		{
			stack = 1;
			inc = 0;
			int beltId = __instance.entityPool[entityId].beltId;
			if (beltId > 0)
				return true;
			// When pick from building, update period is LCM(FacilityUpdatePeriod, InserterUpdatePeriod)
			__result = 0;
			return (GameMain.gameTick + __instance.index) % Parameters.FacilityUpdatePeriod == 0;
		}
	}
}
