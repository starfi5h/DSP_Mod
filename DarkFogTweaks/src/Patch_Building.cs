using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace DarkFogTweaks
{
    public class Patch_Building
    {
		static ConfigEntry<int> BuildingSpeedFactor;
		static ConfigEntry<int> BaseMatterGen;
		static ConfigEntry<int> BaseEnergyGen;
		static ConfigEntry<int> HiveMatterGen;
		static ConfigEntry<int> HiveEnergyGen;

		public static void LoadConfigs(ConfigFile configFile)
		{
			BuildingSpeedFactor = configFile.Bind("Buildings", "BuildingSpeedFactor", 1, "Increase building speed and reduce cost");
			BaseMatterGen = configFile.Bind("Buildings", "BaseExtraMatterGen", 0, "vanilla: +180");
			BaseEnergyGen = configFile.Bind("Buildings", "BaseExtraEnergyGen", 0, "vanilla: -5400 (5.4MW)");
			HiveMatterGen = configFile.Bind("Buildings", "HiveExtraMatterGen", 0, "vanilla: +0");
			HiveEnergyGen = configFile.Bind("Buildings", "HiveExtraEnergyGen", 0, "vanilla: +480000 (480MW)");
		}

		[HarmonyPrefix, HarmonyPriority(Priority.First)]
		[HarmonyPatch(typeof(EnemyBuilderComponent), nameof(EnemyBuilderComponent.LogicTick))]
		static bool EnemyBuilderComponent_LogicTick_Prefix(ref EnemyBuilderComponent __instance)
		{
			if (__instance.sp >= __instance.spMax) return true;

			__instance.state = 0;
			if (__instance.energy >= __instance.spEnergy && __instance.matter >= __instance.spMatter)
			{
				__instance.sp += BuildingSpeedFactor.Value;
				__instance.energy -= __instance.spEnergy;
				__instance.matter -= __instance.spMatter;
				if (__instance.sp >= __instance.spMax)
				{
					__instance.sp = __instance.spMax;
					__instance.buildCDTime = 0;
				}
			}
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DFGBaseComponent), nameof(DFGBaseComponent.LogicTick))]
		static void DFGBaseComponent_LogicTick_Prefix(DFGBaseComponent __instance, ref EnemyBuilderComponent builder)
		{
			if (builder.state > 0)
            {
				builder.matter +=  BaseMatterGen.Value;
				builder.matter = builder.matter < builder.maxMatter ? builder.matter : builder.maxMatter;
				builder.energy += BaseEnergyGen.Value;
				builder.energy = builder.energy < builder.maxEnergy ? builder.energy : builder.maxEnergy;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DFSCoreComponent), nameof(DFSCoreComponent.LogicTick))]
		static void DFSCoreComponent_LogicTick_Prefix(ref DFSCoreComponent __instance, EnemyDFHiveSystem hive)
		{
			if (hive == null) return;
			ref var builder = ref hive.builders.buffer[__instance.builderId];

			if (builder.state > 0)
			{
				builder.matter += HiveMatterGen.Value;
				builder.matter = builder.matter < builder.maxMatter ? builder.matter : builder.maxMatter;
				builder.energy += HiveEnergyGen.Value;
				builder.energy = builder.energy < builder.maxEnergy ? builder.energy : builder.maxEnergy;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EnemyBriefInfo), nameof(EnemyBriefInfo.SetBriefInfo))]
		static void DFSCoreComponent_LogicTick_Prefix(EnemyBriefInfo __instance, SpaceSector _sector)
		{
			if (_sector == null || __instance.enemyId <= 0) return;

			if (__instance.astroId > 1000000)
			{
				ref EnemyData ptr = ref _sector.enemyPool[__instance.enemyId];
				if (ptr.id != __instance.enemyId)
				{
					return;
				}
				if (ptr.dfSCoreId > 0)
				{
					__instance.matterChange += HiveMatterGen.Value;
					__instance.energyChange += HiveEnergyGen.Value;
				}
			}
			else if (__instance.astroId > 100 && __instance.astroId <= 204899 && __instance.astroId % 100 > 0)
			{
				var planetFactory = _sector.skillSystem.astroFactories[__instance.astroId];
				if (planetFactory == null)
				{
					return;
				}
				ref EnemyData ptr = ref planetFactory.enemyPool[__instance.enemyId];
				if (ptr.id != __instance.enemyId)
				{
					return;
				}
				if (ptr.dfGBaseId > 0)
				{
					__instance.matterChange += BaseMatterGen.Value;
					__instance.energyChange += BaseEnergyGen.Value;
				}
			}
		}
	}
}
