namespace AlterTickrate
{
    public static class Parameters
    {
        public static int FacilityUpdatePeriod = 1;
        public static int SorterUpdatePeriod = 1;
        public static int StorageUpdatePeriod = 1;

        public static float FacilitySpeedRate = 1.0f;
        public static float InserterSpeedRate = 1.0f;
        public static PlanetFactory AnimOnlyFactory = null;

        public static void SetValues(int facilityPeriod, int inserterPeriod, int storageUpdatePeriod)
        {
            FacilityUpdatePeriod = facilityPeriod;
            FacilitySpeedRate = facilityPeriod;
            SorterUpdatePeriod = inserterPeriod;
            InserterSpeedRate = inserterPeriod;
            StorageUpdatePeriod = storageUpdatePeriod;
        }
    }
}
