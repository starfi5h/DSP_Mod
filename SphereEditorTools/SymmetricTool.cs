using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

using BepInEx;
using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonPanel.EBrushMode;

namespace SphereEditorTools 
{


    class SymmetricTool : BaseUnityPlugin
    {
        static int tick;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonBrush_Node), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Frame), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Shell), "_OnUpdate")]
        public static void KeyLogger()
        {
            if (Input.GetMouseButtonDown(0))
                Debug.Log("Pressed primary button.");

            if (tick == 0)
            {
                //Log.LogDebug("test");
            }

            //tick = (tick+1)%30;            
        }

        
        static bool overwrite;

        //static int downButton; //0: prime 1:second -1:none
        static Vector3 castPoint;
        static bool resultCast;
        static bool resultSnap;

        static List<UIDysonBrush>[] brushes;
        static UIDysonPanel dysnoPanel;

        public static bool SymmetricMode;
        public static bool MirrorMode;
        public static int RadialCount; //start from 1

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void Init(UIDysonPanel __instance)
        {
            dysnoPanel = __instance;
            brushes = new List<UIDysonBrush>[dysnoPanel.brushes.Length];
            for (int i = 0; i < brushes.Length; i++)
            {
                brushes[i] = new List<UIDysonBrush>();
                brushes[i].Add(null); //the position of original brush
            }
            try
            {
                ChangeParameters(true, 3);
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void Free()
        {
            if (brushes != null)
            {
                for (int i = 0; i < brushes.Length; i++)
                {
                    foreach (var brush in brushes[i])
                    {
                        if (brush?.gameObject != null)
                            Destroy(brush?.gameObject);
                    }
                }
                brushes = null;
            }
            Log.LogDebug("Free");
        }

        public static void ChangeParameters(bool newMirrorMode, int newRadialCount)
        {
            MirrorMode = newMirrorMode;
            RadialCount = newRadialCount;
            SymmetricMode = true;
            int newCount = RadialCount * (MirrorMode ? 2 : 1);
            while (brushes[(int)BrushMode.Node].Count < newCount)
            {
                AddBrushes();
            }
            Log.LogDebug($"{brushes[(int)BrushMode.Node].Count} {MirrorMode} {RadialCount}");
            Comm.SetInfoString($"鏡像:{MirrorMode} 旋轉:{RadialCount}", 120);
        }

        static void AddBrushes()
        {

            //GameObject obj = ((UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node]).preview.transform.parent.gameObject;
            GameObject brushesGroup = ((UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node]).preview.transform.parent.parent.gameObject;
            //brushesGroup = Instantiate(brushesGroup, brushesGroup.transform.parent);
            //brushesGroup.name = "brushesGroup";

            Log.LogDebug("Add Brushes");
            UIDysonBrush_Node target1 = (UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node];

            //UIDysonBrush_Node brushNode = new UIDysonBrush_Node();
            //UysonBrush_Node brushNode = brushesGroup.AddComponent<UIDysonBrush_Node>();

            UIDysonBrush_Node brushNode = (UIDysonBrush_Node)Instantiate(target1, target1.transform.parent);
            
            /*
            GameObject go = new GameObject("brush_nodes(clone)");
            go.transform.parent = brushesGroup.transform;
            UIDysonBrush_Node brushNode = go.AddComponent<UIDysonBrush_Node>();
            */

            brushNode._OnInit();

            Log.LogDebug($"{brushNode} {target1}");

            /*
            brushNode.preview = Instantiate(target1.preview, brushNode.transform);
            brushNode.previewMesh = brushNode.preview.GetComponent<MeshFilter>();
            brushNode.previewRenderer = brushNode.preview.GetComponent<MeshRenderer>();
            brushNode.LoadNodeModel(target1.nodeProtoId);
            
            brushNode._Close();
            */

            Log.LogDebug($"{brushNode.preview} {brushNode.previewMesh} {brushNode.previewRenderer}");
            brushes[(int)BrushMode.Node].Add(brushNode);

            //brushNode._Create();

            DysonSphere sphere = dysnoPanel.viewDysonSphere;
            DysonSphereLayer layer = sphere?.GetLayer(dysnoPanel.layerSelected);
            brushNode.SetDysonSphere(sphere); //Does this change when update?
            brushNode.layer = layer;


            brushNode._OnClose();

            Log.LogDebug($"{brushNode} typeof{brushNode}");
            brushNode.RecalcCollides(new Vector3(0,0,0));

            brushNode._Open();
            brushNode._Update(); //Does this work?

            //brushNode._OnUpdate(); //Or this?
            Log.LogDebug("End");


        }



        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateBrushes")]
        static void wee_OnUpdate()
        {
            try
            {
                

                if (SymmetricMode && tick == 0)
                {
                    //Log.LogDebug("S Start");
                    DysonSphere sphere = dysnoPanel.viewDysonSphere;
                    DysonSphereLayer layer = sphere?.GetLayer(dysnoPanel.layerSelected);
                    string errorText = dysnoPanel.brushError;

                    overwrite = true;
                    Vector3 pos = castPoint;

                    for (int i = 0; i < dysnoPanel.brushes.Length; i++)
                    {
                        //Log.LogDebug($"{i} {dysnoPanel.brushMode} {i == (int)dysnoPanel.brushMode} {dysnoPanel.brushMode == BrushMode.Node}");
                        if (i == (int)dysnoPanel.brushMode && dysnoPanel.brushMode == BrushMode.Node)
                        {
                            for (int t = 1; t < RadialCount; t++)
                            {
                                Log.LogDebug($"{t} {castPoint}");
                                castPoint = Quaternion.Euler(0f, 360f * t / RadialCount, 0f) * pos;
                                brushes[i][t].SetDysonSphere(sphere); //Does this change when update?
                                brushes[i][t].layer = layer;                                
                                brushes[i][t]._Open();
                                brushes[i][t].gameObject.SetActive(true);
                                brushes[i][t]._OnUpdate();
                                Log.LogDebug(brushes[i][t]);
                                
                            }
                            if (MirrorMode)
                            {
                                pos.y = -pos.y;
                                for (int t = RadialCount; t < 2 * RadialCount; t++)
                                {
                                    Log.LogDebug($"{t} {castPoint}");
                                    castPoint = Quaternion.Euler(0f, 360f * t / RadialCount, 0f) * pos;
                                    brushes[i][t].SetDysonSphere(sphere); //Does this change when update?
                                    brushes[i][t].layer = layer;
                                    brushes[i][t].active = true;
                                    brushes[i][t]._Open();
                                    brushes[i][t].gameObject.SetActive(true);
                                    brushes[i][t]._OnUpdate();
                                    Log.LogDebug(brushes[i][t]);
                                }
                            }
                        }
                        else
                        {
                            foreach (var brush in brushes[i]) {
                                brush?._Close();
                                brush?.gameObject.SetActive(false);
                            }
                        }
                    }
                    overwrite = false;
                    dysnoPanel.brushError =  errorText;

                    //Log.LogDebug("S End");
                }                
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
            //tick = (tick + 1) % 1;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Node), "_OnUpdate")]
        public static void Node_OnUpdate(UIDysonBrush_Node __instance)
        {
            //if (tick == 0)
            //    Log.LogDebug(__instance.preview.transform.position);
        }



        #region Overwrite

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIDysonBrush_Node), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Frame), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Shell), "_OnUpdate")]
        public static IEnumerable<CodeInstruction> Transpiler_OnUpdate(IEnumerable<CodeInstruction> instructions)
        {
            
            var code = instructions.ToList();
            var backup = new List<CodeInstruction>(code);

            var methodRaySnap = typeof(UIDysonDrawingGrid).GetMethod("RaySnap");
            var methodRayCast = typeof(UIDysonDrawingGrid).GetMethod("RayCast");
            var methodMouseDown = typeof(Input).GetMethod("GetMouseButtonDown");

            //Log.LogDebug("Patch");

            try
            {
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].operand is MethodInfo mi)
                    {
                        /*
                        if (code[i].opcode == OpCodes.Call && mi == methodMouseDown)
                        {
                            code[i].operand = typeof(SymmetricTool).GetMethod("Overwrite_GetMouseButtonDown");
                            //Log.LogDebug($"[{i}]  Replce GetMouseButtonDown {code[i].operand}");
                        }
                        */
                        if (code[i].opcode == OpCodes.Callvirt && mi == methodRaySnap)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetricTool).GetMethod("Overwrite_RaySnap");
                            //Log.LogDebug($"[{i}]  Replce RaySnap {code[i].operand}");
                        }
                        else if (code[i].opcode == OpCodes.Callvirt && mi == methodRayCast)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetricTool).GetMethod("Overwrite_RayCast");
                            //Log.LogDebug($"[{i}]  Replce RayCast {code[i].operand}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e);
                code = backup;
                Log.LogWarning("Use backup");

            }
            //Log.LogDebug("Restore code");
            return code.AsEnumerable();
        }

        /*
        public static bool Overwrite_GetMouseButtonDown(int button)
        {
            if (overwrite)
            {
                return button == downButton;
            }
            bool result = Input.GetMouseButtonDown(button);
            if (result == true)
                downButton = button;
            return result;
        }
        */
        public static bool Overwrite_RayCast(UIDysonDrawingGrid uidysonDrawingGrid, Ray lookRay, Quaternion rotation, float radius, out Vector3 cast, bool front = true)
        {
            if (overwrite)
            {
                cast = castPoint;
                return resultCast;
            }
            resultCast = uidysonDrawingGrid.RayCast(lookRay, rotation, radius, out cast, front);
            castPoint = cast;
            return resultCast;
        }

        public static bool Overwrite_RaySnap(UIDysonDrawingGrid uidysonDrawingGrid, Ray lookRay, out Vector3 snap)
        {
            if (overwrite)
            {
                snap = castPoint;
                return resultSnap;
            }
            resultSnap = uidysonDrawingGrid.RaySnap(lookRay, out snap);
            castPoint = snap;
            if (tick == 0)
                Log.LogDebug(castPoint);
            return resultSnap;
        }

        #endregion

    }










    class SymmetricTool2 : BaseUnityPlugin
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Node), "_OnInit")]
        public static void test_OnInit()
        {
            Log.LogDebug("Init");
        }
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Node), "_OnFree")]
        public static void test_OnFree()
        {
            Log.LogDebug("_OnFree");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Node), "_OnClose")]
        public static void test_OnClose()
        {
            Log.LogDebug("_OnClose");
        }







        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void Free()
        {
            if (previewGroup)
                Destroy(previewGroup);
            /*
            for (int i = 0; i < previewNodeA.Count; i++)
            {
                if (previewNodeA[i])
                    Destroy(previewNodeA[i]);
                if (previewNodeB[i])
                    Destroy(previewNodeB[i]);
                if (previewFrame[i])
                    Destroy(previewFrame[i]);
            }
            */
        }

        public static UIDysonPanel dysnoPanel;
        static GameObject previewGroup;
        static List<GameObject> previewNodeA; //frame.preview0
        static List<GameObject> previewNodeB; //frame.preview1
        static List<GameObject> previewFrame; //frame.preview2

        public static bool MirrorMode = true;
        public static int RadialCount = 2; //total N+1
        static int previewCount = 1;


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void Init(UIDysonPanel __instance)
        {
            dysnoPanel = __instance;
            previewNodeA = new List<GameObject>();
            previewNodeB = new List<GameObject>();
            previewFrame = new List<GameObject>();

            GameObject obj = ((UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node]).preview.transform.parent.gameObject;
            //GameObject obj = GameObject.Find("UI Root/Dyson Map/brushes/brush-remove");
            previewGroup = Instantiate(obj, obj.transform.parent);
            previewGroup.name = "brush-preview";
            previewGroup.SetActive(true);
            //previewGroup = GameObject.Find("UI Root/Dyson Map/brushes");

            Log.LogDebug(previewGroup);

            try
            {
                OnChange(true, 8);
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }

            Log.LogDebug(previewNodeA.Count);

        }

        public static void OnChange(bool _mirrorMode, int _radialCount)
        {
            MirrorMode = _mirrorMode;
            RadialCount = _radialCount;
            int newCount = RadialCount * (MirrorMode ? 2 : 1);
            while (previewNodeA.Count < newCount)
            {
                AddObject();
            }
            for (int i = newCount; i < previewCount; i++) //Set extra as unactive
            {
                previewNodeA[i].SetActive(false);
                previewNodeB[i].SetActive(false);
                previewFrame[i].SetActive(false);
            }
            previewCount = newCount;
        }

        public static void AddObject()
        {
            GameObject obj = null;

            obj = ((UIDysonBrush_Frame)dysnoPanel.brushes[(int)BrushMode.FrameEuler]).preview0;
            previewNodeA.Add(Instantiate(obj, previewGroup.transform));            
            previewNodeA[previewNodeA.Count - 1].SetActive(false);
            MeshRenderer render = previewNodeA[previewNodeA.Count - 1].GetComponent<MeshRenderer>();
            render.sharedMaterial = ((UIDysonBrush_Node)(dysnoPanel.brushes[(int)BrushMode.Node])).previewOkMat;

            obj = ((UIDysonBrush_Frame)dysnoPanel.brushes[(int)BrushMode.FrameEuler]).preview1;
            //previewNodeB.Add(Instantiate(obj, previewGroup.transform));
            //previewNodeB[previewNodeB.Count - 1].SetActive(false);
            //MeshRenderer render = previewNodeB[previewNodeB.Count - 1].GetComponent<MeshRenderer>();
           // render.sharedMaterial = ((UIDysonBrush_Node)(dysnoPanel.brushes[(int)BrushMode.Node])).previewWhiteMat;



            obj = ((UIDysonBrush_Frame)dysnoPanel.brushes[(int)BrushMode.FrameEuler]).preview2;
            previewFrame.Add(Instantiate(obj, previewGroup.transform));
            previewFrame[previewFrame.Count - 1].SetActive(false);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Node), "_OnUpdate")]
        public static void Node_OnUpdate(UIDysonBrush_Node __instance)
        {
            if (__instance.preview.activeSelf)
            {
                
                
                Vector3 pos = __instance.preview.transform.position; //this.layer.currentRotation * node.pos.normalize * this.layer.orbitRadius * 0.00025f;
                Quaternion roatation = __instance.preview.transform.rotation; //this.layer.currentRotation * Maths.SphericalRotation(node.pos.normalize, 0f);
                Quaternion currentRotation = __instance.layer.currentRotation;
                pos = Quaternion.Inverse(currentRotation) * pos; //node.pos

                Vector3 storePos;

                for (int t = 1; t < RadialCount; t++) {
                    previewNodeA[t].SetActive(true);
                    storePos = Quaternion.Euler(0f, 360f * t / RadialCount, 0f) * pos;
                    previewNodeA[t].transform.position = currentRotation * storePos;
                    previewNodeA[t].transform.rotation = currentRotation * Maths.SphericalRotation(storePos.normalized, 0f);
                    //previewNodeA[t].transform.rotation = Quaternion.Euler(0f, 360f * t / RadialCount, 0f) * roatation;
                }
                if (MirrorMode)
                {
                    //pos = Quaternion.Inverse(__instance.layer.currentRotation) * pos;
                    pos.y = -pos.y;
                    roatation = Maths.SphericalRotation(pos.normalized, 0f);
                    for (int t = 0; t < RadialCount; t++)
                    {
                        previewNodeA[RadialCount + t].SetActive(true);
                        storePos = Quaternion.Euler(0f, 360f * t / RadialCount, 0f) * pos;
                        previewNodeA[RadialCount + t].transform.position = currentRotation * storePos;
                        previewNodeA[RadialCount + t].transform.rotation = currentRotation * Maths.SphericalRotation(storePos.normalized, 0f);

                    }
                }
            }
            else
            {
                for (int i = 0; i < previewCount; i++)
                {
                    previewNodeA[i].SetActive(false);
                }
            }
            //Log.LogDebug($"{__instance.preview.activeSelf}");
        }











    }
}
