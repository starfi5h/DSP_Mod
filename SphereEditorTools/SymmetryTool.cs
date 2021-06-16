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

        static bool mirrorMode;
        static int rdialCount; //start from 1

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void Init(UIDysonPanel __instance)
        {
            try
            {
                dysnoPanel = __instance;
                brushes = new List<UIDysonBrush>[dysnoPanel.brushes.Length];
                for (int i = 0; i < brushes.Length; i++)
                {
                    brushes[i] = new List<UIDysonBrush>();
                    brushes[i].Add(null); //placeholder for original brush
                }
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

            Log.LogDebug($"{brushes[(int)BrushMode.Node].Count} {mirrorMode} {rdialCount}");
            
        }


        static void AddBrushes()
        {
            //GameObject brushesGroup = ((UIDysonBrush_Node)dysnoPanel.brushes[(int)BrushMode.Node]).preview.transform.parent.parent.gameObject;
            //Log.LogDebug("Add Brushes");

            int id;         
            
            id = (int)BrushMode.Node;
            UIDysonBrush_Node brushNode = (UIDysonBrush_Node)Instantiate(dysnoPanel.brushes[id], dysnoPanel.brushes[id].transform.parent);            
            brushes[id].Add(brushNode);
            //Log.LogDebug($"brush: {brushNode}");
            foreach (Transform child in brushNode.gameObject.transform)
                Destroy(child.gameObject);
            brushNode._OnInit();
            //Log.LogDebug($"preview: {brushNode.preview} {brushNode.previewMesh} {brushNode.previewRenderer}");

            //brushNode.RecalcCollides(new Vector3(0,0,0));

        }



        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateBrushes")]
        static void wee_OnUpdate()
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
                    Vector3 pos = castPoint;

                    for (int i = 0; i < dysnoPanel.brushes.Length; i++)
                    {
                        //Log.LogDebug($"{i} {dysnoPanel.brushMode} {i == (int)dysnoPanel.brushMode} {dysnoPanel.brushMode == BrushMode.Node}");
                        if (i == (int)dysnoPanel.brushMode && dysnoPanel.brushMode == BrushMode.Node)
                        {
                            for (int t = 1; t < rdialCount; t++)
                            {
                                //Log.LogDebug($"{t} {castPoint}");
                                castPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                                brushes[i][t].SetDysonSphere(sphere); //May change in future game patch?
                                brushes[i][t].layer = layer;                                
                                brushes[i][t]._Open();
                                brushes[i][t].gameObject.SetActive(true);
                                brushes[i][t]._OnUpdate();
                                //Log.LogDebug(brushes[i][t]);
                                
                            }
                            if (mirrorMode)
                            {
                                pos.y = -pos.y;
                                for (int t = rdialCount; t < 2 * rdialCount; t++)
                                {
                                    //Log.LogDebug($"{t} {castPoint}");
                                    castPoint = Quaternion.Euler(0f, 360f * t / rdialCount, 0f) * pos;
                                    brushes[i][t].SetDysonSphere(sphere); //May change in future game patch?
                                    brushes[i][t].layer = layer;
                                    brushes[i][t].active = true;
                                    brushes[i][t]._Open();
                                    brushes[i][t].gameObject.SetActive(true);
                                    brushes[i][t]._OnUpdate();
                                    //Log.LogDebug(brushes[i][t]);
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
            //var methodMouseDown = typeof(Input).GetMethod("GetMouseButtonDown");

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
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RaySnap");
                            //Log.LogDebug($"[{i}]  Replce RaySnap {code[i].operand}");
                        }
                        else if (code[i].opcode == OpCodes.Callvirt && mi == methodRayCast)
                        {
                            code[i].opcode = OpCodes.Call;
                            code[i].operand = typeof(SymmetryTool).GetMethod("Overwrite_RayCast");
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
            return resultSnap;
        }

        #endregion

    }














}
