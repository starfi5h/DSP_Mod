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
            BlueprintTweaks_Patch.Init(harmony);
            Multfunction_mod_Patch.Init(harmony);
            RebindBuildBar_Patch.Init(harmony);
            UnlimitedFoundations_Patch.Init(harmony);
            Nebula_Patch.Init(harmony);
        }

        public static class BlueprintTweaks_Patch
        {
            public const string GUID = "org.kremnev8.plugin.BlueprintTweaks";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("BlueprintTweaks.BlueprintPasteExtension");
                    harmony.Patch(AccessTools.Method(classType, "OnUpdate"), null, null,
                        new HarmonyMethod(typeof(DeliveryPackagePatch).GetMethod(nameof(DeliveryPackagePatch.UIBuildMenu_Transpiler))));
                    harmony.Patch(AccessTools.Method(classType, "CheckItems"), null, null,
                        new HarmonyMethod(typeof(DeliveryPackagePatch).GetMethod(nameof(DeliveryPackagePatch.UIBuildMenu_Transpiler))));
                    harmony.Patch(AccessTools.Method(classType, "CheckItems"), null, null,
                        new HarmonyMethod(typeof(DeliveryPackagePatch).GetMethod(nameof(DeliveryPackagePatch.TakeItem_Transpiler))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Auxilaryfunction compatibility failed! Last working version: 1.6.4");
                    Plugin.Log.LogWarning(e);
                }
            }
        }


        public static class Auxilaryfunction_Patch
        {
            public const string GUID = "cn.blacksnipe.dsp.Auxilaryfunction";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("Auxilaryfunction.Auxilaryfunction");
                    harmony.Patch(AccessTools.Method(classType, "AutoMovetounbuilt"), null, null,
                        new HarmonyMethod(typeof(DeliveryPackagePatch).GetMethod(nameof(DeliveryPackagePatch.UIBuildMenu_Transpiler))));

                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Auxilaryfunction compatibility failed! Last working version: 2.8.2");
                    Plugin.Log.LogWarning(e);
                }
            }
        }

        public static class Multfunction_mod_Patch
        {
            public const string GUID = "cn.blacksnipe.dsp.Multfuntion_mod";
            static ConfigEntry<bool> architectMode = null;

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("Multifunction_mod.GUIDraw");
                    harmony.Patch(AccessTools.Method(classType, "BuildPannel"), null, new HarmonyMethod(typeof(Multfunction_mod_Patch).GetMethod(nameof(BuildPannel_Postfix))));

                    architectMode = (ConfigEntry<bool>)(AccessTools.Field(AccessTools.TypeByName("Multifunction_mod.Multifunction"), "ArchitectMode").GetValue(null));
                    DeliveryPackagePatch.architectMode = architectMode.Value;
                    Plugin.Log.LogDebug("Multfunction_mod ArchitectModeEnabled: " + DeliveryPackagePatch.architectMode);

                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Multfunction_mod compatibility failed! Last working version: 3.4.5");
                    Plugin.Log.LogWarning(e);
                }
            }

            public static void BuildPannel_Postfix()
            {
                DeliveryPackagePatch.architectMode = architectMode.Value;
            }
        }

        public static class Nebula_Patch
        {
            public const string GUID = "dsp.nebula-multiplayer";
            public static bool IsActive { get; private set; }
            static bool IsPatched;

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Patch(harmony);
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


            private static void Patch(Harmony harmony)
            {
                // Separate for using NebulaModAPI
                if (!NebulaModAPI.NebulaIsInstalled || IsPatched)
                    return;
                NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;

                Type classType = AccessTools.TypeByName("NebulaWorld.SimulatedWorld");
                harmony.Patch(AccessTools.Method(classType, "SetupInitialPlayerState"), null, new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(Plugin.ApplyConfigs))));

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
    
        public static class RebindBuildBar_Patch
        {
            public const string GUID = "org.kremnev8.plugin.RebindBuildBar";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("RebindBuildBar.Patches");
                    harmony.Patch(AccessTools.Method(classType, "EnableButton"), null, null, new HarmonyMethod(typeof(DeliveryPackagePatch).GetMethod(nameof(DeliveryPackagePatch.UIBuildMenu_Transpiler))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("RebindBuildBar compatibility failed! Last working version: 1.0.4");
                    Plugin.Log.LogWarning(e);
                }
            }
        }

        public static class UnlimitedFoundations_Patch
        {
            public const string GUID1 = "com.aekoch.mods.dsp.UnlimitedFoundations";
            public const string GUID2 = "com.tinysquid.infinitefoundations";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID1)
                        || BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID2))
                    harmony.Patch(AccessTools.Method(typeof(DeliveryPackagePatch), nameof(DeliveryPackagePatch.GetItemCount)), 
                        null, 
                        new HarmonyMethod(typeof(UnlimitedFoundations_Patch).GetMethod(nameof(UnlimitedFoundations_Patch.GetItemCount_Postfix))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("UnlimitedFoundations compatibility failed!");
                    Plugin.Log.LogWarning(e);
                }
            }

            public static void GetItemCount_Postfix(int itemId, ref int __result)
            {
                if (itemId == 1131) // FOUNDATION_ITEM_ID
                {
                    __result = 9999;
                }
            }
        }
    }
}