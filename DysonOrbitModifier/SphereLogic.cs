using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DysonOrbitModifier
{
    class SphereLogic
    {
        public static ManualLogSource logger;
        public static bool syncPosition;
        public static bool syncAltitude;
        public static bool syncSwarm;
        public static bool correctOnChange;

        public static int CheckSwarmRadius(DysonSphere sphere, float orbitRadius)
        {
            foreach (var planet in sphere.starData.planets)
            {
                if (planet.orbitAround > 0)
                    continue;
                if (Mathf.Abs(planet.orbitRadius * 40000.0f - orbitRadius) < 2199.95f)
                    return -2;
            }
            return 0;
        }

        public static int CheckLayerRadius(DysonSphere sphere, int layerId, float orbitRadius)
        {
            foreach (var planet in sphere.starData.planets)
            {
                if (planet.orbitAround > 0)
                    continue;
                if (Mathf.Abs(planet.orbitRadius * 40000.0f - orbitRadius) < 2199.95f)
                    return -2;
            }
            for (int i = 0; i < 10; i++)
            {
                if (sphere.layersSorted[i] != null && Mathf.Abs(sphere.layersSorted[i].orbitRadius - orbitRadius) < 999.95f)
                {
                    if (sphere.layersIdBased[layerId].orbitRadius == sphere.layersSorted[i].orbitRadius)
                        continue;
                    else
                        return -1;
                }
            }
            return 0;
        }

        public static void ConvertQuaternion(Quaternion rotation, out float inclination, out float longitude)
        {
            inclination = rotation.eulerAngles.z == 0 ? 0 : 360 - rotation.eulerAngles.z;
            longitude = rotation.eulerAngles.y == 0 ? 0 : 360 - rotation.eulerAngles.y;
        }

        public static void ChangeSwarm(DysonSphere sphere, int orbitId, float radius, Quaternion rotation)
        {
            DysonSwarm swarm = sphere.swarm;
            ConvertQuaternion(sphere.swarm.orbits[orbitId].rotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Swarm[{orbitId}]: ({sphere.swarm.orbits[orbitId].radius}, {inclination}, {longitude})");

            if (syncSwarm) {
                int count = 0;
                Vector3 up = rotation * Vector3.up;
                Quaternion inverse = Quaternion.Inverse(swarm.orbits[orbitId].rotation);
                double k = Math.Sqrt((double)sphere.gravity / radius);
                swarm.sailPoolForSave = new DysonSail[swarm.sailCapacity];
                swarm.swarmBuffer.GetData(swarm.sailPoolForSave, 0, 0, swarm.sailCursor);
                for (int i = 0; i < swarm.sailCursor; i++)
                {
                    if (swarm.sailPoolForSave[i].st != orbitId)
                        continue;
                    DysonSail ss = swarm.sailPoolForSave[i];
                    VectorLF3 posLF = new VectorLF3(ss.px, ss.py, ss.pz);
                    posLF = rotation * inverse * posLF.normalized * radius;
                    VectorLF3 velLF = VectorLF3.Cross(posLF, up).normalized * k;
                    ss.px = (float)posLF.x;
                    ss.py = (float)posLF.y;
                    ss.pz = (float)posLF.z;
                    ss.vx = (float)velLF.x;
                    ss.vy = (float)velLF.y;
                    ss.vz = (float)velLF.z;
                    swarm.sailPoolForSave[i] = ss;
                    count++;
                }
                swarm.swarmBuffer.SetData(swarm.sailPoolForSave, 0, 0, swarm.sailCapacity);
                swarm.sailPoolForSave = null;
                logger.LogDebug($"Change sails: {count}");
            }

            swarm.orbits[orbitId].radius = radius;
            swarm.orbits[orbitId].rotation = rotation;
            swarm.orbits[orbitId].up = rotation * Vector3.up;

            ConvertQuaternion(sphere.swarm.orbits[orbitId].rotation, out inclination, out longitude);
            logger.LogInfo($" Current Swarm[{orbitId}]: ({sphere.swarm.orbits[orbitId].radius}, {inclination}, {longitude})");
        }

        public static void ChangeLayer(DysonSphere sphere, int id, float radius, Quaternion rotation, float angularSpeed)
        {
            DysonSphereLayer layer = sphere.GetLayer(id);
            
            (layer.orbitRadius, radius) = (radius, layer.orbitRadius);
            (layer.orbitRotation, rotation) = (rotation, layer.orbitRotation);
            (layer.orbitAngularSpeed, angularSpeed) = (angularSpeed, layer.orbitAngularSpeed);


            ConvertQuaternion(rotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Layer[{id}]: ({radius}, {inclination}, {longitude}, {angularSpeed})");
            ConvertQuaternion(layer.orbitRotation, out inclination, out longitude);
            logger.LogInfo($" Current Layer[{id}]: ({layer.orbitRadius}, {inclination}, {longitude}, {angularSpeed})");


            if ((syncPosition || rotation == layer.orbitRotation) && (!syncAltitude || radius == layer.orbitRadius)) //node.pos is not changed
            {
                SyncNodeBuffer(sphere, id);
            }
            else
            {
                Quaternion previousRotation = layer.currentRotation;
                layer.currentAngle = syncPosition || (layer.orbitRotation == rotation) ? layer.currentAngle : 0; //if syncPosition is false and orbitRotation is changed, angle set to 0
                layer.currentRotation = layer.orbitRotation * Quaternion.Euler(0f, -layer.currentAngle, 0f); //Update Rotation
                MoveStructure(sphere, id, previousRotation);
                if (radius != layer.orbitRadius)
                    ValueCorrection(sphere.GetLayer(id));
            }                
            
        }


        public static void MoveStructure(DysonSphere sphere, int layerId, Quaternion previousRotation)
        {
            DysonSphereLayer layer = sphere.GetLayer(layerId);
            MoveNodes(layer, previousRotation);
            SyncNodeBuffer(sphere, layerId);
            MoveFrames(layer);
            sphere.CheckAutoNodes(); //clear inactive node
            for (int count = sphere.autoNodeCount; count < 8; ++count)
                sphere.PickAutoNode(); //add rocket target node
            sphere.modelRenderer.RebuildModels();
            MoveShell(layer);
        }

        public static void SyncNodeBuffer(DysonSphere sphere, int layerId)
        {
            DysonSphereLayer layer = sphere.GetLayer(layerId);
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                DysonNode node = layer.nodePool[i];
                if (node != null && node.id == i)
                {
                    sphere.nrdPool[node.rid].pos = node.pos;
                    sphere.nrdPool[node.rid].angularVel = layer.orbitAngularSpeed;
                    //sphere.nrdPool[node.rid].layerRot = layer.currentRotation; //value set at sphere.GameTick() 
                }
            }
            sphere.nrdBuffer.SetData(sphere.nrdPool);
        }

        public static void MoveNodes(DysonSphereLayer layer, Quaternion previousRotation)
        {
            int count = 0;
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                DysonNode node = layer.nodePool[i];
                if (node != null && node.id == i)
                {
                    Vector3 pos = node.pos;
                    if (!syncPosition)
                        node.pos = Quaternion.Inverse(layer.currentRotation) * previousRotation * node.pos;
                    if (syncAltitude)
                        node.pos = node.pos.normalized * layer.orbitRadius;
                    //logger.LogDebug($"Node [{i}]: pos {pos}->{node.pos}");
                    count++;
                }
            }
            logger.LogDebug($"Node count: {count}");
        }

        public static void MoveFrames(DysonSphereLayer layer)
        {
            for (int i = 0; i < layer.frameCursor; i++)
            {
                DysonFrame frame = layer.framePool[i];
                if (frame != null && frame.id == i)
                {
                    int spMax = frame.spMax;
                    int spReqOrderA = frame.nodeA.spReqOrder;
                    int spReqOrderB = frame.nodeB.spReqOrder;

                    frame.spMax = frame.segCount * 10;
                    frame.nodeA.RecalcSpReq();
                    frame.nodeB.RecalcSpReq();

                    string str = $"Frame[{i}]: spMax {spMax}->{frame.spMax}; ";
                    str += $"ReqSP nodeA[{frame.nodeA.id}]:{spReqOrderA}->{frame.nodeA.spReqOrder}, nodeB[{frame.nodeB.id}]:{spReqOrderB}->{frame.nodeB.spReqOrder}";
                    logger.LogDebug(str);

                    frame.segPoints = null; //Reset private field segPoints to recalculate position
                }
            }
        }

        public static void MoveShell(DysonSphereLayer layer)
        {
            List<int> nodeIds = new List<int>(6);
            List<int> nodecps = new List<int>(6);

            for (int sid = 0; sid < layer.shellCursor; sid++)
            {
                DysonShell shell = layer.shellPool[sid];
                if (shell == null || sid != shell.id)
                    continue;

                nodeIds.Clear();
                nodecps.Clear();
                for (int index = 0; index < shell.nodes.Count; index++)
                {
                    nodeIds.Add(shell.nodes[index].id);
                }
                for (int index = 0; index < shell.nodes.Count + 1; index++) //nodecps.Count = this.nodes.Count + 1, the last one is sum
                {
                    nodecps.Add(shell.nodecps[index]);
                    shell.nodecps[index] = 0;                    
                }
                Array.Clear(shell.vertcps, 0, shell.vertcps.Length);
                int vertexCount = shell.vertexCount;
                int protoId = shell.protoId; //material

                layer.RemoveDysonShell(shell.id);
                //logger.LogDebug($"[{i}] v:{vertexCount} cp:[{string.Join(",", nodeIds.ToArray())}] cp({string.Join(",", nodecps.ToArray())}) ");
                int id = layer.NewDysonShell(protoId, nodeIds);
                shell = layer.shellPool[id];

                //Fill back cell point
                for (int index = 0; index < shell.nodes.Count; index++)
                {
                    int cp;
                    for (cp = nodecps[index]; cp > 0 ; cp--)
                    {
                        if (!shell.Construct(index))
                            break;
                    }
                    //logger.LogDebug($"[{index}] old_cp:{nodecps[index]} new_cp:{shell.nodecps[index]} remain_cp:{cp}");
                    shell.nodes[index].RecalcCpReq();
                    shell.nodecps[index] += cp; //Store for future change
                }
                logger.LogDebug($"Shell[{sid}]:Vertex {vertexCount}->{shell.vertexCount}; Total CP {nodecps[shell.nodes.Count]}->{shell.nodecps[shell.nodes.Count]}");
                shell.nodecps[shell.nodes.Count] = nodecps[shell.nodes.Count]; //Store for future change
            }

        }

        //Cut extra structe points and cell points
        public static void ValueCorrection(DysonSphereLayer layer)
        {
            int correctSpCount = 0;
            int correctCpCount = 0;
            
            for (int fid = 0; fid < layer.frameCursor; fid++)
            {
                DysonFrame frame = layer.framePool[fid];
                if (frame == null || frame.id != fid)
                    continue;

                int halfMax = frame.spMax >> 1;
                
                if (frame.spA > halfMax)
                {
                    frame.spA = halfMax;
                    frame.nodeA.RecalcSpReq();
                    correctSpCount++;
                }
                if (frame.spB > halfMax)
                {
                    frame.spB = halfMax;
                    frame.nodeB.RecalcSpReq();
                    correctSpCount++;
                }
            }

            for (int sid = 0; sid < layer.shellCursor; sid++)
            {
                DysonShell shell = layer.shellPool[sid];
                if (shell == null || shell.id != sid)
                    continue;

                shell.nodecps[shell.nodes.Count] = 0;
                for (int index = 0; index < shell.nodes.Count; index++)
                {
                    int cpMax = (shell.vertsqOffset[index + 1] - shell.vertsqOffset[index]) * 2;
                    if (shell.nodecps[index] > cpMax)
                    {
                        shell.nodecps[index] = cpMax;
                        shell.nodes[index].RecalcCpReq();
                        correctCpCount++;
                    }
                    shell.nodecps[shell.nodes.Count] += shell.nodecps[index];                    
                }

            }

            if ((correctSpCount + correctCpCount) > 0 )
                logger.LogDebug($"ValueCorrection layer[{layer.id}] : SP[{correctSpCount}] CP[{correctCpCount}]");
        }

    }

    class ChainedRotation
    {
        internal static bool enable;
        internal static List<Tuple<int, int>> sequenceList;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "Resume")]
        public static void GameMain_Resume()
        {
            DysonOrbitModifier.Config.Reload();
            Stringpool.Set();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(DysonSphere), "GameTick")]
        static IEnumerable<CodeInstruction> DysonSphere_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //change roation after DysonLayer.GameTick(int) done in for loop
            var code = instructions.ToList();
            var backup = new List<CodeInstruction>(code);
            try
            {
                
                int targetIndex = 0;
                for (int i = 0; i < code.Count; i++)
                {
                    if (targetIndex == 0 && code[i].opcode == OpCodes.Callvirt && (MethodInfo)code[i].operand == AccessTools.Method(typeof(DysonSphereLayer), "GameTick"))
                        targetIndex = -1; //stage 1
                    if (targetIndex == -1 && code[i].opcode == OpCodes.Blt)
                    {
                        targetIndex = i; //stage 2
                        break;
                    }
                }
                var codeToInsert = new List<CodeInstruction>();
                codeToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                codeToInsert.Add(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ChainedRotation), "ChangeLayersRotaiton")));
                code.InsertRange(targetIndex + 1, codeToInsert);
            }
            catch (Exception e)
            {                
                code = backup;
                SphereLogic.logger.LogError(e);
                SphereLogic.logger.LogWarning("Transpiler failed. Restore original IL");
            }
            return code.AsEnumerable();     
        }

        static void ChangeLayersRotaiton(DysonSphere sphere)
        {
            if (enable)
            {
                foreach (var tuple in sequenceList)
                {
                    DysonSphereLayer layer1 = sphere.GetLayer(tuple.Item1);
                    DysonSphereLayer layer2 = sphere.GetLayer(tuple.Item2);
                    if (layer1 != null && layer2 != null)
                    {
                        layer2.currentRotation = layer1.currentRotation * layer2.currentRotation;
                        layer2.nextRotation = layer1.nextRotation * layer2.nextRotation;
                    }
                }
            }
        }
    }


}
