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
        public const string VERSION = "1.0.0";
        public static Plugin Instance;
        Harmony harmony;

        public static ConfigEntry<bool> MuteInBackground;
        public static ConfigEntry<string> MuteBuildingIds;

        public void LoadConfig()
        {
            MuteInBackground = Config.Bind("- General -", "Mute In Background", true, "Whether to mute the game when in the background, i.e. alt-tabbed.");
            MuteBuildingIds = Config.Bind("- General -", "Mute Building Ids", "", "The ids of building to mute, separated by comma");
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

        public static void LogWarn(object data) => Instance.Logger.LogWarning(data);
        public static void LogInfo(object data) => Instance.Logger.LogInfo(data);
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
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        [HarmonyAfter("me.xiaoye97.plugin.Dyson.LDBTool")]
        public static void InvokeOnLoadWorkEnded()
        {
            MuteBuildings();
        }

        public static void MuteBuildings()
        {
            foreach (var str in Plugin.MuteBuildingIds.Value.Split(','))
            {
                if (int.TryParse(str, out int itemId))
                {
                    var item = LDB.items.Select(itemId);
                    Plugin.LogInfo($"Mute {itemId} {item.Name.Translate()}");
                    int modelIndex = item.ModelIndex;
                    LDB.models.Select(modelIndex).prefabDesc.audioVolume = 0;
                }
                else
                {
                    Plugin.LogWarn($"Can't parse {str} to int");
                }
            }
        }
    }
}
