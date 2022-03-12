using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using UnityEngine;

using BrushMode = UIDysonEditor.EBrushMode;

namespace SphereEditorTools 
{
    class SymmetryTool : MonoBehaviour
    {
        static UIDysonEditor dysnoEditor;
        static List<UIDysonBrush>[] brushes;
        static int brushId;
        static int mirrorMode; // 0~2
        static int rdialCount; //start from 1

        static bool overwrite;
        static Vector3 dataPoint; //store in sphere data, normalized
        static float castRadius;
        static bool resultSnap;
        static Vector3 clickPoint; //currentRoation * dataPoint
        static float castDist;
        static Vector3 rayOrigin;

        static int tick;

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void Init(UIDysonEditor __instance)
        {
            dysnoEditor = __instance;
            brushes = new List<UIDysonBrush>[dysnoEditor.brushes.Length];
            for (int i = 0; i < brushes.Length; i++)
            {
                brushes[i] = new List<UIDysonBrush>
                {
                    null //placeholder for original brush
                };
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "_OnClose")]
        public static void Free()
        {
            if (brushes != null)
            {
                for (int i = 0; i < brushes.Length; i++)
                {
                    foreach (var brush in brushes[i])
                    {
                        brush?._OnClose();
                        if (brush?.gameObject != null)
                            Destroy(brush?.gameObject);
                    }
                }
                brushes = null;
            }
            Comm.SymmetricMode = false;
        }

        public static void ChangeParameters(int newMirrorMode, int newRadialCount)
        {
            int activeBrushCount = rdialCount * (mirrorMode > 0 ? 2 : 1);
            mirrorMode = newMirrorMode;
            rdialCount = newRadialCount;
            int newCount = rdialCount * (mirrorMode > 0 ? 2 : 1);
            while (brushes[(int)BrushMode.Node].Count < newCount)
            {
                AddBrushes();
            }
            for (int i = 0; i < brushes.Length; i++)
            {
                if (brushes[i].Count < activeBrushCount)
                    continue;
                for (int t = newCount; t < activeBrushCount; t++) //Set extra as unactive
                {
                    brushes[i][t]?._OnClose();
                    brushes[i][t]?.gameObject.SetActive(false);
                }
            }
            //Log.LogDebug($"ChangeParameters C:{brushes[(int)BrushMode.Node].Count} M:{mirrorMode} R:{rdialCount}");
            
        }

        static void AddBrushes()
        {
            int id;
            
            id = (int)BrushMode.Node;
            UIDysonBrush_Node brushNode = (UIDysonBrush_Node)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);            
            brushes[id].Add(brushNode);
            foreach (Transform child in brushNode.gameObject.transform)
                Destroy(child.gameObject);
            brushNode._OnInit();

            id = (int)BrushMode.FrameGeo;
            UIDysonBrush_Frame brushFrame = (UIDysonBrush_Frame)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushFrame);
            foreach (Transform child in brushFrame.gameObject.transform)
                Destroy(child.gameObject);
            brushFrame._OnInit();
            brushFrame.isEuler = false;

            id = (int)BrushMode.FrameEuler;
            brushFrame = (UIDysonBrush_Frame)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushFrame);
            foreach (Transform child in brushFrame.gameObject.transform)
                Destroy(child.gameObject);
            brushFrame._OnInit();
            brushFrame.isEuler = true;

            id = (int)BrushMode.Shell;
            UIDysonBrush_Shell brushShell = (UIDysonBrush_Shell)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushShell);
            foreach (Transform child in brushShell.gameObject.transform)
                Destroy(child.gameObject);
            brushShell._OnInit();

            id = (int)BrushMode.Remove;
            UIDysonBrush_Remove brushRemove = (UIDysonBrush_Remove)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushRemove);
            foreach (Transform child in brushRemove.gameObject.transform)
                Destroy(child.gameObject);
            brushRemove._OnInit();


            id = (int)BrushMode.Select;
            UIDysonBrush_Select brushSelect = (UIDysonBrush_Select)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushSelect);
            foreach (Transform child in brushSelect.gameObject.transform)
                Destroy(child.gameObject);
            brushSelect._OnInit();

        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "UpdateBrushes")]
        public static void Brushes_OnUpdate()
        {
            try
            {
                int activeBrushCount = rdialCount * (mirrorMode > 0 ? 2 : 1);
                if (activeBrushCount > 1) //symmetric mode on
                {
                    if (brushId != (int)dysnoEditor.brushMode)
                    {
                        foreach (var brush in brushes[brushId])
                        {
                            brush?._OnClose(); //Clean previous brushes
                            brush?.gameObject.SetActive(false);
                        }
                        brushId = (int)dysnoEditor.brushMode;
                    }

                    //on left-click, update clone brushes texture (protoId) to match with original one
                    if (Input.GetMouseButtonDown(0))
                    {
                        foreach (var brush in brushes[brushId])
                        {
                            if (brush != null)
                            {
                                switch (brushId)
                                {
                                    case (int)BrushMode.FrameGeo:
                                    case (int)BrushMode.FrameEuler:
                                        UIDysonBrush_Frame uiysonBrush_Frame = brush as UIDysonBrush_Frame;
                                        uiysonBrush_Frame.frameProtoId = dysnoEditor.frameProtoId;
                                        break;
                                    case (int)BrushMode.Shell:
                                        UIDysonBrush_Shell uiysonBrush_Shell = brush as UIDysonBrush_Shell;
                                        uiysonBrush_Shell.shellProtoId = dysnoEditor.shellProtoId;
                                        break;
                                }
                            }
                        }
                    }


                    DysonSphere sphere = dysnoEditor.selection.viewDysonSphere;
                    DysonSphereLayer layer = dysnoEditor.selection.singleSelectedLayer;
                    string errorText = dysnoEditor.controlPanel.toolbox.brushError;

                    overwrite = true;
                    Vector3 pos = dataPoint;
                    Quaternion currentRotation = Quaternion.identity;
                    if (clickPoint != Vector3.zero)
                    {
                        Ray ray = dysnoEditor.screenCamera.ScreenPointToRay(Input.mousePosition);
                        castRadius = (ray.origin.magnitude * 4000 / (rayOrigin.magnitude));
                        bool front = Vector3.Dot(ray.direction, clickPoint) < 0f; //true : clickPoint is toward camera
                        for (int i = sphere.layersSorted.Length - 1; i >= 0; i--)
                        {
                            DysonSphereLayer layer2 = sphere.layersSorted[front ? i : sphere.layersSorted.Length - 1 - i];
                            if (layer2 != null && Mathf.Abs(layer2.orbitRadius - castRadius) < 1.0f)
                            {
                                castRadius = 0; //Don't select frame and shell.
                                currentRotation = layer2.currentRotation;
                                pos = Quaternion.Inverse(currentRotation) * clickPoint;
                                break;
                            }
                        }
                    }

                    if (brushes[brushId].Count >= activeBrushCount)
                    {
                        for (int t = 1; t < rdialCount; t++)
                        {
                            dataPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                            clickPoint = currentRotation * dataPoint;
                            brushes[brushId][t].SetDysonSphere(sphere);
                            brushes[brushId][t].layer = layer;                                
                            brushes[brushId][t]._Open();
                            brushes[brushId][t].gameObject.SetActive(true);
                            brushes[brushId][t]._OnUpdate();
                                
                        }
                        if (mirrorMode > 0)
                        {
                            pos.y = -pos.y;
                            if (mirrorMode == 2) //Antipodal point
                            {
                                pos.x = -pos.x;
                                pos.z = -pos.z;
                            }
                            for (int t = rdialCount; t < 2 * rdialCount; t++)
                            {
                                dataPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                                clickPoint = currentRotation * dataPoint;
                                brushes[brushId][t].SetDysonSphere(sphere);
                                brushes[brushId][t].layer = layer;
                                brushes[brushId][t].active = true;
                                brushes[brushId][t]._Open();
                                brushes[brushId][t].gameObject.SetActive(true);
                                brushes[brushId][t]._OnUpdate();
                            }
                        }
                    }
                    overwrite = false;
                    dysnoEditor.controlPanel.toolbox.brushError =  errorText;
                    clickPoint = Vector3.zero;
                    dataPoint = Vector3.zero;
                }                
            }
            catch (Exception e)
            {
                Log.LogError(e);
                Comm.SetInfoString(Stringpool.SymmetricTool + " Error. The function is now disable", 120);
                ChangeParameters(0, 1); //Exist sysmetry mode
            }
            tick = (tick + 1) % 600;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Node), "RecalcCollides")]
        public static void ForceUpdate(UIDysonBrush_Node __instance)
        {
            if (Input.GetMouseButtonDown(0))
            { //on left-click
                __instance.lastCalcPos = Vector3.zero; //force to check again
            }
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

            try
            {
                var methodRaySnap = typeof(UIDysonDrawingGrid).GetMethod("RaySnap");
                var methodRayCast = typeof(UIDysonDrawingGrid).GetMethod("RayCast");
                var methodRayCastSphere = typeof(Phys).GetMethod("RayCastSphere");

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
                Log.LogWarning("Transpiler failed. Restore backup IL");
                SphereEditorTools.ErrorMessage += "SymmetryTool ";
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

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnNodeClick")]
        public static bool Overwrite_OnNodeClick(DESelection __instance, DysonNode node)
        {
            if (!overwrite || __instance.selectedNodes == null || node == null)
                return true;

            // Multiple select as if LeftControl is hold
            if (!__instance.selectedNodes.Contains(node))
                __instance.AddNodeSelection(node);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnFrameClick")]
        public static bool Overwrite_OnFrameClick(DESelection __instance, DysonFrame frame)
        {
            if (!overwrite || __instance.selectedFrames == null || frame == null)
                return true;

            if (!__instance.selectedFrames.Contains(frame))
                __instance.AddFrameSelection(frame);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnShellClick")]
        public static bool Overwrite_OnFrameClick(DESelection __instance, DysonShell shell)
        {
            if (!overwrite || __instance.selectedShells == null || shell == null)
                return true;

            if (!__instance.selectedShells.Contains(shell))
                __instance.AddShellSelection(shell);
            return false;
        }
        #endregion
    }
}
