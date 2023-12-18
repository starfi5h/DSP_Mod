using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics;
using UnityEngine;

namespace BulletTime
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(NebulaCompat.APIGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.soardev.cheatenabler", BepInDependency.DependencyFlags.SoftDependency)] // Patch after CheatEnabler to avoid conflicts
    public class BulletTimePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.BulletTime";
        public const string NAME = "BulletTime";
        public const string VERSION = "1.3.0";

        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<bool> EnableFastLoading;
        public static ConfigEntry<bool> RemoveGC;
        public static ConfigEntry<bool> UIBlueprintAsync;
        public static ConfigEntry<float> StartingSpeed;
        public static ConfigEntry<float> MinimumUPS;
        public static ConfigEntry<KeyboardShortcut> KeyAutosave;
        public static ConfigEntry<KeyCode> KeyPause;
        static Harmony harmony;

        private void LoadConfig()
        {
            MinimumUPS = Config.Bind("Multiplayer", "MinimumUPS", 50f, new ConfigDescription("Minimum UPS in client of multiplayer game\n联机-客户端的最小逻辑帧"));
            EnableBackgroundAutosave = Config.Bind("Save", "EnableBackgroundAutosave", false, "Do auto-save in background thread\n在背景执行自动存档");
            EnableFastLoading = Config.Bind("Speed", "EnableFastLoading", true, "Increase main menu loading speed\n加快载入主选单");
            RemoveGC = Config.Bind("Speed", "RemoveGC", true, "Remove force garbage collection of build tools\n移除建筑工具的强制内存回收");
            UIBlueprintAsync = Config.Bind("Speed", "UIBlueprintAsync", false, "Optimize blueprint UI to reduce freezing time\n使蓝图非同步载入,减少卡顿时间");
            StartingSpeed = Config.Bind("Speed", "StartingSpeed", 100f, new ConfigDescription("Game speed when the game begin (0-100)\n游戏开始时的游戏速度 (0-100)", new AcceptableValueRange<float>(0f, 100f)));
            KeyAutosave = Config.Bind("Hotkey", "KeyAutosave", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift), "Keyboard shortcut for auto-save\n自动存档的热键组合");
            KeyPause = Config.Bind("Hotkey", "KeyPause", KeyCode.Pause, "Hotkey for toggling pause\n时停模式的热键");
        }

        public void Start()
        {
            if (GameConfig.gameVersion.Major == 0 && GameConfig.gameVersion.Minor < 10)
                throw new Exception($"BulletTime {VERSION} only support 0.10.x game version!\nPlease roll back to BulletTime 1.2.14 for 0.9.x game version");

            Log.Init(Logger);
            harmony = new Harmony(GUID);
            LoadConfig();

            try
            {
                harmony.PatchAll(typeof(GameMain_Patch));
                harmony.PatchAll(typeof(IngameUI));
                if (EnableBackgroundAutosave.Value)
                    GameSave_Patch.Enable(true);
                if (EnableFastLoading.Value)
                {
                    try
                    {
                        harmony.PatchAll(typeof(GameLoader_Patch));
                    }
                    catch
                    {
                        Log.Warn("Fast loading patch didn't success!");
                    }
                }
                if (RemoveGC.Value)
                {
                    try
                    {
                        harmony.PatchAll(typeof(BuildTool_Patch));
                    }
                    catch
                    {
                        Log.Warn("BuildTool no GC patch didn't success!");
                    }
                }
                if (UIBlueprintAsync.Value)
                {
                    try
                    {
                        harmony.PatchAll(typeof(UIBlueprint_Patch));
                    }
                    catch
                    {
                        Log.Warn("UIBlueprint async patch didn't success!");
                    }
                }

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

        public void Update()
        {
            if (Input.GetKeyDown(KeyPause.Value))
            {
                IngameUI.OnKeyPause();
            }
            if (KeyAutosave.Value.IsDown() && UIRoot.instance.uiGame.autoSave.showTime == 0)
            {
                // Initial auto save when there is no autosave in process
                UIAutoSave.lastSaveTick = 0L;
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
            IngameUI.Dispose();
            GameSave_Patch.Enable(false);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(NebulaCompat.GUID))
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
