using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BuildToolOpt
{
	public class BuildTool_Inserter_Patch
	{
		[HarmonyTranspiler, HarmonyAfter("com.hetima.dsp.LongSorter")]
		[HarmonyPatch(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.DeterminePreviews))]
		public static IEnumerable<CodeInstruction> DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				// Change: if (this.cursorValid) { if (this.castObjectId > 0) { ... } }
				// To:     if (this.cursorValid) { if (this.castObjectId != 0) { ... } }
				// 因為虛影(prebuild)的castObjectId < 0, 將原本判定實體改為判定是否有物體

				var codeMacher = new CodeMatcher(instructions)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.castObjectId))),
						new CodeMatch(OpCodes.Ldc_I4_0),
						new CodeMatch(OpCodes.Ble))
					.SetOpcodeAndAdvance(OpCodes.Beq);

				// Change: if (this.cursorValid && this.startObjectId != this.castObjectId && this.startObjectId > 0 && this.castObjectId > 0)
				// To:     if (this.cursorValid && this.startObjectId != this.castObjectId)
				// To allow prebuild as startObject and castObject

				codeMacher.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildTool_Inserter), nameof(BuildTool_Inserter.startObjectId))),
						new CodeMatch(OpCodes.Ldc_I4_0),
						new CodeMatch(OpCodes.Ble));

				for (int i = 0; i < 6; i++)
					codeMacher.SetAndAdvance(OpCodes.Nop, null);
				codeMacher.RemoveInstructions(2)
					.Insert( // Add pose for prebuild belt
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Inserter_Patch), nameof(AddPrebuildBeltSlot)))
					);

				// Replace: base.ObjectIsBelt(objId)
				// To:      ObjectIsBeltEntity(this,objId)
				// Only deal with belt logic if there is entity belt

				codeMacher
					.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(BuildTool), nameof(BuildTool.ObjectIsBelt))))
					.Repeat(
						matcher => matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(BuildTool_Inserter_Patch), nameof(ObjectIsBeltEntity)))
					);




				return codeMacher.InstructionEnumeration();
			}
			catch (Exception e)
			{
				Plugin.Log.LogWarning("Transpiler BuildTool_Inserter.DeterminePreviews error");
				Plugin.Log.LogWarning(e);
				return instructions;
			}
		}

		static bool ObjectIsBeltEntity(BuildTool_Inserter @this, int objId)
		{
			if (objId <= 0)
			{
				return false;
			}
			return @this.factory.entityPool[objId].beltId > 0;
		}

		static void AddPrebuildBeltSlot(BuildTool_Inserter @this)
        {
			if (@this.startObjectId < 0 && @this.ObjectIsBelt(@this.startObjectId))
			{
				foreach (Pose pose in @this.belt_slots)
				{
					BuildTool_Inserter.SlotPoint slotPoint;
					slotPoint.objId = @this.startObjectId;
					ref var prebuild = ref @this.factory.prebuildPool[-@this.startObjectId];
					slotPoint.pose = pose.GetTransformedBy(new Pose(prebuild.pos, prebuild.rot));
					slotPoint.slotIdx = -1;
					@this.startSlots.Add(slotPoint);
				}
			}
			if (@this.castObjectId < 0 && @this.ObjectIsBelt(@this.castObjectId))
			{
				foreach (Pose pose in @this.belt_slots)
				{
					BuildTool_Inserter.SlotPoint slotPoint;
					slotPoint.objId = @this.castObjectId;
					ref var prebuild = ref @this.factory.prebuildPool[-@this.castObjectId];
					slotPoint.pose = pose.GetTransformedBy(new Pose(prebuild.pos, prebuild.rot));
					slotPoint.slotIdx = -1;
					@this.endSlots.Add(slotPoint);
				}
			}
		}

		[HarmonyPostfix, HarmonyPatch(typeof(BuildTool_Inserter), "CheckBuildConditions")]
		public static void BuildTool_Inserter_CheckBuildConditions_Postfix(BuildTool_Inserter __instance, ref bool __result)
		{
			if (!__result && __instance.buildPreviews.Count == 1)
			{
				var buildPreview = __instance.buildPreviews[0];
				if (buildPreview.condition != EBuildCondition.Collide) return;
				if (buildPreview.inputObjId >= 0 && buildPreview.outputObjId >= 0) return;

				//Plugin.Log.LogDebug($"{buildPreview.inputObjId} {__instance.startObjectId} {buildPreview.outputObjId} {__instance.castObjectId} : {buildPreview.paramCount}");
				__result = true;
				buildPreview.condition = EBuildCondition.Ok;
				__instance.actionBuild.model.cursorText = buildPreview.conditionText;
				__instance.actionBuild.model.cursorState = 0;
				if (!VFInput.onGUI)
				{
					UICursor.SetCursor(ECursor.Default);
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BuildTool_Inserter), "DeterminePreviews")]
		public static void PrebuildCorrection(BuildTool_Inserter __instance)
		{
			if (__instance.buildPreviews.Count != 1) return;
			if (__instance.startObjectId >= 0 && __instance.castObjectId >= 0) return;

			bool startIsBelt = __instance.startObjectId < 0 && __instance.ObjectIsBelt(__instance.startObjectId);
			bool endIsBelt = __instance.castObjectId < 0 && __instance.ObjectIsBelt(__instance.castObjectId);

			var buildPreview = __instance.buildPreviews[0];



			if (buildPreview.condition == EBuildCondition.TooSkew)
            {
				if (startIsBelt && !endIsBelt && __instance.endSlots.Count > 0)
                {
					var pos = __instance.startSlots[0].pose.position;
					var slots = __instance.endSlots;
					int slotId = 0;
					float minDist = float.MaxValue;


					for (int i = 0; i < slots.Count; i++)
                    {
						float dist = Vector3.SqrMagnitude(slots[i].pose.position - pos);
						//var vector = pos - slots[i].pose.position;
						//var degree = Vector3.Angle(vector, slots[i].pose.forward);
						//Plugin.Log.LogDebug($"[{i}] {dist} {degree}");
						if (dist < minDist)
                        {
							minDist = dist;
							slotId = i;
						}
                    }
					buildPreview.lpos2 = slots[slotId].pose.position;
					buildPreview.lrot2 = slots[slotId].pose.rotation * Quaternion.Euler(0f, 180f, 0f);
					buildPreview.outputToSlot = slots[slotId].slotIdx;
					//buildPreview.outputOffset = 0; //TODO: Fix offset for belt
					buildPreview.lrot = buildPreview.lrot2;
				}
				if (!startIsBelt && endIsBelt && __instance.startSlots.Count > 0)
				{
					var pos = __instance.endSlots[0].pose.position;
					var slots = __instance.startSlots;
					int slotId = 0;
					float minDist = float.MaxValue;

					for (int i = 0; i < slots.Count; i++)
					{
						float dist = Vector3.SqrMagnitude(slots[i].pose.position - pos);
						var vector = pos - slots[i].pose.position;
						var degree = Vector3.Angle(vector, slots[i].pose.forward);
						//Plugin.Log.LogDebug($"[{i}] {dist} {degree}");
						if (dist < minDist && degree < 40f)
						{
							minDist = dist;
							slotId = i;
						}
					}
					buildPreview.lpos = slots[slotId].pose.position;
					buildPreview.lrot = slots[slotId].pose.rotation;
					buildPreview.inputFromSlot = slots[slotId].slotIdx;
					//buildPreview.inputOffset = 0; //TODO: Fix offset for belt
					buildPreview.lrot2 = buildPreview.lrot;
				}
				if (startIsBelt && endIsBelt)
				{
					// TODO: Fix rot of belt to belt
				}
				
				buildPreview.condition = EBuildCondition.Ok;
			}

			if (buildPreview.condition == EBuildCondition.Ok)
			{
				var inserterBuildTip = __instance.uiGame.inserterBuildTip;
				inserterBuildTip.direction = (buildPreview.lpos2 - buildPreview.lpos).normalized;
				inserterBuildTip.position = (buildPreview.lpos + buildPreview.lpos2) * 0.5f;
				inserterBuildTip.position1 = buildPreview.lpos;
				inserterBuildTip.position2 = buildPreview.lpos2;
				if (startIsBelt && !endIsBelt)
				{
					inserterBuildTip.input = true;
					inserterBuildTip.output = false;
					inserterBuildTip.outputBelt = true;
					inserterBuildTip.inputBelt = false;
				}
				if (!startIsBelt && endIsBelt)
				{
					inserterBuildTip.output = true;
					inserterBuildTip.input = false;
					inserterBuildTip.inputBelt = true;
					inserterBuildTip.outputBelt = false;
				}
			}
		}
	}
}
