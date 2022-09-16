using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
            if (__instance.frame >= 5 && DSPGame.Game.isMenuDemo)
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

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_PathAddon), nameof(BuildTool_PathAddon.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
        static IEnumerable<CodeInstruction> Real_Transpiler3(IEnumerable<CodeInstruction> instructions)
        {
            // Remove force GC.Collect()
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Collect"));

            if (matcher.IsInvalid)
                return instructions;

            return matcher.SetOpcodeAndAdvance(OpCodes.Nop).InstructionEnumeration();
        }
    }
}
