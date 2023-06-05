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

			if (labPool[__instance.nextLabId].id == 0 || labPool[__instance.nextLabId].id != __instance.nextLabId)
			{
				__instance.nextLabId = 0;
			}
			if (labPool[__instance.nextLabId].needs != null && __instance.recipeId == labPool[__instance.nextLabId].recipeId && __instance.techId == labPool[__instance.nextLabId].techId)
			{
				if (__instance.matrixServed != null && labPool[__instance.nextLabId].matrixServed != null)
				{
					int[] array = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? __instance.matrixServed : labPool[__instance.nextLabId].matrixServed;
					int[] array2 = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? labPool[__instance.nextLabId].matrixServed : __instance.matrixServed;
					int[] obj = array;
					lock (obj)
					{
						int[] obj2 = array2;
						lock (obj2)
						{
							// Research mode: Move half of matrix to the upper lab if item count >= 2
							int matrixCount, matrixInc;
							if (__instance.needs[0] == 0 && labPool[__instance.nextLabId].needs[0] == 6001 && __instance.matrixServed[0] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[0] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[0], ref __instance.matrixIncServed[0], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[0] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[0] += matrixCount;
							}
							if (__instance.needs[1] == 0 && labPool[__instance.nextLabId].needs[1] == 6002 && __instance.matrixServed[1] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[1] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[1], ref __instance.matrixIncServed[1], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[1] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[1] += matrixCount;
							}
							if (__instance.needs[2] == 0 && labPool[__instance.nextLabId].needs[2] == 6003 && __instance.matrixServed[2] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[2] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[2], ref __instance.matrixIncServed[2], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[2] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[2] += matrixCount;
							}
							if (__instance.needs[3] == 0 && labPool[__instance.nextLabId].needs[3] == 6004 && __instance.matrixServed[3] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[3] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[3], ref __instance.matrixIncServed[3], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[3] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[3] += matrixCount;
							}
							if (__instance.needs[4] == 0 && labPool[__instance.nextLabId].needs[4] == 6005 && __instance.matrixServed[4] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[4] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[4], ref __instance.matrixIncServed[4], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[4] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[4] += matrixCount;
							}
							if (__instance.needs[5] == 0 && labPool[__instance.nextLabId].needs[5] == 6006 && __instance.matrixServed[5] >= 7200)
							{
								matrixCount = 3600 * (__instance.matrixServed[5] / 7200);
								matrixInc = __instance.split_inc(ref __instance.matrixServed[5], ref __instance.matrixIncServed[5], matrixCount);
								labPool[__instance.nextLabId].matrixIncServed[5] += matrixInc;
								labPool[__instance.nextLabId].matrixServed[5] += matrixCount;
							}
							__instance.UpdateNeedsResearch();
							labPool[__instance.nextLabId].UpdateNeedsResearch();
							goto END;
						}
					}
				}
				if (__instance.served != null && labPool[__instance.nextLabId].served != null)
				{
					int[] array3 = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? __instance.served : labPool[__instance.nextLabId].served;
					int[] array4 = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? labPool[__instance.nextLabId].served : __instance.served;
					int[] obj = array3;
					lock (obj)
					{
						int[] obj2 = array4;
						lock (obj2)
						{
							int len = __instance.served.Length;
							for (int i = 0; i < len; i++)
							{
								if (__instance.needs[i] == 0 && labPool[__instance.nextLabId].needs[i] == __instance.requires[i] && __instance.served[i] >= 2)
								{
									// Produce mode: Move at most LabLiftUpdatePeriod extra input material to uppper lab
									int count = __instance.served[i] / 2;
									int inc = ( count * __instance.incServed[i]) / __instance.served[i];
									__instance.served[i] -= count;
									__instance.incServed[i] -= inc;
									labPool[__instance.nextLabId].served[i] += count;
									labPool[__instance.nextLabId].incServed[i] += inc;
								}
							}
							__instance.UpdateNeedsAssemble();
							labPool[__instance.nextLabId].UpdateNeedsAssemble();
						}
					}
				}

			END:
				if (__instance.produced != null && labPool[__instance.nextLabId].produced != null)
				{
					int[] array5 = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? __instance.produced : labPool[__instance.nextLabId].produced;
					int[] array6 = (__instance.entityId > labPool[__instance.nextLabId].entityId) ? labPool[__instance.nextLabId].produced : __instance.produced;
					int[] obj = array5;
					lock (obj)
					{
						int[] obj2 = array6;
						lock (obj2)
						{
							if (__instance.produced[0] < 10 && labPool[__instance.nextLabId].produced[0] > 0)
							{
								// Produce mode: Move at most LabLiftUpdatePeriod extra output maxtrix to lower lab
								int count = labPool[__instance.nextLabId].produced[0] > Parameters.LabLiftUpdatePeriod ? Parameters.LabLiftUpdatePeriod : labPool[__instance.nextLabId].produced[0];
								__instance.produced[0] += count;
								labPool[__instance.nextLabId].produced[0] -= count;
							}
						}
					}
				}
			}

			return false;
		}
    }
}
