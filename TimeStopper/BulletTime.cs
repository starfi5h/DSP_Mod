using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace BulletTime
{
    [BepInPlugin("com.starfi5h.plugin.BulletTion", "BulletTion", "1.0.0")]
    public class BulletTime : BaseUnityPlugin
    {
        public static GameStateManager State { get; set; }
        Harmony harmony;

        public void Start()
        {
            Log.Init(Logger);
            State = new GameStateManager();

            harmony = new Harmony("com.starfi5h.plugin.BulletTion");
            try
            {
                harmony.PatchAll(typeof(GaemSave_Patch));
                harmony.PatchAll(typeof(GameMain_Patch));
                harmony.PatchAll(typeof(UIStatisticsWindow_Patch));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        int count;

        public void OnUpdate()
        {
            if (count++ % 120 == 0)
            {
                GaemSave_Patch.ShowStatus(count % 240 == 0 ? "測試玩家" + " joining the game, please wait" : "");
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            State.Dispose();
            UIStatisticsWindow_Patch.Dispose();
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
