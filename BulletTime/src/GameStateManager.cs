using BulletTime.Nebula;
using UnityEngine;

namespace BulletTime
{
    public static class GameStateManager
    {
        public static bool Pause { get; private set; }
        public static bool ManualPause { get; set; } // Manual pause state set by user (slider, hotkey)
        public static bool HotkeyPause { get; set; } // Hotkey pause mode
        public static bool StepOneFrame { get; set; } // Forward 1 frame by hotkey
        public static bool EnableMechaFunc { get; set; } // Is mecha available to move in hotkey pause mode?
        public static bool IsSaving { get; set; } // Is during saving?

        public static bool AdvanceTick { get; private set; } = true;
        public static long StoredGameTick { get; private set; }
        public static bool Interactable { get; private set; } = true; //gametick stop, disable interaction with world
        public static bool LockFactory { get; private set; } = false; // Lock all interaction on local planet
        public static float SkipRatio { get; private set; }
        public static double LastFrameRate { get; set; } = 60; // 帧速率档的记忆速度（F/s，运行期更新）
        public static bool FrameRateModeActive { get; set; } // 帧速率档是否为最后激活的速度模式（暂停恢复时用于精确回归）
        public static float LastSliderSpeed { get; set; } = 100; // 滑块减速模式的记忆速度（百分比，运行期更新）
        public static Vector3 PlayerPosition { get; set; } // Lock player position in hotkey pause

        private static float timer;
 

        public static void SetSpeedRatio(float value)
        {
            if (value == 0)
            {
                SetPauseMode(true);
                SkipRatio = 1f;
            }
            else
            {
                if (Pause)
                    SetPauseMode(false);
                SkipRatio = 1 - value;
                timer = 0;
            }
        }

        // 统一入口：帧速率档。F <= 60 走丢帧轨道（并停用加速轨道），F > 60 走加速轨道（并清除丢帧）
        public static void ApplyFrameRate(double fps)
        {
            if (fps < 0.06) fps = 0.06; // 0.001x
            if (fps > 6000.0) fps = 6000.0; // 100x
            LastFrameRate = fps;
            FrameRateModeActive = true;
            if (BulletTimePlugin.FrameRate != null) BulletTimePlugin.FrameRate.Value = (float)fps; // 回写配置，跨存档保持
            if (fps <= 60.0)
            {
                if (FPSController.instance != null && FPSController.instance.fixUPS > 0.0)
                    FPSController.SetFixUPS(0.0); // 停用加速轨道，使有效速度等于 fps
                SetSpeedRatio((float)(fps / 60.0));
            }
            else
            {
                if (Pause)
                    SetPauseMode(false);
                SkipRatio = 0f; // 清除丢帧比例，使有效速度等于 fps
                timer = 0;
                FPSController.SetFixUPS(fps);
            }
        }

        // 退出游戏时重置帧速率档状态
        public static void ResetFrameRateState()
        {
            FrameRateModeActive = false;
            LastFrameRate = BulletTimePlugin.FrameRate != null ? BulletTimePlugin.FrameRate.Value : 60f;
            LastSliderSpeed = BulletTimePlugin.SliderSpeed != null ? BulletTimePlugin.SliderSpeed.Value : 100f;
        }

        public static void SetPauseMode(bool value)
        {
            if (value)
            {
                Pause = true;
                if (StoredGameTick == 0)
                {
                    StoredGameTick = GameMain.gameTick;
                    Log.Debug($"Enter pause mode, gameTick = {StoredGameTick}");
                    if (NebulaCompat.IsClient)
                        FPSController.SetFixUPS(0);
                }
            }
            else
            {
                Pause = false;
                if (StoredGameTick != 0)
                {
                    Log.Debug($"Exit pause mode, duration: {GameMain.gameTick - StoredGameTick} ticks.");
                    GameMain.gameTick = StoredGameTick;
                    StoredGameTick = 0;
                }
                GameMain.isFullscreenPaused = false; //一併解除聯機的鎖定 (戴森球,研究頁面)
                SetLockFactory(false); //一併解除工廠的鎖定
            }
            IngameUI.OnPauseModeChange(value);
        }

        //Before gameTick, determine whether to pause and whether to advance tick
        public static bool PauseInThisFrame()
        {
            bool pauseThisFrame = Pause;
            AdvanceTick = GameMain.data.guideComplete && Interactable;
            if (HotkeyPause && !EnableMechaFunc)
            {
                AdvanceTick = false;
            }
            if (!Pause)
            {
                timer += SkipRatio;
                if (timer >= 1f)
                {
                    timer -= 1f;
                    pauseThisFrame = true;
                    AdvanceTick = false;
                }
            }
            else
            {
                if (StepOneFrame && Interactable)
                {
                    if (StoredGameTick != 0)
                    {
                        StoredGameTick++; // The real gametick advance too
                        IngameUI.OnStoredGameTickChange();
                    }
                    AdvanceTick = GameMain.data.guideComplete && Interactable;
                    pauseThisFrame = false;
                    StepOneFrame = false;
                    //Log.Debug("StepOneFrame. gameTick = " + StoredGameTick);
                }
            }
            return pauseThisFrame;
        }

        public static void SetInteractable(bool value)
        {
            //Log.Debug($"Interactable = {value}");
            Interactable = value;
            // Close blocking message
            if (Interactable && GameSave_Patch.isBlocked)
            { 
                GameSave_Patch.isBlocked = false;
                UIMessageBox.CloseTopMessage();
            }
        }

        public static void SetLockFactory(bool value)
        {
            //Log.Debug($"LockFactory = {value}");
            LockFactory = value;
            if (!LockFactory)
            {                
                // Close blocking message if it is not dysonEditor
                if (!UIRoot.instance.uiGame.dysonEditor.active && GameSave_Patch.isBlocked)
                {
                    GameSave_Patch.isBlocked = false;
                    UIMessageBox.CloseTopMessage();
                }
            }
        }

        public static void SetSyncingLock(bool isLock)
        {
            SetInteractable(!isLock);
            SetLockFactory(isLock);
            Log.Debug("SetSyncingLock: " + isLock);
        }
    }
}
