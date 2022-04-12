using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SampleAndHoldSim
{
    class UIStation
    {
        public static int ViewFactoryIndex = -1;
        public static int VeiwStationId = -1;
        static Text[] changeRateText;

        static int[,] periodArray;
        const int PEROID = 240;
        static int time;
        static int cursor;
        

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
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), "_OnClose")]
        public static void UIStationWindow_OnClose(UIStationWindow __instance)
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
                    changeRateText[i].text = $"{GetStorageChangeRate(i):+0.00;-0.00} /s";
                }
            }
        }
        public static void OnDestory()
        {
            if (changeRateText == null) return;
            foreach (var text in changeRateText)
                GameObject.Destroy(text.gameObject);
        }

        public static void Record(StationData data)
        {
            cursor = (cursor + 1) % PEROID;
            time = time < PEROID ? time + 1 : PEROID;
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                periodArray[PEROID, i] += data.tmpCount[i] - periodArray[cursor, i];
                periodArray[cursor, i] = data.tmpCount[i];
            }
        }

        public static void SetVeiwStation(int factoryId, int stationId, int length)
        {
            if (ViewFactoryIndex != factoryId || VeiwStationId != stationId)
            {
                ViewFactoryIndex = factoryId;
                VeiwStationId = stationId;
                if (length > 0)
                    periodArray = new int[PEROID + 1, length];
                else
                    periodArray = null;
                time = 0;
            }
        }

        public static float GetStorageChangeRate(int storageIndex)
        {
            if (periodArray == null || time < 10)
                return 0;
            return periodArray[PEROID, storageIndex] * 60f / (time / 10 * 10);
        }
    }
}
