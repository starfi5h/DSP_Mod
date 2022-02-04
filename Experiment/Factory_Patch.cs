using HarmonyLib;
using System;
using Unity;
using UnityEngine;

namespace Experiment
{
    class Factory_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.DeterminePreviews))]
        static void DeterminePreviews(BuildTool_PathAddon __instance)
        {
            Log.Info($"{__instance.handbp.lpos} DeterminePreviews");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.SnapToBelt))]
        static void SnapToBelt(BuildTool_PathAddon __instance)
        {
            Log.Info($"{__instance.handbp.lpos} SnapToBelt");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.SnapToBeltAutoAdjust))]
        static void SnapToBeltAutoAdjust(BuildTool_PathAddon __instance)
        {
            Log.Info($"{__instance.handbp.lpos} SnapToBeltAutoAdjust");
            Log.Warn($"{__instance.buildPreviews.Count} Previews");
        }

    }
}
