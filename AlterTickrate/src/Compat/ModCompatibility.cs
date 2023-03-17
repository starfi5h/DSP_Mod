using HarmonyLib;
using System;
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
            return true;
        }

        public static class Incompat
        {
            static string message = "AlterTickrate is not compat with following mods:";

            public static bool Check(Harmony harmony)
            {
                bool isCompat = true;
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (pluginInfos.ContainsKey("com.starfi5h.plugin.SampleAndHoldSim"))
                {
                    message += "\nSampleAndHoldSim";
                    isCompat = false;
                }
                if (pluginInfos.ContainsKey("dev.raptor.dsp.Blackbox"))
                {
                    message += "\nBlackbox";
                    isCompat = false;
                }

                if (pluginInfos.ContainsKey("org.LoShin.GenesisBook") 
                    || pluginInfos.ContainsKey("org.kremnev8.plugin.BetterMachines") 
                    || pluginInfos.ContainsKey("top.awbugl.DSP.BeltSpeedEnhancement"))
                {
                    // These mods change belt speed, which make the max speed > 5
                    Plugin.plugin.SaveConfig(1, 1);
                }

                if  (!isCompat)
                {
                    harmony.PatchAll(typeof(Incompat));
                }
                return isCompat;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
            static void OnGameLoaded()
            {
                UIMessageBox.Show("AlterTickrate", message, "确定".Translate(), 3);
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
