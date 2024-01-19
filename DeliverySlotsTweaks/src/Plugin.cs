using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(DeliverySlotsTweaks.Plugin.GUID)]
[assembly: AssemblyProduct(DeliverySlotsTweaks.Plugin.NAME)]
[assembly: AssemblyVersion(DeliverySlotsTweaks.Plugin.VERSION)]

namespace DeliverySlotsTweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compatibility.CheatEnabler_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.Multfunction_mod_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.Nebula_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.DeliverySlotsTweaks";
        public const string NAME = "DeliverySlotsTweaks";
        public const string VERSION = "1.4.2";

        public static Plugin Instance;
        public static ManualLogSource Log;
        public static ConfigEntry<bool> UseLogisticSlots;
        public static ConfigEntry<bool> AutoRefillFuel;
        public static ConfigEntry<int> ColCount;
        public static ConfigEntry<int> StackSizeMultiplier;
        public static ConfigEntry<bool> DeliveryFirst;
        public static ConfigEntry<int> PlayerPackageStackSize;
        public static ConfigEntry<int> PlayerPackageStackMultiplier;
        public static ConfigEntry<bool> SortToDelieverySlots;
        public static ConfigEntry<bool> EnableHologram;
        public static ConfigEntry<bool> EnableArchitectMode;

        Harmony harmony;

        public void Start()
        {
            UseLogisticSlots = Config.Bind("DeliveryPackage", "UseLogisticSlots", true,
                "Let replicator and build tools use items in logistic slots.\n使手动制造和建筑工具可以使用物流清单内的物品");

            AutoRefillFuel = Config.Bind("DeliveryPackage", "AutoRefillFuel", false,
                "Allow fuel chamber to also take from logistics slots.\n自动补充燃料时也会使用物流清单内的物品");

            ColCount = Config.Bind("DeliveryPackage", "ColCount", 0,
                new ConfigDescription("NoChange:0 TechMax:2 Limit:5\n物流清单容量-列(不改:0 原版科技:2 最高上限:5)", new AcceptableValueRange<int>(0, 5)));

            StackSizeMultiplier = Config.Bind("DeliveryPackage", "StackSizeMultiplier", 0,
                "NoChange:0 TechMax:10\n物流清单物品堆叠倍率(不改:0 原版科技:10)");

            DeliveryFirst = Config.Bind("DeliveryPackage", "DeliveryFirst", true,
                "When logistic bots send items to mecha, send them to delivery slots first.\n配送机会优先将物品送入物流清单的栏位");

            SortToDelieverySlots = Config.Bind("DeliveryPackage", "SortToDelieverySlots", false,
                "When sorting inventory, send them to delivery slots first.\n整理背包时会先将物品送入物流清单的栏位");

            PlayerPackageStackSize = Config.Bind("PlayerPackage", "StackSize", 0,
                "Overwrite stack size in inventory. NoChange:0\n覆蓋玩家背包中的堆疊数值(每件物品皆相同)(不改:0)");

            PlayerPackageStackMultiplier = Config.Bind("PlayerPackage", "StackMultiplier", 0,
                "Apply multiplier for stack size in inventory. NoChange:0\n修改玩家背包中的物品堆疊倍率(不改:0)");

            EnableHologram = Config.Bind("BuildTool", "EnableHologram", false,
                "Ingore lack of item warning and build as white holograms\n即使物品不足也可以放置建筑虚影");

            EnableArchitectMode = Config.Bind("BuildTool", "EnableArchitectMode", false,
                "Build without requirement of items (infinite buildings)\n建筑师模式:建造无需物品");

            Instance = this;
            Log = Logger;
            harmony = new(GUID);
            Compatibility.Init(harmony);
   
            harmony.PatchAll(typeof(Plugin));
            if (UseLogisticSlots.Value)
            {
                harmony.PatchAll(typeof(DeliveryPackagePatch));
                if (AutoRefillFuel.Value)
                {
                    harmony.Patch(AccessTools.Method(typeof(Mecha), nameof(Mecha.AutoReplenishFuel)), null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(DeliveryPackagePatch), nameof(DeliveryPackagePatch.TakeItem_Transpiler))));
                    harmony.Patch(AccessTools.Method(typeof(Mecha), nameof(Mecha.AutoReplenishFuelAll)), null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(DeliveryPackagePatch), nameof(DeliveryPackagePatch.TakeItem_Transpiler))));
                }
            }
            
            if (PlayerPackageStackSize.Value > 0 || PlayerPackageStackMultiplier.Value > 0)
                harmony.PatchAll(typeof(PlayerPackagePatch));
            
            if (EnableHologram.Value)
                harmony.PatchAll(typeof(BuildToolGhost_Patch));
#if DEBUG
            ApplyConfigs();
#endif
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
            ApplyConfigs();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Import))]
        [HarmonyPatch(typeof(Player), nameof(Player.SetForNewGame))]
        internal static void ApplyConfigs()
        {
            ParameterOverwrite();
            PlayerPackagePatch.OnConfigChange();
            SetMaxDeliveryGridIndex(GameMain.mainPlayer?.deliveryPackage);
            DeliveryPackagePatch.architectMode = EnableArchitectMode.Value;
        }

        [HarmonyPostfix, HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(GameHistoryData), nameof(GameHistoryData.UnlockTechFunction))]
        static void ParameterOverwrite()
        {
            DeliveryPackage __instance = GameMain.mainPlayer.deliveryPackage;
            if (ColCount.Value > 0 && ColCount.Value <= 5)
            {
                if (__instance.colCount != ColCount.Value)
                {
                    __instance.colCount = ColCount.Value;
                    GameMain.mainPlayer.deliveryPackage.NotifySizeChange();
                }
            }
            if (StackSizeMultiplier.Value > 0)
                __instance.stackSizeMultiplier = StackSizeMultiplier.Value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DeliveryPackage), nameof(DeliveryPackage.NotifySizeChange))]
        static void SetMaxDeliveryGridIndex(DeliveryPackage __instance)
        {
            if (UseLogisticSlots.Value)
            {
                // 因為有Multifunction.player.deliveryPackage.NotifySizeChange()
                // 所以這個找尋maxDeliveryGridIndex的函式必需限定在主玩家的物流背包
                if (__instance == null || __instance != GameMain.mainPlayer?.deliveryPackage)
                    return;
                DeliveryPackagePatch.maxDeliveryGridIndex = 0;
                for (int i = __instance.gridLength - 1; i >= 0; i--)
                {
                    if (__instance.IsGridActive(i)) // max active grid index is not same as activeCount
                    {
                        DeliveryPackagePatch.maxDeliveryGridIndex = i;
                        break;
                    }
                }
                Log.LogDebug($"DeliveryPackage:{__instance.rowCount}x{__instance.colCount} stack:{__instance.stackSizeMultiplier} maxIndex:{DeliveryPackagePatch.maxDeliveryGridIndex}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerPackageUtility), nameof(PlayerPackageUtility.AddItemToAllPackages))]
        static void AddItemToAllPackages(ref bool deliveryFirst)
        {
            // Use in DispenserComponent.InternalTick
            deliveryFirst = DeliveryFirst.Value;
        }
    }
}
