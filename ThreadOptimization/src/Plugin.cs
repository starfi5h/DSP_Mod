using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace ThreadOptimization
{
    [BepInPlugin("com.starfi5h.plugin.ThreadOptimization", "ThreadOptimization", "0.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        Harmony harmony;
        
        public void Start()
        {
            Log.Init(Logger);

            harmony = new Harmony("com.starfi5h.plugin.ThreadOptimization");
            try
            {
                harmony.PatchAll(typeof(ThreadSystem));
                harmony.PatchAll(typeof(EnhanceMultithread));
                harmony.PatchAll(typeof(Lab_Patch));
                harmony.PatchAll(typeof(PerformanceStat_Patch));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            PerformanceStat_Patch.OnDestory();
        }
    }

    public static class Log
    {
        private static ManualLogSource _logger;
        private static int count;
        public static void Init(ManualLogSource logger) =>
            _logger = logger;
        public static void Error(object obj) =>
            _logger.LogError(obj);
        public static void Warn(object obj) =>
            _logger.LogWarning(obj);
        public static void Info(object obj) =>
            _logger.LogInfo(obj);
        public static void Debug(object obj) =>
            _logger.LogDebug(obj);

        public static void Print(int period, object obj)
        {
            if ((count++) % period == 0)
                _logger.LogDebug(obj);
        }
    }
}
