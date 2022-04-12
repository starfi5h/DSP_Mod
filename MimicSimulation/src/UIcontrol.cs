using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MimicSimulation
{
    class UIcontrol
    {
        static int minFactoryCount = 1;
        static int factoryCount = -1;

        static GameObject group;
        static Slider slider;
        static InputField input;
        //static Text text_MaxCount;
        static Text text_factory;
        static Text text_Cycle;
        static Toggle toggle_local;
        static bool eventLock;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init()
        {
            if (group == null)
            {
                Slider slider0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.slider0;
                InputField input0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.input0;
                Text text0 = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuValueText1;
                GameObject checkBox = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-1/fullscreen");
                GameObject panelObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Statistics Window/performance-bg/cpu-panel");

                group = new GameObject("MimicSimulation_Group");
                group.transform.SetParent(panelObj.transform);
                group.transform.localPosition = new Vector3(200, 105);
                group.transform.localScale = Vector3.one;

                /*
                GameObject tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_MaxCount";
                tmp.transform.localPosition = new Vector3(-5, 32);
                text_MaxCount = tmp.GetComponent<Text>();
                text_MaxCount.text = "MaxCount";
                */

                GameObject tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_factory";
                tmp.transform.localPosition = new Vector3(-50, 8);
                text_factory = tmp.GetComponent<Text>();
                text_factory.text = "factory";
                tmp.AddComponent<UItooltip>().Text = "Maximum number of factories allow to active and run per tick.";

                tmp = GameObject.Instantiate(input0.gameObject, group.transform);
                tmp.name = "input_MaxFactoryCount";
                tmp.transform.localPosition = new Vector3(155, 0);
                input = tmp.GetComponent<InputField>();
                input.characterValidation = InputField.CharacterValidation.Integer;
                input.contentType = InputField.ContentType.IntegerNumber;
                input.onEndEdit.AddListener(new UnityAction<string>(OnInputValueEnd));
                UItooltip tip1 = tmp.AddComponent<UItooltip>();
                tip1.Title = "MaxfactoryCount";
                tip1.Text = "Maximum number of factories allow to active and run per tick.";

                tmp = GameObject.Instantiate(slider0.gameObject, group.transform);
                tmp.name = "slider_factoryCount";
                tmp.transform.localPosition = new Vector3(55, -20, 0);
                slider = tmp.GetComponent<Slider>();
                slider.minValue = 1;
                slider.wholeNumbers = true;
                slider.onValueChanged.AddListener(new UnityAction<float>(OnSliderChange));
                UItooltip tip2 = tmp.AddComponent<UItooltip>();
                tip2.Title = "Cycle";
                tip2.Text = "Average game ticks for a factory to be active agagin.";

                tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_Cycle";
                tmp.transform.localPosition = new Vector3(-3, -27, 0);
                text_Cycle = tmp.GetComponent<Text>();

                tmp = GameObject.Instantiate(checkBox, group.transform);
                tmp.name = "checkbox_local";
                tmp.transform.localPosition = new Vector3(60, - 43, 0);
                GameObject.Destroy(tmp.GetComponent<Localizer>());
                Text text_local = tmp.GetComponent<Text>();
                text_local.font = text_Cycle.font;
                text_local.fontSize = 14;
                text_local.text = "Active local";

                UItooltip tip3 = tmp.AddComponent<UItooltip>();
                tip3.Title = "ActiveLocalFactory";
                tip3.Text = "Set local factory always active.";

                toggle_local = tmp.GetComponentInChildren<UIToggle>().toggle;
                toggle_local.onValueChanged.AddListener(new UnityAction<bool>(OnToggleChange));
                tmp = toggle_local.gameObject;
                tmp.transform.localPosition = new Vector3(70, 0); //60
                tmp.transform.localScale = new Vector3(0.75f, 0.75f);
            }

            if (factoryCount != GameMain.data.factoryCount)
                OnFactroyCountChange();
        }

        public static void OnDestory()
        {
            GameObject.Destroy(group);
        }

        public static void OnFactroyCountChange()
        {
            eventLock = true;
            factoryCount = GameMain.data.factoryCount;
            int workingCount = Math.Min(MainManager.MaxFactoryCount, factoryCount);
            // If there are remote facotries, set minFactoryCount to 2 (1 always on local + 1 circular remote)
            minFactoryCount = (MainManager.ActiveLocalFactory && GameMain.localPlanet != null && factoryCount > 1) ? 2 : 1;

            input.text = MainManager.MaxFactoryCount.ToString();
            slider.maxValue = factoryCount;
            slider.minValue = minFactoryCount;
            slider.value = workingCount;
            if (workingCount > (minFactoryCount - 1))
                text_Cycle.text = string.Format("Cycle:{0,7:F2} ticks", (float)(factoryCount - (minFactoryCount - 1)) / (workingCount - (minFactoryCount - 1)));
            else
                text_Cycle.text = "Only local planet";
            eventLock = false;

            //Log.Debug($"Max{maxFactoryCount} min{minFactoryCount} Enable{FactoryPool.Enable}");
        }

        public static void OnSliderChange(float val)
        {
            if (!eventLock)
            {
                val = Mathf.Round(val / 1f) * 1f;
                slider.value = val;
                MainManager.MaxFactoryCount = (int)val;
                OnFactroyCountChange();
            }
        }

        public static void OnInputValueEnd(string val)
        {
            if (!eventLock)
            {
                if (int.TryParse(val, out int value) && value >= minFactoryCount)
                {
                    MainManager.MaxFactoryCount = value;
                }
                OnFactroyCountChange();
            }
        }

        public static void OnToggleChange(bool val)
        {
            if (!eventLock)
            {
                MainManager.ActiveLocalFactory = val;
                OnFactroyCountChange();
            }
        }
    }
}
