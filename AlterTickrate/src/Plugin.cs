using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using AlterTickrate.Patches;
using BepInEx.Configuration;

namespace AlterTickrate
{
    public class ConfigSettings
    {
        public static bool EnableFacility = true;
        public static bool EnableSorter = true;
        public static bool EnableBelt = false;

        public static int FacilityUpdatePeriod
        {
            get { return _facilityUpdatePeriod; }
            set { Facility_Patch.FacilitySpeedRate = _facilityUpdatePeriod = value; }
        }
        public static int SorterUpdatePeriod
        {
            get { return _sorterUpdatePeriod; }
            set { Inserter_Patch.InserterSpeedRate = _sorterUpdatePeriod = value; }
        }
        public static int BeltUpdatePeriod = 1;

        private static int _facilityUpdatePeriod;
        private static int _sorterUpdatePeriod;
    }

    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AlterSim";
        public const string NAME = "AlterSim";
        public const string VERSION = "0.0.1";
        public static Plugin plugin;

        Harmony harmony;
        ConfigEntry<int> Period_FacilityUpdate;
        ConfigEntry<int> Period_SorterUpdate;
        ConfigEntry<int> Period_StorageUpdate;

        public void LoadConfig()
        {
            Period_FacilityUpdate = Config.Bind<int>("Period", "FacilityUpdate", 3, "How many tick should facilities update again.\n");
            ConfigSettings.FacilityUpdatePeriod = Period_FacilityUpdate.Value;
            Period_SorterUpdate = Config.Bind<int>("Period", "SorterUpdate", 2, "How many tick should sorters update again.\n");
            ConfigSettings.SorterUpdatePeriod = Period_SorterUpdate.Value;
            Period_StorageUpdate = Config.Bind<int>("Period", "StorageUpdate", 2, "How many tick should storage/station to belt update again.\n");
            ConfigSettings.SorterUpdatePeriod = Period_StorageUpdate.Value;
        }

        public void SaveConfig()
        {
            Period_FacilityUpdate.Value = ConfigSettings.FacilityUpdatePeriod;
            Period_SorterUpdate.Value = ConfigSettings.SorterUpdatePeriod;
            Period_StorageUpdate.Value = ConfigSettings.SorterUpdatePeriod;
        }

        public void Awake()
        {
            plugin = this;
            Log.LogSource = Logger;
            harmony = new(GUID);

            LoadConfig();

            harmony.PatchAll(typeof(GameData_Patch));
            if (ConfigSettings.EnableFacility)
            {
                harmony.PatchAll(typeof(Facility_Patch));
                harmony.PatchAll(typeof(UITech_Patch));
            }
            if (ConfigSettings.EnableSorter)
            {
                harmony.PatchAll(typeof(Inserter_Patch));
            }
            if (ConfigSettings.EnableBelt)
            {
                harmony.PatchAll(typeof(CargoPath_Patch));
            }

#if DEBUG
            Init();
#else
            harmony.PatchAll(typeof(Plugin));
#endif
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnCreate))]
        internal static void Init()
        {
            Log.Debug("Initing...");
        }

        public void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (ConfigSettings.FacilityUpdatePeriod == 1)
                {
                    ConfigSettings.FacilityUpdatePeriod = 5;
                    ConfigSettings.SorterUpdatePeriod = 2;
                    ConfigSettings.BeltUpdatePeriod = 2;
                    ConfigSettings.EnableBelt = true;
                }
                else
                {
                    ConfigSettings.FacilityUpdatePeriod = 1;
                    ConfigSettings.SorterUpdatePeriod = 1;
                    ConfigSettings.BeltUpdatePeriod = 1;
                    ConfigSettings.EnableBelt = false;
                }
                Log.Debug("FacilityUpdatePeriod = " + ConfigSettings.FacilityUpdatePeriod);
            }
#endif
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            plugin = null;
        }
    }

    public static class Log
    {
        public static ManualLogSource LogSource;
        public static void Error(object obj) => LogSource.LogError(obj);
        public static void Warn(object obj) => LogSource.LogWarning(obj);
        public static void Info(object obj) => LogSource.LogInfo(obj);
        public static void Debug(object obj) => LogSource.LogDebug(obj);
    }
}
