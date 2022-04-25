using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace HoverTooltipDelay
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.HoverTooltipDelay";
        public const string NAME = "HoverTooltipDelay";
        public const string VERSION = "1.0.0";
        public static Plugin instance;
        Harmony harmony;

        ConfigEntry<int> DelayFrame;

        public void LoadConfig()
        {
            DelayFrame = Config.Bind<int>("General", "DelayFrame", 15, "Time delay for tooltip to show up when mouse hovering on a building.");
            Patch.SetDelay(DelayFrame.Value);
        }

        public void Awake()
        {
            instance = this;
            Log.Init(Logger);

            LoadConfig();
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Patch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Log
    {
        private static ManualLogSource _logger;
        public static void Init(ManualLogSource logger) => _logger = logger;
        public static void Error(object obj) => _logger.LogError(obj);
        public static void Warn(object obj) => _logger.LogWarning(obj);
        public static void Info(object obj) => _logger.LogInfo(obj);
        public static void Debug(object obj) => _logger.LogDebug(obj);
    }
}

