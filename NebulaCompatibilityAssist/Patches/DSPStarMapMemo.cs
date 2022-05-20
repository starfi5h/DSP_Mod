﻿using NebulaAPI;
using HarmonyLib;
using System;
using NebulaCompatibilityAssist.Packets;
using crecheng.DSPModSave;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPStarMapMemo
    {
        private const string NAME = "DSPStarMapMemo";
        private const string GUID = "Appun.DSP.plugin.StarMapMemo";
        private const string VERSION = "0.0.1";

        private static IModCanSave Save;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {                
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
                
                System.Type targetType = AccessTools.TypeByName("DSPStarMapMemo.MemoPool");
                harmony.Patch(AccessTools.Method(targetType, "AddOrUpdate"), null, new HarmonyMethod(typeof(DSPStarMapMemo).GetMethod("SendData")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                Log.Debug(e);
            }
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
                Log.Dev($"DSPStarMapMemo import data");
                using var p = NebulaModAPI.GetBinaryReader(bytes);
                Save.Import(p.BinaryReader);
            }
        }
    }
}
