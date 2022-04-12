using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace MimicSimulation
{
    [BepInPlugin("com.starfi5h.plugin.MimicSimulation", "MimicSimulation", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        Harmony harmony;

        public void Awake()
        {
            Log.Init(Logger);

            harmony = new Harmony("com.starfi5h.plugin.MimicSimulation");

            GameData_Patch.GameMain_Start();
            harmony.PatchAll(typeof(GameData_Patch));
            harmony.PatchAll(typeof(ManagerLogic));
            harmony.PatchAll(typeof(UIcontrol));
            harmony.PatchAll(typeof(Dyson_Patch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            UIcontrol.OnDestory();
        }
    }
}

