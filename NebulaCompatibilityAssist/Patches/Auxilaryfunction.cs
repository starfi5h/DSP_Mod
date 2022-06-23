using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using NebulaModel.Packets.Factory;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class Auxilaryfunction
    {
        public const string NAME = "Auxilaryfunction";
        public const string GUID = "cn.blacksnipe.dsp.Auxilaryfunction";
        public const string VERSION = "1.6.9";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {


                // 自動配置新運輸站
                Type classType = assembly.GetType("Auxilaryfunction.AuxilaryfunctionPatch+NewStationComponentPatch");
                harmony.Patch(AccessTools.Method(classType, "Postfix"),
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("NewStationComponent_Prefix")),
                    new HarmonyMethod(typeof(Auxilaryfunction).GetMethod("NewStationComponent_Postfix")));

                // 填充當前星球飛船,翹曲器

                // 批量配置當前星球物流站

                // 批量配置當前星球大礦機速率

                // 物流塔物品複製黏貼


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

        public static void NewStationComponent_Postfix(StationComponent __0, PlanetTransport __1, bool __runOriginal)
        {
            if (NebulaModAPI.IsMultiplayerActive && __runOriginal)
            {
                PlanetFactory factory = __1.factory;                
                factory.CopyBuildingSetting(__0.entityId);
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new PasteBuildingSettingUpdate(__0.entityId, BuildingParameters.clipboard, factory.planetId));
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationItemCount(in __0, factory.planetId));
            }
        }
    }
}
