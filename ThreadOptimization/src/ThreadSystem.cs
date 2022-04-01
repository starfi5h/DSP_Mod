﻿using System;
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
        FactoryStat
    }

    static class ThreadSystem
    {
        readonly static List<Worker> workers = new List<Worker>();
        static int workerCount;
        static int RunningCount;
        static ManualResetEvent CompleteEvent = new ManualResetEvent(true);

        public static void Schedule(EMission mission, int count)
        {
            if (RunningCount > 0)
            {
                Log.Warn($"Schedule({mission}) called before previous jobs finished");
                Complete();
            }

            workerCount = count;
            for (int i = workers.Count; i < workerCount; i++)
                workers.Add(new Worker());

            RunningCount = workerCount;
            CompleteEvent.Reset();
            for (int i = 0; i < workerCount; i++)
                workers[i].Assign(mission, i);
        }

        public static void Complete()
        {
            CompleteEvent.WaitOne();

            int num = MultithreadSystem.usedThreadCntSetting;
            if (num == 0)
                num = SystemInfo.processorCount;
            for (int i = 0; i < workerCount; i++)
            {
                // Calculate time by sum
                for (int k = 0; k < PerformanceMonitor.timeCostsFrame.Length; k++)
                {
                    PerformanceMonitor.timeCostsFrame[k] += workers[i].TimeCostsFrame[k] / num;
                }
            }
        }
        public static void OnFinished()
        {
            Interlocked.Decrement(ref RunningCount);
            if (RunningCount <= 0)
                CompleteEvent.Set();
        }
    }

    class Worker
    {
        public EMission Mission { get; private set; }
        public int Index { get; private set; }
        public double[] TimeCostsFrame { get; }

        readonly WaitCallback callback;
        readonly HighStopwatch[] clocks;

        public Worker()
        {
            callback = new WaitCallback(ComputerThread);
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
            ThreadPool.QueueUserWorkItem(callback);
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
                ThreadSystem.OnFinished();
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
    }
}
