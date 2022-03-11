using HarmonyLib;
using System;
using Unity;
using UnityEngine;

namespace Experiment
{
    class Factory_Patch
    {
        //[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.DeterminePreviews))]
        static void DeterminePreviews(BuildTool_PathAddon __instance)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;
            Log.Info($"{__instance.handbp.lpos} DeterminePreviews");
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.SnapToBelt))]
        static void SnapToBelt(BuildTool_PathAddon __instance)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;
            Log.Info($"{__instance.handbp.lpos} SnapToBelt");
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.SnapToBeltAutoAdjust))]
        static void SnapToBeltAutoAdjust(BuildTool_PathAddon __instance)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;
            Log.Info($"{__instance.handbp.lpos} SnapToBeltAutoAdjust");
            Log.Warn($"{__instance.buildPreviews.Count} Previews");
        }

        static readonly Vector3 two = new Vector3(2, 2, 2);




        [HarmonyPrefix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.FindPotentialBelt))]
        static bool FindPotentialBelt(BuildTool_PathAddon __instance)
        {
            Array.Clear(__instance.potentialBeltObjIdArray, 0, __instance.potentialBeltObjIdArray.Length);
            Array.Clear(__instance.addonAreaBeltObjIdArray, 0, __instance.addonAreaBeltObjIdArray.Length);
            __instance.potentialBeltCursor = 0;
            Pose[] addonAreaPoses = __instance.handbp.desc.addonAreaPoses;
            Pose[] addonAreaColPoses = __instance.handbp.desc.addonAreaColPoses;
            Vector3[] addonAreaSize = __instance.handbp.desc.addonAreaSize;
            for (int i = 0; i < addonAreaColPoses.Length; i++)
            {
                float num = float.MaxValue;
                int num2 = 0;
                Vector3 b = __instance.handbp.lpos + __instance.handbp.lrot * addonAreaPoses[i].position;
                Vector3 center = __instance.handbp.lpos + __instance.handbp.lrot * addonAreaColPoses[i].position;
                Quaternion orientation = __instance.handbp.lrot * addonAreaColPoses[i].rotation;
                Vector3 halfExtents = addonAreaSize[i] * 2 * GameMain.localPlanet.radius / 200f; // scale by planet radius
                int mask = 428032;
                Array.Clear(BuildTool._tmp_cols, 0, BuildTool._tmp_cols.Length);
                int num3 = Physics.OverlapBoxNonAlloc(center, halfExtents, BuildTool._tmp_cols, orientation, mask, QueryTriggerInteraction.Collide);

                if (GameMain.mainPlayer.controller.cmd.stage == 1)
                    Log.Debug($"OverlapBoxNonAlloc {num3}");

                if (num3 > 0)
                {
                    PlanetPhysics physics = __instance.player.planetData.physics;
                    for (int j = 0; j < num3; j++)
                    {
                        ColliderData colliderData2;
                        bool colliderData = physics.GetColliderData(BuildTool._tmp_cols[j], out colliderData2);
                        int num4 = 0;
                        if (colliderData && colliderData2.isForBuild)
                        {
                            if (colliderData2.objType == EObjectType.Entity)
                            {
                                num4 = colliderData2.objId;
                            }
                            else if (colliderData2.objType == EObjectType.Prebuild)
                            {
                                num4 = -colliderData2.objId;
                            }
                        }
                        PrefabDesc prefabDesc = __instance.GetPrefabDesc(num4);
                        if (prefabDesc != null && prefabDesc.isBelt)
                        {
                            if (__instance.potentialBeltCursor >= __instance.potentialBeltObjIdArray.Length)
                            {
                                int[] array = __instance.potentialBeltObjIdArray;
                                __instance.potentialBeltObjIdArray = new int[__instance.potentialBeltCursor * 2];
                                if (array != null)
                                {
                                    Array.Copy(array, __instance.potentialBeltObjIdArray, __instance.potentialBeltCursor);
                                }
                            }
                            __instance.potentialBeltObjIdArray[__instance.potentialBeltCursor] = ((Mathf.Abs(num4) << 4) + i) * (int)Mathf.Sign((float)num4);
                            __instance.potentialBeltCursor++;
                            float magnitude = (__instance.GetObjectPose(num4).position - b).magnitude;
                            if (magnitude < num && magnitude < 2f)
                            {
                                num = magnitude;
                                num2 = num4;
                            }
                        }
                    }
                }
                __instance.addonAreaBeltObjIdArray[i] = num2;
            }
            if (GameMain.mainPlayer.controller.cmd.stage == 1)
                Log.Debug($"FindPotentialBelt: {__instance.potentialBeltCursor}");

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.FindPotentialBeltStrict))]
        static bool FindPotentialBeltStrict(BuildTool_PathAddon __instance)
        {
            if (__instance.handbp == null)
            {
                return false;
            }
            Array.Clear(__instance.potentialBeltObjIdArray, 0, __instance.potentialBeltObjIdArray.Length);
            __instance.potentialBeltCursor = 0;
            Pose[] addonAreaColPoses = __instance.handbp.desc.addonAreaColPoses;
            Vector3[] addonAreaSize = __instance.handbp.desc.addonAreaSize;
            for (int i = 0; i < addonAreaColPoses.Length; i++)
            {
                Vector3 lineStart = __instance.handbp.lpos + __instance.handbp.lrot * (addonAreaColPoses[i].position + addonAreaColPoses[i].forward * addonAreaSize[i].z * 2.5f);
                Vector3 lineEnd = __instance.handbp.lpos + __instance.handbp.lrot * (addonAreaColPoses[i].position - addonAreaColPoses[i].forward * addonAreaSize[i].z * 2.5f);
                Vector3 center = __instance.handbp.lpos + __instance.handbp.lrot * addonAreaColPoses[i].position;
                Quaternion orientation = __instance.handbp.lrot * addonAreaColPoses[i].rotation;
                Vector3 halfExtents = addonAreaSize[i] * GameMain.localPlanet.radius / 200f; // scale by planet radius
                int mask = 428032;
                Array.Clear(BuildTool._tmp_cols, 0, BuildTool._tmp_cols.Length);
                int num = Physics.OverlapBoxNonAlloc(center, halfExtents, BuildTool._tmp_cols, orientation, mask, QueryTriggerInteraction.Collide);
                if (GameMain.mainPlayer.controller.cmd.stage == 1)
                    Log.Debug($"OverlapBoxNonAlloc {num}");
                if (num > 0)
                {
                    PlanetPhysics physics = __instance.player.planetData.physics;
                    for (int j = 0; j < num; j++)
                    {
                        ColliderData colliderData2;
                        bool colliderData = physics.GetColliderData(BuildTool._tmp_cols[j], out colliderData2);
                        int num2 = 0;
                        if (colliderData && colliderData2.isForBuild)
                        {
                            if (colliderData2.objType == EObjectType.Entity)
                            {
                                num2 = colliderData2.objId;
                            }
                            else if (colliderData2.objType == EObjectType.Prebuild)
                            {
                                num2 = -colliderData2.objId;
                            }
                        }
                        PrefabDesc prefabDesc = __instance.GetPrefabDesc(num2);
                        Pose objectPose = __instance.GetObjectPose(num2);
                        if (prefabDesc != null && prefabDesc.isBelt && Maths.DistancePointLine(objectPose.position, lineStart, lineEnd) <= 0.3f)
                        {
                            if (__instance.potentialBeltCursor >= __instance.potentialBeltObjIdArray.Length)
                            {
                                int[] array = __instance.potentialBeltObjIdArray;
                                __instance.potentialBeltObjIdArray = new int[__instance.potentialBeltCursor * 2];
                                if (array != null)
                                {
                                    Array.Copy(array, __instance.potentialBeltObjIdArray, __instance.potentialBeltCursor);
                                }
                            }
                            __instance.potentialBeltObjIdArray[__instance.potentialBeltCursor] = ((Mathf.Abs(num2) << 4) + i) * (int)Mathf.Sign((float)num2);
                            __instance.potentialBeltCursor++;
                        }
                    }
                }
            }

            if (GameMain.mainPlayer.controller.cmd.stage == 1)
                Log.Debug($"FindPotentialBeltStrict: {__instance.potentialBeltCursor} radius{GameMain.localPlanet.radius}");

            return false;
        }




        [HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.IsPotentialBeltObj))]
        static void IsPotentialBeltObj(BuildTool_PathAddon __instance, bool __result, int objId)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;

            Log.Debug($"IsPotentialBeltObj:{__result} objId{objId}");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.IsPotentialBeltConn))]
        static void CheckBuildConditions(BuildTool_PathAddon __instance, bool __result, int objId)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;

            Log.Debug($"IsPotentialBeltConn:{__result} objId{objId} potentialBeltCursor {__instance.potentialBeltCursor}");
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.CheckBuildConditions))]
        static void CheckBuildConditions(BuildTool_PathAddon __instance)
        {
            if (GameMain.mainPlayer.controller.cmd.stage != 1)
                return;
            Log.Warn(__instance.handbp.condition);

            __instance.handbp.condition = EBuildCondition.Ok;
            for (int i = 0; i < __instance.potentialBeltObjIdArray.Length; i++)
            {
                int num2 = __instance.potentialBeltObjIdArray[i];
                int num3 = (int)Mathf.Sign((float)num2) * (Mathf.Abs(num2) >> 4);
                if (num3 != 0 && __instance.GetBeltInputCount(num3) > 1 && !__instance.HasAddonConn(num3))
                {
                    __instance.handbp.condition = EBuildCondition.Collide;
                }
            }
            //Log.Info("stage 1: " + __instance.handbp.condition);

            Pose[] addonAreaColPoses = __instance.handbp.desc.addonAreaColPoses;
            Vector3[] addonAreaSize = __instance.handbp.desc.addonAreaSize;
            for (int j = 0; j < __instance.potentialBeltCursor; j++)
            {
                int num4 = __instance.potentialBeltObjIdArray[j];
                int objId = (int)Mathf.Sign((float)num4) * (Mathf.Abs(num4) >> 4);
                int num5 = Mathf.Abs(num4) & 15;
                Vector3 b = __instance.handbp.lpos + __instance.handbp.lrot * __instance.handbp.desc.addonAreaPoses[num5].position;
                Quaternion b2 = __instance.handbp.lrot * addonAreaColPoses[num5].rotation;
                Pose objectPose = __instance.GetObjectPose(objId);
                bool flag;
                Pose beltOutputBeltPose = __instance.GetBeltOutputBeltPose(objId, out flag);
                bool flag2;
                Pose beltInputBeltPose = __instance.GetBeltInputBeltPose(objId, out flag2);
                bool flag3 = true;
                if (flag)
                {
                    Vector3 normalized = (beltOutputBeltPose.position - objectPose.position).normalized;
                    Vector3 normalized2 = objectPose.position.normalized;
                    float num6 = Quaternion.Angle(Quaternion.LookRotation(normalized, normalized2), b2);
                    flag3 &= (num6 < 20.5f || num6 > 159.5f);
                    flag3 &= (Mathf.Abs(objectPose.position.magnitude - beltOutputBeltPose.position.magnitude) < 0.6f);
                }
                if (flag2)
                {
                    Vector3 normalized3 = (objectPose.position - beltInputBeltPose.position).normalized;
                    Vector3 normalized4 = objectPose.position.normalized;
                    float num7 = Quaternion.Angle(Quaternion.LookRotation(normalized3, normalized4), b2);
                    flag3 &= (num7 < 20.5f || num7 > 159.5f);
                    flag3 &= (Mathf.Abs(objectPose.position.magnitude - beltInputBeltPose.position.magnitude) < 0.6f);
                }
                bool flag4 = true;
                Vector3 lineStart = __instance.handbp.lpos + __instance.handbp.lrot * (addonAreaColPoses[num5].position + addonAreaColPoses[num5].forward * addonAreaSize[num5].z * 2.5f);
                Vector3 lineEnd = __instance.handbp.lpos + __instance.handbp.lrot * (addonAreaColPoses[num5].position - addonAreaColPoses[num5].forward * addonAreaSize[num5].z * 2.5f);
                float num8 = Maths.DistancePointLine(objectPose.position, lineStart, lineEnd);
                if (Mathf.Pow((objectPose.position - b).sqrMagnitude + num8 * num8, 0.5f) < addonAreaSize[num5].z)
                {
                    flag4 = false;
                }
                if (!flag3 && !flag4)
                {
                    __instance.handbp.condition = EBuildCondition.Collide;
                }
            }
            Log.Info("stage 2: " + __instance.handbp.condition);


            if (__instance.handbp.condition == EBuildCondition.Ok)
            {
                ColliderData[] buildColliders = __instance.handbp.desc.buildColliders;
                for (int k = 0; k < buildColliders.Length; k++)
                {
                    ColliderData colliderData = __instance.handbp.desc.buildColliders[k];
                    colliderData.pos = __instance.handbp.lpos + __instance.handbp.lrot * colliderData.pos;
                    colliderData.q = __instance.handbp.lrot * colliderData.q;
                    int mask = 428032;
                    Array.Clear(BuildTool._tmp_cols, 0, BuildTool._tmp_cols.Length);
                    int num11 = Physics.OverlapBoxNonAlloc(colliderData.pos, colliderData.ext, BuildTool._tmp_cols, colliderData.q, mask, QueryTriggerInteraction.Collide);
                    if (num11 > 0)
                    {
                        bool flag5 = false;
                        PlanetPhysics physics = __instance.player.planetData.physics;
                        for (int l = 0; l < num11; l++)
                        {
                            ColliderData colliderData3;
                            bool colliderData2 = physics.GetColliderData(BuildTool._tmp_cols[l], out colliderData3);
                            int num12 = 0;
                            if (colliderData2 && colliderData3.isForBuild)
                            {
                                if (colliderData3.objType == EObjectType.Entity)
                                {
                                    num12 = colliderData3.objId;
                                }
                                else if (colliderData3.objType == EObjectType.Prebuild)
                                {
                                    num12 = -colliderData3.objId;
                                }
                            }
                            if (!__instance.IsPotentialBeltObj(num12))
                            {
                                PrefabDesc prefabDesc = __instance.GetPrefabDesc(num12);
                                Collider collider = BuildTool._tmp_cols[l];
                                if (collider.gameObject.layer == 18)
                                {
                                    BuildPreviewModel component = collider.GetComponent<BuildPreviewModel>();
                                    if ((component != null && component.index == __instance.handbp.previewIndex) || (__instance.handbp.desc.isInserter && !component.buildPreview.desc.isInserter) || (!__instance.handbp.desc.isInserter && component.buildPreview.desc.isInserter) || (!__instance.handbp.desc.isBelt && component.buildPreview.desc.isBelt))
                                    {
                                        Log.Debug("IL_711 jump");
                                        goto IL_711;
                                    }
                                }
                                if (prefabDesc == null || !prefabDesc.isBelt || (!__instance.IsPotentialBeltConn(num12) && !__instance.HasAddonConn(num12)))
                                {
                                    //Log.Debug(prefabDesc);
                                    Log.Debug("isBelt " + prefabDesc.isBelt);
                                    Log.Debug("IsPotentialBeltConn " + __instance.IsPotentialBeltConn(num12));
                                    Log.Debug("HasAddonConn " + __instance.HasAddonConn(num12));
                                    flag5 = true;
                                }
                            }
                        IL_711:;
                        }
                        if (flag5)
                        {
                            Log.Info("flag 5");
                            __instance.handbp.condition = EBuildCondition.Collide;
                            break;
                        }
                    }
                }
                /*
                if (__instance.planet != null)
                {
                    float num13 = 64f;
                    float num14 = __instance.actionBuild.history.buildMaxHeight + 0.5f + __instance.planet.realRadius; //base!
                    if (__instance.handbp.lpos.sqrMagnitude > num14 * num14)
                    {
                        if (__instance.actionBuild.history.buildMaxHeight + 0.5f <= num13)
                        {
                            BuildModel model = __instance.actionBuild.model; //base!
                            model.cursorText = model.cursorText + "垂直建造可升级".Translate() + "\r\n";
                        }
                        __instance.handbp.condition = EBuildCondition.OutOfVerticalConstructionHeight;
                    }
                }
                bool flag6 = false;
                Vector3 b3 = Vector3.zero;
                if (__instance.planet.id == __instance.planet.galaxy.birthPlanetId && __instance.actionBuild.history.SpaceCapsuleExist())
                {
                    b3 = __instance.planet.birthPoint;
                    flag6 = true;
                }
                if (flag6 && __instance.handbp.lpos.magnitude < __instance.planet.realRadius + 3f)
                {
                    Vector3 ext = __instance.handbp.desc.buildCollider.ext;
                    float num15 = Mathf.Sqrt(ext.x * ext.x + ext.z * ext.z);
                    if ((__instance.handbp.lpos - b3).magnitude - num15 < 3.7f)
                    {
                        __instance.handbp.condition = EBuildCondition.Collide;
                    }
                }
                */
            }
            Log.Info("stage 3: " + __instance.handbp.condition);

        }



    }
}
