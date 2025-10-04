namespace BulletTime
{
    class SkillSystem_Patch
    {
        public static bool Enable { get; set; } = false;

        public static void GameTick()
        {
            if (!Enable || !GameStateManager.Interactable) return;

            SkillSystem skillSystem = GameMain.data.spaceSector?.skillSystem;
            if (skillSystem == null) return;
            AdvanceSkillSystem(skillSystem);
        }

        static void AdvanceSkillSystem(SkillSystem skillSystem)
        {
            DeepProfiler.BeginSample(DPEntry.Sector, -1, 0L);

            DeepProfiler.BeginSample(DPEntry.SkillSystem, -1, 0L);
            skillSystem.PrepareTick();
            DeepProfiler.EndSample(-1, -2L);

            DeepProfiler.BeginSample(DPEntry.SkillSystem, -1, 1L);
            skillSystem.GameTick(GameMain.gameTick);
            DeepProfiler.EndSample(-1, -2L);

            DeepProfiler.BeginSample(DPEntry.SkillSystem, -1, 2L);
            skillSystem.AfterTick();
            DeepProfiler.EndSample(-1, -2L);

            DeepProfiler.EndSample(-1, -2L);
        }
    }
}
