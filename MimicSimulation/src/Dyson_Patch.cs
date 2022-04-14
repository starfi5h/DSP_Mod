using CircularBuffer;
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
        readonly ConcurrentBag<ProjectileData> dysonBag = new ConcurrentBag<ProjectileData>();
        readonly List<ProjectileData> dysonList = new List<ProjectileData>();
        CircularBuffer<ProjectileRecord> dysonBuffer;
        int idleCount;

        const int BulletDelay = 180; //Eject Speed: 20/min => 3s => 180tick
        const int RocketDelay = 360; //Launch Speed: 5/min => 12s => 720tick


        public void AddDysonData(ProjectileData data)
        {
            // Don't add if disable
            if (FactoryPool.MaxFactoryCount < GameMain.data.factoryCount)
                dysonBag.Add(data);
        }

        public void DysonColletEnd()
        {
            // Store projectile in buffer
            if (dysonList.Count > 0)
            {
                if (dysonBuffer == null)
                    dysonBuffer = new CircularBuffer<ProjectileRecord>(8);
                int capactiy = dysonBuffer.Capacity;
                while (capactiy < (dysonBuffer.Size + dysonList.Count))
                    capactiy *= 2;
                if (capactiy != dysonBuffer.Capacity)
                    dysonBuffer = new CircularBuffer<ProjectileRecord>(capactiy, dysonBuffer.ToArray());

                for (int index = 1; index <= idleCount; index++)
                {
                    //Log.Debug(FactoryPool.Ratio);
                    //Log.Info(((long)(RocketDelay * FactoryPool.Ratio * index) / (idleCount + 1)));
                    long scheduleTime = GameMain.gameTick + (long)(BulletDelay * FactoryPool.Ratio / (idleCount + 1));
                    //long scheduleTime = GameMain.gameTick + 30L;
                    for (int i = 0; i < dysonList.Count; i++)
                    {
                        dysonBuffer.PushBack(new ProjectileRecord { data = dysonList[i], time = scheduleTime });
                    }
                }
                //Log.Info($"buffersize: {dysonBuffer.Size}");
            }

            // Collect sailbullet and dysonrocket lanuch in this tick
            idleCount = 0;
            dysonList.Clear();
            while (!dysonBag.IsEmpty)
            {
                dysonBag.TryTake(out ProjectileData data);
                dysonList.Add(data);
            }
        }

        public void DysonIdleTick()
        {
            idleCount++;
            // Lanuch projectile in buffer when time is passed
            if (dysonBuffer != null)
            {
                while (dysonBuffer.Size > 0 && GameMain.gameTick <= dysonBuffer[0].time)
                {
                    //Log.Warn($"buffersize: {dysonBuffer.Size} {dysonBuffer[0].time}");
                    if (dysonBuffer[0].data.TargetId < 0)
                        Dyson_Patch.AddBullet(Factory.dysonSphere.swarm, dysonBuffer[0].data);
                    else
                        Dyson_Patch.AddRocket(Factory.dysonSphere, dysonBuffer[0].data);
                    dysonBuffer.PopFront();
                }
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
                if (swarm.dysonSphere.starData.displayName == "OsPegasi")
                    Log.Warn($"Add Rocket {swarm.dysonSphere.starData.displayName} time:{GameMain.gameTick}");
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
                if (sphere.starData.displayName == "OsPegasi")
                    Log.Warn($"Add Rocket {sphere.starData.displayName} time:{GameMain.gameTick}");
            }
        }
    }

    public struct ProjectileRecord
    {
        public long time;
        public ProjectileData data;
    }

    public struct ProjectileData
    {
        public int PlanetId;
        public int TargetId; // >0:bullet <0:rocket
        public Vector3 LocalPos;
    }
}
