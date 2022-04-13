using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace MimicSimulation
{
    partial class FactoryData
    {
        ConcurrentBag<ProjectileData> dysonBag;
        ProjectileData[] dysonArray;
        readonly Queue<Tuple<long, ProjectileData[]>> dysonQueue = new Queue<Tuple<long, ProjectileData[]>>();
        int idleCount;

        const int BulletDelay = 180; //Eject Speed: 20/min => 3s => 180tick
        const int RocketDelay = 720; //Launch Speed: 5/min => 12s => 720tick

        public void AddDysonData(ProjectileData data)
        {
            if (dysonBag == null)
                dysonBag = new ConcurrentBag<ProjectileData>(); // may miss few data?
            dysonBag.Add(data);
        }

        public void DysonColletEnd()
        {
            // Collect sailbullet and dysonrocket lanuch in this tick
            dysonArray = null;
            if (dysonBag != null)
                dysonArray = dysonBag.ToArray();
            dysonBag = null;
            idleCount = 0;
        }

        public void DysonIdleTick()
        {
            // If pass the time, lanuch pending Projectile
            while (dysonQueue.Count > 0 && GameMain.gameTick <= dysonQueue.Peek().Item1)
            {
                //Log.Warn($"Current{GameMain.gameTick} Queue{dysonQueue.Peek().Item1} Len{dysonQueue.Peek().Item2.Length}");
                ProjectileData[] array = dysonQueue.Dequeue().Item2;
                foreach (var data in array)
                {
                    if (data.TargetId < 0)
                        Dyson_Patch.AddBullet(Factory.dysonSphere.swarm, data);
                    else
                        Dyson_Patch.AddRocket(Factory.dysonSphere, data);
                }
            }
            // Store to launch in future
            if (dysonArray != null)
            {
                idleCount++;
                dysonQueue.Enqueue(Tuple.Create(GameMain.gameTick + BulletDelay * idleCount, dysonArray));
                //Log.Info($"Current{GameMain.gameTick} Insert{GameMain.gameTick + BulletDelay * idleCount} Len{dysonArray.Length} Count{dysonQueue.Count}");
            }
        }
    }

    class Dyson_Patch
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        static IEnumerable<CodeInstruction> EjectorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false,new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SailBullet), nameof(SailBullet.lBegin))));
                CodeInstruction loadInstruction = matcher.InstructionAt(-1);
                matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.AddBullet)))
                    )
                    .Advance(2)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.planetId))),
                        loadInstruction,
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EjectorComponent), nameof(EjectorComponent.orbitId))),
                        HarmonyLib.Transpilers.EmitDelegate<Action<int, Vector3, int>>((planetId, localPos, orbitId) =>
                        {
                            if (FactoryPool.Planets.TryGetValue(planetId, out var factoryData))
                            {
                                ProjectileData data = new ProjectileData
                                {
                                    PlanetId = planetId,
                                    TargetId = -orbitId,
                                    LocalPos = localPos
                                };
                                factoryData.AddDysonData(data);
                            }
                        })
                    );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("EjectorComponent.InternalUpdate_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(SiloComponent), nameof(SiloComponent.InternalUpdate))]
        static IEnumerable<CodeInstruction> SiloComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Store projectile data after sphere.AddDysonRocket(dysonRocket, autoDysonNode) if IsUpdateNeeded == true
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonSphere), nameof(DysonSphere.AddDysonRocket))));
                CodeInstruction loadInstruction = matcher.InstructionAt(-1); //autoDysonNode
                matcher.Advance(2)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SiloComponent), nameof(SiloComponent.planetId))),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SiloComponent), nameof(SiloComponent.localPos))),
                        loadInstruction,
                        HarmonyLib.Transpilers.EmitDelegate<Action<int, Vector3, DysonNode>>((planetId, localPos, autoDysonNode) =>
                        {
                            if (FactoryPool.Planets.TryGetValue(planetId, out var factoryData))
                            {
                                // Assume layerId < 16, nodeId < 4096
                                ProjectileData data = new ProjectileData
                                {
                                    PlanetId = planetId,
                                    TargetId = (autoDysonNode.layerId << 12) | (autoDysonNode.id & 0x0FFF),
                                    LocalPos = localPos
                                };
                                factoryData.AddDysonData(data);
                            }
                        })
                    );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("SiloComponent.InternalUpdate_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        public static void AddBullet(DysonSwarm swarm, ProjectileData projectile)
        {
            ref AstroPose[] astroPoses = ref GameMain.data.galaxy.astroPoses;
            int orbitId = -projectile.TargetId;
            if (swarm.OrbitExist(orbitId))
            {
                VectorLF3 starPos = astroPoses[projectile.PlanetId / 100 * 100].uPos;
                SailBullet bullet = default;
                bullet.lBegin = projectile.LocalPos;
                bullet.uBegin = astroPoses[projectile.PlanetId].uPos + Maths.QRotateLF(astroPoses[projectile.PlanetId].uRot, projectile.LocalPos);
                bullet.uEnd = starPos + VectorLF3.Cross(swarm.orbits[orbitId].up, starPos - bullet.uBegin).normalized * swarm.orbits[orbitId].radius;
                bullet.maxt = (float)((bullet.uEnd - bullet.uBegin).magnitude / 4000.0);
                bullet.uEndVel = VectorLF3.Cross(bullet.uEnd - starPos, swarm.orbits[orbitId].up).normalized * Math.Sqrt(swarm.dysonSphere.gravity / swarm.orbits[orbitId].radius);
                swarm.AddBullet(bullet, orbitId);
            }
        }

        public static void AddRocket(DysonSphere sphere, ProjectileData projectile)
        {
            ref AstroPose[] astroPoses = ref GameMain.data.galaxy.astroPoses;
            // Assume layerId < 16, nodeId < 4096
            int layerId = projectile.TargetId >> 12;
            int nodeId = projectile.TargetId & 0x0FFF;
            DysonNode node = sphere.FindNode(layerId, nodeId);
            if (node != null)
            {
                DysonRocket rocket = default;
                rocket.planetId = projectile.PlanetId;
                rocket.uPos = astroPoses[projectile.PlanetId].uPos + Maths.QRotateLF(astroPoses[projectile.PlanetId].uRot, projectile.LocalPos + projectile.LocalPos.normalized * 6.1f);
                rocket.uRot = astroPoses[projectile.PlanetId].uRot * Maths.SphericalRotation(projectile.LocalPos, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                rocket.uVel = rocket.uRot * Vector3.forward;
                rocket.uSpeed = 0f;
                rocket.launch = projectile.LocalPos.normalized;
                sphere.AddDysonRocket(rocket, node);
            }
        }
    }

    public struct ProjectileData
    {
        public int PlanetId;
        public int TargetId; // >0:bullet <0:rocket
        public Vector3 LocalPos;
    }
}
