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
        public const string VERSION = "1.1.1";

        public static Plugin Instance;
        public static ManualLogSource Log;
        public static ConfigEntry<int> ColCount;
        public static ConfigEntry<int> StackSizeMultiplier;
        public static ConfigEntry<bool> DeliveryFirst;
        public static ConfigEntry<int> PlayerPackageStackSize;
        public static ConfigEntry<int> PlayerPackageStackMultiplier;

         Harmony harmony;

        public void Awake()
        {
            ColCount = Config.Bind("DeliveryPackage", "ColCount", 0,
                new ConfigDescription("NoChange:0 TechMax:2 Limit:5\n物流清单容量-列(不变:0 原版科技:2 最高上限:5)", new AcceptableValueRange<int>(0, 5)));

            StackSizeMultiplier = Config.Bind("DeliveryPackage", "StackSizeMultiplier", 0,
                "NoChange:0 TechMax:10\n物流清单物品堆叠倍率(不变:0 原版科技:10)");

            DeliveryFirst = Config.Bind("DeliveryPackage", "DeliveryFirst", true,
                "When logistic bots send items to mecha, send them to delivery slots first.\n配送机会优先将物品送入物流清单的栏位");

            PlayerPackageStackSize = Config.Bind("PlayerPackage", "StackSize", 0,
                "Overwrite stacksize in inventory. NoChange:0\n覆蓋玩家背包中的物品堆疊数量(不变:0)");

            PlayerPackageStackMultiplier = Config.Bind("PlayerPackage", "StackMultiplier", 0,
                "Apply multiplier for stack count in inventory. NoChange:0\n修改玩家背包中的物品堆疊倍率(不变:0)");

            Instance = this;
            Log = Logger;
            harmony = new(GUID);

            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(DeliveryPackagePatch));
            if (PlayerPackageStackSize.Value > 0 || PlayerPackageStackMultiplier.Value > 0)
                harmony.PatchAll(typeof(PlayerPackagePatch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.OnApplyClick))]
        internal static void OnApplyClick()
        {
            Instance.Config.Reload(); // Reload config file when clicking 'Apply' in game settings
            OnConfigChange();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Import))]
        [HarmonyPatch(typeof(Player), nameof(Player.SetForNewGame))]
        internal static void OnConfigChange()
        {
            DeliveryPackagePatch.ParameterOverwrite(GameMain.mainPlayer.deliveryPackage);
            PlayerPackagePatch.OnConfigChange();
        }
    }
}
