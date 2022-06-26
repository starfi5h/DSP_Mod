using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class AutoStationConfig
    {
        public const string NAME = "AutoStationConfig";
        public const string GUID = "pasukaru.dsp.AutoStationConfig";
        public const string VERSION = "1.4.0";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Sync new station config
                Type classType = assembly.GetType("Pasukaru.DSP.AutoStationConfig.PlanetTransportPatch");
                harmony.Patch(AccessTools.Method(classType, "NewStationComponent"), 
                    new HarmonyMethod(typeof(AutoStationConfig).GetMethod("NewStationComponent_Prefix")),
                    new HarmonyMethod(typeof(AutoStationConfig).GetMethod("NewStationComponent_Postfix")));

                // Fix advance miner power usage abnormal
                classType = assembly.GetType("Pasukaru.DSP.AutoStationConfig.Extensions");
                harmony.Patch(AccessTools.Method(classType, "SetChargingPower"),
                    new HarmonyMethod(typeof(AutoStationConfig).GetMethod("SetChargingPower_Prefix")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static bool NewStationComponent_Prefix()
        {
            if (!NebulaModAPI.IsMultiplayerActive)
                return true;

            // Apply AutoStationConfig if author (the drone owner) is local player
            IFactoryManager factoryManager = NebulaModAPI.MultiplayerSession.Factories;
            return factoryManager.PacketAuthor == NebulaModAPI.MultiplayerSession.LocalPlayer.Id 
                || (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost && factoryManager.PacketAuthor == NebulaModAPI.AUTHOR_NONE);
        }

        public static void NewStationComponent_Postfix(PlanetTransport __0, StationComponent __1, bool __runOriginal)
        {
            if (NebulaModAPI.IsMultiplayerActive && __runOriginal)
            {
                PlanetFactory factory = __0.factory;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationConfig(__1, factory));
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationShipCount(__1, factory.planetId));
            }
        }

        public static bool SetChargingPower_Prefix(StationComponent __0)
        {
            // Disable set charging power if it is advance miner
            return !__0.isVeinCollector;              
        }
    }
}
