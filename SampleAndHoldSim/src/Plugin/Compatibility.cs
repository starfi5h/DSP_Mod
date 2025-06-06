using DysonSphereProgram.Modding.Blackbox;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static CheatEnabler.Patches.DysonSpherePatch.SkipBulletPatch;

namespace SampleAndHoldSim
{
    public class Compatibility
    {
        public static bool IsNoticed { get; set; } = false;

        static string errorMessage = "";
        static string warnMessage = "";

        public static void Init(Harmony harmony)
        {
            Weaver.Init(harmony);
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
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        static void ShowMessage()
        {
            if (IsNoticed || !Plugin.instance.WarnIncompat.Value) return;
            IsNoticed = true;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = "The following compatible patches didn't success:\n模拟帧对以下mod的兼容性补丁失效:\n" + errorMessage;
                UIMessageBox.Show("SampleAndHoldSim Report 模拟帧兼容提示", errorMessage, "确定".Translate(), "Don't show",
                    3, null, () => Plugin.instance.WarnIncompat.Value = false);
            }
            if (!string.IsNullOrEmpty(warnMessage))
            {
                UIMessageBox.Show("SampleAndHoldSim Report 模拟帧兼容提示", warnMessage, "确定".Translate(), "Don't show", 
                    3, null, () => Plugin.instance.WarnIncompat.Value = false);
            }
        }

        public static class Weaver
        {
            public const string GUID = "Weaver";

            public static void Init(Harmony _)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _)) return;
                warnMessage += "SampleAndHoldSim is not compatible with Weaver: stats may be incorrect\nSampleAndHoldSim对Weaver尚未兼容，可能会统计数据异常";
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

                    harmony.Patch(methodInfo, null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(PlanetMiner), nameof(PlanetMiner.Miner_Transpiler))));

                    methodInfo = AccessTools.Method(assembly.GetType("PlanetMiner.PlanetMiner"), "GenerateEnergy");
                    harmony.Patch(methodInfo, null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(PlanetMiner), nameof(PlanetMiner.GenerateEnergy_Transpiler))));

                    Log.Debug("PlanetMiner compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "PlanetMiner compatibility failed! Last working version: 3.1.1";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            static IEnumerable<CodeInstruction> GenerateEnergy_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    CodeMatcher matcher = new CodeMatcher(instructions);

                    // Multiply energy gain based on UpdatePeriod
                    matcher.End().MatchBack(false,
                        new CodeMatch(OpCodes.Add),
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(StationComponent), "energy"))
                    )
                    .Insert(
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainManager), "get_UpdatePeriod")),
                        new CodeInstruction(OpCodes.Conv_I8),
                        new CodeInstruction(OpCodes.Mul)
                    );

                    return matcher.InstructionEnumeration();
                }
                catch (Exception ex)
                {
                    Log.Warn("PlanetMiner GenerateEnergy_Transpiler failed.");
                    Log.Warn(ex);
                    return instructions;
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

                    // Fix frame count to use gameTick(UPS) in game instead of Unity update(FPS)
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

                    return matcher.InstructionEnumeration();
                }
                catch (Exception ex)
                {
                    Log.Warn("PlanetMiner Miner_Transpiler failed.");
                    Log.Warn(ex);
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
                    return;
                }

                try
                {
                    harmony.PatchAll(typeof(Warper));
                    Log.Debug("CheatEnabler compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "CheatEnabler 'Skip bullet period'(跳过子弹阶段) compatibility failed! Last working version: 2.3.31";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            private static class Warper
            {
                [HarmonyPrefix]
                [HarmonyPatch(typeof(CheatEnabler.Patches.DysonSpherePatch.SkipBulletPatch), "AddDysonSail")]
                public static bool AddDysonSail_Prefix(ref EjectorComponent ejector, DysonSwarm swarm, VectorLF3 uPos, VectorLF3 endVec, int[] consumeRegister)
                {
                    // If the sail doesn't come from the focus local planet, repeat
                    int repeatCount = (ejector.planetId == MainManager.FocusPlanetId) ? 1 : MainManager.UpdatePeriod;

                    // AddDysonSail origin author: soarqin
                    // https://github.com/soarqin/DSP_Mods/blob/master/CheatEnabler/Patches/DysonSpherePatch.cs
                    var index = swarm.starData.index;
                    var orbitId = ejector.orbitId;
                    var delta1 = endVec - swarm.starData.uPosition;
                    var delta2 = VectorLF3.Cross(endVec - uPos, swarm.orbits[orbitId].up).normalized * Math.Sqrt(swarm.dysonSphere.gravity / swarm.orbits[orbitId].radius);
                    var bulletCount = ejector.bulletCount;
                    lock (swarm)
                    {
                        var cache = _sailsCache[index];
                        var len = _sailsCacheLen[index];
                        if (cache == null)
                        {
                            SetSailsCacheCapacity(index, 256);
                            cache = _sailsCache[index];
                        }

                        // Main modify part
                        var shootCount = _fireAllBullets ? (bulletCount * repeatCount) : repeatCount; // Repeat
                        var capacity = _sailsCacheCapacity[index];
                        var leastCapacity = len + shootCount; // Repeat
                        if (leastCapacity > capacity)
                        {
                            do
                            {
                                capacity *= 2;
                            } while (leastCapacity > capacity);
                            SetSailsCacheCapacity(index, capacity);
                            cache = _sailsCache[index];
                        }
                        _sailsCacheLen[index] = len + shootCount; // Repeat
                        var end = len + shootCount; // Repeat
                        for (var i = len; i < end; i++)
                            cache[i].FromData(delta1, delta2 + RandomTable.SphericNormal(ref swarm.randSeed, 0.5), orbitId);
                    }

                    // consumeRegister is handle in elsewhere, so no need to modify
                    if (_fireAllBullets)
                    {
                        if (!ejector.incUsed)
                        {
                            ejector.incUsed = ejector.bulletInc >= bulletCount;
                        }
                        ejector.bulletInc = 0;
                        ejector.bulletCount = 0;
                        lock (consumeRegister)
                        {
                            consumeRegister[ejector.bulletId] += bulletCount;
                        }
                    }
                    else
                    {
                        var inc = ejector.bulletInc / bulletCount;
                        if (!ejector.incUsed)
                        {
                            ejector.incUsed = inc > 0;
                        }
                        ejector.bulletInc -= inc;
                        ejector.bulletCount = bulletCount - 1;
                        if (ejector.bulletCount == 0)
                        {
                            ejector.bulletInc = 0;
                        }
                        lock (consumeRegister)
                        {
                            consumeRegister[ejector.bulletId]++;
                        }
                    }
                    ejector.time = ejector.coldSpend;
                    ejector.direction = -1;

                    // Full replace
                    return false;
                }
            }
        }
    }
}
