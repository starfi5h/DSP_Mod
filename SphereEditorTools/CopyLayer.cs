using System;
using System.Collections.Generic;
using UnityEngine;

namespace SphereEditorTools
{
    class CopyLayer
    {
        static LayerData layerData;

        public static string Name()
        {
            if (layerData == null)
                return "";
            else
                return $": {layerData.name}";
        }
        public static void Clear()
        {
            layerData = null;
        }
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
            if (layerData == null || layer == null)
            {
                Comm.SetInfoString("Paste failed! No data", 120);
                return false;
            }
            if (layer.nodeCount > 0 || layer.frameCount > 0 || layer.shellCount > 0)
            {
                Comm.SetInfoString("Paste failed! Target layer needs to be empty.", 120);
                return false;
            }
            string title = Stringpool.Paste + " " + Stringpool.LAYER;
            string content = $"{Stringpool.Paste} {layerData.name} to {Stringpool.LAYER}[{layer.id}] ?";
            var messagebox = UIMessageBox.Show(
                title,
                content,
                "否".Translate(), "是".Translate(), 2,
                null, new UIMessageBox.Response(() => Paste(layer, mode)));
            var go = GameObject.Find("UI Root/Always on Top/Overlay Canvas - Top/Dyson Editor Top");
            if (go != null)
            {
                messagebox.transform.SetParent(go.transform); //move mssagebox on top
                messagebox.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
            return true;
        }

        static void Paste(DysonSphereLayer layer, int mode)
        {
            layerData.Paste(layer, mode);
            Comm.dysnoPanel.layerSelected = layer.id;
            Comm.dysnoPanel.nodeSelected = 0;
            Comm.dysnoPanel.frameSelected = 0;
            Comm.dysnoPanel.shellSelected = 0;
            Comm.dysnoPanel.UpdateSelectionVisibleChange();
        }



        [Serializable]
        public class LayerData
        {
            public string name;
            public Vector3 rotation;
            public List<Node> nodes;
            public List<Frame> frames;
            public List<Shell> shells;
            public struct Node
            {
                public Node(DysonNode node)
                {
                    protoId = node.protoId;
                    pos = node.pos.normalized;
                }
                public int protoId;
                public Vector3 pos;
            }
            public struct Frame
            {
                public Frame(DysonFrame frame)
                {
                    protoId = 0;
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
                        nodeIds.Add(shell.nodes[i].id);
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
                rotation = layer.currentRotation.eulerAngles;
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
                for (int i = 0; i < layer.shellCursor; i++)
                {
                    DysonShell shell = layer.shellPool[i];
                    if (shell != null && shell.id == i)
                    {
                        Shell shell0 = new Shell(shell);
                        for (int k = 0; k < shell.nodes.Count; k++)
                            shell0.nodeIds[k] = nodeIndex[shell0.nodeIds[k]];
                        shells.Add(shell0);
                    }
                }

                name = $"[{layer.id}] - N{nodes.Count},F{frames.Count},S{shells.Count}";
            }

            public void Paste(DysonSphereLayer layer, int mode)
            {
                //Log.LogDebug($"Paste {layer.id} Mode {mode}");
                int[] nodeIndex = new int[nodes.Count];
                layer.gridMode = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    Vector3 pos = layer.orbitRadius * nodes[i].pos; //Snap to grid
                    if (mode == 1)
                        pos = Quaternion.Inverse(layer.currentRotation) * Quaternion.Euler(rotation) * pos; //Restore roation
                    nodeIndex[i] = layer.NewDysonNode(nodes[i].protoId, pos); //storeId -> newId
                    if (nodeIndex[i] <= 0)
                        Log.LogWarning($"Add node {i} unsuccess");
                }
                for (int i = 0; i < frames.Count; i++)
                {
                    int nodeAId = nodeIndex[frames[i].nodeAId];
                    int nodeBId = nodeIndex[frames[i].nodeBId];
                    if (layer.NewDysonFrame(frames[i].protoId, nodeAId, nodeBId, frames[i].euler) <= 0)
                        Log.LogWarning($"Add frame {i} unsuccess");
                }
                var nodeIds = new List<int>();
                for (int i = 0; i < shells.Count; i++)
                {
                    nodeIds.Clear();
                    for (int j = 0; j < shells[i].nodeIds.Count; j++)
                        nodeIds.Add(nodeIndex[shells[i].nodeIds[j]]);
                    if (layer.NewDysonShell(shells[i].protoId, nodeIds) <= 0)
                        Log.LogWarning($"Add shell {i} unsuccess");
                }
            }
        }
    }
}
