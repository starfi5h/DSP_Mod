using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReorderTechQueue
{
    [BepInPlugin("com.starfi5h.plugin.ReorderTechQueue", "ReorderTechQueue", "1.0.0")]
    public class ReorderTechQueuePlugin : BaseUnityPlugin
    {
        Harmony harmony;

        public void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.ReorderTechQueue");
            harmony.PatchAll(typeof(ReorderTechQueue));
        }

        public void OnDestroy()
        {
            ReorderTechQueue.OnDestroy();
            harmony.UnpatchSelf();
        }
    }

    class ReorderTechQueue
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIResearchQueue), "_OnInit")]
        public static void OnInit(UIResearchQueue __instance)
        {
            AddUIReorderNode(__instance.nodes);
        }

        public static void OnDestroy()
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

        public class UIReorderNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
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
                    ReorderQueue(currIndex, Index);
                    currIndex = Index;
                }
            }

            private static void ReorderQueue(int oldIndex, int newIndex)
            {
                int[] techQueue = GameMain.data.history.techQueue;
                int techId = techQueue[oldIndex];
                GameMain.data.history.RemoveTechInQueue(oldIndex);

                int[] newQueue = new int[techQueue.Length - 1];
                for (int i = newQueue.Length - 1; i >= newIndex; i--)
                {
                    newQueue[i] = techQueue[i];
                    GameMain.data.history.RemoveTechInQueue(i);
                }
                GameMain.data.history.EnqueueTech(techId);
                for (int i = newIndex; i < newQueue.Length; i++)
                {
                    GameMain.data.history.EnqueueTech(newQueue[i]);
                }
            }
        }
    }
}