using System;
using System.Linq;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;

namespace BuildToolOpt
{
    // Reference: https://github.com/limoka/DSP-Mods/blob/master/Mods/BlueprintTweaks/src/BlueprintTweaks/BPPanelChanges/UIBlueprintComponentItemPatch.cs
    // Original Author: limoka

    public static class UIBlueprintComponentItem_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBlueprintComponentItem), "_OnRegEvent")]        
        public static void AddEvent(UIBlueprintComponentItem __instance)
        {
            __instance.button.onClick += GetAction(__instance.inspector);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBlueprintComponentItem), "_OnUnregEvent")]        
        public static void RemoveEvent(UIBlueprintComponentItem __instance)
        {
            __instance.button.onClick -= GetAction(__instance.inspector);
        }

        public static Action<int> GetAction(UIBlueprintInspector inspector)
        {
            void OnClick(int itemId)
            {
                UIItemPickerExtension.Popup(new Vector2(-300, 238), proto =>
                {
                    if (proto == null) return;

                    SetBuildings(inspector, itemId, proto);
                }, proto => proto.Upgrades?.Contains(itemId) == true);
            }

            return OnClick;
        }

        public static void SetBuildings(UIBlueprintInspector inspector, int oldItemId, ItemProto newItem)
        {
            foreach (BlueprintBuilding building in inspector.blueprint.buildings)
            {
                if (building.itemId == oldItemId)
                {
                    building.itemId = (short)newItem.ID;
                    building.modelIndex = (short)newItem.ModelIndex;
                }
            }

            if (inspector.usage == UIBlueprintInspector.EUsage.Browser || inspector.usage == UIBlueprintInspector.EUsage.Paste)
            {
                if (inspector.usage == UIBlueprintInspector.EUsage.Paste)
                {
                    inspector.pasteBuildTool.ResetStates();
                }
            }
            else if (inspector.usage == UIBlueprintInspector.EUsage.Copy && inspector.copyBuildTool.active)
            {
            }
            inspector.Refresh(true, true, true);
        }

    }
}
