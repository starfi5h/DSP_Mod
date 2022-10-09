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

                    Log.Info("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    Log.Warn("DSPOptimizations compatibility failed! Last working version: 1.1.11");
                    Log.Warn(e);
                }
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
                harmony.Patch(classType.GetMethod("RegisterPlayer"), null, new HarmonyMethod(typeof(NebulaAPI), "DysonDataPostfix"));

                NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
            }

            public static void OnMultiplayerGameStarted()
            {
                LazyLoading.Reset();
            }

            public static void OnMultiplayerGameEnded()
            {
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

                    if (AfeterImport != null)
                    {
                        AfeterImport.Invoke();
                    }
                }
            }
        }
    }
}
