using HarmonyLib;
using System;
using System.Text;

namespace MinerInfo
{
    public class MaxOutputPatch
    {
        static float[] maxArr = new float[0];
        static readonly StringBuilder stringBuilder = new();

        [HarmonyPostfix, HarmonyPatch(typeof(UIVeinDetail), "_OnUpdate")]
        static void UIVeinDetail_OnUpdate(UIVeinDetail __instance)
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
        static bool UIVeinDetailNode_OnUpdate(UIVeinDetailNode __instance)
        {
            if (__instance.inspectFactory == null)
            {
                __instance._Close();
                return false;
            }
            ref var veinGroup = ref __instance.inspectFactory.veinGroups[__instance.veinGroupIndex];
            if (veinGroup.count == 0 || veinGroup.type == EVeinType.None)
            {
                __instance._Close();
                return false;
            }
            if (__instance.counter % 4 == 0 && (__instance.showingAmount != veinGroup.amount || __instance.counter % 60 == 0))
            {
                __instance.showingAmount = veinGroup.amount;
                stringBuilder.Clear();
                if (veinGroup.type != EVeinType.Oil)
                {
                    stringBuilder.Append(veinGroup.count);
                    stringBuilder.Append("空格个".Translate());
                    stringBuilder.Append(__instance.veinProto.name);
                    stringBuilder.Append("储量".Translate());
                    stringBuilder.Append(__instance.AmountString(veinGroup.amount));
                    MinerInfoString(__instance.veinGroupIndex);
                    __instance.infoText.text = stringBuilder.ToString();
                }
                else
                {
                    stringBuilder.Append(veinGroup.count);
                    stringBuilder.Append("空格个".Translate());
                    stringBuilder.Append(__instance.veinProto.name);
                    stringBuilder.Append("产量".Translate());
                    stringBuilder.Append((veinGroup.amount * VeinData.oilSpeedMultiplier).ToString("0.0000"));
                    stringBuilder.Append("/s");
                    __instance.infoText.text = stringBuilder.ToString();
                }
            }
            __instance.counter++;
            return false;
        }

        static void MinerInfoString(int veinGroupIndex)
        {
            if (veinGroupIndex >= maxArr.Length || maxArr[veinGroupIndex] == 0f)
                return;

            stringBuilder.AppendLine();
            stringBuilder.Append(Plugin.VeinMaxMinerOutputText);
            if (Plugin.ShowItemsPerSecond)
                stringBuilder.Append(string.Format(" {0:F1}/s ", maxArr[veinGroupIndex]));
            if (Plugin.ShowItemsPerMinute)
                stringBuilder.Append(string.Format(" {0:F0}/m", maxArr[veinGroupIndex] * 60f));
        }
    }
}
