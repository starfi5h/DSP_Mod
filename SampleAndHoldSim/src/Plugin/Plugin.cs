using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace SampleAndHoldSim
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compatibility.CommonAPI.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.Multfunction_mod_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.PlanetMiner.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.CheatEnabler_Patch.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SampleAndHoldSim";
        public const string NAME = "SampleAndHoldSim";
        public const string VERSION = "0.7.0";
        public static Plugin instance;
        Harmony harmony;

        public ConfigEntry<int> UpdatePeriod;
        public ConfigEntry<bool> FocusLocalFactory;
        public ConfigEntry<int> SliderMaxUpdatePeriod;
        public ConfigEntry<int> UIStationStoragePeriod;
        public ConfigEntry<bool> UnitPerMinute;
        public ConfigEntry<bool> WarnIncompat;
        public ConfigEntry<bool> EnableRelayLanding;
        //public ConfigEntry<bool> EnableKillStatsFuzze;

        public void LoadConfig()
        {
            UpdatePeriod = Config.Bind("General", "UpdatePeriod", 10, "Compute actual factory simulation every x ticks.\n更新周期: 每x逻辑帧运行一次实际计算");
            FocusLocalFactory = Config.Bind("General", "FocusLocalFactory", true, "Let local planet factory always active.使本地工厂保持每帧运行\n");
            SliderMaxUpdatePeriod = Config.Bind("UI", "SliderMaxUpdatePeriod", 20, "Max value of upate period slider\n更新周期滑动条的最大值");
            UIStationStoragePeriod = Config.Bind("UI", "UIStationStoragePeriod", 600, "Display item count change rate in station storages in x ticks. 0 = no display\n显示过去x帧内物流塔货物的流入或流出速率, 0 = 不显示");
            UnitPerMinute = Config.Bind("UI", "UnitPerMinute", false, "If true, show rate in unit per minute. otherwise show rate in unit per second. \ntrue: 显示单位设为每分钟速率 false: 显示每秒速率");
            WarnIncompat = Config.Bind("UI", "WarnIncompat", true, "Show warning for incompatible mods\n显示不兼容mod的警告");
            EnableRelayLanding = Config.Bind("Combat", "EnableRelayLanding", true, "Allow Dark Fog relay to land on planet.\n允许黑雾中继器登陆星球");
            //EnableKillStatsFuzze = Config.Bind("Combat", "EnableKillStatsFuzze", false, "Allow kill count not precise to improve performance\n允许击杀统计不精确以提高性能");

            if (UpdatePeriod.Value < 1) UpdatePeriod.Value = 1;
            MainManager.UpdatePeriod = UpdatePeriod.Value;
            MainManager.FocusLocalFactory = FocusLocalFactory.Value;
            UIcontrol.SliderMax = SliderMaxUpdatePeriod.Value;
            UIstation.Period = (int)Math.Ceiling(UIStationStoragePeriod.Value / (float)UIstation.STEP);
            UIstation.UnitPerMinute = UnitPerMinute.Value;

            Log.Debug(string.Format("UpdatePeriod:{0} StationUI:{1}",
                MainManager.UpdatePeriod,
                UIstation.Period * UIstation.STEP
                ));
            if (!EnableRelayLanding.Value) Log.Info("Disable Dark Fog relay landing");
        }

        public void SaveConfig(int updatePeriod, bool focusLocalFactory)
        {
            UpdatePeriod.Value = updatePeriod;
            FocusLocalFactory.Value = focusLocalFactory;
        }

        public void Awake()
        {
            instance = this;
            harmony = new Harmony(GUID);
            Log.Init(Logger);
            LoadConfig();

            GameLogic_Patch.GameMain_Begin();
            harmony.PatchAll(typeof(GameLogic_Patch));
            harmony.PatchAll(typeof(ThreadManager_Patch));
            harmony.PatchAll(typeof(GameTick_Patch));
            harmony.PatchAll(typeof(ManagerLogic));
            harmony.PatchAll(typeof(UIcontrol));
            harmony.PatchAll(typeof(Station_Patch));
            harmony.PatchAll(typeof(Dyson_Patch));
            harmony.PatchAll(typeof(Ejector_Patch));
            harmony.PatchAll(typeof(Combat_Patch));
            harmony.PatchAll(typeof(Fix_Patch));

            if (UIstation.Period > 0)
                harmony.PatchAll(typeof(UIstation));

            harmony.PatchAll(typeof(KillStatLogic2));
        }

        public void Start()
        {
            // At the timing of Start, all plugins should be loaded in Chainloader.PluginInfos
            Compatibility.Init(harmony);
        }

#if DEBUG
        public void OnDestroy()
        {
            Compatibility.OnDestory();
            harmony.UnpatchSelf();
            UIcontrol.OnDestory();
            UIstation.OnDestory();
        }
#endif

    }
}
