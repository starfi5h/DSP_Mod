using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace DeliverySlotsTweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.DeliverySlotsTweaks";
        public const string NAME = "DeliverySlotsTweaks";
        public const string VERSION = "1.0.0";

        public static Plugin Instance;
        public static ManualLogSource Log;
        public static ConfigEntry<int> ColCount;
        public static ConfigEntry<int> StackSizeMultiplier;
        public static ConfigEntry<bool> DeliveryFirst;

        Harmony harmony;

        public void Awake()
        {
            ColCount = Config.Bind("DeliveryPackage", "ColCount", 0,
                new ConfigDescription("NoChange:0 TechMax:2 Limit:5\n物流清单容量-列(不变:0 原版科技:2 最高上限:5)", new AcceptableValueRange<int>(0, 5)));

            StackSizeMultiplier = Config.Bind("DeliveryPackage", "StackSizeMultiplier", 0,
                "NoChange:0 TechMax:10\n物流清单物品堆叠倍率(不变:0 原版科技:10)");

            DeliveryFirst = Config.Bind("DeliveryPackage", "DeliveryFirst", true,
                "When logistic bots send items to mecha, send them to delivery slots first.\n配送机会优先将物品送入物流清单的栏位");

            Instance = this;
            Log = Logger;
            harmony = new(GUID);

            harmony.PatchAll(typeof(DeliveryPackagePatches));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
