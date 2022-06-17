using System;
using System.Collections.Generic;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {
        readonly Dictionary<int, StationData> stationDict = new Dictionary<int, StationData>();

        public void SetMinearl(int stationId, int mineralCount)
        {
            if (IsActive)
            {
                if (!stationDict.ContainsKey(stationId))
                    stationDict.Add(stationId, new StationData(factory.transport.stationPool[stationId]));
                StationData.SetMinearl(stationDict[stationId], mineralCount);
            }
        }

        public void StationAfterTransport()
        {
            if (IsActive)
                Traversal(StationData.ActiveBegin, false);
        }

        public void StationAfterTick()
        {
            if (IsActive)
                Traversal(StationData.ActiveEnd, Index == UIstation.ViewFactoryIndex);
            else
                Traversal(StationData.IdleEnd, false);
        }

        private void Traversal(Action<StationData, StationComponent> action, bool record)
        {
            PlanetTransport transport = factory.transport;
            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                if (transport.stationPool[stationId] != null && transport.stationPool[stationId].id == stationId)
                {
                    if (!stationDict.ContainsKey(stationId))
                        stationDict.Add(stationId, new StationData(transport.stationPool[stationId]));
                    action(stationDict[stationId], transport.stationPool[stationId]);
                    if (record && stationId == UIstation.VeiwStationId)
                        UIstation.Record(stationDict[stationId]);
                }
            }
        }
    }

    public class StationData
    {
        public int[] tmpCount;
        int[] tmpInc;
        int tmpWarperCount;
        int tmpMineralCount;

        public StationData (StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            SetArray(length);
        }

        private void SetArray(int length)
        {
            tmpCount = new int[length];
            tmpInc = new int[length];
        }

        public static void SetMinearl(StationData data, int mineralCount)
        {
            data.tmpMineralCount = mineralCount;
        }

        public static void ActiveBegin(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length)
                data.SetArray(length);
            for (int i = 0; i < length; i++)
            {
                data.tmpCount[i] = station.storage[i].count;
                data.tmpInc[i] = station.storage[i].inc;
            }
            data.tmpWarperCount = station.warperCount;
        }

        public static void ActiveEnd(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length)
                data.SetArray(length);
            for (int i = 0; i < length; i++)
            {
                data.tmpCount[i] = station.storage[i].count - data.tmpCount[i];
                data.tmpInc[i] = station.storage[i].inc - data.tmpInc[i];
            }
            data.tmpWarperCount = station.warperCount - data.tmpWarperCount;
        }

        public static void IdleEnd(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length)
                data.SetArray(length);
            for (int i = 0; i < length; i++)
            {
                station.storage[i].count += data.tmpCount[i];
                station.storage[i].inc += data.tmpInc[i];
                station.storage[i].count = Math.Max(station.storage[i].count, 0);
                station.storage[i].inc = Math.Max(station.storage[i].inc, 0);
            }
            if (station.isVeinCollector)
                station.storage[0].count += data.tmpMineralCount;
            else
                station.warperCount += data.tmpWarperCount;
        }
    }
}
