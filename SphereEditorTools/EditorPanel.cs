using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonPanel.EBrushMode;

namespace SphereEditorTools
{

    class Hotkeys : BaseUnityPlugin
    {
        /*
        static string[] keys;
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void UIDysonPanel__OnOpen(UIDysonPanel __instance)
        {
            keys = new string[6] {
                SphereEditorTools.KeySelect.Value,     //0
                SphereEditorTools.KeyNode.Value,       //1
                SphereEditorTools.KeyFrameGeo.Value,   //2
                SphereEditorTools.KeyFrameEuler.Value, //3
                SphereEditorTools.KeyShell.Value,      //4
                SphereEditorTools.KeyRemove.Value      //5
            };
        }
        */

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "UpdateToolbox")]
        public static void UIDysonPanel_UpdateToolbox(UIDysonPanel __instance)
        {
            if (Input.anyKeyDown)
            {
                if (__instance.toolbox.activeSelf)
                {
                    if (SphereEditorTools.EnableToolboxHotkey.Value)
                    {
                        if (Input.GetKeyDown(SphereEditorTools.KeySelect.Value))
                        {
                            __instance.brushMode = BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyNode.Value))
                        {
                            __instance.brushMode = __instance.brushMode != BrushMode.Node ? BrushMode.Node : BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyFrameGeo.Value))
                        {
                            __instance.brushMode = __instance.brushMode != BrushMode.FrameGeo ? BrushMode.FrameGeo : BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyFrameEuler.Value))
                        {
                            __instance.brushMode = __instance.brushMode != BrushMode.FrameEuler ? BrushMode.FrameEuler : BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyShell.Value))
                        {
                            __instance.brushMode = __instance.brushMode != BrushMode.Shell ? BrushMode.Shell : BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyRemove.Value))
                        {
                            __instance.brushMode = __instance.brushMode != BrushMode.Remove ? BrushMode.Remove : BrushMode.Select;
                        }
                        else if (Input.GetKeyDown(SphereEditorTools.KeyGrid.Value))
                        {
                            DysonSphereLayer layer = __instance.viewDysonSphere?.GetLayer(__instance.layerSelected);
                            if (layer != null)
                                layer.gridMode = (layer.gridMode + 1) % 3; //0: No Grid, 1: Graticule, 2: Geometric
                        }
                    }
                }
                if (SphereEditorTools.EnableHideLayer.Value)
                {
                    if (Input.GetKeyDown(SphereEditorTools.KeyHideMode.Value))
                        HideLayer.ToggleMode();

                }
            }
        }

    }



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

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectedLayerUI")] //Use posfix in case other mods want to patch it
        public static void UIDysonPanel_UpdateSelectedLayerUI(UIDysonPanel __instance)
        {
            __instance.layerRmvButton.button.interactable = true; //Ignore Layer nodes checking in Update(), layerRmvButton.button.interactable always true
        }        

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectionVisibleChange")]
        public static void UIDysonPanel_UpdateSelectionVisibleChange(UIDysonPanel __instance)
        {
            __instance.layerRmvButton.button.interactable = (__instance.layerSelected > 0); //Enable to remove objects on layer 1
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
                    "删除".Translate() + " " + TranslateString.Layer, //Remove Layer
                    "删除".Translate() + " " + TranslateString.Layer + " [" + dysnoPanel.layerSelected + "] ?",
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
