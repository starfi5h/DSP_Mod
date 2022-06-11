/*
using BepInEx;
using HarmonyLib;

namespace NoWarpBobbing
{
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    public class NoWarpBobbing : BaseUnityPlugin
    {
        public const string MODNAME = "NoWarpBobbing";
        public const string MODGUID = "starfi5h.plugin.NoWarpBobbing";
        public const string VERSION = "1.0.0";
        Harmony harmony;

        public void Start()
        {
            harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(NoWarpBobbing));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.DetermineSailAnims))]
        public static void DetermineSailAnims(PlayerAnimator __instance)
        {
            __instance.sailAnimIndex = 0;
        }
    }
}
*/