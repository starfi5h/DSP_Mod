using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace BulletTime
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

        static bool isBlocked;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        private static void UIGame_Prefix(UIGame __instance)
        {
            if (!GameMain.isRunning || __instance.willClose)
            {
                return;
            }

            if (!BulletTime.State.Interactable && !isBlocked)
            {                
                bool balcklist = __instance.isAnyFunctionWindowActive || __instance.dysonEditor.active;
                bool whitelist = __instance.statWindow.active || __instance.replicator.active || __instance.mechaWindow.active;
                if (balcklist && !whitelist)
                {
                    Log.Debug("wee");
                    UIMessageBox.Show("Read-Only", "Can't interact with game world during auto-save\nPlease wait or press ESC to close the window", null, 0, () => { });
                    isBlocked = true;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMessageBox), nameof(UIMessageBox.CloseTopMessage))]
        private static void CloseTopMessage_Postfix(ref bool __result)
        {
            // Don't eat ESC so it will close the window
            if (isBlocked)
            {
                __result = false;
                isBlocked = false;
            }
        }




    }
}
