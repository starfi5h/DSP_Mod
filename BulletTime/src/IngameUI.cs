using BulletTime.Nebula;
using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
{
    class IngameUI
    {
        public static string CurrentStatus = "";
        private static Slider slider;
        private static Text sliderText;
        private static Text stateMessage;
        private static UIToggle backgroundSaveToggle;

        // 右下角的時間速度控制
        private static GameObject timeTextGo;
        private static GameObject infoTextGo;
        private static Text infoText; // Show the current (stored) gameTick
        private static Text speedRatioText;
        private static readonly GameObject[] speedBtnGo = new GameObject[3];

        public static void Dispose()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                GameObject.Destroy(slider.gameObject);
            }
            slider = null;
            sliderText = null;
            if (stateMessage != null)
            {
                GameObject.Destroy(stateMessage.transform.parent.gameObject);
                stateMessage = null;
            }
            if (backgroundSaveToggle != null)
            {
                GameObject.Destroy(backgroundSaveToggle.transform.parent.gameObject);
                backgroundSaveToggle = null;
            }

            timeTextGo = null;
            for (int i = 0; i < speedBtnGo.Length; i++)
            {
                GameObject.Destroy(speedBtnGo[i]);
                speedBtnGo[i] = null;
            }
            GameObject.Destroy(infoTextGo);
            infoTextGo = null;
            infoText = null;
            GameObject.Destroy(speedRatioText?.gameObject);
            speedRatioText = null;
        }

        public static void Init()
        {
            try
            {
                if (slider == null)
                {
                    // Set slider
                    GameObject sliderGo = UIRoot.instance.optionWindow.audioVolumeComp.transform.gameObject;
                    Transform cpuPanel = UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuActiveButton.gameObject.transform.parent;
                    sliderGo = GameObject.Instantiate(sliderGo, cpuPanel);
                    sliderGo.name = "RealTickRatioSlider";
                    sliderGo.transform.localPosition = cpuPanel.Find("title-text").localPosition + new Vector3(-120, 1, 0);
                    slider = sliderGo.GetComponent<Slider>();
                    sliderText = sliderGo.GetComponentInChildren<Text>();
                    slider.value = BulletTimePlugin.StartingSpeed.Value;
                    slider.onValueChanged.AddListener(value => OnSliderChange(value));
                    OnSliderChange(slider.value);

                    // Set toggle
                    GameObject checkBoxWithText = UIRoot.instance.optionWindow.vsyncComp.transform.parent.gameObject;
                    Transform dataPanel = UIRoot.instance.uiGame.statWindow.performancePanelUI.dataActiveButton.gameObject.transform.parent;
                    GameObject backgroundSaveGo = GameObject.Instantiate(checkBoxWithText, dataPanel);
                    backgroundSaveGo.name = "Background autosave toggle";
                    backgroundSaveGo.transform.localPosition = new Vector3(250, 390, 0);
                    GameObject.Destroy(backgroundSaveGo.GetComponent<Localizer>());
                    Text text_local = backgroundSaveGo.GetComponent<Text>();
                    //text_local.font = text_factory.font;
                    text_local.fontSize = 14;
                    text_local.text = "Background autosave".Translate();

                    backgroundSaveToggle = backgroundSaveGo.GetComponentInChildren<UIToggle>();
                    backgroundSaveToggle.transform.localPosition = new Vector3(65, -20, 0);
                    backgroundSaveToggle.isOn = GameSave_Patch.isEnabled;
                    backgroundSaveToggle.toggle.onValueChanged.AddListener(OnAutosaveToggleChange);
                }
                // Only host can have control slider
                slider.gameObject.SetActive(!NebulaCompat.IsClient);
                GameStateManager.ManualPause = false;
                SetHotkeyPauseMode(false);
            }
            catch (System.Exception e)
            {
                Log.Warn("IngameUI fail! error:\n" + e);
            }

            try
            {
                if (timeTextGo == null)
                {
                    timeTextGo = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/time-text");
                    timeTextGo.SetActive(true);

                    // Create pause, resume and speedup button
                    RectTransform prefab = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/button-1-bg").GetComponent<RectTransform>();
                    Vector3 newPos = prefab.localPosition;
                    newPos.x += 35f + 5f;
                    newPos.y -= 20f - 13f;
                    for (int i = 0; i  < 3; i++)
                    {
                        var go = GameObject.Instantiate<RectTransform>(prefab, timeTextGo.transform.parent);
                        go.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                        go.localPosition = newPos;
                        newPos.x += 22f;

                        var btn = go.GetComponent<UIButton>();
                        btn.OnPointerDown(null);
                        btn.OnPointerEnter(null);
                        btn.data = i;
                        btn.onClick += OnSpeedButtonClick;

                        Sprite iconSprite = null;
                        switch (i)
                        {
                            case 0: // Sprite Path: "ui/textures/sprites/icons/pause-icon"
                                iconSprite = UIRoot.instance.uiGame.researchQueue.pauseButton.gameObject.transform.Find("icon").GetComponent<Image>().sprite;
                                btn.tips.tipTitle = go.name = "Pause".Translate();
                                btn.tips.tipText = "Toggle tactical pause mode".Translate();
                                break;

                            case 1: // Sprite Path: "ui/textures/sprites/icons/resume-icon"
                                iconSprite = UIRoot.instance.uiGame.researchQueue.resumeButton.gameObject.transform.Find("icon").GetComponent<Image>().sprite;
                                btn.tips.tipTitle = go.name = "Resume".Translate();
                                btn.tips.tipText = "Reset game speed back to 1x".Translate();
                                break;


                            case 2: // Sprite Path: "ui/textures/sprites/test/next-icon-2"
                                iconSprite = Resources.Load<Sprite>("ui/textures/sprites/test/next-icon-2");
                                btn.tips.tipTitle = go.name = "SpeedUp".Translate();
                                btn.tips.tipText = string.Format("Left click to increase game speed\nRight click to set to max ({0}x)".Translate(), BulletTimePlugin.MaxSpeedupScale.Value);
                                btn.onRightClick += OnSpeedupButtonRightClcik;
                                break;
                        }
                        go.Find("button-1/icon").GetComponent<Image>().sprite = iconSprite;
                        speedBtnGo[i] = go.gameObject;
                    }

                    // Create info text
                    infoTextGo = GameObject.Instantiate(timeTextGo, timeTextGo.transform.parent);
                    infoTextGo.name = "pause info-text";
                    infoText = infoTextGo.GetComponent<Text>();
                    infoText.text = "Pause".Translate();
                    infoText.enabled = true;
                    infoTextGo.SetActive(false);

                    // Create speed ratio text and reduce realTime text width
                    var realTimeTextGo = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/real-time-text");
                    var rectTransfrom = (RectTransform)realTimeTextGo.transform;
                    rectTransfrom.sizeDelta = new Vector2(50, rectTransfrom.sizeDelta.y); // Reduce button width
                    var ratioGo = GameObject.Instantiate(realTimeTextGo, realTimeTextGo.transform.parent);
                    ratioGo.name = "speed ratio-text";                    
                    ratioGo.transform.localPosition += new Vector3(0, 15f);
                    GameObject.Destroy(ratioGo.GetComponent<UIButton>()); // Remove the time format toggle
                    ratioGo.SetActive(true);
                    speedRatioText = ratioGo.GetComponent<Text>();

                    // Make sure time text stays on top
                    timeTextGo.transform.SetAsLastSibling();
                    realTimeTextGo.transform.SetAsLastSibling();

                    // Make screenshot button little higher to avoid overlap
                    var screenshotGo = GameObject.Find("UI Root/Overlay Canvas/In Game/Game Menu/button-screenshot");
                    screenshotGo.transform.localPosition = new Vector2(-28, 75);
                }
                // Test: Let client has speedUp button enable
                // speedBtnGo[2].SetActive(!NebulaCompat.IsClient);
                SetSpeedRatioText();
            }
            catch (System.Exception e)
            {
                Log.Warn("Game Menu button UI fail! error:\n" + e);
            }
        }

        private static void OnSpeedButtonClick(int mode)
        {
            switch (mode)
            {
                case 0: // Pause
                    OnKeyPause();
                    break;

                case 1: // Resume                    
                    if (GameStateManager.Pause) OnKeyPause();
                    else // 如果不在暫停狀態, 則回復至普通速度
                    {
                        FPSController.SetFixUPS(0);
                        if (NebulaCompat.IsMultiplayerActive && NebulaCompat.SyncUps)
                        {
                            if (NebulaCompat.IsClient) NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedReset);
                            else NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedSet);
                        }
                    }
                    break;

                case 2: // SpeedUp
                    if (GameStateManager.Pause)
                    {
                        // If it is in pause state, resume
                        OnKeyPause();
                    }
                    else if (FPSController.instance.fixUPS == 0.0)
                    {
                        FPSController.SetFixUPS(120.0);
                        if (NebulaCompat.IsMultiplayerActive && NebulaCompat.SyncUps)
                        {
                            if (NebulaCompat.IsClient) NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedUp);
                            else NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedSet);
                        }
                    }
                    else if (FPSController.instance.fixUPS < 60.0 * BulletTimePlugin.MaxSpeedupScale.Value)
                    {
                        FPSController.SetFixUPS(FPSController.instance.fixUPS + 60.0);
                        if (NebulaCompat.IsMultiplayerActive && NebulaCompat.SyncUps)
                        {
                            if (NebulaCompat.IsClient) NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedUp);
                            else NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedSet);
                        }
                    }
                    break;
            }
            SetSpeedRatioText();
        }

        private static void OnSpeedupButtonRightClcik(int _)
        {
            if (GameStateManager.Pause)
            {
                // If it is in pause state, resume
                OnKeyPause();
            }
            FPSController.SetFixUPS(60.0 * BulletTimePlugin.MaxSpeedupScale.Value);
            SetSpeedRatioText();

            if (NebulaCompat.IsMultiplayerActive && NebulaCompat.SyncUps)
            {
                if (NebulaCompat.IsClient) NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedMax);
                else NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.SpeedSet);
            }
        }

        public static void SetSpeedRatioText()
        {
            if (speedRatioText == null) return;
            if (NebulaCompat.IsClient) // 客戶端的UPS是跟隨主機隨時在變動的, 因此不顯示
            { 
                speedRatioText.text = "";
                return;
            }

            float ratio = (float)FPSController.instance.fixUPS / 60.0f;
            if (ratio == 0.0f)
                speedRatioText.text = "1 x"; //正常流速
            else if ((ratio - (int)ratio) < 0.05) 
                speedRatioText.text = (int)ratio + " x"; //整數倍率
            else
                speedRatioText.text = $"{ratio:0.0} x"; //顯示到小數點1位
        }

        public static void OnStoredGameTickChange()
        {
            if (infoTextGo != null)
            {
                infoText.text = GameStateManager.StoredGameTick.ToString();
            }
        }

        public static void OnPauseModeChange(bool pause)
        {
            if (infoTextGo != null)
            {
                timeTextGo.SetActive(!pause);
                infoTextGo.SetActive(pause);
                infoText.text = GameStateManager.StoredGameTick.ToString();

                if (sliderText != null)
                {
                    sliderText.text = pause ? "Pause".Translate() : $"{(int)slider.value}%";
                }
                SetSpeedRatioText();
            }

            SetHotkeyPauseMode(false); //當暫停模式有任何變化時取消熱鍵時停
        }

        private static void OnSliderChange(float value)
        {
            if (value == 0)
            {                
                GameStateManager.ManualPause = true; //滑條拉至0
                sliderText.text = "pause".Translate();
                if (!GameStateManager.Pause && NebulaCompat.IsMultiplayerActive)
                    NebulaCompat.SendPacket(PauseEvent.Pause);
            }
            else
            {
                GameStateManager.ManualPause = false;
                sliderText.text = $"{(int)value}%";
                if (GameStateManager.Pause && NebulaCompat.IsMultiplayerActive)
                {
                    NebulaCompat.SendPacket(PauseEvent.Resume);                    
                }
                ShowStatus("");
            }
            GameStateManager.SetSpeedRatio(value/100f);

            SetHotkeyPauseMode(false); //當滑條有任何變化時取消熱鍵時停
        }

        public static void SetHotkeyPauseMode(bool active)
        {
            GameStateManager.HotkeyPause = active;
            if (active)
            {
                if (NebulaCompat.IsMultiplayerActive && !NebulaCompat.IsClient)
                    NebulaCompat.SendPacket(PauseEvent.Pause);
                GameStateManager.PlayerPosition = GameMain.mainPlayer?.position ?? Vector3.zero;
                SkillSystem_Patch.Enable = GameStateManager.EnableMechaFunc;
                ShowStatus(BulletTimePlugin.StatusTextPause.Value);
            }
            else
            {
                SkillSystem_Patch.Enable = false;
            }
        }

        public static void OnKeyPause()
        {
            if (NebulaCompat.IsClient) //客戶端向主機提出暫停/恢復請求
            {
                if (!GameStateManager.Interactable) return; //鎖定狀態時無法送出請求

                if (!GameStateManager.Pause)
                {
                    NebulaCompat.SendPacket(PauseEvent.Pause);
                    NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.Pause);
                }
                else
                {
                    NebulaCompat.SendPacket(PauseEvent.Resume);
                    NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.Resume);
                }
                return;
            }

            if (GameStateManager.Pause == false) //不在時停狀態時,切換至熱鍵時停
            {
                if (UIRoot.instance.uiGame.autoSave.showTime == 0) // 不在自動存檔的動畫
                {
                    GameStateManager.ManualPause = true; //進入手動時停狀態
                    GameStateManager.SetPauseMode(true);
                    SetHotkeyPauseMode(true); //啟動熱鍵時停
                    if (NebulaCompat.IsMultiplayerActive) // 主機廣播暫停訊息
                        NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.Pause);
                }
            }
            else if (GameStateManager.Interactable) //如果不是在自動存檔中, 回歸滑條設定值
            {
                OnSliderChange(slider.value);                
                if (NebulaCompat.IsMultiplayerActive) // 主機廣播恢復訊息
                    NebulaCompat.SendPlayerActionPacket(EPlayerSpeedAction.Resume);
            }
        }

        public static void OnKeyStepOneFrame()
        {
            GameStateManager.StepOneFrame = true;
        }

        private static void OnAutosaveToggleChange(bool val)
        {
            BulletTimePlugin.EnableBackgroundAutosave.Value = val;
            GameSave_Patch.Enable(val);
            backgroundSaveToggle.isOn = val;
        }

        public static void ShowStatus(string message)
        {
            if (stateMessage == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Auto Save/content/tip-panel");
                GameObject statePanel = GameObject.Instantiate(go, go.transform.parent.parent);
                statePanel.transform.localPosition = new Vector3(-35, statePanel.transform.localPosition.y + BulletTimePlugin.StatusTextHeightOffset.Value, 0);
                GameObject.Destroy(statePanel.transform.Find("bg").gameObject);
                GameObject.Destroy(statePanel.transform.Find("icon").gameObject);
                GameObject.Destroy(statePanel.transform.Find("glow-1").gameObject);
                GameObject.Destroy(statePanel.transform.Find("achiev-ban-text").gameObject);
                stateMessage = statePanel.transform.Find("text").GetComponent<Text>();
            }
            stateMessage.transform.parent.gameObject.SetActive(message != "");
            stateMessage.text = message;
            CurrentStatus = message;
        }
    }
}
