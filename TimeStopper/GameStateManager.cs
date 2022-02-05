using Unity;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStopper
{
    public static class GameStateManager
    {
        public static bool Pause { get; set; }
        public static bool Interactable { get; set; } = true;

        private static long storedGameTick;

        //private static Text timeText;
        private static GameObject timeText;
        private static GameObject infoText;

        public static void Dispose()
        {
            timeText = null;
            GameObject.Destroy(infoText);
            infoText = null;
        }

        public static void SetPauseMode(bool value, ref long gameTick)
        {
            if (timeText == null)
            {
                timeText = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/time-text");
            }
            if (infoText == null)
            {
                infoText = GameObject.Instantiate(timeText, timeText.transform.parent);
            }
            if (value)
            {
                Pause = true;
                storedGameTick = gameTick;                
                timeText.SetActive(false);
                infoText.SetActive(true);
                infoText.GetComponent<Text>().text = "Pause";
            }
            else
            {
                Pause = false;
                gameTick = storedGameTick;                
                timeText.SetActive(true);
                infoText.SetActive(false);
                GameMain.gameScenario.abnormalityLogic = new AbnormalityLogic();
                GameMain.gameScenario.abnormalityLogic.Init(GameMain.gameScenario.gameData);
            }
        }
    }
}
