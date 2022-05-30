using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPMarker
    {
        private const string NAME = "DSPMarker";
        private const string GUID = "Appun.DSP.plugin.Marker";
        private const string VERSION = "0.0.8";

        private static IModCanSave Save;
        private static Action MarkerPool_Refresh;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Sync Client's mod save with Host when joined
                Save = pluginInfo.Instance as IModCanSave;
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
                Type classType = assembly.GetType("DSPMarker.MarkerEditor");
                harmony.Patch(AccessTools.Method(classType, "onClickApplyButton"), null, new HarmonyMethod(typeof(DSPMarker).GetMethod("SendData")));
                harmony.Patch(AccessTools.Method(classType, "onClickDeleteButton"), null, new HarmonyMethod(typeof(DSPMarker).GetMethod("SendData")));

                // Below are for bugfix
                classType = assembly.GetType("DSPMarker.MarkerPool");
                harmony.Patch(AccessTools.Method(classType, "Refresh"), new HarmonyMethod(typeof(DSPMarker).GetMethod("StopRefreshIfNoLocalPlanet")));
                MarkerPool_Refresh = AccessTools.MethodDelegate<Action>(AccessTools.Method(classType, "Refresh"));

                classType = assembly.GetType("DSPMarker.MarkerList");
                harmony.Patch(AccessTools.Method(classType, "Refresh"), new HarmonyMethod(typeof(DSPMarker).GetMethod("StopRefreshIfNoLocalPlanet")));

                classType = assembly.GetType("DSPMarker.Patch");
                harmony.Patch(AccessTools.Method(classType, "UIStarDetail_ArrivePlanet_Postfix"), null, new HarmonyMethod(typeof(DSPMarker).GetMethod("ArrivePlanet_Postfix")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static bool StopRefreshIfNoLocalPlanet()
        {
            return GameMain.localPlanet != null;
        }

        public static void ArrivePlanet_Postfix()
        {
            MarkerPool_Refresh.Invoke();
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
