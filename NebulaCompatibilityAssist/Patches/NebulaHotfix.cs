using HarmonyLib;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using NebulaModel.Networking;
using NebulaModel.Packets.Logistics;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NebulaHotfix
    {
        private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";
        private static bool isPatched = false;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 11)
                {
                    Patch0811(harmony);
                    Log.Info("Nebula hotfix 0.8.11 - OK");
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        private static void Patch0811(Harmony harmony)
        {
            Type classType = AccessTools.TypeByName("NebulaWorld.Logistics.CourierManager");
            harmony.Patch(AccessTools.Method(classType, "GameTick"), null, null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod("GameTick_Transpiler")));

            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaHotfix).GetMethod("BeforeHostGame")));
        }

        public static void BeforeHostGame()
        {
            if (!isPatched)
            {
                isPatched = true;
                try
                {
                    // We need patch PacketProcessor after NebulaNetwork assembly is loaded
                    foreach (Assembly a in AccessTools.AllAssemblies())
                    {
                        //Log.Info(a.GetName()); //why does iterate all assemblies stop the exception?
                    }
                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Logistics.DispenserCourierProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(DispenserCourierPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaHotfix).GetMethod("DispenserCourierProcessor")));

                    Log.Info("DispenserCourierProcessor patch success! (hotfix 0.8.11)");
                }
                catch (Exception e)
                {
                    Log.Warn("DispenserCourierProcessor patch fail!");
                    Log.Warn(e);
                }
            }
        }

        public static bool DispenserCourierProcessor(DispenserCourierPacket packet)
        {
            // Prevent accessing a null dispenser for host
            PlanetFactory factory = GameMain.mainPlayer.factory;
            DispenserComponent[] pool = factory?.transport.dispenserPool;
            if (pool != null && packet.DispenserId > 0 && packet.DispenserId < pool.Length && pool[packet.DispenserId] != null)
            {
                return true;
            }
            return false;
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
