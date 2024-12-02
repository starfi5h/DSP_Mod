
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PlanetwideSpray
{
    public class Main_Patch
    {
        public static bool LimitSpray = true;
        private static Status[] statusArr = null;
        private static Dictionary<CargoContainer, int> containerToIndex = new();
        const int MAX_INC_COUNT = 11;


        public class Status
        {
            public int incLevel; // 當前星球最高作用中增產劑等級
            public readonly int[] incCount = new int[MAX_INC_COUNT]; // 各等級增產劑點數
            public int incDebt; // 滯留扣除額
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void SetArray()
        {
            statusArr = new Status[GameMain.data.factories.Length];
            for (int i = 0; i < statusArr.Length; i++)
            {
                statusArr[i] = new Status();
            }
            containerToIndex.Clear();
        }

        public class Station_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.TryPickItemAtRear))]
            static void TryPickItemAtRear(CargoTraffic __instance, int __result, ref byte stack, ref byte inc)
            {
                if (__result == 0 || __result == 1210) return; //空抓或是翹曲器
                var status = statusArr[__instance.factory.index]; //定位工廠
                var incToAdd = (stack * status.incLevel) - inc; //達到層級所需要的增產劑點數
                if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
                {
                    Interlocked.Add(ref status.incDebt, stack); //記錄噴塗的物品數
                    inc += (byte)incToAdd;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickItemAtRear))]
            static void TryPickItemAtRear(CargoPath __instance, int __result, ref byte stack, ref byte inc)
            {
                if (__result == 0 || __result == 1210) return;
                if (containerToIndex.TryGetValue(__instance.cargoContainer, out int factoryIndex)) return; //從cargoContainer嘗試反找所屬factory
                var status = statusArr[factoryIndex];
                var incToAdd = (stack * status.incLevel) - inc;
                if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
                {
                    Interlocked.Add(ref status.incDebt, stack);
                    inc += (byte)incToAdd;
                }
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
        static void AddItemInc(PlanetFactory __instance, int entityId, byte itemCount, ref byte itemInc)
        {
            ref var entity = ref __instance.entityPool[entityId];
            if (entity.assemblerId == 0 && entity.labId == 0 && LimitSpray) return;

            var status = statusArr[__instance.index]; //定位工廠
            var incToAdd = (itemCount * status.incLevel) - itemInc; //達到層級所需要的增產劑點數
            if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
            {
                Interlocked.Add(ref status.incDebt, itemCount); //記錄噴塗的物品數 (詳見num9 * this.incAbility)
                itemInc += (byte)incToAdd;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        static void FractionatorSetInc(PlanetFactory factory, ref FractionatorComponent __instance)
        {
            var status = statusArr[factory.index];
            var incToAdd = (__instance.fluidInputCount * status.incLevel) - __instance.fluidInputInc;
            if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
            {
                Interlocked.Add(ref status.incDebt, incToAdd / status.incLevel);
                __instance.fluidInputInc += incToAdd;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.InternalUpdate))]
        static void TurretSetInc(PlanetFactory factory, ref TurretComponent __instance)
        {
            // TurretComponent.BeltUpdate沒有factory可以定位, 因此用外層的InternalUpdate
            var status = statusArr[factory.index];
            var incToAdd = (__instance.itemCount * status.incLevel) - __instance.itemInc;
            if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
            {
                Interlocked.Add(ref status.incDebt, incToAdd / status.incLevel);
                __instance.itemInc += (short)incToAdd;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SpraycoaterGameTick))]
        static void SpraycoaterGameTick_Prefix(CargoTraffic __instance)
        {
            if (statusArr == null) SetArray();
            var status = statusArr[__instance.factory.index];
            if (status == null)
            {
                status = new Status();
                statusArr[__instance.factory.index] = status;
                containerToIndex[__instance.factory.cargoContainer] = __instance.factory.index;
            }
            Array.Clear(status.incCount, 0, MAX_INC_COUNT);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpraycoaterComponent), nameof(SpraycoaterComponent.InternalUpdate))]
        static void InternalUpdate(ref SpraycoaterComponent __instance, CargoTraffic _traffic, AnimData[] _animPool, int[] consumeRegister)
        {
            if (__instance.cargoBeltId > 0 || __instance.incCount <= 0) return;

            var flag = false;
            var status = statusArr[_traffic.factory.index];
            if (status.incDebt > 0) //存在滯留扣除額, 嘗試報銷
            {
                flag = true;
                var incToAdd = (__instance.incSprayTimes * 2) < status.incDebt ? (__instance.incSprayTimes * 2) : status.incDebt; // max: 7200/min
                status.incDebt -= incToAdd;
                __instance.extraIncCount -= incToAdd;
                //__instance.sprayTime = incToAdd; // Use in UI

                if (__instance.extraIncCount < 0)
                {
                    int incCount = __instance.incCount;
                    __instance.incCount += __instance.extraIncCount;
                    __instance.extraIncCount = 0;
                    consumeRegister[__instance.incItemId] += incCount / __instance.incSprayTimes - __instance.incCount / __instance.incSprayTimes;
                    if (__instance.incCount <= 0)
                    {
                        __instance.incItemId = 0;
                        __instance.incAbility = 0;
                    }
                }
            }
            status.incCount[__instance.incAbility] += __instance.extraIncCount + __instance.incCount;


            int animStateTick = Mathf.RoundToInt(_animPool[__instance.entityId].state * 0.001f);
            if (flag)
            {
                if (animStateTick < 15)
                {
                    animStateTick = 60 - animStateTick;
                }
                else if (animStateTick < 45)
                {
                    animStateTick = 45;
                }
            }
            _animPool[__instance.entityId].state = (uint)(1 + __instance.incAbility * 10 + 100 + animStateTick * 1000);
            _animPool[__instance.entityId].time = (float)(__instance.incCount + __instance.extraIncCount) / __instance.incCapacity;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SpraycoaterGameTick))]
        static void SpraycoaterGameTick_Postfix(CargoTraffic __instance)
        {
            var status = statusArr[__instance.factory.index];
            var incLevel = 10;
            for (; incLevel > 0; incLevel--)
            {
                if (status.incCount[incLevel] > 0) break;
            }
            status.incLevel = incLevel;
        }

#pragma warning disable Harmony003
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISpraycoaterWindow), nameof(UISpraycoaterWindow.RefreshSpraycoaterWindow))]
        static void RefreshSpraycoaterWindow(UISpraycoaterWindow __instance, SpraycoaterComponent spraycoater)
        {
            if (spraycoater.cargoBeltId > 0) return;

            if (spraycoater.incAbility == 0)
            {
                __instance.stateText.text = "[PlanetwideSpray] " + "Insufficient supply".Translate();
                __instance.stateText.color = __instance.powerLowColor;
                return;
            }

            var power = __instance.factory.entityAnimPool[spraycoater.entityId].power;
            if (power == 1f)
            {
                __instance.stateText.text = "[PlanetwideSpray] " + "正常运转".Translate();
                __instance.stateText.color = __instance.workNormalColor;
                return;
            }
            else
            {
                if (power > 0.1f)
                {
                    __instance.stateText.text = "[PlanetwideSpray] " + "电力不足".Translate();
                    __instance.stateText.color = __instance.powerLowColor;
                    return;
                }
                __instance.stateText.text = "[PlanetwideSpray] " + "停止运转".Translate();
                __instance.stateText.color = __instance.powerOffColor;
                return;
            }
        }

    }
}
