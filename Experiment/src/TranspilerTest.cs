using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Experiment
{
    class TranspilerTest
    {
        static void Print(IEnumerable<CodeInstruction> instructions, int start, int end)
        {
            int count = -1;
            foreach (var i in instructions)
            {
                if (count++ < start)
                    continue;
                if (count >= end)
                    break;

                if (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt)
                    Log.Warn($"{count,2} {i}");
                else if (i.IsLdarg())
                    Log.Info($"{count,2} {i}");
                else
                    Log.Debug($"{count,2} {i}");
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        static IEnumerable<CodeInstruction> Debug_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator IL)
        {
            Print(instructions, 0, 50);
            var code = Real_Transpiler(instructions, IL);
            Print(code, 0, 50);
            return code;
        }

        //Move attributes to here when debug is finished
        static IEnumerable<CodeInstruction> Real_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator IL)
        {
            try
            {
                var matcher = new CodeMatcher(instructions);
                //Do IL manipulation here
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("Real_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }
    }
}
