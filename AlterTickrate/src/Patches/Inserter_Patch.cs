using HarmonyLib;

namespace AlterTickrate.Patches
{
    public class Inserter_Patch
    {
        public static float InserterSpeedRate = 2.0f;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), nameof(InserterComponent.InternalUpdate))]
        private static void InternalUpdatePrefix(ref float power)
        {
            if (power >= 0.1f)
            {
                power *= InserterSpeedRate;
            }
        }
    }
}
