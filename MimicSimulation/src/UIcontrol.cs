using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MimicSimulation
{
    class UIcontrol
    {
        static int maxFactoryCount;
        static int factoryCount = -1;

        static GameObject group;
        static Slider slider;
        static InputField input;
        static Text text_MaxCount;
        static Text text_factory;
        static Text text_Cycle;
        static bool eventLock;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init()
        {
            if (group == null)
            {
                Slider slider0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.slider0;
                InputField input0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.input0;
                Text text0 = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuValueText1;
                GameObject panelObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Statistics Window/performance-bg/cpu-panel");

                group = new GameObject("MimicSimulation_Group");
                group.transform.SetParent(panelObj.transform);
                group.transform.localPosition = new Vector3(200, 100);
                group.transform.localScale = Vector3.one;

                GameObject tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_MaxCount";
                tmp.transform.localPosition = new Vector3(-5, 32);
                text_MaxCount = tmp.GetComponent<Text>();
                text_MaxCount.text = "MaxCount";

                tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_factory";
                tmp.transform.localPosition = new Vector3(-57, 8.5f);
                text_factory = tmp.GetComponent<Text>();
                text_factory.text = "factory";

                tmp = GameObject.Instantiate(input0.gameObject, group.transform);
                tmp.name = "input_MaxFactoryCount";
                tmp.transform.localPosition = new Vector3(150, 0);
                input = tmp.GetComponent<InputField>();
                input.characterValidation = InputField.CharacterValidation.Integer;
                input.contentType = InputField.ContentType.IntegerNumber;
                input.onEndEdit.AddListener(new UnityAction<string>(OnInputValueEnd));

                tmp = GameObject.Instantiate(slider0.gameObject, group.transform);
                tmp.name = "slider_factoryCount";
                tmp.transform.localPosition = new Vector3(55, -20, 0);
                slider = tmp.GetComponent<Slider>();
                slider.minValue = 1;
                slider.wholeNumbers = true;
                slider.onValueChanged.AddListener(new UnityAction<float>(OnSliderChange));

                tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                tmp.name = "text_Cycle";
                tmp.transform.localPosition = new Vector3(0, -30, 0);
                text_Cycle = tmp.GetComponent<Text>();
            }

            maxFactoryCount = FactoryPool.MaxFactoryCount;
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
            int workingCount = Math.Min(maxFactoryCount, factoryCount);
            FactoryPool.MaxFactoryCount = maxFactoryCount;

            input.text = maxFactoryCount.ToString();
            slider.maxValue = factoryCount;
            slider.value = workingCount;
            if (factoryCount == workingCount)
                text_Cycle.text = string.Format("Cycle: {0,7:F2} ticks", 1.0f);
            else if (workingCount > 1)
                text_Cycle.text = string.Format("Cycle: {0,7:F2} ticks", (float)factoryCount / (workingCount - 1));
            else
                text_Cycle.text = "Only local planet";
            eventLock = false;
        }

        public static void OnSliderChange(float val)
        {
            if (!eventLock)
            {
                val = Mathf.Round(val / 1f) * 1f;
                slider.value = val;
                maxFactoryCount = (int)val;
                OnFactroyCountChange();
            }
        }

        public static void OnInputValueEnd(string val)
        {
            if (!eventLock)
            {
                if (int.TryParse(val, out int value) && value >= 1)
                {
                    maxFactoryCount = value;
                }
                OnFactroyCountChange();
            }
        }
    }
}
