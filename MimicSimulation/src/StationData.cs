using System;

namespace MimicSimulation
{
    class StationData
    {
        readonly int[] tmpCount;
        readonly int[] tmpInc;

        public StationData (StationComponent staion)
        {
            tmpCount = new int[staion.storage.Length];
            tmpInc = new int[staion.storage.Length];
        }

        public void Before(StationComponent staion)
        {
            for (int i = 0; i < staion.storage.Length; i++)
            {
                tmpCount[i] = staion.storage[i].count;
                tmpInc[i] = staion.storage[i].inc;
            }
        }

        public void After(StationComponent staion)
        {
            for (int i = 0; i < staion.storage.Length; i++)
            {
                tmpCount[i] = staion.storage[i].count - tmpCount[i];
                tmpInc[i] = staion.storage[i].inc - tmpInc[i];
            }
        }

        public void GameTick(StationComponent staion)
        {
            for (int i = 0; i < staion.storage.Length; i++)
            {
                staion.storage[i].count += tmpCount[i];
                staion.storage[i].inc += tmpInc[i];
                staion.storage[i].count = Math.Max(staion.storage[i].count, 0);
                staion.storage[i].inc = Math.Max(staion.storage[i].inc, 0);
            }
        }
    }
}
