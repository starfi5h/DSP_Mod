﻿using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SampleAndHoldSim
{
    partial class FactoryManager
    {
        public long EnergyReqCurrentTick { get; set; } // requested energy in this factory from ray receivers
        ConcurrentBag<ProjectileData> dysonBag;
        readonly List<ProjectileData> dysonList = new List<ProjectileData>();
        int idleCount;

        public void AddDysonData(ProjectileData data)
        {
            if (dysonBag == null)
                dysonBag = new ConcurrentBag<ProjectileData>(); // may miss few data?
            dysonBag.Add(data);
        }

        public void DysonColletEnd()
        {
            // Collect sailbullet and dysonrocket lanuch in this tick
            if (dysonBag != null)
            {
                idleCount = 0;
                dysonList.Clear();
                while (dysonBag.TryTake(out var data))
                    dysonList.Add(data);
            }
        }

        public void DysonIdleTick()
        {            
            if (factory.dysonSphere != null)
            {
                // Add EnergyReq in the last active tick
                factory.dysonSphere.energyReqCurrentTick += EnergyReqCurrentTick;
                // Lanuch projectiles in the last active tick
                if (dysonList.Count > 0)
                {
                    idleCount++;
                    foreach (var data in dysonList)
                    {
                        if (data.TargetId < 0)
                            Dyson_Patch.AddBullet(factory.dysonSphere.swarm, data, idleCount);
                        else
                            Dyson_Patch.AddRocket(factory.dysonSphere, data, idleCount);
                    }
                }
            }
        }
    }

    class Dyson_Patch
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.RequestDysonSpherePower))]
        static IEnumerable<CodeInstruction> RequestDysonSpherePower_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Record num in this.dysonSphere.energyReqCurrentTick += num;
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "energyReqCurrentTick"));
                CodeInstruction loadInstruction = matcher.InstructionAt(-2); //ldloc.3
                matcher.Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerSystem), nameof(PowerSystem.factory))),
                        loadInstruction,
                        HarmonyLib.Transpilers.EmitDelegate<Action<PlanetFactory, long>>((factory, energyReq) =>
                        {
                            if (factory.index < MainManager.Factories.Count)
                            {
                                MainManager.Factories[factory.index].EnergyReqCurrentTick = energyReq;
                            }
                        })
                    );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("PowerSystem.RequestDysonSpherePower Transpiler failed.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        public static IEnumerable<CodeInstruction> EjectorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SailBullet), nameof(SailBullet.lBegin))));
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
                            if (MainManager.Planets.TryGetValue(planetId, out var factoryData) && factoryData.IsNextIdle)
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
                Log.Warn("EjectorComponent.InternalUpdate Transpiler failed.");
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
                            if (MainManager.Planets.TryGetValue(planetId, out var factoryData) && factoryData.IsNextIdle)
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
                Log.Warn("SiloComponent.InternalUpdate Transpiler failed.");
                return instructions;
            }
        }

        public static void AddBullet(DysonSwarm swarm, ProjectileData projectile, int idleCount)
        {
            ref AstroData[] astroDatas = ref GameMain.data.galaxy.astrosData;
            int orbitId = -projectile.TargetId;
            if (swarm.OrbitExist(orbitId))
            {
                VectorLF3 starPos = astroDatas[projectile.PlanetId / 100 * 100].uPos;
                SailBullet bullet = default;
                bullet.lBegin = projectile.LocalPos;
                bullet.uBegin = astroDatas[projectile.PlanetId].uPos + Maths.QRotateLF(astroDatas[projectile.PlanetId].uRot, projectile.LocalPos);
                bullet.uEnd = starPos + VectorLF3.Cross(swarm.orbits[orbitId].up, starPos - bullet.uBegin).normalized * swarm.orbits[orbitId].radius;
                bullet.maxt = (float)((bullet.uEnd - bullet.uBegin).magnitude / 4000.0);
                bullet.uEndVel = VectorLF3.Cross(bullet.uEnd - starPos, swarm.orbits[orbitId].up).normalized * Math.Sqrt(swarm.dysonSphere.gravity / swarm.orbits[orbitId].radius);
                swarm.AddBullet(bullet, orbitId);
            }
        }

        public static void AddRocket(DysonSphere sphere, ProjectileData projectile, int idleCount)
        {
            ref AstroData[] astroDatas = ref GameMain.data.galaxy.astrosData;
            // Assume layerId < 16, nodeId < 4096
            int layerId = projectile.TargetId >> 12;
            int nodeId = projectile.TargetId & 0x0FFF;
            DysonNode node = sphere.FindNode(layerId, nodeId);
            if (node != null)
            {
                DysonRocket rocket = default;
                rocket.planetId = projectile.PlanetId;
                rocket.uPos = astroDatas[projectile.PlanetId].uPos + Maths.QRotateLF(astroDatas[projectile.PlanetId].uRot, projectile.LocalPos + projectile.LocalPos.normalized * 6.1f);
                rocket.uRot = astroDatas[projectile.PlanetId].uRot * Maths.SphericalRotation(projectile.LocalPos, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                rocket.uVel = rocket.uRot * Vector3.forward;
                rocket.uSpeed = 0f;
                rocket.launch = projectile.LocalPos.normalized;
                rocket.uPos -= new VectorLF3(rocket.uVel) * 15 * idleCount; // move starting position toward plaent to delay
                if ((node._spReq - node.spOrdered) > 0)
                {
                    // if node is not full, add to original node
                    sphere.AddDysonRocket(rocket, node);
                }
                else if (sphere.GetAutoNodeCount() > 0)
                {
                    // if node is full, find another node waiting to build
                    if ((node = sphere.GetAutoDysonNode(nodeId)) != null) 
                        sphere.AddDysonRocket(rocket, node);
                }
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
