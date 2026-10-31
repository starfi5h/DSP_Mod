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
        public const string VERSION = "1.6.0";
        
        public static ConfigEntry<bool> EnableBackgroundAutosave;
        public static ConfigEntry<bool> EnableHotkeyAutosave;
        public static ConfigEntry<bool> EnableFastLoading;
        public static ConfigEntry<bool> RemoveGC;
        public static ConfigEntry<float> StartingSpeed;
        public static ConfigEntry<KeyboardShortcut> KeyAutosave;
        public static ConfigEntry<KeyboardShortcut> KeyImmediateSave;
        public static ConfigEntry<KeyboardShortcut> KeySpeedUp;
        public static ConfigEntry<KeyboardShortcut> KeySpeedDown;
        public static ConfigEntry<KeyCode> KeyPause;
        public static ConfigEntry<KeyCode> KeyStepOneFrame;
        public static ConfigEntry<KeyboardShortcut> KeyFullSpeed;
        public static ConfigEntry<KeyboardShortcut> KeySliderSpeed;
        public static ConfigEntry<KeyboardShortcut> KeyFrameRate;

        public static ConfigEntry<float> FrameRate;
        public static ConfigEntry<float> SliderSpeed;

        public static ConfigEntry<int> StatusTextHeightOffset;
        public static ConfigEntry<string> StatusTextPause;
        public static ConfigEntry<bool> EnableMechaFunc;
        public static ConfigEntry<int> MaxSpeedupScale;
        public static ConfigEntry<float> MaxSimulationSpeed;
        static Harmony harmony;
        static string errorMessage = "";
        public static BulletTimePlugin Instance { get; private set; }
        public static void SaveConfig() { if (Instance != null) Instance.Config.Save(); }

        private void LoadConfig()
        {
            KeyAutosave = Config.Bind("Hotkey", "KeyAutosave", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift), "Keyboard shortcut for auto-save\n自动存档的热键组合");
            KeyImmediateSave = Config.Bind("Hotkey", "KeyImmediateSave", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl, KeyCode.LeftAlt), "Keyboard shortcut for immediate save (last exit)\n立即存档的热键组合");
            KeySpeedUp = Config.Bind("Hotkey", "KeySpeedUp", new KeyboardShortcut(), "Keyboard shortcut for speed up\n加速的热键");
            KeySpeedDown = Config.Bind("Hotkey", "KeySpeedDown", new KeyboardShortcut(), "Keyboard shortcut for speed down\n减速的热键");
            KeyPause = Config.Bind("Hotkey", "KeyPause", KeyCode.Pause, "Hotkey for toggling special pause mode\n战术暂停(世界停止+画面提示)的热键");
            KeyStepOneFrame = Config.Bind("Hotkey", "KeyStepOneFrame", KeyCode.None, "Hotkey to forward 1 frame in pause mode\n暂停模式下前进1帧的热键");
            KeyFullSpeed = Config.Bind("Hotkey", "KeyFullSpeed", new KeyboardShortcut(KeyCode.Home, KeyCode.LeftControl), "Hotkey for the 100% speed preset (standard 1x speed). Memorizes the current slider value for Ctrl+End\n切换到 100% 速度档（标准 1 倍速）的热键，同时记忆当前滑块值供 Ctrl+End 恢复");
            KeySliderSpeed = Config.Bind("Hotkey", "KeySliderSpeed", new KeyboardShortcut(KeyCode.End, KeyCode.LeftControl), "Hotkey for the slider speed preset (restore the memorized slider value)\n切换到滑块减速模式的热键（恢复记忆的滑块值）");
            KeyFrameRate = Config.Bind("Hotkey", "KeyFrameRate", new KeyboardShortcut(KeyCode.Insert, KeyCode.LeftControl), "Hotkey for the frame-rate preset (restore the last frame-rate setting)\n切换到帧速率档的热键（恢复上次帧速率设定值）");

            EnableMechaFunc = Config.Bind("Pause", "EnableMechaFunc", false, "Enable mecha function in hotkey pause mode\n在热键战术暂停模式下启用机甲功能");
            EnableBackgroundAutosave = Config.Bind("Save", "EnableBackgroundAutosave", false, "Do auto-save in background thread\n在後台执行自动存档");
            EnableHotkeyAutosave = Config.Bind("Save", "EnableHotkeyAutosave", false, "Enable hotkey to trigger autosave\n允许用热键触发自动存档");
            EnableFastLoading = Config.Bind("Speed", "EnableFastLoading", true, "Increase main menu loading speed\n加快载入主选单");
            RemoveGC = Config.Bind("Speed", "RemoveGC", true, "Remove force garbage collection of build tools\n移除建筑工具的强制内存回收");
            StartingSpeed = Config.Bind("Speed", "StartingSpeed", 100f, new ConfigDescription("Game speed when the game begin (0-100)\n游戏开始时的游戏速度 (0-100)", new AcceptableValueRange<float>(0f, 100f)));
            FrameRate = Config.Bind("Speed", "FrameRate", 60f, new ConfigDescription("Actual speed of the frame-rate preset, in frames per second (0.06-6000 = 0.001x-100x). Adjusted in game, saved automatically and kept across saves\n帧速率档的实际设定速率，单位为帧每秒（0.06-6000，对应 0.001x-100x）。游戏内调整后自动保存，跨存档保持", new AcceptableValueRange<float>(0.06f, 6000f)));
            SliderSpeed = Config.Bind("Speed", "SliderSpeed", 100f, new ConfigDescription("Slider value of the slider preset in percent (1-100). Memorized automatically by Ctrl+Home, saved and kept across saves\n滑块减速模式的速度百分比（1-100）。按 Ctrl+Home 时自动记忆当前滑块值，自动保存，跨存档保持", new AcceptableValueRange<float>(1f, 100f)));
            StatusTextHeightOffset = Config.Bind("UI", "StatusTextHeightOffset", 100, "Height of Status text relative to auto save text\n状态提示相对于自动存档提示的高度");
            StatusTextPause = Config.Bind("UI", "StatusTextPause", "Bullet Time", "Status text when in pause mode\n暂停时的状态提示文字");
            MaxSpeedupScale = Config.Bind("UI", "MaxSpeedupScale", 10, "Maximum game speed multiplier for speedup button\n加速按钮的最大游戏速度倍率");
            if (MaxSpeedupScale.Value <= 0) MaxSpeedupScale.Value = 1;
            MaxSimulationSpeed = Config.Bind("UI", "MaxSimulationSpeed", 10f, "In outer space, shift-click to set the simulation speed to this value. 在外太空时,可以shift+点击快速达到此指定倍率");

            GameStateManager.EnableMechaFunc = EnableMechaFunc.Value;
            GameStateManager.LastFrameRate = FrameRate.Value;
            GameStateManager.LastSliderSpeed = SliderSpeed.Value;
            // 配置管理器/cfg 手改时，即时同步到对应档位记忆值
            FrameRate.SettingChanged += (s, e) => GameStateManager.LastFrameRate = FrameRate.Value;
            SliderSpeed.SettingChanged += (s, e) => GameStateManager.LastSliderSpeed = SliderSpeed.Value;
        }

        private bool TestGameVersion()
        {
            if (GameConfig.gameVersion.Major == 0 && GameConfig.gameVersion.Minor < 10)
            {
                errorMessage = $"BulletTime {VERSION} only supports 0.10.33+ game version!\nPlease roll back to BulletTime 1.2.14 for 0.9.27 game version";
                return false;
            }
            if (GameConfig.gameVersion < new Version(0, 10, 33))
            {
                errorMessage = $"BulletTime {VERSION} only supports 0.10.33+ game version!\nPlease roll back to BulletTime 1.5.10 for 0.10.32 game version";
                return false;
            }
            return true;
        }

        public void Awake()
        {
            Instance = this;
            Log.Init(Logger);
            harmony = new Harmony(GUID);
            LoadConfig();
        }

        public void Start()
        {
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
            if (KeySpeedUp.Value.IsDown())
            {
                Log.Debug("KeySpeedUp");
                IngameUI.OnKeySpeedUp();
            }
            if (KeySpeedDown.Value.IsDown())
            {
                Log.Debug("KeySpeedDown");
                IngameUI.OnKeySpeedDown();
            }
            if (Input.GetKeyDown(KeyPause.Value))
            {
                Log.Debug("KeyPause");
                IngameUI.OnKeyPause();
            }
            if (Input.GetKeyDown(KeyStepOneFrame.Value))
            {
                Log.Debug("KeyStepOneFrame");
                IngameUI.OnKeyStepOneFrame();
            }
            if (KeyFullSpeed.Value.IsDown())
            {
                Log.Debug("KeyFullSpeed");
                IngameUI.OnKeyFullSpeed();
            }
            if (KeySliderSpeed.Value.IsDown())
            {
                Log.Debug("KeySliderSpeed");
                IngameUI.OnKeySliderSpeed();
            }
            if (KeyFrameRate.Value.IsDown())
            {
                Log.Debug("KeyFrameRate");
                IngameUI.OnKeyFrameRate();
            }


            if (EnableHotkeyAutosave.Value && KeyAutosave.Value.IsDown() && UIRoot.instance.uiGame.autoSave.showTime == 0)
            {
                // Initial auto save when there is no autosave in process
                UIAutoSave.lastSaveTick = 0L;
                Log.Info("Trigger auto save by hotkey");
            }
            if (KeyImmediateSave.Value.IsDown())
            {
                Log.Info("Trigger immediate auto save by hotkey");
                string saveText = GameSave.AutoSave() ? "保存成功".Translate() : "保存失败".Translate();
                Log.Info("Call GameSave.AutoSave(). Result: " + saveText);
            }

        }

        public void OnDestroy()
        {
            SaveConfig();
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
