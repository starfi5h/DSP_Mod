using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BuildToolOpt
{
    public static class RemoveGC_Patch
	{
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Path.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds))]
		public static IEnumerable<CodeInstruction> RemoveGC_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Remove force GC.Collect()			
			var codeMacher = new CodeMatcher(instructions).End();
			for (int i = 0; i < 5; i++)
			{
				codeMacher.Advance(-1);
				if (codeMacher.Opcode == OpCodes.Call && ((MethodInfo)codeMacher.Operand).Name == "Collect")
				{
					codeMacher.SetOpcodeAndAdvance(OpCodes.Nop);
					break;
				}
			}
			return codeMacher.InstructionEnumeration();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DSPGame), nameof(DSPGame.EndGame))]
		public static void GCCollect()
        {
			//var timer = new HighStopwatch();
			//timer.Begin();
			System.GC.Collect();
			//Plugin.Log.LogDebug("GCCollect() " + timer.duration);
		}
	}
}
