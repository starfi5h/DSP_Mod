using System;
using System.Collections.Generic;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {
        readonly Dictionary<int, StationData> stationDict = new Dictionary<int, StationData>();

        public void StationAfterTransport()
        {
            if (IsActive)
                Traversal(StationData.ActiveBegin, false);
        }

        public void StationAfterTick()
        {
            if (IsActive)
                Traversal(StationData.ActiveEnd, Index == UIStation.ViewFactoryIndex);
            else
                Traversal(StationData.IdleEnd, false);
        }

        private void Traversal(Action<StationData, StationComponent> action, bool record)
        {
            PlanetTransport transport = factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (transport.stationPool[stationId] != null)
                {
                    if (!stationDict.ContainsKey(stationId))
                        stationDict.Add(stationId, new StationData(transport.stationPool[stationId]));
                    action(stationDict[stationId], transport.stationPool[stationId]);
                    if (record && stationId == UIStation.VeiwStationId)
                        UIStation.Record(stationDict[stationId]);
                }
            }
        }
    }

    public class StationData
    {
        public readonly int[] tmpCount;
        readonly int[] tmpInc;
        int tmpWarperCount;

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
            data.tmpWarperCount = staion.warperCount;
        }

        public static void ActiveEnd(StationData data, StationComponent staion)
        {
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                data.tmpCount[i] = staion.storage[i].count - data.tmpCount[i];
                data.tmpInc[i] = staion.storage[i].inc - data.tmpInc[i];
            }
            data.tmpWarperCount = staion.warperCount - data.tmpWarperCount;
        }

        public static void IdleEnd(StationData data, StationComponent staion)
        {
            for (int i = 0; i < data.tmpCount.Length; i++)
            {
                staion.storage[i].count += data.tmpCount[i];
                staion.storage[i].inc += data.tmpInc[i];
                staion.storage[i].count = Math.Max(staion.storage[i].count, 0);
                staion.storage[i].inc = Math.Max(staion.storage[i].inc, 0);
            }
            staion.warperCount += data.tmpWarperCount;
        }
    }
}
