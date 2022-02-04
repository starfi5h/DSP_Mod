using HarmonyLib;
using System;
using Unity;
using UnityEngine;


namespace Experiment
{
    public class PatchTest
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Resume))]
        static void Test()
        {
            Log.Debug(DSPGame.globalOption.dataUploadToMilkyWay);
            Log.Debug(DSPGame.milkyWayWebClient.canUploadGame);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAssemblerWindow), nameof(UIAssemblerWindow.OnIncSwitchClick))]
        internal static void OnIncSwitchClick_Postfix(UIAssemblerWindow __instance)
        {
            AssemblerComponent assemblerComponent = __instance.factorySystem.assemblerPool[__instance.assemblerId];
            Log.Debug($"{assemblerComponent.productive} {assemblerComponent.forceAccMode}");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPSController), nameof(FPSController.Update))]
        internal static void FPSController_Update(FPSController __instance)
        {
            if ((GameMain.gameTick & 64) == 1) {
                Log.Info($"{__instance.fixUPS} {__instance.fixRatio} {__instance.aveDeltaTime}");
                Log.Info($"{1/__instance.fixUPS} {__instance.fixRatio* __instance.aveDeltaTime} delta:{Time.fixedDeltaTime}");
            }
            if (__instance.fixUPS > 0)
            {
                Time.fixedDeltaTime = 1 / (float)__instance.fixUPS;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISpraycoaterWindow), nameof(UISpraycoaterWindow.OnTakeBackPointerUp))]
        public static void OnTakeBackPointerUp_Postfix(UISpraycoaterWindow __instance)
        {
            SpraycoaterComponent sprayer = __instance.traffic.spraycoaterPool[__instance.spraycoaterId];

            // If pressed on the previous frame
            Log.Debug($"incCount {sprayer.incCount} extraIncCount {sprayer.extraIncCount} incSprayTimes {sprayer.incSprayTimes}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.SetInserterInsertTarget))]
        internal static void SetInserterInsertTarget_Prefix(int __0, int __1, int __2)
        {
            Log.Warn($"SetInserterInsertTarget {__0} {__1} {__2}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.SetInserterPickTarget))]
        internal static void SetInserterPickTarget_Prefix(int __0, int __1, int __2)
        {
            Log.Warn($"SetInserterPickTarget {__0} {__1} {__2}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.OnBeltBuilt))]
        internal static void OnBeltBuilt_Prefix(PlanetFactory __instance)
        {
            Log.Info($"OnBeltBuilt {__instance.planet.physics}");
            __instance.planet.physics.nearColliderLogic.RefreshCollidersOnArrayChange();
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.WriteObjectConn))]
        internal static void WriteObjectConn_Postfix(PlanetData __instance, int __0, int __1, bool __2, int __3, int __4)
        {
            Log.Warn($"WriteObjectConn: objId[{__0}] slot{__1} isOutput={__2} otherObjId{__3} othersolt{__4}");
            Log.Debug(Environment.StackTrace);            
        }


        static int preItemIndex;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction.UpdateViewStar))]
        internal static void UpdateViewStar_Prefix(UIDETopFunction __instance)
        {
            int starIndex = __instance.dysonBox.ItemsData[__instance.dysonBox.itemIndex];
            /*
            if (preItemIndex != __instance.dysonBox.itemIndex)
            {
                Log.Debug($"UpdateViewStar {__instance.dysonBox.itemIndex}");

                __instance.editor.selection.SetViewStar(__instance.gameData.galaxy.stars[starIndex]);
                __instance.editor.selection.viewDysonSphere = null;
            }
            */
            preItemIndex = __instance.dysonBox.itemIndex;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DESelection), nameof(DESelection.SetViewStar))]
        internal static void SetViewStar_Prefix(DESelection __instance)
        {
            Log.Debug($"SetViewStar");
            __instance.viewDysonSphere = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction._OnOpen))]
        internal static void OnOpen_Prefix(UIDETopFunction __instance)
        {

        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBeltBuildTip), nameof(UIBeltBuildTip.SetFilterToEntity))]
        internal static void SetFilterToEntity_Postfix(UIBeltBuildTip __instance)
        {
            Log.Warn($"SetFilterToEntity");
            if (__instance.outputEntityId <= 0)
            {
                return;
            }
            if (__instance.outputSlotId < 0)
            {
                return;
            }
            PlanetFactory factory = GameMain.mainPlayer.factory;
            if (factory != null)
            {
                EntityData entityData = factory.entityPool[__instance.outputEntityId];
                if (entityData.stationId > 0)
                {
                    StationComponent stationComponent = factory.transport.stationPool[entityData.stationId];
                    Assert.NotNull(stationComponent);
                    if (stationComponent != null && __instance.outputSlotId < stationComponent.slots.Length)
                    {
                        if (stationComponent.isVeinCollector)
                        {
                            Log.Info($"Vein {stationComponent.slots[__instance.outputSlotId].storageIdx}");
                            return;
                        }
                        Log.Info($"{stationComponent.slots[__instance.outputSlotId].storageIdx}");
                    }
                }
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(NearColliderLogic), nameof(NearColliderLogic.GetEntitiesInAreaNonAlloc))]
        internal static void GetEntitiesInAreaNonAlloc_Postfix(NearColliderLogic __instance, int __result, Vector3 __0, float __1, int[] __2)
        {
            Vector3 center = __0;
            float areaRadius = __1;
            int[] entityIds = __2;
            int count = 0;

            Log.Info($"GetEntitiesInAreaNonAlloc={__result} pos:{__0} r:{__1} array:{string.Join(", ", __2)}");
            Log.Info($"{__instance.activeColHashCount}");


            for (int i = 0; i < __instance.activeColHashCount; i++)
            {
                int num = __instance.activeColHashes[i];                
                ColliderData[] colliderPool = __instance.colChunks[num].colliderPool;
                Log.Debug($"num{num} cusor{__instance.colChunks[num].cursor}");
                for (int j = 1; j < __instance.colChunks[num].cursor; j++)
                {
                    Log.Debug($"[{j}] {colliderPool[j].idType != 0} {!colliderPool[j].notForBuild} {colliderPool[j].objType == EObjectType.Entity} {(colliderPool[j].pos - center).sqrMagnitude <= areaRadius * areaRadius + colliderPool[j].ext.sqrMagnitude}");
                    if (colliderPool[j].idType != 0 && !colliderPool[j].notForBuild && colliderPool[j].objType == EObjectType.Entity && (colliderPool[j].pos - center).sqrMagnitude <= areaRadius * areaRadius + colliderPool[j].ext.sqrMagnitude)
                    {
                        bool flag = false;
                        for (int k = 0; k < count; k++)
                        {
                            if (entityIds[k] == colliderPool[j].objId)
                            {
                                flag = true;
                                Log.Debug($"[{j}] [{k}] {entityIds[k]}");
                                break;
                            }
                        }
                        if (!flag)
                        {
                            Log.Debug($"entity[{count}]={colliderPool[j].objId}");
                            entityIds[count++] = colliderPool[j].objId;
                            if (count >= entityIds.Length - 1)
                            {
                                __instance.ExpandArrayCapacity(ref entityIds);
                            }
                        }
                    }
                }
            }
        }

    }
}
