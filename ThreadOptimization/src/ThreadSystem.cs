using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ThreadOptimization
{
    public enum EMission
    {
        None,
        Factory,
        DysonRocket,
        FactoryStat,
        FactoryBelt,
        FactoryPowerSystem,
        Facility,
        Transport
    }



    static class ThreadSystem
    {
        readonly static List<Worker> workers = new List<Worker>();
        public static int Count { get; private set; }
        public static int UsedThreadCnt { get; private set; }
        static bool running;

        public static void Schedule(EMission mission, int workerCount)
        {
            if (running)
            {
                Log.Warn($"Schedule({mission}) called before previous jobs finished");
                Complete();
            }

            Count = workerCount;
            if (workers.Count < Count)
            {
                workers.Clear();
                for (int i = 0; i < Count; i++)
                    workers.Add(new Worker());
            }

            for (int i = 0; i < Count; i++)
            {
                workers[i].Assign(mission, i);
            }
            running = true;
        }

        public static void Complete()
        {
            int num = MultithreadSystem.usedThreadCntSetting;
            if (num == 0)
            {
                num = SystemInfo.processorCount;
            }
            if (num > 128)
            {
                num = 128;
            }

            for (int i = 0; i < Count; i++)
            {
                workers[i].Wait();

                // Calculate time by sum
                for (int k = 0; k < PerformanceMonitor.timeCostsFrame.Length; k++)
                {
                    PerformanceMonitor.timeCostsFrame[k] += workers[i].TimeCostsFrame[k] / num;
                }
            }
            running = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MultithreadSystem), "Init")]
        [HarmonyPatch(typeof(MultithreadSystem), "ResetUsedThreadCnt")]
        internal static void Record_UsedThreadCnt(MultithreadSystem __instance)
        {
            ThreadSystem.UsedThreadCnt = __instance.usedThreadCnt;
            if (ThreadSystem.UsedThreadCnt <= 0)
                ThreadSystem.UsedThreadCnt = 1;
            Log.Debug($"usedThreadCnt {__instance.usedThreadCnt}");
        }
    }

    class Worker
    {
        public EMission Mission { get; private set; }
        public int Index { get; private set; }
        public double[] TimeCostsFrame { get; }

        WaitCallback callback;
        AutoResetEvent completeEvent;
        readonly HighStopwatch[] clocks;

        public Worker()
        {
            callback = new WaitCallback(ComputerThread);
            completeEvent = new AutoResetEvent(true);
            clocks = new HighStopwatch[PerformanceMonitor.timeCostsFrame.Length];
            TimeCostsFrame = new double[PerformanceMonitor.timeCostsFrame.Length];
            for (int i = 0; i < clocks.Length; i++)
                clocks[i] = new HighStopwatch();
        }

        public void Assign(EMission mission, int index)
        {
            Mission = mission;
            Index = index;
            Array.Clear(TimeCostsFrame, 0, TimeCostsFrame.Length);
            completeEvent.Reset();
            ThreadPool.QueueUserWorkItem(callback);
        }

        public void Wait()
        {
            completeEvent.WaitOne();
        }

        public void ComputerThread(object state = null)
        {
            try
            {
                switch (Mission)
                {
                    case EMission.Factory:
                        //GameMain.data.factories[Index].GameTick(GameMain.gameTick);
                        PlanetFactory_GameTick(GameMain.data.factories[Index], GameMain.gameTick);
                        break;

                    case EMission.DysonRocket:
                        GameMain.data.dysonSpheres[Index].RocketGameTick();
                        break;

                    case EMission.FactoryStat:
                        GameMain.data.statistics.production.factoryStatPool[Index].GameTick(GameMain.gameTick);
                        break;

                    case EMission.FactoryBelt:
                        FactoryBelt_GameTick(GameMain.data.factories[Index], GameMain.gameTick);
                        break;

                    case EMission.FactoryPowerSystem:
                        FactoryPowersystem_GameTick();
                        break;

                    case EMission.Facility:
                        Facility_GameTick();
                        break;

                    case EMission.Transport:
                        Transport_GameTick(GameMain.data.factories[Index], GameMain.gameTick);
                        break;

                    default: break;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Thread Error! mission:{Mission} index:{Index}");
                Log.Error(e);
                throw (e);
            }
            finally
            {
                completeEvent.Set();
            }
        }

        private void PlanetFactory_GameTick(PlanetFactory factory, long time)
        {
            bool flag = GameMain.localPlanet == factory.planet;
            BeginSample(ECpuWorkEntry.PowerSystem);
            if (factory.factorySystem != null)
            {
                factory.factorySystem.GameTickBeforePower(time, flag);
            }
            if (factory.cargoTraffic != null)
            {
                factory.cargoTraffic.GameTickBeforePower(time, flag);
            }
            if (factory.transport != null)
            {
                factory.transport.GameTickBeforePower(time, flag);
            }
            if (factory.powerSystem != null)
            {
                factory.powerSystem.GameTick(time, flag, false);
            }
            EndSample(ECpuWorkEntry.PowerSystem);
            if (factory.factorySystem != null)
            {
                BeginSample(ECpuWorkEntry.Facility);
                factory.factorySystem.CheckBeforeGameTick();
                factory.factorySystem.GameTick(time, flag);
                BeginSample(ECpuWorkEntry.Lab);
                factory.factorySystem.GameTickLabProduceMode(time, flag);
                factory.factorySystem.GameTickLabResearchMode(time, flag);
                factory.factorySystem.GameTickLabOutputToNext(time, flag);
                EndSample(ECpuWorkEntry.Lab);
                EndSample(ECpuWorkEntry.Facility);
            }
            BeginSample(ECpuWorkEntry.Transport);
            if (factory.transport != null)
            {
                factory.transport.GameTick(time, flag);
            }
            EndSample(ECpuWorkEntry.Transport);
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.transport != null)
            {
                factory.transport.GameTick_InputFromBelt();
            }
            EndSample(ECpuWorkEntry.Storage);
            BeginSample(ECpuWorkEntry.Inserter);
            if (factory.factorySystem != null)
            {
                factory.factorySystem.GameTickInserters(time, flag);
            }
            EndSample(ECpuWorkEntry.Inserter);
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.factoryStorage != null)
            {
                factory.factoryStorage.GameTick(time, flag);
            }
            EndSample(ECpuWorkEntry.Storage);
            if (factory.cargoTraffic != null)
            {
                factory.cargoTraffic.GameTick(time);
            }
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.transport != null)
            {
                factory.transport.GameTick_OutputToBelt();
            }
            EndSample(ECpuWorkEntry.Storage);
            if (flag)
            {
                BeginSample(ECpuWorkEntry.LocalCargo);
                if (factory.cargoTraffic != null)
                {
                    factory.cargoTraffic.PresentCargoPathsSync();
                }
                EndSample(ECpuWorkEntry.LocalCargo);
            }
            BeginSample(ECpuWorkEntry.Digital);
            factory.digitalSystem.GameTick(flag);
            EndSample(ECpuWorkEntry.Digital);
        }

        private void BeginSample(ECpuWorkEntry logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
                clocks[(int)logic].Begin();
        }

        private void EndSample(ECpuWorkEntry logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
                TimeCostsFrame[(int)logic] += clocks[(int)logic].duration;
        }

        private void FactoryBelt_GameTick(PlanetFactory factory, long time)
        {
            bool flag = GameMain.localPlanet == factory.planet;
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.transport != null)
            {
                factory.transport.GameTick_InputFromBelt();
            }
            EndSample(ECpuWorkEntry.Storage);
            BeginSample(ECpuWorkEntry.Inserter);
            if (factory.factorySystem != null)
            {
                factory.factorySystem.GameTickInserters(time, flag);
            }
            EndSample(ECpuWorkEntry.Inserter);
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.factoryStorage != null)
            {
                factory.factoryStorage.GameTick(time, flag);
            }
            EndSample(ECpuWorkEntry.Storage);
            if (factory.cargoTraffic != null)
            {
                factory.cargoTraffic.GameTick(time);
            }
            BeginSample(ECpuWorkEntry.Storage);
            if (factory.transport != null)
            {
                factory.transport.GameTick_OutputToBelt();
            }
            EndSample(ECpuWorkEntry.Storage);
            if (flag)
            {
                BeginSample(ECpuWorkEntry.LocalCargo);
                if (factory.cargoTraffic != null)
                {
                    factory.cargoTraffic.PresentCargoPathsSync();
                }
                EndSample(ECpuWorkEntry.LocalCargo);
            }
            BeginSample(ECpuWorkEntry.Digital);
            factory.digitalSystem.GameTick(flag);
            EndSample(ECpuWorkEntry.Digital);

            //PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
            if (!DSPGame.IsMenuDemo)
            {
                GameMain.data.statistics.production.factoryStatPool[factory.index].GameTick(time);
            }
            //PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
        }

        private void FactoryPowersystem_GameTick()
        {
            long time = GameMain.gameTick;
            int usedThreadCnt = ThreadSystem.Count;

            GameMain.data.statistics.production.factoryStatPool[Index].PrepareTick();

            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                PlanetFactory factory = GameMain.data.factories[i];
                bool isActive = GameMain.localPlanet == factory.planet;
                //BeforePowerPartExecute
                factory.factorySystem?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 4);
                factory.cargoTraffic?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 4);
                factory.transport?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 2);
            }

            //PreparePowerSystemFactoryData, PowerSystemPartExecute
            PlanetFactory factory2 = GameMain.data.factories[Index];
            bool isActive2 = GameMain.data.localPlanet == factory2.planet;
            factory2.powerSystem.GameTick(time, isActive2, true);
        }

        private void Facility_GameTick()
        {
            long time = GameMain.gameTick;
            int usedThreadCnt = ThreadSystem.Count;
            for (int l = 0; l < GameMain.data.factoryCount; l++)
            {
                PlanetFactory factory = GameMain.data.factories[l];
                bool isActive = GameMain.localPlanet == factory.planet;
                if (factory.factorySystem != null)
                {
                    factory.factorySystem.GameTick(time, isActive, usedThreadCnt, Index, 4);
                    BeginSample(ECpuWorkEntry.Lab);

                    GameHistoryData history = GameMain.history;
                    GameStatData statistics = GameMain.statistics;
                    FactoryProductionStat factoryProductionStat = statistics.production.factoryStatPool[factory.index];
                    int[] consumeRegister = factoryProductionStat.consumeRegister;
                    AnimData[] entityAnimPool = factory.entityAnimPool;
                    SignData[] entitySignPool = factory.entitySignPool;
                    int[][] entityNeeds = factory.entityNeeds;
                    PowerSystem powerSystem = factory.powerSystem;
                    float[] networkServes = powerSystem.networkServes;
                    PowerConsumerComponent[] consumerPool = powerSystem.consumerPool;
                    float dt = 0.016666668f;
                    int num = history.currentTech;
                    TechProto techProto = LDB.techs.Select(history.currentTech);
                    TechState techState = default(TechState);
                    bool flag = false;
                    float speed = (float)history.techSpeed;
                    int techHashedThisFrame = statistics.techHashedThisFrame;
                    long universeMatrixPointUploaded = history.universeMatrixPointUploaded;
                    long hashRegister = factoryProductionStat.hashRegister;
                    LabComponent[] labPool = factory.factorySystem.labPool;
                    int _techHashedThisFrame = techHashedThisFrame;
                    long _universeMatrixPointUploaded = universeMatrixPointUploaded;
                    long _hashRegister = hashRegister;


                    if (num > 0 && techProto != null && techProto.IsLabTech && GameMain.history.techStates.ContainsKey(num))
                    {
                        techState = history.techStates[num];
                        flag = true;
                    }
                    if (!flag)
                        num = 0;
                    int num2 = 0;
                    if (flag)
                    {
                        for (int i = 0; i < techProto.Items.Length; i++)
                        {
                            int num3 = techProto.Items[i] - LabComponent.matrixIds[0];
                            if (num3 >= 0 && num3 < 5)
                            {
                                num2 |= 1 << num3;
                            }
                            else if (num3 == 5)
                            {
                                num2 = 32;
                                break;
                            }
                        }
                    }
                    if (num2 > 32)
                    {
                        num2 = 32;
                    }
                    if (num2 < 0)
                    {
                        num2 = 0;
                    }
                    float num4 = (float)LabComponent.techShaderStates[num2] + 0.2f;
                    if (factory.factorySystem.researchTechId != num)
                    {
                        factory.factorySystem.researchTechId = num;
                        for (int j = 1; j < factory.factorySystem.labCursor; j++)
                        {
                            if (factory.factorySystem.labPool[j].id == j && factory.factorySystem.labPool[j].researchMode)
                            {
                                factory.factorySystem.labPool[j].SetFunction(true, 0, factory.factorySystem.researchTechId, entitySignPool);
                            }
                        }
                    }

                    if (WorkerThreadExecutor.CalculateMissionIndex(1, factory.factorySystem.labCursor - 1, usedThreadCnt, Index, 30, out int start, out int end))
                    {
                        for (int k = start; k < end; k++)
                        {
                            if (labPool[k].id == k)
                            {
                                if (labPool[k].researchMode)
                                {
                                    int entityId = labPool[k].entityId;
                                    uint num5 = 0U;
                                    float power = networkServes[consumerPool[labPool[k].pcId].networkId];

                                    labPool[k].UpdateNeedsResearch();
                                    if (flag)
                                    {
                                        num5 = labPool[k].InternalUpdateResearch(power, speed, consumeRegister, ref techState, ref techHashedThisFrame, ref universeMatrixPointUploaded, ref hashRegister);
                                        entityAnimPool[entityId].working_length = ((num5 > 0U) ? num4 : 0f);
                                        entityAnimPool[entityId].prepare_length = 1f;
                                    }
                                    entityAnimPool[entityId].power = power;
                                    entityAnimPool[entityId].Step01(num5, dt);
                                    entityNeeds[entityId] = labPool[k].needs;
                                    if (entitySignPool[entityId].signType == 0U || entitySignPool[entityId].signType > 3U)
                                    {
                                        entitySignPool[entityId].signType = (labPool[k].researchMode ? ((num5 > 0U) ? 0U : 6U) : ((labPool[k].recipeId == 0) ? 4U : ((num5 > 0U) ? 0U : 6U)));
                                    }
                                }
                            }
                        }
                    }
                    lock (history)
                    {
                        techState = history.techStates[num];
                        techState.hashUploaded += hashRegister - _hashRegister;
                        history.techStates[num] = techState;
                        statistics.techHashedThisFrame += techHashedThisFrame - _techHashedThisFrame;
                        history.universeMatrixPointUploaded += universeMatrixPointUploaded - _universeMatrixPointUploaded;
                        factoryProductionStat.hashRegister += hashRegister - _hashRegister;
                    }

                    factory.factorySystem.GameTickLabOutputToNext(time, isActive, usedThreadCnt, Index, 30);
                    EndSample(ECpuWorkEntry.Lab);
                }
            }
        }
    
        private void Transport_GameTick(PlanetFactory factory, long time)
        {
            bool isActive = GameMain.localPlanet == factory.planet;
            if (factory.transport != null)
                factory.transport.GameTick(time, isActive);
        }
    }
}
