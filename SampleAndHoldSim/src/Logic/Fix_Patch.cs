using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public class Fix_Patch // Fixes for potential errors
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ScatterTaskContext), nameof(ScatterTaskContext.ResetFrame), new Type[] { typeof(int), typeof(int) })]
		[HarmonyPatch(typeof(ScatterTaskContext), nameof(ScatterTaskContext.ResetFrame), new Type[] { typeof(long), typeof(int), typeof(int) })]
		public static void ResetFrame_Prefix(ref int _batchCount)
		{
			// Avoid DivideByZeroException
			if (_batchCount == 0) _batchCount = 1;
		}

		[HarmonyPrefix, HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(PowerExchangerComponent), nameof(PowerExchangerComponent.CalculateActualEnergyPerTick))]
		public static bool CalculateActualEnergyPerTick_Overwrite(ref PowerExchangerComponent __instance, bool isOutput, ref long __result)
		{
			int inc;
			if (isOutput)
			{
				inc = __instance.poolInc;
			}
			else
			{
				int emptyCount = __instance.emptyCount;
				int emptyInc = __instance.emptyInc;
				inc = __instance.split_inc(ref emptyCount, ref emptyInc, 1);
			}
			if (inc > 0)
			{
				if (inc >= Cargo.accTableMilli.Length) // Fix for abnormal inc
                {
					inc = Cargo.accTableMilli.Length - 1;
				}
				__result = __instance.energyPerTick + (long)(__instance.energyPerTick * Cargo.accTableMilli[inc] + 0.1);
				return false;
			}
			__result = __instance.energyPerTick;
			return false;
		}

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._spraycoater_parallel))]
        public static IEnumerable<CodeInstruction> _spraycoater_parallel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			// 修復增產劑消耗統計數據不正確的問題
			// 目標: 將consumeRegister的索引改成和其他函式一致 (mod替換後兩者將不是同一值)
			// Change: int[] consumeRegister = factoryStatPool[batchCurrent].consumeRegister;
			// To:     int[] consumeRegister = factoryStatPool[this.factories[batchCurrent].index].consumeRegister;

			try
			{
				var matcher = new CodeMatcher(instructions)
					// 尋找: factoryStatPool[batchCurrent].consumeRegister
					// IL 模式:
					// ldloc.s factoryStatPool (或其他 ldloc 變體)
					// ldloc.s batchCurrent
					// ldelem.ref
					// ldfld consumeRegister
					.MatchForward(false,
						new CodeMatch(ci => ci.IsLdloc()), // factoryStatPool
						new CodeMatch(ci => ci.IsLdloc()), // batchCurrent
						new CodeMatch(OpCodes.Ldelem_Ref),
						new CodeMatch(ci => ci.opcode == OpCodes.Ldfld &&
							((FieldInfo)ci.operand).Name == "consumeRegister")
					);

				if (matcher.IsValid)
				{
					// 將batchCurrent替換成this.factories[batchCurrent].index
					matcher.Advance(1);
					var batchCurrentInstruction = matcher.Instruction;
					matcher.RemoveInstruction()
						.Insert(
							new CodeInstruction(OpCodes.Ldarg_0),
							new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GameLogic), nameof(GameLogic.factories))),
							batchCurrentInstruction,
							new CodeInstruction(OpCodes.Ldelem_Ref),
							new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlanetFactory), nameof(PlanetFactory.index)))
						);
				}
				else
				{
					Log.Warn("Transpiler GameLogic._spraycoater_parallel fail. Can't find the target");
				}
				return matcher.InstructionEnumeration();
			}
			catch (Exception ex)
            {
				Log.Warn("Transpiler GameLogic._spraycoater_parallel error:");
				Log.Warn(ex);
				return instructions;
			}            
        }

		[HarmonyPrefix, HarmonyPriority(Priority.Low)]
		[HarmonyPatch(typeof(GameLogic), nameof(GameLogic.ContextCollect_FactoryComponents_MultiMain))]
		static void ContextCollect_FactoryComponents_MultiMain_Prefix(GameLogic __instance, ref PlanetFactory __state)
		{
			// 改動了就不要執行presentCargo, 因為localLoadedFactory.index的對應關係失效
			// if (this.localLoadedFactory != null) Array.Fill<int>(...)
			if (__instance.factoryCount != GameMain.data.factoryCount)
			{
				__state = __instance.localLoadedFactory;
				__instance.localLoadedFactory = null;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameLogic), nameof(GameLogic.ContextCollect_FactoryComponents_MultiMain))]
		static void ContextCollect_FactoryComponents_MultiMain_Postfix(GameLogic __instance, int threadCount, PlanetFactory __state)
		{
			// 在factories內容不相同時, 需要對presentCargo的設定做修正			
			if (__state != null)
			{
				__instance.localLoadedFactory = __state;
				DeepProfiler.BeginSample(DPEntry.CargoPresent, -1, 0L);
				ScatterTaskContext presentCargo = __instance.threadController.gameThreadContext.presentCargo;
				int[] ordinals = presentCargo.ordinals;
				presentCargo.ResetFrame(__instance.factoryCount, threadCount);
				// 找出localLoadedFactory在factories中真正的index
				int realIndex = -1;
				for (int i = 0; i < __instance.factoryCount; i++)
				{
					if (__instance.factories[i] == __instance.localLoadedFactory)
					{
						realIndex = i;
						break;
					}
				}
				if (realIndex != -1)
				{
					var fillValue = __instance.localLoadedFactory.cargoTraffic.pathCursor - 1;
					for (int i = realIndex + 1; i <= __instance.factoryCount; i++)
					{
						ordinals[i] = fillValue;
					}
				}
				presentCargo.DetermineThreadTasks();
				DeepProfiler.EndSample(-1, -2L);
			}
		}

		public static void FixMinerProductCount()
        {
			int facotryCount = GameMain.data.factoryCount;
			for (int factoryIndex = 0; factoryIndex < facotryCount; factoryIndex++)
            {
				var factory = GameMain.data.factories[factoryIndex];
				if (factory == null) continue;
				var factorySystem = factory.factorySystem;

				for (int i = 0; i < factorySystem.minerCursor; i++)
                {
					ref var miner = ref factorySystem.minerPool[i];
					if (miner.productCount < 0) miner.productCount = 0; // Fix miners that have negative tmp storage
					else if (miner.productCount > 50) miner.productCount = 50; // Assume tmp storage max limit is 50
				}
            }
		}
	}
}
