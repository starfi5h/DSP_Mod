using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BulletTime.Nebula;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(BulletTime.BulletTimePlugin.NAME)]
[assembly: AssemblyVersion(BulletTime.BulletTimePlugin.VERSION)]

namespace BulletTime
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(NebulaCompat.APIGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.soardev.cheatenabler", BepInDependency.DependencyFlags.SoftDependency)] // Patch after CheatEnabler to avoid conflicts
    public class BulletTimePlugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.BulletTime";
        public const string NAME = "BulletTime";
        public const string VERSION = "1.5.12";
        
        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<bool> EnableHotkeyAutosave;
        public static ConfigEntry<bool> EnableFastLoading;
        public static ConfigEntry<bool> RemoveGC;
        public static ConfigEntry<float> StartingSpeed;
        public static ConfigEntry<KeyboardShortcut> KeyAutosave;
        public static ConfigEntry<KeyCode> KeyPause;
        public static ConfigEntry<KeyCode> KeyStepOneFrame;
        public static ConfigEntry<int> StatusTextHeightOffset;
        public static ConfigEntry<string> StatusTextPause;
        public static ConfigEntry<bool> EnableMechaFunc;
        public static ConfigEntry<int> MaxSpeedupScale;
        public static ConfigEntry<float> MaxSimulationSpeed;
        static Harmony harmony;
        static string errorMessage = "";

        private void LoadConfig()
        {
            KeyAutosave = Config.Bind("Hotkey", "KeyAutosave", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift), "Keyboard shortcut for auto-save\n自动存档的热键组合");
            KeyPause = Config.Bind("Hotkey", "KeyPause", KeyCode.Pause, "Hotkey for toggling special pause mode\n战术暂停(世界停止+画面提示)的热键");
            KeyStepOneFrame = Config.Bind("Hotkey", "KeyStepOneFrame", KeyCode.None, "Hotkey to forward 1 frame in pause mode\n暂停模式下前进1帧的热键");
            EnableMechaFunc = Config.Bind("Pause", "EnableMechaFunc", false, "Enable mecha function in hotkey pause mode\n在热键战术暂停模式下启用机甲功能");
            EnableBackgroundAutosave = Config.Bind("Save", "EnableBackgroundAutosave", false, "Do auto-save in background thread\n在後台执行自动存档");
            EnableHotkeyAutosave = Config.Bind("Save", "EnableHotkeyAutosave", false, "Enable hotkey to trigger autosave\n允许用热键触发自动存档");
            EnableFastLoading = Config.Bind("Speed", "EnableFastLoading", true, "Increase main menu loading speed\n加快载入主选单");
            RemoveGC = Config.Bind("Speed", "RemoveGC", true, "Remove force garbage collection of build tools\n移除建筑工具的强制内存回收");
            StartingSpeed = Config.Bind("Speed", "StartingSpeed", 100f, new ConfigDescription("Game speed when the game begin (0-100)\n游戏开始时的游戏速度 (0-100)", new AcceptableValueRange<float>(0f, 100f)));
            StatusTextHeightOffset = Config.Bind("UI", "StatusTextHeightOffset", 100, "Height of Status text relative to auto save text\n状态提示相对于自动存档提示的高度");
            StatusTextPause = Config.Bind("UI", "StatusTextPause", "Bullet Time", "Status text when in pause mode\n暂停时的状态提示文字");
            MaxSpeedupScale = Config.Bind("UI", "MaxSpeedupScale", 10, "Maximum game speed multiplier for speedup button\n加速按钮的最大游戏速度倍率");
            if (MaxSpeedupScale.Value <= 0) MaxSpeedupScale.Value = 1;
            MaxSimulationSpeed = Config.Bind("UI", "MaxSimulationSpeed", 10f, "In outer space, shift-click to set the simulation speed to this value. 在外太空时,可以shift+点击快速达到此指定倍率");

            GameStateManager.EnableMechaFunc = EnableMechaFunc.Value;
        }

        private bool TestGameVersion()
        {
            if (GameConfig.gameVersion.Major == 0 && GameConfig.gameVersion.Minor < 10)
            {
                errorMessage = $"BulletTime {VERSION} only supports 0.10.33 game version!\nPlease roll back to BulletTime 1.2.14 for 0.9.27 game version";
                return false;
            }
            if (GameConfig.gameVersion < new Version(0, 10, 33))
            {
                errorMessage = $"BulletTime {VERSION} only supports 0.10.33 game version!\nPlease roll back to BulletTime 1.5.10 for 0.10.32 game version";
                return false;
            }
            return true;
        }

        public void Start()
        {
            Log.Init(Logger);
            harmony = new Harmony(GUID);
            LoadConfig();

            try
            {
                if (!TestGameVersion())
                {
                    // Show error message at the game start and disable the plugin
                    enabled = false;
                    Log.Error(errorMessage);
                    harmony.PatchAll(typeof(BulletTimePlugin));                    
                    return;
                }

                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(NebulaCompat.GUID))
                    NebulaCompat.Init(harmony);

                harmony.PatchAll(typeof(GameMain_Patch));
                harmony.PatchAll(typeof(IngameUI));
                if (NebulaCompat.NebulaIsInstalled)
                {
                    harmony.PatchAll(typeof(GameSave_Patch));
                    GameSave_Patch.Enable(true);
                }
                else if (EnableBackgroundAutosave.Value)
                    GameSave_Patch.Enable(true);

                if (EnableFastLoading.Value)
                {
                    TryPatch(harmony, typeof(GameLoader_Patch), "Fast loading patch didn't success!");
                }
                if (RemoveGC.Value)
                {
                    TryPatch(harmony, typeof(BuildTool_Patch), "BuildTool no GC patch didn't success!");
                }
                TryPatch(harmony, typeof(SimulateSpeed_Patch), "Simulate Tool shift-click fuction disable!");
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

        private void TryPatch(Harmony harmony, Type type, string failMessage = "")
        {
            try
            {
                harmony.PatchAll(type);
            }
            catch (Exception ex)
            {
                Log.Warn(failMessage);
                Log.Warn(ex);
            }
        }


        public void Update()
        {
            if (Input.GetKeyDown(KeyPause.Value))
            {
                IngameUI.OnKeyPause();
            }
            if (Input.GetKeyDown(KeyStepOneFrame.Value))
            {
                IngameUI.OnKeyStepOneFrame();
            }
            if (EnableHotkeyAutosave.Value && KeyAutosave.Value.IsDown() && UIRoot.instance.uiGame.autoSave.showTime == 0)
            {
                // Initial auto save when there is no autosave in process
                UIAutoSave.lastSaveTick = 0L;
                Log.Debug("Trigger auto save by hotkey");
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        static void ShowMessageOnBegin()
        {
            if (string.IsNullOrEmpty(errorMessage)) return;
            UIMessageBox.Show("BulletTime mod version mismatch", errorMessage, "OK", 3, null);
            errorMessage = "";
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
