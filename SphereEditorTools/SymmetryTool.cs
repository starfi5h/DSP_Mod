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
        static Ray castRay = default;
        static bool paint_gridMode;
        static int paint_cast_type; // type of paint cast

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

            id = (int)BrushMode.Select;
            var brushSelect = (UIDysonBrush_Select)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushSelect);
            foreach (Transform child in brushSelect.gameObject.transform)
                Destroy(child.gameObject);
            brushSelect._OnInit();

            id = (int)BrushMode.Node;
            var brushNode = (UIDysonBrush_Node)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);            
            brushes[id].Add(brushNode);
            foreach (Transform child in brushNode.gameObject.transform)
                Destroy(child.gameObject);
            brushNode._OnInit();

            id = (int)BrushMode.FrameGeo;
            var brushFrame = (UIDysonBrush_Frame)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
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
            var brushShell = (UIDysonBrush_Shell)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushShell);
            foreach (Transform child in brushShell.gameObject.transform)
                Destroy(child.gameObject);
            brushShell._OnInit();

            id = (int)BrushMode.Paint;
            var brushPaint = (UIDysonBrush_Paint)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushPaint);
            foreach (Transform child in brushPaint.gameObject.transform)
                Destroy(child.gameObject);
            brushPaint._OnInit();

            id = (int)BrushMode.Remove;
            var brushRemove = (UIDysonBrush_Remove)Instantiate(dysnoEditor.brushes[id], dysnoEditor.brushes[id].transform.parent);
            brushes[id].Add(brushRemove);
            foreach (Transform child in brushRemove.gameObject.transform)
                Destroy(child.gameObject);
            brushRemove._OnInit();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonEditor), "UpdateBrushes")]
        public static void Brushes_Prepare()
        {
            if ((rdialCount * (mirrorMode > 0 ? 2 : 1)) > 1)
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
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonEditor), "UpdateBrushes")]
        public static void Brushes_OnUpdate()
        {
            try
            {
                if (brushId == (int)BrushMode.Paint)
                {
                    if (dysnoEditor.brush_paint.castNodeGizmo != null) paint_cast_type = 1;
                    else if (dysnoEditor.brush_paint.castFrameGizmo != null) paint_cast_type = 2;
                    else if (dysnoEditor.brush_paint.castShellGizmo != null) paint_cast_type = 3;
                    else paint_cast_type = 0;
                }

                int activeBrushCount = rdialCount * (mirrorMode > 0 ? 2 : 1);
                if (activeBrushCount > 1) //symmetric mode on
                {
                    //on left-click, update clone brushes texture (protoId) to match with original one
                    if (Input.GetMouseButton(0))
                    {
                        //Log.LogWarning($"{dysnoEditor.brushMode} {dysnoEditor.selection.singleSelectedLayer?.id ?? -1}");
                        foreach (var brush in brushes[brushId])
                        {
                            if (brush != null)
                            {
                                switch (brushId)
                                {
                                    case (int)BrushMode.Select:
                                        var uiDysonBrush_Select = brush as UIDysonBrush_Select;
                                        var original_select = dysnoEditor.brushes[brushId] as UIDysonBrush_Select;
                                        uiDysonBrush_Select.selectedLayerId = original_select.selectedLayerId;
                                        break;
                                    case (int)BrushMode.FrameGeo:
                                    case (int)BrushMode.FrameEuler:
                                        var uiDysonBrush_Frame = brush as UIDysonBrush_Frame;
                                        uiDysonBrush_Frame.frameProtoId = dysnoEditor.frameProtoId;
                                        break;
                                    case (int)BrushMode.Shell:
                                        var uiDysonBrush_Shell = brush as UIDysonBrush_Shell;
                                        uiDysonBrush_Shell.shellProtoId = dysnoEditor.shellProtoId;
                                        break;
                                    case (int)BrushMode.Paint:
                                        var uiDysonBrush_Paint = brush as UIDysonBrush_Paint;
                                        if (dysnoEditor.brush_paint.pickColorMode)
                                            return; // 使用選色器時跳過多重筆刷
                                        uiDysonBrush_Paint.paint = dysnoEditor.brush_paint.paint;
                                        uiDysonBrush_Paint.eraseMode = dysnoEditor.brush_paint.eraseMode;
                                        uiDysonBrush_Paint.size = dysnoEditor.brush_paint.size;
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
                    Ray ray = dysnoEditor.screenCamera.ScreenPointToRay(Input.mousePosition);
                    paint_gridMode = dysnoEditor.brushMode == BrushMode.Paint && dysnoEditor.brush_paint.isGridMode;
                    if (clickPoint != Vector3.zero)
                    {
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
                            Quaternion quaternion = Quaternion.Euler(0f, 360f * t / rdialCount, 0f);
                            dataPoint = quaternion * pos;
                            clickPoint = currentRotation * dataPoint;
                            castRay = new Ray(quaternion * ray.origin, quaternion * ray.direction);
                            brushes[brushId][t].SetDysonSphere(sphere);
                            brushes[brushId][t].layer = layer;                                
                            brushes[brushId][t]._Open();
                            brushes[brushId][t].gameObject.SetActive(true);
                            brushes[brushId][t]._OnUpdate();                                
                        }
                        if (mirrorMode > 0)
                        {
                            pos.y = -pos.y;
                            ray.origin = new Vector3(ray.origin.x, -ray.origin.y, ray.origin.z);
                            ray.direction = new Vector3(ray.direction.x, -ray.direction.y, ray.direction.z);
                            if (mirrorMode == 2) //Antipodal point
                            {
                                pos.x = -pos.x;
                                pos.z = -pos.z;
                                ray.origin = new Vector3(-ray.origin.x, ray.origin.y, -ray.origin.z);
                                ray.direction = new Vector3(-ray.direction.x, ray.direction.y, -ray.direction.z);
                            }
                            for (int t = rdialCount; t < 2 * rdialCount; t++)
                            {
                                Quaternion quaternion = Quaternion.Euler(0f, 360f * t / rdialCount, 0f);
                                dataPoint = quaternion * pos;
                                clickPoint = currentRotation * dataPoint;
                                castRay = new Ray(quaternion * ray.origin, quaternion * ray.direction);
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
        [HarmonyPatch(typeof(UIDysonBrush_Paint), "_OnUpdate")]
        public static IEnumerable<CodeInstruction> Transpiler_OnUpdate(IEnumerable<CodeInstruction> instructions)
        {
            
            var code = instructions.ToList();
            var backup = new List<CodeInstruction>(code);

            try
            {
                var methodRaySnap = typeof(UIDysonDrawingGrid).GetMethod("RaySnap", new Type[] { typeof(Ray), typeof(Vector3).MakeByRefType()});
                var methodRaySnap2 = typeof(UIDysonDrawingGrid).GetMethod("RaySnap", new Type[] { typeof(Ray), typeof(Vector3).MakeByRefType(), typeof(int).MakeByRefType() });
                var methodRayCast = typeof(UIDysonDrawingGrid).GetMethod("RayCast");
                var methodRayCastSphere = typeof(Phys).GetMethod("RayCastSphere", new Type[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3), typeof(float), typeof(RCHCPU)});

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
                        else if (code[i].opcode == OpCodes.Callvirt && mi == methodRaySnap2)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RaySnap2");
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
                Log.LogWarning("Brush transpiler failed. Restore backup IL");
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
                if (origin == rayOrigin && (center - clickPoint).sqrMagnitude < 1E-6)
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

        public static bool Overwrite_RaySnap2(UIDysonDrawingGrid uidysonDrawingGrid, Ray lookRay, out Vector3 snap, out int triIdx)
        {
            if (overwrite)
            {                
                if (paint_gridMode)
                {
                    bool res = uidysonDrawingGrid.RaySnap(castRay, out snap, out triIdx);
                    //snap = dataPoint;
                    return res;
                }
                else
                {
                    snap = dataPoint;
                    triIdx = 0;
                    return resultSnap;
                }
            }
            resultSnap = uidysonDrawingGrid.RaySnap(lookRay, out snap, out triIdx);
            dataPoint = snap;
            return resultSnap;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPaintingGrid), "RayCastAndHightlight")]
        public static void Overwrite_RayCastAndHightlight(ref Ray lookRay)
        {
            if (overwrite)
            {
                lookRay = castRay;
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Shell), "AddNodeGizmo")]
        public static bool Overwrite_AddNodeGizmo()
        {
            return !overwrite; //if overwrite is on, skip AddNodeGizmo()
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDysonPaintingGrid), "SetCursorCellsGraticule")]
        [HarmonyPatch(typeof(UIDysonPaintingGrid), "SetCursorCells")]
        public static bool Overwrite_Paint()
        {
            // Don't update cursor cells, except when mouse button hit
            return !overwrite || Input.GetMouseButton(0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnNodeClick")]
        public static bool Overwrite_OnNodeClick(DESelection __instance, DysonNode node)
        {
            if (!overwrite || node == null)
                return true;

            // Multiple select as if LeftControl is hold
            if (!__instance.selectedNodes.Contains(node))
            {
                __instance.AddNodeSelection(node);
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnFrameClick")]
        public static bool Overwrite_OnFrameClick(DESelection __instance, DysonFrame frame)
        {            
            if (!overwrite || frame == null)
                return true;

            if (!__instance.selectedFrames.Contains(frame))
                __instance.AddFrameSelection(frame);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DESelection), "OnShellClick")]
        public static bool Overwrite_OnFrameClick(DESelection __instance, DysonShell shell)
        {
            if (!overwrite || shell == null)
                return true;

            if (!__instance.selectedShells.Contains(shell))
                __instance.AddShellSelection(shell);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Paint), "_OnUpdate")]
        public static bool UIDysonBrush_Paint_OnUpdate(UIDysonBrush_Paint __instance)
        {
            if (!overwrite) return true;

            __instance.size = ((__instance.size > __instance.maxPainterBrushSize) ? __instance.maxPainterBrushSize : __instance.size);
            UIDysonDrawingGrid[] drawingGrids = __instance.editor.drawingGrids;
            Camera screenCamera = __instance.editor.screenCamera;
            UIDysonPaintingGrid uidysonPaintingGrid = __instance.editor.paintingGrids[__instance.layer.paintingGridMode];

            if (!RectTransformUtility.RectangleContainsScreenPoint(__instance.editor.controlPanel.inspector.selfRect, Input.mousePosition, __instance.editor.uiCamera) && !RectTransformUtility.RectangleContainsScreenPoint(__instance.editor.controlPanel.hierarchy.selfRect, Input.mousePosition, __instance.editor.uiCamera))
            {
                if (__instance.eraseMode)
                {
                    UICursor.SetCursor(ECursor.DysonEraser);
                }
                else if (__instance.pickColorMode)
                {
                    UICursor.SetCursor(ECursor.DysonEyedropper);
                }
                else
                {
                    UICursor.SetCursor(ECursor.DysonPainter);
                }
            }

            if (!UIBlockZone.blocked && !UIDysonEditor.onGUIOperate)
            {
                int num2 = 0;
                int num3 = 0;
                int num4 = 0;
                int num5 = 0;
                //DysonSphereLayer dysonSphereLayer = __instance.layer;
                if (__instance.layer != null)
                {
                    int id = __instance.layer.id;
                    Ray ray = screenCamera.ScreenPointToRay(Input.mousePosition);
                    Ray ray2 = ray;
                    ray2.origin /= (float)((double)__instance.layer.orbitRadius * 0.00025);

                    if (paint_cast_type == 1) // Orignal brush select node
                    {
                        for (int i = 1; i < __instance.layer.nodeCursor; i++)
                        {
                            if (__instance.layer.nodePool[i] != null && __instance.layer.nodePool[i].id == i)
                            {
                                if (Vector3.SqrMagnitude(__instance.layer.nodePool[i].pos.normalized - dataPoint) < 1E-3f)
                                {
                                    num3 = i;
                                    num2 = id;
                                }
                            }
                        }
                    }
                    __instance._tmp_frame_list.Clear();
                    __instance._tmp_shell_list.Clear();
                    Vector3 vector2;
                    if (Overwrite_RayCast(drawingGrids[0], ray, __instance.layer.currentRotation, __instance.layer.orbitRadius, out vector2, true))
                    {
                        double num6 = 0.16099441051483154;
                        for (int j = 1; j < __instance.layer.nodeCursor; j++)
                        {
                            DysonNode dysonNode = __instance.layer.nodePool[j];
                            if (dysonNode != null && dysonNode.id == j && (double)(__instance.layer.nodePool[j].pos.normalized - vector2).sqrMagnitude < num6)
                            {
                                List<DysonFrame> frames = dysonNode.frames;
                                for (int k = 0; k < frames.Count; k++)
                                {
                                    if (!__instance._tmp_frame_list.Contains(frames[k]))
                                    {
                                        __instance._tmp_frame_list.Add(frames[k]);
                                    }
                                }
                                List<DysonShell> shells = dysonNode.shells;
                                for (int l = 0; l < shells.Count; l++)
                                {
                                    if (!__instance._tmp_shell_list.Contains(shells[l]))
                                    {
                                        __instance._tmp_shell_list.Add(shells[l]);
                                    }
                                }
                            }
                        }
                        float num7 = 0.00028900002f;
                        DysonFrame dysonFrame = null;
                        DysonShell dysonShell = null;
                        foreach (DysonFrame dysonFrame2 in __instance._tmp_frame_list)
                        {
                            List<Vector3> segments = dysonFrame2.GetSegments();
                            for (int m = 0; m < segments.Count - 1; m++)
                            {
                                Vector3 normalized = segments[m].normalized;
                                Vector3 normalized2 = segments[m + 1].normalized;
                                float num8 = UIDysonBrush_Paint.PointToSegmentSqr(normalized, normalized2, vector2);
                                if (num8 < num7)
                                {
                                    num7 = num8;
                                    dysonFrame = dysonFrame2;
                                }
                            }
                        }
                        VectorLF3 vectorLF = vector2 * __instance.layer.orbitRadius;
                        if (dysonFrame != null)
                        {
                            num4 = dysonFrame.id;
                            num2 = id;
                            //dysonSphereLayer = __instance.layer;
                        }
                        else
                        {
                            foreach (DysonShell dysonShell2 in __instance._tmp_shell_list)
                            {
                                if (dysonShell2.IsPointInShell(vectorLF))
                                {
                                    dysonShell = dysonShell2;
                                    break;
                                }
                            }
                            if (dysonShell != null)
                            {
                                num5 = dysonShell.id;
                                num2 = id;
                                //dysonSphereLayer = __instance.layer;
                            }
                        }
                    }
                    __instance._tmp_frame_list.Clear();
                }
                if (num2 > 0 && __instance.layer != null)
                {
                    if (num3 > 0 && (__instance.targetFilter & 1) > 0)
                    {
                        DysonNode dysonNode2 = (__instance.castNodeGizmo = __instance.layer.nodePool[num3]);
                        __instance.castFrameGizmo = null;
                        __instance.castShellGizmo = null;
                        if (__instance.pickColorMode)
                        {
                            return false;
                        }
                        else if (Input.GetMouseButton(0))
                        {
                            dysonNode2.color = (__instance.eraseMode ? new Color32(0, 0, 0, 0) : __instance.paint);
                            __instance.editor.selection.viewDysonSphere.UpdateColor(dysonNode2);
                        }
                    }
                    else if (num4 > 0 && (__instance.targetFilter & 2) > 0)
                    {
                        __instance.castNodeGizmo = null;
                        DysonFrame dysonFrame3 = (__instance.castFrameGizmo = __instance.layer.framePool[num4]);
                        __instance.castShellGizmo = null;
                        if (__instance.pickColorMode)
                        {
                            return false;
                        }
                        else if (Input.GetMouseButton(0))
                        {
                            dysonFrame3.color = (__instance.eraseMode ? new Color32(0, 0, 0, 0) : __instance.paint);
                            __instance.editor.selection.viewDysonSphere.UpdateColor(dysonFrame3);
                        }
                    }
                    else if (num5 > 0 && (__instance.targetFilter & 4) > 0)
                    {
                        __instance.castNodeGizmo = null;
                        __instance.castFrameGizmo = null;
                        DysonShell dysonShell3 = (__instance.castShellGizmo = __instance.layer.shellPool[num5]);
                        if (__instance.pickColorMode)
                        {
                            return false;
                        }
                        else if (Input.GetMouseButton(0))
                        {
                            dysonShell3.color = (__instance.eraseMode ? new Color32(0, 0, 0, 0) : __instance.paint);
                        }
                    }
                    else
                    {
                        __instance.castNodeGizmo = null;
                        __instance.castFrameGizmo = null;
                        __instance.castShellGizmo = null;
                    }
                }
                else
                {
                    __instance.castNodeGizmo = null;
                    __instance.castFrameGizmo = null;
                    __instance.castShellGizmo = null;
                }
                if (__instance.isGridMode)
                {
                    if (__instance.pickColorMode)
                    {
                        return false;
                    }
                    else if (uidysonPaintingGrid.RayCastAndHightlight(screenCamera.ScreenPointToRay(Input.mousePosition), __instance.size) && Input.GetMouseButton(0))
                    {
                        uidysonPaintingGrid.PaintCells(__instance.eraseMode ? new Color32(0, 0, 0, 0) : __instance.paint);
                    }
                }
                if (__instance.pickColorMode && Input.GetMouseButtonUp(0))
                {
                    __instance.pickColorMode = false;
                }
                if (Input.GetMouseButtonDown(1))
                {
                    if (!__instance.isGridMode)
                    {
                        __instance.editor.brushMode = UIDysonEditor.EBrushMode.Select;
                        __instance._Close();
                        return false;
                    }
                    __instance.paintingbox.SwitchPaintingGrid();
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    __instance.eraseMode = !__instance.eraseMode;
                    return false;
                }
            }
            else
            {
                __instance.castNodeGizmo = null;
                __instance.castFrameGizmo = null;
                __instance.castShellGizmo = null;
                uidysonPaintingGrid.ClearCursorCells();
            }
            return false;
        }

        #endregion
    }
}
