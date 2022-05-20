using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NebulaCompatibilityAssist.Packets;
using System.Diagnostics;

namespace NebulaCompatibilityAssist
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("dsp.nebula-multiplayer")]
    [BepInDependency("dsp.nebula-multiplayer-api")]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony Harmony { get; private set; }

        public void Awake()
        {
            Log.LogSource = Logger;
            Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Patches.NC_Patch.OnAwake();
        }

        [Conditional("DEBUG")]
        public void OnDestroy()
        {
            Harmony.UnpatchSelf();
            Harmony = null;
            NC_ModSaveRequest.OnReceive = null;
            NC_ModSaveData.OnReceive = null;
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
