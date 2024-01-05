using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MinerInfo
{
    class VeinFilterPatch
    {
        static EVeinType filterType = EVeinType.None;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "OnPlanetDataSet")]
        public static void OnPlanetDataSet(UIPlanetDetail __instance)
        {
            // OnPlanetDataSet is called when 1. OnOpen 2. Switch planet 3. Switch display fliter 4. OnClose
            filterType = EVeinType.None; // Reset vein type filter
            if (__instance.planet?.factory != null)
            {
                foreach (var entry in __instance.entries)
                {
                    entry.iconButton.data = entry.refId;
                    entry.iconButton.BindOnClickSafe(OnVeinIconClick);
                    //Plugin.Log.LogDebug((EVeinType)entry.refId);
                }
            }
        }

        public static void OnVeinIconClick(int data)
        {
            var veinDetail = UIRoot.instance.uiGame.veinDetail;
            var planetDetail = UIRoot.instance.uiGame.planetDetail;
            if (planetDetail.planet == veinDetail.inspectPlanet && planetDetail.planet != null) // Only apply on inspectPlanet
            {
                filterType = filterType == EVeinType.None ? (EVeinType)data : EVeinType.None; // Toggle filter type
                foreach (var entry in planetDetail.entries)
                {
                    entry.gameObject.SetActive(entry.refId == data || filterType == EVeinType.None); // ON=>Focus OFF=>Resume
                }
                UIRoot.instance.uiGame.veinDetail.SetInspectPlanet(UIRoot.instance.uiGame.veinDetail.inspectPlanet); // Refresh node view
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(UIVeinDetailNode), "Refresh")]
        static IEnumerable<CodeInstruction> FitlerVeinType(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var fieldinfo = AccessTools.Field(typeof(VeinGroup), nameof(VeinGroup.type));
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.IsLdloc()),
                        new CodeMatch(OpCodes.Ldfld, fieldinfo),
                        new CodeMatch(OpCodes.Brtrue)
                    )
                    .Advance(2)
                    .Insert(Transpilers.EmitDelegate<Func<EVeinType, bool>>(
                        (type) =>
                        {
                            if (filterType == EVeinType.None)
                                return type != EVeinType.None; // normal: display all vein
                            else
                                return type == filterType; // filter: display only filterType
                        }
                    ));
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Transpiler UIVeinDetailNode.Refresh fail!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }
    }
}
