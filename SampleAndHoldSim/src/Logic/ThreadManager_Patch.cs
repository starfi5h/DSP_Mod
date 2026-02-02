using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace SampleAndHoldSim
{
    public static class ThreadManager_Patch
    {
        // 默認ThreadManager是單例
        static EGameLogicTask currentTask;

        public static void OnTaskBegin(int taskNum)
        {
            try
            {
                // 在OnTaskLogic前呼叫
                currentTask = (EGameLogicTask)taskNum;

                switch (currentTask)
                {
                    //case EGameLogicTask.FactoryTransport: // 1751
                    case EGameLogicTask.FactoryLabResearch: // 1700 (取尾數為0有barrier的)
                        BeforeTransport();
                        break;
                }
            }
            catch (Exception ex)
            {
                // 用try-catch避免卡死
                Debug.LogError($"[SampleAndHoldSim] Error on task begin {currentTask}: \n" + ex.ToString());
            }
        }

        public static void OnPhaseEnd()
        {
            try
            {
                // 在phaseBarrier.SignalAndWait後呼叫
                // 只有尾數為0的task有phase barrier
                switch (currentTask)
                {
                    case EGameLogicTask.FactoryLabOutput: // 1800
                        AfterTransport();
                        GameTick_Patch.FixLocalLabOutput();
                        break;
                }
            }
            catch (Exception ex)
            {
                // 用try-catch避免走不到SignalAndWait卡死
                Debug.LogError($"[SampleAndHoldSim] Error on phase end {currentTask}: \n" + ex.ToString());
            }
        }


        static void BeforeTransport()
        {
            // Goal: Execute FactoryManager.StationBeforeTick() before any PlanetTransport.GameTick() is called
            var gameLogic = GameMain.logic;
            for (int i = 0; i < gameLogic.factoryCount; i++)
            {
                if (MainManager.TryGet(gameLogic.factories[i].index, out FactoryManager manager) && manager.IsActive)
                {
                    manager.StationBeforeTransport();
                }
            }
            if (gameLogic.factoryCount != GameMain.data.factoryCount)
            {
                // 重置BatchTaskContext, 使用全部factories
                int threadCount = gameLogic.threadController.threadManager.enabledWorkerThreadCount;
                BatchTaskContext planetTransport = gameLogic.threadController.gameThreadContext.planetTransport;
                planetTransport.ResetFrame(gameLogic.factoryCount, threadCount);
                for (int j = 0; j < GameMain.data.factoryCount; j++)
                {
                    PlanetFactory planetFactory2 = GameMain.data.factories[j];
                    planetTransport.batchValues[j] = planetFactory2.transport.workerThreadWeight;
                }
                planetTransport.SortValues();
            }
        }

        static void AfterTransport()
        {
            // Goal: Execute FactoryManager.StationAfterTransport() after all PlanetTransport.GameTick() is done
            // To Prevent other stations's ship delivery interfare item count diff record
            var gameLogic = GameMain.logic;
            for (int i = 0; i < gameLogic.factoryCount; i++)
            {
                if (MainManager.TryGet(gameLogic.factories[i].index, out FactoryManager manager) && manager.IsActive)
                {
                    manager.StationAfterTransport();
                }
            }
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(StationComponent), nameof(StationComponent.InternalTickLocal))]
        static void Test()
        {
            // 似乎有InternalTickLocal比攔截函式先執行的情況, 要再觀察
            if (currentTask != EGameLogicTask.FactoryTransport
                && currentTask != EGameLogicTask.FactoryLabOutput
                && currentTask != EGameLogicTask.FactoryLabResearch)
                Log.Warn(currentTask);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ThreadManager), nameof(ThreadManager.ProcessFrame))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var matcher = new CodeMatcher(instructions, il);

            // ========================================================================
            // 任務 1: 在 OnTaskLogic 之前插入 OnTaskBegin(num)
            // ========================================================================
            // 目標代碼: this.OnTaskLogic(num, -1, enabledWorkerThreadCount);
            // IL 結構大致如下:
            // Ldarg.0 (this)
            // Ldfld (OnTaskLogic)
            // Ldloc.? (num)  <-- 我們需要抓取這個指令的操作數(Operand)來知道 num 是第幾個區域變數
            // Ldc.I4.M1 (-1)
            // Ldloc.? (enabledWorkerThreadCount)
            // Callvirt (Invoke)

            matcher.MatchForward(false, // false = 指針停在匹配序列的開頭
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ThreadManager), nameof(ThreadManager.OnTaskLogic))),
                new CodeMatch(i => i.IsLdloc()) // 這裡是 num
            );

            if (matcher.IsValid)
            {
                // 取得載入 num 的指令，我們需要它的 operand (變數索引)
                var loadNumInstruction = matcher.InstructionAt(2);

                // 插入: GetTaskEnum(num)
                // 1. 載入 num (使用剛才抓到的操作數)
                // 2. 呼叫靜態函式
                matcher.Insert(
                    new CodeInstruction(loadNumInstruction.opcode, loadNumInstruction.operand),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThreadManager_Patch), nameof(OnTaskBegin)))
                );
            }
            else
            {
                Log.Error("Transpiler ThreadManager.ProcessFrame Error: Could not find injection point for OnTaskBegin");
            }

            // ========================================================================
            // 任務 2: 在 phaseBarrier.SignalAndWait(0) 之後插入 OnPhaseEnd()
            // ========================================================================
            // 目標代碼: this.phaseBarrier.SignalAndWait(0);
            // IL 結構:
            // Ldarg.0
            // Ldfld (phaseBarrier)
            // Ldc.I4.0
            // Callvirt (SignalAndWait)

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(ThreadManager), nameof(ThreadManager.phaseBarrier))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HybridBarrier), nameof(HybridBarrier.SignalAndWait)))
            );

            if (matcher.IsValid)
            {
                matcher.Advance(1);
                // 插入 OnPhaseEnd()
                matcher.Insert(
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThreadManager_Patch), nameof(OnPhaseEnd)))
                );
            }
            else
            {
                Log.Error("Transpiler ThreadManager.ProcessFrame Error: Could not find injection point for OnPhaseEnd");
            }
            return matcher.InstructionEnumeration();
        }
    }
}
