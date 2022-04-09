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
    }

    static class ThreadSystem
    {
        readonly static List<Worker> workers = new List<Worker>();
        public static int Count { get; private set; }
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
        }

        private void FactoryPowersystem_GameTick()
        {
            PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
            long time = GameMain.gameTick;
            int usedThreadCnt = ThreadSystem.Count;
            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                PlanetFactory factory = GameMain.data.factories[i];
                bool isActive = GameMain.data.localPlanet == factory.planet;
                //BeforePowerPartExecute
                factory.factorySystem?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 4);
                factory.cargoTraffic?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 4);
                factory.transport?.ParallelGameTickBeforePower(time, isActive, usedThreadCnt, Index, 2);
            }

            //PreparePowerSystemFactoryData, PowerSystemPartExecute
            PlanetFactory factory2 = GameMain.data.factories[Index];
            bool isActive2 = GameMain.data.localPlanet == factory2.planet;
            factory2.powerSystem.GameTick(time, isActive2, true);
            PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
        }

    }
}
