using HarmonyLib;
using System;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPFreeMechaCustom
    {
        private const string NAME = "DSPFreeMechaCustom";
        private const string GUID = "Appun.DSP.plugin.FreeMechaCustom";
        private const string VERSION = "0.0.1";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                // Sync player appearance after clicking apply button
                harmony.PatchAll(typeof(DSPFreeMechaCustom));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIMechaEditor), nameof(UIMechaEditor.ApplyMechaAppearance))]
        public static bool ApplyMechaAppearance_Prefix()
        {
            // DSPFreeMechaCustom has done setting mainPlayer.mecha.appearance
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMechaMatsGroup), nameof(UIMechaMatsGroup.OnApplyClick))]
        public static void OnApplyClick_Postfix(UIMechaMatsGroup __instance)
        {
            // Nebula send PlayerMechaArmor packet after ApplyMechaAppearance
            __instance.mechaEditor.ApplyMechaAppearance();
        }
    }
}
