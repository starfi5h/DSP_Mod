using DysonSphereProgram.Modding.Blackbox;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class Compatibility
    {
        public static bool IsNoticed { get; set; } = false;

        static string errorMessage = "";
        static string warnMessage = "";

        public static void Init(Harmony harmony)
        {
            CommonAPI.Init(harmony);
            DSPOptimizations.Init(harmony);
            Auxilaryfunction_Patch.Init(harmony);
            Multfunction_mod_Patch.Init(harmony);
            PlanetMiner.Init(harmony);
            Blackbox_Patch.Init(harmony);
            CheatEnabler_Patch.Init(harmony);

            if (!string.IsNullOrEmpty(errorMessage) || !string.IsNullOrEmpty(warnMessage))
            {
                harmony.PatchAll(typeof(Compatibility));
            }
#if DEBUG
            ShowMessage();
#endif
        }

        public static void OnDestory()
        {
            CheatEnabler_Patch.OnDestory();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        static void ShowMessage()
        {
            if (IsNoticed || !Plugin.instance.WarnIncompat.Value) return;
            IsNoticed = true;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = "The following compatible patches didn't success:\n模拟帧对以下mod的兼容性补丁失效:\n" + errorMessage;
                UIMessageBox.Show("SampleAndHoldSim 模拟帧", errorMessage, "确定".Translate(), "Don't show",
                    3, null, () => Plugin.instance.WarnIncompat.Value = false);
            }
            if (!string.IsNullOrEmpty(warnMessage))
            {
                UIMessageBox.Show("SampleAndHoldSim 模拟帧", warnMessage, "确定".Translate(), "Don't show", 
                    3, null, () => Plugin.instance.WarnIncompat.Value = false);
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
                    string message = "CommonAPI compatibility failed! Last working version: 1.6.5";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
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
                    string message = "DSPOptimizations compatibility failed! Last working version: 1.1.16";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
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

        public static class Auxilaryfunction_Patch
        {
            public const string GUID = "cn.blacksnipe.dsp.Auxilaryfunction";
            static bool enable = false;
            static int storedUpdatePeriod = 1;

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type targetType = assembly.GetType("Auxilaryfunction.Patch.GameTickPatch");
                    harmony.Patch(targetType.GetMethod("set_Enable"), new HarmonyMethod(typeof(Auxilaryfunction_Patch).GetMethod(nameof(OnStopFactory))));

                    Log.Debug("Auxilaryfunction compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "Auxilaryfunction compatibility failed! Last working version: 2.7.7";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            public static void OnStopFactory(bool value)
            {
                if (enable == value) return;
                enable = value;
                if (enable)
                {
                    storedUpdatePeriod = MainManager.UpdatePeriod;
                    MainManager.UpdatePeriod = 1;
                }
                else
                {
                    MainManager.UpdatePeriod = storedUpdatePeriod;
                    storedUpdatePeriod = 1;
                }
                Log.Debug($"Auxilaryfunction stop factory:{enable}  ratio => {MainManager.UpdatePeriod}");
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
                    warnMessage += "Multifunction: some game-breaking features are not compatible\nSampleAndHoldSim对Multifunction的改机制功能(跳过太阳帆子弹阶段,星球矿机等)兼容性不佳,可能会造成统计数据异常";
                    //Log.Debug("Multfunction_mod compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "Multfunction_mod compatibility failed! Last working version: 3.4.4";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            public static class Warper
            {
                /*
                [HarmonyTranspiler, HarmonyPatch(typeof(Multifunctionpatch.SomePatch), nameof(Multifunctionpatch.SomePatch.EjectorComponentPatch))]
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
                */
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
                    string message = "PlanetMiner compatibility failed! Last working version: 3.0.8";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
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
                    string message = "Blackbox compatibility failed! Last working version: 0.2.4";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
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
                            /* BlackboxSimulation.continuousStats is always true */
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
                    //harmony.PatchAll(typeof(Warper)); //Note: Patching on generic class PatchImpl will make CE unable to function.
                    harmony.PatchAll(typeof(CheatEnabler_Patch));
#if DEBUG
                    OnGameBegin();
#endif
                    Log.Debug("CheatEnabler compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "CheatEnabler skipbullet compatibility failed! Last working version: 2.3.26";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
            static void OnGameBegin()
            {
                try
                {
                    Warper.Init();
                }
                catch (Exception e)
                {
                    string message = "CheatEnabler skipbullet compatibility failed! Last working version: 2.3.26";
                    Log.Warn(message);
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
                    bool enable = CheatEnabler.Patches.DysonSpherePatch.SkipBulletEnabled.Value;
                    SkipBulletValueChanged_Prefix(enable);
                    SkipBulletValueChanged_Postfix(enable);
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

                internal static void SkipBulletValueChanged_Prefix(bool enable)
                {
                    if (enable)
                    {
                        if (patch_sample != null)
                        {
                            Log.Info("patch_sample UnpatchSelf");
                            patch_sample.UnpatchSelf(); // Remove Ejector_Patch frist to avoid conflict
                            patch_sample = null;
                        }
                    }
                    else
                    {
                        if (patch_cheatEnabler != null)
                        {
                            Log.Info("patch_cheatEnabler UnpatchSelf");
                            patch_cheatEnabler.UnpatchSelf(); // Remove the IL modification first
                            patch_cheatEnabler = null;
                        }
                    }
                }

                internal static void SkipBulletValueChanged_Postfix(bool enable)
                {
                    if (enable)
                    {
                        if (patch_cheatEnabler == null)
                        {
                            // Somehow this gets constantly trigger when CE unpatchself
                            Log.Info("patch_cheatEnabler create");
                            patch_cheatEnabler = Harmony.CreateAndPatchAll(typeof(SkipBullet_Compat_Patch), Plugin.GUID + ".patch_cheatEnabler");
                        }
                    }
                    else
                    {
                        if (patch_sample == null)
                        {
                            Log.Info("patch_sample create");
                            patch_sample = Harmony.CreateAndPatchAll(typeof(Ejector_Patch), Plugin.GUID+ ".patch_sample"); // Apply Ejector_Patch after CE patch is unload
                        }
                    }
                }
            }

            private static class SkipBullet_Compat_Patch
            {
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
                private static IEnumerable<CodeInstruction> EjectorComponent_ReplaceAddDysonSail(IEnumerable<CodeInstruction> instructions)
                {
                    try
                    {
                        var matcher = new CodeMatcher(instructions);
                        matcher.MatchForward(false,
                            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CheatEnabler.Patches.DysonSpherePatch.SkipBulletPatch), "AddDysonSail"))
                        );
                        if (matcher.IsInvalid)
                        {
                            Log.Warn("Unable to replace SkipBulletPatch.AddDysonSail for CheatEnabler");
                            return instructions;
                        }
                        matcher.RemoveInstruction().Insert(
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.planetId))), //new
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SkipBullet_Compat_Patch), nameof(AddDysonSailWithPlanetId)))
                        );
                        return matcher.InstructionEnumeration();
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Error in EjectorComponent_ReplaceAddDysonSail");
                        Log.Warn(e);
                        return instructions;
                    }
                }

                private static void AddDysonSailWithPlanetId(DysonSwarm swarm, int orbitId, VectorLF3 uPos, VectorLF3 endVec, int planetId)
                {
                    // If the sail doesn't come from the focus local planet, repeat
                    int repeatCount = (planetId == MainManager.FocusPlanetId) ? 1 : MainManager.UpdatePeriod;
                    for (int i = 0; i < repeatCount; i++)
                        CheatEnabler.Patches.DysonSpherePatch.SkipBulletPatch.AddDysonSail(swarm, orbitId, uPos, endVec);
                }
            }
        }
    }
}
