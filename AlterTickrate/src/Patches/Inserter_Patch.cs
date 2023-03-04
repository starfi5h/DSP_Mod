using HarmonyLib;

namespace AlterTickrate.Patches
{
    public class Inserter_Patch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        private static void InternalUpdatePrefix(ref float power)
        {
            if (power >= 0.1f)
            {
                power *= Parameters.InserterSpeedRate;
            }
        }
    }
}
