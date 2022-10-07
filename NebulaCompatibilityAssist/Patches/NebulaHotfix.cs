using HarmonyLib;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NebulaHotfix
    {
        private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 11)
                {
                    Type classType = AccessTools.TypeByName("NebulaWorld.Logistics.CourierManager");
                    harmony.Patch(AccessTools.Method(classType, "GameTick"), null, null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod("GameTick_Transpiler")));
                    Log.Info("Nebula hotfix 0.8.11 - OK");
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        public static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace : this.CourierDatas[j++] = this.CourierDatas[k];
                // with    : this.CourierDatas[j] = this.CourierDatas[k];
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Ldloc_0),
                        new CodeMatch(OpCodes.Dup),      //+3 => Nop
                        new CodeMatch(OpCodes.Ldc_I4_1), //+4 => Nop
                        new CodeMatch(OpCodes.Add),      //+5 => Nop
                        new CodeMatch(OpCodes.Stloc_0),  //+6 => Nop
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldelem),
                        new CodeMatch(OpCodes.Stelem))
                    .Advance(3)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null);

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Warn("CourierManager.GameTick_Transpiler fail!");
                return instructions;
            }
        }
    }
}
