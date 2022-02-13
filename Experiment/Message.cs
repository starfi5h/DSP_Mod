using BepInEx;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Experiment
{
    class Message
    {
        public static void DisplayTemporaryWarning(string warningText, int millisecond)
        {
            DisplayCriticalWarning(warningText);
            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                Thread.Sleep(millisecond);
                return (() =>
                {
                    RemoveCriticalWarning();
                });
            });
        }

        public static void DisplayCriticalWarning(string warningText)
        {
            WarningSystem warningSystem = GameMain.data.warningSystem;
            ECriticalWarning id = ECriticalWarning.Null;

            if (warningSystem.criticalWarnings.ContainsKey(id))
            {
                if (warningSystem.criticalWarnings[id].warningParam != 0)
                {
                    warningSystem.criticalWarnings[id].warningParam = 0;
                    warningSystem.criticalWarnings[id].Update();
                    warningSystem.UpdateCriticalWarningText();
                    return;
                }
            }
            else
            {
                CriticalWarningData data = new CriticalWarningData(id, 0);
                data.warningText = warningText;
                warningSystem.criticalWarnings.Add(id, data);
                warningSystem.UpdateCriticalWarningText();
            }
        }

        public static void RemoveCriticalWarning()
        {
            GameMain.data.warningSystem.UnsetCriticalWarning(ECriticalWarning.Null);
        }
    }

    public static class InGamePopup
    {
        private static UIMessageBox displayedMessage;
        private static GameObject statePanel;
        private static Text stateMessage;

        public static void FadeOut()
        {
            displayedMessage?.FadeOut();
            displayedMessage = null;
        }

        public static void UpdateMessage(string message)
        {
            if (displayedMessage != null)
            {
                displayedMessage.m_MessageText.horizontalOverflow = HorizontalWrapMode.Overflow;
                displayedMessage.m_MessageText.verticalOverflow = VerticalWrapMode.Overflow;
                displayedMessage.m_MessageText.text = message;
            }
        }

        // Info
        public static void ShowInfo(string title, string message, string btn1, Action resp1 = null)
        {
            Show(UIMessageBox.INFO, title, message, btn1, resp1);
        }

        public static void ShowInfo(string title, string message, string btn1, string btn2, Action resp1, Action resp2)
        {
            Show(UIMessageBox.INFO, title, message, btn1, btn2, resp1, resp2);
        }

        public static void ShowInfo(string title, string message, string btn1, string btn2, string btn3, Action resp1, Action resp2, Action resp3)
        {
            Show(UIMessageBox.INFO, title, message, btn1, btn2, btn3, resp1, resp2, resp3);
        }

        // Warning
        public static void ShowWarning(string title, string message, string btn1, Action resp1 = null)
        {
            Show(UIMessageBox.WARNING, title, message, btn1, resp1);
        }

        public static void ShowWarning(string title, string message, string btn1, string btn2, Action resp1, Action resp2)
        {
            Show(UIMessageBox.WARNING, title, message, btn1, btn2, resp1, resp2);
        }

        public static void ShowWarning(string title, string message, string btn1, string btn2, string btn3, Action resp1, Action resp2, Action resp3)
        {
            Show(UIMessageBox.WARNING, title, message, btn1, btn2, btn3, resp1, resp2, resp3);
        }

        // Question
        public static void ShowQuestion(string title, string message, string btn1, Action resp1 = null)
        {
            Show(UIMessageBox.QUESTION, title, message, btn1, resp1);
        }

        public static void ShowQuestion(string title, string message, string btn1, string btn2, Action resp1, Action resp2)
        {
            Show(UIMessageBox.QUESTION, title, message, btn1, btn2, resp1, resp2);
        }

        public static void ShowQuestion(string title, string message, string btn1, string btn2, string btn3, Action resp1, Action resp2, Action resp3)
        {
            Show(UIMessageBox.QUESTION, title, message, btn1, btn2, btn3, resp1, resp2, resp3);
        }

        // Error
        public static void ShowError(string title, string message, string btn1, Action resp1 = null)
        {
            Show(UIMessageBox.ERROR, title, message, btn1, resp1);
        }

        public static void ShowError(string title, string message, string btn1, string btn2, Action resp1, Action resp2)
        {
            Show(UIMessageBox.ERROR, title, message, btn1, btn2, resp1, resp2);
        }

        public static void ShowError(string title, string message, string btn1, string btn2, string btn3, Action resp1, Action resp2, Action resp3)
        {
            Show(UIMessageBox.ERROR, title, message, btn1, btn2, btn3, resp1, resp2, resp3);
        }

        public static void ShowStatus(string message)
        {
            if (stateMessage == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Auto Save/content/tip-panel");
                statePanel = GameObject.Instantiate(go, go.transform.parent.parent);
                statePanel.transform.localPosition = new Vector3(0, 300, 0);
                GameObject.Destroy(statePanel.transform.Find("bg").gameObject);
                GameObject.Destroy(statePanel.transform.Find("icon").gameObject);
                GameObject.Destroy(statePanel.transform.Find("glow-1").gameObject);
                GameObject.Destroy(statePanel.transform.Find("achiev-ban-text").gameObject);
                stateMessage = statePanel.transform.Find("text").GetComponent<Text>();
            }
            statePanel.SetActive(message != "");
            stateMessage.text = message;
        }

        // Base
        private static void Show(int type, string title, string message, string btn1, Action resp1 = null)
        {
            displayedMessage = UIMessageBox.Show(title, message, btn1, type, () => { resp1?.Invoke(); });
        }

        private static void Show(int type, string title, string message, string btn1, string btn2, Action resp1, Action resp2)
        {
            displayedMessage = UIMessageBox.Show(title, message, btn1, btn2, type, () => { resp1?.Invoke(); }, () => { resp2?.Invoke(); });
        }

        private static void Show(int type, string title, string message, string btn1, string btn2, string btn3, Action resp1, Action resp2, Action resp3)
        {
            displayedMessage = UIMessageBox.Show(title, message, btn1, btn2, btn3, type, () => { resp1?.Invoke(); }, () => { resp2?.Invoke(); }, () => { resp3?.Invoke(); });
        }
    }
}
