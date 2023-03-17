using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using AlterTickrate.Patches;
using BepInEx.Configuration;

namespace AlterTickrate
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compat.ModCompatibility.DSPOptimizations.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AlterTickrate";
        public const string NAME = "AlterTickrate";
        public const string VERSION = "0.1.0";
        public static Plugin plugin;
        public static bool Enable;

        Harmony harmony;
        ConfigEntry<int> Period_FacilityUpdate;
        ConfigEntry<int> Period_SorterUpdate;
        ConfigEntry<int> Period_StorageUpdate;
        ConfigEntry<int> Period_BeltUpdate;

        public void LoadConfig()
        {
            Period_FacilityUpdate = Config.Bind("Period", "FacilityUpdate", 5, "Update facilities every x ticks.\n每x帧更新一次电力与生产设施");
            Period_SorterUpdate = Config.Bind("Period", "SorterUpdate", 2, "Update sorters every x ticks.\n每x帧更新一次分拣器");
            Period_StorageUpdate = Config.Bind("Period", "StorageUpdate", 2, "Update storage every x ticks.\n每x帧更新一次仓储");
            Period_BeltUpdate = Config.Bind("Period", "BeltUpdate", 2, "Update belt every x ticks.(Max:2)\n每x帧更新一次传送带(最大:2)");
        }

        public void SaveConfig(int beltUpdate, int storageUpdate)
        {
            Period_BeltUpdate.Value = beltUpdate;
            Period_StorageUpdate.Value = storageUpdate;
        }

        public void SetEnable(bool enable)
        {
            if (enable)
            {
                Parameters.SetValues(Period_FacilityUpdate.Value, Period_SorterUpdate.Value, Period_StorageUpdate.Value, Period_BeltUpdate.Value);
                Enable = true;
            }
            else
            {
                Parameters.SetValues(1, 1, 1, 1);
                Enable = false;
            }
        }

        public void Awake()
        {
            plugin = this;
            Log.LogSource = Logger;
            harmony = new(GUID);

            LoadConfig();
            if (!Compat.ModCompatibility.Init(harmony))
                return;
            SetEnable(true);
            Log.Info($"Parameters: {Period_FacilityUpdate.Value}, {Period_SorterUpdate.Value}, {Period_StorageUpdate.Value}, {Period_BeltUpdate.Value}");

            if (Parameters.BeltUpdatePeriod > 1)
            {
                Log.Debug("Patch cargo path");
                harmony.PatchAll(typeof(CargoPath_Patch));
            }
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
