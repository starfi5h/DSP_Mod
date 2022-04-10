using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ThreadOptimization
{
    class PerformanceStat_Patch
    {
        static Text text;
        static int counter;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnInit))]
        [HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        internal static void UIPerformancePanel_OnOpen(UIPerformancePanel __instance)
        {
            if (text == null)
            {
                Log.Debug("UIPerformancePanel Init");
                InstantiateText();
                UIPerformancePanel_Alter(__instance);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel.OnCpuActiveButtonClick))]
        internal static void UIPerformancePanel_OnCpuActiveButtonClick()
        {
            if (++counter >= 2)
            {
                EnhanceMultithread.Enable = !EnhanceMultithread.Enable;
                SetIndictor(EnhanceMultithread.Enable);
                counter = 0;
            }
        }

        public static void OnDestory()
        {
            Object.Destroy(text?.gameObject);
        }

        static void InstantiateText()
        {
            GameObject obj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Statistics Window/performance-bg/cpu-panel/Scroll View/Viewport/Content/label");
            obj = GameObject.Instantiate(obj, obj.transform.parent.parent.parent.parent);
            obj.name = "ThreadOptimization Indicator";
            obj.transform.localPosition = new Vector3(240, 60, 0);
            text = obj.GetComponent<Text>();
            SetIndictor(EnhanceMultithread.Enable);
        }

        static void SetIndictor(bool state)
        {
            text.text = EnhanceMultithread.Enable ? "Optimization: On" : "Optimization: Off";
        }

        static void UIPerformancePanel_Alter(UIPerformancePanel __instance)
        {
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSphere] = 2;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonSphere] = ECpuWorkEntry.Factory;
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSwarm] = -1;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonBullet] = ECpuWorkEntry.DysonSphere; //this should be a part of swarm
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonRocket] = -1;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.Statistics] = ECpuWorkEntry.Factory;
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.Statistics] = 2;

            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSphere].level = -1;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSphere].parent = (int)ECpuWorkEntry.Null; //don't show dyson sphere on graph
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSwarm].level = -1;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonBullet].parent = (int)ECpuWorkEntry.DysonSphere;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonRocket].level = -1;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.Statistics].parent = (int)ECpuWorkEntry.Factory;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.Statistics].level = 2;
        }
    }
}
