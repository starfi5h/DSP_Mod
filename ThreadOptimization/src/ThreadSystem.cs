using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity;
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
		static int count;

        public static void Schedule(EMission mission, int workerCount)
        {
			count = workerCount;
			for (int i = 0; i < count; i++)
            {
				if (workers.Count < count)
					workers.Add(new Worker(i));
				workers[i].Mission = mission;
				workers[i].CompleteEvent.Reset();
				ThreadPool.QueueUserWorkItem(workers[i].Callback);
			}
		}

		public static void Complete()
        {
			for (int i = 0; i < count; i++)
				workers[i].CompleteEvent.WaitOne();
		}
    }

	class Worker
	{
		public EMission Mission { get; set; }
		public int Index { get; }
		public WaitCallback Callback { get; }
		public AutoResetEvent CompleteEvent { get; }

		public Worker(int index)
		{
			Index = index;
			Callback = new WaitCallback(ComputerThread);
			CompleteEvent = new AutoResetEvent(true);
		}

		public void ComputerThread(object state = null)
		{
			try
			{
				//var watch = new HighStopwatch();
				//watch.Begin();
				switch (Mission)
				{
					case EMission.Factory:
						GameMain.data.factories[Index].GameTick(GameMain.gameTick);
						break;

					case EMission.DysonRocket:
						GameMain.data.dysonSpheres[Index].RocketGameTick();
						break;

					case EMission.FactoryStat:
						GameMain.data.statistics.production.factoryStatPool[Index].GameTick(GameMain.gameTick);
						break;

					default: break;
				}
				CompleteEvent.Set();
				//Log.Debug($"[{Index,2}]: {watch.duration * 1000}");
			}
			catch (Exception e)
			{
				Log.Error($"Thread Error! mission:{Mission} index:{Index}");
				Log.Error(e);
			}
		}
	}

}
