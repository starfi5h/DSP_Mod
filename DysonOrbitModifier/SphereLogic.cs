using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;


namespace DysonOrbitModifier
{

    class SphereLogic
    {
        public static ManualLogSource logger;
        public static bool moveStructure;
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

        public static void ChangeSwarm(DysonSphere sphere, int id, float radius, Quaternion rotation)
        {
            ConvertQuaternion(sphere.swarm.orbits[id].rotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Swarm[{id}]: ({sphere.swarm.orbits[id].radius}, {inclination}, {longitude})");
            sphere.swarm.orbits[id].radius = radius;
            sphere.swarm.orbits[id].rotation = rotation;
            sphere.swarm.orbits[id].up = rotation * Vector3.up;
            ConvertQuaternion(sphere.swarm.orbits[id].rotation, out inclination, out longitude);
            logger.LogInfo($" Current Swarm[{id}]: ({sphere.swarm.orbits[id].radius}, {inclination}, {longitude})");
        }

        public static void ChangeLayer(DysonSphere sphere, int id, float radius, Quaternion rotation, float angularSpeed)
        {
            DysonSphereLayer layer = sphere.GetLayer(id);
            
            (layer.orbitRadius, radius) = (radius, layer.orbitRadius);
            (layer.orbitRotation, rotation) = (rotation, layer.orbitRotation);
            (layer.orbitAngularSpeed, angularSpeed) = (angularSpeed, layer.orbitAngularSpeed);
            //Quaternion currentRotation = layer.currentRotation;
            layer.currentRotation = layer.orbitRotation * Quaternion.Euler(0f, -layer.currentAngle, 0f); //Update Rotation
            layer.nextRotation = layer.orbitRotation * Quaternion.Euler(0f, -layer.currentAngle - layer.orbitAngularSpeed * 0.016666668f, 0f);


            ConvertQuaternion(rotation, out float inclination, out float longitude);
            logger.LogInfo($"Previous Layer[{id}]: ({radius}, {inclination}, {longitude}, {angularSpeed})");
            ConvertQuaternion(layer.orbitRotation, out inclination, out longitude);
            logger.LogInfo($" Current Layer[{id}]: ({layer.orbitRadius}, {inclination}, {longitude}, {angularSpeed})");

            if (moveStructure && radius != layer.orbitRadius)
            {
                MoveStructure(sphere, id);
                if (correctOnChange)
                    ValueCorrection(sphere.GetLayer(id));
            }
            else
                SyncNodeBuffer(sphere, id);
            
        }


        public static void MoveStructure(DysonSphere sphere, int layerId)
        {
            DysonSphereLayer layer = sphere.GetLayer(layerId);
            MoveNodes(layer);
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
                    sphere.nrdPool[node.rid].layerRot = layer.currentRotation; //value set at sphere.GameTick() 
                }
            }
            sphere.nrdBuffer.SetData(sphere.nrdPool);
        }

        public static void MoveNodes(DysonSphereLayer layer)
        {
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                DysonNode node = layer.nodePool[i];
                if (node != null && node.id == i)
                {
                    Vector3 pos = node.pos;
                    node.pos = node.pos.normalized * layer.orbitRadius;
                    logger.LogDebug($"Node [{i}]: pos {pos}->{node.pos}");
                }
            }            
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
            //List<uint> vertcps = new List<uint>(6); //vertcps, vertexCount will change when radius change

            for (int sid = 0; sid < layer.shellCursor; sid++)
            {
                DysonShell shell = layer.shellPool[sid];
                if (shell == null || sid != shell.id)
                    continue;

                nodeIds.Clear();
                nodecps.Clear();
                //vertcps.Clear();
                for (int n = 0; n < shell.nodes.Count; n++)
                {
                    nodeIds.Add(shell.nodes[n].id);
                }
                for (int n = 0; n < shell.nodes.Count + 1; n++) //nodecps.Count = this.nodes.Count + 1, the last one is sum
                {
                    nodecps.Add(shell.nodecps[n]);
                    shell.nodecps[n] = 0;                    
                }
                Array.Clear(shell.vertcps, 0, shell.vertcps.Length);
                int vertexCount = shell.vertexCount;
                int protoId = shell.protoId; //material

                layer.RemoveDysonShell(shell.id);
                //logger.LogDebug($"[{i}] v:{vertexCount} cp:[{string.Join(",", nodeIds.ToArray())}] cp({string.Join(",", nodecps.ToArray())}) ");


                int id = layer.NewDysonShell(protoId, nodeIds);
                shell = layer.shellPool[id];
                //logger.LogDebug($"[{id}] v:{shell.vertexCount}");

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

}
