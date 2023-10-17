using AlterTickrate.Patches;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyFileVersion(AlterTickrate.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(AlterTickrate.Plugin.VERSION)]
[assembly: AssemblyVersion(AlterTickrate.Plugin.VERSION)]
[assembly: AssemblyProduct(AlterTickrate.Plugin.NAME)]
[assembly: AssemblyTitle(AlterTickrate.Plugin.NAME)]

namespace AlterTickrate
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(Compat.ModCompatibility.DSPOptimizations.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AlterTickrate";
        public const string NAME = "AlterTickrate";
        public const string VERSION = "0.2.4";
        public static Plugin plugin;
        public static bool Enable;

        Harmony harmony;
        ConfigEntry<int> Period_PowerUpdate;
        ConfigEntry<int> Period_FacilityUpdate;
        ConfigEntry<int> Period_LabProduceUpdate;
        ConfigEntry<int> Period_LabResearchUpdate;
        ConfigEntry<int> Period_LabLiftUpdate;
        ConfigEntry<int> Period_SorterUpdate;
        ConfigEntry<int> Period_StorageUpdate;
        ConfigEntry<int> Period_BeltUpdate;
        ConfigEntry<bool> UI_SmoothProgress;

        public void LoadConfig()
        {
            Period_PowerUpdate = Config.Bind("Facility", "PowerSystem", 10, "Update power system every x ticks.\n每x帧更新一次电力系统");
            Period_FacilityUpdate = Config.Bind("Facility", "Facility", 10, "Update facilities every x ticks.\n每x帧更新一次生产设施");
            Period_LabProduceUpdate = Config.Bind("Lab", "Produce", 10, "Update producing lab every x ticks.\n每x帧更新一次生产模式的研究站");
            Period_LabResearchUpdate = Config.Bind("Lab", "Research", 10, "Update researching lab every x ticks.\n每x帧更新一次科研模式的研究站");
            Period_LabLiftUpdate = Config.Bind("Lab", "Lift", 5, "Transfer items in lab tower every x ticks.\n每x帧搬运研究站塔内的物料");
            Period_SorterUpdate = Config.Bind("Transport", "Sorter", 2, "Update sorters every x ticks.\n每x帧更新一次分拣器");
            Period_StorageUpdate = Config.Bind("Transport", "Storage", 2, "Update storage every x ticks.\n每x帧更新一次仓储");
            Period_BeltUpdate = Config.Bind("Transport", "Belt", 1, "Update belt every x ticks.(Max:2)\n每x帧更新一次传送带(最大:2)");
            UI_SmoothProgress = Config.Bind("UI", "SmoothProgress", false, "Interpolates progress animation in UI.\n利用插植使進度動畫平滑");
        }

        public void SaveBeltConfig(int beltUpdate, int storageUpdate)
        {
            Period_BeltUpdate.Value = beltUpdate;
            Period_StorageUpdate.Value = storageUpdate;
        }

        public void SaveLabConfig(int labProduceUpdate, int labResearchUpdate, int labLiftUpdate)
        {
            Period_LabProduceUpdate.Value = labProduceUpdate;
            Period_LabResearchUpdate.Value = labResearchUpdate;
            Period_LabLiftUpdate.Value = labLiftUpdate;
        }

        public void SetEnable(bool enable)
        {
            if (enable)
            {
                Parameters.SetFacilityValues(Period_PowerUpdate.Value, Period_FacilityUpdate.Value);
                Parameters.SetLabValues(Period_LabProduceUpdate.Value, Period_LabResearchUpdate.Value, Period_LabLiftUpdate.Value);
                Parameters.SetBeltValues(Period_SorterUpdate.Value, Period_StorageUpdate.Value, Period_BeltUpdate.Value);
                Enable = true;
            }
            else
            {
                Parameters.SetFacilityValues(1, 1);
                Parameters.SetLabValues(1, 1, 1);
                Parameters.SetBeltValues(1, 1, 1);
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
            Log.Info($"Facility:({Parameters.PowerUpdatePeriod}, {Parameters.FacilityUpdatePeriod}) " +
                $"Lab:({Parameters.LabProduceUpdatePeriod}, {Parameters.LabResearchUpdatePeriod}, {Parameters.LabLiftUpdatePeriod}) " +
                $"Belt:({Parameters.InserterUpdatePeriod}, {Parameters.StorageUpdatePeriod}, {Parameters.BeltUpdatePeriod})");

            harmony.PatchAll(typeof(GameData_Patch));
            if (Parameters.PowerUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(PowerSystem_Patch));
                harmony.PatchAll(typeof(DysonReqPower_Patch));
            }            
            if (Parameters.FacilityUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(Facility_Patch));
            }
            if (Parameters.LabProduceUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(LabProduce_Patch));
            }
            if (Parameters.LabResearchUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(LabResearch_Patch));
            }
            if (Parameters.LabLiftUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(LabLift_Patch));
            }
            if (Parameters.InserterUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(Inserter_Patch));
            }
            if (Parameters.BeltUpdatePeriod > 1)
            {
                harmony.PatchAll(typeof(CargoPath_Patch));
            }
            if (UI_SmoothProgress.Value)
            {
                harmony.PatchAll(typeof(UIProgress_Patch));
            }
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
