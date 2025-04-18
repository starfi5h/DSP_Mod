﻿using BulletTime.Nebula;
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
