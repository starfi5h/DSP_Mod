using HarmonyLib;

namespace ThreadOptimization
{
    class PerformanceStat_Patch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        internal static void UIPerformancePanel_Prefix()
        {
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSphere] = 2;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonSphere] = ECpuWorkEntry.Factory;
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonSwarm] = -1;
            PerformanceMonitor.cpuWorkParents[(int)ECpuWorkEntry.DysonBullet] = ECpuWorkEntry.DysonSphere; //this should be a part of swarm
            PerformanceMonitor.cpuWorkLevels[(int)ECpuWorkEntry.DysonRocket] = -1;
        }
    }
}
