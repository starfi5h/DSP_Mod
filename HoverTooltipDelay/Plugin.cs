using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HoverTooltipDelay
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.HoverTooltipDelay";
        public const string NAME = "HoverTooltipDelay";
        public const string VERSION = "1.0.0";
        public static Plugin instance;
        public static ManualLogSource log;
        Harmony harmony;        

        public void LoadConfig()
        {
            var delayFrame = Config.Bind("General", "DelayFrame", 15, "Time delay for tooltip to show up when mouse hovering on a building.");
            var keyFastFillin = Config.Bind("Hotkey", "FastFillin", KeyCode.Tab, "Hotkey to transfer group of items to the selecting buildings.");
            Patch.SetConfig(delayFrame.Value, keyFastFillin.Value);
        }

        public void Awake()
        {
            instance = this;
            LoadConfig();
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Patch));
            log = Logger;
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
