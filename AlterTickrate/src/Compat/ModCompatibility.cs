using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AlterTickrate.Compat
{
    public class ModCompatibility
    {
        public static bool Init(Harmony harmony)
        {
            if (!Incompat.Check(harmony))
                return false;

            DSPOptimizations.Init(harmony);
#if DEBUG
            Incompat.OnGameLoaded();
#endif
            return true;
        }

        public static class Incompat
        {
            static string message = "";

            public static bool Check(Harmony harmony)
            {
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey("com.starfi5h.plugin.SampleAndHoldSim"))
                {
                    message += "\nSampleAndHoldSim";
                }
                if (pluginInfos.ContainsKey("dev.raptor.dsp.Blackbox"))
                {
                    message += "\nBlackbox";
                }

                if (pluginInfos.ContainsKey("org.LoShin.GenesisBook")
                    || pluginInfos.ContainsKey("org.kremnev8.plugin.BetterMachines")
                    || pluginInfos.ContainsKey("top.awbugl.DSP.BeltSpeedEnhancement"))
                {
                    // These mods change belt speed, which make the max speed > 5
                    Plugin.plugin.SaveConfig(1, 1);
                }

                harmony.PatchAll(typeof(Incompat));
                if  (message != "")
                {
                    message = "AlterTickrate is not compat with following mods:" + message;                    
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
            public static void OnGameLoaded()
            {
                if (message != "")
                    UIMessageBox.Show("AlterTickrate", message, "确定".Translate(), 3);
                else if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("me.xiaoye97.plugin.Dyson.LDBTool"))
                {
                    var type = AccessTools.TypeByName("xiaoye97.LDBTool");
                    if (type != null)
                    {
                        Log.Debug("Checking LDBTool proto...");
                        List<List<Proto>> TotalDict = (List<List<Proto>>)AccessTools.Field(type, "TotalDict").GetValue(null);
                        bool flag = false;
                        foreach (var list in TotalDict)
                        {
                            foreach (var proto in list)
                            {
                                if (proto is ItemProto || proto is RecipeProto)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            message = "Proto is modified! Please use SampleAndHoldSim for better compatibility.\n侦测到物品和配方修改,可能无法兼容.请改用SampleAndHoldSim";
                            Log.Warn(message);
                            UIMessageBox.Show("AlterTickrate", message, "确定".Translate(), 3);
                        }
                    }
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

                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    Type classType = assembly.GetType("DSPOptimizations.StationStorageOpt");
                    harmony.Patch(AccessTools.Method(classType, "RunStorageLogic"), new HarmonyMethod(typeof(DSPOptimizations).GetMethod("RunStorageLogic_Prefix")));

                    Log.Debug("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSPOptimizations compatibility failed! Last working version: 1.1.14");
                    Log.Warn(e);
                }
            }

            public static bool RunStorageLogic_Prefix()
            {
                //Log.Debug(GameMain.gameTick % Parameters.StorageUpdatePeriod);
                return GameMain.gameTick % Parameters.StorageUpdatePeriod == 0;
            }
        }
    }


}
