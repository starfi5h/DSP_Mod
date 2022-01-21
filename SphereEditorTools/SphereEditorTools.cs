using System;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SphereEditorTools
{
    [BepInPlugin("com.starfi5h.plugin.SphereEditorTools", "SphereEditorTools", "2.0.0")]
    public class SphereEditorTools : BaseUnityPlugin
    {
        Harmony harmony;
        internal static string ErrorMessage;
        internal static new ConfigFile Config;
        public static ConfigEntry<bool> EnableToolboxHotkey;
        public static ConfigEntry<bool> EnableDisplayOptions;
        public static ConfigEntry<bool> EnableSymmetryTool;
        public static ConfigEntry<bool> EnableOrbitTool;
        public static ConfigEntry<bool> EnableGUI;
        public static ConfigEntry<string> WindowPosition;
        public static ConfigEntry<string> KeySelect;
        public static ConfigEntry<string> KeyNode;
        public static ConfigEntry<string> KeyFrameGeo;
        public static ConfigEntry<string> KeyFrameEuler;
        public static ConfigEntry<string> KeyShell;
        public static ConfigEntry<string> KeyGrid;        
        public static ConfigEntry<string> KeyShowAllLayers;
        public static ConfigEntry<string> KeyHideMode;
        public static ConfigEntry<string> KeySymmetryTool;
        public static ConfigEntry<string> KeyMirroring;
        public static ConfigEntry<string> KeyRotationInc;
        public static ConfigEntry<string> KeyRotationDec;

        private void BindConfig()
        {            
            EnableToolboxHotkey     = Config.Bind<bool>("- General -", "EnableToolboxHotkey", true, "Switch between build plan tools with hotkeys.\n启用工具箱热键");
            EnableDisplayOptions    = Config.Bind<bool>("- General -", "EnableDisplayOptions", true, "Enable display control of star and black mask.\n啟用顯示控制(恆星/黑色遮罩)");
            EnableSymmetryTool      = Config.Bind<bool>("- General -", "EnableSymmetryTool", true, "Enable mirror and rotation symmetry of building tools.\n启用对称建造工具(镜像/旋转)");
            EnableOrbitTool         = Config.Bind<bool>("- General -", "EnableOrbitTool", true, "Enable dyson sphere layer orbit modifiy tool.\n启用殼層軌道工具");

            EnableGUI = Config.Bind<bool>("GUI", "EnableGUI", true, "Show a simple window to use the tools. \n启用图形操作窗口");
            WindowPosition          = Config.Bind<string>("GUI", "WindowPosition", "300, 250", "Position of the window. Format: x,y\n窗口的位置 格式: x,y");

            KeySelect               = Config.Bind<string>("Hotkeys - Toolbox", "KeySelect", "1", "Inspect / 查看");
            KeyNode                 = Config.Bind<string>("Hotkeys - Toolbox", "KeyNode", "2", "Build Node / 修建节点");
            KeyFrameGeo             = Config.Bind<string>("Hotkeys - Toolbox", "KeyFrameGeo", "3", "Build Frame(Geodesic) / 修建测地线框架");
            KeyFrameEuler           = Config.Bind<string>("Hotkeys - Toolbox", "KeyFrameEuler", "4", "Build Frame(Euler) / 修建经纬度框架");
            KeyShell                = Config.Bind<string>("Hotkeys - Toolbox", "KeyShell", "5", "Build Shell / 修建壳");
            KeyGrid                 = Config.Bind<string>("Hotkeys - Toolbox", "KeyGrid", "r", "Toggle Grid / 切换网格");

            KeyHideMode             = Config.Bind<string>("Hotkeys - Visibility", "KeyHideMode", "h", "Toggle mask & star display mode / 切换遮罩與恒星显示模式");

            KeySymmetryTool         = Config.Bind<string>("Hotkeys - Symmetry Tool", "KeySymmetryTool", "tab", "Toggle symmetry tool / 开关对称建造工具");
            KeyMirroring            = Config.Bind<string>("Hotkeys - Symmetry Tool", "KeyMirroring", "m", "Toggle mirroring mode / 切换镜像对称模式");
            KeyRotationInc          = Config.Bind<string>("Hotkeys - Symmetry Tool", "KeyRotationInc", "[+]", "Increase the degree of rotational symmetry / 增加旋转对称的个数");
            KeyRotationDec          = Config.Bind<string>("Hotkeys - Symmetry Tool", "KeyRotationDec", "[-]", "Decrease the degree of rotational symmetry / 减少旋转对称的个数");
           

        }

        public void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.SphereEditorTools");
            Config = base.Config;
            BindConfig();
            Log.Init(Logger);
            
            TryPatch(typeof(Comm));
            if (EnableDisplayOptions.Value)
                TryPatch(typeof(HideLayer));
            if (EnableSymmetryTool.Value)
                TryPatch(typeof(SymmetryTool));
            if (EnableOrbitTool.Value)
                TryPatch(typeof(EditOrbit));

            if (EnableGUI.Value)
            {
                int height = 20 + (EnableDisplayOptions.Value ? 30 : 0) + (EnableSymmetryTool.Value ? 110 : 0) + (EnableOrbitTool.Value ? 60 : 0);
                UIWindow.LoadWindowPos(height);
            }
        }

        public void TryPatch(Type type)
        {
            try
            {
                harmony.PatchAll(type);
            }
            catch (Exception e)
            {
                Logger.LogError($"Patch {type.Name} error");
                Logger.LogError(e);
                throw new Exception($"SphereEditorTools: {type.Name} patch error. Disable this function in the config file.\n" + e.ToString());
            }
        }

        //readonly static HighStopwatch watch = new HighStopwatch();
        public void OnGUI()
        {
            //watch.Begin();
            if (UIWindow.isShow)
                UIWindow.OnGUI();
            //Log.LogPeriod($"OnGUI: {watch.duration,10}");
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            HideLayer.Free();
            SymmetryTool.Free();
        }
    }
    public static class Log
    {
        private static ManualLogSource _logger;
        public static void Init(ManualLogSource logger) =>
            _logger = logger;
        public static void LogError(object obj) =>
            _logger.LogError(obj);
        public static void LogWarning(object obj) =>
            _logger.LogWarning(obj);
        public static void LogInfo(object obj) =>
            _logger.LogInfo(obj);
        public static void LogDebug(object obj) =>
            _logger.LogDebug(obj);

        public static void LogPeriod(object obj)
        {
            if (GameMain.gameTick % 600 == 0)
                _logger.LogDebug(obj);
        }
    }
}
