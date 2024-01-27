using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;

namespace BuildToolOpt
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            DeliverySlotsTweaks_Patch.Init();
            Nebula_Patch.Init();
            CheatEnabler_Patch.Init(harmony);
        }

        public static class DeliverySlotsTweaks_Patch
        {
            public const string GUID = "starfi5h.plugin.DeliverySlotsTweaks";

            public static void Init()
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;

                try
                {
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("DeliverySlotsTweaks.Plugin");

                    var entry = (ConfigEntry<bool>)(AccessTools.Field(classType, "EnableHologram").GetValue(null));
                    Plugin.EnableHologram = entry == null || !entry.Value;

                    Plugin.Log.LogDebug("DeliverySlotsTweaks hologram=" + Plugin.EnableHologram);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("DeliverySlotsTweaks compatibility failed! Last working version: 1.5.0");
                    Plugin.Log.LogWarning(e);
                }
            }
        }

        public static class Nebula_Patch
        {
            public const string GUID = "dsp.nebula-multiplayer";

            public static void Init()
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _)) return;

                Plugin.EnableReplaceStation = false;
                Plugin.EnableHologram = false;
                Plugin.Log.LogDebug("Nebula: Disable replace station and hologram function");
            }
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
                    var classType = assembly.GetType("CheatEnabler.FactoryPatch");
                    harmony.Patch(AccessTools.Method(classType, "ArrivePlanet"),
                        new HarmonyMethod(AccessTools.Method(typeof(CheatEnabler_Patch), nameof(ArrivePlanet_Prefix))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("CheatEnabler compatibility failed! Last working version: 2.3.9");
                    Plugin.Log.LogWarning(e);
                }
            }

            // https://github.com/soarqin/DSP_Mods/blob/master/CheatEnabler/FactoryPatch.cs#L78-L105
            internal static bool ArrivePlanet_Prefix()
            {
                return !ReplaceStationLogic.IsReplacing;
            }
        }
    }
}