using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
    public class UITech_Patch
    {
        static int techHashedThisFrame;

        [HarmonyPostfix, HarmonyPatch(typeof(GameStatData), nameof(GameStatData.Init))]
        static void Init_Postfix()
        {
            techHashedThisFrame = 0;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameStatData), nameof(GameStatData.RecordTechHashed))]
        static void RecordTechHashed_Postfix(GameStatData __instance)
        {
            // sliding window
            techHashedThisFrame += __instance.techHashedHistory[0] - __instance.techHashedHistory[Parameters.FacilityUpdatePeriod];
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.UpdateInfoDynamic))]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechTip.UpdateInfoDynamic))]
        static IEnumerable<CodeInstruction> UpdateInfoDynamic_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace statistics.techHashedThisFrame with techHashedThisFrame
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.IsLdloc()),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "techHashedThisFrame")
                    )
                    .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Nop, null)
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(UITech_Patch), nameof(techHashedThisFrame)))
                    );

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("Transpiler UpdateInfoDynamic failed");
                return instructions;
            }
        }
    }
}
