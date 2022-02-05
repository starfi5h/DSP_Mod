using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace BulletTime
{
    [BepInPlugin("com.starfi5h.plugin.BulletTime", "BulletTime", "1.0.0")]
    public class BulletTime : BaseUnityPlugin
    {
        public static GameStateManager State { get; set; }
        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<string> KeyAutosave;

        Harmony harmony;

        public void Start()
        {
            Log.Init(Logger);
            State = new GameStateManager();
            harmony = new Harmony("com.starfi5h.plugin.BulletTime");
            EnableBackgroundAutosave = Config.Bind<bool>("Save", "EnableBackgroundAutosave", false, "Do auto-save in background thread\n在背景执行自动存档");
            KeyAutosave = Config.Bind<string>("Save", "KeyAutosave", "f10", "Hotkey for auto-save\n自动存档的热键");

            try
            {                
                harmony.PatchAll(typeof(GameMain_Patch));
                harmony.PatchAll(typeof(UIStatisticsWindow_Patch));
                if (EnableBackgroundAutosave.Value)
                {
                    harmony.PatchAll(typeof(GameSave_Patch));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw e;
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
