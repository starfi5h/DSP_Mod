using UnityEngine;
using System;

namespace SphereEditorTools
{
    class UIWindow
    {
        static UIDysonEditor dysnoEditor;
        public static bool isShow;
        static bool isMin;

        static Rect normalSize = new Rect(0, 0, 250f, 220f);
        static Rect miniSize = new Rect(0, 0, 75f, 20f);
        private static Rect windowRect = new Rect(300f, 250f, 250f, 220f);
        static GUIStyle textStyle;


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

        public static void SaveWindowPos()
        {
            string posString = $"{windowRect.x}, {windowRect.y}";
            SphereEditorTools.WindowPosition.Value = posString;
        }


        public static void OnOpen(UIDysonEditor __instance)
        {
            isShow = true;
            dysnoEditor = __instance;
        }
 
        public static void OnClose()
        {
            isShow = false;
            dysnoEditor = null;
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

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box); //Display options
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

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box); //Symmetry Tool
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Stringpool.Mirror_symmetry, GUILayout.MinWidth(180));
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
                GUILayout.Label(Stringpool.Rotation_symmetry, GUILayout.MinWidth(180));
                if (GUILayout.Toggle(Comm.SymmetricMode, "", GUILayout.Width(20)) != Comm.SymmetricMode)
                    Comm.SetSymmetricMode(!Comm.SymmetricMode);
                GUILayout.EndHorizontal();
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
