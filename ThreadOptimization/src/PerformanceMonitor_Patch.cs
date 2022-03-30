using HarmonyLib;

namespace ThreadOptimization
{
    class PerformanceStat_Patch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        internal static void UIPerformancePanel_Prefix(UIPerformancePanel __instance)
        {
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSphere] = 2;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonSphere] = ECpuWorkEntry.Factory;
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSwarm] = -1;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonBullet] = ECpuWorkEntry.DysonSphere; //this should be a part of swarm
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonRocket] = -1;

            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSphere].level = 2;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSphere].parent = (int)ECpuWorkEntry.Factory;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonSwarm].level = -1;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonBullet].parent = (int)ECpuWorkEntry.DysonSphere;
            __instance.cpuGraph.fanDatas[(int)ECpuWorkEntry.DysonRocket].level = -1;
        }
    }
}
