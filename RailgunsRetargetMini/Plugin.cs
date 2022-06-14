using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace RailgunsRetargetMini
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.RailgunsRetargetMini";
        public const string NAME = "RailgunsRetargetMini";
        public const string VERSION = "1.0.0";
        Harmony harmony;

        public void Awake()
        {
            harmony = new(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(Patch));

            ConfigEntry<int> config1 = Config.Bind<int>("General", "CheckPeriod", 120,
            "Check reachable orbits every x ticks.\n每x祯检查可用轨道一次");
            Configs.CheckPeriod = config1.Value;

# if DEBUG
            Log.LogSource = Logger;
            Patch.GameData_GameTick();
# endif
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Configs
    {
        public static int CheckPeriod = 120;
    }

# if DEBUG
    public static class Log
    {
        public static ManualLogSource LogSource;
        public static void Error(object obj) => LogSource.LogError(obj);
        public static void Warn(object obj) => LogSource.LogWarning(obj);
        public static void Info(object obj) => LogSource.LogInfo(obj);
        public static void Debug(object obj) => LogSource.LogDebug(obj);
    }
# endif
}
