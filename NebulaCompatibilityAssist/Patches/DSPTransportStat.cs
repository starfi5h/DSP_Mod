using BepInEx;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using HarmonyLib;
using UnityEngine;
using System;
using DSPTransportStat;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPTransportStat_Patch
    {
        public const string NAME = "DSPTransportStat";
        public const string GUID = "IndexOutOfRange.DSPTransportStat";
        public const string VERSION = "0.0.10";

        private static BaseUnityPlugin instance;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                instance = BepInEx.Bootstrap.Chainloader.PluginInfos[GUID].Instance;

                // Send request when client open window or click global/systme buttons
                //System.Type targetType = AccessTools.TypeByName("DSPTransportStat.UITransportStationsWindow");
                //harmony.Patch(targetType.GetMethod("ComputeTransportStationsWindow_LoadStations"), null, new HarmonyMethod(typeof(DSPTransportStat_Patch).GetMethod("LoadStations")));
                //NC_StationStorageReponse.OnReceive += OnReceive;

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

        public static void OnReceive()
        {
            var plugin = instance as DSPTransportStat.Plugin;
            if (plugin.uiTransportStationsWindow.active)
            {
                //plugin.uiTransportStationsWindow.ComputeTransportStationsWindow_LoadStations();
            }
            Log.Dev("OnReceive");
        }

        public static void LoadStations(UITransportStationsWindow __instance)
        {
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
            {
                return;
            }

            bool toggleInPlanet = __instance.uiTSWParameterPanel.ToggleInPlanet;
            bool toggleInterstellar = __instance.uiTSWParameterPanel.ToggleInterstellar;
            bool toggleCollector = __instance.uiTSWParameterPanel.ToggleCollector;
            int relatedItemFilter = __instance.uiTSWParameterPanel.RelatedItemFilter;

            for (int k = 1; k < GameMain.data.galacticTransport.stationCursor; ++k)
            {
                StationComponent station = GameMain.data.galacticTransport.stationPool[k];

                
                if (station == null || station.entityId == 0)
                {
                    continue;
                }

                // 当地星球的物流塔已经载入过了
                if (station.planetId == GameMain.localPlanet?.id || station.storage == null)
                {
                    continue;
                }

                // 是否显示行星内物流站
                if (!toggleInPlanet && !station.isCollector && !station.isStellar)
                {
                    continue;
                }

                // 是否显示星际物流运输站
                if (!toggleInterstellar && !station.isCollector && station.isStellar)
                {
                    continue;
                }

                // 是否显示采集站
                if (!toggleCollector && station.isCollector)
                {
                    continue;
                }

                // 通过搜索字符串对站点进行过滤
                string name = DSPTransportStat.Extensions.StationComponentExtensions.GetStationName(station);
                PlanetData planet = GameMain.galaxy.PlanetById(station.planetId);
                StarData star = planet.star;
                if (!string.IsNullOrWhiteSpace(__instance.searchString) 
                    && !name.Contains(__instance.searchString) 
                    && !star.name.Contains(__instance.searchString) 
                    && !planet.name.Contains(__instance.searchString))
                {
                    continue;
                }

                // 过滤相关物品
                if (relatedItemFilter != DSPTransportStat.Global.Constants.NONE_ITEM_ID)
                {
                    // 该站点至少有一个槽位包含用户选择的物品
                    int ii = 0;
                    for (; ii < station.storage.Length; ++ii)
                    {
                        if (station.storage[ii].itemId == relatedItemFilter)
                        {
                            break;
                        }
                    }
                    if (ii == station.storage.Length)
                    {
                        continue;
                    }
                }

                Log.Debug(planet.id);
                __instance.stations.Add(new StationInfoBundle(star, planet, station));
            }
            __instance.OnSort();
            __instance.contentRectTransform.offsetMin = new Vector2(0, -DSPTransportStat.Global.Constants.TRANSPORT_STATIONS_ENTRY_HEIGHT * __instance.stations.Count);
            __instance.contentRectTransform.offsetMax = new Vector2(0, 0);
            __instance.uiStationCountInListTranslation.SetNumber(__instance.stations.Count);

            NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_StationStorageRequest());
        }

    }
}
