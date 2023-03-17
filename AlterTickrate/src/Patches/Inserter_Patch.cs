using HarmonyLib;

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
	}
}
