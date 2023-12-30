using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace RailgunsRetargetMini
{
    public class UIEjectorWindow_Patch
    {
        static UISwitch forceRetargetToggle;

        [HarmonyPrefix, HarmonyPatch(typeof(UIEjectorWindow), "_OnOpen")]
        public static void OnOpen(UIEjectorWindow __instance)
        {
            try
            {
                if (forceRetargetToggle == null)
                {
                    var go = GameObject.Instantiate(__instance.boostSwitch.gameObject, __instance.boostSwitch.transform.parent);

                    forceRetargetToggle = go.GetComponent<UISwitch>();
                    forceRetargetToggle.SetToggleNoEvent(Configs.ForceRetargeting);
                    forceRetargetToggle.uiButton.onClick += Onclick;

                    var label = go.transform.Find("label");
                    label.transform.localPosition += new Vector3(-10, 0, 0);
                    UnityEngine.Object.Destroy(label.GetComponent<Localizer>());
                    var text = label.GetComponent<Text>();
                    text.text = "Force Retarget".Translate();
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;

                    var uibutton = go.GetComponent<UIButton>();
                    uibutton.tips.tipTitle = "[RailgunsRetargetMini]";
                    uibutton.tips.tipText = "Retarget orbit for all unset ejctors";
                    uibutton.tips.corner = 2;
                }
                forceRetargetToggle.gameObject.SetActive(!GameMain.sandboxToolsEnabled);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void OnDestory()
        {
            UnityEngine.Object.Destroy(forceRetargetToggle?.gameObject);
            forceRetargetToggle = null;
        }

        public static void Onclick(int _)
        {
            Configs.ForceRetargeting = !Configs.ForceRetargeting;
            Plugin.ForceRetargeting.Value = Configs.ForceRetargeting;
            Plugin.Log.LogDebug("ForceRetargeting: " + Configs.ForceRetargeting);
        }
    }
}
