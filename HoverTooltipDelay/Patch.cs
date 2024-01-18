using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace HoverTooltipDelay
{
    class Patch
    {
        static int UIEntityBriefInfo_DelayFrame = 15;
        static KeyCode KeyFastFillin = KeyCode.Tab;

        public static void SetConfig(int delay, KeyCode fastFillin)
        {
            UIEntityBriefInfo_DelayFrame = delay;
            KeyFastFillin = fastFillin;
        }

        public static int GetDelay()
        {
            return UIEntityBriefInfo_DelayFrame;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        public static IEnumerable<CodeInstruction> UIEntityBriefInfo_Transpiler(IEnumerable<CodeInstruction> instructions)
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
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Patch), nameof(GetDelay))) //change to bool flag3 = this.frame % 4 == 0 || this.frame == GetDelay() + 1;
                    .SetAndAdvance(OpCodes.Ldc_I4_1, null)
                    .SetAndAdvance(OpCodes.Add, null);

                return matcher.InstructionEnumeration();
            }
            catch
            {
                Plugin.log.LogError("Transpiler UIEntityBriefInfo._OnUpdate failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnUpdate")]
        public static void OnUpdate_Postfix(UIEntityBriefInfo __instance)
        {
            if (!Input.GetKeyDown(KeyFastFillin)) return;
            if (__instance.factory == null) return;
            if (__instance.entityId <= 0 || __instance.entityId >= __instance.factory.entityCursor) return;

            TryFastFillStationStoarge(__instance.factory, __instance.entityId);
            // fast fillin from package
            __instance.factory.EntityFastFillIn(__instance.entityId, true, out ItemBundle itemBundle);
            if (itemBundle.items.Count > 0)
                VFAudio.Create("transfer-item", null, Vector3.zero, true, 3, -1, -1L);
            UIItemup.ForceResetAllGets();
        }

        static void TryFastFillStationStoarge(PlanetFactory factory, int entityId)
        {
            var stationId = factory.entityPool[entityId].stationId;
            if (stationId <= 0) return;

            //Plugin.log.LogDebug("Fast Fillin to station " + stationId);
            var stationComponent = factory.transport.stationPool[stationId];
            if (stationComponent?.storage == null) return;
            int stationMaxItemCount = LDB.models.Select(factory.entityPool[entityId].modelIndex).prefabDesc.stationMaxItemCount;
            if (stationComponent.isCollector)
                stationMaxItemCount += GameMain.history.localStationExtraStorage;
            else if (stationComponent.isVeinCollector)
                stationMaxItemCount += GameMain.history.localStationExtraStorage;
            else if (stationComponent.isStellar)
                stationMaxItemCount += GameMain.history.remoteStationExtraStorage;
            else
                stationMaxItemCount += GameMain.history.localStationExtraStorage;

            StationStore[] storage = stationComponent.storage;
            for (int i = 0; i < stationComponent.storage.Length; i++)
            {
                int itemId = storage[i].itemId;
                if (itemId > 0)
                {
                    int count = stationMaxItemCount - storage[i].count;
                    int inc = 0;
                    if (count > 0)
                    {
                        GameMain.mainPlayer.packageUtility.TryTakeItemFromAllPackages(ref itemId, ref count, out inc, false);
                    }
                    if (count > 0)
                    {                        
                        storage[i].count = storage[i].count + count;
                        storage[i].inc = storage[i].inc + inc;
                        break;
                    }
                }
            }
        }
    }
}
