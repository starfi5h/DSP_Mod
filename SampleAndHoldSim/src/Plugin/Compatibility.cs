using BepInEx.Configuration;
using DysonSphereProgram.Modding.Blackbox;
using HarmonyLib;
using Multifunction_mod;
using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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
            PlanetMiner.Init(harmony);
            DSP_Battle_Patch.Init(harmony);
            Blackbox_Patch.Init(harmony);
            CheatEnabler_Patch.Init(harmony);
        }

        public static void OnDestory()
        {
            CheatEnabler_Patch.OnDestory();
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
                    harmony.PatchAll(typeof(Warper));

                    Log.Debug("Multfunction_mod compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Multfunction_mod compatibility failed! Last working version: 2.8.6");
                    Log.Warn(e);
                }
            }

            public static class Warper
            {
                [HarmonyTranspiler, HarmonyPatch(typeof(Multifunctionpatch), nameof(Multifunctionpatch.EjectorComponentPatch))]
                public static IEnumerable<CodeInstruction> EjectorComponentPatch_Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    // Repeat __instance.AddSolarSail(tempsail.ss, tempsail.orbitid, tempsail.time + time) multiple times
                    try
                    {
                        // EjectorComponentPatch use prefix to patch, so we need to apply transpiler on it
                        var newinstructions = Ejector_Patch.EjectorComponent_Transpiler(instructions);

                        // Fix skip bullet
                        CodeMatcher matcher = new CodeMatcher(newinstructions)
                            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<Multifunction_mod.Tempsail>), "Add")))
                            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                            .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Warper), nameof(AddTempSail)));

                        return matcher.InstructionEnumeration();
                    }
                    catch
                    {
                        Log.Warn("Transpiler EjectorComponentPatch failed.");
                        return instructions;
                    }
                }

                static void AddTempSail(List<Multifunction_mod.Tempsail> list, Multifunction_mod.Tempsail tempSail, ref EjectorComponent ejector)
                {
                    // Do not multiply if it is local focus planet
                    int times = ejector.planetId == MainManager.FocusPlanetId ? 1 : MainManager.UpdatePeriod;
                    for (int i = 0; i < times; i++)
                        list.Add(tempSail);
                }

                static bool clearFlag;

                [HarmonyPrefix, HarmonyPatch(typeof(Multifunctionpatch), nameof(Multifunctionpatch.Prefix), new Type[] { typeof(ProductionStatistics) })]
                public static bool AddStatDic_Prefix(ref bool __result)
                {
                    // 將星球礦機視為工廠外部, 覆寫原本的邏輯
                    var factoryStatPool = GameMain.data.statistics.production.factoryStatPool;
                    clearFlag = false;
                    __result = true;
                    if (!GameMain.instance.running || !Multifunction.FinallyInit)
                    {
                        Multifunction.addStatDic = new Dictionary<int, Dictionary<int, long>>();
                        Multifunction.consumeStatDic = new Dictionary<int, Dictionary<int, long>>();
                        return false;
                    }
                    if (GameMain.galaxy != null && (Time.time - Multifunctionpatch.time) > 0.5f)
                    {
                        Multifunctionpatch.time = Time.time;
                        if (Multifunction.addStatDic != null)
                        {
                            foreach (var planetEntry in Multifunction.addStatDic)
                            {
                                int factoryIndex = GameMain.galaxy.PlanetById(planetEntry.Key).factoryIndex;
                                foreach (var kvp in planetEntry.Value)
                                {
                                    int itemId = kvp.Key;
                                    int itemNum = (int)kvp.Value; // Assume addStatDic[planetId][itemId] doesn't exceed the range of int
                                    factoryStatPool[factoryIndex].productRegister[itemId] += itemNum;
                                }
                            }
                        }
                        if (Multifunction.consumeStatDic != null)
                        {
                            foreach (var planetEntry in Multifunction.consumeStatDic)
                            {
                                int factoryIndex = GameMain.galaxy.PlanetById(planetEntry.Key).factoryIndex;
                                foreach (var kvp in planetEntry.Value)
                                {
                                    int itemId = kvp.Key;
                                    int itemNum = (int)kvp.Value; // Assume consumeStatDic[planetId][itemId] doesn't exceed the range of int
                                    factoryStatPool[factoryIndex].consumeRegister[itemId] += itemNum;
                                }
                            }
                        }
                        clearFlag = true;
                    }
                    return false; // Overwrite original mod function
                }

                [HarmonyPostfix, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
                public static void AddStatDic_Postfix()
                {
                    if (clearFlag)
                    {
                        // 將額外的統計減去, 回歸原本數值
                        var factoryStatPool = GameMain.data.statistics.production.factoryStatPool;
                        if (Multifunction.addStatDic != null)
                        {
                            foreach (var planetEntry in Multifunction.addStatDic)
                            {
                                int factoryIndex = GameMain.galaxy.PlanetById(planetEntry.Key).factoryIndex;
                                foreach (var kvp in planetEntry.Value)
                                {
                                    int itemId = kvp.Key;
                                    int itemNum = (int)kvp.Value;
                                    factoryStatPool[factoryIndex].productRegister[itemId] -= itemNum; // restore stats
                                }
                                planetEntry.Value.Clear();
                            }
                        }
                        if (Multifunction.consumeStatDic != null)
                        {
                            foreach (var planetEntry in Multifunction.consumeStatDic)
                            {
                                int factoryIndex = GameMain.galaxy.PlanetById(planetEntry.Key).factoryIndex;
                                foreach (var kvp in planetEntry.Value)
                                {
                                    int itemId = kvp.Key;
                                    int itemNum = (int)kvp.Value;
                                    factoryStatPool[factoryIndex].productRegister[itemId] -= itemNum; // restore stats
                                }
                                planetEntry.Value.Clear();
                            }
                        }
                    }
                }
            }
        }

        public static class PlanetMiner
        {
            public const string GUID = "crecheng.PlanetMiner";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    MethodInfo methodInfo = AccessTools.Method(assembly.GetType("PlanetMiner.PlanetMiner"), "Miner");

                    harmony.CreateReversePatcher(methodInfo,
                    new HarmonyMethod(AccessTools.Method(typeof(PlanetMiner), nameof(PlanetMiner.Miner_Original)))).Patch();

                    harmony.Patch(methodInfo, 
                        null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(PlanetMiner), nameof(PlanetMiner.Miner_Transpiler))));

                    Log.Debug("PlanetMiner compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("PlanetMiner compatibility failed! Last working version: 3.0.7");
                    Log.Warn(e);
                }
            }

            static IEnumerable<CodeInstruction> Miner_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                try
                {
                    CodeMatcher matcher = new CodeMatcher(instructions, iLGenerator);

                    
                    matcher.Advance(2)
                        .CreateLabel(out Label start);

                    // if (__instance.planet.id == MainManager.FocusPlanetId) { Miner_Original(__instance); return; }
                    matcher.Insert(
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FactorySystem), "planet")),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PlanetData), "id")),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_FocusPlanetId")),
                            new CodeInstruction(OpCodes.Bne_Un_S, start),
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlanetMiner), nameof(Miner_Original))),
                            new CodeInstruction(OpCodes.Ret)
                        );

                    matcher.MatchForward(false, 
                            new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "frame")
                        )
                        .RemoveInstruction()
                        .Insert(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameMain), "get_gameTick")),
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_UpdatePeriod")),
                            new CodeInstruction(OpCodes.Conv_I8),
                            new CodeInstruction(OpCodes.Div)
                        );
                    
                    matcher.MatchForward(false,
                            new CodeMatch(OpCodes.Add),
                            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(StationComponent), "energy"))
                        )
                        .Insert(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_UpdatePeriod")),
                            new CodeInstruction(OpCodes.Conv_I8),
                            new CodeInstruction(OpCodes.Mul)
                        );

                    // storage2[num7].count = storage2[num7].count + (int)num5 * MainManager.UpdatePeriod;
                    matcher.MatchForward(true,
                            new CodeMatch(OpCodes.Ldelema),
                            new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(StationStore), "count")),
                            new CodeMatch(OpCodes.Dup),
                            new CodeMatch(OpCodes.Ldind_I4),
                            new CodeMatch(OpCodes.Ldloc_S),
                            new CodeMatch(OpCodes.Conv_I4),
                            new CodeMatch(OpCodes.Add)
                        )
                        .Insert(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_UpdatePeriod")),
                            new CodeInstruction(OpCodes.Mul)
                        );

                    // storage3[num8].count = storage3[num8].count + 100 * MainManager.UpdatePeriod;
                    matcher.MatchForward(true,
                            new CodeMatch(OpCodes.Ldelema),
                            new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(StationStore), "count")),
                            new CodeMatch(OpCodes.Dup),
                            new CodeMatch(OpCodes.Ldind_I4),
                            new CodeMatch(OpCodes.Ldc_I4_S),
                            new CodeMatch(OpCodes.Add)
                        )
                        .Insert(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_UpdatePeriod")),
                            new CodeInstruction(OpCodes.Mul)
                        );

                    //return instructions;
                    return matcher.InstructionEnumeration();
                }
                catch
                {
                    Log.Warn("Transpiler Miner failed.");
                    return instructions;
                }
            }

#pragma warning disable CS8321
            public static void Miner_Original(FactorySystem _) // reverse patch
            {

                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    // Replace PlanetMiner.frame (FPS) to GameMain.gameTick (UPS)
                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchForward(false,
                            new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "frame")
                        )
                        .RemoveInstruction()
                        .Insert(
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameMain), "get_gameTick"))
                        );

                    return matcher.InstructionEnumeration();
                }
            }
#pragma warning restore CS8321
        }

        public static class DSP_Battle_Patch
        {
            public const string GUID = "com.ckcz123.DSP_Battle";
            public static bool IsPatched { get; private set; } = false;

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    harmony.PatchAll(typeof(Warper));
                    IsPatched = true;
                    Log.Debug("DSP_Battle compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSP_Battle compatibility failed! Last working version: 2.2.4");
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

                public static void IdleTick(PlanetFactory factory)
                {
                    if (GameMain.instance.timei % 30 != 1) return; //ShieldGenPowerUpdatePatch1: 每半秒檢查1次

                    PowerSystem powerSystem = factory.powerSystem;
                    for (int i = 1; i < powerSystem.netCursor; i++)
                    {
                        PowerNetwork powerNetwork = powerSystem.netPool[i];
                        if (powerNetwork != null && powerNetwork.id == i)
                        {
                            List<int> exchangers = powerNetwork.exchangers;
                            int count = exchangers.Count;
                            for (int k = 0; k < count; k++)
                            {
                                ref PowerExchangerComponent exchanger = ref powerSystem.excPool[exchangers[k]];
                                exchanger.currEnergyPerTick *= MainManager.UpdatePeriod; // 低速狀態下, 補償護盾恢復值
                                DSP_Battle.ShieldGenerator.ShieldGenPowerUpdatePatch1(ref exchanger);
                                exchanger.currEnergyPerTick /= MainManager.UpdatePeriod;
                            }
                        }
                    }
                }

                [HarmonyPrefix, HarmonyPatch(typeof(DSP_Battle.ShieldGenerator), "RefreshPowerNetworkUI")]
                public static void RefreshPowerNetworkUI_Prefix()
                {
                    // 在UI中, 顯示正確的護盾恢復值。(假設UI是當地星球)
                    if (MainManager.UpdatePeriod > 1 && (!MainManager.FocusLocalFactory))
                        DSP_Battle.ShieldGenerator.curShieldIncUI /= MainManager.UpdatePeriod;
                }

                [HarmonyPostfix, HarmonyPatch(typeof(DSP_Battle.ShieldGenerator), "RefreshPowerNetworkUI")]
                public static void RefreshPowerNetworkUI_Postfix()
                {
                    if (MainManager.UpdatePeriod > 1 && (!MainManager.FocusLocalFactory))
                        DSP_Battle.ShieldGenerator.curShieldIncUI *= MainManager.UpdatePeriod;
                }
            }
        }

        public static class Blackbox_Patch
        {
            public const string GUID = "dev.raptor.dsp.Blackbox";
            public static bool IsPatched { get; private set; } = false;

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    harmony.PatchAll(typeof(Warper));
                    IsPatched = true;
                    Log.Debug("Blackbox compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Blackbox compatibility failed! Last working version: 0.2.4");
                    Log.Warn(e);
                }
            }

            public static class Warper
            {
                public static void RevertStats(int factoryIndex)
                {
                    foreach (var blackbox in BlackboxManager.Instance.blackboxes)
                    {
                        var simulation = blackbox.Simulation;
                        if (simulation == null || simulation.factoryRef == null) continue;

                        if (simulation.factoryRef.TryGetTarget(out var factory) && factory.index == factoryIndex)
                        {
                            if (!simulation.isWorking) continue;

                            var Recipe = blackbox.Recipe;
                            var timeIdx = simulation.timeIdx <= 0 ? Recipe.timeSpend - 1 : simulation.timeIdx - 1; // revert to last time
                            
                            //if (BlackboxSimulation.continuousStats)
                            {
                                var totalTimeSpend = (float)Recipe.timeSpend;
                                var curPercent = timeIdx / totalTimeSpend;
                                var nextPercent = (timeIdx + 1) / totalTimeSpend;
                                var factoryStatPool = GameMain.data.statistics.production.factoryStatPool[factory.index];

                                foreach (var production in Recipe.produces)
                                {
                                    var countToAdd = (int)(nextPercent * production.Value) - (int)(curPercent * production.Value);
                                    factoryStatPool.productRegister[production.Key] -= countToAdd; // revert count
                                }
                                foreach (var consumption in Recipe.consumes)
                                {
                                    var countToAdd = (int)(nextPercent * consumption.Value) - (int)(curPercent * consumption.Value);
                                    factoryStatPool.consumeRegister[consumption.Key] -= countToAdd; // revert count
                                }
                            }
                            /* BlackboxSimulation.continuousStats is always true
                            else if (simulation.timeIdx == 0)
                            {
                                var factoryStatPool = GameMain.data.statistics.production.factoryStatPool[factory.index];
                                foreach (var production in Recipe.produces)
                                    factoryStatPool.productRegister[production.Key] -= production.Value;
                                foreach (var consumption in Recipe.consumes)
                                    factoryStatPool.consumeRegister[consumption.Key] -= consumption.Value;
                            }
                            */
                        }
                    }
                }
            }
        }
    
        public static class CheatEnabler_Patch
        {
            public const string GUID = "org.soardev.cheatenabler";            

            public static void Init(Harmony harmony)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                {
                    harmony.PatchAll(typeof(Ejector_Patch)); // No need to dynamic patch ejectors in this case
                    return;
                }

                try
                {
                    harmony.PatchAll(typeof(Warper));
                    Warper.Init();

                    Log.Debug("CheatEnabler compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("CheatEnabler compatibility failed! Last working version: 2.2.0");
                    Log.Warn(e);
                }
            }

            public static void OnDestory()
            {
                Warper.OnDestory();
            }

            private static class Warper
            {
                private static Harmony patch_sample = null;
                private static Harmony patch_cheatEnabler = null;

                public static void Init()
                {
                    CheatEnabler.DysonSpherePatch.SkipBulletValueChanged();
                }

                public static void OnDestory()
                {
                    if (patch_sample != null)
                    {
                        patch_sample.UnpatchSelf();
                        patch_sample = null;
                    }
                    if (patch_cheatEnabler != null)
                    {
                        patch_cheatEnabler.UnpatchSelf();
                        patch_cheatEnabler = null;
                    }
                }

                [HarmonyPrefix, HarmonyPatch(typeof(CheatEnabler.DysonSpherePatch), "SkipBulletValueChanged")]
                internal static void SkipBulletValueChanged_Prefix(ConfigEntry<bool> ___SkipBulletEnabled)
                {
                    if (___SkipBulletEnabled.Value)
                    {
                        if (patch_sample != null)
                        {
                            patch_sample.UnpatchSelf(); // Remove Ejector_Patch frist to avoid conflict
                            patch_sample = null;
                        }
                    }
                    else
                    {
                        if (patch_cheatEnabler != null)
                        {
                            patch_cheatEnabler.UnpatchSelf(); // Remove the IL modification first
                            patch_cheatEnabler = null;
                        }
                    }
                }

                [HarmonyPostfix, HarmonyPatch(typeof(CheatEnabler.DysonSpherePatch), "SkipBulletValueChanged")]
                internal static void SkipBulletValueChanged_Postfix(ConfigEntry<bool> ___SkipBulletEnabled)
                {
                    if (___SkipBulletEnabled.Value)
                    {
                        if (patch_cheatEnabler == null)
                        {
                            patch_cheatEnabler = new Harmony(Plugin.GUID + "-CE");
                            patch_cheatEnabler.Patch(
                                AccessTools.Method(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate)),
                                null,
                                null,
                                new HarmonyMethod(AccessTools.Method(typeof(Warper), nameof(EjectorComponent_ReplaceAddDysonSail))));
                        }
                    }
                    else
                    {
                        if (patch_sample == null)
                        {
                            patch_sample = Harmony.CreateAndPatchAll(typeof(Ejector_Patch)); // Apply Ejector_Patch after CE patch is unload
                        }
                    }
                }

                private static IEnumerable<CodeInstruction> EjectorComponent_ReplaceAddDysonSail(IEnumerable<CodeInstruction> instructions)
                {
                    var matcher = new CodeMatcher(instructions);
                    matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CheatEnabler.DysonSpherePatch.SkipBulletPatch), "AddDysonSail"))
                    ).RemoveInstruction().Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.planetId))), //new
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Warper), nameof(AddDysonSailWithPlanetId)))
                    );
                    return matcher.InstructionEnumeration();
                }

                private static void AddDysonSailWithPlanetId(DysonSwarm swarm, int orbitId, VectorLF3 uPos, VectorLF3 endVec, int planetId)
                {
                    // If the sail doesn't come from the focus local planet, repeat
                    int repeatCount = (planetId == MainManager.FocusPlanetId) ? 1 : MainManager.UpdatePeriod;
                    for (int i = 0; i < repeatCount; i++)
                        CheatEnabler.DysonSpherePatch.SkipBulletPatch.AddDysonSail(swarm, orbitId, uPos, endVec);
                }
            }
        }
    }
}
