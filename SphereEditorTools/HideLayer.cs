using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SphereEditorTools
{
    class HideLayer : BaseUnityPlugin
    {
        static int viewLayerId;
        static bool hideOtherLayers;       
        static DysonSphereLayer[] focusLayer;
        static DysonSphereLayer[] tmpLayers;
        static DysonSphere sphere;
        static int displayMode;

        static public void SetDisplayMode(int mode)
        {
            displayMode = mode; //0:normal 1,3: hide swarm 2,3: hide star
            GameObject.Find("UI Root/Dyson Map/Star")?.SetActive(displayMode < 2);
            Comm.SetInfoString(Stringpool.DisplayMode[displayMode], 120);
        }

        public static void Free(string str)
        {
            //Log.LogDebug($"{str} : Reset visibility status.");
            hideOtherLayers = false;
            viewLayerId = 0;
            focusLayer = null;
            tmpLayers = null;
            sphere = null;
            displayMode = 0;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectionVisibleChange")]
        public static void UIDysonPanel_UpdateSelectionVisibleChange(UIDysonPanel __instance)
        {
            int id = __instance.layerSelected;
            if (sphere == null || sphere != __instance.viewDysonSphere)
            {
                //Log.LogDebug($"Reset visibility status.");
                sphere = __instance.viewDysonSphere;
                focusLayer = new DysonSphereLayer[1];
                tmpLayers = new DysonSphereLayer[__instance.viewDysonSphere.layersIdBased.Length];
                hideOtherLayers = false;
                viewLayerId = 0;
                //hideMode = 0;
                __instance.viewDysonSphere.modelRenderer.RebuildModels();
            }

            if (!__instance.showAllLayers)
            {

                if (viewLayerId != id)
                {
                    focusLayer[0] = __instance.viewDysonSphere.layersIdBased[id]; //switch with layersSorted
                    tmpLayers[id] = __instance.viewDysonSphere.layersIdBased[id]; //switch with layersIdBased
                    tmpLayers[viewLayerId] = null;
                    viewLayerId = id;
                    __instance.viewDysonSphere.modelRenderer.RebuildModels();
                }
            }
            if (__instance.showAllLayers == hideOtherLayers)
            {
                hideOtherLayers = !__instance.showAllLayers;
                __instance.viewDysonSphere.modelRenderer.RebuildModels();
            }
            //Log.LogDebug($"UpdateSelection ShowAll[{!hideOtherLayers}] ViewLayer[{viewLayerId}] {focusLayer[0]} {String.Join(",",tmpLayers.ToList())}");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Remove), "_OnUpdate")]
        public static void UIDysonBrush_Remove_OnUpdate_Prefix(UIDysonBrush_Remove __instance)
        {
            if (hideOtherLayers) //__instance.dysonPanel.layerSelected > 0 && focusLayer != null
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted); //switch reference

        }
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Remove), "_OnUpdate")]
        public static void UIDysonBrush_Remove_OnUpdate_Postfix(UIDysonBrush_Remove __instance)
        {
            if (hideOtherLayers)
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted); //switch back
        }
        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonBrush_Select), "_OnUpdate")]
        public static void UIDysonBrush_Select_OnUpdate_Prefix(UIDysonBrush_Select __instance)
        {
            if (hideOtherLayers)
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted); //switch layersSorted
        }
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonBrush_Select), "_OnUpdate")]
        public static void UIDysonBrush_Select_OnUpdate_Postfix(UIDysonBrush_Select __instance)
        {
            if (hideOtherLayers)
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted);
        }


        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), "RebuildModels")]
        public static void RebuildModels_Prefix(DysonSphereSegmentRenderer __instance)
        {
            //Log.LogDebug($"RebuildModels HideOther[{hideOtherLayers}] ViewLayer[{viewLayerId}] ");
            if (hideOtherLayers)
            {
                if (__instance.dysonSphere != sphere)
                    Free("RebuildModels");
                else
                    (__instance.dysonSphere.layersIdBased, tmpLayers) = (tmpLayers, __instance.dysonSphere.layersIdBased); //switch layersIdBased
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), "RebuildModels")]
        public static void RebuildModels_Postfix(DysonSphereSegmentRenderer __instance)
        {
            if (hideOtherLayers)
            {
                (__instance.dysonSphere.layersIdBased, tmpLayers) = (tmpLayers, __instance.dysonSphere.layersIdBased);
                for (int lid = 1; lid <= 10; ++lid)
                {
                    DysonSphereLayer layer = __instance.dysonSphere.layersIdBased[lid];
                    if (layer != null && tmpLayers[lid] == null)
                    {
                        for (int i = 0; i < layer.nodeCursor; ++i)
                            if (layer.nodePool[i] != null && layer.nodePool[i].id == i)
                                layer.nodePool[i].modelIdx = 0;
                        for (int i = 0; i < layer.frameCursor; ++i)
                            if (layer.framePool[i] != null && layer.framePool[i].id == i)
                            {
                                layer.framePool[i].modelIdx = 0;
                                layer.framePool[i].modelCount = 0;
                            }
                    }
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), "DrawModels")]
        public static void DysonSphereSegmentRenderer_DrawModels_Prefix(DysonSphereSegmentRenderer __instance)
        {
            if (hideOtherLayers)
            {
                if (__instance.dysonSphere != sphere)
                    Free("DrawModels");
                else
                    (__instance.dysonSphere.layersIdBased, tmpLayers) = (tmpLayers, __instance.dysonSphere.layersIdBased); //switch layersIdBased
            }
            //Log.LogDebug($"Mask {mask}"); //mask : unuse argument?
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), "DrawModels")]
        public static void DysonSphereSegmentRenderer_DrawModels_Postfix(DysonSphereSegmentRenderer __instance)
        {
            if (hideOtherLayers)
                (__instance.dysonSphere.layersIdBased, tmpLayers) = (tmpLayers, __instance.dysonSphere.layersIdBased);
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphere), "DrawPost")]
        public static bool DysonSwarm_DrawPost()
        {
            return displayMode%2 == 0;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "SetViewStar")]
        public static void UIDysonPanel_SetViewStar()
        {
            Free("SetViewStar");
        }        

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void OnClose(UIDysonPanel __instance)
        {
            if (SphereEditorTools.EnableHideOutside.Value == false)
            {
                Free("Close");
                __instance.viewDysonSphere.modelRenderer.RebuildModels();
            }
        }

    }
}
