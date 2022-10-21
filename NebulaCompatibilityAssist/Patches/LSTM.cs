using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;

namespace NebulaCompatibilityAssist.Patches
{
    public static class LSTM
    {
        public const string NAME = "LSTM";
        public const string GUID = "com.hetima.dsp.LSTM";
        public const string VERSION = "0.6.7";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                // Send request when client open window or click global/systme buttons
                Type classType = AccessTools.TypeByName("LSTMMod.UIBalanceWindow");
                harmony.Patch(AccessTools.Method(classType, "_OnOpen"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));
                harmony.Patch(classType.GetMethod("SwitchToStarSystem"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));
                harmony.Patch(classType.GetMethod("SwitchToGlobal"), new HarmonyMethod(typeof(LSTM).GetMethod("SendRequest")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
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
