using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonPanel.EBrushMode;

namespace SphereEditorTools
{
    class Comm : BaseUnityPlugin
    {
        static string infoString;
        static int infoCounter;

        public static bool SymmetricMode; //switch on/off
        static bool mirrorMode;    //switch on/off
        static int radialCount = 1;    //range 1 - 60

        public static void SetInfoString(string str, int counter)
        {
            infoString = str;
            infoCounter = counter;
        }

        public static void ShowSymmetricToolStatus()
        {
            string str;
            if (SymmetricMode)
            {
                 str = String.Format("{0} {1}:{2,-2} {3}:{4}", TranslateString.SymmetricTool, 
                    TranslateString.Rotation, radialCount, TranslateString.Mirror, mirrorMode ? "ON" : "OFF");                
            }
            else
            {
                str = String.Format("{0} OFF", TranslateString.SymmetricTool);
            }
            SetInfoString(str, 120);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateOthers")]
        public static void Update_CheckKeyDown(UIDysonPanel __instance)
        {

            if (infoCounter > 0)
            {
                __instance.brushError = infoString;
                infoCounter--;
            }

            if (Input.anyKeyDown)
            {
                infoCounter = 0;
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

                    if (Input.GetKeyDown(SphereEditorTools.KeyShowAllLayers.Value))
                    {
                        __instance.showAllLayers = !__instance.showAllLayers;
                        __instance.UpdateSelectionVisibleChange();
                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyHideMode.Value))
                    {
                        HideLayer.ToggleMode();
                    }
                }

                if (SphereEditorTools.EnableSymmetryTool.Value)
                {
                    if (Input.GetKeyDown(SphereEditorTools.KeySymmetryTool.Value))
                    {
                        SymmetricMode = !SymmetricMode;
                        if (SymmetricMode)
                            SymmetryTool.ChangeParameters(mirrorMode, radialCount);
                        else
                            SymmetryTool.ChangeParameters(false, 1);
                        ShowSymmetricToolStatus();

                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyMirroring.Value))
                    {
                        SymmetricMode = true;
                        mirrorMode = !mirrorMode;
                        SymmetryTool.ChangeParameters(mirrorMode, radialCount);
                        ShowSymmetricToolStatus();
                    }
                    
                    else if (Input.GetKeyDown(SphereEditorTools.KeyRotationInc.Value))
                    {
                        SymmetricMode = true;
                        if (radialCount < 60)
                            SymmetryTool.ChangeParameters(mirrorMode, ++radialCount);
                        ShowSymmetricToolStatus();
                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyRotationDec.Value))
                    {
                        SymmetricMode = true;
                        if (radialCount > 1)
                            SymmetryTool.ChangeParameters(mirrorMode, --radialCount);
                        ShowSymmetricToolStatus();
                    }
                }
            }
        }
    }
}
