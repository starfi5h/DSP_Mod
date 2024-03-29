﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
    public class LabResearch_Patch
    {
        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        private static void ResearchSpeedModify(ref float speed)
        {
            // Note: LabComponent.InternalUpdateResearch need to handle by speed due to matrixPoints (num)
            speed *= Parameters.LabResearchUpdatePeriod;
        }

        static int techHashedThisFrame;

        [HarmonyPostfix, HarmonyPatch(typeof(GameStatData), nameof(GameStatData.RecordTechHashed))]
        static void RecordTechHashed(GameStatData __instance)
        {
            // When FacilityUpdatePeriod > 0, use the average hash in x tick
            techHashedThisFrame = 0;
            for (int i = 0; i < Parameters.LabResearchUpdatePeriod; i++)
                techHashedThisFrame += __instance.techHashedHistory[i];
            techHashedThisFrame /= Parameters.LabResearchUpdatePeriod;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.UpdateInfoDynamic))]
        [HarmonyPatch(typeof(UITechTip), nameof(UITechTip.UpdateInfoDynamic))]
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
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(LabResearch_Patch), nameof(techHashedThisFrame)))
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
