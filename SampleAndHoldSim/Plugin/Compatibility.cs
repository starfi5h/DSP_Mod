using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace SampleAndHoldSim
{
    public class Compatibility
    {
        public static class NebulaAPI
        {
            public const string GUID = "dsp.nebula-multiplayer-api";
            public static bool IsClient { get; private set; }
            public static bool IsPatched { get; private set; }

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) 
                        return;
                    if (!NebulaModAPI.NebulaIsInstalled || IsPatched)
                        return;

                    NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                    NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
                    IsPatched = true;

                    Log.Info("Nebula compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Nebula compatibility failed!");
                    Log.Warn(e);
                }
            }

            public static void OnMultiplayerGameStarted()
            {
                IsClient = NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
                if (IsClient)
                {
                    Log.Warn("Nebula client: Unload plugin!");
                    Plugin.instance.OnDestroy();
                }
            }

            public static void OnMultiplayerGameEnded()
            {
                if (IsClient)
                {
                    Log.Warn("Nebula client: Reload plugin!");
                    Plugin.instance.Awake();
                }
                IsClient = false;
            }
        }

        public static class CommonAPI
        {
            public const string GUID = "dsp.common-api.CommonAPI";

            public static void Init(Harmony harmony)
            {
                try
                {
                    // Patch fall-back calls in PlanetExtensionSystem
                    // Change their GameData.factories => GameData_Patch.workfactories
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type targetType = assembly.GetType("CommonAPI.Systems.PlanetExtensionSystem");
                    harmony.Patch(targetType.GetMethod("PowerUpdateOnlySinglethread"), null, null, new HarmonyMethod(typeof(GameData_Patch).GetMethod("ReplaceFactories")));
                    harmony.Patch(targetType.GetMethod("PreUpdateOnlySinglethread"), null, null, new HarmonyMethod(typeof(GameData_Patch).GetMethod("ReplaceFactories")));
                    harmony.Patch(targetType.GetMethod("UpdateOnlySinglethread"), null, null, new HarmonyMethod(typeof(GameData_Patch).GetMethod("ReplaceFactories")));
                    harmony.Patch(targetType.GetMethod("PostUpdateOnlySinglethread"), null, null, new HarmonyMethod(typeof(GameData_Patch).GetMethod("ReplaceFactories")));

                    Log.Info("CommonAPI compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("CommonAPI compatibility failed! Last working version: 1.5.4");
                    Log.Warn(e);
                }
            }
        }

        public static class DSPOptimizations
        {
            public const string GUID = "com.Selsion.DSPOptimizations";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    harmony.PatchAll(typeof(DSPOptimizations));
                    Log.Info("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSPOptimizations compatibility failed! Last working version: 1.1.10");
                    Log.Warn(e);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(StationComponent), "UpdateInputSlots")]
            [HarmonyPatch(typeof(StationComponent), "UpdateOutputSlots")]
            public static bool UpdateSlots_Prefix(CargoTraffic traffic)
            {
                if (MainManager.TryGet(traffic.factory.index, out var factory))
                {
                    // StationStorageOpt will let slots update happen in idle tick, so we need to disable them
                    // If station's factory is idle, skip update
                    if (!factory.IsActive)
                        return false;
                }
                return true;
            }
        }

        public static class Auxilaryfunction
        {
            public const string GUID = "cn.blacksnipe.dsp.Auxilaryfunction";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("Auxilaryfunction.AuxilaryfunctionPatch+GameTick1Patch");

                    // Vein got deleted for slowed planets, don't know why :(
                    //harmony.Patch(classType.GetMethod("Prefix"), null, null, new HarmonyMethod(typeof(GameData_Patch).GetMethod("ReplaceFactories")));

                    // Suppress stop factory and stop dyson sphere function
                    harmony.Patch(classType.GetMethod("Prefix"), new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("SuppressModPatch")));

                    Log.Info("Auxilaryfunction compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Auxilaryfunction compatibility failed! Last working version: 1.7.6");
                    Log.Warn(e);
                }
            }

            public static bool SuppressModPatch(ref bool __result)
            {
                __result = true;
                return false;
            }
        }
    }
}
