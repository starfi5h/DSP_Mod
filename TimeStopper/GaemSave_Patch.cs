using NGPT;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStopper
{
    class GaemSave_Patch
    {
        private static GameObject statePanel;
        private static Text stateMessage;
        public static void ShowStatus(string message)
        {
            if (stateMessage == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Auto Save/content/tip-panel");
                statePanel = GameObject.Instantiate(go, go.transform.parent.parent);
                statePanel.transform.localPosition = new Vector3(0, 300, 0);
                //GameObject.Destroy(UIAutoSaveContent.GetComponent<Tweener>());
                GameObject.Destroy(statePanel.transform.Find("bg").gameObject);
                GameObject.Destroy(statePanel.transform.Find("icon").gameObject);
                GameObject.Destroy(statePanel.transform.Find("glow-1").gameObject);
                GameObject.Destroy(statePanel.transform.Find("achiev-ban-text").gameObject);
                stateMessage = statePanel.transform.Find("text").GetComponent<Text>();                
            }
            statePanel.SetActive(message != "");
            stateMessage.text = message;
        }

    }
}
