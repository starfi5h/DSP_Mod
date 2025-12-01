using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static CheatEnabler.Patches.DysonSpherePatch.SkipBulletPatch;

namespace SampleAndHoldSim
{
    public class Compatibility
    {
        public static bool IsNoticed { get; set; } = false;

        static string errorMessage = "";
        static string warnMessage = "";

        public static bool ShouldPatchEjector()
        {
            return !BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GenesisBook_Patch.GUID);
        }

        public static void Init(Harmony harmony)
        {
            System.Version bepInExVersion = typeof(BaseUnityPlugin).Assembly.GetName().Version;
            if (bepInExVersion.Minor != 4 || bepInExVersion.Build != 17)
            {
                warnMessage = $"You are using BepInEx version {bepInExVersion}. The version that is not 5.4.17 may not work correctly.\n";
                warnMessage += $"您使用的BepInEx版本为{bepInExVersion}。非5.4.17可能无法正常运作";
            }

            Weaver.Init(harmony);
            CommonAPI.Init(harmony);
            CheatEnabler_Patch.Init(harmony);
            Auxilaryfunction_Patch.Init(harmony);
            Multfunction_mod_Patch.Init(harmony);
            PlanetMiner.Init(harmony);
            GenesisBook_Patch.Init(harmony);

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
                Log.Warn(warnMessage);
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
                    var harmontyMethod = new HarmonyMethod(typeof(GameLogic_Patch).GetMethod("ReplaceFactories"));
                    harmony.Patch(targetType.GetMethod("PowerUpdateOnlySinglethread"), null, null, harmontyMethod);
                    harmony.Patch(targetType.GetMethod("PreUpdateOnlySinglethread"), null, null, harmontyMethod);
                    harmony.Patch(targetType.GetMethod("UpdateOnlySinglethread"), null, null, harmontyMethod);
                    harmony.Patch(targetType.GetMethod("PostUpdateOnlySinglethread"), null, null, harmontyMethod);

                    Log.Debug("CommonAPI compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "CommonAPI compatibility failed! Last working version: 1.6.7";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
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
                    string message = "Auxilaryfunction compatibility failed! Last working version: 3.0.2";
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
                // TODO
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
                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(MainManager), nameof(MainManager.UpdatePeriod))),
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
                    Warper.Init();
                    Log.Debug("CheatEnabler compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "CheatEnabler 'Skip bullet period'(跳过子弹阶段) compatibility failed! Last working version: 2.4.0";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }

            private static class Warper
            {
                
                public static void Init()
                {
                    CheatEnabler.Patches.DysonSpherePatch.SkipBulletEnabled.SettingChanged += (obj, s) => OnSettingChanged();
                }

                static void OnSettingChanged()
                {
                    Log.Debug(CheatEnabler.Patches.DysonSpherePatch.SkipBulletEnabled.Value);
                }

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

        public static class GenesisBook_Patch
        {
            public const string GUID = "org.LoShin.GenesisBook";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;
                    harmony.PatchAll(typeof(GenesisBook_Patch));
                    Log.Debug("GenesisBook compatibility - OK");
                }
                catch (Exception e)
                {
                    string message = "GenesisBook compatibility failed! Last working version: 3.1.4";
                    Log.Warn(message);
                    Log.Warn(e);
                    errorMessage += message + "\n";
                }
            }


            [HarmonyTranspiler, HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
            public static IEnumerable<CodeInstruction> EjectorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchForward(false,
                            new CodeMatch(OpCodes.Ldloc_S),
                            new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SailBullet), nameof(SailBullet.lBegin)))
                            );
                    if (matcher.IsInvalid)
                    {
                        Log.Warn("EjectorComponent_Transpiler: Can't find SailBullet.lBegin");
                        return instructions;
                    }
                    CodeInstruction loadInstruction = matcher.Instruction;

                    // 改動: 目標從DysonSwarm.AddBullet改成EjectorPatches.Ejector_PatchMethod
                    matcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Ejector_PatchMethod"));
                    if (matcher.IsInvalid)
                    {
                        Log.Warn("EjectorComponent_Transpiler: Can't find Ejector_PatchMethod");
                        return instructions;
                    }

                    matcher.MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "time"));

                    matcher
                        .Advance(1)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.planetId))),
                            loadInstruction,
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.orbitId))),
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.bulletCount))),
                            HarmonyLib.Transpilers.EmitDelegate<Action<int, Vector3, int, int>>((planetId, localPos, orbitId, bulletCount) =>
                            {
                                if (MainManager.Planets.TryGetValue(planetId, out var factoryData) && factoryData.IsNextIdle)
                                {
                                    int stationPilerLevel = GameMain.history.stationPilerLevel;
                                    int count = stationPilerLevel > bulletCount ? bulletCount : stationPilerLevel;
                                    for (int i = 0; i < count; i++)
                                    {
                                        factoryData.AddDysonData(planetId, -orbitId, localPos);
                                    }
                                }
                            })
                        );

                    return matcher.InstructionEnumeration();
                }
                catch (Exception ex)
                {
                    string message = "GenesisBook compatibility failed! Last working version: 3.1.4";
                    errorMessage += message + "\n";
                    Log.Warn(message);
                    Log.Warn(ex);
                    return instructions;
                }
            }
        }
    }
}
