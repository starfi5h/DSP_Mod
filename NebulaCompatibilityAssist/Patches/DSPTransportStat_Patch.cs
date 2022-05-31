using BepInEx;
using DSPTransportStat;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Reflection;
using UnityEngine;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPTransportStat_Patch
    {
        public const string NAME = "DSPTransportStat";
        public const string GUID = "IndexOutOfRange.DSPTransportStat";
        public const string VERSION = "0.0.13";

        private static BaseUnityPlugin instance;
        private static bool supression;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                instance = pluginInfo.Instance;
                
                // Send request when client open window or click global/systme buttons
                Type classType = assembly.GetType("DSPTransportStat.UITransportStationsWindow");
                harmony.Patch(classType.GetMethod("ComputeTransportStationsWindow_LoadStations"), null, new HarmonyMethod(typeof(DSPTransportStat_Patch).GetMethod("LoadStations")));
                
                // Fix remote station window
                classType = assembly.GetType("DSPTransportStat.Plugin+Patch_UIStationWindow");
                harmony.Patch(classType.GetMethod("OpenStationWindowOfAnyStation"), null, new HarmonyMethod(typeof(DSPTransportStat_Patch).GetMethod("OpenStationWindowOfAnyStation_Postfix")));
                harmony.PatchAll(typeof(DSPTransportStat_Patch));
                
                NC_StationStorageData.OnReceive += OnReceive;

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }
        
        public static void OnReceive()
        {
            var plugin = instance as DSPTransportStat.Plugin;
            if (plugin.uiTransportStationsWindow.active)
            {
                supression = true;
                plugin.uiTransportStationsWindow.ComputeTransportStationsWindow_LoadStations();
                supression = false;
            }
        }
        
        public static void LoadStations(UITransportStationsWindow __instance)
        {
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
            {
                return;
            }

            bool toggleInterstellar = __instance.uiTSWParameterPanel.ToggleInterstellar;
            bool toggleCollector = __instance.uiTSWParameterPanel.ToggleCollector;
            int relatedItemFilter = __instance.uiTSWParameterPanel.RelatedItemFilter;
            int count = 0;

            for (int k = 1; k < GameMain.data.galacticTransport.stationCursor; ++k)
            {
                StationComponent station = GameMain.data.galacticTransport.stationPool[k];

                // 已刪除, 還沒載入的物流塔跳過
                if (station == null || station.storage == null)
                {
                    continue;
                }

                // 当地星球的物流塔已经载入过了
                if (station.planetId == GameMain.localPlanet?.id)
                {
                    continue;
                }

                // 行星内物流站不會在此

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

                __instance.stations.Add(new StationInfoBundle(star, planet, station));
                count++;
            }
            Log.Dev($"Add {count} ILS stations");
            __instance.OnSort();
            __instance.contentRectTransform.offsetMin = new Vector2(0, -DSPTransportStat.Global.Constants.TRANSPORT_STATIONS_ENTRY_HEIGHT * __instance.stations.Count);
            __instance.contentRectTransform.offsetMax = new Vector2(0, 0);
            __instance.uiStationCountInListTranslation.SetNumber(__instance.stations.Count);

            if (!supression)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_StationStorageRequest());
        }       

        public static void OpenStationWindowOfAnyStation_Postfix(PlanetFactory factory)
        {
            //add open() and supress its content to call nebula patch OnOpen
            supression = true;
            UIRoot.instance.uiGame.stationWindow._OnOpen();
            supression = false;

            if (factory == null)
            {
                UIMessageBox.Show("ACCESS DENY", "Can't open remote ILS on planet not loaded yet", "确定".Translate(), 3);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnOpen))]
        public static bool _OnOpen_Prefix()
        {
            return !supression;
        }
    }
}
