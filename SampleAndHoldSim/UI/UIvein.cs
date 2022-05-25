using HarmonyLib;

namespace SampleAndHoldSim
{
    class UIvein
    {
        public static bool UnitPerMinute = false;
        public static int ViewFactoryIndex = -1;
        
        static int[,] periodArray = null;
        static int[] sumArray;
        const int PEROID = 30;
        const int SETP = 60;
        static int cursor;

        // Due to there is random seed in MinerComponent, sliding window can't get accurate results
        [HarmonyPrefix, HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static void OnUpdate_Prefix(UIVeinDetailNode __instance, out long __state)
        {
            __state = __instance.showingAmount;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static void OnUpdate_Postfix(UIVeinDetailNode __instance, long __state)
        {
            if (__instance.counter % 4 == 1)
            {
                if (ViewFactoryIndex != (GameMain.localPlanet?.factory.index ?? -1))
                {
                    periodArray = new int[PEROID + 1, GameMain.localPlanet.factory.planet.veinGroups.Length];
                    sumArray = new int[GameMain.localPlanet.factory.planet.veinGroups.Length];
                    ViewFactoryIndex = GameMain.localPlanet.factory.index;
                    cursor = 0;
                }
                float rate = GetVeinGroupChangeRate(__instance.veinGroupIndex);
                if (__instance.showingAmount != __state)
                {
                    __instance.infoText.text += GetRateString(rate);
                }
                else
                {
                    string str = __instance.infoText.text;
                    int index = str.LastIndexOf("\n-");
                    if (index > 0)
                        str = str.Remove(index);
                    if (rate > 0)
                        str += GetRateString(rate);
                    __instance.infoText.text = str;
                }
            }
        }

        static string GetRateString(float rate)
        {
            if (UnitPerMinute)
                return string.Format("\n- {0:0} /min", rate * 60);
            else
                return string.Format("\n- {0:0.0} /s", rate);
        }

        public static void AdvanceCursor()
        {
            if (GameMain.gameTick % SETP == 0)
            {
                for (int i = 0; i < sumArray.Length; i++)
                {
                    // sliding window: replace old value with new value
                    sumArray[i] += -periodArray[cursor, i] + periodArray[PEROID, i];
                    periodArray[cursor, i] = periodArray[PEROID, i];
                    periodArray[PEROID, i] = 0;
                }
                cursor = (cursor + 1) % PEROID;
            }            
        }

        public static void Record(int groupIndex, int amount)
        {
            periodArray[PEROID, groupIndex] += amount;
        }

        public static float GetVeinGroupChangeRate(int groupIndex)
        {
            if (sumArray == null)
                return 0;
            return sumArray[groupIndex] * 60f / PEROID / SETP;
        }
    }
}
