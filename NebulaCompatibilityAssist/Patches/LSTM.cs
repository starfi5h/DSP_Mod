using NebulaAPI;
using HarmonyLib;
using System;
using NebulaCompatibilityAssist.Packets;

namespace NebulaCompatibilityAssist.Patches
{
    public static class LSTM
    {
        public const string GUID = "com.hetima.dsp.LSTM";
        public const string VERSION = "6.5.0";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                // Send request when client open window or click global/systme buttons
                System.Type targetType = AccessTools.TypeByName("LSTMMod.UIBalanceWindow");
                harmony.Patch(AccessTools.Method(targetType, "_OnOpen"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));
                harmony.Patch(targetType.GetMethod("SwitchToStarSystem"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));
                harmony.Patch(targetType.GetMethod("SwitchToGlobal"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));

                Log.Info("LSTM - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"LSTM - Fail! Last target version: {VERSION}");
                Log.Debug(e);
            }
        }

        public static void SendRequest()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_StationStorageRequest());
            }
        }
    }
}
