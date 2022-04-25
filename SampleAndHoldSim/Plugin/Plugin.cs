using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SampleAndHoldSim
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compatibility.CommonAPI.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.DSPOptimizations.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.SampleAndHoldSim";
        public const string NAME = "SampleAndHoldSim";
        public const string VERSION = "0.2.0";
        public static Plugin instance;
        Harmony harmony;

        ConfigEntry<int> MaxFactoryCount;
        ConfigEntry<bool> EnableStationStorageUI;
        ConfigEntry<bool> EnableVeinConsumptionUI;

        public void LoadConfig()
        {
            MaxFactoryCount = Config.Bind<int>("General", "MaxFactoryCount", 100, "Maximum number of factories allow to active and run per tick\n每个逻辑祯所能运行的最大工厂数量");
            EnableStationStorageUI = Config.Bind<bool>("UI", "EnableStationStorageUI", true, "Display item count change rate in station storages.\n显示物流塔货物变化速率");
            EnableVeinConsumptionUI = Config.Bind<bool>("UI", "EnableVeinConsumptionUI", true, "Display mineral consumption rate of mineral.\n显示矿脉的矿物消耗速率");
            MainManager.MaxFactoryCount = MaxFactoryCount.Value;
            Log.Debug($"MaxFactoryCount:{MainManager.MaxFactoryCount} EnableStationStorageUI:{EnableStationStorageUI.Value}");
        }

        public void SaveConfig(int maxFactoryCount)
        {
            MaxFactoryCount.Value = maxFactoryCount;
        }

        public void Awake()
        {
            instance = this;
            Log.Init(Logger);
            
            LoadConfig();
            GameData_Patch.GameMain_Begin();
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GameData_Patch));
            harmony.PatchAll(typeof(ManagerLogic));
            harmony.PatchAll(typeof(UIcontrol));
            harmony.PatchAll(typeof(Dyson_Patch));

            if (EnableVeinConsumptionUI.Value)
                harmony.PatchAll(typeof(UIvein));
            if (EnableStationStorageUI.Value)
                harmony.PatchAll(typeof(UIStation));

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Compatibility.CommonAPI.GUID))
                Compatibility.CommonAPI.Init(harmony);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Compatibility.DSPOptimizations.GUID))
                Compatibility.DSPOptimizations.Init(harmony);
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            UIcontrol.OnDestory();
            UIStation.OnDestory();
        }
    }
}

