using UnityEngine;

namespace BulletTime
{
    public static class GameStateManager
    {
        public static bool Pause { get; set; }
        public static bool ManualPause { get; set; } // Manual pause state set by user (slider, hotkey)
        public static bool HotkeyPause { get; set; } // Hotkey pause mode
        public static bool AdvanceTick { get; private set; } = true;
        public static long StoredGameTick { get; set; }
        public static bool Interactable { get; set; } = true; //gametick stop, disable interaction with world
        public static bool LockFactory { get; set; } = false; // Lock all interaction on local planet
        public static float SkipRatio { get; set; }
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
                GameMain.isFullscreenPaused = true;
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
                GameMain.isFullscreenPaused = false;
            }
            IngameUI.OnPauseModeChange(value);
        }

        //Before gameTick, determine whether to pause and whether to advance tick
        public static bool PauseInThisFrame()
        {
            bool pauseThisFrame = Pause;
            AdvanceTick = GameMain.data.guideComplete && Interactable;
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
            return pauseThisFrame;
        }

        public static void SetInteractable(bool value)
        {
            //Log.Debug($"Interactable = {value}");
            Interactable = value;
            if (Interactable)
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
                if (!UIRoot.instance.uiGame.dysonEditor.active)
                {
                    GameSave_Patch.isBlocked = false;
                    UIMessageBox.CloseTopMessage();
                }
            }
        }
    }
}
