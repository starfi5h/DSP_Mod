using HarmonyLib;
using UnityEngine;

namespace BuildToolOpt
{
    class PlayerController_Patch
    {
        static int clipboardLength;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateCommandState))]
        public static void UpdateCommandState_Prefix(PlayerController __instance)
        {
            // 按下複製熱鍵
            if (VFInput.readyToBuild 
                && VFInput._pasteBlueprintKey 
                && VFInput.inScreen 
                && GameMain.history.blueprintLimit > 0)
            {

                // 從複製模式過來, 則不解析剪貼簿
                if (__instance.actionBuild.blueprintMode == EBlueprintMode.Copy
                    && !BlueprintData.IsNullOrEmpty(__instance.actionBuild.blueprintCopyTool.blueprint))
                {
                    return;
                }                

                // 如果剪貼簿的長度變化, 檢查是否可以解析為藍圖
                if (clipboardLength != GUIUtility.systemCopyBuffer.Length)
                {
                    string systemCopyBuffer = GUIUtility.systemCopyBuffer;                    
                    if (string.IsNullOrEmpty(systemCopyBuffer))
                    {
                        clipboardLength = 0;
                        return;
                    }
                    clipboardLength = systemCopyBuffer.Length;
                    var blueprintData = new BlueprintData();
                    blueprintData.FromBase64String(systemCopyBuffer);
                    if (blueprintData.isValid)
                    {
                        // 更新blueprintClipboard, 以便後續呼叫this.OpenBlueprintPasteMode(null, "")
                        __instance.actionBuild.blueprintClipboard = blueprintData;
                        UIRealtimeTip.Popup($"Parse {clipboardLength:N0} from clipboard!", false, 0);
                    }
                }

            }
        }
    }
}
