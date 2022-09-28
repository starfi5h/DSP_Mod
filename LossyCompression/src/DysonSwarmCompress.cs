using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace LossyCompression
{
    class DysonSwarmCompress
    {
        // Store the overall sail life distribution and total absorbing sails count in .moddsv
        // 4*(DIVISION) bytes for whole sails

        public static bool Enable { get; set; }
        public static readonly int EncodedVersion = 1; //PeekChar() max is 127

        private const int DIVISION = 350; // default = 70

        public static void Export(BinaryWriter w)
		{
			if (!Enable)
			{
				w.Write(0);
				return;
			}

            var stopWatch = new HighStopwatch();
            stopWatch.Begin();
            long datalen = -w.BaseStream.Length;

            w.Write(EncodedVersion);
            w.Write(GameMain.data.dysonSpheres.Length);
            for (int starIndex = 0; starIndex < GameMain.data.dysonSpheres.Length; starIndex++)
            {
                if (GameMain.data.dysonSpheres[starIndex] != null)
                {
                    w.Write(starIndex);
                    Encode(GameMain.data.dysonSpheres[starIndex], w);
                }
                else
                    w.Write(-1);
            }

            datalen += w.BaseStream.Length;
            PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonSphere] += datalen;
            PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonSwarm] += datalen;

            Log.Info($"Compress DysonSwarm: {datalen:N0} bytes {stopWatch.duration} s");
        }

        public static void Import(BinaryReader r)
        {
            int version = r.ReadInt32();
            if (version == EncodedVersion)
            {
                long datalen = -r.BaseStream.Length;
                var stopWatch = new HighStopwatch();
                stopWatch.Begin();

                int dysonSpheresLength = r.ReadInt32();
                Assert.True(dysonSpheresLength == GameMain.data.dysonSpheres.Length);
                for (int j = 0; j < dysonSpheresLength; j++)
                {
                    int starIndex = r.ReadInt32();
                    if (starIndex != -1)
                    {
                        Decode(GameMain.data.dysonSpheres[starIndex], r);
                    }
                }

                PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonSphere] += datalen;
                PerformanceMonitor.dataLengths[(int)ESaveDataEntry.DysonShell] += datalen;
                Log.Info($"Decompress DysonSwarm: {stopWatch.duration}s");
            }
        }

        public static void Encode(DysonSphere dysonSphere, BinaryWriter w)
		{
            // Modify from CalculateSailLifeDistribution
            ExpiryOrder[] expiryOrder = dysonSphere.swarm.expiryOrder;
            long step = (long)(GameMain.history.solarSailLife * 60 / DIVISION);
            long gameTick = GameMain.gameTick;
            int[] array = new int[DIVISION + 1];
            for (int i = 0; i < expiryOrder.Length; i++)
            {
                if ((expiryOrder[i].index != 0 || expiryOrder[i].time != 0L))
                {
                    int index = (int)((expiryOrder[i].time - gameTick) / step);
                    if (index > DIVISION)
                        index = DIVISION;
                    else if (index < 0)
                        index = 0;
                    array[index] += 1;
                }
            }

            // Sum absorbing sails
            int absorbingSailCount = dysonSphere.swarm.absorbEnding - dysonSphere.swarm.absorbCursor;
            if (absorbingSailCount < 0)
                absorbingSailCount += dysonSphere.swarm.sailCapacity;

            // Export binary data
            w.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
                w.Write(array[i]);
            w.Write(absorbingSailCount);
        }

        public static void Decode(DysonSphere dysonSphere, BinaryReader r)
        {
            DysonSwarm dysonSwarm = dysonSphere.swarm;

            // Import binary data
            long totalSailCount = 0;
            int[] array = new int[r.ReadInt32() + 1];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = r.ReadInt32();
                totalSailCount += array[i];
            }
            Log.Debug($"[{dysonSphere.starData.index ,2}] sail count: {totalSailCount}");
            if (totalSailCount == 0)
                return;

            RemoveSailsByOrbit_NoSync(dysonSwarm , - 1); // Clean all sails
            int newCap = 512;
            while (newCap <= totalSailCount)
                newCap *= 2;
            dysonSwarm.SetSailCapacity(newCap);

            // Modify from DysonSwarm.AutoConstruct
            List<int> activeOrbitIds = new List<int>();
            for (int i = 1; i < dysonSwarm.orbitCursor; i++)
            {
                if (dysonSwarm.orbits[i].id == i && dysonSwarm.orbits[i].enabled)
                    activeOrbitIds.Add(i);
            }
            if (activeOrbitIds.Count == 0)
            {
                // Orbit 1 cannot be deleted
                activeOrbitIds.Add(1);
            }

            // Split sails evenly across all active orbits
            int sailCount = 0;
            long maxSailLife = (long)(GameMain.history.solarSailLife * 60);
            long step = (long)(GameMain.history.solarSailLife * 60 / DIVISION);
            float gravity = dysonSwarm.dysonSphere.gravity;
            long time = GameMain.gameTick;
            for (int col = 0; col < array.Length; col++)
            {
                long life = step * (col + 1);
                if (life > maxSailLife)
                {
                    life = maxSailLife;
                }

                for (int k = 0; k < array[col]; k++)
                {
                    int orbitId = activeOrbitIds[sailCount++ % activeOrbitIds.Count];
                    ref SailOrbit obrit = ref dysonSwarm.orbits[orbitId];

                    DysonSail ss = default;
                    VectorLF3 vectorLF = VectorLF3.Cross(obrit.up, RandomTable.SphericNormal(ref dysonSwarm.autoConstructSeed, 1.0)).normalized * obrit.radius;
                    vectorLF += RandomTable.SphericNormal(ref dysonSwarm.randSeed, 200.0); // original: 200.0
                    ss.px = (float)vectorLF.x;
                    ss.py = (float)vectorLF.y;
                    ss.pz = (float)vectorLF.z;
                    vectorLF = VectorLF3.Cross(vectorLF, obrit.up).normalized * Math.Sqrt(gravity / obrit.radius);
                    vectorLF += RandomTable.SphericNormal(ref dysonSwarm.randSeed, 0.6000000238418579);
                    vectorLF += RandomTable.SphericNormal(ref dysonSwarm.randSeed, 0.5);
                    ss.vx = (float)vectorLF.x;
                    ss.vy = (float)vectorLF.y;
                    ss.vz = (float)vectorLF.z;
                    ss.gs = 1f;

                    // TODO: Use AddSolarSail_NoGPU to speed up creation process
                    dysonSwarm.AddSolarSail(ss, orbitId, time + life);
                }
            }
        }

#pragma warning disable CS8321, IDE0060

        [HarmonyPrefix, HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.Export))]
        public static bool DysonSwarm_Export_Prefix(DysonSwarm __instance, BinaryWriter w)
        {
            if (!Enable) return true;

            int sailCapacity, sailCursor, sailRecycleCursor;
            int expiryCursor, expiryEnding, absorbCursor, absorbEnding;
			ExpiryOrder[] expiryOrder;
			AbsorbOrder[] absorbOrder;

			sailCapacity = __instance.sailCapacity;
            sailCursor = __instance.sailCursor;
            sailRecycleCursor = __instance.sailRecycleCursor;
            expiryCursor = __instance.expiryCursor;
            expiryEnding = __instance.expiryEnding;
            absorbCursor = __instance.absorbCursor;
            absorbEnding = __instance.absorbEnding;
			expiryOrder = __instance.expiryOrder;
			absorbOrder = __instance.absorbOrder;

			__instance.sailCapacity = 512;
            __instance.sailCursor = 0;
            __instance.sailRecycleCursor = 0;
            __instance.expiryCursor = 0;
            __instance.expiryEnding = 0;
            __instance.absorbCursor = 0;
            __instance.absorbEnding = 0;
			__instance.expiryOrder = Array.Empty<ExpiryOrder>();
			__instance.absorbOrder = Array.Empty<AbsorbOrder>();

			DysonSwarm_Export_NoGPU(__instance, w);

			__instance.sailCapacity = sailCapacity;
            __instance.sailCursor = sailCursor;
            __instance.sailRecycleCursor = sailRecycleCursor;
            __instance.expiryCursor = expiryCursor;
            __instance.expiryEnding = expiryEnding;
            __instance.absorbCursor = absorbCursor;
            __instance.absorbEnding = absorbEnding;
			__instance.expiryOrder = expiryOrder;
			__instance.absorbOrder = absorbOrder;

			return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.Export))]
        public static void DysonSwarm_Export_NoGPU(DysonSwarm __instance, BinaryWriter _)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    var codeMatcher = new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetData"))
                        .Repeat(matcher => matcher
                            .Advance(-8)
                            .SetAndAdvance(OpCodes.Nop, null)
                            .RemoveInstructions(8)
                        );

                    return codeMatcher.InstructionEnumeration();
                }
                catch (Exception err)
                {
                    Log.Error("DysonSwarm_Export_NoGPU error!");
                    Log.Error(err);
                    Enable = false;
                    return instructions;
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DysonNode), nameof(DysonNode.Export))]
        static IEnumerable<CodeInstruction> DysonNodeExport_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            try
            {
                // Replace: w.Write(this.cpOrdered);
                // To     : w.Write(DysonSwarmCompress.Enable ? 0 : this.cpOrdered);

                var codeMatcher = new CodeMatcher(instructions, iL)
                    .MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "cpOrdered"),
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "Write")
                    );
                codeMatcher.CreateLabelAt(codeMatcher.Pos, out Label jmpNormalFlow)
                           .CreateLabelAt(codeMatcher.Pos + 2, out Label end);
                codeMatcher.Insert(
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DysonSwarmCompress), "get_Enable")),
                    new CodeInstruction(OpCodes.Brfalse_S, jmpNormalFlow),
                    new CodeInstruction(OpCodes.Ldc_I4_0, null),
                    new CodeInstruction(OpCodes.Br_S, end)
                );

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception err)
            {
                Log.Error("DysonNodeExport_Transpiler error!");
                Log.Error(err);
                Enable = false;
                return instructions;
            }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.RemoveSailsByOrbit))]
        public static void RemoveSailsByOrbit_NoSync(DysonSwarm __instance, int orbitId)
        {
            // Don't sync this in nebula multiplayer
        }

        /*
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.AddSolarSail))]
        public static int AddSolarSail_NoGPU(DysonSwarm __instance, DysonSail ss, int orbitId, long expiryTime)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // remove swarmBuffer.GetData and swarmInfoBuffer.GetData
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "GetData"))
                    .Repeat(matcher => matcher
                            .Advance(-7)
                            .RemoveInstructions(8)
                    );
                return codeMatcher.InstructionEnumeration();
            }

            return 0;
        }
        */
#pragma warning restore CS8321, IDE0060
    }
}
