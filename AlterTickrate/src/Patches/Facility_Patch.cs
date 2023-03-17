using HarmonyLib;
using System;
using UnityEngine;

namespace AlterTickrate.Patches
{
    public class Facility_Patch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble))]
        private static void FacilitySpeedModify(ref float power)
        {
            // only multiply speed when power > 10%
            if (power >= 0.1f)
            {
                power *= Parameters.FacilitySpeedRate;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static void AnimPowerCorrection(ref SiloComponent __instance, float power, AnimData[] animPool)
        {
            if (power >= 0.1f)
            {
                animPool[__instance.entityId].power = power / Parameters.FacilitySpeedRate;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void ResearchSpeedModify(ref float speed)
        {
            // Note: LabComponent.InternalUpdateResearch need to handle by speed due to matrixPoints (num)
            speed *= Parameters.FacilitySpeedRate;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        static bool GameTick(FactorySystem __instance, long time, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
        {
            if ((__instance.factory.index + time) % Parameters.FacilityUpdatePeriod == 0) // normal tick
                return true;

            AnimData[] entityAnimPool = __instance.factory.entityAnimPool;
            //int start, end;
            if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.fractionatorCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out int start, out int end))
            {
                // fractionators are updated every tick
                FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[__instance.factory.index];
                int[] productRegister = factoryProductionStat.productRegister;
                int[] consumeRegister = factoryProductionStat.consumeRegister;
                SignData[] entitySignPool = __instance.factory.entitySignPool;
                for (int i = start; i < end; i++)
                {
                    if (__instance.fractionatorPool[i].id == i)
                    {
                        int entityId4 = __instance.fractionatorPool[i].entityId;
                        float power = entityAnimPool[entityId4].power; // Use pre-store power which updates in normal tick
                        uint state = __instance.fractionatorPool[i].InternalUpdate(__instance.factory, power, entitySignPool, productRegister, consumeRegister);
                        entityAnimPool[entityId4].time = Mathf.Sqrt(__instance.fractionatorPool[i].fluidInputCount * 0.025f);
                        entityAnimPool[entityId4].state = state;
                    }
                }
            }

            if ((__instance.factory.index + time) % Parameters.BeltUpdatePeriod == 0)
            {
                // output cargo in miner to belt
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.minerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.minerPool[i].id == i)
                        {
                            ref var miner = ref __instance.minerPool[i];
                            if (miner.productCount > 0 && miner.insertTarget > 0 && miner.productId > 0)
                            {
                                int insertCount = (miner.productCount < 4) ? miner.productCount : 4;
                                int transferCount = __instance.factory.InsertInto(miner.insertTarget, 0, miner.productId, (byte)insertCount, 0, out _);
                                miner.productCount -= transferCount;
                                if (miner.productCount == 0 && miner.type == EMinerType.Vein)
                                {
                                    miner.productId = 0;
                                }
                            }
                        }
                    }
                }
            }

            if (__instance.factory == Parameters.AnimOnlyFactory)
            {
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.minerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.minerPool[i].id == i)
                        {
                            int entityId = __instance.minerPool[i].entityId;
                            ref AnimData animData = ref entityAnimPool[entityId];
                            animData.time %= 10f;
                            animData.Step(animData.state, 0.016666668f);
                        }
                    }
                }
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.assemblerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.assemblerPool[i].id == i)
                        {
                            int entityId = __instance.assemblerPool[i].entityId;
                            ref AnimData animData = ref entityAnimPool[entityId];
                            animData.Step(animData.state, 0.016666668f);
                        }
                    }
                }
                // ejector and slio update anim time before its time, so tickOffset has to be negative
                int tickOffset = (Parameters.AnimOnlyFactory.index + (int)time) % Parameters.FacilityUpdatePeriod - Parameters.FacilityUpdatePeriod;
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.ejectorCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    float[] networkServes = __instance.factory.powerSystem.networkServes;
                    PowerConsumerComponent[] consumerPool = __instance.factory.powerSystem.consumerPool;
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.ejectorPool[i].id == i)
                        {
                            int entityId = __instance.ejectorPool[i].entityId;
                            ref AnimData animData = ref entityAnimPool[entityId];
                            float power = networkServes[consumerPool[__instance.ejectorPool[i].pcId].networkId];
                            AnimUpdate(ref __instance.ejectorPool[i], ref animData, tickOffset, power);
                        }
                    }
                }
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.siloCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.siloPool[i].id == i)
                        {
                            int entityId = __instance.siloPool[i].entityId;
                            ref AnimData animData = ref entityAnimPool[entityId];
                            AnimUpdate(ref __instance.siloPool[i], ref animData, tickOffset);
                        }
                    }
                }
            }
            return false;
        }

        static void AnimUpdate(ref EjectorComponent ejector, ref AnimData animData, int tickOffset, float power)
        {
            if (ejector.targetState == EjectorComponent.ETargetState.OK)
            {
                if (ejector.fired)
                {
                    animData.time += 0.016666668f;
                    if (animData.time >= 11f)
                    {
                        ejector.fired = false;
                        animData.time = 0f;
                    }
                }
                else if (ejector.direction != 0)
                {
                    float num = (float)Cargo.accTableMilli[ejector.incLevel];
                    int deltaTime = (int)(power * 10000f * (1f + num) + 0.1f) * tickOffset;
                    if (ejector.boost)
                        deltaTime *= 10;

                    if (ejector.direction > 0)
                    {
                        float animTime = (ejector.time + deltaTime) / (float)ejector.chargeSpend;
                        animData.time = Mathf.Min(animTime, 1f);
                        //Log.Debug(animData.time + ":" + (ejector.time + deltaTime) + " " + deltaTime);
                    }
                    else
                    {
                        float animTime = -(ejector.time - deltaTime) / (float)ejector.coldSpend;
                        animData.time = Mathf.Min(animTime, 0f);
                    }
                }
            }
        }

        static void AnimUpdate(ref SiloComponent silo, ref AnimData animData, int tickOffset)
        {
            if (silo.direction != 0)
            {
                float power = animData.power;
                float num = (float)Cargo.accTableMilli[silo.incLevel];
                int deltaTime = (int)(power * 10000f * (1f + num) + 0.1f) * tickOffset;
                if (silo.boost)
                    deltaTime *= 10;

                if (silo.direction > 0)
                {
                    float animTime = (silo.time + deltaTime) / (float)silo.chargeSpend;
                    animData.time = Mathf.Min(animTime, 1f);
                    //Log.Debug(animData.time + ":" + (silo.time + deltaTime) + " " + deltaTime);
                }
                else
                {
                    float animTime = -(silo.time - deltaTime) / (float)silo.coldSpend;
                    animData.time = Mathf.Min(animTime, 0f);
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabProduceMode), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
        static bool LocalAnim_Lab(FactorySystem __instance, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
        {
            if ((__instance.factory.index + GameMain.gameTick) % Parameters.FacilityUpdatePeriod == 0) // normal tick
                return true;

            if (__instance.factory == Parameters.AnimOnlyFactory)
            {
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.labCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out int start, out int end))
                {
                    AnimData[] entityAnimPool = __instance.factory.entityAnimPool;
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.labPool[i].id == i)
                        {
                            ref AnimData animData = ref entityAnimPool[__instance.labPool[i].entityId];
                            animData.Step01(animData.state, 0.016666668f); // advance time by dt without updating state
                        }
                    }
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode))]
        static bool LocalAnim_Lab_Guard(FactorySystem __instance)
        {
            return __instance.factory != Parameters.AnimOnlyFactory;
        }
    }
}
