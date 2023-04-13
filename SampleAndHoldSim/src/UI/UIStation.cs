using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SampleAndHoldSim
{
    class UIstation
    {
        public static bool UnitPerMinute = false;
        public static int ViewFactoryIndex = -1;
        public static int VeiwStationId = -1;
        static Text[] changeRateText;

        static int[,] periodArray;
        static int[] sumArray;
        public static int Period = 60;
        public const int STEP = 10;
        static int time;
        static int cursor;
        static int counter;        

        [HarmonyPostfix, HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
        public static void UIStationWindow_OnOpen(UIStationWindow __instance)
        {
            SetVeiwStation(__instance.factory.index, __instance.stationId, __instance.storageUIs.Length);
            if (changeRateText == null || changeRateText.Length < __instance.storageUIs.Length)
            {
                changeRateText = new Text[__instance.storageUIs.Length];
                for (int i = 0; i < __instance.storageUIs.Length; i++)
                {
                    var obj = __instance.storageUIs[i].countValueText.gameObject;
                    var tmp = GameObject.Instantiate(obj, obj.transform.parent);
                    tmp.name = "SAHS-changeRate";
                    tmp.transform.localPosition = new Vector3(150, 8.5f, 0);
                    changeRateText[i] = tmp.GetComponent<Text>();
                    changeRateText[i].alignment = TextAnchor.MiddleLeft;
                }
            }
            for (int i = 0; i < changeRateText.Length; i++)
            {
                changeRateText[i].gameObject.SetActive(false);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), "_OnClose")]
        public static void UIStationWindow_OnClose()
        {
            SetVeiwStation(-1, -1, 0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        public static void UIStationWindow_OnUpdate(UIStationWindow __instance)
        {
            if (GameMain.gameTick % 10 != 0)
                return;
            if (__instance.stationId != VeiwStationId)
            {
                UIStationWindow_OnOpen(__instance);
                return;
            }

            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                if (__instance.storageUIs[i].station != null)
                {
                    float rate = GetStorageChangeRate(i);
                    if (rate != 0)
                    {
                        changeRateText[i].text = GetRateString(rate);
                        if (!changeRateText[i].gameObject.activeSelf)
                            changeRateText[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        if (changeRateText[i].gameObject.activeSelf)
                            changeRateText[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        static float GetStorageChangeRate(int storageIndex)
        {
            if (periodArray == null || storageIndex >= periodArray.Length || time < 1)
                return 0;
            return sumArray[storageIndex] * 60f / time / STEP;
        }

        static string GetRateString(float rate)
        {
            if (UnitPerMinute)
                return string.Format("{0:+0.0;-0.0} /min", rate * 60);
            else
                return string.Format("{0:+0.00;-0.00} /s", rate);
        }

        public static void OnDestory()
        {
            if (changeRateText == null) return;
            foreach (var text in changeRateText)
                GameObject.Destroy(text.gameObject);
        }

        public static void Record(StationData data)
        {
            // Reset record array if veinGroups length change
            if (sumArray.Length < data.tmpCount.Length)
            {
                Log.Warn($"UIstation.Record: length: {sumArray.Length} -> {data.tmpCount.Length}");
                periodArray = new int[Period + 1, data.tmpCount.Length];
                sumArray = new int[data.tmpCount.Length];
                cursor = 0;
                counter = 0;
            }

            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                // collect item count change in SETP ticks
                periodArray[Period, i] += data.tmpCount[i];
            }
            if (++counter >= STEP)
            {
                for (int i = 0; i < data.tmpCount.Length; i++)
                {
                    // sliding window: replace old value with new value
                    sumArray[i] += -periodArray[cursor, i] + periodArray[Period, i];
                    periodArray[cursor, i] = periodArray[Period, i];
                    periodArray[Period, i] = 0;
                }
                cursor = (cursor + 1) % Period;
                time = time < Period ? time + 1 : Period;
                counter = 0;
            }
        }

        public static void SetVeiwStation(int factoryId, int stationId, int length)
        {
            // If UI station is disabled, return
            if (Period == 0)
                return;

            if (ViewFactoryIndex != factoryId || VeiwStationId != stationId)
            {
                ViewFactoryIndex = factoryId;
                VeiwStationId = stationId;
                if (length > 0)
                {
                    periodArray = new int[Period + 1, length];
                    sumArray = new int[length];
                }
                else
                {
                    periodArray = null;
                    sumArray = null;
                }
                time = 0;
                cursor = 0;
                counter = 0;
            }
        }
    }
}
