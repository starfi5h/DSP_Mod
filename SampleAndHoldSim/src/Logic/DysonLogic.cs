using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace SampleAndHoldSim
{
    public struct ProjectileData
    {
        public int PlanetId;
        public int TargetId; // >0:bullet <0:rocket
        public Vector3 LocalPos;
    }

    partial class FactoryManager
    {
        public long EnergyReqCurrentTick; // requested energy in this factory from ray receivers
        ConcurrentBag<ProjectileData> dysonBag;
        readonly List<ProjectileData> dysonList = new List<ProjectileData>();
        int idleCount;

        public void AddDysonData(int planetId, int targetId, in Vector3 localPos)
        {
            if (dysonBag == null)
                dysonBag = new ConcurrentBag<ProjectileData>(); // may miss few data?
            dysonBag.Add(new ProjectileData() { PlanetId = planetId, TargetId = targetId, LocalPos = localPos });
        }

        public void DysonBeforeTick()
        {
            EnergyReqCurrentTick = 0;
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
                            Dyson_Patch.AddBullet(factory.dysonSphere.swarm, in data);
                        else
                            Dyson_Patch.AddRocket(factory.dysonSphere, in data, idleCount);
                    }
                }
            }
        }
    }

    class Ejector_Patch // Separate to compat with CheatEnabler and GenesisBook
    {
        [HarmonyTranspiler, HarmonyPatch(typeof(EjectorComponent), nameof(EjectorComponent.InternalUpdate))]
        public static IEnumerable<CodeInstruction> EjectorComponent_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SailBullet), nameof(SailBullet.lBegin)))
                        );
                if (matcher.IsInvalid)
                {
                    Log.Warn("EjectorComponent_Transpiler: Can't find SailBullet.lBegin");
                    return instructions;
                }
                CodeInstruction loadInstruction = matcher.Instruction;

                matcher.MatchForward(false,
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.AddBullet))),
                        new CodeMatch(OpCodes.Pop));
                if (matcher.IsInvalid)
                {
                    Log.Warn("EjectorComponent_Transpiler: Can't find AddBullet");
                    return instructions;
                }

                matcher.Advance(1)
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
                                factoryData.AddDysonData(planetId, -orbitId, localPos);
                            }
                        })
                    );
                return matcher.InstructionEnumeration();
            }
            catch (Exception ex)
            {
                Log.Warn("EjectorComponent_Transpiler error!");
                Log.Warn(ex);
                return instructions;
            }
        }
    }

    class Dyson_Patch
    {
        internal static void AddEnergyReqCurrentTick(PlanetFactory factory, long value)
        {
            if (MainManager.TryGet(factory.index, out var manager))
            {
                Interlocked.Add(ref manager.EnergyReqCurrentTick, value);
            }
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.RequestDysonSpherePower))]
        static IEnumerable<CodeInstruction> RequestDysonSpherePower_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Record num in this.dysonSphere.energyReqCurrentTick += num;
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .End()
                    .MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == "energyReqCurrentTick"));
                CodeInstruction loadInstruction = matcher.InstructionAt(-2); //ldloc.3
                matcher.Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PowerSystem), nameof(PowerSystem.factory))),
                        loadInstruction,
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dyson_Patch), nameof(AddEnergyReqCurrentTick)))
                    );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("PowerSystem.RequestDysonSpherePower Transpiler failed.");
                return instructions;
            }
        }

        [HarmonyTranspiler, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic._power_gen_gamma_parallel))]
        static IEnumerable<CodeInstruction> _power_gen_gamma_parallel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // 多線程
                var matcher = new CodeMatcher(instructions);

                // 任務1: 擷取PlanetFactory planetFactory = this.factories[batchCurrent];                
                matcher.MatchForward(true,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "factories"),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldelem_Ref),
                        new CodeMatch(OpCodes.Stloc_S)
                    );
                var facotryInstruction = new CodeInstruction(OpCodes.Ldloc_S, matcher.Operand);

                // 任務2: 在Interlocked.Add(ref dysonSphere.energyReqCurrentTick, num4);
                //        之後加上AddEnergyReqCurrentTick(planetFactory, num4);
                matcher.End()
                    .MatchBack(true, 
                        new CodeMatch(i => i.opcode == OpCodes.Ldflda && ((FieldInfo)i.operand).Name == "energyReqCurrentTick"),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Add")                        
                    );
                var loadInstruction = matcher.InstructionAt(-1); //value
                matcher.Advance(2)
                    .InsertAndAdvance(
                        facotryInstruction,
                        loadInstruction,
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dyson_Patch), nameof(AddEnergyReqCurrentTick)))
                    );

                return matcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("Transpiler GameLogic._power_gen_gamma_parallel Transpiler failed.");
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
                                factoryData.AddDysonData(planetId, (autoDysonNode.layerId << 12) | (autoDysonNode.id & 0x0FFF), localPos);
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

        public static void AddBullet(DysonSwarm swarm, in ProjectileData projectile)
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

        public static void AddRocket(DysonSphere sphere, in ProjectileData projectile, int idleCount)
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
}
