using Compatibility;
using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
{
    public class GameStateManager
    {
        public bool Pause { get; set; }
        public bool AdvanceTick { get; private set; } = true;
        public long StoredGameTick { get; set; }
        public bool Interactable { get; set; } = true; //gametick stop, disable interaction with world
        public bool LockFactory { get; set; } = false; // Lock all interaction on local planet
        public float SkipRatio { get; set; }

        private GameObject timeText;
        private GameObject infoText;
        private float timer;

        public void Dispose()
        {
            timeText = null;
            GameObject.Destroy(infoText);
            infoText = null;
        }        

        public void SetSpeedRatio(float value)
        {
            if (value == 0)
            {
                SetPauseMode(true);
            }
            else
            {
                if (Pause)
                    SetPauseMode(false);
                SkipRatio = 1 - value;
                timer = 0;
            }
        }

        public void SetPauseMode(bool value)
        {
            if (timeText == null)
            {
                timeText = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/time-text");
            }
            if (infoText == null && timeText != null)
            {
                infoText = GameObject.Instantiate(timeText, timeText.transform.parent);
                infoText.name = "pause info-text";
                infoText.GetComponent<Text>().text = "Pause";
                infoText.GetComponent<Text>().enabled = true;
            }
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
                timeText?.SetActive(false);
                infoText?.SetActive(true);
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
                timeText?.SetActive(true);
                infoText?.SetActive(false);
                if (GameMain.gameScenario != null)
                {
                    GameMain.gameScenario.abnormalityLogic = new AbnormalityLogic();
                    GameMain.gameScenario.abnormalityLogic.Init(GameMain.gameScenario.gameData);
                }
                GameMain.isFullscreenPaused = false;
            }
        }

        //Before gameTick, determine whether to pause and whether to advance tick
        public bool PauseInThisFrame()
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

        public void SetInteractable(bool value)
        {
            Log.Debug($"Interactable = {value}");
            Interactable = value;
            if (!Interactable)
            {
                //infoText.GetComponent<Text>().text = "Read-Only";
            }
            else
            {
                GameSave_Patch.isBlocked = false;
                UIMessageBox.CloseTopMessage();
                //infoText.GetComponent<Text>().text = "Pause";
            }
        }

        public void SetLockFactory(bool value)
        {
            Log.Debug($"LockFactory = {value}");
            LockFactory = value;
            if (LockFactory)
            {
                infoText.GetComponent<Text>().text = "Read-Only";
            }
            else
            {                
                // Close blocking message if it is not dysonEditor
                if (!UIRoot.instance.uiGame.dysonEditor.active)
                {
                    GameSave_Patch.isBlocked = false;
                    UIMessageBox.CloseTopMessage();
                }
                infoText.GetComponent<Text>().text = "Pause";
            }
        }
    }
}
