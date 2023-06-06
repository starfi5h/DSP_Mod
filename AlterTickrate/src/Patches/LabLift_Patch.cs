using HarmonyLib;

namespace AlterTickrate.Patches
{
    public class LabLift_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateOutputToNext))]
        static bool IncreaseLiftThroughput(ref LabComponent __instance, LabComponent[] labPool)
        {
			if (!Plugin.Enable) return true;

			ref LabComponent nextLab = ref labPool[__instance.nextLabId];
			if (nextLab.id == 0 || nextLab.id != __instance.nextLabId)
			{
				__instance.nextLabId = 0;
				return false;
			}
			if (nextLab.needs != null && __instance.recipeId == nextLab.recipeId && __instance.techId == nextLab.techId)
			{
				if (__instance.matrixServed != null && nextLab.matrixServed != null)
				{
					int[] array = (__instance.entityId > nextLab.entityId) ? __instance.matrixServed : nextLab.matrixServed;
					int[] array2 = (__instance.entityId > nextLab.entityId) ? nextLab.matrixServed : __instance.matrixServed;
					int[] obj = array;
					lock (obj)
					{
						int[] obj2 = array2;
						lock (obj2)
						{
							// Research mode: Move half of matrix to the upper lab if item count >= 10
							int matrixCount, matrixInc;
							for (int i = 0; i < 6; i++)
							{
								if (__instance.matrixServed[i] >= 36000 && nextLab.needs[i] == (6001 + i))
								{
									matrixCount = 3600 * (__instance.matrixServed[i] / 7200);
									matrixInc = __instance.split_inc(ref __instance.matrixServed[i], ref __instance.matrixIncServed[i], matrixCount);									
									nextLab.matrixIncServed[i] += matrixInc;
									nextLab.matrixServed[i] += matrixCount;

									// UpdateNeedsResearch
									__instance.needs[i] = __instance.matrixServed[i] < 36000 ? (6001 + i): 0;
									nextLab.needs[0] = nextLab.matrixServed[i] < 36000 ? (6001 + i) : 0;
								}
							}
							goto END;
						}
					}
				}
				if (__instance.served != null && nextLab.served != null)
				{
					int[] array3 = (__instance.entityId > nextLab.entityId) ? __instance.served : nextLab.served;
					int[] array4 = (__instance.entityId > nextLab.entityId) ? nextLab.served : __instance.served;
					int[] obj = array3;
					lock (obj)
					{
						int[] obj2 = array4;
						lock (obj2)
						{
							int len = __instance.served.Length;
							for (int i = 0; i < len; i++)
							{
								if (__instance.needs[i] == 0 && nextLab.needs[i] == __instance.requires[i] && __instance.served[i] >= 1)
								{
									// Produce mode: Move at most LabLiftUpdatePeriod extra input material to uppper lab
									int count = __instance.served[i] / 2 + 1;
									int inc = ( count * __instance.incServed[i]) / __instance.served[i];
									__instance.served[i] -= count;
									__instance.incServed[i] -= inc;
									nextLab.served[i] += count;
									nextLab.incServed[i] += inc;

									// UpdateNeedsAssemble
									__instance.needs[i] =  __instance.served[i] < 4 ? __instance.requires[i] : 0;
									nextLab.needs[i] = nextLab.served[i] < 4 ? nextLab.requires[i] : 0;
								}
							}
						}
					}
				}

			END:
				if (__instance.produced != null && nextLab.produced != null)
				{
					int[] array5 = (__instance.entityId > nextLab.entityId) ? __instance.produced : nextLab.produced;
					int[] array6 = (__instance.entityId > nextLab.entityId) ? nextLab.produced : __instance.produced;
					int[] obj = array5;
					lock (obj)
					{
						int[] obj2 = array6;
						lock (obj2)
						{
							if (__instance.produced[0] < 9 && nextLab.produced[0] > 0)
							{
								// Produce mode: Move as most as it can from upper lab until product count reach 9
								int count = 9 - __instance.produced[0];
								count = nextLab.produced[0] < count ? nextLab.produced[0] : count;
								__instance.produced[0] += count;
								nextLab.produced[0] -= count;
							}
						}
					}
				}
			}

			return false;
		}
    }
}
