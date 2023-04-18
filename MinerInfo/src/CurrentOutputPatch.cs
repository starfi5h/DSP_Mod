using HarmonyLib;

namespace MinerInfo
{
    class CurrentOutputPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        static void UIEntityBriefInfo_OnUpdate(UIEntityBriefInfo __instance)
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

                        if (Plugin.ShowItemsPerSecond)
                            __instance.entityNameText.text += $"  {currentRate:F2}/s  ({ratio:P0})";
                        if (Plugin.ShowItemsPerMinute)
                            __instance.entityNameText.text += $"  {currentRate * 60:F1}/m  ({ratio:P0})";
                    }
                }
            }
        }
    }
}
