using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using Unity;
using UnityEngine;

namespace ThreadOptimization
{
	class GameData_Patch
    {
		[HarmonyPrefix, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
		internal static bool GameData_Prefix(GameData __instance, long time)
		{
			#region origin1
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			if (!DSPGame.IsMenuDemo)
			{
				__instance.statistics.PrepareTick();
				__instance.history.PrepareTick();
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
				__instance.localPlanet.factory.cargoTraffic.ClearStates();
				__instance.localPlanet.physics.GameTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
			}
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
			if (__instance.guideMission != null)
			{
				__instance.guideMission.GameTick();
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
			if (__instance.mainPlayer != null && !__instance.demoTicked)
			{
				__instance.mainPlayer.GameTick(time);
			}
			__instance.DetermineRelative();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			for (int i = 0; i < __instance.dysonSpheres.Length; i++)
			{
				if (__instance.dysonSpheres[i] != null)
				{
					__instance.dysonSpheres[i].BeforeGameTick(time);
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);
			#endregion

			PerformanceMonitor.BeginSample(ECpuWorkEntry.Factory);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
			for (int j = 0; j < __instance.factoryCount; j++)
			{
				Assert.NotNull(__instance.factories[j]);
				__instance.factories[j].BeforeGameTick(time);
				// CreateDysonSphere() has to done in mainthread
				if (__instance.factories[j].factorySystem != null)
					__instance.factories[j].factorySystem.CheckBeforeGameTick();
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);

			// Assign Factory.GameTick()
			ThreadSystem.Schedule(EMission.Factory, __instance.factoryCount);

			PerformanceMonitor.BeginSample(ECpuWorkEntry.Trash);
			__instance.trashSystem.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Trash);
			
			// Run DysonSphere.GameTick() in mainthread
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			for (int i = 0; i < __instance.dysonSpheres.Length; i++)
				__instance.dysonSpheres[i]?.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);

			if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalAudio);
				__instance.localPlanet.audio.GameTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.LocalAudio);

			}

			// Wait for all Factory to finish
			ThreadSystem.Complete();
			// Handle changes of historyData and UI popup
			Lab_Patch.ProcessUnlockTech();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Factory);

			// Use orignal multithreadSystem for DysonRocket
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
			GameMain.multithreadSystem.PrepareRocketFactoryData(__instance.dysonSpheres, __instance.dysonSpheres.Length);
			GameMain.multithreadSystem.Schedule();
			GameMain.multithreadSystem.Complete();
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);

			#region origin2
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			if (!DSPGame.IsMenuDemo)
			{
				__instance.statistics.GameTick(time);
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Digital);
			if (!DSPGame.IsMenuDemo)
			{
				__instance.warningSystem.GameTick(time);
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.Digital);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
			__instance.milestoneSystem.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			__instance.history.AfterTick();
			__instance.statistics.AfterTick();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
			__instance.preferences.Collect();
			if (DSPGame.IsMenuDemo)
			{
				__instance.demoTicked = true;
			}
			#endregion

			return false;
		}
	}
}
