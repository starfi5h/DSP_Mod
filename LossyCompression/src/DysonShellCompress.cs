using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LossyCompression
{
    class DysonShellCompress
    {
        // Remove all shell data in original, save the necessary data for reconstruction in .moddsv
        // 24+(nodesCount)*4 bytes data for a shell

        public static bool Enable { get; set; }
        public static bool IsMultithread { get; set; }
        public static readonly int EncodedVersion = 2; //PeekChar() max is 127?

        public static void Export(BinaryWriter w)
        {
            if (!Enable)
            {
                w.Write(0);
                return;
            }

            var stopWatch = new HighStopwatch();
            stopWatch.Begin();
            long datalen = -w.BaseStream.Length;

            w.Write(EncodedVersion);
            w.Write(GameMain.data.dysonSpheres.Length);
            for (int starIndex = 0; starIndex < GameMain.data.dysonSpheres.Length; starIndex++)
            {
                if (GameMain.data.dysonSpheres[starIndex] != null)
                {
                    //Log.Info($"{j} {GameMain.data.dysonSpheres[j].starData.index}");
                    w.Write(starIndex);
                    Encode(GameMain.data.dysonSpheres[starIndex], w);
                }
                else
                    w.Write(-1);
            }

            datalen += w.BaseStream.Length;
            PerformanceMonitor.dataLengths[(int)ESaveDataEntry.Total] += datalen;
            PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonSphere] += datalen;
            PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonShell] += datalen;
            Log.Info($"Compress DysonShell: {datalen:N0} bytes {stopWatch.duration}s");
        }

        public static void Encode(DysonSphere dysonSphere, BinaryWriter w)
        {
            w.Write(dysonSphere.layersIdBased.Length);
            for (int layerId = 1; layerId < dysonSphere.layersIdBased.Length; layerId++)
            {
                if (dysonSphere.layersIdBased[layerId] != null && dysonSphere.layersIdBased[layerId].id == layerId)
                {
                    DysonSphereLayer dysonSphereLayer = dysonSphere.layersIdBased[layerId];
                    w.Write(layerId);
                    w.Write(dysonSphereLayer.shellCapacity);
                    w.Write(dysonSphereLayer.shellCursor);
                    w.Write(dysonSphereLayer.shellRecycleCursor);
                    // shellCount = shellCursor - shellRecycleCursor - 1;
                    int shellCount = 0;
                    for (int n = 1; n < dysonSphereLayer.shellCursor; n++)
                    {
                        if (dysonSphereLayer.shellPool[n] != null && dysonSphereLayer.shellPool[n].id == n)
                        {
                            DysonShell dysonShell = dysonSphereLayer.shellPool[n];
                            #region ExportShell: modify from DysonShell.ExportAsBlueprint

                            w.Write(dysonShell.id);
                            w.Write(dysonShell.protoId);
                            w.Write(dysonShell.randSeed);
                            w.Write(dysonShell.color.r);
                            w.Write(dysonShell.color.g);
                            w.Write(dysonShell.color.b);
                            w.Write(dysonShell.color.a);
                            w.Write(dysonShell.nodes.Count);
                            for (int i = 0; i < dysonShell.nodes.Count; i++)
                                w.Write(dysonShell.nodes[i].id);
                            //nodecps.Count = this.nodes.Count + 1, the last one is sum			
                            for (int j = 0; j < dysonShell.nodes.Count + 1; j++)
                                w.Write(dysonShell.nodecps[j]);
                            //vertexCount, vertsqOffset , cpPerVertex are needed in constructCp
                            for (int j = 0; j < dysonShell.nodes.Count + 1; j++)
                                w.Write(dysonShell.vertsqOffset[j]);
                            w.Write(dysonShell.vertexCount);
                            w.Write(dysonShell.cpPerVertex);

                            #endregion
                            shellCount++;
                        }
                    }
                    Assert.True(shellCount == (shellCursor - shellRecycleCursor - 1));
                    for (int num = 0; num < dysonSphereLayer.shellRecycleCursor; num++)
                        w.Write(dysonSphereLayer.shellRecycle[num]);
                }
                else
                    w.Write(0);
            }
        }

        public static void Import(BinaryReader r)
        {
            int version = r.ReadInt32();
            if (version <= EncodedVersion)
            {
                long datalen = -r.BaseStream.Length;
                var stopWatch = new HighStopwatch();
                stopWatch.Begin();

                int dysonSpheresLength = r.ReadInt32();
                Assert.True(dysonSpheresLength == GameMain.data.dysonSpheres.Length);
                for (int j = 0; j < dysonSpheresLength; j++)
                {
                    int starIndex = r.ReadInt32();
                    if (starIndex != -1)
                    {
                        Decode(GameMain.data.dysonSpheres[starIndex], r, version);
                    }
                }

                datalen += r.BaseStream.Length;
                PerformanceMonitor.dataLengths[(int)ESaveDataEntry.Total] += datalen;
                PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonSphere] += datalen;
                PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonShell] += datalen;
                Log.Info($"Decompress DysonShell: {datalen:N0} bytes {stopWatch.duration}s");
                FreeRAM();
            }
        }

        public static void Decode(DysonSphere dysonSphere, BinaryReader r, int version)
        {
            List<DysonShell> dysonShells = new List<DysonShell>();
            List<int> list = new List<int>(12);
            int layerLength = r.ReadInt32();
            Assert.True(layerLength == dysonSphere.layersIdBased.Length);
            for (int lid = 1; lid < layerLength; lid++)
            {
                int layerId = r.ReadInt32();
                if (layerId != 0)
                {
                    //Log.Warn($"layerId: {layerId}");
                    DysonSphereLayer dysonSphereLayer = dysonSphere.layersIdBased[layerId];
                    dysonSphereLayer.shellCapacity = r.ReadInt32();
                    dysonSphereLayer.shellCursor = r.ReadInt32();
                    dysonSphereLayer.shellRecycleCursor = r.ReadInt32();
                    dysonSphereLayer.shellPool = new DysonShell[dysonSphereLayer.shellCapacity];
                    dysonSphereLayer.shellRecycle = new int[dysonSphereLayer.shellCapacity];

                    int shellCount = dysonSphereLayer.shellCursor - dysonSphereLayer.shellRecycleCursor - 1;
                    //Log.Warn($"shellCount: {shellCount}");

                    for (int n = 1; n <= shellCount; n++)
                    {
                        #region ImportShell: modify form DysonShell.ImportFromBlueprint
                        DysonShell dysonShell = new DysonShell(dysonSphereLayer)
                        {
                            layerId = layerId,
                            id = r.ReadInt32(),
                            protoId = r.ReadInt32(),
                            randSeed = r.ReadInt32(),
                            color = new Color32(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte())
                        };

                        list.Clear();
                        int nodesCount = r.ReadInt32();
                        for (int i = 0; i < nodesCount; i++)
                            list.Add(r.ReadInt32()); // nodeId                       

                        //nodecps.Count = this.nodes.Count + 1, the last one is sum
                        dysonShell.nodecps = new int[nodesCount + 1];
                        for (int i = 0; i < nodesCount + 1; i++)
                            dysonShell.nodecps[i] = r.ReadInt32();

                        if (version >= 2)
                        {
                            dysonShell.vertsqOffset = new int[nodesCount + 1];
                            for (int i = 0; i < nodesCount + 1; i++)
                                dysonShell.vertsqOffset[i] = r.ReadInt32();
                            dysonShell.vertexCount = r.ReadInt32();
                            dysonShell.cpPerVertex = r.ReadInt32();
                        }
                        #endregion

                        #region Add collections and gnerate polygon
                        for (int i = 0; i < nodesCount; i++)
                        {
                            int nodeId = list[i];
                            DysonNode dysonNode = dysonSphere.FindNode(dysonShell.layerId, nodeId);
                            Assert.NotNull(dysonNode);
                            if (dysonNode != null)
                            {
                                dysonShell.nodeIndexMap[nodeId] = dysonShell.nodes.Count;
                                dysonShell.nodes.Add(dysonNode);
                                if (!dysonNode.shells.Contains(dysonShell))
                                    dysonNode.shells.Add(dysonShell);
                            }
                        }

                        for (int j = 0; j < nodesCount; j++)
                        {
                            int index = j;
                            int index2 = (j + 1) % nodesCount;
                            DysonFrame dysonFrame = DysonNode.FrameBetween(dysonShell.nodes[index], dysonShell.nodes[index2]);
                            Assert.NotNull(dysonFrame);
                            dysonShell.frames.Add(dysonFrame);
                        }

                        DysonNode[] nodePool = dysonSphere.layersIdBased[dysonShell.layerId].nodePool;
                        for (int k = 0; k < list.Count; k++)
                        {
                            DysonNode dysonNode2 = nodePool[list[k % list.Count]];
                            DysonNode b = nodePool[list[(k + 1) % list.Count]];
                            DysonFrame dysonFrame2 = DysonNode.FrameBetween(dysonNode2, b);
                            List<Vector3> segments = dysonFrame2.GetSegments();
                            if (dysonNode2 == dysonFrame2.nodeA)
                            {
                                for (int l = 0; l < segments.Count - 1; l++)
                                    dysonShell.polygon.Add(segments[l]);
                            }
                            else
                            {
                                for (int m = segments.Count - 1; m >= 1; m--)
                                    dysonShell.polygon.Add(segments[m]);
                            }
                        }
                        #endregion

                        // Save in the list to do GenerateGeometry and GenerateModelObjects laster
                        dysonShells.Add(dysonShell);

                        // Place dysonShell into the correct position
                        dysonSphereLayer.shellPool[dysonShell.id] = dysonShell;
                    }

                    for (int num = 0; num < dysonSphereLayer.shellRecycleCursor; num++)
                    {
                        dysonSphereLayer.shellRecycle[num] = r.ReadInt32();
                    }
                }
            }

            if (dysonShells.Count > 0)
            {
                if (LazyLoading.Enable && version >= 2)
                {
                    LazyLoading.Add(dysonSphere);
                }
                else
                {
                    // Old version doesn't support lazy loading. Check for all layers.
                    GenerateModel(dysonSphere, -1); // -1 => all bits in mask on
                }
                // Recalculate CP request for all nodes
                for (int layerId = 1; layerId < dysonSphere.layersIdBased.Length; layerId++)
                {
                    if (dysonSphere.layersIdBased[layerId] != null && dysonSphere.layersIdBased[layerId].id == layerId)
                    {
                        DysonSphereLayer dysonSphereLayer = dysonSphere.layersIdBased[layerId];
                        for (int nodeId = 1; nodeId < dysonSphereLayer.nodeCursor; nodeId++)
                        {
                            if (dysonSphereLayer.nodePool[nodeId] != null && dysonSphereLayer.nodePool[nodeId].id == nodeId)
                                dysonSphereLayer.nodePool[nodeId].RecalcCpReq();
                        }
                    }
                }
            }
        }

        public static int GenerateModel(DysonSphere dysonSphere, int bitMask)
        {
            double t1, t2;
            var stopwatch = new HighStopwatch();
            stopwatch.Begin();
            List<DysonShell> dysonShells = new List<DysonShell>();

            for (int layerId = 1; layerId < dysonSphere.layersIdBased.Length; layerId++)
            {
                if ((bitMask & (1 << layerId)) > 0)
                {
                    if (dysonSphere.layersIdBased[layerId] != null && dysonSphere.layersIdBased[layerId].id == layerId)
                    {
                        DysonSphereLayer dysonSphereLayer = dysonSphere.layersIdBased[layerId];
                        for (int shellId = 1; shellId < dysonSphereLayer.shellCursor; shellId++)
                        {
                            if (dysonSphereLayer.shellPool[shellId] != null && dysonSphereLayer.shellPool[shellId].id == shellId)
                            {
                                if (dysonSphereLayer.shellPool[shellId].verts == null)
                                {
                                    dysonShells.Add(dysonSphereLayer.shellPool[shellId]);
                                }
                            }
                        }
                    }
                }
            }

            if (dysonShells.Count > 0)
            {
                stopwatch.Begin();
                if (IsMultithread && dysonShells.Count > 8)
                {
                    GenerateGeometryParallel(dysonShells);
                }
                else
                {
                    foreach (var dysonShell in dysonShells)
                        dysonShell.GenerateGeometry();
                }
                t1 = stopwatch.duration;

                stopwatch.Begin();
                foreach (var dysonShell in dysonShells)
                    dysonShell.GenerateModelObjects();
                t2 = stopwatch.duration;

                Log.Debug($"[{dysonSphere.starData.index,2}] Generate {dysonShells.Count,4} shells.  Time: {t1:F4} | {t2:F4}");
            }
            return dysonShells.Count;
        }


#pragma warning disable CS8321

        private static readonly Dictionary<int, Vector3>[] s_vmap = new Dictionary<int, Vector3>[3];
        private static readonly Dictionary<int, Vector3>[] s_outvmap = new Dictionary<int, Vector3>[3];
        private static readonly Dictionary<int, int>[] s_ivmap = new Dictionary<int, int>[3];

        static void GenerateGeometryParallel(List<DysonShell> dysonShells)
        {
            var currentIndex = dysonShells.Count;
            var tasks = new Task[3];

            for (int i = 0; i < 3; i++)
            {
                if (s_vmap[i] == null)
                {
                    s_vmap[i] = new Dictionary<int, Vector3>();
                    s_outvmap[i] = new Dictionary<int, Vector3>();
                    s_ivmap[i] = new Dictionary<int, int>();
                }
            }

            tasks[0] = Task.Run(() =>
            {
                while (true)
                {
                    int decIndex = Interlocked.Decrement(ref currentIndex);
                    if (decIndex < 0)
                        break;
                    GenerateGeometry0(dysonShells[decIndex]);
                }
            });

            tasks[1] = Task.Run(() =>
            {
                while (true)
                {
                    int decIndex = Interlocked.Decrement(ref currentIndex);
                    if (decIndex < 0)
                        break;
                    GenerateGeometry1(dysonShells[decIndex]);
                }
            });

            tasks[2] = Task.Run(() =>
            {
                while (true)
                {
                    int decIndex = Interlocked.Decrement(ref currentIndex);
                    if (decIndex < 0)
                        break;
                    GenerateGeometry2(dysonShells[decIndex]);
                }
            });

            while (true)
            {
                int decIndex = Interlocked.Decrement(ref currentIndex);
                if (decIndex < 0)
                    break;
                dysonShells[decIndex].GenerateGeometry();
            }
            Task.WaitAll(tasks);
        }

        public static void FreeRAM()
        {
            for (int i = 0; i < 3; i++)
            {
                if (s_vmap[i] == null)
                {
                    s_vmap[i] = null;
                    s_outvmap[i] = null;
                    s_ivmap[i] = null;
                }
            }
        }

        // this feels wrong :/
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DysonShell), nameof(DysonShell.GenerateGeometry))]
        public static void GenerateGeometry0(DysonShell _)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                instructions = ReplaceDictionary(instructions, 0);
                return instructions;
            }
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(DysonShell), nameof(DysonShell.GenerateGeometry))]
        public static void GenerateGeometry1(DysonShell _)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                instructions = ReplaceDictionary(instructions, 1);
                return instructions;
            }
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(DysonShell), nameof(DysonShell.GenerateGeometry))]
        public static void GenerateGeometry2(DysonShell _)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                instructions = ReplaceDictionary(instructions, 2);
                return instructions;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceDictionary(IEnumerable<CodeInstruction> instructions, int index)
        {
            // replace s_vmap with s_vmap[index]
            var codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "s_vmap"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(DysonShellCompress), "s_vmap"))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, index))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_Ref, null))
                );

            // replace s_outvmap with s_outvmap[index]
            codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "s_outvmap"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(DysonShellCompress), "s_outvmap"))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, index))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_Ref, null))
                );

            // replace s_outvmap with s_outvmap[index]
            codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "s_ivmap"))
                .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(DysonShellCompress), "s_ivmap"))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_S, index))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldelem_Ref, null))
                );
            return codeMatcher.InstructionEnumeration();
        }

#pragma warning restore CS8321


        static int shellCapacity, shellCursor, shellRecycleCursor;

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.Export))]
        public static void DysonSphereLayer_Export_Prefix(DysonSphereLayer __instance)
        {
            if (!Enable) return;

            shellCapacity = __instance.shellCapacity;
            shellCursor = __instance.shellCursor;
            shellRecycleCursor = __instance.shellRecycleCursor;
            __instance.shellCapacity = 64;
            __instance.shellCursor = 1;
            __instance.shellRecycleCursor = 0;
            return;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.Export))]
        public static void DysonSphereLayer_Export_Postfix(DysonSphereLayer __instance)
        {
            if (!Enable) return;

            __instance.shellCapacity = shellCapacity;
            __instance.shellCursor = shellCursor;
            __instance.shellRecycleCursor = shellRecycleCursor;
            return;
        }
    }
}