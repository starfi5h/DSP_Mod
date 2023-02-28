using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AlterTickrate.Patches
{
    public class Facility_Patch
    {
        public static float FacilitySpeedRate = 5.0f;
        public static PlanetFactory AnimOnlyFactory = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble))]
        private static void FacilitySpeedModify(ref float power)
        {
            // only multiply speed when power > 10%
            if (power >= 0.1f)
            {
                power *= FacilitySpeedRate;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        private static void AnimPowerCorrection(ref EjectorComponent __instance, float power, AnimData[] animPool)
        {
            if (power >= 0.1f)
            {
                animPool[__instance.entityId].power = power / FacilitySpeedRate;
                //Log.Info(animPool[__instance.entityId].time + ":" + (__instance.time + deltaTime) + " " + deltaTime);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        private static void AnimPowerCorrection(ref SiloComponent __instance, float power, AnimData[] animPool)
        {
            if (power >= 0.1f)
            {
                animPool[__instance.entityId].power = power / FacilitySpeedRate;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void ResearchSpeedModify(ref float speed)
        {
            // Note: LabComponent.InternalUpdateResearch need to handle by speed due to matrixPoints (num)
            speed *= FacilitySpeedRate;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        static IEnumerable<CodeInstruction> FractionatorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Change: if (this.progress > 100000) this.progress = 100000;
            // To    : if (this.progress > 600000) this.progress = 600000;
            try
            {
                var codeMatcher = new CodeMatcher(instructions);
                codeMatcher.MatchForward(true,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "progress"),
                    new CodeMatch(OpCodes.Ldc_I4),
                    new CodeMatch(OpCodes.Ble),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "progress"),
                    new CodeMatch(OpCodes.Stfld)
                );

                
                codeMatcher.Advance(-1)
                    .SetOperandAndAdvance(600000) // (7200/min input => 20000 progress added) * 30 tick
                    .Advance(-4)
                    .SetOperandAndAdvance(600000);

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("Transpiler FractionatorComponent.InternalUpdate failed");
                return instructions;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) } )]
        static bool GameTick(FactorySystem __instance, long time, int _usedThreadCnt, int _curThreadIdx, int _minimumMissionCnt)
        {
            if (__instance.factory == AnimOnlyFactory)
            {
                AnimData[] entityAnimPool = __instance.factory.entityAnimPool;
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.minerCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out int start, out int end))
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
                int tickOffset = (AnimOnlyFactory.index + (int)time) % ConfigSettings.FacilityUpdatePeriod - ConfigSettings.FacilityUpdatePeriod;
                if (WorkerThreadExecutor.CalculateMissionIndex(1, __instance.ejectorCursor - 1, _usedThreadCnt, _curThreadIdx, _minimumMissionCnt, out start, out end))
                {
                    for (int i = start; i < end; i++)
                    {
                        if (__instance.ejectorPool[i].id == i)
                        {
                            int entityId = __instance.ejectorPool[i].entityId;
                            ref AnimData animData = ref entityAnimPool[entityId];
                            AnimUpdate(ref __instance.ejectorPool[i], ref animData, tickOffset);
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
                return false;
            }
            return true;
        }

        static void AnimUpdate(ref EjectorComponent ejector, ref AnimData animData, int tickOffset)
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
                    float power = animData.power;
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
            if (__instance.factory == AnimOnlyFactory)
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
                 return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode))]
        static bool LocalAnim_Lab_Guard(FactorySystem __instance)
        {
            return __instance.factory != AnimOnlyFactory;
        }
    }
}
