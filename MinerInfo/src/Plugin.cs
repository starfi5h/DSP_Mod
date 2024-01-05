using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MinerInfo
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.MinerInfo";
        public const string NAME = "MinerInfo";
        public const string VERSION = "1.1.2";

        //Configs
        public static bool ShowItemsPerSecond;
        public static bool ShowItemsPerMinute;
        public static bool ShowVeinMaxMinerOutput;
        public static string VeinMaxMinerOutputText;

        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            ShowItemsPerSecond = Config.Bind("MinerInfo", "ShowItemsPerSecond", true,
                "If true, display unit per second.").Value;

            ShowItemsPerMinute = Config.Bind("MinerInfo", "ShowItemsPerMinute", false,
                "If true, display unit per minute.").Value;

            ShowVeinMaxMinerOutput = Config.Bind("MaxMinerOutput", "Enable", true,
                "Show the maximum number of items per time period output by all miners on a vein.").Value;

            VeinMaxMinerOutputText = Config.Bind("MaxMinerOutput", "Text", "Max Output:",
                "Prefix text show before numbers.").Value;

            Log = Logger;
            harmony = new(GUID);
            harmony.PatchAll(typeof(CurrentOutputPatch));
            if (ShowVeinMaxMinerOutput)
                harmony.PatchAll(typeof(MaxOutputPatch));
            harmony.PatchAll(typeof(VeinFilterPatch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
