using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DeliverySlotsTweaks
{
    public class PlayerPackagePatch
    {
        public static int packageStacksize = 1000;

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItem),
new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) },
new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItemStacked))]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.Sort))]
        static IEnumerable<CodeInstruction> OverwritePlayerPackageStacksize(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            CodeMatcher matcher = new CodeMatcher(instructions, il);

            // Change: num2 = StorageComponent.itemStackCount[itemId];
            // To:     num2 = this == GameMain.mainPlayer.package ? PlayerPackagePatch.packageStacksize : StorageComponent.itemStackCount[itemId];

            matcher
                .MatchForward(false, new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.itemStackCount))))
                .Advance(1)
                .CreateLabelAt(matcher.Pos, out Label jmpNormalFlow);
            int startPos = matcher.Pos;

            matcher
                .MatchForward(false, new CodeMatch(i => i.IsStloc()))
                .CreateLabelAt(matcher.Pos, out Label jmpEnd);

            matcher.Advance(startPos - matcher.Pos);
            matcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameMain), "get_mainPlayer")),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Player), "get_package")),
                    new CodeInstruction(OpCodes.Bne_Un_S, jmpNormalFlow),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPackagePatch), "packageStacksize")),
                    new CodeInstruction(OpCodes.Br_S, jmpEnd)
                );

            return matcher.Instructions();
        }
    }
}
