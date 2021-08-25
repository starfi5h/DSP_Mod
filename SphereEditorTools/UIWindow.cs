using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SphereEditorTools
{
    class UIWindow : BaseUnityPlugin
    {
        static UIDysonPanel dysnoPanel;
        static bool isShow;
        static bool isMin;

        static Rect normalSize = new Rect(0, 0, 250f, 275f);
        static Rect miniSize = new Rect(0, 0, 75f, 20f);
        private static Rect windowRect = new Rect(100f, 100f, 250f, 275f);
        static HighStopwatch watch = new HighStopwatch();
        static int count;

        public static void OnGUI()
        {
            watch.Begin();

            if (isShow)
            {
                if (isMin)
                    windowRect = GUI.Window(1297890671, windowRect, MinWindowFun, Stringpool.Tool);
                else
                    windowRect = GUI.Window(1297890671, windowRect, NormalWindowFun, Stringpool.Tool);
            }
            
            count = count + 1;
            if (count == 600)
            {
                Log.LogDebug($"OnGUI: {watch.duration,10}");
                count = 0;
            }
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

        static string stringToEdit = "";


        static void NormalWindowFun(int id)
        {
            bool tmpBool;
            int tmpInt;
            string tmpString;

            #region upper-right buttons
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
            #endregion

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            {
                GUILayout.Label(Stringpool.Display_options);
                GUILayout.BeginHorizontal();

                tmpBool = GUILayout.Toggle(Comm.DisplayMode % 2 == 0, Stringpool.Swarm);
                if (tmpBool != (Comm.DisplayMode % 2 == 0)) 
                {
                    Comm.DisplayMode ^= 1;
                    HideLayer.SetDisplayMode(Comm.DisplayMode);
                }
                tmpBool = GUILayout.Toggle(Comm.DisplayMode < 2, Stringpool.Star);
                if (tmpBool != (Comm.DisplayMode < 2))
                {
                    Comm.DisplayMode ^= 2;
                    HideLayer.SetDisplayMode(Comm.DisplayMode);
                }
                tmpBool = GUILayout.Toggle(true, "Mask");
                if (tmpBool)
                {
                    //TODO: black-mask
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            {
                GUILayout.Label(Stringpool.Mirror_symmetry);
                tmpInt = GUILayout.Toolbar(Comm.MirrorMode, Stringpool.MirrorModes);
                if (tmpInt != Comm.MirrorMode)
                {
                    Comm.MirrorMode = tmpInt;
                    SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                }
                GUILayout.Label(Stringpool.Rotation_symmetry);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<<"))
                {
                    Comm.RadialCount = 1;
                    SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                }
                if (GUILayout.Button("-"))
                {
                    if (Comm.RadialCount > 1)
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, --Comm.RadialCount);
                }
                tmpString = GUILayout.TextField(Comm.RadialCount.ToString(), 3, GUILayout.MinWidth(35));
                if (int.TryParse(tmpString, out tmpInt))
                {
                    if (tmpInt != Comm.RadialCount && tmpInt < 60 && tmpInt > 0)
                    {
                        Comm.RadialCount = tmpInt;
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                    }
                }
                if (GUILayout.Button("+"))
                {
                    if (Comm.RadialCount < 60)
                        SymmetryTool.ChangeParameters(Comm.MirrorMode, ++Comm.RadialCount);
                }
                if (GUILayout.Button(">>"))
                {
                    Comm.RadialCount = 60;
                    SymmetryTool.ChangeParameters(Comm.MirrorMode, Comm.RadialCount);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
            {
                GUILayout.Label(Stringpool.Layer_copying);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Stringpool.Copy))
                {
                    Log.LogDebug("Copy");
                    CopyLayer.TryCopy(dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected));
                }

                if (GUILayout.Button(Stringpool.Clear))
                    Log.LogDebug("Clear");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Stringpool.Paste1))
                {
                    Log.LogDebug("Paste1");
                    CopyLayer.TryPaste(dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected), 0);
                }
                if (GUILayout.Button(Stringpool.Paste2))
                {
                    Log.LogDebug("Paste2");
                    CopyLayer.TryPaste(dysnoPanel.viewDysonSphere.GetLayer(dysnoPanel.layerSelected), 1);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnUpdate")]
        public static void DysonSphere_Export_Prefix(HighStopwatch __state)
        {
            if (count == 10)
                Log.LogDebug($"{__state.duration,10}s");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "_OnUpdate")]
        public static void DysonSphere_Import_Prefix(out HighStopwatch __state)
        {
            __state = new HighStopwatch();
            __state.Begin();
        }


        public static void OnOpen(UIDysonPanel __instance)
        {
            isShow = true;
            dysnoPanel = __instance;
        }

        public static void OnClose()
        {
            isShow = false;
            dysnoPanel = null;
        }

    }
}
