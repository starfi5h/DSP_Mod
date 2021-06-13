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
    [BepInPlugin("com.starfi5h.plugin.SphereEditorTools", "SphereEditorTools", "0.3.0")]
    public class SphereEditorTools : BaseUnityPlugin
    {
        private Harmony harmony;
        string errorMessage;

        public static ConfigEntry<bool> EnableDeleteLayer;
        public static ConfigEntry<bool> EnableToolboxHotkey;
        public static ConfigEntry<bool> EnableHideLayer;
        public static ConfigEntry<bool> EnableHideLayerOutside;

        public static ConfigEntry<String> KeySelect;
        public static ConfigEntry<String> KeyNode;
        public static ConfigEntry<String> KeyFrameGeo;
        public static ConfigEntry<String> KeyFrameEuler;
        public static ConfigEntry<String> KeyShell;
        public static ConfigEntry<String> KeyRemove;
        public static ConfigEntry<String> KeyGrid;
        public static ConfigEntry<String> KeyToggleHideMode;
        public static ConfigEntry<String> KeyShowAllLayers;

        private void BindConfig()
        {
            EnableDeleteLayer       = Config.Bind<bool>("- General -", "EnableDeleteLayer", true, "Enable deletion of a constructed layer.\n启用已建立层级删除功能");
            EnableToolboxHotkey     = Config.Bind<bool>("- General -", "EnableToolboxHotkey", true, "Switch between build plan tools with hotkeys.\n启用工具箱热键");
            EnableHideLayer         = Config.Bind<bool>("- General -", "EnableHideLayer", true, "Hide unselected layers when not showing all layers.\n启用层级隐藏功能");
            EnableHideLayerOutside  = Config.Bind<bool>("- General -", "EnableHideLayerOutside", false, "Make visible changes temporarily applied to the outside world.\n使隐藏效果暂时套用至外界");

            KeySelect               = Config.Bind<String>("Hotkey - Toolbox", "KeySelect", "1", "Inspect / 查看");
            KeyNode                 = Config.Bind<String>("Hotkey - Toolbox", "KeyNode", "2", "Build Node / 修建节点");
            KeyFrameGeo             = Config.Bind<String>("Hotkey - Toolbox", "KeyFrameGeo", "3", "Build Frame(Geodesic) / 修建测地线框架");
            KeyFrameEuler           = Config.Bind<String>("Hotkey - Toolbox", "KeyFrameEuler", "4", "Build Frame(Euler) / 修建经纬度框架");
            KeyShell                = Config.Bind<String>("Hotkey - Toolbox", "KeyShell", "5", "Build Shell / 修建壳");
            KeyRemove               = Config.Bind<String>("Hotkey - Toolbox", "KeyRemove", "x", "Remove / 移除");
            KeyGrid                 = Config.Bind<String>("Hotkey - Toolbox", "KeyGrid", "r", "Toggle Grid / 切换网格");
            KeyShowAllLayers        = Config.Bind<String>("Hotkey - Visible", "KeyShowAllLayers", "`", "Show all layers / 显示所有层");
            KeyToggleHideMode       = Config.Bind<String>("Hotkey - Visible", "KeyToggleHideMode", "h", "Toogle swarm & star hide mode / 切换太阳帆与恒星隐藏模式");
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
