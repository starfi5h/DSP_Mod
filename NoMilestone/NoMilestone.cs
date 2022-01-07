using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace NoMilestone
{
    [BepInPlugin("com.starfi5h.plugin.NoMilestone", "NoMilestone", "1.0.2")]
    public class NoMilestone : BaseUnityPlugin
    {
        Harmony harmony;
        new public static ConfigFile Config;
        public static ConfigEntry<bool> AchievementPopup;
        public static ConfigEntry<bool> MilestonePopup;
        public static ConfigEntry<bool> AchievementActive;
        public static ConfigEntry<bool> MilestoneActive;        

        public void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.NoMilestone");
            harmony.PatchAll(typeof(Patches));
            Config = base.Config;

            AchievementPopup = Config.Bind<bool>("Pop-up", "Achievement", false, "Enable pop-up of achievement");
            MilestonePopup = Config.Bind<bool>("Pop-up", "Milestone", false, "Enable pop-up of milestone");
            AchievementActive = Config.Bind<bool>("Active", "Achievement", true, "Enable achievement system");
            MilestoneActive = Config.Bind<bool>("Active", "Milestone", true, "Enable milestone system");
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "Resume")]
        internal static void GameMain_Resume()
        {
            NoMilestone.Config.Reload();
        }


        [HarmonyPrefix, HarmonyPatch(typeof(AchievementSystem), "NotifyAchievementChange")]
        internal static bool AchievementSystem_NotifyAchievementChange()
        {
            return NoMilestone.AchievementPopup.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MilestoneSystem), "NotifyUnlockMilestone")]
        internal static bool MilestoneSystem_NotifyUnlockMilestone()
        {
            return NoMilestone.MilestonePopup.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AchievementLogic), "active", MethodType.Getter)]
        internal static void AchievementLogic_active(ref bool __result)
        {
            __result &= NoMilestone.AchievementActive.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MilestoneLogic), "GameTick")]
        internal static bool MilestoneLogic_active()
        {
            return NoMilestone.MilestoneActive.Value;
        }
    }    
}
