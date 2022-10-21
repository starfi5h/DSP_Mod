using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LossyCompression
{
    public static class LazyLoading
    {
        public static bool Enable { get; set; } = true;

        private static readonly Dictionary<DysonSphere, int> dysonSphereMasks = new Dictionary<DysonSphere, int>();
        private static DysonSphere viewingSphere = null;
        private static int editorMask;
        private static int gameMask;
        private static readonly System.Random random = new System.Random();

        public static void Reset()
        {
            viewingSphere = null;
            dysonSphereMasks.Clear();
        }

        public static void Add(DysonSphere dysonSphere)
        {
            dysonSphereMasks[dysonSphere] = 0;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), nameof(DysonSphereSegmentRenderer.DrawModels))]
        public static void DrawModels(DysonSphere ___dysonSphere)
        {
            if (___dysonSphere != viewingSphere)
            {
                viewingSphere = ___dysonSphere;
                editorMask = ___dysonSphere.inEditorRenderMaskL;
                gameMask = ___dysonSphere.inGameRenderMaskL;
                if (dysonSphereMasks.ContainsKey(___dysonSphere))
                {
                    dysonSphereMasks[___dysonSphere] |= editorMask | gameMask;
                    DysonShellCompress.GenerateModel(viewingSphere, dysonSphereMasks[___dysonSphere]);
                    // Remove if all layer has been generated
                    if (dysonSphereMasks[___dysonSphere] == -1)
                        dysonSphereMasks.Remove(___dysonSphere);
                    DysonShellCompress.FreeRAM();
                }
            }
            else if (editorMask != ___dysonSphere.inEditorRenderMaskL || gameMask != ___dysonSphere.inGameRenderMaskL)
            {
                editorMask = ___dysonSphere.inEditorRenderMaskL;
                gameMask = ___dysonSphere.inGameRenderMaskL;
                if (dysonSphereMasks.ContainsKey(___dysonSphere))
                {
                    dysonSphereMasks[___dysonSphere] |= editorMask | gameMask;
                    DysonShellCompress.GenerateModel(viewingSphere, dysonSphereMasks[___dysonSphere]);
                    // Remove if all layer has been generated
                    if (dysonSphereMasks[___dysonSphere] == -1)
                        dysonSphereMasks.Remove(___dysonSphere);
                    DysonShellCompress.FreeRAM();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Export))]
        public static void Export(DysonSphere __instance)
        {
            // Generate data for vanilla save
            if (Enable && !DysonShellCompress.Enable && dysonSphereMasks.ContainsKey(__instance))
            {
                // GenerateModel must run on main thread
                if (ThreadingHelper.Instance.InvokeRequired)
                {
                    ThreadingHelper.Instance.StartSyncInvoke(() =>
                    {
                        DysonShellCompress.GenerateModel(__instance, -1);
                    });
}
                else
                {
                    DysonShellCompress.GenerateModel(__instance, -1);
                }
                dysonSphereMasks.Remove(__instance);
                DysonShellCompress.FreeRAM();
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.RemoveDysonShell))]
        public static bool RemoveDysonShell(DysonSphereLayer __instance, int shellId)
        {
            // use original method if verts is loaded.
            if (__instance.shellPool[shellId].id == 0 || __instance.shellPool[shellId].verts != null)
            {
                return true;
            }

            // verts is not loaded, so use our own algorithm to generate sails
            int num = (int)(GameMain.history.solarSailLife * 60f + 0.1f);
            long expiryTime = GameMain.gameTick + (long)num;
            DysonShell dysonShell = __instance.shellPool[shellId];


            Vector3 centerPoint = Vector3.zero;
            int totalCp = 0;
            for (int i = 0; i < dysonShell.nodes.Count; i++)
            {
                centerPoint += dysonShell.nodes[i].pos;
                totalCp += dysonShell.nodecps[i] / 2;
            }
            centerPoint /= dysonShell.nodes.Count;
            int cpPerNode = totalCp / dysonShell.nodes.Count;
            int cpRemain = totalCp % dysonShell.nodes.Count;


            int seed = shellId;
            for (int i = 0; i < dysonShell.nodes.Count; i++)
            {
                Vector3 vert1 = dysonShell.nodes[i].pos;
                Vector3 vert2 = i + 1 != dysonShell.nodes.Count ? dysonShell.nodes[i + 1].pos : dysonShell.nodes[0].pos;
                int cp = i + 1 != dysonShell.nodes.Count ? cpPerNode : cpPerNode + cpRemain;

                for (int j = 0; j < cp; j++)
                {
                    // uniform distribution in a triagnle centerPoint, vert1, vert2
                    float a = (float)random.NextDouble();
                    float b = (float)random.NextDouble();
                    if (a + b > 1)
                    {
                        a = 1 - a;
                        b = 1 - b;
                    }
                    Vector3 point = (1 - a - b) * centerPoint +  a * vert1 + b * vert2;
                    point = __instance.orbitRadius * point.normalized;

                    Vector3 velocity = __instance.currentRotation * (Vector3.Cross(point, Vector3.up) * __instance.orbitAngularSpeed * 0.017453292f);
                    point = __instance.currentRotation * point;
                    velocity += (Vector3)RandomTable.SphericNormal(ref seed, 1.2000000476837158);
                    DysonSail ss = default;
                    ss.px = point.x;
                    ss.py = point.y;
                    ss.pz = point.z;
                    ss.vx = velocity.x;
                    ss.vy = velocity.y;
                    ss.vz = velocity.z;
                    ss.gs = 1f;
                    __instance.dysonSphere.swarm.AddSolarSail(ss, 30, expiryTime);
                }
            }
            foreach (DysonNode dysonNode in __instance.shellPool[shellId].nodes)
            {
                dysonNode.shells.Remove(__instance.shellPool[shellId]);
                dysonNode.RecalcCpReq();
            }
            __instance.shellPool[shellId].Free();
            __instance.shellPool[shellId] = null;
            __instance.shellRecycle[__instance.shellRecycleCursor++] = shellId;

            return false;
        }

    }
}
