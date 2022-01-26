using HarmonyLib;
using System;
using Unity;
using UnityEngine;

namespace Experiment
{
    class StaionUI
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnMaxChargePowerSliderValueChange))]
        internal static bool OnMaxChargePowerSliderValueChange_Prefix()
        {
            //Log.Debug("cHANGE");
            if (Input.GetMouseButtonUp(0))
            {
                Log.Info("UP");
            }
            if (Input.GetMouseButton(0))
            {
                //Log.Info("EXIST");
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnUpdate))]
        internal static void OnUpdate_Prefix(UIStationWindow __instance)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Log.Info("UP");
                __instance.OnStationIdChange();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnOpen))]
        public static void _OnOpen_Postfix(UIStationWindow __instance)
        {
            //Hide UI elements until sync data arrive
            Log.Info("OnOpen");
            __instance.titleText.text = "Loading...";
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                __instance.storageUIs[i]._Close();
                __instance.storageUIs[i].ClosePopMenu();
            }
            __instance.panelDown.SetActive(false);
            Log.Info($"PlanetId {__instance.factory.planetId} {__instance.transport.stationPool[__instance.stationId].planetId}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnStationIdChange))]
        internal static void OnStationIdChange_Postfix(UIStationWindow __instance)
        {
            Log.Info("OnStationIdChange");
            //Log.Debug(Environment.StackTrace);
        }

    }
}
