using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SphereEditorTools
{
    class EditorPanel : BaseUnityPlugin
    {
        public static UIDysonPanel dysnoPanel;
        public static int storedLayerId;


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void Free()
        {
            dysnoPanel = null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void UIDysonPanel__OnOpen(UIDysonPanel __instance)
        {
            dysnoPanel = __instance;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectedLayerUI")]
        public static bool UIDysonPanel_UpdateSelectedLayerUI()
        {
            return false; //Skip Layer nodes checking in Update(), layerRmvButton.button.interactable remains same
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectionVisibleChange")]
        public static void UIDysonPanel_UpdateSelectionVisibleChange(UIDysonPanel __instance)
        {
            __instance.layerRmvButton.button.interactable = (__instance.layerSelected > 0); //Enable remove objects on layer 1
            if (dysnoPanel != null)
                Log.LogDebug($"{dysnoPanel.layerSelected}");
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnShellLayerRemoveClick")]
        public static bool OnShellLayerRemoveClick(int obj)
        {

            DysonSphereLayer layer = dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected);
            if (layer != null && (layer.nodeCount > 0 || layer.frameCount > 0 || layer.shellCount > 0))
            {
                dysnoPanel.brushMode = UIDysonPanel.EBrushMode.None;
                storedLayerId = dysnoPanel.layerSelected; //layerSelected may change by brush select
                
                var messagebox = UIMessageBox.Show(
                    "删除".Translate() + TranslateString.layer, //Remove Layer
                    "删除".Translate() + " " + TranslateString.layer + " [" + dysnoPanel.layerSelected + "] ?",
                    "否".Translate(), "是".Translate(), 2,
                    null, new UIMessageBox.Response(RemoveLayer));
                var go = GameObject.Find("UI Root/Always on Top/Overlay Canvas - Top/Dyson Editor Top");
                if (go != null)
                {
                    messagebox.transform.SetParent(go.transform); //move mssagebox on top
                    messagebox.transform.localPosition = new Vector3(0f, 0f, 0f);
                }                
                return false;
            }
            return true; //preserve original method in case of messagebox fail 
        }

        public static void RemoveLayer()
        {
            GameMain.UnlockFullscreenPauseOneFrame();
            DysonSphereLayer layer = dysnoPanel.viewDysonSphere.GetLayer(storedLayerId);
            Log.LogDebug($"Remove layer {layer.id}");
            if (layer != null)
            {

                for (int index = 1; index < layer.shellCursor; ++index)
                {
                    DysonShell shell = layer.shellPool[index];
                    if (shell != null && shell.id == index)
                        layer.RemoveDysonShell(shell.id);
                }

                for (int index = 1; index < layer.frameCursor; ++index)
                {
                    DysonFrame frame = layer.framePool[index];
                    if (frame != null && frame.id == index)
                        layer.RemoveDysonFrame(frame.id);
                }

                for (int index = 1; index < layer.nodeCursor; ++index)
                {
                    DysonNode node = layer.nodePool[index];
                    if (node != null && node.id == index)
                        layer.RemoveDysonNode(node.id);
                }             
                if (layer.id > 1)
                {
                    dysnoPanel.viewDysonSphere.RemoveLayer(layer);                    
                }
                dysnoPanel.layerSelected = 0;
                dysnoPanel.nodeSelected = 0;
                dysnoPanel.frameSelected = 0;
                dysnoPanel.shellSelected = 0;
                dysnoPanel.UpdateSelectionVisibleChange();
            }
        }

    }
}
