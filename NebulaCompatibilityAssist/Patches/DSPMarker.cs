using NebulaAPI;
using HarmonyLib;
using System;
using NebulaCompatibilityAssist.Packets;
using crecheng.DSPModSave;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPMarker
    {
        private const string NAME = "DSPMarker";
        private const string GUID = "Appun.DSP.plugin.Marker";
        private const string VERSION = "0.0.8";

        private static IModCanSave Save;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                // Sync Client's mod save with Host when joined
                Save = BepInEx.Bootstrap.Chainloader.PluginInfos[GUID].Instance as IModCanSave;
                NC_Patch.OnLogin += SendRequest;
                NC_ModSaveRequest.OnReceive += (guid, conn) =>
                {
                    if (guid != GUID) return;
                    conn.SendPacket(new NC_ModSaveData(GUID, Export()));
                };
                NC_ModSaveData.OnReceive += (guid, bytes) =>
                {
                    if (guid != GUID) return;
                    Import(bytes);
                };

                // Send mod save when changing marker
                Type targetType = AccessTools.TypeByName("DSPMarker.MarkerEditor");
                harmony.Patch(AccessTools.Method(targetType, "onClickApplyButton"), null, new HarmonyMethod(typeof(DSPMarker).GetMethod("SendData")));
                harmony.Patch(AccessTools.Method(targetType, "onClickDeleteButton"), null, new HarmonyMethod(typeof(DSPMarker).GetMethod("SendData")));

                // This is for debug
                targetType = AccessTools.TypeByName("DSPMarker.MarkerPool");
                harmony.Patch(AccessTools.Method(targetType, "Refresh"), new HarmonyMethod(typeof(DSPMarker).GetMethod("StopRefreshIfNoLocalPlanet")));
                targetType = AccessTools.TypeByName("DSPMarker.MarkerList");
                harmony.Patch(AccessTools.Method(targetType, "Refresh"), new HarmonyMethod(typeof(DSPMarker).GetMethod("StopRefreshIfNoLocalPlanet")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                Log.Debug(e);
            }
        }

        public static bool StopRefreshIfNoLocalPlanet()
        {
            return GameMain.localPlanet != null;
        }

        public static void SendRequest()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveRequest(GUID));
            }
        }

        public static void SendData()
        {
            if (NebulaModAPI.IsMultiplayerActive)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveData(GUID, Export()));
            }
        }

        public static byte[] Export()
        {
            if (Save != null)
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                Save.Export(p.BinaryWriter);
                return p.CloseAndGetBytes();
            }
            else
            {
                return new byte[0];
            }
        }

        public static void Import(byte[] bytes)
        {
            if (Save != null)
            {
                Log.Dev($"{NAME} import data");
                using var p = NebulaModAPI.GetBinaryReader(bytes);
                Save.Import(p.BinaryReader);
            }
        }
    }
}
