using HarmonyLib;

namespace SampleAndHoldSim
{
    class UIvein
    {
        public static int ViewFactoryIndex = -1;
        
        static int[,] periodArray = null;
        const int PEROID = 600;
        static int cursor;

        // Due to there is random seed in MinerComponent, sliding window can't get accurate results
        /*
        [HarmonyPrefix, HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static void OnUpdate_Prefix(UIVeinDetailNode __instance, out long __state)
        {
            __state = __instance.showingAmount;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIVeinDetailNode), "_OnUpdate")]
        public static void OnUpdate_Postfix(UIVeinDetailNode __instance, long __state)
        {
            if (__instance.showingAmount != __state)
            {
                if (ViewFactoryIndex != (GameMain.localPlanet?.factory.index ?? -1))
                {
                    periodArray = new int[PEROID + 1, GameMain.localPlanet.factory.planet.veinGroups.Length];
                    ViewFactoryIndex = GameMain.localPlanet.factory.index;
                    cursor = 0;
                }
                __instance.infoText.text += $"\nMiningRate: {GetVeinGroupChangeRate(__instance.veinGroupIndex):+0.00;-0.00} /s";
            }
        }
        */

        public static void AdvanceCursor()
        {            
            cursor = (cursor + 1) % PEROID;
            for (int i = 0; i < periodArray.GetLength(1); i++)
            {
                periodArray[PEROID, i] -= periodArray[cursor, i];
                periodArray[cursor, i] = 0;
            }
            
        }

        public static void Record(int groupIndex, int amount)
        {
            periodArray[PEROID, groupIndex] += amount;
            periodArray[cursor, groupIndex] += amount;
        }

        public static float GetVeinGroupChangeRate(int groupIndex)
        {
            if (periodArray == null)
                return 0;
            return periodArray[PEROID, groupIndex] * (60f / PEROID);
        }
    }
}
