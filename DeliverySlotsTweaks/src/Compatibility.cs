using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DeliverySlotsTweaks
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            CheatEnabler_Patch.Init(harmony);
        }

        public static class CheatEnabler_Patch
        {
            public const string GUID = "org.soardev.cheatenabler";

            public static void Init(Harmony harmony)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;

                try
                {
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("CheatEnabler.FactoryPatch");
                    harmony.Patch(AccessTools.Method(classType, "ArchitectModeValueChanged"),
                        null, new HarmonyMethod(AccessTools.Method(typeof(CheatEnabler_Patch), nameof(ArchitectModeValueChanged_Postfix))));
                    DeliveryPackagePatch.architectMode = ((ConfigEntry<bool>)(AccessTools.Field(classType, "ArchitectModeEnabled").GetValue(null))).Value;
                    Plugin.Log.LogDebug("ArchitectModeEnabled: " + DeliveryPackagePatch.architectMode);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("CheatEnabler compatibility failed! Last working version: 2.4.0");
                    Plugin.Log.LogWarning(e);
                }
            }

            internal static void ArchitectModeValueChanged_Postfix(ConfigEntry<bool> ___ArchitectModeEnabled)
            {
                if (___ArchitectModeEnabled.Value)
                    DeliveryPackagePatch.architectMode = true;
                else
                    DeliveryPackagePatch.architectMode = false;
            }
        }
    }
}