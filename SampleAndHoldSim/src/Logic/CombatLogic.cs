using HarmonyLib;

namespace SampleAndHoldSim
{
    public partial class GameData_Patch
	{
		public static bool DFGroundSystemLogic_Prefix(GameData gameData, long time)
		{
			if (MainManager.UpdatePeriod <= 1) return true;

			// [Copy] tweak part in if (this.gameDesc.isCombatMode) { ... }
			PlanetFactory localLoadedPlanetFactory = gameData.localLoadedPlanetFactory;
			if (localLoadedPlanetFactory != null)
			{
				localLoadedPlanetFactory.LocalizeEnemies();
			}
			if (time > 0L)
			{
				PerformanceMonitor.BeginSample(ECpuWorkEntry.Enemy);
				for (int i = 0; i < workFactoryCount; i++)
				{
					workFactories[i].enemySystem.GameTickLogic(workFactoryTimes[i]); // keytick_timer++ in this
					workFactories[i].enemySystem.ExecuteDeferredEnemyChange();
				}
				PerformanceMonitor.EndSample(ECpuWorkEntry.Enemy);

				for (int i = 0; i < workFactoryCount; i++)
				{
					var enemySystem = workFactories[i].enemySystem;
					if (enemySystem != null && enemySystem.keytick_timer >= 60) // Update KeyTickLogic every 60 ticks
					{
						enemySystem.DecisionAI();
						enemySystem.KeyTickLogic(workFactoryTimes[i]);
						enemySystem.ExecuteDeferredEnemyChange();
						enemySystem.ExecuteDeferredUnitFormation();
						enemySystem.PostKeyTick();
					}
				}
			}
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.GameTick))]
		static bool SpaceSector_Prefix(SpaceSector __instance, long time)
		{
			// [Copy] Mofify SpaceSector.GameTick for tick twist
			if (MainManager.UpdatePeriod <= 1) return true;
			
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Skill);
			__instance.skillSystem.GameTick(time); //projectiles
			PerformanceMonitor.EndSample(ECpuWorkEntry.Skill);
			PerformanceMonitor.BeginSample(ECpuWorkEntry.Skill);
			__instance.skillSystem.AfterTick();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Skill);

			// Focus local hive logic
			UpdateHives(__instance, time);

			PerformanceMonitor.BeginSample(ECpuWorkEntry.Craft);
			__instance.combatSpaceSystem.GameTick(time); //fleet
			__instance.ExecuteDeferredCraftChange();
			PerformanceMonitor.EndSample(ECpuWorkEntry.Craft);
			__instance.RuinDataGameTick(time);
			return false;
		}
	
	
		static void UpdateHives(SpaceSector @this, long realTime)
        {
			int scale = MainManager.UpdatePeriod;
			int time = (int)(realTime & 0x7FFFFFFF); //positive value only
			int focusStarIndex = MainManager.FocusStarIndex;

			if (@this.dfHives != null)
			{
				// Set up localHive that run in normal tick
				EnemyDFHiveSystem localHive = null;
				if (focusStarIndex != -1 && @this.dfHives[focusStarIndex] != null)
					localHive = @this.dfHives[focusStarIndex];

				// Update space hive regular logic every UpdatePeriod tick
				PerformanceMonitor.BeginSample(ECpuWorkEntry.Enemy);
				int hiveLength = @this.dfHives.Length;
				for (int i = 0; i < hiveLength; i++)
				{
					if (i == focusStarIndex || (i +time) % scale != 0)
                    {
						continue;
                    }
					for (var enemyDFHiveSystem = @this.dfHives[i]; enemyDFHiveSystem != null; enemyDFHiveSystem = enemyDFHiveSystem.nextSibling)
					{
						// Update remote hives gametick logic
						enemyDFHiveSystem.GameTickLogic(time / scale, @this.galaxyAstros, @this.astros, @this.enemyPool, @this.enemyAnimPool);
						enemyDFHiveSystem.ExecuteDeferredEnemyChange();
					}
				}
				if (localHive != null)
				{					
					for (var enemyDFHiveSystem = localHive; enemyDFHiveSystem != null; enemyDFHiveSystem = enemyDFHiveSystem.nextSibling)
					{
						// Update local hive gametick logic
						enemyDFHiveSystem.GameTickLogic(time, @this.galaxyAstros, @this.astros, @this.enemyPool, @this.enemyAnimPool);
						enemyDFHiveSystem.ExecuteDeferredEnemyChange();
					}
				}
				PerformanceMonitor.EndSample(ECpuWorkEntry.Enemy);

				// Update space hive keyTick logic every (60 * UpdatePeriod) tick
				int cycleTick = time % (60 * scale); // prevent overflow in the following index
				int startIndex = (cycleTick - 1) * hiveLength / (60 * scale);
				int endIndex = cycleTick * hiveLength / (60 * scale);
				for (int i = startIndex; i < endIndex; i++)
				{
					int id = i % hiveLength;
					if (id == focusStarIndex) continue;
					HiveKeyTickLogic(@this.dfHives[id], time / scale);
				}
				if (localHive != null && time / 60 == 0)
				{
					// Update local hive keytick logic every 60 tick
					HiveKeyTickLogic(localHive, time);
				}
			}
		}
	
		static void HiveKeyTickLogic(EnemyDFHiveSystem DFhive, long time)
        {
			int expshr = 0;
			int threatshr = 0;
			EnemyDFHiveSystem enemyDFHiveSystem;
			for (enemyDFHiveSystem = DFhive; enemyDFHiveSystem != null; enemyDFHiveSystem = enemyDFHiveSystem.nextSibling)
			{
				if (!enemyDFHiveSystem.isEmpty)
				{
					enemyDFHiveSystem.DecisionAI(time);
					enemyDFHiveSystem.KeyTickLogic(time);
					enemyDFHiveSystem.InterLearningFromLocalSystem();
					enemyDFHiveSystem.InterLearningFromOtherSystem();
					enemyDFHiveSystem.ExecuteDeferredEnemyChange();
					enemyDFHiveSystem.ExecuteDeferredUnitFormation();
					expshr += enemyDFHiveSystem.evolve.exppshr;
					threatshr += enemyDFHiveSystem.evolve.threatshr;
					if (enemyDFHiveSystem.evolve.waveTicks == 0 && enemyDFHiveSystem.evolve.waveAsmTicks == 0)
					{
						enemyDFHiveSystem.evolve.threat += enemyDFHiveSystem.evolve.threatshr / 250;
					}
				}
			}
			for (enemyDFHiveSystem = DFhive; enemyDFHiveSystem != null; enemyDFHiveSystem = enemyDFHiveSystem.nextSibling)
			{
				if (!enemyDFHiveSystem.isEmpty)
				{
					enemyDFHiveSystem.evolve.AddExpPoint((expshr - enemyDFHiveSystem.evolve.exppshr) / 10);
					if (enemyDFHiveSystem.evolve.waveTicks == 0 && enemyDFHiveSystem.evolve.waveAsmTicks == 0)
					{
						enemyDFHiveSystem.evolve.threat += (threatshr - enemyDFHiveSystem.evolve.threatshr) / 50;
					}
					enemyDFHiveSystem.evolve.exppshr = 0;
					enemyDFHiveSystem.evolve.threatshr = 0;
				}
			}
		}
	}
}
