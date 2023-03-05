using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using AlterTickrate.Patches;
using BepInEx.Configuration;

namespace AlterTickrate
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AlterSim";
        public const string NAME = "AlterSim";
        public const string VERSION = "0.0.1";
        public static Plugin plugin;
        public static bool Enable;

        Harmony harmony;
        ConfigEntry<bool> General_Enable;
        ConfigEntry<int> Period_FacilityUpdate;
        ConfigEntry<int> Period_SorterUpdate;
        ConfigEntry<int> Period_StorageUpdate;

        public void LoadConfig()
        {
            General_Enable = Config.Bind<bool>("General", "Enable", true, "Enable alter tick.\n");
            Period_FacilityUpdate = Config.Bind<int>("Period", "FacilityUpdate", 5, "How long should facilities update.\n");
            Period_SorterUpdate = Config.Bind<int>("Period", "SorterUpdate", 2, "How long should sorters update.\n");
            Period_StorageUpdate = Config.Bind<int>("Period", "StorageUpdate", 2, "How long should storage update.\n");
            Log.Info($"Parameters: {Period_FacilityUpdate.Value}, {Period_SorterUpdate.Value}, {Period_StorageUpdate.Value}");
        }

        public void SetEnable(bool enable)
        {
            if (enable)
            {
                Parameters.SetValues(Period_FacilityUpdate.Value, Period_SorterUpdate.Value);
                Enable = true;
            }
            else
            {
                Parameters.SetValues(1, 1);
                Enable = false;
            }
        }

        public void Awake()
        {
            plugin = this;
            Log.LogSource = Logger;
            harmony = new(GUID);

            LoadConfig();
            SetEnable(General_Enable.Value);

            harmony.PatchAll(typeof(GameData_Patch));
            harmony.PatchAll(typeof(Facility_Patch));
            harmony.PatchAll(typeof(PowerSystem_Patch));
            harmony.PatchAll(typeof(Inserter_Patch));
            harmony.PatchAll(typeof(UITech_Patch));
            harmony.PatchAll(typeof(UIcontrol));
        }


#if DEBUG
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                SetEnable(!Enable);
                Log.Debug("FacilityUpdatePeriod = " + Parameters.FacilityUpdatePeriod);
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            plugin = null;
            UIcontrol.OnDestory();
        }
#endif
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
