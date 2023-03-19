using HarmonyLib;
using System;

namespace AlterTickrate.Patches
{
    public class LabProduce_Patch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble))]
        static void FacilitySpeedModify(ref float power)
        {
            // only multiply speed when power > 10%
            if (power >= 0.1f)
            {
                power *= Parameters.LabProduceUpdatePeriod;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        static bool LocalAnim_Lab(FactorySystem __instance, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
        {
            if ((__instance.factory.index + GameMain.gameTick) % Parameters.LabProduceUpdatePeriod == 0) // normal tick
                return true;

            if (__instance.factory == GameMain.localPlanet?.factory)
            {
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.labCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out int start, out int end))
                {
                    AnimData[] entityAnimPool = __instance.factory.entityAnimPool;
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.labPool[i].id == i)
                        {
                            ref AnimData animData = ref entityAnimPool[__instance.labPool[i].entityId];
                            animData.Step01(animData.state, 0.016666668f); // advance time by dt without updating state
                        }
                    }
                }
            }
            return false;
        }
    }
}
