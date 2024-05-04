using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ReorderTechQueue
{
    [BepInPlugin("com.starfi5h.plugin.ReorderTechQueue", "ReorderTechQueue", "1.2.0")]
    public class ReorderTechQueuePlugin : BaseUnityPlugin
    {
        Harmony harmony;
        public static ConfigEntry<int> TechQueueLength;

        public void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.ReorderTechQueue");
            harmony.PatchAll(typeof(ReorderTechQueue));
            TechQueueLength = Config.Bind<int>("General", "TechQueueLength", 8, "Length of reserach queue.\n研究佇列的长度");
            harmony.PatchAll(typeof(UITechNodePatch));
#if DEBUG
            ReorderTechQueue.Init();
#endif
        }

#if DEBUG
        public void OnDestroy()
        {
            ReorderTechQueue.Free();
            UITechNodePatch.Free();
            harmony.UnpatchSelf();
        }
#endif
    }

    class ReorderTechQueue
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "Import")]
        [HarmonyPatch(typeof(GameHistoryData), "SetForNewGame")]
        public static void ChangeTechQueueLength(GameHistoryData __instance)
        {
            int[] newArray = new int[ReorderTechQueuePlugin.TechQueueLength.Value];
            int minLength = Math.Min(__instance.techQueue.Length, newArray.Length);
            Array.Copy(__instance.techQueue, newArray, minLength);
            __instance.techQueue = newArray;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIResearchQueue), "_OnInit")]
        public static void OnInit(UIResearchQueue __instance)
        {
            AddUIReorderNode(__instance.nodes);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UITechNode), nameof(UITechNode.DeterminePrerequisiteSuffice))]
        public static void SkipMetadataRequirement(ref bool __result)
        {
            __result = true;
        }

#if DEBUG
        internal static void Init()
        {
            AddUIReorderNode(UIRoot.instance.uiGame.researchQueue.nodes);
            AddUIReorderNode(UIRoot.instance.uiGame.techTree.resQueueUI.nodes);
        }

        internal static void Free()
        {
            RemoveUIReorderNode(UIRoot.instance.uiGame.researchQueue.nodes);
            RemoveUIReorderNode(UIRoot.instance.uiGame.techTree.resQueueUI.nodes);
        }
#endif

        public static void AddUIReorderNode(UIResearchQueueNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                GameObject gameObject = nodes[i].gameObject;
                UIReorderNode component = gameObject.AddComponent<UIReorderNode>();
                component.Index = i;
            }
        }

        public static void RemoveUIReorderNode(UIResearchQueueNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                GameObject gameObject = nodes[i].gameObject;
                GameObject.Destroy(gameObject.GetComponent<UIReorderNode>());
            }
        }
    }

    class UITechNodePatch
    {
        static UITechNode currentSelectNode;
        static UIButton locateBtn;

#if DEBUG
        internal static void Free()
        {
            GameObject.Destroy(locateBtn?.gameObject);
        }
#endif

        [HarmonyPostfix, HarmonyPatch(typeof(UITechNode), nameof(UITechNode.DeterminePrerequisiteSuffice))]
        public static void SkipMetadataRequirement(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UITechTree), nameof(UITechTree._OnLateUpdate))]
        public static void UpdateNaviBtn(UITechTree __instance)
        {
            try
            {
                if (currentSelectNode == __instance.selected) return;
                currentSelectNode = __instance.selected;

                if (__instance.selected == null)
                {
                    locateBtn?.gameObject.SetActive(false);
                    return;
                }
                if (locateBtn == null) AddBtn(currentSelectNode.gameObject.transform);
                var preTechId = GameMain.history.ImplicitPreTechRequired(currentSelectNode.techProto?.ID ?? 0);
                if (preTechId != 0)
                {
                    locateBtn.transform.SetParent(currentSelectNode.transform);
                    locateBtn.transform.localPosition = new Vector3(286f, -218f, 0f);
                    locateBtn.gameObject.SetActive(true);
                }
                else
                {
                    locateBtn.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                //Debug.Log(e);
            }
        }

        private static void AddBtn(Transform parent)
        {
            var go = GameObject.Instantiate(UIRoot.instance.uiGame.researchQueue.pauseButton.gameObject, parent);
            go.name = "ReorderTechQueue_Navi";
            go.transform.localScale = new Vector3(0.33f, 0.33f, 0);
            go.transform.localPosition = new Vector3(286f, -218f, 0f);
            Image img = go.transform.Find("icon")?.GetComponent<Image>();
            if (img != null)
            {
                UIStarmap starmap = UIRoot.instance.uiGame.starmap;
                img.sprite = starmap.cursorFunctionButton3.transform.Find("icon")?.GetComponent<Image>()?.sprite;
            }
            locateBtn = go.GetComponent<UIButton>();
            locateBtn.tips.tipTitle = "Locate";
            locateBtn.tips.tipText = "Navigate to the required tech";
            locateBtn.onClick += OnLocateButtonClick;
        }

        static void OnLocateButtonClick(int obj)
        {
            if (currentSelectNode == null) return;

            var preTechId = GameMain.history.ImplicitPreTechRequired(currentSelectNode.techProto?.ID ?? 0);
            if (preTechId != 0) UIRoot.instance.uiGame.techTree.SelectTech(preTechId);
        }
    }

    public class UIReorderNode : ManualBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
    {
        public int Index { get; set; }
        static int currIndex = -1;

        public void OnPointerDown(PointerEventData pointerEventData)
        {
            currIndex = Index;
        }

        public void OnPointerUp(PointerEventData pointerEventData)
        {
            currIndex = -1;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currIndex != -1)
            {
                currIndex = ReorderQueue(currIndex, Index);
            }
        }

        private static int ReorderQueue(int oldIndex, int newIndex)
        {
            int minIndex = oldIndex < newIndex ? oldIndex : newIndex;
            int[] newQueue = new int[GameMain.data.history.techQueueLength - minIndex];
            Array.Copy(GameMain.data.history.techQueue, minIndex, newQueue, 0, newQueue.Length);

            int techId = newQueue[oldIndex - minIndex];
            newQueue[oldIndex - minIndex] = newQueue[newIndex - minIndex];
            newQueue[newIndex - minIndex] = techId;

            for (int i = GameMain.data.history.techQueueLength - 1; i >= minIndex; i--)
            {
                GameMain.data.history.RemoveTechInQueue(i);
            }
            for (int i = 0; i < newQueue.Length; i++)
            {
                GameMain.data.history.EnqueueTech(newQueue[i]);
            }

            // if techId is removed due to dependenies conflict, reset currIndex
            return GameMain.data.history.techQueue[newIndex] == techId ? newIndex : -1;
        }
    }
}
