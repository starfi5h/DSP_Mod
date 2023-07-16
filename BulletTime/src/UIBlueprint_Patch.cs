using BepInEx;
using HarmonyLib;
using System.IO;
using System.Text;
using UnityEngine;

namespace BulletTime
{
    class UIBlueprint_Patch
    {
        static float lastRefreshTime;
        static string lastFilePath;

        [HarmonyPrefix, HarmonyPatch(typeof(UIBlueprintBrowser), nameof(UIBlueprintBrowser._OnClose))]
        public static void UIBlueprintBrowser_OnClose()
        {
            lastRefreshTime = -1;
            lastFilePath = null;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BlueprintData), nameof(BlueprintData.Clone))]
        public static bool BlueprintData_Clone(BlueprintData __instance, ref BlueprintData __result)
        {
            // Optimize BlueprintData.Clone to use only export and import
            __result = new BlueprintData();
            __result.HeaderFromBase64String(__instance.headerStr);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    __instance.Export(binaryWriter);
                    binaryWriter.Flush();
                    memoryStream.Position = 0;

                    using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                    {
                        __result.Import(binaryReader);
                    }
                }
            }

            if (!__result.isValid)
            {
                Log.Warn("BlueprintData Clone is invalid!");
                __result = null;
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.Refresh))]
        public static void UIBlueprintInspector_Refresh(ref bool refreshCode, ref bool __state)
        {
            if (refreshCode)
            {
                refreshCode = false;
                __state = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector.Refresh))]
        public static void UIBlueprintInspector_Refresh(UIBlueprintInspector __instance, bool __state)
        {
            lastRefreshTime = Time.time;
            if (__state)
            {
                BlueprintData blueprint = __instance.blueprint;
                float time = lastRefreshTime;
                __instance.shareLengthText.text = "Generating...".Translate();
                __instance.shareCodeText.text = "Generating...".Translate();

                ThreadingHelper.Instance.StartAsyncInvoke(() =>
                {                    
                    StringBuilder stringBuilder = null;                    
                    lock (__instance) // MD5F is single-threaded
                    {
                        if (blueprint != __instance.blueprint) return () => {};
                        stringBuilder = blueprint.ToBase64StringBuilder();
                    }

                    return () =>
                    {
                        if (blueprint == __instance.blueprint)
                        {
                            __instance.shareLengthText.text = string.Format("几字节".Translate(), stringBuilder.Length);
                            if (stringBuilder.Length > 256)
                            {
                                stringBuilder.Length = 256;
                            }
                            __instance.shareCodeText.text = stringBuilder.ToString();
                        }
                        else
                        {
                            //Log.Debug("Blueprint preview string is outdated!");
                        }
                    };
                });
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIBlueprintFileItem), nameof(UIBlueprintFileItem.OnThisClick))]
        public static bool UIBlueprintFileItem_OnThisClick(UIBlueprintFileItem __instance)
        {    
            VFAudio.Create("ui-click-0", null, Vector3.zero, true, 1, -1, -1L);
            if (__instance.time - __instance.lastClickTime < 0.3f) // Switch order to reduce duplicate load
            {
                __instance.lastClickTime = -1f;
                __instance.OnThisDoubleClick();
                return false;
            }
            __instance.lastClickTime = __instance.time;

            if (__instance.fullPath != lastFilePath) // Reset when clicking on different file item
            {
                lastFilePath = __instance.fullPath;
                __instance.browser.inspector._Close(); // blueprint is reset to null
                __instance.browser.boolInspector._Close();
                if (__instance.isDirectory)
                {
                    if (Directory.Exists(__instance.fullPath))
                    {
                        __instance.bgImage.sprite = __instance.folderSprite;
                        __instance.browser.boolInspector.ResetOriginalPath(__instance.fullPath);
                    }
                    else
                    {
                        __instance.bgImage.sprite = __instance.folderSpriteGrey;
                    }
                    __instance.browser.corruptTip.SetActive(false);
                }
                if (!__instance.isDirectory)
                {
                    CreateFromFile_OnThisClick(__instance);
                }
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIBlueprintFileItem), nameof(UIBlueprintFileItem.OnThisDoubleClick))]
        public static bool UIBlueprintFileItem_OnThisDoubleClick(UIBlueprintFileItem __instance)
        {
            if (__instance.isDirectory)
            {
                if (Directory.Exists(__instance.fullPath))
                {
                    __instance.browser.SetCurrentDirectory(__instance.fullPath);
                    return false;
                }
            }
            else if (VFInput.readyToBuild)
            {
                if (BlueprintData.IsNullOrEmpty(__instance.browser.inspector.blueprint))
                {
                    UIRealtimeTip.Popup("Blueprint is not ready yet!".Translate());
                    return false;
                }
                BlueprintData blueprint = __instance.browser.inspector.blueprint.Clone(); // Make a clone to separte from UI
                GameMain.mainPlayer.controller.OpenBlueprintPasteMode(blueprint, __instance.fullPath);
                __instance.browser._Close();
            }
            return false;
        }

        static float lastClickTime;
        private static void CreateFromFile_OnThisClick(UIBlueprintFileItem __instance)
        {
            lastClickTime = Time.time;
            float time = Time.time;

            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                BlueprintData blueprintData = null;
                lock (__instance.browser.inspector) // MD5F is single-threaded
                {
                    if (lastClickTime != time) return () => {};
                    blueprintData = BlueprintData.CreateFromFile(__instance.fullPath);
                }

                return () =>
                {
                    if (lastClickTime == time)
                    {
                        if (blueprintData != null)
                        {
                            __instance.browser.inspector.SetBlueprint(blueprintData, __instance.fullPath);
                            __instance.browser.inspector._Open();
                            __instance.bgImage.sprite = __instance.fileSprite;
                            __instance.browser.corruptTip.SetActive(false);
                        }
                        else
                        {
                            __instance.bgImage.sprite = __instance.fileSpriteGrey;
                            __instance.browser.corruptTip.SetActive(true);
                        }
                    }
                    else
                    {
                        //Log.Debug("CreateFromFile_OnThisClick is outdated!");
                    }
                };
            });
        }
    }
}
