using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace Experiment
{
    [BepInPlugin("com.starfi5h.plugin.Experiment", "Experiment", "1.0.0")]
    public class Experiment : BaseUnityPlugin
    {
        Harmony harmony;

        public void Start()
        {
            Log.Init(Logger);

            harmony = new Harmony("com.starfi5h.plugin.Experiment");
            try
            {
                //harmony.PatchAll(typeof(PatchTest));
                //harmony.PatchAll(typeof(StaionUI));
                //harmony.PatchAll(typeof(TranspilerTest));
                harmony.PatchAll(typeof(UIFatalErrorTip_Patch));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            UIFatalErrorTip_Patch._OnCreate_Postfix();

            throw (new Exception("Nebula error\n\n\n"));
        }

        public void OnGUI()
        {
        }

        public void OnDestroy()
        {
            UIFatalErrorTip_Patch.Dispose();
            harmony.UnpatchSelf();
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

