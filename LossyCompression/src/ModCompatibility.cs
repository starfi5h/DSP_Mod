using HarmonyLib;
using NebulaAPI;
using System;
using System.IO;
using System.Reflection;

namespace LossyCompression
{
    public class ModCompatibility
    {
        public static Action AfeterImport;

        public static class DSPOptimizations
        {
            public const string GUID = "com.Selsion.DSPOptimizations";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;

                    // Invoke DysonNodeOpt.InitSPAndCPCounts after shells and swarm are loaded
                    var classType = pluginInfo.Instance.GetType().Assembly.GetType("DSPOptimizations.DysonNodeOpt");
                    var methodInfo = AccessTools.Method(classType, "InitSPAndCPCounts");
                    AfeterImport = AccessTools.MethodDelegate<Action>(methodInfo);

                    // ShellShaderVarOpt will call SetMaterialDynamicVars so guard is needed
                    harmony.PatchAll(typeof(DSPOptimizations));

                    Log.Info("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSPOptimizations compatibility failed! Last working version: 1.1.11");
                    Log.Warn(e);
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(DysonShell), nameof(DysonShell.SetMaterialDynamicVars))]
            public static bool SetMaterialDynamicVars(DysonShell __instance)
            {
                // nodeProgressArr is intialized in DysonShell.GenerateModelObjects, which is null before lazy loading
                if (__instance.nodeProgressArr != null)
                {
                    int num = __instance.nodecps.Length - 1;
                    int num2 = 0;
                    while (num2 < num && num2 < 768)
                    {
                        __instance.nodeProgressArr[num2] = (float)((double)__instance.nodecps[num2] / ((__instance.vertsqOffset[num2 + 1] - __instance.vertsqOffset[num2]) * __instance.cpPerVertex));
                        num2++;
                    }
                    if (__instance.nodecps.Length <= 768)
                    {
                        __instance.nodeProgressArr[num] = (float)((double)__instance.nodecps[num] / __instance.vertsqOffset[num]);
                    }
                }
                // material is intialized in GenerateModelObjects too
                if (__instance.material != null)
                {
                    __instance.material.SetFloat("_State", __instance.state);
                    int value = __instance.color.a << 24 | __instance.color.b << 16 | __instance.color.g << 8 | __instance.color.r;
                    __instance.material.SetInt("_Color32Int", value);
                    __instance.material.SetFloatArray("_NodeProgressArr", __instance.nodeProgressArr);
                }
                return false;
            }
        }

        public static class NebulaAPI
        {
            public const string GUID = "dsp.nebula-multiplayer-api";

            public static void Init(Harmony harmony)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                        return;
                    Patch(harmony);
                    Log.Info("Nebula compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("Nebula compatibility failed!");
                    Log.Warn(e);
                }
            }

            private static void Patch(Harmony harmony)
            {
                // Separate for using NebulaModAPI
                if (!NebulaModAPI.NebulaIsInstalled)
                    return;

                NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
                var classType = AccessTools.TypeByName("NebulaWorld.Universe.DysonSphereManager");
                var methodInfo = AccessTools.Method(classType, "RegisterPlayer");
                harmony.Patch(methodInfo, null, new HarmonyMethod(typeof(NebulaAPI), "DysonDataPostfix"));
            }

            public static void DysonDataPostfix(INebulaConnection conn, int starIndex)
            {
                conn.SendPacket(new LC_DysonData(starIndex));
            }
        }


        internal class LC_DysonData
        {
            public int StarIndex { get; set; }
            public int EnableFlags { get; set; }
            public byte[] Bytes { get; set; }
            public long GameTick { get; set; }

            public LC_DysonData() { }
            public LC_DysonData(int starIndex)
            {
                StarIndex = starIndex;
                EnableFlags = Plugin.GetEnables();
                DysonSphere dysonSphere = GameMain.data.dysonSpheres[starIndex];
                using (var w = NebulaModAPI.GetBinaryWriter())
                {
                    if (DysonShellCompress.Enable)
                        DysonShellCompress.Encode(dysonSphere, w.BinaryWriter);
                    if (DysonSwarmCompress.Enable)
                        DysonSwarmCompress.Encode(dysonSphere, w.BinaryWriter);
                    Bytes = w.CloseAndGetBytes();
                }
                GameTick = GameMain.gameTick;
                Log.Debug($"Send compressed data {Bytes.Length:N0}");
                DysonShellCompress.FreeRAM();
            }
        }

        [RegisterPacketProcessor]
        internal class NC_ModSaveDataProcessor : BasePacketProcessor<LC_DysonData>
        {
            public override void ProcessPacket(LC_DysonData packet, INebulaConnection conn)
            {
                if (IsClient)
                {
                    Log.Debug($"Recv compressed data {packet.Bytes.Length:N0}, flag {packet.EnableFlags}");

                    DysonSphere dysonSphere = GameMain.data.dysonSpheres[packet.StarIndex];
                    using (var r = NebulaModAPI.GetBinaryReader(packet.Bytes))
                    {
                        if ((packet.EnableFlags & 2) != 0)
                            DysonShellCompress.Decode(dysonSphere, r.BinaryReader, DysonShellCompress.EncodedVersion);
                        if ((packet.EnableFlags & 4) != 0)
                            DysonSwarmCompress.Decode(dysonSphere, r.BinaryReader, packet.GameTick);
                    }
                    DysonShellCompress.FreeRAM();

                    // Let DSPOpt init
                    AfeterImport?.Invoke();

                    // Reset veiwing dyson sphere to check again
                    LazyLoading.Reset();
                }
            }
        }
    }
}
