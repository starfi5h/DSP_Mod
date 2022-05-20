using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Diagnostics;

namespace NebulaCompatibilityAssist
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("dsp.nebula-multiplayer")]
    [BepInDependency("dsp.nebula-multiplayer-api")]
    public class Plugin : BaseUnityPlugin
    {
        Harmony harmony;

        public void Awake()
        {
            Log.LogSource = Logger;
            harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Patches.NC_Patch.Init(harmony);
        }

        [Conditional("DEBUG")]
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Log
    {
        public static ManualLogSource LogSource;
        public static void Error(object obj) => LogSource.LogError(obj);
        public static void Warn(object obj) => LogSource.LogWarning(obj);
        public static void Info(object obj) => LogSource.LogInfo(obj);
        public static void Debug(object obj) => LogSource.LogDebug(obj);

        [Conditional("DEBUG")]
        public static void Dev(object obj) => LogSource.LogDebug(obj);
    }
}
