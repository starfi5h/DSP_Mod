using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DeliverySlotsTweaks
{
    public class PlayerPackagePatch
    {
        static int packageStacksize = 1000;
        static int packageStackMultiplier = 1;

        public static void OnConfigChange()
        {
            packageStackMultiplier = 1;
            if (Plugin.PlayerPackageStackSize.Value > 0)
            {
                // TODO: Add ammo in the future
                packageStacksize = Plugin.PlayerPackageStackSize.Value;
                Plugin.Log.LogDebug("PlayerPackage stack count:" + packageStacksize);
            }
            else if (Plugin.PlayerPackageStackMultiplier.Value > 0)
            {
                packageStackMultiplier = Plugin.PlayerPackageStackMultiplier.Value;
                Plugin.Log.LogDebug("PlayerPackage stack multiplier:" + packageStackMultiplier);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.SetFilter))] //玩家背包的中間鍵固定格子
        public static void OverwritePlayerPackageStacksize2(StorageComponent __instance, int gridIndex, int filterId)
        {
            if (filterId > 0 && IsPlayerStorage(__instance))
            {
                if (Plugin.PlayerPackageStackSize.Value > 0)
                    __instance.grids[gridIndex].stackSize = packageStacksize;
                else
                    __instance.grids[gridIndex].stackSize = StorageComponent.itemStackCount[filterId] * packageStackMultiplier;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItem),
new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) },
new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItemStacked))]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.Sort))]
        [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.AddItemForSort))]
        static IEnumerable<CodeInstruction> OverwritePlayerPackageStacksize(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var matcher = new CodeMatcher(instructions, il);

            // Change: num2 = StorageComponent.itemStackCount[itemId];
            // To:     num2 = IsPlayerStorage(this) ? PlayerPackagePatch.packageStacksize : StorageComponent.itemStackCount[itemId];
            // Or:     num2 = IsPlayerStorage(this) ? StorageComponent.itemStackCount[itemId] * PlayerPackagePatch.packageStackMultiplier : StorageComponent.itemStackCount[itemId];

            matcher
                .MatchForward(false, new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(StorageComponent), nameof(StorageComponent.itemStackCount))))
                .Advance(1)
                .CreateLabelAt(matcher.Pos, out Label jmpNormalFlow);
            int startPos = matcher.Pos;

            matcher
                .MatchForward(false, new CodeMatch(i => i.IsStloc()))
                .CreateLabelAt(matcher.Pos, out Label jmpEnd);


            if (Plugin.PlayerPackageStackSize.Value > 0)
            {
                matcher.Advance(startPos - matcher.Pos);
                matcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlayerPackagePatch), nameof(IsPlayerStorage))),
                    new CodeInstruction(OpCodes.Brfalse_S, jmpNormalFlow),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPackagePatch), nameof(packageStacksize))),
                    new CodeInstruction(OpCodes.Br_S, jmpEnd)
                );
            }
            else
            {
                matcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlayerPackagePatch), nameof(IsPlayerStorage))),
                    new CodeInstruction(OpCodes.Brfalse_S, jmpEnd),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PlayerPackagePatch), nameof(packageStackMultiplier))),
                    new CodeInstruction(OpCodes.Mul)
                ); ;
            }

            return matcher.Instructions();
        }

        public static bool IsPlayerStorage(StorageComponent storageComponent)
        {
            return storageComponent == GameMain.mainPlayer.package
                || storageComponent == GameMain.mainPlayer.mecha.reactorStorage;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PackageStatistics), nameof(PackageStatistics.Count))]
        static bool CountOverwrite(PackageStatistics __instance, StorageComponent package)
        {
            if (!IsPlayerStorage(package)) return true;

            __instance.itemBundle.Clear();
            ref StorageComponent.GRID[] grids = ref package.grids;
            int emptySlotCount = 0;

            if (packageStackMultiplier > 1)
            {
                int[] itemStackCount = StorageComponent.itemStackCount;
                for (int i = 0; i < package.size; i++)
                {
                    int itemId = grids[i].itemId;
                    if (itemId > 0)
                        __instance.itemBundle.Add(itemId, grids[i].count, itemStackCount[itemId] * packageStackMultiplier - grids[i].count); // Fix capactiy
                    else
                        emptySlotCount++;
                }
                if (emptySlotCount > 0)
                {
                    int[] itemIds = ItemProto.itemIds;
                    int num2 = itemIds.Length;
                    for (int j = 0; j < num2; j++)
                        __instance.itemBundle.Add(itemIds[j], 0, itemStackCount[itemIds[j]] * packageStackMultiplier * emptySlotCount); // Fix capactiy
                }
            }
            else
            {
                for (int i = 0; i < package.size; i++)
                {
                    int itemId = grids[i].itemId;
                    if (itemId > 0)
                        __instance.itemBundle.Add(itemId, grids[i].count, packageStacksize - grids[i].count); // Fix capactiy
                    else
                        emptySlotCount++;
                }
                if (emptySlotCount > 0)
                {
                    int[] itemIds = ItemProto.itemIds;
                    for (int j = 0; j < itemIds.Length; j++)
                        __instance.itemBundle.Add(itemIds[j], 0, packageStacksize * emptySlotCount); // Fix capactiy
                }
            }
            return false;
        }
    }
}
