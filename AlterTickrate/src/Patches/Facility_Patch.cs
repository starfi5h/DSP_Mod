using HarmonyLib;

namespace AlterTickrate.Patches
{
    public class Facility_Patch
    {
        public static float FacilitySpeedRate = 3.0f;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble))]
        private static void FacilitySpeedModify(ref float power)
        {
            // only multiply speed when power is normal
            if (power >= 0.1f)
            {
                power *= FacilitySpeedRate;
            }
        }

        // Note: LabComponent.InternalUpdateResearch need to handle by speed due to matrixPoints (num)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void ResearchSpeedModify(ref float speed)
        {
            speed *= FacilitySpeedRate;
        }
    }
}
