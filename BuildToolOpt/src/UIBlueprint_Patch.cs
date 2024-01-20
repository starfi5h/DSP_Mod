using BepInEx;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace BuildToolOpt
{
    class UIBlueprint_Patch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(BlueprintData), nameof(BlueprintData.Clone))]
        public static bool BlueprintData_Clone(BlueprintData __instance, ref BlueprintData __result)
        {
            // Optimize BlueprintData.Clone to use only export and import
            __result = new BlueprintData();
            __result.HeaderFromBase64String(__instance.headerStr);
            using (var memoryStream = new MemoryStream())
            {
                using var binaryWriter = new BinaryWriter(memoryStream);
                __instance.Export(binaryWriter);
                binaryWriter.Flush();
                memoryStream.Position = 0;

                using var binaryReader = new BinaryReader(memoryStream);
                __result.Import(binaryReader);
            }

            if (!__result.isValid)
            {
                Plugin.Log.LogWarning("BlueprintData Clone is invalid!");
                __result = null;
            }
            return false;
        }        

        [HarmonyPrefix, HarmonyPatch(typeof(UIBlueprintFileItem), nameof(UIBlueprintFileItem.OnThisClick))]
        public static bool UIBlueprintFileItem_OnThisClick(UIBlueprintFileItem __instance)
        {
            if (__instance.time - __instance.lastClickTime < (0.5f * Time.timeScale)) // Adjust time to fit speed change mods
            {
                VFAudio.Create("ui-click-0", null, Vector3.zero, true, 1, -1, -1L);
                __instance.browser.inspector._Close(); // blueprint is reset to null
                __instance.browser.boolInspector._Close();
                __instance.lastClickTime = -1f;
                __instance.OnThisDoubleClick(); // Shortcut to prevent the duplicated load
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnOpen))]
        public static bool UIBlueprintInspector_OnOpen(UIBlueprintInspector __instance)
        {
            __instance.player.package.onStorageChange += __instance.OnPlayerPackageChange;
            __instance.Refresh(false, true, false); // code preview will use info of the file instead of generate from blueprint data

            __instance.shareLengthText.text = "";
            __instance.shareCodeText.text = "";
            string fullPath = GameConfig.blueprintFolder + __instance.newPath + ".txt";
            if (File.Exists(fullPath))
            {
                try
                {
                    var fi = new FileInfo(fullPath);
                    __instance.shareLengthText.text = string.Format("几字节".Translate(), fi.Length);

                    char[] buffer = new char[256];
                    using (var reader = new StreamReader(fullPath))
                    {
                        reader.ReadBlock(buffer, 0, buffer.Length);
                    }
                    __instance.shareCodeText.text = new string(buffer);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogWarning("UIBlueprintInspector_OnOpen: " + e);
                }
            }

            return false;
        }
    }
}
