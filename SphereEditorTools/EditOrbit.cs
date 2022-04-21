using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SphereEditorTools
{
    public class EditOrbit
    {
        public static bool EnableAnchorMode;
        public static float AuglarSpeed;
        static DysonSphereLayer selectedLayer;


        public static bool Toggle(bool enable)
        {
            //DysonSphereLayer currentLayer = UIRoot.instance.uiGame.dysonEditor.selection.singleSelectedLayer;
            if (enable && selectedLayer != null)
            {
                selectedLayer.orbitAngularSpeed = 0f; //Stop selected layer rotation
                EnableAnchorMode = true;
            }
            else if (!enable && selectedLayer != null)
            {
                selectedLayer.orbitAngularSpeed = AuglarSpeed;
                EnableAnchorMode = false;
            }
            return EnableAnchorMode;
        }

        public static void TrySetAngularSpeed(float angularSpeed)
        {
            if (selectedLayer != null)
            {
                if (angularSpeed < 0)
                    AuglarSpeed = Mathf.Sqrt(selectedLayer.dysonSphere.gravity / selectedLayer.orbitRadius) / selectedLayer.orbitRadius * 57.29578f;
                else
                    AuglarSpeed = angularSpeed;
                Log.LogInfo($"Set angular speed of layer[{selectedLayer.id}]: {selectedLayer.orbitAngularSpeed} => {AuglarSpeed}");
                selectedLayer.orbitAngularSpeed = AuglarSpeed;
                UIWindow.SpeedInput = AuglarSpeed.ToString();
                EnableAnchorMode = false;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DESelection), "NotifySelectionChange")]
        public static void DESelection_Postfix(DESelection __instance)
        {
            if (__instance.singleSelectedLayer != selectedLayer)
            {
                if (EnableAnchorMode)
                {
                    selectedLayer.orbitAngularSpeed = AuglarSpeed;
                    EnableAnchorMode = false;
                }
                if (__instance.singleSelectedLayer != null)
                {
                    AuglarSpeed = __instance.singleSelectedLayer.orbitAngularSpeed;
                    UIWindow.SpeedInput = AuglarSpeed.ToString();
                }
                selectedLayer = __instance.singleSelectedLayer;                
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereLayer), "InitOrbitRotation")]
        public static bool InitOrbitRotation_Prefix(DysonSphereLayer __instance, Quaternion __1)
        {
            if (EnableAnchorMode)
            {
                ChangeLayer(__instance, __1);
                return false;
            }
            return true;
        }
        

        public static void ConvertQuaternion(Quaternion rotation, out float inclination, out float longitude)
        {
            inclination = rotation.eulerAngles.z == 0 ? 0 : 360 - rotation.eulerAngles.z;
            longitude = rotation.eulerAngles.y == 0 ? 0 : 360 - rotation.eulerAngles.y;
        }

        public static void ChangeLayer(DysonSphereLayer layer, Quaternion rotation)
        {
            (layer.orbitRotation, rotation) = (rotation, layer.orbitRotation);

            ConvertQuaternion(rotation, out float inclination, out float longitude);
            Log.LogInfo($"Previous Layer[{layer.id}]: ({inclination},{longitude})");
            ConvertQuaternion(layer.orbitRotation, out inclination, out longitude);
            Log.LogInfo($" Current Layer[{layer.id}]: ({inclination},{longitude})");

            Quaternion previousRotation = layer.currentRotation;
            layer.currentAngle = 0; //Angle is set to 0 in anchorMode
            layer.currentRotation = layer.orbitRotation * Quaternion.Euler(0f, -layer.currentAngle, 0f); //Update Rotation
            MoveStructure(layer, previousRotation);
        }

        public static void MoveStructure(DysonSphereLayer layer, Quaternion previousRotation)
        {
            DysonSphere sphere = layer.dysonSphere;
            MoveNodes(layer, previousRotation);
            SyncNodeBuffer(layer);
            MoveFrames(layer);
            sphere.CheckAutoNodes(); //clear inactive node
            for (int count = sphere.autoNodeCount; count < 8; ++count)
                sphere.PickAutoNode(); //add rocket target node
            sphere.modelRenderer.RebuildModels();
            MoveShells(layer);
        }

        public static void SyncNodeBuffer(DysonSphereLayer layer)
        {
            DysonSphere sphere = layer.dysonSphere;
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
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                DysonNode node = layer.nodePool[i];
                if (node != null && node.id == i)
                {
                    if (EnableAnchorMode)
                        node.pos = Quaternion.Inverse(layer.currentRotation) * previousRotation * node.pos;
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

                    Log.LogDebug($"Frame[{i}]: spMax {spMax}->{frame.spMax}; " + $"ReqSP nodeA[{frame.nodeA.id}]:{spReqOrderA}->{frame.nodeA.spReqOrder}, nodeB[{frame.nodeB.id}]:{spReqOrderB}->{frame.nodeB.spReqOrder}");

                    frame.segPoints = null; //Reset private field segPoints to recalculate position
                }
            }
        }

        public static void MoveShells(DysonSphereLayer layer)
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
                    for (cp = nodecps[index]; cp > 0; cp--)
                    {
                        if (!shell.Construct(index))
                            break;
                    }
                    //logger.LogDebug($"[{index}] old_cp:{nodecps[index]} new_cp:{shell.nodecps[index]} remain_cp:{cp}");
                    shell.nodes[index].RecalcCpReq();
                    shell.nodecps[index] += cp; //Extra cell point, store for future change
                }
                Log.LogDebug($"Shell[{sid}]:Vertex {vertexCount}->{shell.vertexCount}; Total CP {nodecps[shell.nodes.Count]}->{shell.nodecps[shell.nodes.Count]}");
                shell.nodecps[shell.nodes.Count] = nodecps[shell.nodes.Count]; //Store for future change
            }
        }



    }
}
