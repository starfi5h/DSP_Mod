using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace LossyCompression
{
	class EntityDataCompress
	{
		// Compress EntityData

		public static bool Enable { get; set; }
		public const int ModdedStartVersion = 10000;
		public const int CurrentVersion = 10001; // 10001 => vanilla7

		public static long len;
#pragma warning disable CS8321, IDE0060
		
		[HarmonyTranspiler, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
		static IEnumerable<CodeInstruction> PlanetFactoryExport_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
		{
			var codeMatcher = new CodeMatcher(instructions, iL)
				.Start()
				.CreateLabel(out var headLabel)
				.Insert(
					// If Enable == true, write CurrentVersion
					new CodeInstruction(OpCodes.Call, typeof(EntityDataCompress).GetProperty(nameof(Enable)).GetMethod),
					new CodeInstruction(OpCodes.Brfalse_S, headLabel),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldc_I4, CurrentVersion),
					new CodeInstruction(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(int) }))
				)
				.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
					new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndData")
				)
				.CreateLabel(out var endLabel)
				.MatchBack(true,
					new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
					new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginData")
				)
				.Advance(1)
				.CreateLabel(out var nextLabel)
				.Insert(
					// If Enable == true, run EntityExport; else run vanilla
					new CodeInstruction(OpCodes.Call, typeof(EntityDataCompress).GetProperty(nameof(Enable)).GetMethod),
					new CodeInstruction(OpCodes.Brfalse_S, nextLabel),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, typeof(EntityDataCompress).GetMethod(nameof(EntityExport))),
					new CodeInstruction(OpCodes.Br_S, endLabel)
				);			

			return codeMatcher.InstructionEnumeration();
		}

		static int version;
		
		[HarmonyTranspiler, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
		static IEnumerable<CodeInstruction> PlanetFactoryImport_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
		{
			// check if version > ModdedStartVersion, if yes, there use modded import
			var codeMatcher = new CodeMatcher(instructions, iL)
				.MatchForward(false,
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "ReadInt32"),
					new CodeMatch(i => i.opcode == OpCodes.Stloc_0)
				)
				.Advance(1)
				.RemoveInstruction()
				.Insert(
					HarmonyLib.Transpilers.EmitDelegate<Func<BinaryReader, int>>(
						(r) =>
						{
							version = r.ReadInt32();
							Log.Warn(version);
							return version >= ModdedStartVersion ? r.ReadInt32() : version;
						}
					)
				)
				.MatchForward(false,
					new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
					new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndData")
				)
				.CreateLabel(out var endLabel)
				.MatchBack(true,
					new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
					new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginData")
				)
				.Advance(1)
				.CreateLabel(out var nextLabel)
				.Insert(
					// If Enable == true, run EntityExport; else run vanilla
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EntityDataCompress), nameof(version))),
					new CodeInstruction(OpCodes.Ldc_I4, ModdedStartVersion),
					new CodeInstruction(OpCodes.Ble, nextLabel),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, typeof(EntityDataCompress).GetMethod(nameof(EntityImport))),
					new CodeInstruction(OpCodes.Br_S, endLabel)
				);

			return codeMatcher.InstructionEnumeration();
		}
		
		[HarmonyReversePatch, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
		public static void PlanetFactoryImportMod(PlanetFactory __instance, int _index, GameData _gameData, BinaryReader r)
		{
			IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				// Remove all part between ESaveDataEntry.Entity and replace with EntityImport
				var codeMatcher = new CodeMatcher(instructions)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
						new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "EndData")
					);
				int posEnd = codeMatcher.Pos;
				codeMatcher
					.MatchBack(true,
						new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)ESaveDataEntry.Entity),
						new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "BeginData")
					)
					.Advance(1)
					.RemoveInstructions(posEnd - codeMatcher.Pos)
					.Insert(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldarg_3),
						new CodeInstruction(OpCodes.Call, typeof(EntityDataCompress).GetMethod(nameof(EntityImport)))
					);

				return codeMatcher.InstructionEnumeration();
			}
		}

		public static void EntityExport(PlanetFactory factory, BinaryWriter w)
        {
			w.Write(factory.entityCapacity);
			w.Write(factory.entityCursor);
			w.Write(factory.entityRecycleCursor);
			
			for (int i = 1; i < factory.entityCursor; i++)
			{
				//factory.entityPool[i].Export(w);
				EntityData_Export(ref factory.entityPool[i], w);

				if (factory.entityPool[i].id != 0)
				{
					bool flag = factory.entitySignPool[i].count0 > 0.0001f;
					w.Write(factory.entityAnimPool[i].time);
					w.Write(factory.entityAnimPool[i].prepare_length);
					w.Write(factory.entityAnimPool[i].working_length);
					//w.Write(factory.entityAnimPool[i].state);
					//w.Write(factory.entityAnimPool[i].power);
					//w.Write((byte)factory.entitySignPool[i].signType);
					w.Write((byte)((ulong)factory.entitySignPool[i].iconType + (ulong)(flag ? 128L : 0L)));
					w.Write((ushort)factory.entitySignPool[i].iconId0);
					if (flag)
					{
						w.Write(factory.entitySignPool[i].count0);
					}
					//w.Write(factory.entitySignPool[i].x);
					//w.Write(factory.entitySignPool[i].y);
					//w.Write(factory.entitySignPool[i].z);
					//w.Write(factory.entitySignPool[i].w);

					int bitmask = 0; // optimize by bit mask
					int connStart = i * 16;
					for (int j = 0; j < 16; j++)
					{
						if (factory.entityConnPool[connStart + j] != 0)
							bitmask |= 1 << j;
					}
					w.Write((short)bitmask);
					for (int j = 0; j < 16; j++)
					{
						if (factory.entityConnPool[connStart + j] != 0)
							w.Write(factory.entityConnPool[connStart + j]);
					}
				}

			}

			for (int k = 0; k < factory.entityRecycleCursor; k++)
			{
				w.Write(factory.entityRecycle[k]);
			}
			w.Write(factory.prebuildCapacity);
			w.Write(factory.prebuildCursor);
			w.Write(factory.prebuildRecycleCursor);
			for (int l = 1; l < factory.prebuildCursor; l++)
			{
				factory.prebuildPool[l].Export(w);
				if (factory.prebuildPool[l].id != 0)
				{
					int bitmask = 0; // optimize by bit mask
					int connStart = l * 16;
					for (int j = 0; j < 16; j++)
					{
						if (factory.prebuildConnPool[connStart + j] != 0)
							bitmask |= 1 << j;
					}
					w.Write((short)bitmask);
					for (int j = 0; j < 16; j++)
					{
						if (factory.prebuildConnPool[connStart + j] != 0)
							w.Write(factory.prebuildConnPool[connStart + j]);
					}
				}
			}
			for (int n = 0; n < factory.prebuildRecycleCursor; n++)
			{
				w.Write(factory.prebuildRecycle[n]);
			}
		}

		public static void EntityImport(PlanetFactory factory, BinaryReader r)
        {
			int num2 = r.ReadInt32();
			factory.SetEntityCapacity(num2);
			factory.entityCursor = r.ReadInt32();
			factory.entityRecycleCursor = r.ReadInt32();
			
			for (int i = 1; i < factory.entityCursor; i++)
			{
				//factory.entityPool[i].Import(r);
				EntityData_Import(ref factory.entityPool[i], r);

				if (factory.entityPool[i].id != 0)
				{
					bool flag3 = false;
					factory.entityAnimPool[i].time = r.ReadSingle();
					factory.entityAnimPool[i].prepare_length = r.ReadSingle();
					factory.entityAnimPool[i].working_length = r.ReadSingle();
					//factory.entityAnimPool[i].state = r.ReadUInt32();
					//factory.entityAnimPool[i].power = r.ReadSingle();
					//factory.entitySignPool[i].signType = (uint)r.ReadByte();

					
					ItemProto itemProto = LDB.items.Select(factory.entityPool[i].protoId);
					if (itemProto != null) // Reconstruct position from entity
					{
						float signHegiht = factory.entityPool[i].beltId > 0 ? 1.2f : itemProto.prefabDesc.signHeight;
						factory.entitySignPool[i].Reset(factory.entityPool[i].pos, signHegiht, itemProto.prefabDesc.signSize);
					}
					factory.entitySignPool[i].iconType = (uint)r.ReadByte();
					if (factory.entitySignPool[i].iconType >= 128U)
					{
						flag3 = true;
						SignData[] array = factory.entitySignPool;
						int num3 = i;
						array[num3].iconType = array[num3].iconType - 128U;
					}
					factory.entitySignPool[i].iconId0 = (uint)r.ReadUInt16();
					if (flag3)
					{
						factory.entitySignPool[i].count0 = r.ReadSingle();
					}
					//factory.entitySignPool[i].x = r.ReadSingle();
					//factory.entitySignPool[i].y = r.ReadSingle();
					//factory.entitySignPool[i].z = r.ReadSingle();
					//factory.entitySignPool[i].w = r.ReadSingle();


					int connStart = i * 16;
					short bitmask = r.ReadInt16(); // optimize by bit mask
					for (int j = 0; j < 16; j++)
					{
						if ((bitmask & (1 << j)) != 0)
							factory.entityConnPool[connStart + j] = r.ReadInt32();
						else
							factory.entityConnPool[connStart + j] = 0;
					}

					if (factory.entityPool[i].beltId == 0 && factory.entityPool[i].inserterId == 0 && factory.entityPool[i].splitterId == 0 && factory.entityPool[i].monitorId == 0 && factory.entityPool[i].spraycoaterId == 0 && factory.entityPool[i].pilerId == 0)
					{
						factory.entityMutexs[i] = new Mutex(i);
					}
				}				
			}

			for (int k = 0; k < factory.entityRecycleCursor; k++)
			{
				factory.entityRecycle[k] = r.ReadInt32();
			}
			num2 = r.ReadInt32();
			factory.SetPrebuildCapacity(num2);
			factory.prebuildCursor = r.ReadInt32();
			factory.prebuildRecycleCursor = r.ReadInt32();
			for (int l = 1; l < factory.prebuildCursor; l++)
			{
				factory.prebuildPool[l].Import(r);
				if (factory.prebuildPool[l].id != 0)
				{
					int connStart = l * 16;
					int bitmask = r.ReadInt16(); // optimize by bit mask
					for (int j = 0; j < 16; j++)
					{
						if ((bitmask & (1 << j)) != 0)
						{
							factory.entityConnPool[connStart + j] = r.ReadInt32();
							//if (factory.entityConnPool[connStart + j] == 0)
							//	Log.Warn($"entityConnPool[{connStart + j}] == 0");
						}
						else
							factory.entityConnPool[connStart + j] = 0;
					}
				}
			}
			for (int n = 0; n < factory.prebuildRecycleCursor; n++)
			{
				factory.prebuildRecycle[n] = r.ReadInt32();
			}
		}

		//[HarmonyPrefix, HarmonyPatch(typeof(EntityData), nameof(EntityData.Export))]
		public static bool EntityData_Export(ref EntityData __instance, BinaryWriter w)
        {
			w.Write(__instance.id);
			if (__instance.id <= 0)
			{
				return false;
			}
			w.Write(__instance.protoId);
			w.Write(__instance.modelIndex);
			w.Write(__instance.pos.x);
			w.Write(__instance.pos.y);
			w.Write(__instance.pos.z);
			Utils.WriteCompressedRotation(w, in __instance.pos, in __instance.rot);
			//w.Write(__instance.rot.x);
			//w.Write(__instance.rot.y);
			//w.Write(__instance.rot.z);
			//w.Write(__instance.rot.w);

			// If id type >= 32 in the future, we will use bitmask1, and so on
			int bitmask0 = 0;

			if (__instance.beltId > 0)		bitmask0 |= 1 << 0;
			if (__instance.powerConId > 0)	bitmask0 |= 1 << 1;
			if (__instance.inserterId > 0)	bitmask0 |= 1 << 2;
			if (__instance.assemblerId > 0)	bitmask0 |= 1 << 3;
			if (__instance.labId > 0)		bitmask0 |= 1 << 4;
			if (__instance.powerNodeId > 0)	bitmask0 |= 1 << 5;
			if (__instance.powerGenId > 0)	bitmask0 |= 1 << 6;
			if (__instance.fractionatorId > 0)bitmask0 |= 1 << 7;
			if (__instance.storageId > 0)	bitmask0 |= 1 << 8;
			if (__instance.tankId > 0)		bitmask0 |= 1 << 9;
			if (__instance.splitterId > 0)	bitmask0 |= 1 << 10;
			if (__instance.ejectorId > 0)	bitmask0 |= 1 << 11;
			if (__instance.minerId > 0)		bitmask0 |= 1 << 12;
			if (__instance.siloId > 0)		bitmask0 |= 1 << 13;
			if (__instance.stationId > 0)	bitmask0 |= 1 << 14;
			if (__instance.dispenserId > 0)	bitmask0 |= 1 << 15;
			if (__instance.powerAccId > 0)	bitmask0 |= 1 << 16;
			if (__instance.powerExcId > 0)	bitmask0 |= 1 << 17;
			if (__instance.warningId > 0)	bitmask0 |= 1 << 18;
			if (__instance.monitorId > 0)	bitmask0 |= 1 << 19;
			if (__instance.speakerId > 0)	bitmask0 |= 1 << 20;
			if (__instance.spraycoaterId > 0)bitmask0 |= 1 << 21;
			if (__instance.pilerId > 0)		bitmask0 |= 1 << 22;
			
			w.Write(bitmask0);
			if ((bitmask0 & 255) > 0)
			{
				WriteCId(w, __instance.beltId, bitmask0, 0);
				WriteCId(w, __instance.powerConId, bitmask0, 1);
				WriteCId(w, __instance.inserterId, bitmask0, 2);
				WriteCId(w, __instance.assemblerId, bitmask0, 3);
				WriteCId(w, __instance.labId, bitmask0, 4);
				WriteCId(w, __instance.powerNodeId, bitmask0, 5);
				WriteCId(w, __instance.powerGenId, bitmask0, 6);
				WriteCId(w, __instance.fractionatorId, bitmask0, 7);
			}
			if ((bitmask0 >> 8 & 255) > 0)
			{
				WriteCId(w, __instance.storageId, bitmask0, 8);
				WriteCId(w, __instance.tankId, bitmask0, 9);
				WriteCId(w, __instance.splitterId, bitmask0, 10);
				WriteCId(w, __instance.ejectorId, bitmask0, 11);
				WriteCId(w, __instance.minerId, bitmask0, 12);
				WriteCId(w, __instance.siloId, bitmask0, 13);
				WriteCId(w, __instance.stationId, bitmask0, 14);
				WriteCId(w, __instance.dispenserId, bitmask0, 15);
			}
			if ((bitmask0 >> 16 & 255) > 0)
			{
				WriteCId(w, __instance.powerAccId, bitmask0, 16);
				WriteCId(w, __instance.powerExcId, bitmask0, 17);
				WriteCId(w, __instance.warningId, bitmask0, 18);
				WriteCId(w, __instance.monitorId, bitmask0, 19);
				WriteCId(w, __instance.speakerId, bitmask0, 20);
				WriteCId(w, __instance.spraycoaterId, bitmask0, 21);
				WriteCId(w, __instance.pilerId, bitmask0, 22);
			}

			return false;
        }

		//[HarmonyPrefix, HarmonyPatch(typeof(EntityData), nameof(EntityData.Import))]
		public static bool EntityData_Import(ref EntityData __instance, BinaryReader r)
        {
			__instance.id = r.ReadInt32();
			if (__instance.id <= 0)
			{
				return false;
			}
			__instance.protoId = r.ReadInt16();
			__instance.modelIndex = r.ReadInt16();
			__instance.pos.x = r.ReadSingle();
			__instance.pos.y = r.ReadSingle();
			__instance.pos.z = r.ReadSingle();
			__instance.rot = Utils.ReadCompressedRotation(r, in __instance.pos);
			//__instance.rot.x = r.ReadSingle();
			//__instance.rot.y = r.ReadSingle();
			//__instance.rot.z = r.ReadSingle();
			//__instance.rot.w = r.ReadSingle();

			int bitmask0 = r.ReadInt32();
			if ((bitmask0 & 255) > 0)
			{
				ReadCId(r, ref __instance.beltId, bitmask0, 0);
				ReadCId(r, ref __instance.powerConId, bitmask0, 1);
				ReadCId(r, ref __instance.inserterId, bitmask0, 2);
				ReadCId(r, ref __instance.assemblerId, bitmask0, 3);
				ReadCId(r, ref __instance.labId, bitmask0, 4);
				ReadCId(r, ref __instance.powerNodeId, bitmask0, 5);
				ReadCId(r, ref __instance.powerGenId, bitmask0, 6);
				ReadCId(r, ref __instance.fractionatorId, bitmask0, 7);
			}
			if ((bitmask0 >> 8 & 255) > 0)
			{
				ReadCId(r, ref __instance.storageId, bitmask0, 8);
				ReadCId(r, ref __instance.tankId, bitmask0, 9);
				ReadCId(r, ref __instance.splitterId, bitmask0, 10);
				ReadCId(r, ref __instance.ejectorId, bitmask0, 11);
				ReadCId(r, ref __instance.minerId, bitmask0, 12);
				ReadCId(r, ref __instance.siloId, bitmask0, 13);
				ReadCId(r, ref __instance.stationId, bitmask0, 14);
				ReadCId(r, ref __instance.dispenserId, bitmask0, 15);
			}
			if ((bitmask0 >> 16 & 255) > 0)
			{
				ReadCId(r, ref __instance.powerAccId, bitmask0, 16);
				ReadCId(r, ref __instance.powerExcId, bitmask0, 17);
				ReadCId(r, ref __instance.warningId, bitmask0, 18);
				ReadCId(r, ref __instance.monitorId, bitmask0, 19);
				ReadCId(r, ref __instance.speakerId, bitmask0, 20);
				ReadCId(r, ref __instance.spraycoaterId, bitmask0, 21);
				ReadCId(r, ref __instance.pilerId, bitmask0, 22);
			}

			return false;
		}

		private static void WriteCId(BinaryWriter w, int cid, int bitmask, int bitIndex)
		{
			if ((bitmask & (1 << bitIndex)) > 0)
			{
				w.Write((byte)(cid & 255));
				w.Write((byte)(cid >> 8 & 255));
				w.Write((byte)(cid >> 16 & 255));
			}
		}

		private static void ReadCId(BinaryReader r, ref int cid, int bitmask, int bitIndex)
		{
			cid = 0;
			if ((bitmask & (1 << bitIndex)) > 0)
			{
				int num1 = r.ReadByte();
				int num2 = r.ReadByte();
				int num3 = r.ReadByte();
				cid = (num1 | num2 << 8 | num3 << 16);
			}
		}
#pragma warning restore CS8321, IDE0060
	}
}
