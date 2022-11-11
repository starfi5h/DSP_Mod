using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace LossyCompression
{
    public static class LazyLoading
    {
        public static bool Enable { get; set; } = true;
        public static bool ReduceRAM { get; set; } = false;

        private static readonly Dictionary<DysonSphere, int> dysonSphereMasks = new Dictionary<DysonSphere, int>();
        private static DysonSphere viewingSphere = null;
        private static int editorMask;
        private static int gameMask;
        private static readonly System.Random random = new System.Random();

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void Reset()
        {
            Log.Debug("Lazyload Reset. Clear masks: " + GameMain.isEnded);
            viewingSphere = null;
            if (GameMain.isEnded)
                dysonSphereMasks.Clear();
        }

        public static void Add(DysonSphere dysonSphere)
        {
            dysonSphereMasks[dysonSphere] = 0;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), nameof(DysonSphereSegmentRenderer.DrawModels))]
        public static void DrawModels(DysonSphere ___dysonSphere)
        {
            if (___dysonSphere != viewingSphere || (editorMask != ___dysonSphere.inEditorRenderMaskL || gameMask != ___dysonSphere.inGameRenderMaskL))
            {
                viewingSphere = ___dysonSphere;
                editorMask = ___dysonSphere.inEditorRenderMaskL;
                gameMask = ___dysonSphere.inGameRenderMaskL;
                if (dysonSphereMasks.TryGetValue(___dysonSphere, out int visibleMask))
                {
                    visibleMask |= editorMask | gameMask;
                    if (visibleMask != dysonSphereMasks[___dysonSphere])
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
        }

        static readonly AutoResetEvent autoEvent = new AutoResetEvent(false);

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Export))]
        public static void Export_Prefix(DysonSphere __instance)
        {
            // Generate data for vanilla save
            if (!DysonShellCompress.Enable && dysonSphereMasks.ContainsKey(__instance))
            {
                if (ThreadingHelper.Instance.InvokeRequired)
                {
                    // Wait for GenerateModel run on main thread
                    autoEvent.Reset();
                    ThreadingHelper.Instance.StartSyncInvoke(() =>
                    {
                        DysonShellCompress.GenerateModel(__instance, -1);
                        autoEvent.Set();
                    });
                    autoEvent.WaitOne(-1);
                }
                else
                {
                    DysonShellCompress.GenerateModel(__instance, -1);
                }
                if (!ReduceRAM)
                    dysonSphereMasks.Remove(__instance);
                DysonShellCompress.FreeRAM();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Export))]
        public static void Export_Postfix(DysonSphere __instance)
        {
            if (!DysonShellCompress.Enable && dysonSphereMasks.ContainsKey(__instance) && __instance != viewingSphere)
            {
                if (ThreadingHelper.Instance.InvokeRequired)
                {
                    // Wait for RemoveAllVerts run on main thread
                    autoEvent.Reset();
                    ThreadingHelper.Instance.StartSyncInvoke(() =>
                    {
                        RemoveAllVerts(__instance);
                        autoEvent.Set();
                    });
                    autoEvent.WaitOne(-1);
                }
                else
                {
                    RemoveAllVerts(__instance);
                }
                // Set visibleMask to all disabled
                dysonSphereMasks[__instance] = 0;
            }
        }

        private static void RemoveAllVerts(DysonSphere dysonSphere)
        {
            int count = 0;
            for (int lid = 1; lid < dysonSphere.layersIdBased.Length; lid++)
            {
                if (dysonSphere.layersIdBased[lid] != null && dysonSphere.layersIdBased[lid].id == lid)
                {
                    DysonSphereLayer dysonSphereLayer = dysonSphere.layersIdBased[lid];
                    for (int shellId = 1; shellId < dysonSphereLayer.shellCursor; shellId++)
                    {
                        if (dysonSphereLayer.shellPool[shellId] != null && dysonSphereLayer.shellPool[shellId].id == shellId)
                        {
                            RemoveVerts(dysonSphereLayer.shellPool[shellId]);
                            count++;
                        }
                    }
                }
            }
            Log.Debug($"ReduceRAM: Remove verts for {count} shells.");
            System.GC.Collect();
        }

        static readonly List<DysonNode> emptyNodes = new List<DysonNode>(0);
        static readonly Dictionary<int, int> emptyNodeIndexMap = new Dictionary<int, int>(0);
        static readonly List<DysonFrame> emptyFrames = new List<DysonFrame>(0);
        static readonly List<VectorLF3> emptyPolygon = new List<VectorLF3>(0);

        public static void RemoveVerts(DysonShell dysonShell)
        {
            // Preserve only necessary information need to rebuild model
            int layerId = dysonShell.layerId;
            int id = dysonShell.id;
            int protoId = dysonShell.protoId;
            int randSeed = dysonShell.randSeed;
            Color32 color = dysonShell.color;
            int[] nodecps = dysonShell.nodecps;
            int[] vertsqOffset = dysonShell.vertsqOffset;
            int vertexCount = dysonShell.vertexCount;
            int cpPerVertex = dysonShell.cpPerVertex;
            // SetEmpty use Clear() for containers, so give it dummpy empty containers
            var nodes = dysonShell.nodes;
            var nodeIndexMap = dysonShell.nodeIndexMap;
            var frames = dysonShell.frames;
            var polygon = dysonShell.polygon;
            dysonShell.nodes = emptyNodes;
            dysonShell.nodeIndexMap = emptyNodeIndexMap;
            dysonShell.frames = emptyFrames;
            dysonShell.polygon = emptyPolygon;

            dysonShell.SetEmpty();

            dysonShell.layerId = layerId;
            dysonShell.id = id;
            dysonShell.protoId = protoId;
            dysonShell.randSeed = randSeed;
            dysonShell.color = color;
            dysonShell.nodecps = nodecps;
            dysonShell.vertsqOffset = vertsqOffset;
            dysonShell.vertexCount = vertexCount;
            dysonShell.cpPerVertex = cpPerVertex;

            dysonShell.nodes = nodes;
            dysonShell.nodeIndexMap = nodeIndexMap;
            dysonShell.frames = frames;
            dysonShell.polygon = polygon;
        }

        static int shellCount = 0;

        public static void GenerateModelObjectsGuard(DysonShell dysonShell)
        {
            if (Enable)
            {
                RemoveVerts(dysonShell);
                shellCount++;
            }
            else
                dysonShell.GenerateModelObjects();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(DysonShell), nameof(DysonShell.Import))]
        public static IEnumerable<CodeInstruction> DysonShellImport_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            try
            {
                // Replace: this.GenerateModelObjects()
                // To     : GenerateModelObjectsGuard(this);
                var codeMatcher = new CodeMatcher(instructions, iL)
                    .End()
                    .MatchBack(true, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "GenerateModelObjects"))
                    .SetOperandAndAdvance(typeof(LazyLoading).GetMethod(nameof(LazyLoading.GenerateModelObjectsGuard)));

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception err)
            {
                Log.Error("DysonShellImport_Transpiler error! Lazyload will not work on vanilla save");
                Log.Error(err);
                return instructions;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.Import)), HarmonyPriority(Priority.Low)]
        static void DysonSphereImportPostfix(DysonSphere __instance)
        {
            if (Enable && shellCount > 0)
            {
                Add(__instance);
                Log.Debug($"[{__instance.starData.index,2}] lazy load: skip {shellCount} shells.");
                shellCount = 0;
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
                    Vector3 point = (1 - a - b) * centerPoint + a * vert1 + b * vert2;
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
