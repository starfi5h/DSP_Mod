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


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnOpen))]
        public static void _OnOpen_Postfix1(UIStationWindow __instance)
        {
            //Hide UI elements until sync data arrive
            Log.Info("OnOpen_Postfix");
            __instance.titleText.text = "Loading...";
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                __instance.storageUIs[i]._Close();
                __instance.storageUIs[i].ClosePopMenu();
            }
            __instance.panelDown.SetActive(false);
            Log.Info($"PlanetId {__instance.factory.planetId} {__instance.transport.stationPool[__instance.stationId].planetId}");
        }

        static bool[] HasChanged;
        static bool eventLock;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnOpen))]
        public static void _OnOpen_Postfix2(UIStationWindow __instance)
        {
            Log.Info("OnOpen_Postfix2");

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            int storageCount = ((stationComponent.isCollector || stationComponent.isVeinCollector) ? stationComponent.collectionIds.Length : stationComponent.storage.Length);

            HasChanged = new bool[__instance.storageUIs.Length];
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage.OnMaxSliderValueChange))]
        public static bool OnMaxSliderValueChangePrefix(UIStationStorage __instance, float val)
        {            
            if (!eventLock) 
            {
                Log.Debug(__instance.index);
                // If the silder value isn't set to the same with storage data, mark it
                HasChanged[__instance.index] = val != (float)(__instance.station.storage[__instance.index].max / 100);
            }
            return false;
        }        

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage._OnUpdate))]
        public static void _OnUpdate_Prefix(UIStationStorage __instance, ref float __state)
        {
            // Set up eventLock so value changes in maxSlider.value don't trigger changed check
            eventLock = true;
            __state = __instance.maxSlider.value;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage._OnUpdate))]
        public static void _OnUpdate_Postfix(UIStationStorage __instance, float __state)
        {            
            // Restore the silder value so it is not modified by RefreshValues()
            __instance.maxSlider.value = __state;
            // Make text reflect the change of silder value
            __instance.maxValueText.text = ((int)(__instance.maxSlider.value * 100)).ToString();
            eventLock = false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), nameof(UIStationWindow._OnUpdate))]
        public static void OnUpdate_Prefix(UIStationWindow __instance)
        {
            if (Input.GetMouseButtonUp(0))
            {
                Log.Info("UP");
                //Check if slider values are changed by the user
                for (int index = 0; index < __instance.storageUIs.Length; index++)
                {
                    if (HasChanged[index])
                    {
                        Log.Warn(index);
                        //Do the job in OnMaxSliderValueChange()
                        StationStore stationStore = __instance.transport.stationPool[__instance.stationId].storage[index];
                        float val = __instance.storageUIs[index].maxSlider.value;
                        //__instance.transport.SetStationStorage(__instance.stationId, index, stationStore.itemId, (int)(val * 100f + 0.5f), stationStore.localLogic, stationStore.remoteLogic, GameMain.mainPlayer);
                        HasChanged[index] = false;
                    }
                }
            }
        }

    }
}
