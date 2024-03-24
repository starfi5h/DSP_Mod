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
        static UIToggle backGroundMuteToggle;
        static InputField filterInput;
        static UIComboBox audioComboBox;
        static Text audioClipPath;
        static UIToggle audioEnableToggle;
        static Button playAudioButton;

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
        public static void Init()
        {
            if (group == null)
            {
                Plugin.Log.LogDebug("init");
                try
                {
                    GameObject settingTab = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-2");
                    GameObject checkBoxTemple = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-1/fullscreen");
                    GameObject comboBoxTemple = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-1/resolution/ComboBox");
                    GameObject inputTemple = GameObject.Find("UI Root/Overlay Canvas/In Game/Globe Panel/name-input");
                    GameObject buttonTemple = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-2/revert-button");

                    // Create group
                    group = new GameObject("AutoMute_Group");
                    group.transform.SetParent(settingTab.transform);
                    group.transform.localPosition = new Vector3(-100, 280);
                    group.transform.localScale = Vector3.one;

                    // Create background mute toggle
                    var go = GameObject.Instantiate(checkBoxTemple, group.transform);
                    go.name = "MuteInBackground Toggle";
                    go.transform.localPosition = new Vector3(0, 0, 0);
                    GameObject.Destroy(go.GetComponent<Localizer>());
                    Text text_local = go.GetComponent<Text>();
                    //text_local.font = text_factory.font;
                    text_local.text = "Mute in background".Translate();

                    backGroundMuteToggle = go.GetComponentInChildren<UIToggle>();
                    backGroundMuteToggle.transform.localPosition = new Vector3(120, 5, 0);
                    backGroundMuteToggle.isOn = Plugin.Instance.MuteInBackground.Value;
                    backGroundMuteToggle.toggle.onValueChanged.AddListener(OnBackGroundMuteToggleChange);


                    // Create InputField filterInput to search audio with string 
                    go = GameObject.Instantiate(inputTemple, group.transform);
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

                    // Create UIComboBox audioComboBox for drop-down select audio
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


                    // Create UIToggle audioEnableToggle and Text audioClipPath
                    go = GameObject.Instantiate(checkBoxTemple, group.transform);
                    go.name = "Audio Enable Toggle";
                    go.transform.localPosition = new Vector3(210 + 30, -60, 0);
                    GameObject.Destroy(go.GetComponent<Localizer>());
                    audioClipPath = go.GetComponent<Text>();
                    audioClipPath.text = "(Audio ClipPath)";

                    audioEnableToggle = go.GetComponentInChildren<UIToggle>();
                    audioEnableToggle.transform.localPosition = new Vector3(-40, 5, 0);
                    audioEnableToggle.isOn = true;
                    audioEnableToggle.toggle.onValueChanged.AddListener(OnAudioEnableToggleChange);

                    // Create button to play audio
                    go = GameObject.Instantiate(buttonTemple, group.transform);
                    go.name = "Play Audio Button";
                    go.transform.localPosition = new Vector3(210, -130, 0);
                    var rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(90, 30);
                    playAudioButton = go.GetComponent<Button>();
                    playAudioButton.onClick.AddListener(PlayAudio);
                    var text = go.GetComponentInChildren<Text>();
                    text.text = "Play Audio";
                    GameObject.Destroy(go.GetComponent<UIButton>());
                    GameObject.Destroy(go.GetComponentInChildren<Localizer>());
                }
                catch
                {
                    Plugin.Log.LogWarning("UI component initial fail!");
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
            backGroundMuteToggle = null;
            GameObject.Destroy(group);
            group = null;
        }

        static void OnBackGroundMuteToggleChange(bool value)
        {
            Plugin.Instance.MuteInBackground.Value = value;
            backGroundMuteToggle.isOn = value;
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
                    audioComboBox.m_Input.text = audioProto.name;
                    playAudioButton.gameObject.SetActive(true);
                }                
            }
            else
            {
                if (!Plugin.AudioVolumes.ContainsKey(audioProto.name))
                {
                    Plugin.AudioVolumes.Add(audioProto.name, audioProto.Volume);
                    audioProto.Volume = 0;
                    audioComboBox.m_Input.text = "<color=#ff9900ff>" + audioProto.name + "</color>";
                    playAudioButton.gameObject.SetActive(false);
                }
            }
            Plugin.Log.LogDebug($"[{audioProto.name}]: {audioProto.Volume}");
            
            // Stored the changed ids
            var sb = new StringBuilder();
            foreach (var name in Plugin.AudioVolumes.Keys)
            {
                sb.Append(name);
                sb.Append(' ');
            }
            Plugin.Instance.MuteList.Value = sb.ToString();

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
                playAudioButton.gameObject.SetActive(!isMute);
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
            else
            {
                audioComboBox.m_Input.text = "";
                audioClipPath.text = "";
                playAudioButton.gameObject.SetActive(false);
            }
            OnComboBoxIndexChange();
        }

        static void RefreshAudioComboBox()
        {
            audioComboBox.Items.Clear();
            audioComboBox.ItemsData.Clear();

            for (var index = 0; index < LDB.audios.dataArray.Length; index++)
            {
                var name = LDB.audios.dataArray[index].name;

                if (!string.IsNullOrEmpty(searchStr))
                {
                    var result = name.IndexOf(searchStr, 0, StringComparison.OrdinalIgnoreCase);
                    if (result == -1) continue;
                }

                audioComboBox.ItemsData.Add(index);
                if (Plugin.AudioVolumes.ContainsKey(name)) // mute
                {
                    audioComboBox.Items.Add("<color=#ff9900ff>" + name + "</color>");
                }
                else
                {
                    audioComboBox.Items.Add(name);
                }
            }
            
            //Plugin.Log.LogDebug(audioComboBox.Items.Count);
        }
    }
}
