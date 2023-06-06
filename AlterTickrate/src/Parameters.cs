using System;

namespace AlterTickrate
{
    public static class Parameters
    {
        public static int PowerUpdatePeriod = 1;
        public static int FacilityUpdatePeriod = 1;
        public static int LabProduceUpdatePeriod = 1;
        public static int LabResearchUpdatePeriod = 1;
        public static int LabLiftUpdatePeriod = 1;
        public static int InserterUpdatePeriod = 1;
        public static int StorageUpdatePeriod = 1;
        public static int BeltUpdatePeriod = 1;

        public static float FacilitySpeedRate = 1.0f;
        public static float InserterSpeedRate = 1.0f;

        const int MaxBeltUpdatePeriod = 2;

        public static void SetFacilityValues(int powerPeriod, int facilityPeriod)
        {
            PowerUpdatePeriod = powerPeriod;
            FacilityUpdatePeriod = facilityPeriod;
            FacilitySpeedRate = facilityPeriod;
        }

        public static void SetLabValues(int produceLabPeriod, int researchLabPeriod, int liftLabPeriod)
        {
            LabProduceUpdatePeriod = produceLabPeriod;
            LabResearchUpdatePeriod = researchLabPeriod;
            LabLiftUpdatePeriod = liftLabPeriod;
        }

        public static void SetBeltValues(int inserterPeriod, int storageUpdatePeriod, int beltUpdatePeriod)
        {
            InserterUpdatePeriod = inserterPeriod;
            InserterSpeedRate = inserterPeriod;
            StorageUpdatePeriod = storageUpdatePeriod;
            BeltUpdatePeriod = Math.Min(beltUpdatePeriod, MaxBeltUpdatePeriod);
        }
    }
}
