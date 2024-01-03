using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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


	}

	class EnemyUnitComponent_Patch
    {
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.ApproachToTargetPoint_SLancer))]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.Attack_SLancer))]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_OrbitTarget_SLancer))]
		static IEnumerable<CodeInstruction> ScaleDamage(IEnumerable<CodeInstruction> instructions)
		{
			var plasma = AccessTools.Field(typeof(GeneralProjectile), nameof(GeneralProjectile.damage));
			var laser = AccessTools.Field(typeof(SpaceLaserOneShot), nameof(SpaceLaserOneShot.damage));
			var sweep = AccessTools.Field(typeof(SpaceLaserSweep), nameof(SpaceLaserSweep.damage));

			var codeMatcher = new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Stfld &&
					((FieldInfo)i.operand == plasma || (FieldInfo)i.operand == laser || (FieldInfo)i.operand == sweep)))
				.Repeat(matcher => {
					matcher
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EnemyUnitComponent_Patch), nameof(ModDamage)))
					)
					.Advance(5);
					}
				);
			return codeMatcher.InstructionEnumeration();
		}

		static int ModDamage(int originValue, EnemyDFHiveSystem enemyDFHive)
		{
			if (MainManager.UpdatePeriod <= 1) return originValue;
			if (enemyDFHive.starData.index == MainManager.FocusStarIndex) return originValue;
			return originValue / MainManager.UpdatePeriod;
		}
	}
}
