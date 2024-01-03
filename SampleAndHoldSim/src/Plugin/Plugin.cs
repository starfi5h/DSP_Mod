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
    [BepInDependency(Compatibility.Multfunction_mod_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.PlanetMiner.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.CheatEnabler_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SampleAndHoldSim";
        public const string NAME = "SampleAndHoldSim";
        public const string VERSION = "0.6.3";
        public static Plugin instance;
        Harmony harmony;

        ConfigEntry<int> UpdatePeriod;
        ConfigEntry<bool> FocusLocalFactory;
        ConfigEntry<int> SliderMaxUpdatePeriod;
        ConfigEntry<int> UIStationStoragePeriod;
        ConfigEntry<bool> UnitPerMinute;

        public void LoadConfig()
        {
            UpdatePeriod = Config.Bind("General", "UpdatePeriod", 5, "Compute actual factory simulation every x ticks.\n更新周期: 每x逻辑帧运行一次实际计算");
            FocusLocalFactory = Config.Bind("General", "FocusLocalFactory", true, "Let local planet factory always active.使本地工厂保持每帧运行\n");
            SliderMaxUpdatePeriod = Config.Bind("UI", "SliderMaxUpdatePeriod", 10, "Max value of upate period slider\n更新周期滑动条的最大值");
            UIStationStoragePeriod = Config.Bind("UI", "UIStationStoragePeriod", 600, "Display item count change rate in station storages in x ticks. 0 = no display\n显示过去x帧内物流塔货物的流入或流出速率, 0 = 不显示");
            UnitPerMinute = Config.Bind("UI", "UnitPerMinute", false, "If true, show rate in unit per minute. otherwise show rate in unit per second. \ntrue: 显示单位设为每分钟速率 false: 显示每秒速率");
            
            MainManager.UpdatePeriod = UpdatePeriod.Value;
            MainManager.FocusLocalFactory = FocusLocalFactory.Value;
            UIcontrol.SliderMax = SliderMaxUpdatePeriod.Value;
            UIstation.Period = (int)Math.Ceiling(UIStationStoragePeriod.Value / (float)UIstation.STEP);
            UIstation.UnitPerMinute = UnitPerMinute.Value;

            Log.Debug(string.Format("UpdatePeriod:{0} StationUI:{1}",
                MainManager.UpdatePeriod,
                UIstation.Period * UIstation.STEP
                ));
        }

        public void SaveConfig(int updatePeriod, bool focusLocalFactory)
        {
            UpdatePeriod.Value = updatePeriod;
            FocusLocalFactory.Value = focusLocalFactory;
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
            harmony.PatchAll(typeof(EnemyUnitComponent_Patch));

            if (UIstation.Period > 0)
                harmony.PatchAll(typeof(UIstation));

            Compatibility.Init(harmony);
        }

        public void OnDestroy()
        {
            Compatibility.OnDestory();
            harmony.UnpatchSelf();
            UIcontrol.OnDestory();
            UIstation.OnDestory();
        }
    }
}

