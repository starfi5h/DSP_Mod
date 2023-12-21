using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BulletTime
{
    class GameLoader_Patch
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(GameLoader), nameof(GameLoader.FixedUpdate))]
        static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(StarData), "get_loaded")))
                    .Advance(-1)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .Insert(HarmonyLib.Transpilers.EmitDelegate<Func<bool>>(() =>
                    {
                        return GameMain.localPlanet?.loaded ?? true;
                    }));
                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("Transpiler GameLoader.FixedUpdate failed. Fast loading won't work");
                return instructions;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameLoader), nameof(GameLoader.FixedUpdate))]
        static void FixedUpdate_Postfix(GameLoader __instance)
        {
            if (__instance.frame >= 5 && DSPGame.Game.isMenuDemo && !DSPGame.IsCombatCutscene)
            {
                Log.Debug("MenuDemo - Fast forward");
                GameMain.data.SetReady();
                //GameCamera.instance.SetReady();
                GameMain.Begin();
                __instance.SelfDestroy();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), nameof(VFPreload.IsMusicReached))]
        static void IsMusicReached_Postfix(ref bool __result)
        {
            // Skip syncing of BGM
            __result |= true;
        }
    }
}
