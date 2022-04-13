/*
using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace MimicSimulation
{
    class DysonBulletData
    {
        ConcurrentBag<BulletData> bag;
        static readonly ConcurrentDictionary<int, Dictionary<int, BulletData>> factroyDict = new ConcurrentDictionary<int, Dictionary<int, StationData>>();











        [HarmonyTranspiler]
        [HarmonyPatch(nameof(EjectorComponent.InternalUpdate))]
        private static IEnumerable<CodeInstruction> InternalUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(SailBullet), nameof(SailBullet.lBegin)))
                    );
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
                            if (GameData_Patch.ActivePlanets(planetId))
                            BulletData data = new BulletData
                            {
                                PlanetId = planetId,
                                TargetId = orbitId,
                                LocalPos = localPos
                            };
                            bag.Add(data);
                        })
                    );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                NebulaModel.Logger.Log.Error("EjectorComponent.InternalUpdate_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }


    }







    struct BulletData
    {
        public int PlanetId;
        public int TargetId;
        public Vector3 LocalPos;
    }
}
*/