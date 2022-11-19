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
		public static readonly int ModdedStartVersion = 10000;
		public static readonly int CurrentVersion = 10001; // 10001 => vanilla7

		public static long len;
#pragma warning disable CS8321, IDE0060
		
		[HarmonyPostfix, HarmonyPatch(typeof(PerformanceMonitor), nameof(PerformanceMonitor.EndData))]
		static void EndData(ESaveDataEntry entry)
        {
			if (entry == ESaveDataEntry.Entity)
            {
				//Log.Debug(System.Environment.StackTrace);
            }
        }


		//[HarmonyPrefix, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Export))]
		public static bool PlanetFactory_Prefix(PlanetFactory __instance, BinaryWriter w)
		{
			if (!Enable)
				return true;
			w.Write(CurrentVersion);

			w.Write(7);
			PerformanceMonitor.BeginData(ESaveDataEntry.Planet);
			w.Write(__instance.planetId);
			w.Write(__instance.planet.theme);
			w.Write(__instance.planet.algoId);
			w.Write(__instance.planet.style);
			__instance.planet.ExportRuntime(w);
			w.Write(__instance.landed);
			PerformanceMonitor.EndData(ESaveDataEntry.Planet);
			PerformanceMonitor.BeginData(ESaveDataEntry.Entity);
			EntityExport(__instance, w);
			PerformanceMonitor.EndData(ESaveDataEntry.Entity);
			PerformanceMonitor.BeginData(ESaveDataEntry.Planet);
			w.Write(__instance.vegeCapacity);
			w.Write(__instance.vegeCursor);
			w.Write(__instance.vegeRecycleCursor);
			for (int num5 = 1; num5 < __instance.vegeCursor; num5++)
			{
				__instance.vegePool[num5].Export(w);
			}
			for (int num6 = 0; num6 < __instance.vegeRecycleCursor; num6++)
			{
				w.Write(__instance.vegeRecycle[num6]);
			}
			w.Write(__instance.veinCapacity);
			w.Write(__instance.veinCursor);
			w.Write(__instance.veinRecycleCursor);
			for (int num7 = 1; num7 < __instance.veinCursor; num7++)
			{
				__instance.veinPool[num7].Export(w);
			}
			for (int num8 = 0; num8 < __instance.veinRecycleCursor; num8++)
			{
				w.Write(__instance.veinRecycle[num8]);
			}
			for (int num9 = 1; num9 < __instance.veinCursor; num9++)
			{
				w.Write(__instance.veinAnimPool[num9].time);
				w.Write(__instance.veinAnimPool[num9].prepare_length);
				w.Write(__instance.veinAnimPool[num9].working_length);
				w.Write(__instance.veinAnimPool[num9].state);
				w.Write(__instance.veinAnimPool[num9].power);
			}
			PerformanceMonitor.EndData(ESaveDataEntry.Planet);
			PerformanceMonitor.BeginData(ESaveDataEntry.BeltAndCargo);
			__instance.cargoContainer.Export(w);
			__instance.cargoTraffic.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.BeltAndCargo);
			PerformanceMonitor.BeginData(ESaveDataEntry.Storage);
			__instance.factoryStorage.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.Storage);
			PerformanceMonitor.BeginData(ESaveDataEntry.PowerSystem);
			__instance.powerSystem.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.PowerSystem);
			PerformanceMonitor.BeginData(ESaveDataEntry.Facility);
			__instance.factorySystem.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.Facility);
			PerformanceMonitor.BeginData(ESaveDataEntry.Transport);
			__instance.transport.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.Transport);
			PerformanceMonitor.BeginData(ESaveDataEntry.Platform);
			__instance.platformSystem.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.Platform);
			PerformanceMonitor.BeginData(ESaveDataEntry.Digital);
			__instance.digitalSystem.Export(w);
			PerformanceMonitor.EndData(ESaveDataEntry.Digital);

			return false;
		}


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
					new CodeInstruction(OpCodes.Ldc_I4_S, CurrentVersion),
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

		
		[HarmonyTranspiler, HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
		static IEnumerable<CodeInstruction> PlanetFactoryImport_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
		{
			// check if version > ModdedStartVersion, if yes, there use modded import
			var codeMatcher = new CodeMatcher(instructions, iL)
				.MatchForward(true,
					new CodeMatch(i => i.IsLdarg()),
					new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "ReadInt32"),
					new CodeMatch(i => i.opcode == OpCodes.Stloc_0)
				)
				.Advance(1)
				.CreateLabel(out var nextLebel)
				.Insert(
					HarmonyLib.Transpilers.EmitDelegate<Action>(
					() =>
					{
						Log.Warn("pass1");
					}
					),
					new CodeInstruction(OpCodes.Ldloc_0),
					new CodeInstruction(OpCodes.Ldc_I4, ModdedStartVersion),
					new CodeInstruction(OpCodes.Ble, nextLebel),
					new CodeInstruction(OpCodes.Nop),
					HarmonyLib.Transpilers.EmitDelegate<Action>(
					() =>
					{
						Log.Warn("pass2");
					}
					),
					new CodeInstruction(OpCodes.Nop),
					new CodeInstruction(OpCodes.Ret),
					/*
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
                    //new CodeInstruction(OpCodes.Callvirt, typeof(EntityDataCompress).GetMethod(nameof(PlanetFactoryImportMod))),
                    HarmonyLib.Transpilers.EmitDelegate<Action<PlanetFactory, int, GameData, BinaryReader>> (
					(factroy, index, data, r) =>
                    {
						Log.Debug(index);
						PlanetFactoryImportMod(factroy, index, data, r);					
					}					
					),
					*/
					new CodeInstruction(OpCodes.Ret)
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
			long pos = w.BaseStream.Position;
			w.Write((byte)0);
			w.Write(factory.entityCapacity);
			w.Write(factory.entityCursor);
			w.Write(factory.entityRecycleCursor);
			for (int i = 1; i < factory.entityCursor; i++)
			{
				//__instance.entityPool[i].Export(w);
				EntityData_Export(ref factory.entityPool[i], w);

				if (factory.entityPool[i].id != 0)
				{
					bool flag = factory.entitySignPool[i].count0 > 0.0001f;
					w.Write(factory.entityAnimPool[i].time);
					w.Write(factory.entityAnimPool[i].prepare_length);
					w.Write(factory.entityAnimPool[i].working_length);
					//w.Write(__instance.entityAnimPool[i].state); // Change every tick
					//w.Write(__instance.entityAnimPool[i].power); // Change every tick
					//w.Write((byte)__instance.entitySignPool[i].signType); // Change every tick
					w.Write((byte)((ulong)factory.entitySignPool[i].iconType + (ulong)(flag ? 128L : 0L)));
					w.Write((ushort)factory.entitySignPool[i].iconId0);
					if (flag)
					{
						w.Write(factory.entitySignPool[i].count0);
					}
					//w.Write(__instance.entitySignPool[i].x); // Can be gained by reconstruct
					//w.Write(__instance.entitySignPool[i].y);
					//w.Write(__instance.entitySignPool[i].z);
					//w.Write(__instance.entitySignPool[i].w);

					
					short bitmask = 0; // optimize by bit mask
					int connStart = i * 16;
					int connEnd = connStart + 16;
					for (int j = connStart; j < connEnd; j++)
					{
						if (factory.entityConnPool[j] != 0)
							bitmask |= (short)(1 << (j- connStart));
					}
					w.Write(bitmask);
					for (int j = connStart; j < connEnd; j++) 
					{
						if (factory.entityConnPool[j] != 0)
							w.Write(factory.entityConnPool[j]);
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
					short bitmask = 0; // optimize by bit mask
					int connStart = l * 16;
					int connEnd = connStart + 16;
					for (int j = connStart; j < connEnd; j++)
					{
						if (factory.prebuildConnPool[j] != 0)
							bitmask |= (short)(1 << (j - connStart));
					}
					w.Write(bitmask); 
					for (int j = connStart; j < connEnd; j++)
					{
						if (factory.prebuildConnPool[j] != 0)
							w.Write(factory.prebuildConnPool[j]);
					}
				}
			}
			for (int n = 0; n < factory.prebuildRecycleCursor; n++)
			{
				w.Write(factory.prebuildRecycle[n]);
			}
			Log.Debug(w.BaseStream.Position - pos);
			len += w.BaseStream.Position - pos;
		}

		public static void EntityImport(PlanetFactory factory, BinaryReader r)
		{
			Log.Info("Import!");
			int version = r.ReadByte();
			factory.SetEntityCapacity(r.ReadInt32());
			factory.entityCursor = r.ReadInt32();
			factory.entityRecycleCursor = r.ReadInt32();
			for (int i = 1; i < factory.entityCursor; i++)
			{
				//factory.entityPool[i].Import(r);
				EntityData_Import(ref factory.entityPool[i], r);

				if (factory.entityPool[i].id != 0)
				{
					bool flag = false;
					factory.entityAnimPool[i].time = r.ReadSingle();
					factory.entityAnimPool[i].prepare_length = r.ReadSingle();
					factory.entityAnimPool[i].working_length = r.ReadSingle();
					//factory.entityAnimPool[i].state = r.ReadUInt32();
					//factory.entityAnimPool[i].power = r.ReadSingle();
					//factory.entitySignPool[i].signType = (uint)r.ReadByte();
					factory.entitySignPool[i].iconType = r.ReadByte();
					if (factory.entitySignPool[i].iconType >= 128U)
					{
						flag = true;
						factory.entitySignPool[i].iconType -= 128U;
					}
					factory.entitySignPool[i].iconId0 = r.ReadUInt16();
					if (flag)
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
						if ((bitmask & (1 << j)) > 0)
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
			factory.SetPrebuildCapacity(r.ReadInt32());
			factory.prebuildCursor = r.ReadInt32();
			factory.prebuildRecycleCursor = r.ReadInt32();
			for (int l = 1; l < factory.prebuildCursor; l++)
			{
				factory.prebuildPool[l].Import(r);
				if (factory.prebuildPool[l].id != 0)
				{
					int connStart = l * 16;
					short bitmask = r.ReadInt16(); // optimize by bitmask
					for (int j = 0; j < 16; j++)
					{
						if ((bitmask & (1 << j)) > 0)
							factory.prebuildConnPool[connStart + j] = r.ReadInt32();
						else
							factory.prebuildConnPool[connStart + j] = 0;
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

		[HarmonyPostfix, HarmonyPatch(typeof(UIEntityBriefInfo), "_OnOpen")]
		public static void UIEntityBriefInfo_OnOpen(UIEntityBriefInfo __instance)
		{
			ref EntityData entity = ref __instance.factory.entityPool[__instance.entityId];
			//Log.Debug(entity.rot);
			//entity.rot = Quaternion.Euler(90, 0, 0);

			//this.yaw = (Quaternion.Inverse(Maths.SphericalRotation(entityPool[objectId].pos, 0f)) * entityPool[objectId].rot).eulerAngles.y;


			var q1 = Quaternion.LookRotation(entity.pos);
			var q2 = Maths.SphericalRotation(entity.pos, 0f);
			Maths.SphericalRotation(Vector3.zero, 0f);

			var q3 = (Quaternion.Inverse(Maths.SphericalRotation(entity.pos, 0f)) * entity.rot);
			float yaw = (Quaternion.Inverse(Maths.SphericalRotation(entity.pos, 0f)) * entity.rot).eulerAngles.y;

			Maths.GetLatitudeLongitude(entity.pos, out int latd, out int latf, out int logd, out int logf, out bool north, out bool south, out bool west, out bool east);
			Log.Debug($"N{north} {latd} E{east} {logd} - {entity.pos} {entity.rot}");


			//Log.Info(q3 + " " + q3.eulerAngles);
			//Log.Warn(yaw);

			//entity.rot = q1;
		}

#pragma warning restore CS8321, IDE0060
	}
}
