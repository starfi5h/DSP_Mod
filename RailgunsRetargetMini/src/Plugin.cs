using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace RailgunsRetargetMini
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(NebulaCompat.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.RailgunsRetargetMini";
        public const string NAME = "RailgunsRetargetMini";
        public const string VERSION = "1.2.0";
        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            Configs.ForceRetargeting = Config.Bind<bool>("General", "ForceRetargeting", false,
            "Retarget orbit for unset ejctors\n使未设置的电磁弹射器自动换轨").Value;

            Configs.Method = Config.Bind<int>("General", "Method", 1,
            "Which retarget algorithm should use (1 or 2)\n使用哪种算法(1 或 2)").Value;

            Configs.RotatePeriod = Config.Bind<int>("Method1", "RotatePeriod", 60,
            "Rotate to next orbit every x ticks.\n无法发射时,每x祯切换至下一个轨道").Value;

            Configs.CheckPeriod = Config.Bind<int>("Method2", "CheckPeriod", 120,
            "Check reachable orbits every x ticks.\n无法发射时,每x祯检查可用轨道一次").Value;

            Log = Logger;
            harmony = new(PluginInfo.PLUGIN_GUID);
            if (Configs.Method == 1)
                harmony.PatchAll(typeof(Patch1));
            else
                harmony.PatchAll(typeof(Patch2));

            try
            {
                harmony.PatchAll(typeof(UIPatch));
            }
            catch (Exception e)
            {
                Logger.LogWarning("Can't patch ejector control UI!");
                Logger.LogWarning(e);
            }

            NebulaCompat.Init();
        }

        public void OnDestroy()
        {
            UIPatch.OnDestory();
            harmony.UnpatchSelf();
        }
    }

    public static class Configs
    {
        public static bool ForceRetargeting = false;
        public static int Method = 1;
        public static int RotatePeriod = 60;
        public static int CheckPeriod = 120;
    }
}
