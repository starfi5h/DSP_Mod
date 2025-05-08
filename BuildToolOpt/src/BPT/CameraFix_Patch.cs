using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BuildToolOpt
{
    // Reference: https://github.com/limoka/DSP-Mods/blob/master/Mods/BlueprintTweaks/src/BlueprintTweaks/Camera/CameraFixPatch.cs
    // Original Author: limoka

    public static class CameraFix_Patch
    {
        public static bool Enable; // If enable, blueprint mode switch from god mode to normal mode
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Logic))]
        [HarmonyPatch(typeof(PlayerMove_Drift), nameof(PlayerMove_Drift.GameTick))]
        [HarmonyPatch(typeof(PlayerMove_Fly), nameof(PlayerMove_Fly.GameTick))]
        [HarmonyPatch(typeof(PlayerMove_Walk), nameof(PlayerMove_Walk.GameTick))]
        static IEnumerable<CodeInstruction> ModeReplace(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerController), nameof(PlayerController.actionBuild))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlayerAction_Build), "get_blueprintMode"))
                )
                .Advance(1)
                .InsertAndAdvance(Transpilers.EmitDelegate<Func<EBlueprintMode, EBlueprintMode>>(mode => Enable ? EBlueprintMode.None : mode));

            return matcher.InstructionEnumeration();
        }
    }
}
