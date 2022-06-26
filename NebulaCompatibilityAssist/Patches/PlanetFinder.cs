using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.UI;

namespace NebulaCompatibilityAssist.Patches
{
    public static class PlanetFinder
    {
        public const string NAME = "PlanetFinder";
        public const string GUID = "com.hetima.dsp.PlanetFinder";
        public const string VERSION = "0.4.2";

        public struct PlanetInfo
        {
            public long energyCapacity;
            public long energyRequired;
            public long energyExchanged;
            public int networkCount;
        }
        private static Dictionary<int, PlanetInfo> planetInfos = null;
        private static StringBuilder sbWatt = null;
        private static StringBuilder sbText = null;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                planetInfos = new();
                sbWatt = new StringBuilder("         W", 12);
                sbText = new();
                
                Type classType = assembly.GetType("PlanetFinderMod.UIPlanetFinderWindow");
                // Send request when client open window
                harmony.Patch(AccessTools.Method(classType, "SetUpAndOpen"), new HarmonyMethod(typeof(PlanetFinder).GetMethod("SendRequest")));
                // Show HasFactory list on client
                harmony.Patch(classType.GetMethod("PlanetsWithFactory"), new HarmonyMethod(typeof(PlanetFinder).GetMethod("PlanetsWithFactory_Prefix")));
                                
                classType = assembly.GetType("PlanetFinderMod.UIPlanetFinderListItem");
                // Show power network information on client
                harmony.Patch(classType.GetMethod("RefreshValues"), null, new HarmonyMethod(typeof(PlanetFinder).GetMethod("RefreshValues_Postfix")));

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
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_PlanetInfoRequest());
            }
        }

        public static void OnReceive(NC_PlanetInfoData packet)
        {
            Log.Dev("NC_PlanetInfoData: " + packet.PlanetId);
            planetInfos[packet.PlanetId] = new PlanetInfo
            {
                energyCapacity = packet.EnergyCapacity,
                energyRequired = packet.EnergyRequired,
                energyExchanged = packet.EnergyExchanged,
                networkCount = packet.NetworkCount
            };
        }

        public static void RefreshValues_Postfix(bool shown, bool onlyNewlyEmerged, int ____itemId, PlanetData ___planetData, Text ___valueText, Text ___valueSketchText)
        {
            if (!shown || onlyNewlyEmerged || ____itemId != 0)
                return;

            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                return;

            if (___planetData.factory == null && planetInfos.ContainsKey(___planetData.id))
            {
                long energyRequired = planetInfos[___planetData.id].energyRequired;
                long energyCapacity = planetInfos[___planetData.id].energyCapacity;
                long energyX = -planetInfos[___planetData.id].energyExchanged;
                int networkCount = planetInfos[___planetData.id].networkCount;
                
                StringBuilderUtility.WriteKMG(sbWatt, 8, energyRequired * 60L, false);
                sbText.Append(sbWatt);
                StringBuilderUtility.WriteKMG(sbWatt, 8, energyCapacity * 60L, false);
                sbText.Append(" / ").Append(sbWatt.ToString().Trim());
                if (energyX > 0L)
                {
                    StringBuilderUtility.WriteKMG(sbWatt, 8, energyX * 60L, false);
                    sbText.Append(" + ").Append(sbWatt.ToString().Trim());
                }
                else
                {
                    energyX = 0;
                }
                float ratio = (float)energyRequired / (energyCapacity + energyX);
                if (ratio > 0.9f)
                {
                    sbText.Append(" (").Append(ratio.ToString("P1")).Append(")");
                    sbText.Insert(0, (ratio > 0.99f) ? "<color=#FF404D99>" : "<color=#DB883E85>");
                    sbText.Append("</color>");
                }
                ___valueText.text = sbText.ToString();
                sbText.Clear();

                if (networkCount > 1)
                    ___valueSketchText.text = "(" + networkCount + ")";
            }
        }

        public static bool PlanetsWithFactory_Prefix(ref List<PlanetData> __result)
        {
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                return true;

            __result = new List<PlanetData>(planetInfos.Count);
            foreach (int planetId in planetInfos.Keys)
                __result.Add(GameMain.galaxy.PlanetById(planetId));

            return false;
        }
    }
}
