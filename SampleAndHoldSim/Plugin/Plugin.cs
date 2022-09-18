using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace SampleAndHoldSim
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compatibility.NebulaAPI.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.CommonAPI.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.DSPOptimizations.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.Auxilaryfunction.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.SampleAndHoldSim";
        public const string NAME = "SampleAndHoldSim";
        public const string VERSION = "0.4.0";
        public static Plugin instance;
        Harmony harmony;

        ConfigEntry<int> MaxFactoryCount;
        ConfigEntry<int> UIStationStoragePeriod;
        ConfigEntry<int> UIVeinConsumptionPeriod;
        ConfigEntry<bool> UnitPerMinute;

        public void LoadConfig()
        {
            MaxFactoryCount = Config.Bind<int>("General", "MaxFactoryCount", 100, "Maximum number of factories allow to active and run per tick\n每个逻辑帧所能运行的最大工厂数量");
            UIStationStoragePeriod = Config.Bind<int>("UI", "UIStationStoragePeriod", 600, "Display item count change rate in station storages in x ticks. 0 = no display\n显示过去x帧内物流塔货物的流入或流出速率, 0 = 不显示");
            UIVeinConsumptionPeriod = Config.Bind<int>("UI", "UIVeinConsumptionPeriod", 1800, "Display mineral consumption rate of mineral in x ticks. 0 = no display\n显示过去x帧内矿脉的矿物消耗速率, 0 = 不显示");
            UnitPerMinute = Config.Bind<bool>("UI", "UnitPerMinute", false, "If true, show rate in unit per minute. otherwise show rate in unit per second. \ntrue: 显示单位设为每分钟速率 false: 显示每秒速率");
            MainManager.MaxFactoryCount = MaxFactoryCount.Value;
            UIstation.Period = (int)Math.Ceiling(UIStationStoragePeriod.Value / (float)UIstation.STEP);
            UIstation.UnitPerMinute = UnitPerMinute.Value;
            UIvein.Period = (int)Math.Ceiling(UIVeinConsumptionPeriod.Value / (float)UIvein.STEP);
            UIvein.UnitPerMinute = UnitPerMinute.Value;

            Log.Debug(string.Format("MaxFactoryCount:{0} StationUI:{1} VeinUI:{2} {3}",
                MainManager.MaxFactoryCount,
                UIstation.Period * UIstation.STEP,
                UIvein.Period * UIvein.STEP,
                UnitPerMinute.Value ? "/min" : "/s"
                ));
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

            if (UIstation.Period > 0)
                harmony.PatchAll(typeof(UIvein));
            if (UIvein.Period > 0)
                harmony.PatchAll(typeof(UIstation));

            Compatibility.NebulaAPI.Init(harmony);
            Compatibility.CommonAPI.Init(harmony);
            Compatibility.DSPOptimizations.Init(harmony);
            Compatibility.Auxilaryfunction.Init(harmony);
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            UIcontrol.OnDestory();
            UIstation.OnDestory();
        }
    }
}

