using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
{
    public class GameStateManager
    {
        public bool Pause { get; set; }
        public bool AdvanceTick { get; private set; } = true;
        public bool Interactable { get; set; } = true; //gametick stop, disable interaction with world
        public long StoredGameTick { get; set; }

        private GameObject timeText;
        private GameObject infoText;
        private float sliderValue;
        private float skipRatio;
        private float timer;

        public void Dispose()
        {
            timeText = null;
            GameObject.Destroy(infoText);
            infoText = null;
        }        

        public void OnSliderChange(float value)
        {
            if (value == 0)
            {
                SetPauseMode(true);
            }
            else
            {
                if (Pause == true)
                {
                    SetPauseMode(false);
                }
                skipRatio = (100 - value) / 100f;
                timer = 0;
            }
            sliderValue = value;
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
                infoText.GetComponent<Text>().text = "Pause";
            }
            if (value)
            {
                Pause = true;
                if (StoredGameTick == 0)
                {
                    StoredGameTick = GameMain.gameTick;
                    Log.Info($"Enter pause mode, gameTick = {StoredGameTick}");
                }
                timeText?.SetActive(false);
                infoText?.SetActive(true);
            }
            else
            {
                Pause = false;
                if (StoredGameTick != 0)
                {
                    Log.Info($"Exit pause mode, duration: {GameMain.gameTick - StoredGameTick} ticks.");
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
            }
        }

        //Before gameTick, determine whether to pause and whether to advance tick
        public bool PauseInThisFrame()
        {
            bool pauseThisFrame = Pause;
            AdvanceTick = GameMain.data.guideComplete && Interactable;
            if (!Pause && sliderValue < 100)
            {
                timer += skipRatio;
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
                // Exit build mode
                GameMain.mainPlayer.controller.actionBuild.EscLogic();
                infoText.GetComponent<Text>().text = "Read-Only";
            }
            else
            {
                GameSave_Patch.isBlocked = false;
                UIMessageBox.CloseTopMessage();
                infoText.GetComponent<Text>().text = "Pause";
            }
        }
    }
}
