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

		[HarmonyPrefix, HarmonyPriority(Priority.High)]
		[HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.AddTrashFromGroundEnemy))]
		public static void AddTrashFromGroundEnemy_Prefix(PlanetFactory factory, ref int life)
        {
			// Scale the life (1800) of dark fog drop on remote planets
			if (factory.planetId != MainManager.FocusPlanetId)
				life *= MainManager.UpdatePeriod;
		}
	}

	class Combat_Patch
    {
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.ApproachToTargetPoint_SLancer))]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.Attack_SLancer))]
		[HarmonyPatch(typeof(EnemyUnitComponent), nameof(EnemyUnitComponent.RunBehavior_OrbitTarget_SLancer))]
		static IEnumerable<CodeInstruction> ScaleDamageDown(IEnumerable<CodeInstruction> instructions)
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
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Combat_Patch), nameof(ModDamageDown)))
					)
					.Advance(5);
					}
				);
			return codeMatcher.InstructionEnumeration();
		}

		static int ModDamageDown(int originValue, EnemyDFHiveSystem enemyDFHive)
		{
			if (MainManager.UpdatePeriod <= 1 || enemyDFHive.starData.index == MainManager.FocusStarIndex)
				return originValue;
			return originValue / MainManager.UpdatePeriod;
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.Shoot_Plasma))]
		[HarmonyPatch(typeof(TurretComponent), nameof(TurretComponent.Shoot_Missile))]
		static IEnumerable<CodeInstruction> ScaleDamageUp(IEnumerable<CodeInstruction> instructions)
		{
			var plasma = AccessTools.Field(typeof(GeneralProjectile), nameof(GeneralProjectile.damage));
			var missile = AccessTools.Field(typeof(GeneralMissile), nameof(GeneralMissile.damage));

			var codeMatcher = new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Stfld &&
					((FieldInfo)i.operand == plasma || (FieldInfo)i.operand == missile)))
				.Repeat(matcher => {
					matcher
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Combat_Patch), nameof(ModDamageUp)))
					)
					.Advance(5);
				}
				);
			return codeMatcher.InstructionEnumeration();
		}

		static int ModDamageUp(int originValue, PlanetFactory factory)
		{
			if (MainManager.UpdatePeriod <= 1 || factory.index == MainManager.FocusFactoryIndex)
				return originValue;
			return originValue * MainManager.UpdatePeriod;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetATField), nameof(PlanetATField.TestRelayCondition))]
		static bool TestRelayCondition()
		{
			return Plugin.instance.EnableRelayLanding.Value;
			//return (___planet.id / 100) == MainManager.FocusStarIndex; // Old method: Do not land on remote star system
		}
	}
}
