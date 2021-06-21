using System;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ResetSaveData
{
    [BepInPlugin("com.starfi5h.plugin.ResetSaveData", "ResetSaveData", "0.1.0")]
    public class ResetSaveData : BaseUnityPlugin
    {
        Harmony harmony;
        internal void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.ResetSaveData");
            Log.Init(Logger);
            TryPatch(typeof(DeleteShell));
        }

        void TryPatch(Type type)
        {
            try
            {
                harmony.PatchAll(type);
            }
            catch (Exception e)
            {
                Logger.LogError($"Patch {type.Name} error");
                Logger.LogError(e);
            }
        }

        internal void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Log
    {
        private static ManualLogSource _logger;
        public static void Init(ManualLogSource logger) =>
            _logger = logger;
        public static void LogError(object obj) =>
            _logger.LogError(obj);
        public static void LogWarning(object obj) =>
            _logger.LogWarning(obj);
        public static void LogInfo(object obj) =>
            _logger.LogInfo(obj);
        public static void LogDebug(object obj) =>
            _logger.LogDebug(obj);
    }
}
