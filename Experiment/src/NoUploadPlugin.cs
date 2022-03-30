using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoUpload
{
    //[BepInPlugin(MODGUID, MODNAME, VERSION)]
    public class NoUpload : BaseUnityPlugin
    {
        public const string MODNAME = "NoUpload";
        public const string MODGUID = "com.starfi5h.plugin.NoUpload";
        public const string VERSION = "0.1.0";
        Harmony harmony;
        public static new ManualLogSource Logger;

        public void Start()
        {
            harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(PatchMilkyWay));
            Logger = base.Logger;
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public class PatchMilkyWay
    {
        [HarmonyPrefix, HarmonyPatch(typeof(MilkyWayWebClient), "Update")]
        public static bool Patch_MilkyWayWebClient_Update()
        {
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(STEAMX), "UploadScoreToLeaderboard")]
        public static bool Patch_STEAMX_UploadScoreToLeaderboard()
        {
            NoUpload.Logger.LogDebug("Skip update");
            return false;
        }
    }
}
