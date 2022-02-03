﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
                if (++count < start)
                    continue;
                if (count > end)
                    break;

                if (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt || i.opcode == OpCodes.Ret)
                    Log.Warn($"{count,2} {i}");
                else if (i.IsLdarg())
                    Log.Info($"{count,2} {i}");
                else if (i.opcode == OpCodes.Nop)
                    Log.Error($"{count,2} {i}");
                else
                    Log.Debug($"{count,2} {i}");

            }
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.PickupBeltItems))]
        static IEnumerable<CodeInstruction> Debug_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //Log.Info("Before");
            //Print(instructions, 0, 70);
            Log.Info("");
            //Log.Info("After");
            var code = PickupBeltItems_Transpiler(instructions);
            Print(code, 75, 90);
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


        //[HarmonyTranspiler]
        //[HarmonyPatch(nameof(CargoTraffic.PickupBeltItems))]
        private static IEnumerable<CodeInstruction> PickupBeltItems_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(true,
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == nameof(CargoPath.TryPickItem))
                    );

            if (codeMatcher.IsInvalid)
            {
                //NebulaModel.Logger.Log.Error("CargoTraffic.PickupBeltItems_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }

            CodeInstruction itemCountRef = codeMatcher.InstructionAt(-2);

            return codeMatcher
                    .Advance(2)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 5))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3))
                    .InsertAndAdvance(HarmonyLib.Transpilers.EmitDelegate<Action<int, int, int, bool>>((item, cnt, belt, all) =>
                    {
                        Log.Debug($"{item} {cnt} {belt} {all}");
                        //if (Multiplayer.IsActive)
                        {
                        //    Multiplayer.Session.Belts.RegisterBeltPickupUpdate(item, cnt, belt, seg, inc);
                        }
                    }))
                    .InstructionEnumeration();
        }


        //[HarmonyTranspiler]
        //[HarmonyPatch(typeof(UISpraycoaterWindow), nameof(UISpraycoaterWindow._OnUpdate))]
        public static IEnumerable<CodeInstruction> _OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Replace: if (cargoPath.TryInsertItem(Mathf.Max(4, beltComponent.segIndex + beltComponent.segPivotOffset - 20), this.player.inhandItemId, 1, (byte)num))
            // To:      if (this.traffic.PutItemOnBelt(spraycoaterComponent.cargoBeltId, this.player.inhandItemId, (byte)num))

            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CargoPath), nameof(CargoPath.TryInsertItem))))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    HarmonyLib.Transpilers.EmitDelegate<Func<byte, UISpraycoaterWindow, bool>>((itemInc, window) =>
                    {
                        Log.Debug($"{window.player.inhandItemId} {itemInc}");

                        int itemId = window.player.inhandItemId;
                        int cargoBeltId = window.traffic.spraycoaterPool[window.spraycoaterId].cargoBeltId;
                        return window.traffic.PutItemOnBelt(cargoBeltId, itemId, itemInc);
                    }))
                .RemoveInstruction()
                .Advance(-17)
                .RemoveInstructions(14); // remove #81~94, leave only (byte)num

            Log.Debug(matcher.Pos);
            return matcher.InstructionEnumeration();
        }





        //[HarmonyTranspiler]
        //[HarmonyPatch(typeof(UIBeltBuildTip), nameof(UIBeltBuildTip.SetFilterToEntity))]
        public static IEnumerable<CodeInstruction> SetFilterToEntity_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldelema),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stfld))
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2),
                                    new CodeInstruction(OpCodes.Ldarg_0),
                                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBeltBuildTip), nameof(UIBeltBuildTip.outputSlotId))),
                                    new CodeInstruction(OpCodes.Ldc_I4_1),
                                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerTest), nameof(TranspilerTest.SetSlot))))
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldelema),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Stfld))
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2),
                                    new CodeInstruction(OpCodes.Ldarg_0),
                                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBeltBuildTip), nameof(UIBeltBuildTip.outputSlotId))),
                                    new CodeInstruction(OpCodes.Ldarg_0),
                                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIBeltBuildTip), nameof(UIBeltBuildTip.selectedIndex))),
                                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerTest), nameof(TranspilerTest.SetSlot))))
                ;
            Log.Debug(matcher.Pos);
            return matcher.InstructionEnumeration();
        }

        private static void SetSlot(StationComponent stationComponent, int outputSlotId, int selectedIndex)
        {
            Log.Debug($"{stationComponent.id} {outputSlotId} {selectedIndex}");
        }


        static IEnumerable<CodeInstruction> Demo_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator IL)
        {
            Log.Info("Original code");
            Print(instructions, 0, 10);
            Log.Info("");
            Log.Info("Matchforward false");
            var code = Test1(instructions, IL);
            Print(code, 0, 10);
            Log.Info("");
            Log.Info("Matchforward true");
            code = Test2(instructions, IL);
            Print(code, 0, 10);
            return instructions;
        }

        //[HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveAsLastExit))]
        private static IEnumerable<CodeInstruction> Test1(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            CodeMatcher matcher = new CodeMatcher(instructions, il);
            matcher.MatchForward(false,
                new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "SaveCurrentGame"),
                new CodeMatch(i => i.opcode == OpCodes.Ret)
                );
            Log.Info($"matcher.Pos = {matcher.Pos}");
            matcher.Insert(new CodeInstruction(OpCodes.Nop));
            return matcher.InstructionEnumeration();
        }

        private static IEnumerable<CodeInstruction> Test2(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            CodeMatcher matcher = new CodeMatcher(instructions, il);
            matcher.MatchForward(true,
                new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "SaveCurrentGame"),
                new CodeMatch(i => i.opcode == OpCodes.Ret)
                );
            Log.Info($"matcher.Pos = {matcher.Pos}");
            matcher.Insert(new CodeInstruction(OpCodes.Nop));
            return matcher.InstructionEnumeration();
        }








        private static IEnumerable<CodeInstruction> CreatePrebuilds_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            CodeMatcher matcher = new CodeMatcher(instructions, il)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_controller"),
                    new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(PlayerController), nameof(PlayerController.cmd))),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(CommandState), nameof(CommandState.stage))));

            if (matcher.IsValid)
            {
                Log.Debug(matcher.Pos);
                Log.Debug(matcher.Pos + 20);
                Log.Debug(matcher.InstructionAt(20));
            }

            matcher
                    .InsertAndAdvance(HarmonyLib.Transpilers.EmitDelegate<Func<bool>>(() =>
                    {
                        return true;
                    }))
                    .CreateLabelAt(matcher.Pos + 19, out Label jmpLabel)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Brtrue, jmpLabel));
            Log.Debug(jmpLabel);
            return matcher.InstructionEnumeration();
        }
    }
}
