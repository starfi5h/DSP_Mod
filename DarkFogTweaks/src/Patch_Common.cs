using HarmonyLib;
using System;

namespace DarkFogTweaks
{
    public class Patch_Common
    {
        static int[] HpMaxByModelIndex;
        static int[] HpUpgradeByModelIndex;
        static int[] HpRecoverByModelIndex;

        public static void SaveHPArray()
        {
            HpMaxByModelIndex = new int[SkillSystem.HpMaxByModelIndex.Length];
            Array.Copy(SkillSystem.HpMaxByModelIndex, HpMaxByModelIndex, HpMaxByModelIndex.Length);
            HpUpgradeByModelIndex = new int[SkillSystem.HpUpgradeByModelIndex.Length];
            Array.Copy(SkillSystem.HpUpgradeByModelIndex, HpUpgradeByModelIndex, HpUpgradeByModelIndex.Length);
            HpRecoverByModelIndex = new int[SkillSystem.HpRecoverByModelIndex.Length];
            Array.Copy(SkillSystem.HpRecoverByModelIndex, HpRecoverByModelIndex, HpRecoverByModelIndex.Length);
        }

        public static void RestoreArray()
        {
            if (HpMaxByModelIndex == null) return;
            Array.Copy(HpMaxByModelIndex, SkillSystem.HpMaxByModelIndex, HpMaxByModelIndex.Length);
            Array.Copy(HpUpgradeByModelIndex, SkillSystem.HpUpgradeByModelIndex, HpUpgradeByModelIndex.Length);
            Array.Copy(HpRecoverByModelIndex, SkillSystem.HpRecoverByModelIndex, HpRecoverByModelIndex.Length);
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.Init))]
        public static void BackupAndApply()
        {
            SaveHPArray();
            EnemyUnitScale.RestoreAll();
            EnemyUnitScale.ApplyAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.OnApplyClick))]
        public static void OnApplyClick()
        {
            Plugin.Instance.Config.Reload(); // Reload config file when clicking 'Apply' in game settings
            RestoreArray();
            SaveHPArray();
            EnemyUnitScale.RestoreAll();
            EnemyUnitScale.ApplyAll();
        }
    }
}
