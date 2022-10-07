using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LossyCompression
{
    class UIcontrol
    {
        static GameObject compressButton;
        static Text compressButtonText;
        static Image compressButtonColor;
        static Color orange = new Color(1f, 0.5961f, 0.3804f, 0.7216f);
        static Color grey = new Color(0.6196f, 0.6196f, 0.6196f, 0.7216f);

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init()
        {
            if (compressButton == null)
            {
                try
                {
                    UIButton dataActiveButton = UIRoot.instance.uiGame.statWindow.performancePanelUI.dataActiveButton;
                    compressButton = GameObject.Instantiate(dataActiveButton.gameObject, dataActiveButton.transform.parent);
                    compressButton.name = "LossyCompression - ActiveButton";
                    compressButton.transform.localPosition += new Vector3(120, 0);

                    GameObject.Destroy(compressButton.GetComponent<UIButton>());
                    Button button = compressButton.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(new UnityAction(OnCompressActiveButtonClick));

                    Transform transform = compressButton.transform.Find("button-text");
                    GameObject.Destroy(transform.GetComponent<Localizer>());
                    compressButtonText = transform.GetComponent<Text>();
                    compressButtonColor = compressButton.GetComponent<Image>();

                    compressButtonText.text = "Compress - ON";
                    compressButtonColor.color = orange;

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
            if (compressButton != null)
            {
                GameObject.Destroy(compressButton);
                compressButtonText = null;
                compressButtonColor = null;
            }
        }

        public static void OnCompressActiveButtonClick()
        {
            Plugin.Enable = !Plugin.Enable;

            if (Plugin.Enable)
            {
                compressButtonText.text = "Compress - ON";
                compressButtonColor.color = orange;
            }
            else
            {
                compressButtonText.text = "Compress - OFF";
                compressButtonColor.color = grey;
            }
        }
    }
}
