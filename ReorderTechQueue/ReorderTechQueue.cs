using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReorderTechQueue
{
    [BepInPlugin("com.starfi5h.plugin.ReorderTechQueue", "ReorderTechQueue", "1.0.0")]
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

        public void OnDestroy()
        {
            ReorderTechQueue.Free();
            harmony.UnpatchSelf();
        }
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
        static int currIndex = -1;
        public int Index { get; set; }

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
            int[] techQueue = GameMain.data.history.techQueue;
            int techId = techQueue[oldIndex];

            int[] newQueue = new int[GameMain.data.history.techQueueLength - newIndex];
            Array.Copy(techQueue, newIndex, newQueue, 0, newQueue.Length);
            if (oldIndex > newIndex)
                newQueue[oldIndex - newIndex] = 0;
            GameMain.data.history.RemoveTechInQueue(oldIndex);
            for (int i = GameMain.data.history.techQueueLength; i >= newIndex; i--)
            {
                GameMain.data.history.RemoveTechInQueue(i);
            }
            GameMain.data.history.EnqueueTech(techId);
            for (int i = 0; i < newQueue.Length; i++)
            {
                GameMain.data.history.EnqueueTech(newQueue[i]);
            }

            // if techId is removed due to dependenies conflict, reset currIndex
            return techQueue[newIndex] == techId ? newIndex : -1;
        }
    }
}