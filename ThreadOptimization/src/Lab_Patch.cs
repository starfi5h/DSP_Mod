using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace ThreadOptimization
{
    class Lab_Patch
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        internal static IEnumerable<CodeInstruction> InternalUpdateResearch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Only preserve TechState.hashUploaded value change, skip other changes of TechState
            // Change: if (ts.hashUploaded >= ts.hashNeeded)
            // To:     if (ts.hashUploaded >= ts.hashNeeded && fasle)
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(true,
                        new CodeMatch(i => i.IsLdarg()),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechState), nameof(TechState.hashUploaded))),
                        new CodeMatch(i => i.IsLdarg()),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechState), nameof(TechState.hashNeeded))),
                        new CodeMatch(OpCodes.Blt) //IL 448
                    );
                object label = matcher.Instruction.operand;
                matcher.Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Br, label));
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("LabComponent.InternalUpdateResearch_Transpiler failed.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode))]
        internal static IEnumerable<CodeInstruction> GameTickLabResearchMode_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            // lock(GameMain.history) for whole function
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions, iLGenerator)
                    .MatchForward(false, new CodeMatch(OpCodes.Ret))
                    .CreateLabel(out Label endLabel)
                    .Insert(
                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(GameMain), "history")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit"))
                    )
                    .Start()
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        Transpilers.EmitDelegate<Func<FactorySystem, bool>>((factorySystem) =>
                        {
                            bool hasLab = false;
                            hasLab = true;
                            /*
                            for (int k = 1; k < factorySystem.labCursor; k++)
                            {
                                if (factorySystem.labPool[k].id == k)
                                {
                                    hasLab = true;
                                    break;
                                }
                            }
                            */
                            // Skip lock if there is no lab on this planet
                            if (hasLab)
                                Monitor.Enter(GameMain.history);
                            return hasLab;
                        }),
                        new CodeInstruction(OpCodes.Brfalse, endLabel)
                    );

                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("FactorySystem.GameTickLabResearchMode_Transpiler failed.");
                return instructions;
            }
        }

        internal static bool ProcessUnlockTech()
        {
            // From FactorySystem.GameTickLabResearchMode() unlock tech part
            // Don't run this in Nebula client
            GameHistoryData history = GameMain.history;
            int techId = GameMain.history.currentTech;
            TechProto techProto = LDB.techs.Select(techId);
            if (techId > 0 && techProto != null && techProto.IsLabTech && GameMain.history.techStates.ContainsKey(techId))
            {
                var techState = GameMain.history.techStates[techId];
                if (techState.hashUploaded >= techState.hashNeeded)
                {
                    int curLevel = techState.curLevel;
                    if (techState.curLevel >= techState.maxLevel)
                    {
                        techState.curLevel = techState.maxLevel;
                        techState.hashUploaded = techState.hashNeeded;
                        techState.unlocked = true;
                    }
                    else
                    {
                        techState.curLevel++;
                        techState.hashUploaded = 0L;
                        techState.hashNeeded = techProto.GetHashNeeded(techState.curLevel);
                    }

                    history.techStates[techId] = techState;

                    if (techState.unlocked)
                        for (int l = 0; l < techProto.UnlockRecipes.Length; l++)
                            history.UnlockRecipe(techProto.UnlockRecipes[l]);

                    for (int m = 0; m < techProto.UnlockFunctions.Length; m++)
                        history.UnlockTechFunction(techProto.UnlockFunctions[m], techProto.UnlockValues[m], curLevel);

                    for (int n = 0; n < techProto.AddItems.Length; n++)
                        history.GainTechAwards(techProto.AddItems[n], techProto.AddItemCounts[n]);

                    history.NotifyTechUnlock(techId, curLevel);
                    history.DequeueTech();
                    return true;
                }
            }
            return false;
        }

    }
}
