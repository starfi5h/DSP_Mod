using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonPanel.EBrushMode;

namespace SphereEditorTools
{
    class Comm : BaseUnityPlugin
    {
        public static UIDysonPanel dysnoPanel;
        static string infoString;
        static int infoCounter;

        public static int DisplayMode;     //0~3
        public static bool SymmetricMode;  //switch on/off
        public static int MirrorMode;      //0~2
        public static int RadialCount = 1; //range 1 - 60

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void Init(UIDysonPanel __instance)
        {
            dysnoPanel = __instance;
            Stringpool.Set();
            UIWindow.OnOpen(dysnoPanel);
            DeleteLayer.OnOpen(dysnoPanel);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void Free()
        {
            dysnoPanel = null;
            UIWindow.OnClose();
            DeleteLayer.OnClose();
        }


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
                 str = String.Format("{0} {1}:{2,-2} {3}:{4}", Stringpool.SymmetricTool,
                    Stringpool.Rotation, RadialCount, Stringpool.Mirror, Stringpool.MirrorModes[MirrorMode]);                
            }
            else
            {
                str = String.Format("{0} OFF", Stringpool.SymmetricTool);
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
                
                    if (Input.GetKeyDown(SphereEditorTools.KeyLayerCopy.Value))
                    {
                        Log.LogDebug("copy!");
                        if (CopyLayer.TryCopy(dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected)))
                            SetInfoString("Copy successed!", 120);
                        else
                            SetInfoString("Copy failed!", 120);

                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyLayerPaste.Value))
                    {
                        Log.LogDebug("paste");
                        CopyLayer.TryPaste(dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected), 0);
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
                        DisplayMode = (DisplayMode + 1) % 4;
                        HideLayer.SetDisplayMode(DisplayMode);
                    }
                }

                if (SphereEditorTools.EnableSymmetryTool.Value)
                {
                    if (Input.GetKeyDown(SphereEditorTools.KeySymmetryTool.Value))
                    {
                        SymmetricMode = !SymmetricMode;
                        if (SymmetricMode)
                            SymmetryTool.ChangeParameters(MirrorMode, RadialCount);
                        else
                            SymmetryTool.ChangeParameters(0, 1);
                        ShowSymmetricToolStatus();

                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyMirroring.Value))
                    {
                        SymmetricMode = true;
                        MirrorMode = (MirrorMode + 1) % 3;
                        SymmetryTool.ChangeParameters(MirrorMode, RadialCount);
                        ShowSymmetricToolStatus();
                    }
                    
                    else if (Input.GetKeyDown(SphereEditorTools.KeyRotationInc.Value))
                    {
                        SymmetricMode = true;
                        if (RadialCount < 60)
                            SymmetryTool.ChangeParameters(MirrorMode, ++RadialCount);
                        ShowSymmetricToolStatus();
                    }
                    else if (Input.GetKeyDown(SphereEditorTools.KeyRotationDec.Value))
                    {
                        SymmetricMode = true;
                        if (RadialCount > 1)
                            SymmetryTool.ChangeParameters(MirrorMode, --RadialCount);
                        ShowSymmetricToolStatus();
                    }
                }
            }
        }
    }
}
