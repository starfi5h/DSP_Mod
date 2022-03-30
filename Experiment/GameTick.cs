using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using Unity;
using UnityEngine;

namespace Experiment
{
    class GameTick
    {
		static Thread mainThread = System.Threading.Thread.CurrentThread;
		static List<Worker> FactoryWorkers = new List<Worker>();

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        internal static bool GameData_Prefix(GameData __instance, long time)
        {
            #region origin1
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
			double gameTime = GameMain.gameTime;
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

			if (FactoryWorkers.Count < __instance.factoryCount)
            {
				FactoryWorkers.Clear();
				for (int i = 0; i < __instance.factoryCount; i++)
                {
					FactoryWorkers.Add(new Worker(Worker.EMission.Factory, i));
                }
			}

			// Assign Factory.GameTick()
			for (int i = 0; i < __instance.factoryCount; i++)
			{
				FactoryWorkers[i].Mission = Worker.EMission.Factory;
				FactoryWorkers[i].CompleteEvent.Reset();
				ThreadPool.QueueUserWorkItem(FactoryWorkers[i].Callback);
			}


			



			
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Trash);
			__instance.trashSystem.GameTick(time);
			PerformanceMonitor.EndSample(ECpuWorkEntry.Trash);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
			if (GameMain.multithreadSystem.multithreadSystemEnable)
			{
				for (int num6 = 0; num6 < __instance.dysonSpheres.Length; num6++)
				{
					if (__instance.dysonSpheres[num6] != null)
					{
						__instance.dysonSpheres[num6].GameTick(time);
					}
				}
				PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
				GameMain.multithreadSystem.PrepareRocketFactoryData(__instance.dysonSpheres, __instance.dysonSpheres.Length);
				GameMain.multithreadSystem.Schedule();
				GameMain.multithreadSystem.Complete();
				PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
			}
			else
			{
				for (int num7 = 0; num7 < __instance.dysonSpheres.Length; num7++)
				{
					if (__instance.dysonSpheres[num7] != null)
					{
						__instance.dysonSpheres[num7].GameTick(time);
						PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
						__instance.dysonSpheres[num7].RocketGameTick();
						PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
					}
				}
			}
			PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);
			if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalAudio);
				__instance.localPlanet.audio.GameTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.LocalAudio);
			
			}

			// Wait for Factory
			for (int i = 0; i < __instance.factoryCount; i++)
			{
				FactoryWorkers[i].CompleteEvent.WaitOne();
			}
			DetermineUnlockTech();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Factory);

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

		//[HarmonyPrefix, HarmonyPatch(typeof(ProductionStatistics), nameof(ProductionStatistics.GameTick))]
		internal static bool ProductionStatistics_Prefix(ProductionStatistics __instance)
        {
			return false;
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
		internal static IEnumerable<CodeInstruction> InternalUpdateResearch_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// Only preserve TechState.hashUploaded value change, skip other changes of TechState
			// Change: if (ts.hashUploaded >= ts.hashNeeded)
			// To:     if (ts.hashUploaded >= ts.hashNeeded && fasle)
			try
			{
				CodeMatcher matcher = new CodeMatcher(instructions)
					.MatchForward(true,
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechState), nameof(TechState.hashUploaded))),
						new CodeMatch(i => i.IsLdarg()),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechState), nameof(TechState.hashNeeded))),
						new CodeMatch(OpCodes.Blt) //IL 448
					);
				object label = matcher.Instruction.operand;
				matcher.Advance(1)
					.InsertAndAdvance(new CodeInstruction(OpCodes.Br, label));
				return matcher.InstructionEnumeration();
			}
			catch
			{
				Log.Error("LabComponent.InternalUpdateResearch_Transpiler failed.");
				return instructions;
			}
		}

		[HarmonyTranspiler, HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabResearchMode))]
		internal static IEnumerable<CodeInstruction> GameTickLabResearchMode_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			// lock(GameMain.history) for whole function
			try
			{
				CodeMatcher matcher = new CodeMatcher(instructions, iLGenerator)
					.MatchForward(false, new CodeMatch(OpCodes.Ret))
					.CreateLabel(out Label endLabel)
					.Insert(
						new CodeInstruction(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(GameMain), "history")),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit"))
					)
					.Start()					
					.Insert(
						new CodeInstruction(OpCodes.Ldarg_0),
						Transpilers.EmitDelegate<Func<FactorySystem, bool>>((factorySystem) =>
						{
							int labCount = 0;
							for (int k = 1; k < factorySystem.labCursor; k++)
							{
								if (factorySystem.labPool[k].id == k)
									labCount++;
							}
							// Skip lock if there is no lab on this planet
							if (labCount > 0)
								Monitor.Enter(GameMain.history);
							return labCount > 0;
						}),
						new CodeInstruction(OpCodes.Brfalse, endLabel)
					);


				TranspilerTest.Print(matcher.InstructionEnumeration(), 0, 20);
				TranspilerTest.Print(matcher.InstructionEnumeration(), 500, 600);

				//return instructions;
				return matcher.InstructionEnumeration();
			}
			catch
			{
				Log.Error("FactorySystem.GameTickLabResearchMode_Transpiler failed.");
				return instructions;
			}
		}

		private static bool DetermineUnlockTech()
        {
			// From FactorySystem.GameTickLabResearchMode() unlock tech part
			// Don't run this in Nebula client
			GameHistoryData history = GameMain.history;
			int techId = GameMain.history.currentTech;
			TechProto techProto = LDB.techs.Select(techId);
			if (techId > 0 && techProto != null && techProto.IsLabTech && GameMain.history.techStates.ContainsKey(techId))
			{
				var techState = GameMain.history.techStates[techId];
				if (techState.hashUploaded >= techState.hashNeeded)
				{
					int curLevel = techState.curLevel;
					if (techState.curLevel >= techState.maxLevel)
					{
						techState.curLevel = techState.maxLevel;
						techState.hashUploaded = techState.hashNeeded;
						techState.unlocked = true;
					}
					else
					{
						techState.curLevel++;
						techState.hashUploaded = 0L;
						techState.hashNeeded = techProto.GetHashNeeded(techState.curLevel);
					}

					history.techStates[techId] = techState;

					if (techState.unlocked)
						for (int l = 0; l < techProto.UnlockRecipes.Length; l++)
							history.UnlockRecipe(techProto.UnlockRecipes[l]);

					for (int m = 0; m < techProto.UnlockFunctions.Length; m++)
						history.UnlockTechFunction(techProto.UnlockFunctions[m], techProto.UnlockValues[m], curLevel);

					for (int n = 0; n < techProto.AddItems.Length; n++)
						history.GainTechAwards(techProto.AddItems[n], techProto.AddItemCounts[n]);

					history.NotifyTechUnlock(techId, curLevel);
					history.DequeueTech();
					return true;
				}				
			}
			return false;
		}

		class Worker
        {
			public enum EMission
            {
				Factory,
				DysonRocket,
				FactoryStat
			}

			public EMission Mission { get; set; }
			public int Index { get;  }
			public WaitCallback Callback { get; }
			public AutoResetEvent CompleteEvent { get; }

			public Worker(EMission mission, int index)
            {
				Mission = mission;
				Index = index;
				Callback = new WaitCallback(ComputerThread);
				CompleteEvent = new AutoResetEvent(true);
			}

			public void ComputerThread(object state = null)
            {
				try
				{
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
					}
					CompleteEvent.Set();
				}
				catch (Exception e)
                {
					Log.Error($"Thread Error! mission:{Mission} index:{Index}");
					Log.Error(e);
                }
			}
		}


    }
}
