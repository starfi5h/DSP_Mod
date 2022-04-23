using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReorderTechQueue
{
    [BepInPlugin("com.starfi5h.plugin.ReorderTechQueue", "ReorderTechQueue", "1.1.1")]
    public class ReorderTechQueuePlugin : BaseUnityPlugin
    {
        Harmony harmony;
        public static ConfigEntry<int> TechQueueLength;

        public void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.ReorderTechQueue");
            harmony.PatchAll(typeof(ReorderTechQueue));
            TechQueueLength = Config.Bind<int>("General", "TechQueueLength", 8, "Length of reserach queue.\n研究佇列的长度");
#if DEBUG
            ReorderTechQueue.Init();
#endif
        }

#if DEBUG
        public void OnDestroy()
        {
            ReorderTechQueue.Free();
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

        private static void AddUIReorderNode(UIResearchQueueNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                GameObject gameObject = nodes[i].gameObject;
                UIReorderNode component = gameObject.AddComponent<UIReorderNode>();
                component.Index = i;
            }
        }

        private static void RemoveUIReorderNode(UIResearchQueueNode[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                GameObject gameObject = nodes[i].gameObject;
                GameObject.Destroy(gameObject.GetComponent<UIReorderNode>());
            }
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
