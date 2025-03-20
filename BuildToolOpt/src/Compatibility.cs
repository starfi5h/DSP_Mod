using HarmonyLib;
using System;
using System.Reflection;

namespace BuildToolOpt
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            Nebula_Patch.Init();
            CheatEnabler_Patch.Init(harmony);
        }

        public static class Nebula_Patch
        {
            public const string GUID = "dsp.nebula-multiplayer";

            public static void Init()
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _)) return;

                Plugin.EnableReplaceStation = false;
                Plugin.EnableHologram = false;
                Plugin.EnableStationBuildOptimize = false;
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
                    var classType = assembly.GetType("CheatEnabler.Patches.FactoryPatch");
                    harmony.Patch(AccessTools.Method(classType, "ArrivePlanet"),
                        new HarmonyMethod(AccessTools.Method(typeof(CheatEnabler_Patch), nameof(ArrivePlanet_Prefix))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("CheatEnabler compatibility failed! Last working version: 2.3.26");
                    Plugin.Log.LogWarning(e);
                }
            }

            // https://github.com/soarqin/DSP_Mods/blob/master/CheatEnabler/Patches/FactoryPatch.cs#L146-L173
            internal static bool ArrivePlanet_Prefix()
            {
                return !ReplaceStationLogic.IsReplacing;
            }
        }
    }
}