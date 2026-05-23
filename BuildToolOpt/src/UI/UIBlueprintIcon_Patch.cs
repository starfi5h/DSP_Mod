using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BuildToolOpt
{
    class UIBlueprintIcon_Patch
    {
        static UIBlueprintBookInspector operationInspector = null;

        [HarmonyPrefix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIBlueprintBookInspector), nameof(UIBlueprintBookInspector.OnThumbIcon0Click))]
        [HarmonyPatch(typeof(UIBlueprintBookInspector), nameof(UIBlueprintBookInspector.OnThumbIcon1Click))]
        [HarmonyPatch(typeof(UIBlueprintBookInspector), nameof(UIBlueprintBookInspector.OnThumbIcon2Click))]
        [HarmonyPatch(typeof(UIBlueprintBookInspector), nameof(UIBlueprintBookInspector.OnThumbIcon3Click))]
        [HarmonyPatch(typeof(UIBlueprintBookInspector), nameof(UIBlueprintBookInspector.OnThumbIcon4Click))]
        public static bool OnThumbIconClick_Prefix(UIBlueprintBookInspector __instance, MethodBase __originalMethod)
        {
            int index = __originalMethod.Name["OnThumbIcon".Length] - '0';
            operationInspector = __instance;
            OnThumbIconClick(index);
            return false;
        }

        private static void OnThumbIconClick(int index)
        {
            if (operationInspector.header == null)
            {
                return;
            }
            operationInspector.icon_picking_idx = index;
            if (UISignalPicker.isOpened)
            {
                UISignalPicker.Close();
                return;
            }
            UISignalPicker.Popup(new Vector2(50f, 350f), (signalId) => OnItemPickerReturn(signalId));
        }

        private static void OnItemPickerReturn(int signalId)
        {
            if (operationInspector.header == null)
            {
                return;
            }
            if (!Directory.Exists(GameConfig.blueprintFolder + operationInspector.originalPath))
            {
                return;
            }
            switch (operationInspector.icon_picking_idx)
            {
                case 0: operationInspector.header.icon0 = signalId; break;
                case 1: operationInspector.header.icon1 = signalId; break;
                case 2: operationInspector.header.icon2 = signalId; break;
                case 3: operationInspector.header.icon3 = signalId; break;
                case 4: operationInspector.header.icon4 = signalId; break;
            }
            operationInspector.icon_picking_idx = -1;
            operationInspector.RefreshInfo(false);
        }
    }
}
