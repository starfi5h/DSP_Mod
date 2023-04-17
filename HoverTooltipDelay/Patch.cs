using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HoverTooltipDelay
{
    class Patch
    {
        static int UIEntityBriefInfo_DelayFrame = 15;

        public static void SetDelay(int delay)
        {
            UIEntityBriefInfo_DelayFrame = delay;
        }

        public static int GetDelay()
        {
            return UIEntityBriefInfo_DelayFrame;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        public static IEnumerable<CodeInstruction> Real_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, // #8~11: if (this.frame > 15)
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "frame"),
                        new CodeMatch(OpCodes.Ldc_I4_S),
                        new CodeMatch(OpCodes.Ble)
                    )
                    .Advance(2)
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Patch), "GetDelay")) // change to if (this.frame > GetDelay())
                    .MatchForward(false, // #192~194: bool flag2 = flag && this.frame > 15;
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "frame"),
                        new CodeMatch(OpCodes.Ldc_I4_S),
                        new CodeMatch(OpCodes.Cgt)
                    )
                    .Advance(2)
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Patch), "GetDelay"))
                    .MatchForward(false, // #250~255: bool flag3 = this.frame % 4 == 0 || this.frame <= 16;
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "frame"),
                        new CodeMatch(OpCodes.Ldc_I4_S), // => call
                        new CodeMatch(OpCodes.Cgt),     // => Ldc_I4_1
                        new CodeMatch(OpCodes.Ldc_I4_0), // => add
                        new CodeMatch(OpCodes.Ceq)
                    )
                    .Advance(2)
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Patch), "GetDelay")) //change to bool flag3 = this.frame % 4 == 0 || this.frame == GetDelay() + 1;
                    .SetAndAdvance(OpCodes.Ldc_I4_1, null)
                    .SetAndAdvance(OpCodes.Add, null);

                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler UIEntityBriefInfo._OnUpdate failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        /*
        public static void Print(IEnumerable<CodeInstruction> instructions, int start, int end)
        {
            int count = -1;
            foreach (var i in instructions)
            {
                if (count++ < start)
                    continue;
                if (count > end)
                    break;

                if (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt)
                    Log.Warn($"{count,2} {i}");
                else if (i.IsLdarg())
                    Log.Info($"{count,2} {i}");
                else
                    Log.Debug($"{count,2} {i}");
            }
        }
        */
    }
}
