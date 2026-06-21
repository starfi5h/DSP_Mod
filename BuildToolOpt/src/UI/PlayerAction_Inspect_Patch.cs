using HarmonyLib;

namespace BuildToolOpt
{
    class PlayerAction_Inspect_Patch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
        private static void GameTick_Postfix(PlayerAction_Inspect __instance)
        {
            if (VFInput.miningMachineKey && VFInput.readyToBuild)
            {
                if (__instance.player.inhandItemId == 2301) // 小礦機
                {
                    if (GameMain.history.ItemUnlocked(2316)) // 大礦機
                    {
                        __instance.player.SetHandItems(2316, 0, 0);
                    }
                }
            }
        }
    }
}
