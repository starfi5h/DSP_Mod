using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AlterTickrate.Patches
{
	public class ProductionStatistics_Patch
	{
		static int threadCount = Environment.ProcessorCount;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MultithreadSystem), "Init")]
		[HarmonyPatch(typeof(MultithreadSystem), "ResetUsedThreadCnt")]
		internal static void Record_UsedThreadCnt(MultithreadSystem __instance)
		{
			threadCount = __instance.usedThreadCnt > 0 ? __instance.usedThreadCnt : 1;
			Log.Debug($"ThreadCount: {threadCount}");
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.PrepareTick))]
		static IEnumerable<CodeInstruction> PrepareTick_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// Add Threading.ForEachParallel(PrepareTick, GameMain.data.factoryCount)); return; at the begining
				var codeMatcher = new CodeMatcher(instructions)
					.Start()
					.InsertAndAdvance(
						HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
						Threading.ForEachParallel(PrepareTick, GameMain.data.factoryCount, threadCount)),
						new CodeInstruction(OpCodes.Ret)
					);
				return codeMatcher.InstructionEnumeration();
			}
			catch
			{
				Log.Error("Transpiler ProductionStatistics.PrepareTick failed.");
				return instructions;
			}
		}
		static void PrepareTick(int index)
		{
			GameMain.data.statistics.production.factoryStatPool[index].PrepareTick();
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
		static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// Remove this.factoryStatPool[i].GameTick(time); in the loop
				// Add Threading.ForEachParallel(GameTick, GameMain.data.factoryCount); at the begining
				var codeMatcher = new CodeMatcher(instructions)
					.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FactoryProductionStat), "GameTick")))
					.Advance(-5)
					.SetOpcodeAndAdvance(OpCodes.Nop)
					.RemoveInstructions(5)
					.Start()
					.Insert(
						HarmonyLib.Transpilers.EmitDelegate<Action>(() =>
						{
							Threading.ForEachParallel(GameTick, GameMain.data.factoryCount, threadCount);
						}
					));
				return codeMatcher.InstructionEnumeration();
			}
			catch
			{
				Log.Error("Transpiler ProductionStatistics.GameTick failed.");
				return instructions;
			}
		}

		static void GameTick(int index)
        {
			GameMain.data.statistics.production.factoryStatPool[index].GameTick(GameMain.gameTick);
		}
	}
}
