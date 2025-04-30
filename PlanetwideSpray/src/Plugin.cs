using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

[assembly: AssemblyTitle(PlanetwideSpray.Plugin.NAME)]
[assembly: AssemblyVersion(PlanetwideSpray.Plugin.VERSION)]

namespace PlanetwideSpray
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.PlanetwideSpray";
        public const string NAME = "PlanetwideSpray";
        public const string VERSION = "1.1.5";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);

            var ForceProliferatorLevel = Config.Bind("Cheat", "Force Proliferator Level", 0, 
                new ConfigDescription("Spray everything insert by sorters if this value > 0\n(作弊选项)当此值>0, 使分捡器抓取的货物皆为此增产等级", new AcceptableValueRange<int>(0, 10)));

            var EnableSprayAll = Config.Bind("General", "Spray All Cargo", false,
                "Spray every item transfer by sorters (including products)\n喷涂任何分捡器抓取的货物(包含产物)");

            var EnableSprayStation = Config.Bind("General", "Spray Station Input", false,
                "Spray every item flow into station or mega assemblers(GenesisBook mod)\n喷涂流入物流塔/塔厂(创世之书mod)的货物");

            var EnableSprayFractionator = Config.Bind("General", "Spray Fractionator", true,
                "Spray every item flow into fractionator\n喷涂经过分馏塔的原料");

            var EnableSprayTurret = Config.Bind("General", "Spray Turret", true,
                "Spray every item flow into turret\n喷涂输入防御塔的弹药");

            if (ForceProliferatorLevel.Value > 0)
            {
                ForceProliferatorLevel.Value = Math.Min(ForceProliferatorLevel.Value, 10);
                harmony.PatchAll(typeof(Cheat_Patch));
                Cheat_Patch.IncAbility = ForceProliferatorLevel.Value;
                Logger.LogInfo("Cheat mode: Force Proliferator Level = " + Cheat_Patch.IncAbility);
            }
            else
            {
                Logger.LogInfo($"Normal mode: spray all [{EnableSprayAll.Value}], station input [{EnableSprayStation.Value}], fractionator [{EnableSprayFractionator.Value}], turret [{EnableSprayTurret.Value}]");
                harmony.PatchAll(typeof(Main_Patch));
                Main_Patch.LimitSpray = !EnableSprayAll.Value;
                if (EnableSprayStation.Value)
                {
                    harmony.PatchAll(typeof(Main_Patch.Station_Patch));
                }
                if (EnableSprayFractionator.Value)
                {
                    harmony.Patch(AccessTools.Method(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate)),
                        null, new HarmonyMethod(AccessTools.Method(typeof(Main_Patch), nameof(Main_Patch.FractionatorSetInc))));
                }
                if (EnableSprayTurret.Value)
                {
                    harmony.Patch(AccessTools.Method(typeof(TurretComponent), nameof(TurretComponent.InternalUpdate)),
                        null, new HarmonyMethod(AccessTools.Method(typeof(Main_Patch), nameof(Main_Patch.TurretSetInc))));
                }
#if DEBUG
                Main_Patch.SetArray();
#endif
            }
        }

#if DEBUG
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif
    }

    public class Cheat_Patch
    {
        public static int IncAbility = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
        static void AddItemInc(byte itemCount, ref byte itemInc)
        {
            itemInc = (byte)(itemCount * IncAbility);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.TryPickItemAtRear))]
        [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickItemAtRear))]
        static void TryPickItemAtRear(ref byte stack, ref byte inc)
        {
            inc = (byte)(stack * IncAbility);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InputItem))]
        static void InputItemSetInc(int stack, ref int inc)
        {
            inc = stack * IncAbility;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        static void FractionatorSetInc(ref FractionatorComponent __instance)
        {
            __instance.fluidInputInc = __instance.fluidInputCount * IncAbility;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.BeltUpdate))]
        static void TurretSetInc(ref TurretComponent __instance)
        {
            __instance.itemInc = (short)(__instance.itemCount * IncAbility);
        }
    }
}
