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

        public void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.SphereEditorTools");
            try
            {
                Log.Init(Logger);
                harmony.PatchAll(typeof(EditorPanel));
                harmony.PatchAll(typeof(HideLayer));
                harmony.PatchAll(typeof(HideLayerClose));                
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            Logger.LogDebug("SphereEditorTools patch");
        }
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            EditorPanel.Free();
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
