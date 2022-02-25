using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Compatibility;
using HarmonyLib;
using System;
using System.Diagnostics;

namespace BulletTime
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("dsp.nebula-multiplayer-api", BepInDependency.DependencyFlags.SoftDependency)]
    public class BulletTimePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.BulletTime";
        public const string NAME = "BulletTime";
        public const string VERSION = "1.2.0";

        public static GameStateManager State { get; set; }
        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<string> KeyAutosave;
        public static ConfigEntry<float> StartingSpeed;
        public static Harmony harmony;

        public void Start()
        {
            Log.Init(Logger);
            State = new GameStateManager();
            harmony = new Harmony("com.starfi5h.plugin.BulletTime");
            EnableBackgroundAutosave = Config.Bind<bool>("Save", "EnableBackgroundAutosave", false, "Do auto-save in background thread\n在背景执行自动存档");
            KeyAutosave = Config.Bind<string>("Save", "KeyAutosave", "f10", "Hotkey for auto-save\n自动存档的热键");
            StartingSpeed = Config.Bind<float>("Speed", "StartingSpeed", 100f, new ConfigDescription("Game speed when the game begin (0-100)\n游戏开始时的游戏速度 (0-100)", new AcceptableValueRange<float>(0f, 100f)));

            NebulaCompat.Enable = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dsp.nebula-multiplayer-api");

            try
            {
                harmony.PatchAll(typeof(GameMain_Patch));
                if (EnableBackgroundAutosave.Value)
                    harmony.PatchAll(typeof(GameSave_Patch));
                if (NebulaCompat.Enable)
                    NebulaCompat.Init(harmony);
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
            harmony = null;
            State.Dispose();
            IngameUI.Dispose();
            if (NebulaCompat.Enable)
            {
                NebulaCompat.Dispose();
            }
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

        [Conditional("DEBUG")]
        public static void Dev(object obj) =>
            _logger.LogDebug(obj);

        public static void Print(int period, object obj)
        {
            if ((count++) % period == 0)
                _logger.LogDebug(obj);
        }
    }
}
