using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace MinerInfo
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.MinerInfo";
        public const string NAME = "MinerInfo";
        public const string VERSION = "1.0.0";

        //Configs
        public static bool ShowItemsPerSecond;
        public static bool ShowVeinMaxMinerOutput;

        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            ShowItemsPerSecond = Config.Bind("MinerInfo", "ShowItemsPerSecond", true,
                "If true, display unit per second. If false, display unit per minute.").Value;

            ShowVeinMaxMinerOutput = Config.Bind("MinerInfo", "ShowVeinMaxMinerOutput", true,
                "Show the maximum number of items per time period output by all miners on a vein.").Value;

            Log = Logger;
            harmony = new(GUID);
            harmony.PatchAll(typeof(Plugin));
            if (ShowVeinMaxMinerOutput)
                harmony.PatchAll(typeof(MaxOutputPatch));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        public static void UIEntityBriefInfo_OnUpdate(UIEntityBriefInfo __instance)
        {
            if (__instance.frame % 4 == 0) 
            {
                ItemProto itemProto = __instance.entityInfo.itemProto;
                PrefabDesc prefabDesc = itemProto?.prefabDesc;
                if (prefabDesc.minerType > 0) 
                {
                    int minerId = __instance.factory.entityPool[__instance.entityId].minerId;
                    ref var miner = ref __instance.factory.factorySystem.minerPool[minerId];

                    if (miner.type == EMinerType.Vein)
                    {
                        float maxOutput = 60.0f / miner.period * miner.speed * GameMain.history.miningSpeedScale * miner.veinCount;
                        float ratio = miner.workstate <= EWorkState.Idle ? 0f : miner.speedDamper * (float)__instance.entityInfo.powerConsumerRatio;
                        float currentRate = maxOutput * ratio;

                        if (ShowItemsPerSecond)
                            __instance.entityNameText.text += $"  {currentRate:F2}/s  ({ratio:P0})";
                        else
                            __instance.entityNameText.text += $"  {currentRate * 60:F1}/min  ({ratio:P0})";
                    }
                }
            }
        }

    }

    public class MaxOutputPatch
    {
        static float[] maxArr = new float[0];

        [HarmonyPostfix, HarmonyPatch(typeof(UIVeinDetail), "_OnUpdate")]
        public static void UIVeinDetail_OnUpdate(UIVeinDetail __instance)
        {
            // Update every 60 tick
            if (VFInput.inFullscreenGUI || GameMain.gameTick % 60 != 0)
                return;

            PlanetFactory factory = __instance.inspectPlanet?.factory;
            if (factory == null || !__instance.inspectPlanet.factoryLoaded)
                return;

            if (maxArr.Length != factory.veinGroups.Length)
            {
                maxArr = new float[factory.veinGroups.Length];
                //Log.LogDebug("Set veinGroupsLength to " + factory.veinGroups.Length);
            }
            else
            {
                Array.Clear(maxArr, 0, maxArr.Length);
            }

            for (int i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                ref var miner = ref factory.factorySystem.minerPool[i];
                if (miner.id == i && miner.veinCount > 0)
                {
                    float maxOutput = 60.0f / miner.period * miner.speed * GameMain.history.miningSpeedScale * miner.veinCount;
                    // Assume miner veins belong to the same group
                    int groupIndex = factory.veinPool[miner.veins[0]].groupIndex;
                    maxArr[groupIndex] += maxOutput;
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static bool UIVeinDetailNode_OnUpdate(UIVeinDetailNode __instance)
        {
            if (__instance.inspectFactory == null)
            {
                __instance._Close();
                return false;
            }
            VeinGroup veinGroup = __instance.inspectFactory.veinGroups[__instance.veinGroupIndex];
            if (veinGroup.count == 0 || veinGroup.type == EVeinType.None)
            {
                __instance._Close();
                return false;
            }
            if (__instance.counter % 4 == 0 && (__instance.showingAmount != veinGroup.amount || __instance.counter % 60 == 0))
            {
                __instance.showingAmount = veinGroup.amount;
                if (veinGroup.type != EVeinType.Oil)
                {
                    __instance.infoText.text = string.Concat(new string[]
                    {
                    veinGroup.count.ToString(),
                    "空格个".Translate(),
                    __instance.veinProto.name,
                    "储量".Translate(),
                    __instance.AmountString(veinGroup.amount),
                    MinerInfoString(__instance.veinGroupIndex)
                    });
                }
                else
                {
                    __instance.infoText.text = string.Concat(new string[]
                    {
                    veinGroup.count.ToString(),
                    "空格个".Translate(),
                    __instance.veinProto.name,
                    "产量".Translate(),
                    (veinGroup.amount * VeinData.oilSpeedMultiplier).ToString("0.0000"),
                    "/s"
                    });
                }
            }
            __instance.counter++;
            return false;
        }

        public static string MinerInfoString(int veinGroupIndex)
        {
            if (veinGroupIndex >= maxArr.Length || maxArr[veinGroupIndex] == 0f)
                return "";

            if (Plugin.ShowItemsPerSecond)
                return string.Format("\nMax Output: {0:F1}/s", maxArr[veinGroupIndex]);
            else
                return string.Format("\nMax Output: {0:F0}/min", maxArr[veinGroupIndex] * 60f);
        }   
    }
}
