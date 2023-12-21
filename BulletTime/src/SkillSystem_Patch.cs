namespace BulletTime
{
    class SkillSystem_Patch
    {
        public static bool Enable = false;

        public static void GameTick(SkillSystem skillSystem)
        {
            if (!Enable) return;

            PerformanceMonitor.BeginSample(ECpuWorkEntry.Combat);
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Skill);
            AdvanceSkillSystem(skillSystem);
            PerformanceMonitor.EndSample(ECpuWorkEntry.Skill);
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Combat);
        }

        static void AdvanceSkillSystem(SkillSystem skillSystem)
        {
            skillSystem.PrepareTick();
            skillSystem.CollectPlayerStates();
            skillSystem.GameTick(GameMain.gameTick);
            skillSystem.AfterTick();
        }
    }
}
