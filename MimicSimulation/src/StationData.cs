using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MimicSimulation
{
    public static class StationPool
    {
        static readonly ConcurrentDictionary<int, Dictionary<int, StationData>> factroyDict = new ConcurrentDictionary<int, Dictionary<int, StationData>>();

        public static void Before(int index)
        {
            if (!factroyDict.ContainsKey(index))
                factroyDict.TryAdd(index, new Dictionary<int, StationData>());
            var dict = factroyDict[index];
            PlanetTransport transport = GameMain.data.factories[index].transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (!dict.ContainsKey(stationId))
                    dict.Add(stationId, new StationData(transport.stationPool[stationId]));
                dict[stationId].Before(transport.stationPool[stationId]);
            }
        }

        public static void After(int index)
        {
            if (!factroyDict.TryGetValue(index, out Dictionary<int, StationData> dict))
                return;
            PlanetTransport transport = GameMain.data.factories[index].transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (dict.ContainsKey(stationId))
                    dict[stationId].After(transport.stationPool[stationId]);
            }
        }

        public static void IdleTick(int index)
        {
            if (!factroyDict.TryGetValue(index, out Dictionary<int, StationData> dict))
                return;
            PlanetTransport transport = GameMain.data.factories[index].transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (dict.ContainsKey(stationId))
                    dict[stationId].IdleTick(transport.stationPool[stationId]);
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
