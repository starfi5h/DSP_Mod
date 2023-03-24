using HarmonyLib;
using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            NebulaAPI.Init();
            CommonAPI.Init(harmony);
            DSPOptimizations.Init(harmony);
            Multfunction_mod_Patch.Init(harmony);
            DSP_Battle_Patch.Init(harmony);
        }

        public static class NebulaAPI
        {
            public const string GUID = "dsp.nebula-multiplayer-api";
            public static bool IsClient { get; private set; }
            public static bool IsPatched { get; private set; }

            public static void Init()
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) 
                        return;

                    Patch();
                    Log.Debug("Nebula compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Nebula compatibility failed!");
                    Log.Warn(e);
                }
            }

            private static void Patch()
            {
                // Separate for using NebulaModAPI
                if (!NebulaModAPI.NebulaIsInstalled || IsPatched)
                    return;
                NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
                IsPatched = true;
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

                    Log.Debug("CommonAPI compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("CommonAPI compatibility failed! Last working version: 1.5.7");
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
                    Log.Debug("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSPOptimizations compatibility failed! Last working version: 1.1.13");
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

        public static class Multfunction_mod_Patch
        {
            public const string GUID = "cn.blacksnipe.dsp.Multfuntion_mod";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("Multfunction_mod.Multifunctionpatch");

                    // EjectorComponentPatch use prefix to patch, so we need to apply transpiler on it
                    harmony.Patch(classType.GetMethod("EjectorComponentPatch"), null, null, new HarmonyMethod(typeof(Dyson_Patch).GetMethod("EjectorComponent_Transpiler")));

                    // TODO: Fix skip bullet
                    harmony.Patch(classType.GetMethod("EjectorComponentPatch"), null, null, new HarmonyMethod(typeof(Multfunction_mod_Patch).GetMethod("EjectorComponentPatch_Transpiler")));

                    Log.Debug("Multfunction_mod compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Multfunction_mod compatibility failed! Last working version: 2.7.4");
                    Log.Warn(e);
                }
            }

            public static IEnumerable<CodeInstruction> EjectorComponentPatch_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Repeat __instance.AddSolarSail(tempsail.ss, tempsail.orbitid, tempsail.time + time) multiple times
                try
                {
                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<Multfunction_mod.Tempsail>), "Add")))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                        .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Warper), "AddTempSail"));

                    return matcher.InstructionEnumeration();
                }
                catch
                {
                    Log.Warn("Transpiler EjectorComponentPatch failed.");
                    return instructions;
                }
            }

            public static class Warper
            {
                public static void AddTempSail(List<Multfunction_mod.Tempsail> list, Multfunction_mod.Tempsail tempSail, ref EjectorComponent ejector)
                {
                    // Do not multiply if it is local focus planet
                    int times = MainManager.FocusLocalFactory && ejector.planetId == GameMain.localPlanet?.id ? 1 : MainManager.UpdatePeriod;
                    for (int i = 0; i < times; i++)
                        list.Add(tempSail);
                }
            }
        }

        public static class DSP_Battle_Patch
        {
            public const string GUID = "com.ckcz123.DSP_Battle";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    harmony.PatchAll(typeof(Warper));
                    Log.Debug("DSP_Battle compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSP_Battle compatibility failed! Last working version: 2.1.4");
                    Log.Warn(e);
                }
            }

            public static class Warper
            {
                [HarmonyPostfix, HarmonyPatch(typeof(DSP_Battle.WaveStages), "Update")]
                public static void Update()
                {
                    // Focus on star system that is under attack during battle stage
                    if (DSP_Battle.Configs.nextWaveState == 3)
                    {
                        MainManager.FocusStarIndex = DSP_Battle.Configs.nextWaveStarIndex;
                    }
                    else
                    {
                        MainManager.FocusStarIndex = -1;
                    }
                }
            }
        }
    }
}
