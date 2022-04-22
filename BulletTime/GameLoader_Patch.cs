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
    }
}
