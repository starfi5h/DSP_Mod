using HarmonyLib;

namespace BulletTime
{
    public class SimulateSpeed_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIUPSTool), nameof(UIUPSTool.OnSimSpeedIncreaseButtonClick))]

        static void OnSimSpeedIncreaseButtonClick(UIUPSTool __instance)
        {
            if (VFInput.shift)
            {
                Log.Debug("Set simulate speed to " + BulletTimePlugin.MaxSpeedupScale.Value);
                __instance.targetSimSpeed = BulletTimePlugin.MaxSimulationSpeed.Value;
                __instance.SetSimSpeed(__instance.targetSimSpeed);
                __instance.showTarValueTextTime = 1.2f;
                __instance.simSpeedTarValueText.text = string.Format("目标模拟速度".Translate(), __instance.targetSimSpeed);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIUPSTool), nameof(UIUPSTool.OnSimSpeedDecreaseButtonClick))]

        static void OnSimSpeedDecreaseButtonClick(UIUPSTool __instance)
        {
            if (VFInput.shift && __instance.targetSimSpeed > 1.0)
            {
                __instance.targetSimSpeed = 1;
                __instance.SetSimSpeed(__instance.targetSimSpeed);
                __instance.showTarValueTextTime = 1.2f;
                __instance.simSpeedTarValueText.text = string.Format("目标模拟速度".Translate(), __instance.targetSimSpeed);
            }
        }
    }
}
