using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace SphereEditorTools
{
    class CopyLayer
    {
        static LayerData layerData;




        public static bool TryCopy(DysonSphereLayer layer)
        {
            if (layer == null)
                return false;
            layerData = new LayerData();
            layerData.Copy(layer);
            return true;
        }

        public static bool TryPaste(DysonSphereLayer layer, int mode)
        {
            if (layer == null || (layer.nodeCount > 0 || layer.frameCount > 0 || layer.shellCount > 0))
            {
                Comm.SetInfoString("Paste failed! Target layer needs to be empty.", 120);
                return false;
            }
            if (layerData == null)
            {
                Comm.SetInfoString("Paste failed! No copied layer data", 120);
                return false;
            }
            var messagebox = UIMessageBox.Show(
                Stringpool.Paste + " " + Stringpool.LAYER, //Remove Layer
                Stringpool.Paste + " " + Stringpool.LAYER + " [" + layer.id + "] ?",
                "否".Translate(), "是".Translate(), 2,
                null, new UIMessageBox.Response(() => layerData.Paste(layer, 0)));
            var go = GameObject.Find("UI Root/Always on Top/Overlay Canvas - Top/Dyson Editor Top");
            if (go != null)
            {
                messagebox.transform.SetParent(go.transform); //move mssagebox on top
                messagebox.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
            return true;
        }





        [Serializable]
        public class LayerData
        {
            public String name;
            public Quaternion currentRotation;
            public List<Node> nodes;
            public List<Frame> frames;
            public List<Shell> shells;
            public struct Node
            {
                public Node(DysonNode node)
                {
                    protoId = node.protoId;
                    pos = node.pos;
                }
                public int protoId;
                public Vector3 pos;
            }
            public struct Frame
            {
                public Frame(DysonFrame frame)
                {
                    protoId = frame.protoId;
                    nodeAId = frame.nodeA.id;
                    nodeBId = frame.nodeB.id;
                    euler = frame.euler;
                }
                public int protoId;
                public int nodeAId;
                public int nodeBId;
                public bool euler;
            }
            public struct Shell
            {
                public Shell(DysonShell shell)
                {
                    protoId = shell.protoId;
                    nodeIds = new List<int>();
                    for (int i = 0; i < shell.nodes.Count; i++)
                    {
                        nodeIds.Add(shell.nodes[i].id);
                    }
                }
                public int protoId;
                public List<int> nodeIds;
            }

            public LayerData()
            {
                nodes = new List<Node>();
                frames = new List<Frame>();
                shells = new List<Shell>();
            }
            public void Copy(DysonSphereLayer layer)
            {
                name = layer.id.ToString();
                currentRotation = layer.currentRotation;
                nodes.Clear();
                frames.Clear();
                shells.Clear();
                Dictionary<int, int> nodeIndex = new Dictionary<int, int>(); 

                for (int i = 0; i < layer.nodeCursor; i++)
                {
                    DysonNode node = layer.nodePool[i];
                    if (node != null && node.id == i)
                    {
                        Node node0 = new Node(node);
                        nodeIndex[node.id] = nodes.Count; //originId -> storeId
                        nodes.Add(node0);
                    }
                }

                for (int i = 0; i < layer.frameCursor; i++)
                {
                    DysonFrame frame = layer.framePool[i];
                    if (frame != null && frame.id == i)
                    {
                        Frame frame0 = new Frame(frame);
                        frame0.nodeAId = nodeIndex[frame0.nodeAId];
                        frame0.nodeBId = nodeIndex[frame0.nodeBId];
                        frames.Add(frame0);
                    }
                }
                return;
                for (int sid = 0; sid < layer.shellCursor; sid++)
                {
                    DysonShell shell = layer.shellPool[sid];
                    if (shell != null || sid == shell.id)
                    {
                        Shell shell0 = new Shell(shell);
                        for (int i = 0; i < shell.nodes.Count; i++)
                        {
                            Log.LogDebug($"{shell0.nodeIds[i]} {nodeIndex.TryGetValue(shell0.nodeIds[i], out _)}");
                            //shell0.nodeIds[i] = nodeIndex[shell0.nodeIds[i]];
                        }
                        shells.Add(shell0);
                    }
                }
            }

            public void Paste(DysonSphereLayer layer, int mode)
            {
                Log.LogDebug($"Paste {layer.id} Mode {mode}");
                Dictionary<int, int> nodeIndex = new Dictionary<int, int>(); 

                for (int i = 0; i < nodes.Count; i++)
                {
                    Vector3 pos = nodes[i].pos * layer.orbitRadius; //Snap to grid
                    if (mode == 1)
                        pos = Quaternion.Inverse(layer.currentRotation) * currentRotation * pos;
                    nodeIndex[i] = layer.NewDysonNode(nodes[i].protoId, pos); //storeId -> newId
                    Log.LogDebug(nodeIndex[i]);
                }
                for (int i = 0; i < frames.Count; i++)
                {
                    int nodeAId = nodeIndex[frames[i].nodeAId];
                    int nodeBId = nodeIndex[frames[i].nodeBId];
                    layer.NewDysonFrame(frames[i].protoId, nodeAId, nodeBId, frames[i].euler);
                }
                for (int i = 0; i < shells.Count; i++)
                {
                    var nodeIds = new List<int>();
                    for (int j = 0; j < shells[i].nodeIds.Count; j++)
                        nodeIds.Add(nodeIndex[shells[i].nodeIds[j]]);
                    layer.NewDysonShell(shells[i].protoId, nodeIds);
                }
            }
        }
    }
}
