using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonEditor.EBrushMode;

namespace SphereEditorTools
{
    class Comm
    {
        public static UIDysonEditor dysonEditor;
        static string infoString;
        static int infoCounter;

        public static int DisplayMode;     //Swarm(bit1), Star(bit2)
        public static bool SymmetricMode = true;  //switch on/off
        public static int MirrorMode;      //None(0), Equatorial(1), Antipodal(2)
        public static int RadialCount = 1; //range 1 - 90
        public static int MaxRadialCount = 90;

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void Init(UIDysonEditor __instance)
        {
            dysonEditor = __instance;
            Stringpool.Set();
            if (SphereEditorTools.EnableGUI.Value)
                UIWindow.OnOpen();

            HideLayer.SetDisplayMode(DisplayMode);
            HideLayer.SetMask(HideLayer.EnableMask);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDETopFunction), "SetDysonComboBox")]
        public static bool SetDysonComboBox(UIDETopFunction __instance)
        {
            if (!SphereEditorTools.EnableNonemptyList.Value)
                return true;

            UIComboBox dysonBox = __instance.dysonBox;
            dysonBox.Items = new List<string>();
            dysonBox.ItemsData = new List<int>();
            for (int i = 0; i < __instance.gameData.dysonSpheres.Length; i++)
            {
                DysonSphere sphere = __instance.gameData.dysonSpheres[i];
                if (sphere != null)
                {
                    if (sphere.swarm.sailCount > 0 || sphere.totalNodeCount > 0 || i == GameMain.localStar?.index)
                    {
                        dysonBox.Items.Add(sphere.starData.displayName);
                        dysonBox.ItemsData.Add(sphere.starData.index);
                    }
                }
            }
            if (dysonBox.Items.Count == 0 && __instance.gameData.dysonSpheres[0] != null)
            {
                // fail safte, add a least one dyson sphere
                dysonBox.Items.Add(__instance.gameData.dysonSpheres[0].starData.displayName);
                dysonBox.ItemsData.Add(__instance.gameData.dysonSpheres[0].starData.index);
            }
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "_OnClose")]
        public static void Free()
        {
            dysonEditor = null;
            if (SphereEditorTools.EnableGUI.Value)
                UIWindow.OnClose();
            HideLayer.Free();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "Resume")]
        public static void OnResume()
        {
            UIWindow.SaveWindowPos();
            SphereEditorTools.Config.Reload();
            Stringpool.Set();
            UIWindow.LoadWindowPos();
            //VisualEffect.LoadSequence();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            if (SphereEditorTools.EnableGUI.Value)
            {
                UIWindow.SaveWindowPos();
            }
            SphereEditorTools.Config.Save();
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

        public static void SetSymmetricMode(bool state)
        {           
            SymmetricMode = state;
            if (SymmetricMode)
                SymmetryTool.ChangeParameters(MirrorMode, RadialCount);
            else
                SymmetryTool.ChangeParameters(0, 1);
            ShowSymmetricToolStatus();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), nameof(UIDysonEditor.UpdateBrushes))]
        public static void Update_CheckKeyDown(UIDysonEditor __instance)
        {

            if (infoCounter > 0)
            {
                __instance.controlPanel.toolbox.brushError = infoString;
                infoCounter--;
            }

            if (Input.anyKeyDown)
            {
                infoCounter = 0;
                DysonSphereLayer singleSelectedLayer = __instance.selection.singleSelectedLayer;
                if (singleSelectedLayer != null)
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
                        else if (Input.GetKeyDown(SphereEditorTools.KeyGrid.Value))
                        {
                            singleSelectedLayer.drawingGridMode = (singleSelectedLayer.drawingGridMode + 1) % 5; //0: No Grid, 1: Graticule, 2~4: Geometric
                        }
                    }         
                }

                if (Input.GetKeyDown(SphereEditorTools.KeyHideMode.Value))
                {
                    DisplayMode = (DisplayMode + 1) % 4;
                    HideLayer.SetDisplayMode(DisplayMode);
                    SetInfoString(Stringpool.DisplayMode[DisplayMode], 120);
                }                

                if (SphereEditorTools.EnableSymmetryTool.Value)
                {
                    if (Input.GetKeyDown(SphereEditorTools.KeySymmetryTool.Value))
                    {
                        SetSymmetricMode(!SymmetricMode);                        
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
                        if (RadialCount < MaxRadialCount)
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
