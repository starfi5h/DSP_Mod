
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PlanetwideSpray
{
    public class Main_Patch
    {
        private static Status[] statusArr = null;
        private static readonly Dictionary<CargoContainer, int> containerToIndex = new();
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
            // 創世之書 - 巨構使用 https://github.com/Awbugl/ProjectGenesis/blob/main/src/Patches/MegaAssembler/MegaAssemblerPatches.cs
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

            // 原版 - 物流塔傳送帶輸入
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CargoPath), nameof(CargoPath.TryPickItemAtRear))]
            static void TryPickItemAtRear(CargoPath __instance, int __result, ref byte stack, ref byte inc)
            {
                if (__result == 0 || __result == 1210) return;
                if (containerToIndex.TryGetValue(__instance.cargoContainer, out int factoryIndex)) return; //從cargoContainer嘗試反找所屬factory                                
                var status = statusArr[factoryIndex];
                var incToAdd = (stack * status.incLevel) - inc;
                if (incToAdd > 0) // incLevel > 0: 有工作中的全球噴塗機有足夠儲量
                {
                    Interlocked.Add(ref status.incDebt, stack);
                    inc += (byte)incToAdd;
                }
            }
        }

        #region 爪子輸入

        // 白爪輸出到傳送帶(全噴)
        public static void TryInsertItemToBeltWithStackIncreasement_Prefix
            (CargoTraffic __instance, ref int itemCount, ref int itemInc, ref int __state)
        {
            var status = statusArr[__instance.factory.index];
            var incToAdd = (itemCount * status.incLevel) - itemInc; //達到層級所需要的增產劑點數
            if (incToAdd > 0)
            {
                __state = itemCount; // 保存嘗試送入前的itemCount
                itemInc += (byte)incToAdd;
            }
        }

        // 白爪輸出到傳送帶(全噴)
        public static void TryInsertItemToBeltWithStackIncreasement_Postfix
            (CargoTraffic __instance, ref int itemCount, ref int __state)
        {
            int insertedItemCount = __state - itemCount;
            if (insertedItemCount != 0) //有成功將物品送入機器, insertedItemCount:送入的物品數
            {
                var status = statusArr[__instance.factory.index]; //定位工廠
                Interlocked.Add(ref status.incDebt, insertedItemCount); //扣除噴塗的物品數，準備之後報銷
            }
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))] //爪子輸入(全噴)
        public static void AddItemInc0(PlanetFactory __instance, byte itemCount, ref byte itemInc, ref bool __state)
        {
            var status = statusArr[__instance.index]; //定位工廠
            var incToAdd = (itemCount * status.incLevel) - itemInc; //達到層級所需要的增產劑點數
            if (incToAdd > 0)
            {
                __state = true; // 保存flag:將要噴塗
                itemInc += (byte)incToAdd;
            }
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))] //爪子輸入(LimitSpray)
        public static void AddItemInc1(PlanetFactory __instance, uint ioTargetTypedId, byte itemCount, ref byte itemInc, ref bool __state)
        {
            var targetType = (EFactoryIOTargetType)(ioTargetTypedId & 0xFF000000u); // EFactoryIOTargetType.TypeMask
            if (targetType != EFactoryIOTargetType.Assembler && targetType != EFactoryIOTargetType.Lab) return;

            var status = statusArr[__instance.index]; //定位工廠
            var incToAdd = (itemCount * status.incLevel) - itemInc; //達到層級所需要的增產劑點數
            if (incToAdd > 0)
            {
                __state = true; // 保存flag:將要噴塗
                itemInc += (byte)incToAdd;
            }
        }

        //[HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))] //爪子輸入(LimitSpray,含燃料)
        public static void AddItemInc2(PlanetFactory __instance, uint ioTargetTypedId, byte itemCount, ref byte itemInc, ref bool __state)
        {
            var targetType = (EFactoryIOTargetType)(ioTargetTypedId & 0xFF000000u); // EFactoryIOTargetType.TypeMask
            if (targetType != EFactoryIOTargetType.Assembler && targetType != EFactoryIOTargetType.Lab && targetType != EFactoryIOTargetType.PowerGen) return;

            var status = statusArr[__instance.index]; //定位工廠
            var incToAdd = (itemCount * status.incLevel) - itemInc; //達到層級所需要的增產劑點數
            if (incToAdd > 0)
            {
                __state = true; // 保存flag:將要噴塗
                itemInc += (byte)incToAdd;
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto))]
        public static void InsertInto_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result != 0) //有成功將物品送入機器, __result:送入的物品數
            {
                var status = statusArr[__instance.index]; //定位工廠
                Interlocked.Add(ref status.incDebt, __result); //扣除噴塗的物品數，準備之後報銷
            }
        }

        #endregion

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(FractionatorComponent), nameof(FractionatorComponent.InternalUpdate))]
        public static void FractionatorSetInc(PlanetFactory factory, ref FractionatorComponent __instance)
        {
            var status = statusArr[factory.index];
            var incToAdd = (__instance.fluidInputCount * status.incLevel) - __instance.fluidInputInc;
            if (incToAdd > 0 && status.incDebt < status.incCount[status.incLevel])
            {
                Interlocked.Add(ref status.incDebt, incToAdd / status.incLevel);
                __instance.fluidInputInc += incToAdd;
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.InternalUpdate))]
        public static void TurretSetInc(PlanetFactory factory, ref TurretComponent __instance)
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
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.LogicFrame))]
        static void LogicFrame_Prefix()
        {
            if (statusArr == null) SetArray();
            for (int factoryIndex = 0; factoryIndex < GameMain.data.factoryCount; factoryIndex++)
            {
                if (GameMain.data.factories[factoryIndex] == null) continue;
                var status = statusArr[factoryIndex];
                if (status == null)
                {
                    status = new Status();
                    statusArr[factoryIndex] = status;
                    containerToIndex[GameMain.data.factories[factoryIndex].cargoContainer] = factoryIndex;
                }
                Array.Clear(status.incCount, 0, MAX_INC_COUNT);
            }
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
                // 每幀最多補充incSprayTimes*2個(藍漆:60)，最大速率=120*60*60=432,000/每分鐘
                int incToAdd = 0;
                lock (statusArr) // InternalUpdate為多線程,所以要鎖住
                {
                    incToAdd = (__instance.incSprayTimes * 2) < status.incDebt ? (__instance.incSprayTimes * 2) : status.incDebt;
                    status.incDebt -= incToAdd;
                }
                __instance.extraIncCount -= incToAdd;
                //__instance.sprayTime = incToAdd; // Use in UI
                flag = true;

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

            // 更新動畫。注意在遠端工廠_animPool為null
            if (_animPool == null) return;
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
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.LogicFrame))]
        static void LogicFrame_Postfix()
        {
            for (int factoryIndex = 0; factoryIndex < GameMain.data.factoryCount; factoryIndex++)
            {
                var status = statusArr[factoryIndex];
                if (status == null) continue;
                var incLevel = 10;
                for (; incLevel > 0; incLevel--)
                {
                    if (status.incCount[incLevel] > 0) break;
                }
                status.incLevel = incLevel; // 設置目前最大的增產劑等級
            }
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
