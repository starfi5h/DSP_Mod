using BepInEx.Configuration;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using NebulaModel.Packets.Factory.PowerGenerator;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class Auxilaryfunction
    {
        public const string NAME = "Auxilaryfunction";
        public const string GUID = "cn.blacksnipe.dsp.Auxilaryfunction";
        public const string VERSION = "1.6.9";

        private static ConfigEntry<bool> stationcopyItem_bool; // 物流站复制物品配方
        private static ConfigEntry<bool> auto_supply_station; // 自动配置新运输站

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("Auxilaryfunction.Auxilaryfunction");
                stationcopyItem_bool = (ConfigEntry<bool>)AccessTools.Field(classType, "stationcopyItem_bool").GetValue(pluginInfo.Instance);
                auto_supply_station = (ConfigEntry<bool>)AccessTools.Field(classType, "auto_supply_station").GetValue(pluginInfo.Instance);

                // 填充當前星球飛機,飛船,翹曲器
                harmony.Patch(AccessTools.Method(classType, "addDroneShiptooldstation"), null,
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("AddDroneShiptooldstation_Postfix")));

                // 批量配置當前星球物流站
                harmony.Patch(AccessTools.Method(classType, "changeallstationconfig"), null,
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("Changeallstationconfig_Postfix")));

                // 批量配置當前星球大礦機速率
                harmony.Patch(AccessTools.Method(classType, "changeallveincollectorspeedconfig"), null,
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("Changeallstationconfig_Postfix")));

                // 填充當前星球人造恆星
                harmony.Patch(AccessTools.Method(classType, "addfueltoallStar"), null,
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("AddfueltoallStar_Postfix")));

                // 自動配置新運輸站
                classType = assembly.GetType("Auxilaryfunction.AuxilaryfunctionPatch+NewStationComponentPatch");
                harmony.Patch(AccessTools.Method(classType, "Postfix"),
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("NewStationComponent_Prefix")),
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("NewStationComponent_Postfix")));

                // 物流塔物品複製黏貼
                classType = assembly.GetType("Auxilaryfunction.AuxilaryfunctionPatch+PasteToFactoryObjectPatch");
                harmony.Patch(AccessTools.Method(classType, "Prefix"),
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("PasteToFactoryObject_Prefix")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static void AddDroneShiptooldstation_Postfix()
        {
            if (NebulaModAPI.IsMultiplayerActive && GameMain.localPlanet?.factory != null)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationShipCount(GameMain.localPlanet.factory.transport.stationPool, GameMain.localPlanet.id));
            }
        }

        public static void Changeallstationconfig_Postfix()
        {
            if (NebulaModAPI.IsMultiplayerActive && GameMain.localPlanet?.factory != null)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationConfig(GameMain.localPlanet.factory.transport.stationPool, GameMain.localPlanet.factory));
            }
        }

        public static void AddfueltoallStar_Postfix()
        {
            if (NebulaModAPI.IsMultiplayerActive && GameMain.localPlanet?.factory != null)
            {
                foreach (PowerGeneratorComponent pgc in GameMain.localPlanet.factory.powerSystem.genPool)
                {
                    if (pgc.fuelMask == 4)
                    {
                        NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                            new PowerGeneratorFuelUpdatePacket(pgc.id, pgc.fuelId, pgc.fuelCount, pgc.fuelInc, GameMain.localPlanet.id));
                    }
                }
            }
        }

        public static bool NewStationComponent_Prefix()
        {
            if (!auto_supply_station.Value)
                return false;
            if (!NebulaModAPI.IsMultiplayerActive)
                return true;

            // Apply AutoStationConfig if author (the drone owner) is local player
            IFactoryManager factoryManager = NebulaModAPI.MultiplayerSession.Factories;
            return factoryManager.PacketAuthor == NebulaModAPI.MultiplayerSession.LocalPlayer.Id 
                || (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost && factoryManager.PacketAuthor == NebulaModAPI.AUTHOR_NONE);
        }

        public static void NewStationComponent_Postfix(StationComponent __0, PlanetTransport __1, bool __runOriginal)
        {
            if (NebulaModAPI.IsMultiplayerActive && __runOriginal)
            {
                PlanetFactory factory = __1.factory;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationConfig(__0, factory));
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationShipCount(__0, factory.planetId));
            }
        }

        public static bool PasteToFactoryObject_Prefix()
        {
            if (!stationcopyItem_bool.Value)
                return false;
            if (!NebulaModAPI.IsMultiplayerActive)
                return true;

            // Apply AutoStationConfig if author (the drone owner) is local player
            IFactoryManager factoryManager = NebulaModAPI.MultiplayerSession.Factories;
            return !factoryManager.IsIncomingRequest.Value;
        }
    }
}
