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


    class SymmetryTool : BaseUnityPlugin
    {
        static int tick;


        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphere), "UpdateStates", new Type[] { typeof(DysonShell), typeof(uint), typeof(bool), typeof(bool) })]
        public static void Wee(DysonShell shell, uint state, bool add, bool remove)
        {
            if (tick % 60 == 1)
            {
                Log.LogDebug($"Shell id{shell.id} state{shell.state} new state {state} {add}{remove}");
            }
        }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDysonPanel), "SetSelectedNode")]
        [HarmonyPatch(typeof(UIDysonPanel), "SetSelectedFrame")]
        [HarmonyPatch(typeof(UIDysonPanel), "SetSelectedShell")]
        public static bool Overwrite_SetSelected()
        {
            if (overwrite)
            {
                //Log.LogDebug("Block Overwrite_SetSelected");
                return false;
            }
            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonBrush_Node), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Frame), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Shell), "_OnUpdate")]
        public static void KeyLogger()
        {
            if (Input.GetMouseButtonDown(0))
                Debug.Log("Pressed primary button.");

            
            //if (tick == 0)
            {
                //Log.LogDebug("test");
            }
            

            //tick = (tick+1)%30;            
        }

        
        static bool overwrite;

        static Vector3 dataPoint; //store in sphere data, normalized
        static float castRadius;
        static bool resultSnap;
        
        static Vector3 clickPoint; //currentRoation * dataPoint
        static float castDist;
        static Vector3 rayOrigin;

        static List<UIDysonBrush>[] brushes;
        static UIDysonPanel dysnoPanel;

        static bool mirrorMode;
        static int rdialCount; //start from 1

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void Init(UIDysonPanel __instance)
        {
            dysnoPanel = __instance;
            brushes = new List<UIDysonBrush>[dysnoPanel.brushes.Length];
            for (int i = 0; i < brushes.Length; i++)
            {
                brushes[i] = new List<UIDysonBrush>();
                brushes[i].Add(null); //placeholder for original brush
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
            int previewCount = rdialCount * (mirrorMode ? 2 : 1);
            mirrorMode = newMirrorMode;
            rdialCount = newRadialCount;
            int newCount = rdialCount * (mirrorMode ? 2 : 1);
            while (brushes[(int)BrushMode.Node].Count < newCount)
            {
                AddBrushes();
            }
            for (int i = 0; i < brushes.Length; i++)
            {
                if (brushes[i].Count < previewCount)
                    continue;
                for (int t = newCount; t < previewCount; t++) //Set extra as unactive
                {
                    brushes[i][t]?._Close();
                    brushes[i][t]?.gameObject.SetActive(false);
                }
            }
            Log.LogDebug($"ChangeParameters C:{brushes[(int)BrushMode.Node].Count} M:{mirrorMode} R:{rdialCount}");
            
        }

        static void AddBrushes()
        {
            //GameObject brushesGroup = ((UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node]).preview.transform.parent.parent.gameObject;
            int id;
            
            id = (int)BrushMode.Node;
            UIDysonBrush_Node brushNode = (UIDysonBrush_Node)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);            
            brushes[id].Add(brushNode);
            foreach (Transform child in brushNode.gameObject.transform)
                Destroy(child.gameObject);
            brushNode._OnInit();

            id = (int)BrushMode.FrameGeo;
            UIDysonBrush_Frame brushFrame = (UIDysonBrush_Frame)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);
            brushes[id].Add(brushFrame);
            foreach (Transform child in brushFrame.gameObject.transform)
                Destroy(child.gameObject);
            brushFrame._OnInit();
            brushFrame.isEuler = false;

            id = (int)BrushMode.FrameEuler;
            brushFrame = (UIDysonBrush_Frame)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);
            brushes[id].Add(brushFrame);
            foreach (Transform child in brushFrame.gameObject.transform)
                Destroy(child.gameObject);
            brushFrame._OnInit();
            brushFrame.isEuler = true;


            id = (int)BrushMode.Shell;
            UIDysonBrush_Shell brushShell = (UIDysonBrush_Shell)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);
            brushes[id].Add(brushShell);
            foreach (Transform child in brushShell.gameObject.transform)
                Destroy(child.gameObject);
            brushShell._OnInit();

            id = (int)BrushMode.Remove;
            UIDysonBrush_Remove brushRemove = (UIDysonBrush_Remove)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);
            brushes[id].Add(brushRemove);
            foreach (Transform child in brushRemove.gameObject.transform)
                Destroy(child.gameObject);
            brushRemove._OnInit();


            id = (int)BrushMode.Select;
            UIDysonBrush_Select brushSelect = (UIDysonBrush_Select)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);
            brushes[id].Add(brushSelect);
            foreach (Transform child in brushSelect.gameObject.transform)
                Destroy(child.gameObject);
            brushSelect._OnInit();

        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateBrushes")]
        static void Brushes_OnUpdate()
        {
            try
            {                

                if (rdialCount * (mirrorMode ? 2 : 1) > 1) //symmetric mode on
                {
                    //Log.LogDebug("S Start");
                    DysonSphere sphere = dysnoPanel.viewDysonSphere;
                    DysonSphereLayer layer = sphere?.GetLayer(dysnoPanel.layerSelected);
                    string errorText = dysnoPanel.brushError;

                    overwrite = true;
                    Vector3 pos = dataPoint;
                    Quaternion currentRotation = Quaternion.identity;
                    /*
                    if (clickPoint != Vector3.zero)
                    {
                        Ray ray = dysnoPanel.screenCamera.ScreenPointToRay(Input.mousePosition);
                        castRadius = (float)(ray.origin.magnitude / (rayOrigin.magnitude * 0.00025));
                        bool front = Vector3.Dot(ray.direction, clickPoint) < 0f ? true : false;
                        for (int i = sphere.layersSorted.Length - 1; i >= 0; i--)
                        {
                            DysonSphereLayer layer2 = front ? sphere.layersSorted[i] : sphere.layersSorted[sphere.layersSorted.Length - 1 - i];
                            if (layer2 != null && Mathf.Abs(layer2.orbitRadius - castRadius) < 1E-08)
                            {
                                castRadius = layer2.orbitRadius;
                                currentRotation = layer2.currentRotation;
                                pos = Quaternion.Inverse(currentRotation) * clickPoint;
                                break;
                            }
                        }                            
                    }
                    */
                    if (clickPoint != Vector3.zero)
                    {
                        DysonNode nodeGizmo;
                        if (dysnoPanel.brushMode == BrushMode.Select)
                            nodeGizmo = ((UIDysonBrush_Select)dysnoPanel.brushes[(int)BrushMode.Select]).castNodeGizmo;
                        else
                            nodeGizmo = ((UIDysonBrush_Remove)dysnoPanel.brushes[(int)BrushMode.Remove]).castNodeGizmo;

                        if (nodeGizmo != null)
                        {
                            DysonSphereLayer layer2 = sphere.GetLayer(nodeGizmo.layerId);
                            castRadius = 0; //Don't select frame and shell.
                            currentRotation = layer2.currentRotation;
                            pos = nodeGizmo.pos.normalized;
                            //pos = Quaternion.Inverse(currentRotation) * clickPoint;
                            if (tick%60 == 1)
                                Log.LogDebug($"nodeGizmo {nodeGizmo.layerId} {nodeGizmo.pos}");
                        }
                    }


                    for (int i = 0; i < dysnoPanel.brushes.Length; i++)
                    {
                        //Log.LogDebug($"{i} {dysnoPanel.brushMode} {i == (int)dysnoPanel.brushMode} {dysnoPanel.brushMode == BrushMode.Node}");

                        if (brushes[i].Count <= 1) //Empty Brush
                            continue;

                        if (i == (int)dysnoPanel.brushMode)
                        {
                            for (int t = 1; t < rdialCount; t++)
                            {
                                dataPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                                clickPoint = currentRotation * dataPoint;
                                brushes[i][t].SetDysonSphere(sphere); //May change in future game patch?
                                brushes[i][t].layer = layer;                                
                                brushes[i][t]._Open();
                                brushes[i][t].gameObject.SetActive(true);
                                brushes[i][t]._OnUpdate();
                                
                            }
                            if (mirrorMode)
                            {
                                pos.y = -pos.y;
                                for (int t = rdialCount; t < 2 * rdialCount; t++)
                                {
                                    dataPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                                    clickPoint = currentRotation * dataPoint;
                                    brushes[i][t].SetDysonSphere(sphere); //May change in future game patch?
                                    brushes[i][t].layer = layer;
                                    brushes[i][t].active = true;
                                    brushes[i][t]._Open();
                                    brushes[i][t].gameObject.SetActive(true);
                                    brushes[i][t]._OnUpdate();
                                }
                            }
                        }
                        else
                        {
                            foreach (var brush in brushes[i]) {
                                brush?._OnClose();
                                brush?.gameObject.SetActive(false);
                            }
                        }
                    }
                    overwrite = false;
                    dysnoPanel.brushError =  errorText;
                    clickPoint = Vector3.zero;

                    //Log.LogDebug("S End");
                }                
            }
            catch (Exception e)
            {
                Log.LogError(e);
                ChangeParameters(false, 1); //Exist sysmetry mode
            }
            //tick = (tick + 1) % 1;
            tick = (tick + 1) % 600;
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
        [HarmonyPatch(typeof(UIDysonBrush_Select), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Remove), "_OnUpdate")]
        public static IEnumerable<CodeInstruction> Transpiler_OnUpdate(IEnumerable<CodeInstruction> instructions)
        {
            
            var code = instructions.ToList();
            var backup = new List<CodeInstruction>(code);

            var methodRaySnap = typeof(UIDysonDrawingGrid).GetMethod("RaySnap");
            var methodRayCast = typeof(UIDysonDrawingGrid).GetMethod("RayCast");
            var methodRayCastSphere = typeof(Phys).GetMethod("RayCastSphere");

            try
            {
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].operand is MethodInfo mi)
                    {
                        
                        if (code[i].opcode == OpCodes.Call && mi == methodRayCastSphere)
                        {
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RayCastSphere");
                        }                        
                        if (code[i].opcode == OpCodes.Callvirt && mi == methodRaySnap)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RaySnap");
                        }
                        else if (code[i].opcode == OpCodes.Callvirt && mi == methodRayCast)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RayCast");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e);
                code = backup;
                Log.LogWarning("Restore backup IL");

            }
            return code.AsEnumerable();
        }

        public static bool Overwrite_RayCastSphere(Vector3 origin, Vector3 dir, float length, Vector3 center, float radius, out RCHCPU rch)
        {
            if (overwrite)
            {
                rch.point = Vector3.zero;
                rch.normal = Vector3.zero;
                if (origin == rayOrigin && (center - clickPoint).sqrMagnitude < 1E-8)
                {
                    rch.dist = castDist; //only use dist 
                    return true;
                }
                rch.dist = 0f;
                return false;
            }

            bool result = Phys.RayCastSphere(origin, dir, length, center, radius, out rch);
            if (result == true)
            {
                rayOrigin = origin;
                clickPoint = center;
                castDist = rch.dist;
            }
            return result;
        }



        public static bool Overwrite_RayCast(UIDysonDrawingGrid uidysonDrawingGrid, Ray lookRay, Quaternion rotation, float radius, out Vector3 cast, bool front = true)
        {
            if (overwrite)
            {
                if (radius == castRadius)
                {
                    cast = dataPoint;
                    return true;
                }
                cast = Vector3.zero;
                return false;
            }
            bool resultCast = uidysonDrawingGrid.RayCast(lookRay, rotation, radius, out cast, front);
            if (resultCast == true)
            {
                castRadius = radius;
                dataPoint = cast;
            }
            return resultCast;
        }

        public static bool Overwrite_RaySnap(UIDysonDrawingGrid uidysonDrawingGrid, Ray lookRay, out Vector3 snap)
        {
            if (overwrite)
            {
                snap = dataPoint;
                return resultSnap;
            }
            resultSnap = uidysonDrawingGrid.RaySnap(lookRay, out snap);
            dataPoint = snap;
            return resultSnap;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Shell), "AddNodeGizmo")]
        public static bool Overwrite_AddNodeGizmo()
        {
            return !overwrite; //if overwrite is on, skip AddNodeGizmo()
        }

        #endregion

    }














}
