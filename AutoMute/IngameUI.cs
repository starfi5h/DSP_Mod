using HarmonyLib;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AutoMute
{
    class IngameUI
    {
        static GameObject group;
        static InputField filterInput;
        static Text muteInfoText;
        static UIToggle filterMuteOnlyToggle;

        static UIComboBox audioComboBox;
        static Text audioClipPath;
        static UIToggle audioEnableToggle;
        static Button playAudioButton;
        static Text playAutoButtonText; 

        static bool filterMuteOnly = false;
        static string searchStr = "";
        static AudioProto audioProto = null;
        static VFAudio vFAudio = null;

        [HarmonyPrefix, HarmonyPatch(typeof(VFListener), nameof(VFListener.SetPassFilter))]
        public static bool SetPassFilter_Block()
        {
            // This is set in BGMController.UpdateLogic to make audio low when pause
            if (vFAudio != null && vFAudio.isPlaying)
            {
                VFListener.lowPassFilter.enabled = false;
                VFListener.highPassFilter.enabled = false;
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow._OnOpen))]
        public static void Init(UIOptionWindow __instance)
        {
            if (group == null)
            {
                Plugin.Log.LogDebug("init");
                try
                {
                    GameObject settingTab = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-2");
                    GameObject checkBoxWithTextTemple = __instance.vsyncComp.transform.parent.gameObject;
                    GameObject comboBoxTemple = __instance.resolutionComp.transform.gameObject;
                    GameObject inputTemple = UIRoot.instance.uiGame.planetGlobe.nameInput.gameObject; //UI Root/Overlay Canvas/In Game/Globe Panel/name-input
                    GameObject buttonTemple = __instance.revertButtons[0].gameObject; //../Option Window/details/content-1/revert-button

                    // 創建一個群組包含所有mod的物件
                    group = new GameObject("AutoMute_Group");
                    group.transform.SetParent(settingTab.transform);
                    group.transform.localPosition = new Vector3(-100, 320);
                    group.transform.localScale = Vector3.one;


                    // 創建一個輸入框, 篩選音頻檔名
                    var go = GameObject.Instantiate(inputTemple, group.transform);
                    go.name = "AudioName Filter";
                    go.transform.localPosition = new Vector3(-2, -30, 0);
                    filterInput = go.GetComponent<InputField>();
                    filterInput.text = "";
                    filterInput.onValueChanged.AddListener(new UnityAction<string>(OnInputValueChanged));
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(203f, 30f);
                    go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                    // 在聚焦輸入框時無法使用滾輪, 因此不套用以下的聚焦設定
                    // tmp.transform.parent.GetChild(0).GetComponent<Button>().onClick.AddListener(new UnityAction(OnComboBoxClicked));
                    go.SetActive(true);

                    // 狀態回報文字, 以及勾選按鈕過濾靜音列表
                    go = GameObject.Instantiate(checkBoxWithTextTemple, group.transform);
                    go.name = "Filter Mute Only Toggle";
                    go.transform.localPosition = new Vector3(210 + 30, -30, 0);
                    GameObject.Destroy(go.GetComponent<Localizer>());
                    muteInfoText = go.GetComponent<Text>();
                    muteInfoText.text = "";

                    filterMuteOnlyToggle = go.GetComponentInChildren<UIToggle>();
                    filterMuteOnlyToggle.transform.localPosition = new Vector3(-40, 5, 0);
                    filterMuteOnlyToggle.isOn = filterMuteOnly;
                    filterMuteOnlyToggle.toggle.onValueChanged.AddListener(OnFilterMuteOnlyToggleChange);

                    // 創建一個下拉表單, 選音頻檔案
                    go = GameObject.Instantiate(comboBoxTemple, group.transform, false);
                    go.name = "Audio ComboBox";
                    go.transform.localPosition = new Vector3(0, -60, 0);
                    var transform = go.transform.Find("Dropdown List ScrollBox/Mask/Content Panel/");
                    for (var i = transform.childCount - 1; i >= 0 ; i--)
                    {
                        if (transform.GetChild(i).name == "Item Button(Clone)")
                        {
                            // Clean up old itemButtons
                            GameObject.Destroy(transform.GetChild(i).gameObject);
                        }
                    }

                    audioComboBox = go.GetComponentInChildren<UIComboBox>();
                    audioComboBox.onItemIndexChange.RemoveAllListeners();
                    RefreshAudioComboBox();
                    audioComboBox.m_Text.supportRichText = true;
                    audioComboBox.m_EmptyItemRes.supportRichText = true;
                    audioComboBox.m_ListItemRes.GetComponentInChildren<Text>().supportRichText = true;                    
                    foreach (var button in audioComboBox.ItemButtons)
                    { 
                        button.GetComponentInChildren<Text>().supportRichText = true;
                    }
                    audioComboBox.DropDownCount = 20;
                    audioComboBox.itemIndex = 0;
                    audioComboBox.m_Input.text = "<i>(Select Audio)</i>";
                    audioComboBox.onItemIndexChange.AddListener(OnComboBoxIndexChange);

                    // 創建音頻文件的路徑文字, 及靜音勾選按鈕
                    go = GameObject.Instantiate(checkBoxWithTextTemple, group.transform);
                    go.name = "Audio Enable Toggle";
                    go.transform.localPosition = new Vector3(210 + 30, -60, 0);
                    GameObject.Destroy(go.GetComponent<Localizer>());
                    audioClipPath = go.GetComponent<Text>();
                    audioClipPath.text = "(Audio ClipPath)";

                    audioEnableToggle = go.GetComponentInChildren<UIToggle>();
                    audioEnableToggle.transform.localPosition = new Vector3(-40, 5, 0);
                    audioEnableToggle.isOn = true;
                    audioEnableToggle.toggle.onValueChanged.AddListener(OnAudioEnableToggleChange);

                    // 創建播放音頻的按鈕
                    go = GameObject.Instantiate(buttonTemple, group.transform);
                    go.name = "Play Audio Button";
                    go.transform.localPosition = new Vector3(210, -130, 0);
                    var rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(90, 30);
                    playAudioButton = go.GetComponent<Button>();
                    playAudioButton.onClick.AddListener(PlayAudio);
                    playAutoButtonText = go.GetComponentInChildren<Text>();
                    playAutoButtonText.text = "Play Audio".Translate();
                    GameObject.Destroy(go.GetComponent<UIButton>());
                    GameObject.Destroy(go.GetComponentInChildren<Localizer>());
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("UI component initial fail!");
                    Plugin.Log.LogWarning(e);
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow._OnClose))]
        public static void OnClose()
        {
            vFAudio?.Stop();
            vFAudio = null;
        }


        public static void OnDestory()
        {
            vFAudio?.Stop();
            vFAudio = null;
            GameObject.Destroy(group);
            group = null;
        }

        static void PlayAudio()
        {            
            if (audioProto != null)
            {
                Plugin.Log.LogDebug("PlayAudio: " + audioProto.name);
                if (vFAudio != null) vFAudio.Stop();
                vFAudio = VFAudio.Create(audioProto.name, null, Vector3.zero, false, 0, -1, -1L);
                vFAudio.Play();
            }
        }

        static void OnFilterMuteOnlyToggleChange(bool value)
        {
            filterMuteOnlyToggle.isOn = value;
            filterMuteOnly = value;

            RefreshAudioComboBox();
            OnComboBoxIndexChange();
        }

        static void OnAudioEnableToggleChange(bool value)
        {
            audioEnableToggle.isOn = value;
            if (audioProto == null) return;
            if (value)
            {
                if (Plugin.AudioVolumes.TryGetValue(audioProto.name, out var volume))
                {
                    audioProto.Volume = volume;
                    Plugin.AudioVolumes.Remove(audioProto.name);
                    //playAudioButton.gameObject.SetActive(true);
                    playAutoButtonText.text = "Play Audio".Translate();
                }                
            }
            else
            {
                if (!Plugin.AudioVolumes.ContainsKey(audioProto.name))
                {
                    Plugin.AudioVolumes.Add(audioProto.name, audioProto.Volume);
                    audioProto.Volume = 0;
                    //playAudioButton.gameObject.SetActive(false);
                    playAutoButtonText.text = "Muted".Translate();
                }
            }
            audioComboBox.m_Input.text = GetRichText(audioProto.name);
            Plugin.Log.LogDebug($"[{audioProto.name}]: {audioProto.Volume}");
            
            // Stored the changed ids
            var sb = new StringBuilder();
            foreach (var name in Plugin.AudioVolumes.Keys)
            {
                sb.Append(name);
                sb.Append(' ');
            }
            Plugin.Instance.MuteList.Value = sb.ToString();
            Plugin.IsDirty = true;

            RefreshAudioComboBox();
        }

        public static void OnComboBoxIndexChange()
        {
            if (audioComboBox.itemIndex >= 0 && audioComboBox.itemIndex < audioComboBox.Items.Count)
            {
                var dataArrayIndex = audioComboBox.ItemsData[audioComboBox.itemIndex];
                audioProto = LDB.audios.dataArray[dataArrayIndex];
                audioClipPath.text = audioProto.ClipPath;
                var isMute = Plugin.AudioVolumes.ContainsKey(audioProto.name);
                audioEnableToggle.isOn = !isMute;
                //playAudioButton.gameObject.SetActive(!isMute);
                playAutoButtonText.text = isMute ? "Muted".Translate() : "Play Audio".Translate();
            }
            else
            {
                audioProto = null;
                audioComboBox.m_Input.text = "";
                audioClipPath.text = "";
                audioEnableToggle.isOn = false;
                //playAudioButton.gameObject.SetActive(false);
                playAutoButtonText.text = "No Audio".Translate();
            }
            if (vFAudio != null)
            {
                // Stop the playing sound
                vFAudio.Stop();
                vFAudio = null;
            }
        }

        static void OnInputValueChanged(string value)
        {
            searchStr = value;
            RefreshAudioComboBox();
            if (audioComboBox.Items.Count > 0)
            {
                audioComboBox.itemIndex = 0;
            }
            OnComboBoxIndexChange();
        }

        static void RefreshAudioComboBox()
        {
            audioComboBox.Items.Clear();
            audioComboBox.ItemsData.Clear();

            var muteCount = 0;
            for (var index = 0; index < LDB.audios.dataArray.Length; index++)
            {
                var name = LDB.audios.dataArray[index].name;

                if (Plugin.AudioVolumes.ContainsKey(name))
                {
                    muteCount++;
                }
                else if (filterMuteOnly)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(searchStr))
                {
                    var result = name.IndexOf(searchStr, 0, StringComparison.OrdinalIgnoreCase);
                    if (result == -1) continue;
                }

                audioComboBox.ItemsData.Add(index);
                audioComboBox.Items.Add(GetRichText(name));
            }

            muteInfoText.text = $"{audioComboBox.Items.Count}/{LDB.audios.dataArray.Length}";
            if (filterMuteOnly) muteInfoText.text += " (mute only)";
            else muteInfoText.text += $" ({muteCount} muted)";
            //Plugin.Log.LogDebug(audioComboBox.Items.Count);
        }

        static string GetRichText(string audioName)
        {
            if (Plugin.AudioVolumes.ContainsKey(audioName)) // mute
            {
                return "<color=#ff9900ff>" + audioName + "</color>";
            }
            return audioName;
        }
    }
}
