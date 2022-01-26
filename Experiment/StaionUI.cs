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
            Log.Debug("change");
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





        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow.OnStationIdChange))]
        internal static void OnStationIdChange_Postfix(UIStationWindow __instance)
        {
            Log.Info("OnStationIdChange");
            //Log.Debug(Environment.StackTrace);
        }

        static bool canPass = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnMaxSliderValueChange))]
        internal static bool OnMaxSliderValueChangePrefix(UIStationStorage __instance, float val)
        {
            //Log.Info($"OnMaxSliderValueChange {val}");
            //Log.Debug(Environment.StackTrace);
            return canPass;
        }

        static bool[] HasChanged;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage._OnUpdate))]
        public static void _OnUpdate_Prefix(UIStationStorage __instance, ref float __state)
        {
            __state = __instance.maxSlider.value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage._OnUpdate))]
        public static void _OnUpdate_Postfix(UIStationStorage __instance, float __state)
        {
            HasChanged[__instance.index] = __instance.maxSlider.value != __state;
            __instance.maxSlider.value = __state;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnOpen))]
        public static void _OnOpen_Postfix(UIStationWindow __instance)
        {
            //Hide UI elements until sync data arrive
            Log.Info("OnOpen");
            //__instance.titleText.text = "Loading...";
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                //__instance.storageUIs[i]._Close();
                //__instance.storageUIs[i].ClosePopMenu();
            }
            //__instance.panelDown.SetActive(false);
            HasChanged = new bool[__instance.storageUIs.Length];
            Log.Info($"PlanetId {__instance.factory.planetId} {__instance.transport.stationPool[__instance.stationId].planetId}");
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnUpdate))]
        public static void OnUpdate_Prefix(UIStationWindow __instance)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Log.Info("UP");
                //Check storage max value changes
                for (int index = 0; index < __instance.storageUIs.Length; index++)
                {
                    if (HasChanged[index])
                    {
                        Log.Debug(index);
                        //Do the work in OnMaxSliderValueChange()
                        StationStore stationStore = __instance.transport.stationPool[__instance.stationId].storage[index];
                        float val = __instance.storageUIs[index].maxSlider.value;
                        __instance.transport.SetStationStorage(__instance.stationId, index, stationStore.itemId, (int)(val * 100f + 0.5f), stationStore.localLogic, stationStore.remoteLogic, GameMain.mainPlayer);
                        HasChanged[index] = false;
                    }
                }
            }
        }

    }
}
