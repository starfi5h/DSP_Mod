using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MimicSimulation
{
    public partial class FactoryData
    {
        readonly Dictionary<int, StationData> stationDict = new Dictionary<int, StationData>();

        public void StationBeforeTransport()
        {
            if (!IsActive)
                Traversal(StationData.IdleBegin);
        }

        public void StationAfterTransport()
        {
            if (IsActive)
                Traversal(StationData.ActiveBegin);
        }

        public void StationAfterTick()
        {
            if (IsActive)
                Traversal(StationData.ActiveEnd);
            else
                Traversal(StationData.IdleEnd);
        }

        private void Traversal(Action<StationData, StationComponent> action)
        {
            PlanetTransport transport = Factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (transport.stationPool[stationId] != null)
                {
                    if (!stationDict.ContainsKey(stationId))
                        stationDict.Add(stationId, new StationData(transport.stationPool[stationId]));
                    action(stationDict[stationId], transport.stationPool[stationId]);
                }
            }
        }
    }

    public class StationData
    {
        readonly int[] tmpCount;
        readonly int[] tmpInc;
        long energy;

        public StationData (StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            tmpCount = new int[length];
            tmpInc = new int[length];
        }

        public static void ActiveBegin(StationData data, StationComponent staion)
        {
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                data.tmpCount[i] = staion.storage[i].count;
                data.tmpInc[i] = staion.storage[i].inc;
            }
        }

        public static void ActiveEnd(StationData data, StationComponent staion)
        {
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                data.tmpCount[i] = staion.storage[i].count - data.tmpCount[i];
                data.tmpInc[i] = staion.storage[i].inc - data.tmpInc[i];
            }
        }

        public static void IdleBegin(StationData data, StationComponent staion)
        {
            data.energy = staion.energy;
        }

        public static void IdleEnd(StationData data, StationComponent staion)
        {
            staion.energy = data.energy;
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                staion.storage[i].count += data.tmpCount[i];
                staion.storage[i].inc += data.tmpInc[i];
                staion.storage[i].count = Math.Max(staion.storage[i].count, 0);
                staion.storage[i].inc = Math.Max(staion.storage[i].inc, 0);
            }
        }
    }
}
