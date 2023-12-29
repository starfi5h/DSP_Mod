using HarmonyLib;

namespace SampleAndHoldSim
{
	public partial class GameData_Patch
	{
		public static bool DFGroundSystemLogic_Prefix(GameData gameData, long time)
		{
			if (MainManager.UpdatePeriod <= 1) return true;

			// Copy and tweak part in if (this.gameDesc.isCombatMode) { ... }
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
					workFactories[i].enemySystem.GameTickLogic(factoryTimes[i]); // keytick_timer++ in this
					workFactories[i].enemySystem.ExecuteDeferredEnemyChange();
				}
				PerformanceMonitor.EndSample(ECpuWorkEntry.Enemy);

				for (int i = 0; i < workFactoryCount; i++)
				{
					var enemySystem = workFactories[i].enemySystem;
					if (enemySystem != null && enemySystem.keytick_timer >= 60) // Update KeyTickLogic every 60 ticks
					{
						enemySystem.DecisionAI();
						enemySystem.KeyTickLogic(factoryTimes[i]);
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
		static bool SpaceSector_Prefix(SpaceSector __instance, ref long time)
		{
			if (MainManager.UpdatePeriod <= 1) return true;

			if (time % MainManager.UpdatePeriod != 0)
			{
				// Update only projectiles in idle tick
				PerformanceMonitor.BeginSample(ECpuWorkEntry.Skill);
				__instance.skillSystem.GameTick(time);
				PerformanceMonitor.EndSample(ECpuWorkEntry.Skill);
				PerformanceMonitor.BeginSample(ECpuWorkEntry.Skill);
				__instance.skillSystem.AfterTick();
				PerformanceMonitor.EndSample(ECpuWorkEntry.Skill);
				return false;
			}
			else
			{
				// Update space hive logic every UpdatePeriod
				time /= MainManager.UpdatePeriod;
				return true;
			}
		}
	}
}
