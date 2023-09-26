using BepInEx.Configuration;
using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace DeliverySlotsTweaks
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            CheatEnabler_Patch.Init(harmony);
            Nebula_Patch.Init();
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

        public static class Nebula_Patch
        {
            public const string GUID = "dsp.nebula-multiplayer-api";
            public static bool IsActive { get; private set; }
            static bool IsPatched;

            public static void Init()
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Patch();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Nebula compatibility failed!");
                    Plugin.Log.LogWarning(e);
                }
            }

            public static bool IsOthers() // Action triggered by packets from other player
            {
                var factoryManager = NebulaModAPI.MultiplayerSession.Factories;
                return factoryManager.IsIncomingRequest.Value && factoryManager.PacketAuthor != NebulaModAPI.MultiplayerSession.LocalPlayer.Id;
            }


            private static void Patch()
            {
                // Separate for using NebulaModAPI
                if (!NebulaModAPI.NebulaIsInstalled || IsPatched)
                    return;
                NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
#if DEBUG
                OnMultiplayerGameStarted();
#endif
                IsPatched = true;
            }

            private static void OnMultiplayerGameStarted()
            {
                IsActive = NebulaModAPI.IsMultiplayerActive;
            }

            private static void OnMultiplayerGameEnded()
            {
                IsActive = false;
            }
        }
    }
}