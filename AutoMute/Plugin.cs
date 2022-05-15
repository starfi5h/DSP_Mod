using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Diagnostics;
using UnityEngine;

namespace AutoMute
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AutoMute";
        public const string NAME = "AutoMute";
        public const string VERSION = "0.1.0";
        public static Plugin Instance;
        Harmony harmony;

        public static ConfigEntry<bool> MuteInBackground;
        public static ConfigEntry<bool> WindTurbine;
        public static ConfigEntry<bool> RayReceiver;

        public void LoadConfig()
        {
            MuteInBackground = Config.Bind("- General -", "Mute In Background", true, "Whether to mute the game when in the background, i.e. alt-tabbed.");
            WindTurbine = Config.Bind("Building Audio - Power", "Wind Turbine", true);
            RayReceiver = Config.Bind("Building Audio - Power", "Ray Receiver", true);
        }

        public void Awake()
        {
            LoadConfig();
            Instance = this;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Patch));
        }

        [Conditional("DEBUG")]
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [Conditional("DEBUG")]
        public static void Log(object data)
        {
            Instance.Logger.LogDebug(data);
        }
    }

    class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GlobalObject), "OnApplicationFocus")]
        public static void OnApplicationFocus(bool focus)
        {
            if (Plugin.MuteInBackground.Value)
            {
                AudioListener.pause = !focus;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioData), "BindToObject")]
        public static void BindToObject(ref AudioData __instance, EObjectType _objType, PrefabDesc _pdesc)
        {
            if (_objType == EObjectType.Entity)
            {
                if (_pdesc.isPowerGen)
                {
                    if (_pdesc.windForcedPower && !Plugin.WindTurbine.Value)
                    {
                        __instance.volume = 0;
                    }
                    else if (_pdesc.gammaRayReceiver && !Plugin.RayReceiver.Value)
                    {
                        __instance.volume = 0;
                    }
                }
            }
        }
    }
}
