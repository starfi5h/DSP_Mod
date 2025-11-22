using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SampleAndHoldSim
{
    class UIcontrol
    {
        public static int SliderMax = 20;

        static bool initialized;
        static GameObject group;
        static Slider slider;
        static InputField input;
        static Text text_factory;
        static Toggle toggle_local;
        static bool eventLock;
        static UItooltip tip1, tip2, tip3;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init()
        {
            if (group == null)
            {
                try
                {
                    Slider slider0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.slider0;
                    InputField input0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.input0;
                    Text text0 = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuValueText1;
                    GameObject checkBoxWithTextTemple = UIRoot.instance.optionWindow.vsyncComp.transform.parent.gameObject;
                    GameObject panelObj = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuActiveButton.gameObject.transform.parent.gameObject;

                    group = new GameObject("SAHS_Group");
                    group.transform.SetParent(panelObj.transform);
                    group.transform.localPosition = new Vector3(200, 105);
                    group.transform.localScale = Vector3.one;

                    GameObject tmp = GameObject.Instantiate(text0.gameObject, group.transform);
                    tmp.name = "text_factory";
                    tmp.transform.localPosition = new Vector3(-50, 9 - 10);
                    text_factory = tmp.GetComponent<Text>();
                    text_factory.text = "Ratio".Translate();

                    tmp = GameObject.Instantiate(input0.gameObject, group.transform);
                    tmp.name = "input_UpdatePeriod";
                    tmp.transform.localPosition = new Vector3(155, 1 - 10);
                    tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 20);
                    input = tmp.GetComponent<InputField>();
                    input.characterValidation = InputField.CharacterValidation.Integer;
                    input.contentType = InputField.ContentType.IntegerNumber;
                    input.onEndEdit.AddListener(new UnityAction<string>(OnInputValueEnd));
                    tip1 = tmp.AddComponent<UItooltip>();
                    tip1.Title = "Update Period".Translate();
                    tip1.Text = "Compute actual factory simulation every x ticks.".Translate();

                    tmp = GameObject.Instantiate(slider0.gameObject, group.transform);
                    tmp.name = "slider_UpdatePeriod";
                    tmp.transform.localPosition = new Vector3(157, -27, -2);
                    tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 12);
                    slider = tmp.GetComponent<Slider>();
                    slider.minValue = 1;
                    slider.maxValue = SliderMax;
                    slider.wholeNumbers = true;
                    slider.onValueChanged.AddListener(new UnityAction<float>(OnSliderChange));
                    tip2 = tmp.AddComponent<UItooltip>();
                    tip2.Title = "Update Period".Translate();
                    tip2.Text = "Compute actual factory simulation every x ticks.".Translate();

                    tmp = GameObject.Instantiate(checkBoxWithTextTemple, group.transform);
                    tmp.name = "checkbox_local";
                    tmp.transform.localPosition = new Vector3(80, 30, 0);
                    GameObject.Destroy(tmp.GetComponent<Localizer>());
                    Text text_local = tmp.GetComponent<Text>();
                    text_local.font = text_factory.font;
                    text_local.fontSize = 12;
                    text_local.text = "Focus local".Translate();

                    tip3 = tmp.AddComponent<UItooltip>();
                    tip3.Title = "Focus on local factory".Translate();
                    tip3.Text = "Let local planet factory always active.".Translate();

                    toggle_local = tmp.GetComponentInChildren<UIToggle>().toggle;
                    toggle_local.onValueChanged.AddListener(new UnityAction<bool>(OnToggleChange));
                    tmp = toggle_local.gameObject;
                    tmp.transform.localPosition = new Vector3(52, 0); //60
                    tmp.transform.localScale = new Vector3(0.75f, 0.75f);
                    initialized = true;

                    RefreshUI();
                }
                catch
                {
                    Log.Error("UI component initial fail!");
                }
            }
        }

        public static void OnDestory()
        {
            GameObject.Destroy(group);
            tip1?.OnDestory();
            tip2?.OnDestory();
            tip3?.OnDestory();
            initialized = false;
        }

        public static void RefreshUI()
        {
            eventLock = true;
            if (initialized)
            {
                input.text = MainManager.UpdatePeriod.ToString();
                slider.value = MainManager.UpdatePeriod;
                toggle_local.isOn = MainManager.FocusLocalFactory;
            }
            eventLock = false;
        }

        public static void OnSliderChange(float val)
        {
            if (!eventLock)
            {
                val = Mathf.Round(val / 1f) * 1f;
                slider.value = val;
                MainManager.UpdatePeriod = (int)val;
                MainManager.Init();
                RefreshUI();
            }
        }

        public static void OnInputValueEnd(string val)
        {
            if (!eventLock)
            {
                if (int.TryParse(val, out int value) && value >= 1)
                {
                    MainManager.UpdatePeriod = value;
                    MainManager.Init();
                }
                RefreshUI();
            }
        }

        public static void OnToggleChange(bool val)
        {
            if (!eventLock)
            {
                MainManager.FocusLocalFactory = val;
                RefreshUI();
            }
        }
    }
}
