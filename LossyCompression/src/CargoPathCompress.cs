using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LossyCompression
{
    class CargoPathCompress
    {
        // Compress position and rotation using a line simplification algorithm
        // Format: (int)capactiy - (int)bufferLength - [point][point][line start point, length, line end point][point]....

        public static bool Enable { get; set; }
        public static readonly int EncodedVersion = 61; //PeekChar() max is 127

        public static void Encode(CargoPath cargoPath, BinaryWriter w)
        {
            w.Write(cargoPath.capacity);
            w.Write(cargoPath.bufferLength);
            int len = 0;
            Vector3 prevPos = Vector3.zero;
            Quaternion prevRot = Quaternion.identity, headRot = Quaternion.identity;
            Vector3[] pointPos = cargoPath.pointPos;
            Quaternion[] pointRot = cargoPath.pointRot;

            for (int i = 0; i < cargoPath.bufferLength; i++)
            {
                // merge only if rotation from previous point difference < 0.5deg and from head rotation < 1.0deg
                if (Quaternion.Dot(pointRot[i], prevRot) > 0.9999619f && Quaternion.Dot(pointRot[i], headRot) > 0.99984769515)
                {
                    //Log.Debug($"{(pointPos[i] - prevPos).sqrMagnitude} {Quaternion.Dot(pointRot[i], prevRot)} {Quaternion.Dot(pointRot[i], headRot)}");
                    len++;
                }
                else
                {
                    if (len > 0)
                    {
                        w.Write(len * 10000f); // assume max height is < 10000f
                        w.Write(prevPos.x); // write the ending point of the sequence
                        w.Write(prevPos.y);
                        w.Write(prevPos.z);
                        w.Write(prevRot.x);
                        w.Write(prevRot.y);
                        w.Write(prevRot.z);
                        w.Write(prevRot.w);
                        len = 0;
                    }
                    w.Write(pointPos[i].x);
                    w.Write(pointPos[i].y);
                    w.Write(pointPos[i].z);
                    w.Write(pointRot[i].x);
                    w.Write(pointRot[i].y);
                    w.Write(pointRot[i].z);
                    w.Write(pointRot[i].w);
                    headRot = pointRot[i];
                }
                prevPos = pointPos[i];
                prevRot = pointRot[i];
            }
            if (len > 0)
            {
                w.Write(len * 10000f);
                w.Write(prevPos.x);
                w.Write(prevPos.y);
                w.Write(prevPos.z);
                w.Write(prevRot.x);
                w.Write(prevRot.y);
                w.Write(prevRot.z);
                w.Write(prevRot.w);
            }
        }

        public static void Decode(CargoPath cargoPath, BinaryReader r)
        {
            int capactiy = r.ReadInt32();
            int bufferLength = r.ReadInt32();
            int compressedLength = 0;
            Vector3 pos1 = Vector3.zero, pos2;
            Quaternion rot1 = Quaternion.identity, rot2;
            Vector3[] pointPos = new Vector3[capactiy];
            Quaternion[] pointRot = new Quaternion[capactiy];

            int i = 0;
            while (i < bufferLength)
            {
                float token = r.ReadSingle();
                if (token > 9999f)
                {
                    int len = ((int)token) / 10000;
                    pos2.x = r.ReadSingle();
                    pos2.y = r.ReadSingle();
                    pos2.z = r.ReadSingle();
                    rot2.x = r.ReadSingle();
                    rot2.y = r.ReadSingle();
                    rot2.z = r.ReadSingle();
                    rot2.w = r.ReadSingle();

                    // Interpolate 
                    for (int j = 1; j <= len; j++)
                    {
                        pointPos[i] = Vector3.Slerp(pos1, pos2, j / (float)len);
                        pointRot[i] = Quaternion.Slerp(rot1, rot2, j / (float)len);
                        i++;
                    }

                    compressedLength += len - 1;
                }
                else
                {
                    pos1.x = token;
                    pos1.y = r.ReadSingle();
                    pos1.z = r.ReadSingle();
                    rot1.x = r.ReadSingle();
                    rot1.y = r.ReadSingle();
                    rot1.z = r.ReadSingle();
                    rot1.w = r.ReadSingle();
                    pointPos[i] = pos1;
                    pointRot[i] = rot1;
                    i++;
                }

            }

#if DEBUG
            //Log.Info($"Total: {bufferLength} Compressed: {compressedLength} ({100f * compressedLength / bufferLength}%)");
            //Compare(bufferLength, cargoPath.pointPos, pointPos);
#endif
            cargoPath.capacity = capactiy;
            cargoPath.bufferLength = bufferLength;
            cargoPath.pointPos = pointPos;
            cargoPath.pointRot = pointRot;
        }

        static void Compare(int length, Vector3[] oldPos, Vector3[] newPos)
        {
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                float diff = (oldPos[i] - newPos[i]).sqrMagnitude;
                if (diff > 0.5f)
                {
                    count++;
                    //Log.Debug($"{oldPos[i]} {newPos[i]} => {diff}");
                }
            }
            if (count > 0)
                Log.Warn($"Diff count: {count}");
        }

#pragma warning disable CS8321, IDE0060

        [HarmonyPrefix, HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Export))]
        public static bool CargoPath_Export(CargoPath __instance, BinaryWriter w)
        {
            if (!Enable) return true;

            w.Write(EncodedVersion);
            w.Write(__instance.id);

            //w.Write(__instance.capacity);
            //w.Write(__instance.bufferLength);
            Encode(__instance, w);

            w.Write(__instance.chunkCapacity);
            w.Write(__instance.chunkCount);
            w.Write(__instance.updateLen);
            w.Write(__instance.closed);
            w.Write((__instance.outputPath == null) ? 0 : __instance.outputPath.id);
            w.Write((__instance.outputPath == null) ? -1 : __instance.outputIndex);
            w.Write(__instance.belts.Count);
            w.Write(__instance.inputPaths.Count);
            w.Write(__instance.buffer, 0, __instance.bufferLength);
            for (int i = 0; i < __instance.chunkCount; i++)
            {
                w.Write(__instance.chunks[i * 3]);
                w.Write(__instance.chunks[i * 3 + 1]);
                w.Write(__instance.chunks[i * 3 + 2]);
            }

            //Skip pointPos, pointRot loop

            for (int k = 0; k < __instance.belts.Count; k++)
                w.Write(__instance.belts[k]);
            for (int l = 0; l < __instance.inputPaths.Count; l++)
                w.Write(__instance.inputPaths[l]);

            return false;
        }

        static int version;

        [HarmonyPrefix, HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Import))]
        public static bool CargoPathImport_Prefix(CargoPath __instance, BinaryReader r)
        {
            version = r.ReadInt32();
            if (version != EncodedVersion)
            {
                CargoPathImport(__instance, r);
                return false;
            }
            __instance.Free();
            __instance.id = r.ReadInt32();

            //__instance.SetCapacity(r.ReadInt32());
            //__instance.bufferLength = r.ReadInt32();
            Decode(__instance, r);
            __instance.buffer = new byte[__instance.capacity];

            __instance.SetChunkCapacity(r.ReadInt32());
            __instance.chunkCount = r.ReadInt32();
            __instance.updateLen = r.ReadInt32();
            __instance.closed = r.ReadBoolean();
            __instance.outputPathIdForImport = r.ReadInt32();
            __instance.outputIndex = r.ReadInt32();
            int num = r.ReadInt32();
            int num2 = r.ReadInt32();
            r.BaseStream.Read(__instance.buffer, 0, __instance.bufferLength);
            for (int i = 0; i < __instance.chunkCount; i++)
            {
                __instance.chunks[i * 3] = r.ReadInt32();
                __instance.chunks[i * 3 + 1] = r.ReadInt32();
                __instance.chunks[i * 3 + 2] = r.ReadInt32();
            }

            //Skip pointPos, pointRot loop

            __instance.belts = new List<int>();
            for (int k = 0; k < num; k++)
                __instance.belts.Add(r.ReadInt32());
            __instance.inputPaths = new List<int>();
            for (int l = 0; l < num2; l++)
                __instance.inputPaths.Add(r.ReadInt32());

            return false;
        }

        [HarmonyReversePatch, HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Import))]
        public static void CargoPathImport(CargoPath __instance, BinaryReader r)
        {
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Replace the first r.ReadInt32() to CargoPathCompress.version
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.IsLdarg()),
                        new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "ReadInt32")
                    )
                    .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CargoPathCompress), "version")))
                    .SetOpcodeAndAdvance(OpCodes.Nop);

                return codeMatcher.InstructionEnumeration();
            }
        }

#pragma warning restore CS8321, IDE0060
    }
}