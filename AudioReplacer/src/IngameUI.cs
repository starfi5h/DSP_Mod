using HarmonyLib;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AudioReplacer
{
    class IngameUI
    {
        static AudioReplacerPlugin plugin;
        static GameObject group;

        static InputField folderPathInput;
        static UIButton loadFolderBtn;
        static UIButton unloadFolderBtn;
        static UIButton unloadAllBtn;
        static Text loadInfoText;

        static InputField filterInput;
        static UIComboBox audioComboBox;
        static Text audioClipPath;
        static Button playAudioButton;
        static Text playAudioText;

        static string searchStr = "";
        static AudioProto currentAudioProto = null;
        static VFAudio currentVFAudio = null;
        static int audioCount = 0;

        [HarmonyPrefix, HarmonyPatch(typeof(VFListener), nameof(VFListener.SetPassFilter))]
        public static bool SetPassFilter_Block()
        {
            // This is set in BGMController.UpdateLogic to make audio low when pause
            if (currentVFAudio != null && currentVFAudio.isPlaying)
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
                plugin = AudioReplacerPlugin.Instance;
                try
                {
                    GameObject settingTab = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/details/content-2");
                    GameObject textTemple = GameObject.Find("UI Root/Overlay Canvas/Top Windows/Option Window/title-text");
                    GameObject comboBoxTemple = __instance.resolutionComp.transform.gameObject;
                    GameObject inputTemple = UIRoot.instance.uiGame.planetGlobe.nameInput.gameObject; //UI Root/Overlay Canvas/In Game/Globe Panel/name-input
                    GameObject buttonTemple = __instance.revertButtons[0].gameObject; //../Option Window/details/content-1/revert-button

                    // Create group
                    group = new GameObject("AudioReplacer_Group");
                    group.transform.SetParent(settingTab.transform);
                    group.transform.localPosition = new Vector3(-100, 90);
                    group.transform.localScale = Vector3.one;
                    GameObject go;

                    // Create folder grounp
                    go = GameObject.Instantiate(buttonTemple, group.transform);
                    go.name = "Load Folder Button";
                    go.transform.localPosition = new Vector3(0, 0, 0);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 30);
                    loadFolderBtn = go.GetComponent<UIButton>();
                    loadFolderBtn.onClick += LoadFolder;
                    var text = go.GetComponentInChildren<Text>();
                    text.text = "Load Folder".Translate();
                    GameObject.Destroy(go.GetComponentInChildren<Localizer>());

                    go = GameObject.Instantiate(loadFolderBtn.gameObject, group.transform);
                    go.name = "Unload Folder Button";
                    go.transform.localPosition = new Vector3(130 + 5, 0, 0);
                    unloadFolderBtn = go.GetComponent<UIButton>();
                    unloadFolderBtn.onClick += UnloadFolder;
                    text = go.GetComponentInChildren<Text>();
                    text.text = "Unload Folder".Translate();

                    go = GameObject.Instantiate(loadFolderBtn.gameObject, group.transform);
                    go.name = "Unload All Button";
                    go.transform.localPosition = new Vector3(260 + 10, 0, 0);
                    unloadAllBtn = go.GetComponent<UIButton>();
                    unloadAllBtn.onClick += UnloadAll;
                    text = go.GetComponentInChildren<Text>();
                    text.text = "Unload All".Translate();

                    go = GameObject.Instantiate(inputTemple, group.transform);
                    go.name = "Folder Path Input";
                    go.transform.localPosition = new Vector3(-2, -5, 0);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(402f, 30f);
                    go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                    go.SetActive(true);
                    folderPathInput = go.GetComponent<InputField>();
                    folderPathInput.text = AudioReplacerPlugin.Instance.AudioFolderPath.Value;
                    folderPathInput.characterLimit = 256;
                    if (!(GameConfig.gameVersion < new Version(0, 10, 34)))
                    {
                        for (var i = go.transform.childCount - 1; i >= 0; i--)
                        {
                            if (go.transform.GetChild(i).name != "name-text")
                            {
                                GameObject.Destroy(go.transform.GetChild(i).gameObject);
                            }
                        }
                    }

                    go = GameObject.Instantiate(textTemple, group.transform);
                    go.name = "Load Info";
                    go.transform.localPosition = new Vector3(0, 0, 0);
                    GameObject.Destroy(go.GetComponent<Localizer>());
                    loadInfoText = go.GetComponent<Text>();
                    loadInfoText.text = plugin.lastInfoMsg + " " + plugin.lastWarningMsg;
                    loadInfoText.fontSize = 14;

                    // Create InputField filterInput to search audio with string
                    const int OffsetAudioY = -80;
                    go = GameObject.Instantiate(inputTemple, group.transform);
                    go.name = "AudioName Filter";
                    go.transform.localPosition = new Vector3(-2, OffsetAudioY, 0);
                    filterInput = go.GetComponent<InputField>();
                    filterInput.text = "";
                    filterInput.onValueChanged.AddListener(new UnityAction<string>(OnFilterInputValueChanged));
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(203f, 30f);
                    go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                    go.SetActive(true);
                    if (!(GameConfig.gameVersion < new Version(0, 10, 34)))
                    {
                        for (var i = go.transform.childCount - 1; i >= 0; i--)
                        {
                            if (go.transform.GetChild(i).name != "name-text")
                            {
                                GameObject.Destroy(go.transform.GetChild(i).gameObject);
                            }
                        }
                    }

                    // Create UIComboBox audioComboBox for drop-down select audio
                    go = GameObject.Instantiate(comboBoxTemple, group.transform, false);
                    go.name = "Audio ComboBox";
                    go.transform.localPosition = new Vector3(0, OffsetAudioY - 30, 0);
                    var transform = go.transform.Find("Dropdown List ScrollBox/Mask/Content Panel/");
                    for (var i = transform.childCount - 1; i >= 0 ; i--)
                    {
                        if (transform.GetChild(i).name == "Item Button(Clone)")
                        {
                            // Clean up old itemButtons
                            GameObject.Destroy(transform.GetChild(i).gameObject);
                        }
                    }
                    transform = go.transform.Find("Main Button");
                    transform.GetComponent<Button>().onClick.AddListener(OnComboBoxClick);

                    audioComboBox = go.GetComponentInChildren<UIComboBox>();
                    //audioComboBox.onItemIndexChange.RemoveAllListeners();
                    RefreshAudioComboBox();
                    audioComboBox.m_Text.supportRichText = true;
                    audioComboBox.m_EmptyItemRes.supportRichText = true;
                    audioComboBox.m_ListItemRes.GetComponentInChildren<Text>().supportRichText = true;                    
                    foreach (var button in audioComboBox.ItemButtons)
                    { 
                        button.GetComponentInChildren<Text>().supportRichText = true;
                    }
                    audioComboBox.DropDownCount = 10;
                    audioComboBox.itemIndex = 0;
                    audioComboBox.m_Input.text = "<i>(Audio filtered by above field)</i>";
                    audioComboBox.onItemIndexChange.AddListener(OnComboBoxIndexChange);

                    // Create UIToggle audioEnableToggle and Text audioClipPath
                    go = GameObject.Instantiate(loadInfoText.gameObject, group.transform);
                    go.name = "Audio ClipPath";
                    go.transform.localPosition = new Vector3(210, OffsetAudioY + 15, 0);
                    audioClipPath = go.GetComponent<Text>();
                    audioClipPath.text = "(Custom Audio Path)".Translate();

                    // Create button to play audio
                    go = GameObject.Instantiate(buttonTemple, group.transform);
                    go.name = "Play Audio Button";
                    go.transform.localPosition = new Vector3(210, OffsetAudioY - 90, 0);
                    var rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(90, 30);
                    playAudioButton = go.GetComponent<Button>();
                    playAudioButton.onClick.AddListener(PlayAudio);
                    playAudioText = go.GetComponentInChildren<Text>();
                    playAudioText.text = "Play Audio".Translate();
                    GameObject.Destroy(go.GetComponent<UIButton>());
                    GameObject.Destroy(go.GetComponentInChildren<Localizer>());
                }
                catch
                {
                    plugin.LogWarn("UI component initial fail!");
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow._OnClose))]
        public static void OnClose()
        {
            currentVFAudio?.Stop();
            currentVFAudio = null;
        }


        public static void OnDestory()
        {
            currentVFAudio?.Stop();
            currentVFAudio = null;
            GameObject.Destroy(group);
            group = null;
        }

        static void LoadFolder(int _)
        {
            plugin.lastInfoMsg = "";
            plugin.lastWarningMsg = "";
            plugin.AudioFolderPath.Value = folderPathInput.text;
            AudioReplacerPlugin.LoadAudioFromDirectory(folderPathInput.text);
            loadInfoText.text = plugin.lastInfoMsg + " " + plugin.lastWarningMsg;
            RefreshAudioComboBox();
            OnComboBoxIndexChange();
        }

        static void UnloadFolder(int _)
        {
            plugin.lastInfoMsg = "";
            plugin.lastWarningMsg = "";
            if (string.IsNullOrEmpty(folderPathInput.text))
            {
                loadInfoText.text = "Path is empty.";
                return;
            }
            AudioReplacerPlugin.UnloadAudioFromDirectory(folderPathInput.text);
            loadInfoText.text = plugin.lastInfoMsg + " " + plugin.lastWarningMsg;
            RefreshAudioComboBox();
            OnComboBoxIndexChange();
        }

        static void UnloadAll(int _)
        {
            plugin.lastInfoMsg = "";
            plugin.lastWarningMsg = "";
            AudioReplacerPlugin.UnloadAudioFromDirectory();
            loadInfoText.text = plugin.lastInfoMsg + " " + plugin.lastWarningMsg;
            RefreshAudioComboBox();
            OnComboBoxIndexChange();
        }

        static void PlayAudio()
        {            
            if (currentAudioProto != null)
            {
                if (currentVFAudio != null) currentVFAudio.Stop();
                currentVFAudio = VFAudio.Create(currentAudioProto.name, null, Vector3.zero, false, 0, -1, -1L);
                currentVFAudio.Play();
            }
        }

        public static void OnComboBoxIndexChange()
        {
            if (audioComboBox.itemIndex >= 0 && audioComboBox.itemIndex < audioComboBox.Items.Count)
            {
                var dataArrayIndex = audioComboBox.ItemsData[audioComboBox.itemIndex];
                currentAudioProto = LDB.audios.dataArray[dataArrayIndex];
                if (currentAudioProto != null )
                {
                    if (plugin.ModifyAudio.TryGetValue(currentAudioProto.name, out var entry))
                    {
                        audioClipPath.text = entry.filePath;
                    }
                    playAudioText.text = currentAudioProto.Volume == 0.0f ? "Muted".Translate() : "Play Audio".Translate();
                }                
            }
            if (currentVFAudio != null)
            {
                // Stop the playing sound
                currentVFAudio.Stop();
                currentVFAudio = null;
            }
        }

        static void OnFilterInputValueChanged(string value)
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

        static void OnComboBoxClick()
        {
            if (audioCount != plugin.ModifyAudio.Count)
            {
                audioCount = plugin.ModifyAudio.Count;
                RefreshAudioComboBox();
            }
        }

        static void RefreshAudioComboBox()
        {
            audioComboBox.Items.Clear();
            audioComboBox.ItemsData.Clear();

            for (var index = 0; index < LDB.audios.dataArray.Length; index++)
            {
                var name = LDB.audios.dataArray[index].name;

                if (!plugin.ModifyAudio.ContainsKey(name)) continue;

                if (!string.IsNullOrEmpty(searchStr))
                {
                    var result = name.IndexOf(searchStr, 0, StringComparison.OrdinalIgnoreCase);
                    if (result == -1) continue;
                }

                audioComboBox.ItemsData.Add(index);
                audioComboBox.Items.Add(name);
            }
        }
    }
}
