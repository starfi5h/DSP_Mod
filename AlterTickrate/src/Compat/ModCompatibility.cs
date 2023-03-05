using HarmonyLib;
using System;
using System.Reflection;

namespace AlterTickrate.Compat
{
    public class ModCompatibility
    {
        public static bool Init(Harmony harmony)
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.starfi5h.plugin.SampleAndHoldSim"))
            {
                Incompat.Message += "SampleAndHoldSim";
                harmony.PatchAll(typeof(Incompat));
                return false;
            }

            DSPOptimizations.Init(harmony);
            return true;
        }

        public static class Incompat
        {
            public static string Message = "AlterTickrate is not compat with following mods:\n";

            [HarmonyPostfix]
            [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
            static void OnGameLoaded()
            {
                UIMessageBox.Show("AlterTickrate", Message, "确定".Translate(), 3);
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
