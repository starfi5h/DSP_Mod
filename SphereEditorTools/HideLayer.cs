using HarmonyLib;
using UnityEngine;

namespace SphereEditorTools
{
    class HideLayer : MonoBehaviour
    {
        static int viewLayerId;
        static bool hideOtherLayers;
        static DysonSphereLayer[] focusLayer;
        static DysonSphereLayer[] tmpLayers;
        static DysonSphere sphere;
        static int displayMode;
        public static bool EnableMask;
        static GameObject blackmask;
        public static bool EnableOutside;

        public static void Free(string str)
        {
            //Log.LogDebug($"{str} : Reset visibility status.");
            hideOtherLayers = false;
            viewLayerId = 0;
            focusLayer = null;
            tmpLayers = null;
            sphere = null;
            displayMode = 0;
            //EnableMask = false;
            if (blackmask != null)
            {
                Destroy(blackmask);
                blackmask = null;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "SetViewStar")]
        public static void UIDysonPanel_SetViewStar()
        {
            Free("SetViewStar");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void OnClose(UIDysonPanel __instance)
        {
            if (!EnableOutside)
            {
                Free("Close");
                __instance.viewDysonSphere.modelRenderer.RebuildModels();
            }
        }

        public static void SetDisplayMode(int mode)
        {
            displayMode = mode; //0:normal 1,3: hide swarm 2,3: hide star
            GameObject.Find("UI Root/Dyson Map/Star")?.SetActive(displayMode < 2);            
        }

        public static void SetMask(bool enable)
        {
            if (blackmask == null)
            {
                var go = GameObject.Find("UI Root/Dyson Map/Star/black-mask");
                blackmask = Instantiate(go, go.transform.parent.parent);
                Destroy(blackmask.GetComponent<UnityEngine.SphereCollider>());
                var go2 = GameObject.Find("UI Root/Dyson Map/preview/compass");
                blackmask.transform.localScale = new Vector3(go2.transform.localScale.x, go2.transform.localScale.y, 0f); ;
            }
            blackmask.SetActive(enable);
            EnableMask = enable;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnLateUpdate")]
        public static void UpdateMask(UIDysonPanel __instance)
        {
            if (EnableMask && blackmask != null)
                blackmask.transform.rotation = __instance.screenCamera.transform.rotation;
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
                __instance.viewDysonSphere.modelRenderer.RebuildModels();
            }

            if (!__instance.showAllLayers)
            {

                if (viewLayerId != id)
                {
                    focusLayer[0] = __instance.viewDysonSphere.layersIdBased[id]; //to switch with layersSorted
                    tmpLayers[id] = __instance.viewDysonSphere.layersIdBased[id]; //to switch with layersIdBased
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDysonBrush_Remove), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Select), "_OnUpdate")]
        public static void UIDysonBrush_OnUpdate_Prefix(UIDysonBrush_Remove __instance)
        {
            if (hideOtherLayers) //__instance.dysonPanel.layerSelected > 0 && focusLayer != null
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted); //switch layersSorted
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonBrush_Remove), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonBrush_Select), "_OnUpdate")]
        public static void UIDysonBrush_OnUpdate_Postfix(UIDysonBrush_Remove __instance)
        {
            if (hideOtherLayers)
                (__instance.dysonSphere.layersSorted, focusLayer) = (focusLayer, __instance.dysonSphere.layersSorted); //switch back
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSphereSegmentRenderer), "RebuildModels")]
        public static void RebuildModels_Prefix(DysonSphereSegmentRenderer __instance)
        {
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

    }
}
