using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace DarkFogTweaks
{
    class Patch_Behavior
    {
		static ConfigEntry<bool> Relay_FillPitFirst;
		static ConfigEntry<bool> Relay_ToPlayerPlanetFirst;
		static ConfigEntry<int> Hive_AssaultUnitCount;
		static ConfigEntry<float> Hive_AssaultUnitFactor;
		static ConfigEntry<int> Base_AssaultUnitCount;
		static ConfigEntry<float> Base_AssaultUnitFactor;
		static ConfigEntry<float> Space_ThreatFactor;
		static ConfigEntry<float> All_ExpFactor;

		public static void LoadConfigs(ConfigFile configFile)
		{
			Base_AssaultUnitCount = configFile.Bind("Behavior", "Base_AssaultUnitCount", 0, "Send as many unit as this overwritten value when base assault (max: 180)"); //InitFormations: 108 + 72
			Base_AssaultUnitFactor = configFile.Bind("Behavior", "Base_AssaultUnitFactor", 1.0f, "Scale the original assaulting unit count");
			Hive_AssaultUnitCount = configFile.Bind("Behavior", "Hive_AssaultUnitCount", 0, "Send as many unit as this overwritten value when hive assault (max: 1440)");//InitFormations: 1440
			Hive_AssaultUnitFactor = configFile.Bind("Behavior", "Hive_AssaultUnitFactor", 1.0f, "Scale the original assaulting unit count");
			Relay_FillPitFirst = configFile.Bind("Behavior", "Relay_FillPitFirst", false, "Relay will try to land on base first");
			Relay_ToPlayerPlanetFirst = configFile.Bind("Behavior", "Relay_ToPlayerPlanetFirst", false, "Relay will try to land on player's local planet first");
			Space_ThreatFactor = configFile.Bind("Behavior", "Space_ThreatFactor", 1.0f, "Multiplier of threat increase when attacking space enemies");
			All_ExpFactor = configFile.Bind("Behavior", "All_ExpFactor", 1.0f, "Extra multiplier to exp gain");
		}

		[HarmonyPrefix, HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(DFGBaseComponent), nameof(DFGBaseComponent.LaunchAssault))]
		static void LaunchAssault_Prefix(ref int unitCount0, ref int unitCount1)
		{
			unitCount0 = (int)(unitCount0 * Base_AssaultUnitFactor.Value + 0.5f);
			unitCount1 = (int)(unitCount1 * Base_AssaultUnitFactor.Value + 0.5f);
			if (Base_AssaultUnitCount.Value != 0) unitCount0 = Base_AssaultUnitCount.Value;
			if (Base_AssaultUnitCount.Value != 0) unitCount1 = Base_AssaultUnitCount.Value;
		}

		[HarmonyPrefix, HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.LaunchLancerAssault))]
		static void LaunchLancerAssault_Prefix(ref int unitCount0)
        {
			unitCount0 = (int)(unitCount0 * Hive_AssaultUnitFactor.Value + 0.5f);
			if (Hive_AssaultUnitCount.Value != 0) unitCount0 = Hive_AssaultUnitCount.Value;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.CollectTempStates))]
		static void CollectTempStates_Postfix(SkillSystem __instance)
		{
			__instance.combatSettingsTmp.battleExpFactor *= All_ExpFactor.Value;
		}

		[HarmonyPrefix, HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.AddSpaceEnemyHatred), 
			new Type[] { typeof(EnemyDFHiveSystem), typeof(EnemyData), typeof(ETargetType), typeof(int), typeof(int), typeof(int) },
			new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
		static void AddSpaceEnemyHatred_Prefix(SkillSystem __instance)
		{
			__instance.combatSettingsTmp.battleThreatFactor = GameMain.history.combatSettings.battleThreatFactor * Space_ThreatFactor.Value;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.AddSpaceEnemyHatred),
	new Type[] { typeof(EnemyDFHiveSystem), typeof(EnemyData), typeof(ETargetType), typeof(int), typeof(int), typeof(int) },
	new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
		static void AddSpaceEnemyHatred_Postfix(SkillSystem __instance)
		{
			__instance.combatSettingsTmp.battleThreatFactor = GameMain.history.combatSettings.battleThreatFactor;
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last)]
		[HarmonyPatch(typeof(DFRelayComponent), nameof(DFRelayComponent.SearchTargetPlaceProcess))]
		static void SearchTargetPlaceProcess(DFRelayComponent __instance, ref bool __runOriginal)
		{
			if (!__runOriginal) return; // Do not patch if other mods have skip it (e.g. Nebula client)

			if (__instance.searchAstroId == 0)
			{
				StarData starData = __instance.hive.starData;
				int planetCount = starData.planetCount;
				for (int i = 0; i < planetCount; i++)
				{
					if (starData.planets[i].type != EPlanetType.Gas)
					{
						PlanetFactory factory = starData.planets[i].factory;
						if (factory == null) continue;

						if (Relay_FillPitFirst.Value)
						{
							EnemyDFGroundSystem enemySystem = factory.enemySystem;
							DFGBaseComponent[] buffer = enemySystem.bases.buffer;
							int cursor = enemySystem.bases.cursor;
							for (int k = 1; k < cursor; k++)
							{
								if (buffer[k] != null && buffer[k].id == k && buffer[k].relayId == 0 && buffer[k].hiveAstroId == 0)
								{
									__instance.searchBaseId = k;
									__instance.searchLPos = factory.enemyPool[buffer[k].enemyId].pos;
									__instance.searchLPos += __instance.searchLPos.normalized * 70f;
									buffer[k].hiveAstroId = __instance.hiveAstroId;
									__runOriginal = false;
									Plugin.Log.LogDebug("Relay_FillPitFirst " + starData.planets[i].displayName);
									return;
								}
							}
						}
						if (Relay_ToPlayerPlanetFirst.Value)
                        {
							if (starData.planets[i] == GameMain.localPlanet)
                            {
								__instance.searchAstroId = starData.planets[i].astroId;
								__instance.searchChance = 5;
								Plugin.Log.LogDebug("Relay_ToPlayerPlanetFirst " + starData.planets[i].displayName);
								return;
							}
                        }
					}
				}
			}
		}

	}
}
