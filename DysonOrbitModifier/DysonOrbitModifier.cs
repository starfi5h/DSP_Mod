using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace DysonOrbitModifier
{
    [BepInPlugin("com.starfi5h.plugin.DysonOrbitModifier", "DysonOrbitModifier", "1.4.1")]
    public class DysonOrbitModifier : BaseUnityPlugin
    {
        private Harmony harmony;
        new internal static BepInEx.Configuration.ConfigFile Config;

        private static ConfigEntry<float> minRadiusMultiplier;
        private static ConfigEntry<float> maxRadiusMultiplier;
        private static ConfigEntry<float> maxAngularSpeed;
        private static ConfigEntry<bool> correctOnChange;
        private static ConfigEntry<bool> chainedRotation;
        private static ConfigEntry<string> chainedSequence;

        internal void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.DysonOrbitModifier");

            DysonOrbitUI.logger = Logger;
            SphereLogic.logger = Logger;
            Config = base.Config;
            ChainedRotation.sequenceList = new List<Tuple<int, int>>();

            minRadiusMultiplier = Config.Bind<float>("modify panel setting", "minRadiusMultiplier", 1.0f, "Multiplier of minimum radius \n最小軌道半徑的倍率");
            maxRadiusMultiplier = Config.Bind<float>("modify panel setting", "maxRadiusMultiplier", 1.0f, "Multiplier of maximum radius \n最大轨道半径的倍率");
            maxAngularSpeed = Config.Bind<float>("modify panel setting", "maxAngularSpeed", 10.0f, "Maximum rotation speed \n最大旋轉速度");
            correctOnChange = Config.Bind<bool>("modify panel setting", "correctOnChange", true, "Remove exceeding structure point/cell point right after entities are moved. \n移动物体后，立即移除超出的结构点数(SP)/细胞点数(CP)。");
            chainedRotation = Config.Bind<bool>("visual", "chainedRotation", false, "Let layers rotate chained together.\n让壳层连锁转动。可以用下面的字串指定连锁的顺序。");
            chainedSequence = Config.Bind<string>("visual", "chainedSquence", "5-4, 4-3, 3-2, 2-1", "In each pair, the rotaion of former layer (a) will apply to the latter one (b).\nFormat: layer1a-layer1b, layer2a-layer2b, ...");

            Stringpool.Set();
            ChangeSetting(null, null);
            Config.ConfigReloaded += ChangeSetting;
            harmony.PatchAll(typeof(DysonOrbitUI));
            harmony.PatchAll(typeof(ChainedRotation));
        }

        private void ChangeSetting(object sender, EventArgs eventArgs)
        {
            DysonOrbitUI.minOrbitRadiusMultiplier = minRadiusMultiplier.Value;
            DysonOrbitUI.maxOrbitRadiusMultiplier = maxRadiusMultiplier.Value;
            DysonOrbitUI.maxOrbitAngularSpeed = maxAngularSpeed.Value;
            SphereLogic.correctOnChange = correctOnChange.Value;
            ChainedRotation.sequenceList.Clear();
            ChainedRotation.enable = chainedRotation.Value;
            if (ChainedRotation.enable)
            {
                try
                {
                    string str = "ChainedRotation enable. Pair: ";
                    foreach (var x in chainedSequence.Value.Split(','))
                    {
                        var y = x.Split('-');
                        int a = int.Parse(y[0].Trim());
                        int b = int.Parse(y[y.Length - 1].Trim());
                        ChainedRotation.sequenceList.Add(new Tuple<int, int>(a, b));
                        str += $"({a}-{b})";
                    }
                    Logger.LogDebug(str);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("chainedSequence parse error");
                    Logger.LogWarning(ex);
                }
            }
            else
                Logger.LogDebug("ChainedRotation disable");
        }

        internal void OnDestroy()
        {
            DysonOrbitUI.Free();
            harmony.UnpatchSelf();  
        }
    }


}