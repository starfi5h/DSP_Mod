using System;

namespace AlterTickrate
{
    public static class Parameters
    {
        public static int FacilityUpdatePeriod = 1;
        public static int InserterUpdatePeriod = 1;
        public static int StorageUpdatePeriod = 1;
        public static int BeltUpdatePeriod = 1;

        public static float FacilitySpeedRate = 1.0f;
        public static float InserterSpeedRate = 1.0f;
        public static int InserterWaitPeriod = 1;

        public static PlanetFactory AnimOnlyFactory = null;
        public static CargoContainer LocalCargoContainer = null;

        public static void SetValues(int facilityPeriod, int inserterPeriod, int storageUpdatePeriod, int beltUpdatePeriod)
        {
            FacilityUpdatePeriod = facilityPeriod;
            FacilitySpeedRate = facilityPeriod;

            InserterUpdatePeriod = inserterPeriod;
            InserterSpeedRate = inserterPeriod;
            InserterWaitPeriod = (facilityPeriod / inserterPeriod + (facilityPeriod % inserterPeriod > 0 ? 1 : 0)) * inserterPeriod;

            StorageUpdatePeriod = storageUpdatePeriod;

            BeltUpdatePeriod = Math.Min(beltUpdatePeriod, 2); // Max allowance: 2
        }
    }
}
