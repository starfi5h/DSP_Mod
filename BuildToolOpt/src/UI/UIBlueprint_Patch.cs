using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
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

        [HarmonyPrefix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIBlueprintFileItem), nameof(UIBlueprintFileItem.OnThisClick))]
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

        [HarmonyTranspiler, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIBlueprintInspector), nameof(UIBlueprintInspector._OnOpen))]
        public static IEnumerable<CodeInstruction> UIBlueprintInspector_OnOpen(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // code preview will use info from the file instead of generate from blueprint data
                // Change: this.Refresh(false, true, true, false);
                // To:     this.Refresh(false, true, false, false); // refreshCode = false
                // and insert UpdateCodeFromFile(this) at the end of the function

                var matcher = new CodeMatcher(instructions)
                    .MatchForward(true,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldc_I4_0),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(OpCodes.Ldc_I4_0),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Refresh"))
                    .Advance(-2)
                    .SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
                    .End()
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIBlueprint_Patch), nameof(UpdateCodeFromFile)))
                    );
                return matcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Transpiler UIBlueprintInspector._OnOpen fail!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

        static void UpdateCodeFromFile(UIBlueprintInspector __instance)
        {
            // Update the string preview by reading the first 256 charactors in the file
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
        }
    }
}
