using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
{
    class UIStatisticsWindow_Patch
    {
        private static Slider slider;
        private static Text text;

        public static void Dispose()
        {
            GameObject.Destroy(slider?.gameObject);
            slider = null;
            text = null;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnOpen))]
        private static void OnOpen()
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
                slider.value = 100f;
                slider.onValueChanged.AddListener(value => OnSliderChange(value));                
            }
        }

        private static void OnSliderChange(float value)
        {
            if (value == 0)
            {
                text.text = "pause";
            }
            else
            {
                text.text = $"{(int)value}%";
            }
            BulletTime.State.OnSliderChange(value);
        }
    }
}
