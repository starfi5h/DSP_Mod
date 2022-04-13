using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MimicSimulation
{
    public partial class FactoryData
    {
        readonly Dictionary<int, StationData> stationDict = new Dictionary<int, StationData>();

        public void StationStorageBegin()
        {
            PlanetTransport transport = Factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (!stationDict.ContainsKey(stationId))
                    stationDict.Add(stationId, new StationData(transport.stationPool[stationId]));
                stationDict[stationId].Begin(transport.stationPool[stationId]);
            }
        }

        public void StationStorageEnd()
        {
            PlanetTransport transport = Factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (stationDict.ContainsKey(stationId))
                    stationDict[stationId].End(transport.stationPool[stationId]);
            }
        }

        public void StationIdleTick()
        {
            PlanetTransport transport = Factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (stationDict.ContainsKey(stationId))
                    stationDict[stationId].IdleTick(transport.stationPool[stationId]);
            }
        }
    }

    public class StationData
    {
        readonly int[] tmpCount;
        readonly int[] tmpInc;

        public StationData (StationComponent staion)
        {
            tmpCount = new int[staion.storage.Length];
            tmpInc = new int[staion.storage.Length];
        }

        public void Begin(StationComponent staion)
        {
            for (int i = 0; i < staion.storage.Length; i++)
            {
                tmpCount[i] = staion.storage[i].count;
                tmpInc[i] = staion.storage[i].inc;
            }
        }

        public void End(StationComponent staion)
        {
            for (int i = 0; i < staion.storage.Length; i++)
            {
                tmpCount[i] = staion.storage[i].count - tmpCount[i];
                tmpInc[i] = staion.storage[i].inc - tmpInc[i];
            }
        }

        public void IdleTick(StationComponent staion)
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
