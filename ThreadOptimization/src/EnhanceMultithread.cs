using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ThreadOptimization
{
    class EnhanceMultithread
    {
		static readonly WaitCallback factoryCallBack =  new WaitCallback(FactoryTick_Multithread);
		static readonly AutoResetEvent factoryEvent = new AutoResetEvent(true);

		internal static void FactoryTick_Multithread(object state = null)
        {
			GameData data = GameMain.data;
			long time = GameMain.gameTick;

			#region power system			
            PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
			GameMain.multithreadSystem.PrepareBeforePowerFactoryData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
			GameMain.multithreadSystem.PreparePowerSystemFactoryData(GameMain.localPlanet, data.factories, data.factoryCount, time, GameMain.mainPlayer);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);

			/* New method - power system load balance is still required
			GameMain.multithreadSystem.PrepareBeforePowerFactoryData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.PreparePowerSystemFactoryData(GameMain.localPlanet, data.factories, data.factoryCount, time, GameMain.mainPlayer);
			ThreadSystem.Schedule(EMission.FactoryPowerSystem, data.factoryCount);
			ThreadSystem.Complete();
			*/
			#endregion



			PerformanceMonitor.BeginSample(ECpuWorkEntry.Facility);
			GameMain.multithreadSystem.PrepareAssemblerFactoryData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Lab);
			for (int l = 0; l < data.factoryCount; l++)
			{
				bool isActive = GameMain.localPlanet == data.factories[l].planet;
				if (data.factories[l].factorySystem != null)
				{
					data.factories[l].factorySystem.GameTickLabResearchMode(time, isActive);
				}
			}
			GameMain.multithreadSystem.PrepareLabOutput2NextData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Lab);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Facility);

			PerformanceMonitor.BeginSample(ECpuWorkEntry.Transport);
			GameMain.multithreadSystem.PrepareTransportData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Transport);

			#region belt
			/*
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
			for (int m = 0; m < data.factoryCount; m++)
			{
				if (data.factories[m].transport != null)
				{
					data.factories[m].transport.GameTick_InputFromBelt();
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Inserter);
			GameMain.multithreadSystem.PrepareInserterData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Inserter);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
			for (int n = 0; n < data.factoryCount; n++)
			{
				bool isActive2 = GameMain.localPlanet == data.factories[n].planet;
				if (data.factories[n].factoryStorage != null)
				{
					data.factories[n].factoryStorage.GameTick(time, isActive2);
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
			GameMain.multithreadSystem.PrepareCargoPathsData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Splitter);
			for (int num = 0; num < data.factoryCount; num++)
			{
				if (data.factories[num].cargoTraffic != null)
				{
					data.factories[num].cargoTraffic.SplitterGameTick();
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Splitter);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
			for (int num2 = 0; num2 < data.factoryCount; num2++)
			{
				if (data.factories[num2].cargoTraffic != null)
				{
					data.factories[num2].cargoTraffic.MonitorGameTick();
					data.factories[num2].cargoTraffic.SpraycoaterGameTick();
					data.factories[num2].cargoTraffic.PilerGameTick();
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
			for (int num3 = 0; num3 < data.factoryCount; num3++)
			{
				if (data.factories[num3].transport != null)
				{
					data.factories[num3].transport.GameTick_OutputToBelt();
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalCargo);
			GameMain.multithreadSystem.PreparePresentCargoPathsData(GameMain.localPlanet, data.factories, data.factoryCount, time);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.LocalCargo);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Digital);
			for (int num4 = 0; num4 < data.factoryCount; num4++)
			{
				bool isActive3 = GameMain.localPlanet == data.factories[num4].planet;
				if (data.factories[num4].digitalSystem != null)
				{
					data.factories[num4].digitalSystem.GameTick(isActive3);
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Digital);
			*/
			#endregion

			ThreadSystem.Schedule(EMission.FactoryBelt, data.factoryCount);
			ThreadSystem.Complete();

			PerformanceMonitor.EndSample(ECpuWorkEntry.Factory);
			factoryEvent.Set();
		}

		//[HarmonyPrefix]
		//[HarmonyPatch(typeof(MultithreadSystem), "PrepareBeforePowerFactoryData", new Type[] { typeof(PlanetData), typeof(PlanetFactory[]), typeof(int), typeof(long) })]
		//[HarmonyPatch(typeof(MultithreadSystem), "PreparePowerSystemFactoryData", new Type[] { typeof(PlanetData), typeof(PlanetFactory[]), typeof(int), typeof(long), typeof(Player) })]
		internal static bool PrepareData_Prefix()
		{
			return !enable;
		}



		static bool enable = false;

		[HarmonyPrefix, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick)), HarmonyPriority(Priority.Last)]
		internal static bool GameTick_Prefix()
		{
			if (Input.GetKeyDown(KeyCode.F9))
			{
				enable = !enable;
				Log.Info("Activate: " + enable);
			}
			if (enable)
			{
				GameTick();
				return false;
			}
			return true;
		}

		internal static void GameTick()
        {
			GameData data = GameMain.data;
			long time = GameMain.gameTick;
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			double gameTime = GameMain.gameTime;
			if (!DSPGame.IsMenuDemo)
			{
				data.statistics.PrepareTick();
				data.history.PrepareTick();
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			if (data.localPlanet != null && data.localPlanet.factoryLoaded)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
				data.localPlanet.factory.cargoTraffic.ClearStates();
				data.localPlanet.physics.GameTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
			}
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
			if (data.guideMission != null)
			{
				data.guideMission.GameTick();
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
			if (data.mainPlayer != null && !data.demoTicked)
			{
				data.mainPlayer.GameTick(time);
			}
			data.DetermineRelative();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			for (int i = 0; i < data.dysonSpheres.Length; i++)
			{
				if (data.dysonSpheres[i] != null)
				{
					data.dysonSpheres[i].BeforeGameTick(time);
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Factory);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
			for (int j = 0; j < data.factoryCount; j++)
			{
				Assert.NotNull(data.factories[j]);
				if (data.factories[j] != null)
				{
					data.factories[j].BeforeGameTick(time);
					// CreateDysonSphere() has to done in mainthread
					if (data.factories[j].factorySystem != null)
					{
						data.factories[j].factorySystem.CheckBeforeGameTick();
					}
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
			if (GameMain.multithreadSystem.multithreadSystemEnable)
			{
				// Schedule multithread factory in another thread
				factoryEvent.Reset();
				ThreadPool.QueueUserWorkItem(factoryCallBack, data);
			}
			else
			{
				for (int num5 = 0; num5 < data.factoryCount; num5++)
				{
					data.factories[num5].GameTick(time);
				}
				PerformanceMonitor.EndSample(ECpuWorkEntry.Factory);
			}
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Trash);
			data.trashSystem.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Trash);
			if (data.localPlanet != null && data.localPlanet.factoryLoaded)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalAudio);
				data.localPlanet.audio.GameTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.LocalAudio);
			}

			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			if (GameMain.multithreadSystem.multithreadSystemEnable)
			{
				for (int num6 = 0; num6 < data.dysonSpheres.Length; num6++)
				{
					if (data.dysonSpheres[num6] != null)
					{
						data.dysonSpheres[num6].GameTick(time);
					}
				}
				PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);


				// Wait for factory
				factoryEvent.WaitOne();
				PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
				PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
				GameMain.multithreadSystem.PrepareRocketFactoryData(data.dysonSpheres, data.dysonSpheres.Length);
				GameMain.multithreadSystem.Schedule();
				GameMain.multithreadSystem.Complete();
				PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
			}
			else
			{
				for (int num7 = 0; num7 < data.dysonSpheres.Length; num7++)
				{
					if (data.dysonSpheres[num7] != null)
					{
						data.dysonSpheres[num7].GameTick(time);
						PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
						data.dysonSpheres[num7].RocketGameTick();
						PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
					}
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);

			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			if (!DSPGame.IsMenuDemo)
			{
				data.statistics.GameTick(time);
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Digital);
			if (!DSPGame.IsMenuDemo)
			{
				data.warningSystem.GameTick(time);
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Digital);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
			data.milestoneSystem.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			data.history.AfterTick();
			data.statistics.AfterTick();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			data.preferences.Collect();
			if (DSPGame.IsMenuDemo)
			{
				data.demoTicked = true;
			}
		}
    }
}
