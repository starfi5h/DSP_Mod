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
        public const string VERSION = "1.1.4";

        //Configs
        public static bool ShowItemsPerSecond;
        public static bool ShowItemsPerMinute;
        public static bool ShowMinedNodesCount;
        public static string VeinMaxMinerOutputText;
        
        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            ShowItemsPerSecond = Config.Bind("MinerInfo", "ShowItemsPerSecond", false,
                "If true, display unit per second.").Value;

            ShowItemsPerMinute = Config.Bind("MinerInfo", "ShowItemsPerMinute", true,
                "If true, display unit per minute.").Value;

            ShowMinedNodesCount = Config.Bind("MinerInfo", "ShowMinedNodesCount", false,
                "If true, display (mined node/total node) for each veinGroup").Value;

            var ShowVeinMaxMinerOutput = Config.Bind("MaxMinerOutput", "Enable", true,
                "Show the maximum number of items per time period output by all miners on a vein.").Value;

            VeinMaxMinerOutputText = Config.Bind("MaxMinerOutput", "Text", "Max Output:",
                "Prefix text show before numbers.").Value;

            var ShowCurrentMinerOutput = Config.Bind("Other", "ShowCurrentMinerOutput", true,
                "Show mining efficiency in miner hovering tip").Value;

            Log = Logger;
            harmony = new(GUID);
            if (ShowCurrentMinerOutput)
                harmony.PatchAll(typeof(CurrentOutputPatch));
            if (ShowVeinMaxMinerOutput)
                harmony.PatchAll(typeof(MaxOutputPatch));
            harmony.PatchAll(typeof(VeinFilterPatch));
        }

#if DEBUG
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
#endif
    }
}
