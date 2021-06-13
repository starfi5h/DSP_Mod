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
    [BepInPlugin("com.starfi5h.plugin.SphereEditorTools", "SphereEditorTools", "0.2.0")]
    public class SphereEditorTools : BaseUnityPlugin
    {
        private Harmony harmony;

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
        public static ConfigEntry<String> KeyHideMode;

        private void BindConfig()
        {
            EnableDeleteLayer       = Config.Bind<bool>("- General -", "EnableDeleteLayer", true, "");
            EnableToolboxHotkey     = Config.Bind<bool>("- General -", "EnableToolboxHotkey", true, "");
            EnableHideLayer         = Config.Bind<bool>("- General -", "EnableHideLayer", true, "");
            EnableHideLayerOutside  = Config.Bind<bool>("- General -", "EnableHideLayerOutside", false, "");

            KeySelect               = Config.Bind<String>("Hotkey - Toolbox", "KeySelect", "1", "Select");
            KeyNode                 = Config.Bind<String>("Hotkey - Toolbox", "KeyNode", "2", "Node");
            KeyFrameGeo             = Config.Bind<String>("Hotkey - Toolbox", "KeyFrameGeo", "3", "FrameGeo");
            KeyFrameEuler           = Config.Bind<String>("Hotkey - Toolbox", "KeyFrameEuler", "4", "FrameEuler");
            KeyShell                = Config.Bind<String>("Hotkey - Toolbox", "KeyShell", "5", "Shell");
            KeyRemove               = Config.Bind<String>("Hotkey - Toolbox", "KeyRemove", "x", "Remove");
            KeyGrid                 = Config.Bind<String>("Hotkey - Toolbox", "KeyGrid", "r", "Grid");
            KeyHideMode             = Config.Bind<String>("Hotkey - Visible", "KeyHideMode", "h", "");
        }

        public void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.SphereEditorTools");
            
            BindConfig();
            Log.Init(Logger);            

            TryPatch(typeof(Hotkeys));
            if (EnableDeleteLayer.Value)
                TryPatch(typeof(EditorPanel));
            if (EnableHideLayer.Value)
                TryPatch(typeof(HideLayer));
            
            Logger.LogDebug("SphereEditorTools patch");
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
            }
        }
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            EditorPanel.Free();
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
