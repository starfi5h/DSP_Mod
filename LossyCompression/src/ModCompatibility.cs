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
        static string dialogMessage = "";

        public static void Init(Harmony harmony)
        {
            if (GameConfig.gameVersion != new Version(0, 9, 27))
            {
                if (Localization.language == Language.zhCN)
                    dialogMessage = "此mod仅适用于0.9.27, 可能无法在新的游戏版本中运作!\n若有压缩后的旧存档, 请先回滚游戏版本再储存成未压缩(原版)存档\n";
                else
                    dialogMessage = "This mod is only applicable to 0.9.27 and may not work in the newer game version!\nIf there is an old compressed save, please roll back the game version first and then save it as a uncompressed(vanilla) save.\n";
            }

            SphereOpt.Init(harmony);
            DSPOptimizations.Init(harmony);
            NebulaAPI.Init(harmony);

            if (!string.IsNullOrEmpty(dialogMessage))
            {
                harmony.Patch(AccessTools.Method(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded)), null,
                    new HarmonyMethod(AccessTools.Method(typeof(ModCompatibility), nameof(ShowMessage))));
            }
        }

        static void ShowMessage()
        {
            UIMessageBox.Show("Lossy Compression兼容性提示", dialogMessage, "确定".Translate(), 3);
        }

        static void LogNotice(string modName, string lastWorkingVersion)
        {
            string message;
            if (Localization.language == Language.zhCN)
                message = modName + "兼容失效! 记录中最后适用版本: " + lastWorkingVersion;
            else
                message = modName + " compatibility failed! Last working version: " + lastWorkingVersion;
            dialogMessage += message + "\n";
            Log.Warn(message);
        }

        public static class SphereOpt
        {
            public const string GUID = "SphereOpt";

            public static void Init(Harmony _)
            {
                try
                {
                    if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;

                    if (LazyLoading.Enable)
                    {
                        var message = "Lazy loading is disabled due to compat with SphereOpt.\n侦测到SphereOpt,已将延迟载入功能关闭";
                        dialogMessage += message + "\n";
                        LazyLoading.Enable = false;
                        Log.Debug("SphereOpt compatibility - OK");
                    }
                }
                catch (Exception e)
                {
                    LogNotice("SphereOpt", "0.8.1");
                    Log.Warn(e);
                }
            }
        }


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

                    Log.Debug("DSPOptimizations compatibility - OK");
                }
                catch (Exception e)
                {
                    LogNotice("DSPOptimizations", "1.1.14");
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
                    Log.Debug("Nebula compatibility - OK");
                }
                catch (Exception e)
                {
                    LogNotice("Nebula", "0.8.14");
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

                harmony.Patch(AccessTools.Method(typeof(DysonSwarm), nameof(DysonSwarm.RemoveSailsByOrbit)), new HarmonyMethod(typeof(NebulaAPI), "RemoveSailsByOrbitPrefix"));
            }

            public static void DysonDataPostfix(INebulaConnection conn, int starIndex)
            {
                conn.SendPacket(new LC_DysonData(starIndex));
            }

            public static bool RemoveSailsByOrbitPrefix(int orbitId)
            {
                if (orbitId == 0)
                {
                    if ((bool)AccessTools.Property(AccessTools.TypeByName("NebulaWorld.Multiplayer"), "IsDedicated")?.GetValue(null) == true)
                    {
                        Log.Debug("Skip sails rearrange");
                        return false;
                    }
                }
                return true;
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
