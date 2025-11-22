using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
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
