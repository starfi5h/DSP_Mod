using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DeliverySlotsTweaks
{
    public class BuildToolGhost_Patch
	{
		[HarmonyTranspiler]
		[HarmonyAfter("dsp.nebula-multiplayer"), HarmonyPriority(Priority.Low)]
		[HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
		[HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CheckBuildConditions))]
		[HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CheckBuildConditions))]
		[HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CheckBuildConditions))]
		public static IEnumerable<CodeInstruction> CheckBuildConditions_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
			try
			{
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldc_I4_2), // EBuildCondition.NotEnoughItem
						new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BuildPreview), nameof(BuildPreview.condition)))
						)
					.Advance(-1)
					.SetAndAdvance(OpCodes.Nop, null)
					.SetAndAdvance(OpCodes.Nop, null)
					.SetAndAdvance(OpCodes.Nop, null);

				if (codeMacher.Opcode == OpCodes.Br)
                {
					codeMacher.RemoveInstruction();
                }

				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler BuildTool.CheckBuildConditions error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		[HarmonyTranspiler]
		[HarmonyAfter("dsp.nebula-multiplayer"), HarmonyPriority(Priority.Low)]
		[HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Addon), nameof(BuildTool_Addon.CreatePrebuilds))]
		[HarmonyPatch(typeof(BuildTool_Path), nameof(BuildTool_Path.CreatePrebuilds))]
		public static IEnumerable<CodeInstruction> CreatePrebuilds_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
		{
			try
			{
				// Find and copy buildPreview.objId = -this.factory.AddPrebuildDataWithComponents(prebuildData)
				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(PlanetFactory), "AddPrebuildDataWithComponents")));
				var list = codeMacher.InstructionsInRange(codeMacher.Pos - 4, codeMacher.Pos + 2);

				// Turn it to use &PrebuildData as argument
				switch (__originalMethod.DeclaringType.Name)
                {
					case "BuildTool_Click":
						ToLdloca(list[3]);
						list[4] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildToolGhost_Patch), nameof(AddPrebuildData_Click)));
						break;

					case "BuildTool_Inserter":
						ToLdloca(list[3]);
						list[4] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildToolGhost_Patch), nameof(AddPrebuildData_Inserter)));
						list.Insert(0, list[0]);
						break;

					case "BuildTool_Path":
						ToLdloca(list[3]);
						list[4] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildToolGhost_Patch), nameof(AddPrebuildData_Addon)));
						list.Insert(0, list[0]);
						break;

					case "BuildTool_Addon":
						ToLdloca(list[3]);
						list[4] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildToolGhost_Patch), nameof(AddPrebuildData_Path)));
						list.RemoveAt(2);
						list.Insert(0, list[0]);
						break;
				}

				// Replace:
				//   Assert.CannotBeReached();
				//   UIRealtimeTip.Popup("物品不足".Translate(), true, 1);
				// To the custom AddPrebuildDataWithComponents methods
				codeMacher.MatchForward(true, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Assert), "CannotBeReached")))
					.Advance(1);
				do
				{
					codeMacher.RemoveInstruction();
				} while (!(codeMacher.Opcode == OpCodes.Call && ((MethodInfo)codeMacher.Operand).Name == "Popup"));
				codeMacher.RemoveInstruction();

				codeMacher.Insert(list);

				//return instructions;
				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler CreatePrebuilds error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		private static void ToLdloca(CodeInstruction code)
        {
			if (code.opcode == OpCodes.Ldloc_3)
            {
				code.operand = (sbyte)3;
			}
			else if (code.opcode == OpCodes.Ldloc_2)
            {
				code.operand = (sbyte)2;
			}
			else if (code.opcode == OpCodes.Ldloc_1)
			{
				code.operand = (sbyte)1;
			}
			else if (code.opcode == OpCodes.Ldloc_0)
			{
				code.operand = (sbyte)0;
			}
			code.opcode = OpCodes.Ldloca_S;
		}


		public static int AddPrebuildData_Click(PlanetFactory factory, ref PrebuildData prebuildData)
        {
			prebuildData.itemRequired = 1;
			int prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);
			return prebuildId;
		}

		public static int AddPrebuildData_Inserter(BuildPreview buildPreview, PlanetFactory factory, ref PrebuildData prebuildData)
		{
			if (buildPreview.coverObjId != 0)
			{
				//UIRealtimeTip.Popup("物品不足".Translate(), true, 1);
				return 0;
			}
			prebuildData.itemRequired = 1;
			int prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);
			return prebuildId;
		}

		public static int AddPrebuildData_Addon(BuildPreview buildPreview, PlanetFactory factory, ref PrebuildData prebuildData)
		{
			prebuildData.itemRequired = 1;
			int prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);
			if (prebuildId != 0 && (buildPreview.desc.isSpraycoster || buildPreview.desc.isDispenser || buildPreview.desc.isTurret))
            {
				if (buildPreview.outputObjId != 0)
				{
					factory.WriteObjectConn(buildPreview.objId, buildPreview.outputFromSlot, true, buildPreview.outputObjId, buildPreview.outputToSlot);
				}
				else if (buildPreview.output != null)
				{
					factory.WriteObjectConn(buildPreview.objId, buildPreview.outputFromSlot, true, buildPreview.output.objId, buildPreview.outputToSlot);
				}
				if (buildPreview.inputObjId != 0)
				{
					factory.WriteObjectConn(buildPreview.objId, buildPreview.inputToSlot, false, buildPreview.inputObjId, buildPreview.inputFromSlot);
				}
				else if (buildPreview.input != null)
				{
					factory.WriteObjectConn(buildPreview.objId, buildPreview.inputToSlot, false, buildPreview.input.objId, buildPreview.inputFromSlot);
				}
			}
			return prebuildId;
		}

		public static int AddPrebuildData_Path(BuildPreview buildPreview, BuildTool_Path @this, ref PrebuildData prebuildData)
		{
			if (buildPreview.coverObjId != 0 && !buildPreview.willRemoveCover)
            {
				UIRealtimeTip.Popup("物品不足".Translate(), true, 1);
				return 0;
			}
			prebuildData.itemRequired = 1;
			int prebuildId = 0;

			var factory = @this.factory;
			if (buildPreview.coverObjId == 0)
			{
				prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);
			}
			else if (buildPreview.willRemoveCover)
			{
				int coverObjId = buildPreview.coverObjId;
				if (ObjectIsBelt(factory, coverObjId))
				{
					for (int j = 0; j < 4; j++)
					{
                        factory.ReadObjectConn(coverObjId, j, out bool _, out int num4, out int _);
                        int num6 = num4;
						if (num6 != 0 && ObjectIsBelt(factory, num6))
						{
							bool flag3 = false;
							for (int k = 0; k < 2; k++)
							{
								factory.ReadObjectConn(num6, k, out _, out num4, out _);
								if (num4 != 0)
								{
									bool flag4 = ObjectIsBelt(factory, num4);
									bool flag5 = ObjectIsInserter(factory, num4);
									if (!flag4 && !flag5)
									{
										flag3 = true;
										break;
									}
								}
							}
							if (flag3)
							{
								@this.tmp_links.Add(num6);
							}
						}
					}
				}
				if (buildPreview.coverObjId > 0)
				{
					Array.Copy(factory.entityConnPool, buildPreview.coverObjId * 16, @this.tmp_conn, 0, 16);
					for (int l = 0; l < 16; l++)
					{
                        factory.ReadObjectConn(buildPreview.coverObjId, l, out bool _, out int num7, out int num8);
                        if (num7 > 0)
						{
							factory.ApplyEntityDisconnection(num7, buildPreview.coverObjId, num8, l);
						}
					}
					Array.Clear(@this.factory.entityConnPool, buildPreview.coverObjId * 16, 16);
				}
				else
				{
					Array.Copy(factory.prebuildConnPool, -buildPreview.coverObjId * 16, @this.tmp_conn, 0, 16);
					Array.Clear(factory.prebuildConnPool, -buildPreview.coverObjId * 16, 16);
				}
				prebuildId = factory.AddPrebuildDataWithComponents(prebuildData);
				if (buildPreview.objId > 0)
				{
					Array.Copy(@this.tmp_conn, 0, factory.entityConnPool, buildPreview.objId * 16, 16);
				}
				else
				{
					Array.Copy(@this.tmp_conn, 0, factory.prebuildConnPool, -buildPreview.objId * 16, 16);
				}
				factory.EnsureObjectConn(buildPreview.objId);
			}
			else
			{
				buildPreview.objId = buildPreview.coverObjId;
			}
			return prebuildId;
		}

		private static bool ObjectIsBelt(PlanetFactory factory, int objId)
		{
			if (objId == 0)
			{
				return false;
			}
			if (objId > 0)
			{
				return factory.entityPool[objId].beltId > 0;
			}
			var itemProto = LDB.items.Select(factory.prebuildPool[-objId].protoId);
			return itemProto != null && itemProto.prefabDesc.isBelt;
		}

		private static bool ObjectIsInserter(PlanetFactory factory, int objId)
        {
			if (objId == 0)
			{
				return false;
			}
			if (objId > 0)
			{
				return factory.entityPool[objId].inserterId > 0;
			}
			var itemProto = LDB.items.Select(factory.prebuildPool[-objId].protoId);
			return itemProto != null && itemProto.prefabDesc.isInserter;
		}
	}
}
