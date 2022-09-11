using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPOptimizations
    {
        public const string NAME = "DSPOptimizations";
        public const string GUID = "com.Selsion.DSPOptimizations";
        public const string VERSION = "1.1.11";

        private static Action<GameData> InitFactoryInfo;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Reset stationIdMap when factories unload
                Type classType = assembly.GetType("DSPOptimizations.StationStorageOpt");
                InitFactoryInfo = AccessTools.MethodDelegate<Action<GameData>>(AccessTools.Method(classType, "InitFactoryInfo"));
                
                harmony.PatchAll(typeof(DSPOptimizations));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.LeaveStar))]
        public static void LeaveStar_Prefix()
        {
            //Client will unload all factories once they leave the star system
            if (GameMain.data.factoryCount == 0)
            {
                Log.Debug("Unload factories data for DSPOptimizations");
                InitFactoryInfo.Invoke(GameMain.data);
            }
        }
    }
}
