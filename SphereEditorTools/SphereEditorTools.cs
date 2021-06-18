using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SphereEditorTools
{
    [BepInPlugin("com.starfi5h.plugin.SphereEditorTools", "SphereEditorTools", "1.0.0")]
    public class SphereEditorTools : BaseUnityPlugin
    {
        private Harmony harmony;
        string errorMessage;

        public static ConfigEntry<bool> EnableDeleteLayer;
        public static ConfigEntry<bool> EnableToolboxHotkey;
        public static ConfigEntry<bool> EnableHideLayer;
        public static ConfigEntry<bool> EnableHideOutside;
        public static ConfigEntry<bool> EnableSymmetryTool;

        public static ConfigEntry<String> KeySelect;
        public static ConfigEntry<String> KeyNode;
        public static ConfigEntry<String> KeyFrameGeo;
        public static ConfigEntry<String> KeyFrameEuler;
        public static ConfigEntry<String> KeyShell;
        public static ConfigEntry<String> KeyRemove;
        public static ConfigEntry<String> KeyGrid;        
        public static ConfigEntry<String> KeyShowAllLayers;
        public static ConfigEntry<String> KeyHideMode;
        public static ConfigEntry<String> KeySymmetryTool;
        public static ConfigEntry<String> KeyMirroring;
        public static ConfigEntry<String> KeyRotationInc;
        public static ConfigEntry<String> KeyRotationDec;


        private void BindConfig()
        {
            EnableDeleteLayer       = Config.Bind<bool>("- General -", "EnableDeleteLayer", true, "Enable deletion of a constructed layer.\n启用已建立层级删除功能");
            EnableToolboxHotkey     = Config.Bind<bool>("- General -", "EnableToolboxHotkey", true, "Switch between build plan tools with hotkeys.\n启用工具箱热键");
            EnableHideLayer         = Config.Bind<bool>("- General -", "EnableHideLayer", true, "Hide unselected layers when not showing all layers.\n启用层级隐藏功能");
            EnableHideOutside       = Config.Bind<bool>("- General -", "EnableHideOutside", false, "Apply visibility changes to the game world temporarily.\n使隐藏效果暂时套用至外界");
            EnableSymmetryTool      = Config.Bind<bool>("- General -", "EnableSymmetryTool", true, "Enable mirror and rotation symmetry of building tools.\n启用对称建造工具(镜像/旋转)");


            KeySelect               = Config.Bind<String>("Hotkeys - Toolbox", "KeySelect", "1", "Inspect / 查看");
            KeyNode                 = Config.Bind<String>("Hotkeys - Toolbox", "KeyNode", "2", "Build Node / 修建节点");
            KeyFrameGeo             = Config.Bind<String>("Hotkeys - Toolbox", "KeyFrameGeo", "3", "Build Frame(Geodesic) / 修建测地线框架");
            KeyFrameEuler           = Config.Bind<String>("Hotkeys - Toolbox", "KeyFrameEuler", "4", "Build Frame(Euler) / 修建经纬度框架");
            KeyShell                = Config.Bind<String>("Hotkeys - Toolbox", "KeyShell", "5", "Build Shell / 修建壳");
            KeyRemove               = Config.Bind<String>("Hotkeys - Toolbox", "KeyRemove", "x", "Remove / 移除");
            KeyGrid                 = Config.Bind<String>("Hotkeys - Toolbox", "KeyGrid", "r", "Toggle Grid / 切换网格");

            KeyShowAllLayers        = Config.Bind<String>("Hotkeys - Visibility", "KeyShowAllLayers", "`", "Toggle show all layers mode / 显示所有层");
            KeyHideMode             = Config.Bind<String>("Hotkeys - Visibility", "KeyHideMode", "h", "Toggle swarm & star hide mode / 切换太阳帆与恒星隐藏模式");

            KeySymmetryTool         = Config.Bind<String>("Hotkeys - Symmetry Tool", "KeySymmetryTool", "tab", "Toggle symmetry tool / 开关对称建造工具");
            KeyMirroring            = Config.Bind<String>("Hotkeys - Symmetry Tool", "KeyMirroring", "m", "Toggle mirroring / 开关镜像对称");
            KeyRotationInc          = Config.Bind<String>("Hotkeys - Symmetry Tool", "KeyRotationInc", "[+]", "Increase rotational symmetry level / 增加旋转对称的个数");
            KeyRotationDec          = Config.Bind<String>("Hotkeys - Symmetry Tool", "KeyRotationDec", "[-]", "Decrease rotational symmetry level / 减少旋转对称的个数");

        }

        public void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.SphereEditorTools");
            
            BindConfig();
            Log.Init(Logger);
            errorMessage = "";

            TryPatch(typeof(Comm));
            if (EnableDeleteLayer.Value)
                TryPatch(typeof(DeleteLayer));
            if (EnableHideLayer.Value)
                TryPatch(typeof(HideLayer));
            if (EnableSymmetryTool.Value)
                TryPatch(typeof(SymmetryTool));

            if (errorMessage != "")
            {
                errorMessage = "Load Error: " + errorMessage;
                Comm.SetInfoString(errorMessage, 600);
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
                errorMessage += type.Name + " ";
                Logger.LogError($"Patch {type.Name} error");
                Logger.LogError(e);
            }
        }
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            DeleteLayer.Free();
            HideLayer.Free("Unpatch");
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
    }
}
