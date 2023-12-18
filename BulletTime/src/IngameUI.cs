using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
{
    class IngameUI
    {
        private static Slider slider;
        private static Text text;
        private static Text stateMessage;
        private static GameObject timeText;
        private static GameObject infoText;

        public static void Dispose()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                GameObject.Destroy(slider.gameObject);
            }
            slider = null;
            text = null;
            if (stateMessage != null)
            {
                GameObject.Destroy(stateMessage.transform.GetParent().gameObject);
                stateMessage = null;
            }
            timeText = null;
            GameObject.Destroy(infoText);
            infoText = null;
        }

        public static void Init()
        {
            if (slider == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-2/audio/Slider");
                Transform cpuPanel = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Statistics Window/performance-bg/cpu-panel").transform;
                go = GameObject.Instantiate(go, cpuPanel);
                go.name = "RealTickRatioSlider";
                go.transform.localPosition = cpuPanel.Find("title-text").localPosition + new Vector3(-120, 1, 0);
                slider = go.GetComponent<Slider>();
                text = go.GetComponentInChildren<Text>();                
                slider.value = BulletTimePlugin.StartingSpeed.Value;
                slider.onValueChanged.AddListener(value => OnSliderChange(value));
                OnSliderChange(slider.value);
            }
            // Only host can have control slider
            slider.gameObject.SetActive(!NebulaCompat.IsClient);
            GameStateManager.ManualPause = false;
        }

        public static void OnPauseModeChange(bool pause)
        {            
            if (timeText == null)
            {
                timeText = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/time-text");
            }            
            if (infoText == null && timeText != null)
            {
                infoText = GameObject.Instantiate(timeText, timeText.transform.parent);
                infoText.name = "pause info-text";
                infoText.GetComponent<Text>().text = "Pause".Translate();
                infoText.GetComponent<Text>().enabled = true;
            }
            if (timeText != null && infoText != null)
            {
                timeText.SetActive(!pause);
                infoText.SetActive(pause);
            }            
            if (text != null)
                text.text = pause ? "Pause".Translate() : $"{(int)slider.value}%";
        }

        private static void OnSliderChange(float value)
        {
            if (value == 0)
            {                
                GameStateManager.ManualPause = true;
                text.text = "pause".Translate();
                if (!GameStateManager.Pause && NebulaCompat.IsMultiplayerActive)
                    NebulaCompat.SendPacket(PauseEvent.Pause);
            }
            else
            {
                GameStateManager.ManualPause = false;
                text.text = $"{(int)value}%";
                if (GameStateManager.Pause && NebulaCompat.IsMultiplayerActive)
                {
                    NebulaCompat.SendPacket(PauseEvent.Resume);
                    ShowStatus("");
                }
            }
            GameStateManager.SetSpeedRatio(value/100f);
        }

        public static void OnKeyPause()
        {
            if (NebulaCompat.IsClient) //暫時不允許客戶端啟用時停
                return;

            if (GameStateManager.Pause == false) //不在時停狀態時,切換至時停
            {
                GameStateManager.ManualPause = true; //進入手動時停狀態
                if (NebulaCompat.IsMultiplayerActive)
                    NebulaCompat.SendPacket(PauseEvent.Pause);
                GameStateManager.SetPauseMode(true);
            }
            else if (GameStateManager.Interactable) //如果不是在自動存檔中, 回歸滑條設定值
            {
                OnSliderChange(slider.value);
            }
        }

        public static void ShowStatus(string message)
        {
            if (stateMessage == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Auto Save/content/tip-panel");
                GameObject statePanel = GameObject.Instantiate(go, go.transform.parent.parent);
                statePanel.transform.localPosition = new Vector3(0, 300, 0);
                GameObject.Destroy(statePanel.transform.Find("bg").gameObject);
                GameObject.Destroy(statePanel.transform.Find("icon").gameObject);
                GameObject.Destroy(statePanel.transform.Find("glow-1").gameObject);
                GameObject.Destroy(statePanel.transform.Find("achiev-ban-text").gameObject);
                stateMessage = statePanel.transform.Find("text").GetComponent<Text>();
            }
            stateMessage.transform.GetParent().gameObject.SetActive(message != "");
            stateMessage.text = message;
        }
    }
}
