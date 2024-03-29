﻿using UnityEngine;
using System;

namespace SphereEditorTools
{
    class UIWindow
    {
        public static bool isShow;
        static bool isMin;

        static Rect normalSize = new Rect(0, 0, 240f, 200f);
        static Rect miniSize = new Rect(0, 0, 75f, 20f);
        private static Rect windowRect = new Rect(300f, 250f, 240f, 200f);
        static GUIStyle textStyle;

        public static string SpeedInput;

        public static void LoadWindowPos()
        {
            try
            {
                var token = SphereEditorTools.WindowPosition.Value.Split(',');
                int x = int.Parse(token[0].Trim());
                int y = int.Parse(token[token.Length - 1].Trim());
                windowRect.x = x;
                windowRect.y = y;
            }
            catch (Exception ex)
            {
                Log.LogWarning("WindowPos parse error, use defualt position (300, 250)");
                Log.LogWarning(ex);
            }
        }

        public static void LoadWindowSize()
        {
            try
            {
                var token = SphereEditorTools.WindowSize.Value.Split(',');
                int width = int.Parse(token[0].Trim());
                int height = int.Parse(token[token.Length - 1].Trim());
                normalSize.width = width;
                normalSize.height = height;
                Log.LogDebug($"window size: {width},{height}");
            }
            catch (Exception ex)
            {
                Log.LogWarning("WindowPos parse error, use defualt size (240, 200)");
                Log.LogWarning(ex);
                normalSize.width = 240f;
                normalSize.height = 200f;
            }
            windowRect.width = normalSize.width;
            windowRect.height = normalSize.height;
        }

        public static void SaveWindowPos()
        {
            string posString = $"{windowRect.x}, {windowRect.y}";
            SphereEditorTools.WindowPosition.Value = posString;
        }


        public static void OnOpen()
        {
            isShow = true;
            SpeedInput = "";
        }
 
        public static void OnClose()
        {
            isShow = false;
        }

        public static void OnGUI()
        {
            if (isMin)
                windowRect = GUI.Window(1297890671, windowRect, MinWindowFun, Stringpool.Tool);
            else
                windowRect = GUI.Window(1297890671, windowRect, NormalWindowFun, Stringpool.Tool);
            EatInputInRect(windowRect);
        }

        static void MinWindowFun(int id)
        {
            GUILayout.BeginArea(new Rect(10f, 1f, 40f, 16f));
            GUILayout.Label(Stringpool.Tool);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(windowRect.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("▼"))
            {
                isMin = false;
                windowRect.height = normalSize.height;
                windowRect.width = normalSize.width;
                windowRect.x -= normalSize.width - miniSize.width;
            }
            GUILayout.EndArea();
            GUI.DragWindow();
        }

        static void NormalWindowFun(int id)
        {
            bool tmpBool;
            int tmpInt;
            string tmpString;

            GUILayout.BeginArea(new Rect(windowRect.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("▲"))
            {
                isMin = true;
                windowRect.height = miniSize.height;
                windowRect.width = miniSize.width;
                windowRect.x += normalSize.width - miniSize.width;
                return;
            }
            GUILayout.EndArea();

            if (SphereEditorTools.EnableDisplayOptions.Value)
            {
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Stringpool.Display_options);
                    tmpBool = Comm.DisplayMode < 2;
                    if (tmpBool != GUILayout.Toggle(tmpBool, Stringpool.Star))
                    {
                        Comm.DisplayMode ^= 2;
                        HideLayer.SetDisplayMode(Comm.DisplayMode);
                    }
                    if (HideLayer.EnableMask != GUILayout.Toggle(HideLayer.EnableMask, Stringpool.Mask))
                    {
                        HideLayer.SetMask(!HideLayer.EnableMask);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            if (SphereEditorTools.EnableSymmetryTool.Value)
            {
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box); //Symmetry Tool
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Stringpool.SymmetryTool, GUILayout.MinWidth(180));
                    if (GUILayout.Toggle(Comm.SymmetricMode, "", GUILayout.Width(20)) != Comm.SymmetricMode)
                        Comm.SetSymmetricMode(!Comm.SymmetricMode);
                    GUILayout.EndHorizontal();
                    tmpInt = GUILayout.Toolbar(Comm.MirrorMode, Stringpool.MirrorModes);
                    if (tmpInt != Comm.MirrorMode)
                    {
                        Comm.MirrorMode = tmpInt;
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                        Comm.SymmetricMode = true;
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<<"))
                    {
                        Comm.RadialCount = Math.Max(Comm.RadialCount - 10, 1);
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                        Comm.SymmetricMode = true;
                    }
                    if (GUILayout.Button("-"))
                    {
                        if (Comm.RadialCount > 1)
                            SymmetryTool.ChangeParameters(Comm.MirrorMode, --Comm.RadialCount);
                        Comm.SymmetricMode = true;
                    }
                    if (textStyle == null)
                    {
                        textStyle = new GUIStyle(GUI.skin.textField)
                        {
                            alignment = TextAnchor.MiddleCenter
                        };
                    }
                    tmpString = GUILayout.TextField(Comm.RadialCount.ToString(), 3, textStyle, GUILayout.MinWidth(35));
                    if (int.TryParse(tmpString, out tmpInt))
                    {
                        if (tmpInt != Comm.RadialCount && tmpInt < Comm.MaxRadialCount && tmpInt > 0)
                        {
                            Comm.RadialCount = tmpInt;
                            SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                            Comm.SymmetricMode = true;
                        }
                    }
                    if (GUILayout.Button("+"))
                    {
                        if (Comm.RadialCount < Comm.MaxRadialCount)
                            SymmetryTool.ChangeParameters(Comm.MirrorMode, ++Comm.RadialCount);
                        Comm.SymmetricMode = true;
                    }
                    if (GUILayout.Button(">>"))
                    {
                        Comm.RadialCount = Math.Min(Comm.RadialCount + 10, Comm.MaxRadialCount);
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                        Comm.SymmetricMode = true;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            if (SphereEditorTools.EnableOrbitTool.Value)
            {
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box); //Orbit Tool
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Stringpool.OrbitTool);
                    tmpBool = EditOrbit.EnableAnchorMode;
                    if (tmpBool != GUILayout.Toggle(tmpBool, Stringpool.AnchorMode))
                    {
                        EditOrbit.Toggle(!tmpBool);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Stringpool.AngularSpeed);
                    SpeedInput = GUILayout.TextField(SpeedInput, 7, GUILayout.MinWidth(35));
                    if (GUILayout.Button(Stringpool.Set1))
                    {
                        if (float.TryParse(SpeedInput, out float tmpFloat) && tmpFloat >= 0f)
                        {
                            EditOrbit.TrySetAngularSpeed(tmpFloat);
                        }
                        else if (SpeedInput == "")
                        {
                            EditOrbit.TrySetAngularSpeed(-1);
                        }
                        else
                        {
                            SpeedInput = EditOrbit.AuglarSpeed.ToString();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            GUI.DragWindow();
        }

        public static void EatInputInRect(Rect eatRect)
        {
            if (!(Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) //Eat only when left-click
                return;
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }

    }

}
