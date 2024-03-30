using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(PlanetwideSpray.Plugin.NAME)]
[assembly: AssemblyVersion(PlanetwideSpray.Plugin.VERSION)]

namespace PlanetwideSpray
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.PlanetwideSpray";
        public const string NAME = "PlanetwideSpray";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);

            var ForceProliferatorLevel = Config.Bind("Cheat", "Force Proliferator Level", 0, 
                new ConfigDescription("Spray everything insert by sorters if this value > 0\n(作弊选项)当此值>0, 使分捡器抓取的货物皆为此增产等级", new AcceptableValueRange<int>(0, 10)));

            if (ForceProliferatorLevel.Value > 0)
            {
                ForceProliferatorLevel.Value = Math.Min(ForceProliferatorLevel.Value, 10);
                harmony.PatchAll(typeof(Cheat_Patch));
                Cheat_Patch.IncAbility = ForceProliferatorLevel.Value;
            }
            else
            {
                harmony.PatchAll(typeof(Main_Patch));
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
    }
}
