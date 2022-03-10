using Compatibility;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace BulletTime
{
    class IngameUI
    {
        private static Slider slider;
        private static Text text;
        private static Text stateMessage;

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
        }

        private static void OnSliderChange(float value)
        {
            if (value == 0)
            {
                text.text = "pause";
                if (!BulletTimePlugin.State.Pause && NebulaCompat.IsMultiplayerActive)
                    NebulaCompat.SendPacket(PauseEvent.Pause);
            }
            else
            {
                text.text = $"{(int)value}%";
                if (BulletTimePlugin.State.Pause && NebulaCompat.IsMultiplayerActive)
                {
                    NebulaCompat.SendPacket(PauseEvent.Resume);
                    ShowStatus("");
                }
            }
            BulletTimePlugin.State.SetSpeedRatio(value/100f);
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIFpsStat), nameof(UIFpsStat.FixedUpdate))]
        private static void UIFpsStat_Postfix(UIFpsStat __instance, ref bool __state)
        {
            __state = ((__instance.watch != null) && (__instance.frame_u - __instance.lastframe_u) >= 15);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFpsStat), nameof(UIFpsStat.FixedUpdate))]
        private static void UIFpsStat_Postfix(UIFpsStat __instance, bool __state)
        {
            if (__state)
            {
                if (__instance.fpsText.Capacity < 17)
                {
                    __instance.fpsText = new StringBuilder("000 | 00 -00/%", 17);
                    GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/FPS Stats/fps-ups-text");
                    if (go != null)
                        go.transform.localPosition += new Vector3(20, 0, 0);
                }
                if (FPSController.currentUPS >= 99.0)
                {
                    StringBuilderUtility.WritePositiveFloat(__instance.fpsText, 6, 2, (float)FPSController.currentUPS, 0, ' ');
                    __instance.fpsTextChanged = true;
                }
                if (__instance.fpsTextChanged)
                {
                    // assume normal ups is 60/s, realSpeed = realUps / 60f
                    float realSpeed = ((float)FPSController.currentUPS * (1f - BulletTimePlugin.State.SkipRatio)) / 60f;
                    StringBuilderUtility.WritePositiveFloat(__instance.fpsText, 10, 3, realSpeed * 100, 0, '-');
                }
            }
        }
    }
}
