using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AlterTickrate
{
    class UIcontrol
    {
        static GameObject activeButton;
        static Text activeButtonText;
        static Image activeButtonColor;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init()
        {
            if (activeButton == null)
            {
                try
                {
                    UIButton dataActiveButton = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuActiveButton;
                    activeButton = GameObject.Instantiate(dataActiveButton.gameObject, dataActiveButton.transform.parent);
                    activeButton.name = "AlterTickrate - ActiveButton";
                    activeButton.transform.localPosition += new Vector3(120, 0);

                    GameObject.Destroy(activeButton.GetComponent<UIButton>());
                    Button button = activeButton.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(new UnityAction(OnActiveButtonClick));

                    Transform transform = activeButton.transform.Find("button-text");
                    GameObject.Destroy(transform.GetComponent<Localizer>());
                    activeButtonText = transform.GetComponent<Text>();
                    activeButtonColor = activeButton.GetComponent<Image>();

                    activeButtonText.text = "AlterTickrate";
                    SetButtonState();

                    Log.Debug("UI component init");
                }
                catch
                {
                    Log.Warn("UI component initial fail!");
                }
            }
        }

        public static void OnDestory()
        {
            if (activeButton != null)
            {
                GameObject.Destroy(activeButton);
                activeButtonText = null;
                activeButtonColor = null;
            }
        }

        public static void OnActiveButtonClick()
        {
            Plugin.plugin.SetEnable(!Plugin.Enable);
            SetButtonState();
        }

        static void SetButtonState()
        {
            if (Plugin.Enable)
            {
                activeButtonText.text = "AlterTick - ON";
                activeButtonColor.color = new Color(0.3804f, 0.8431f, 1f, 0.7216f); // blue
            }
            else
            {
                activeButtonText.text = "AlterTick - OFF";
                activeButtonColor.color = new Color(0.6196f, 0.6196f, 0.6196f, 0.7216f); // grey
            }
        }
    }
}
