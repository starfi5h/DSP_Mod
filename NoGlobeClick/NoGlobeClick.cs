using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;


[assembly: AssemblyTitle(NoGobleClick.Plugin.NAME)]
[assembly: AssemblyVersion(NoGobleClick.Plugin.VERSION)]

namespace NoGobleClick
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.NoGobleClick";
        public const string NAME = "NoGobleClick";
        public const string VERSION = "1.0.0";

        Harmony harmony;

        public void Awake()
        {
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
        }

#if DEBUG
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
#endif

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGlobemap), nameof(UIGlobemap.LocateLogic))]
        static void LocateLogic_Prefix(ref bool ___ignoreLocate)
        {
            // ignore locate marker when shift is not pressed
            ___ignoreLocate = !VFInput._godModeMechaMove;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerAction_Rts), nameof(PlayerAction_Rts.GameTick))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetField = AccessTools.Field(typeof(CommandState), "type");
            var matcher = new CodeMatcher(instructions);

            // 1. 找到以下段落
            // 
            // else if (cmd.type != ECommand.Creative) <<<
            // {
            //   if (this.controller.input0.x != 0f || this.controller.input0.y != 0f)
            //   {
            //      this.player.ClearOrders();
            //      return;
            //   }
            // }

            matcher.End().MatchBack(false,
                new CodeMatch(OpCodes.Ret),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, targetField),
                new CodeMatch(OpCodes.Ldc_I4_6), // ECommand.Creative
                new CodeMatch(i => i.opcode == OpCodes.Beq)
            );

            if (matcher.IsInvalid)
            {
                Debug.LogWarning("[Transpiler] 未能匹配到目標 IL 指令段落！");
                return instructions;
            }

            // 2. 獲取 beq 指令的目標 Label（跳轉地址）
            // matcher.InstructionAt(3) 即為 beq.s 指令
            matcher.Advance(1);
            var jumpTarget = matcher.InstructionAt(3).operand;

            // 3. 替換為無條件跳轉 br.s (或 br)
            // 將 ldloc.0, ldfld, ldc.i4.6 三條指令移除/替換，並把 beq 改為 br.s
            matcher.SetAndAdvance(OpCodes.Nop, null)       // 覆蓋 ldloc.0
                   .SetAndAdvance(OpCodes.Nop, null)       // 覆蓋 ldfld
                   .SetAndAdvance(OpCodes.Nop, null)       // 覆蓋 ldc.i4.6
                   .Set(OpCodes.Br_S, jumpTarget);         // 將 beq.s 改為 br.s

            return matcher.InstructionEnumeration();
        }
    }
}
