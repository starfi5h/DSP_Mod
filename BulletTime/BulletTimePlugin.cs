using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Compatibility;
using HarmonyLib;
using System;
using System.Diagnostics;
using UnityEngine;

namespace BulletTime
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(NebulaCompat.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class BulletTimePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.BulletTime";
        public const string NAME = "BulletTime";
        public const string VERSION = "1.2.7";

        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<bool> EnableFastLoading;
        public static ConfigEntry<KeyboardShortcut> KeyAutosave;
        public static ConfigEntry<float> StartingSpeed;
        public static ConfigEntry<float> MinimumUPS;
        static Harmony harmony;

        private void LoadConfig()
        {
            EnableBackgroundAutosave = Config.Bind<bool>("Save", "EnableBackgroundAutosave", true, "Do auto-save in background thread\n在背景执行自动存档");
            EnableFastLoading = Config.Bind<bool>("Speed", "EnableFastLoading", true, "Increase main menu loading speed\n加快载入主选单");
            KeyAutosave = Config.Bind("Save", "KeyAutosave", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift), "Keyboard shortcut for auto-save\n自动存档的热键组合");
            StartingSpeed = Config.Bind<float>("Speed", "StartingSpeed", 100f, new ConfigDescription("Game speed when the game begin (0-100)\n游戏开始时的游戏速度 (0-100)", new AcceptableValueRange<float>(0f, 100f)));
            MinimumUPS = Config.Bind<float>("Multiplayer", "MinimumUPS", 50f, new ConfigDescription("Minimum UPS in client of multiplayer game\n联机-客户端的最小逻辑帧"));
        }

        public void Start()
        {
            Log.Init(Logger);
            harmony = new Harmony(GUID);
            LoadConfig();

            try
            {
                harmony.PatchAll(typeof(GameMain_Patch));
                harmony.PatchAll(typeof(IngameUI));
                if (EnableBackgroundAutosave.Value)
                    harmony.PatchAll(typeof(GameSave_Patch));
                if (EnableFastLoading.Value)
                    harmony.PatchAll(typeof(GameLoader_Patch));
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(NebulaCompat.GUID))
                    NebulaCompat.Init(harmony);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw e;
            }
#if DEBUG
            IngameUI.Init(); //Only enable in develop mode
#endif
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
            IngameUI.Dispose();
            NebulaCompat.Dispose();
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
