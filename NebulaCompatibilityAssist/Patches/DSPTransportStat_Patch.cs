using BepInEx;
using DSPTransportStat;
using DSPTransportStat.Enum;
using DSPTransportStat.Global;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPTransportStat_Patch
    {
        public const string NAME = "DSPTransportStat";
        public const string GUID = "IndexOutOfRange.DSPTransportStat";
        public const string VERSION = "0.0.17";

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
            // 客户端已经载入的星球工厂
            HashSet<int> loadedPlanets = new();
            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                loadedPlanets.Add(GameMain.data.factories[i].planetId);
            }
            int count = 0;

            bool toggleInterstellar = __instance.uiTSWParameterPanel.ToggleInterstellar;
            bool toggleCollector = __instance.uiTSWParameterPanel.ToggleCollector;
            int relatedItemFilter = __instance.uiTSWParameterPanel.RelatedItemFilter;
            StorageUsageTypeFilter storageUsageType = __instance.uiTSWParameterPanel.StorageUsageTypeFilter;
            StorageUsageDirectionFilter storageUsageDirection = __instance.uiTSWParameterPanel.StorageUsageDirectionFilter;

            for (int k = 1; k < GameMain.data.galacticTransport.stationCursor; ++k)
            {
                StationComponent station = GameMain.data.galacticTransport.stationPool[k];

                // 跳过已删除或尚未载入的物流塔
                if (station == null || station.storage == null)
                {
                    continue;
                }

                // 当地星球的物流塔已经载入过了
                if (loadedPlanets.Contains(station.planetId))
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

                if (!string.IsNullOrWhiteSpace(__instance.searchString) &&
                    star.name.IndexOf(__instance.searchString, StringComparison.CurrentCultureIgnoreCase) == -1 &&
                    planet.name.IndexOf(__instance.searchString, StringComparison.CurrentCultureIgnoreCase) == -1 &&
                    name.IndexOf(__instance.searchString, StringComparison.CurrentCultureIgnoreCase) == -1
                )
                {
                    continue;
                }

                // 过滤相关物品
                if (relatedItemFilter != Constants.NONE_ITEM_ID)
                {
                    // 该站点至少有一个槽位包含用户选择的物品
                    int ii = 0;
                    for (; ii < station.storage.Length; ++ii)
                    {
                        if (station.storage[ii].itemId == relatedItemFilter)
                        {
                            StationStore ss = station.storage[ii];
                            // 对 usage type 和 usage direction 进行过滤
                            if (storageUsageDirection == StorageUsageDirectionFilter.Supply)
                            {
                                // usage direction 为 supply
                                if (storageUsageType == StorageUsageTypeFilter.All && ss.localLogic != ELogisticStorage.Supply && ss.remoteLogic != ELogisticStorage.Supply)
                                {
                                    // usage type 为 all 但是 local logic 和 remote logic 都不为 supply
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Local && ss.localLogic != ELogisticStorage.Supply)
                                {
                                    // usage type 为 local 但是 local logic 不为 supply
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Remote && ss.remoteLogic != ELogisticStorage.Supply)
                                {
                                    // usage type 为 remote 但是 remote logic 不为 supply
                                    continue;
                                }
                            }
                            else if (storageUsageDirection == StorageUsageDirectionFilter.Demand)
                            {
                                // usage direction 为 demand
                                if (storageUsageType == StorageUsageTypeFilter.All && ss.localLogic != ELogisticStorage.Demand && ss.remoteLogic != ELogisticStorage.Demand)
                                {
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Local && ss.localLogic != ELogisticStorage.Demand)
                                {
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Remote && ss.remoteLogic != ELogisticStorage.Demand)
                                {
                                    continue;
                                }
                            }
                            else if (storageUsageDirection == StorageUsageDirectionFilter.Storage)
                            {
                                // usage direction 为 storage
                                if (storageUsageType == StorageUsageTypeFilter.All && ss.localLogic != ELogisticStorage.None && ss.remoteLogic != ELogisticStorage.None)
                                {
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Local && ss.localLogic != ELogisticStorage.None)
                                {
                                    continue;
                                }
                                if (storageUsageType == StorageUsageTypeFilter.Remote && ss.remoteLogic != ELogisticStorage.None)
                                {
                                    continue;
                                }
                            }
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
        public static bool OnOpen_Prefix()
        {
            return !supression;
        }
    }
}
