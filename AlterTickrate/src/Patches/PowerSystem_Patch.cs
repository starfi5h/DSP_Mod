using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AlterTickrate.Patches
{
    public class PowerSystem_Patch
    {
        public static float PowerSystem_SpeedRate = 5.0f;
        public static PlanetFactory AnimOnlyFactory = null;
		const float DELTA_TIME = 0.016666668f; // num6 in PowerSystem.GameTick

		[HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static bool GameTick(PowerSystem __instance)
        {
            if (__instance.factory != AnimOnlyFactory)
                return true;

			var entityPool = __instance.factory.entityPool;
			var entityAnimPool = __instance.factory.entityAnimPool;			
			var genPool = __instance.genPool;

			// Update animation for generator
			for (int i = 1; i < __instance.genCursor; i++)
            {
				if (genPool[i].id == i)
                {
					var entityId = genPool[i].entityId;
					ref var animData = ref entityAnimPool[entityId];
					if (genPool[i].wind)
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
					else if (genPool[i].gamma)
                    {
						animData.time += DELTA_TIME;
						if (animData.time > 1f)
							animData.time -= 1f;
					}
					else if (genPool[i].fuelMask > 1)
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
					else if (genPool[i].geothermal)
                    {
						animData.Step(animData.state, DELTA_TIME, 2f, 0f);
					}
					else
                    {
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.7f);
					}
				}
            }

			// wireless charger for mecha & animation
			var mecha = GameMain.mainPlayer.mecha;
			for (int i = 1; i < __instance.nodeCursor; i++)
			{
				if (__instance.nodePool[i].id == i)
				{
					int entityId = __instance.nodePool[i].entityId;
					int networkId = __instance.nodePool[i].networkId;
					ref var animData = ref entityAnimPool[entityId];
					if (__instance.nodePool[i].isCharger)
					{
						int num62 = __instance.nodePool[i].requiredEnergy - __instance.nodePool[i].idleEnergyPerTick;
						float num63 = __instance.networkServes[networkId];
						if (__instance.nodePool[i].coverRadius < 15f)
						{
							animData.StepPoweredClamped(num63, DELTA_TIME, (num62 > 0) ? 2U : 1U);
						}
						else
						{
							animData.StepPoweredClamped2(num63, DELTA_TIME, (num62 > 0) ? 2U : 1U);
						}
						if (num62 > 0 && entityAnimPool[entityId].state == 2U)
						{
							num62 = (int)(num63 * num62);
							mecha.coreEnergy += num62;
							mecha.MarkEnergyChange(2, num62);
							mecha.AddChargerDevice(entityId);
							if (mecha.coreEnergy > mecha.coreEnergyCap)
							{
								mecha.coreEnergy = mecha.coreEnergyCap;
							}
						}
					}
					else if (entityPool[entityId].powerGenId == 0 && entityPool[entityId].powerAccId == 0 && entityPool[entityId].powerExcId == 0)
					{
						animData.Step2(animData.state, DELTA_TIME, animData.power, 0.4f);
					}
				}
			}

			return false;
        }
    }
}
