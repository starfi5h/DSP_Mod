using HarmonyLib;
using UnityEngine;

namespace RailgunsRetargetMini
{
    public class Patch
    {
        static int checkIndex;

        [HarmonyPrefix, HarmonyPatch(typeof(GameData), "GameTick")]
        public static void GameData_GameTick()
        {
            checkIndex = (++checkIndex >= Configs.CheckPeriod) ? 0 : checkIndex;
        }        

        [HarmonyPostfix, HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        public static void EjectorComponent_InternalUpdate_Postfix(ref EjectorComponent __instance, DysonSwarm swarm, AstroData[] astroPoses)
        {
            // check every Configs.CheckPeriod ticks
            if (__instance.id % Configs.CheckPeriod != checkIndex || __instance.bulletCount == 0) return;
            if (__instance.targetState == EjectorComponent.ETargetState.OK || __instance.targetState == EjectorComponent.ETargetState.None) return;

            // Find a reachable orbit from all other enabled orbits
            int originalId = __instance.orbitId;

            // Precalculate common parts
            int starId = __instance.planetId / 100 * 100;
            float num5 = __instance.localAlt + __instance.pivotY + (__instance.muzzleY - __instance.pivotY) / Mathf.Max(0.1f, Mathf.Sqrt(1f - __instance.localDir.y * __instance.localDir.y));
            Vector3 vector = new(__instance.localPosN.x * num5, __instance.localPosN.y * num5, __instance.localPosN.z * num5);
            VectorLF3 vectorLF = astroPoses[__instance.planetId].uPos + Maths.QRotateLF(astroPoses[__instance.planetId].uRot, vector);
            Quaternion q = astroPoses[__instance.planetId].uRot * __instance.localRot;
            VectorLF3 b = astroPoses[starId].uPos - vectorLF;
            for (int i = 1; i < swarm.orbitCursor; i++)
            {
                if (swarm.orbits[i].id == i && swarm.orbits[i].enabled && i != originalId)
                {
                    // Calculate the parts related to swarm obrit
                    __instance.orbitId = i;
                    if (IsReachable(in __instance, swarm, starId, astroPoses, in vectorLF, in q, in b))
                    {
                        __instance.SetOrbit(i);
                        return;
                    }
                }
            }

            // Can't find retarget orbit, reset to original orbitId;
            __instance.orbitId = originalId;
        }

        public static bool IsReachable(in EjectorComponent ejector, DysonSwarm swarm, int starId, AstroData[] astroPoses, in VectorLF3 vectorLF, in Quaternion q, in VectorLF3 b)
        {
            VectorLF3 vectorLF2 = astroPoses[starId].uPos + VectorLF3.Cross(swarm.orbits[ejector.orbitId].up, b).normalized * swarm.orbits[ejector.orbitId].radius;
            VectorLF3 vectorLF3 = vectorLF2 - vectorLF;
            double targetDist = vectorLF3.magnitude;
            vectorLF3.x /= targetDist;
            vectorLF3.y /= targetDist;
            vectorLF3.z /= targetDist;
            Vector3 vector2 = Maths.QInvRotate(q, vectorLF3);
            if (vector2.y < 0.08715574 || vector2.y > 0.8660254f)
            {
                // ETargetState.AngleLimit
                return false;
            }			

            for (int i = starId + 1; i <= ejector.planetId + 2; i++)
            {
                if (i != ejector.planetId)
                {
                    double num6 = astroPoses[i].uRadius;
                    if (num6 > 1.0)
                    {
                        VectorLF3 vectorLF4 = astroPoses[i].uPos - vectorLF;
                        double num7 = vectorLF4.x * vectorLF4.x + vectorLF4.y * vectorLF4.y + vectorLF4.z * vectorLF4.z;
                        double num8 = vectorLF4.x * vectorLF3.x + vectorLF4.y * vectorLF3.y + vectorLF4.z * vectorLF3.z;
                        if (num8 > 0.0)
                        {
                            double num9 = num7 - num8 * num8;
                            num6 += 120.0;
                            if (num9 < num6 * num6)
                            {
                                // ETargetState.Blocked
                                return false;
                            }
                        }
                    }
                }
            }

            //Log.Debug($"Retarget to {ejector.orbitId}: {vector2.y}");
            return true;			
        }
    }
}
