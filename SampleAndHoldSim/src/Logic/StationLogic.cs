using System;
using System.Collections.Generic;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {
        readonly Dictionary<int, StationData> stationDict = new Dictionary<int, StationData>();

        public void SetMineral(StationComponent station, int mineralCount)
        {
            if (!stationDict.TryGetValue(station.id, out var stationData))
            {
                stationDict.Add(station.id, new StationData(station));
                stationData = stationDict[station.id];
            }
            StationData.SetMineral(stationData, mineralCount);
        }

        public void StationAfterTransport()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveBegin, false);
            }
        }

        public void StationAfterTick()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveEnd, Index == UIstation.ViewFactoryIndex);
            }
            else
            {
                Traversal(StationData.IdleEnd, false);
            }
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

        public static void SetMineral(StationData data, int mineralCount)
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
            // tmpMineralCount is set inside PlanetTransport.GameTick()
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

                // DEBUG for abnormal change
                // if (data.tmpCount[i] >= 1000)
                //    Log.Debug($"station: {station.planetId} - {station.id} [{i}] itemId:{station.storage[i].itemId} diff:{data.tmpCount[i]}");
                
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
                if (station.storage[i].count < MainManager.StationStoreLowerbound)
                {
                    Log.Debug($"station{station.id} - store{i}: {station.storage[i].count}");
                    station.storage[i].count = MainManager.StationStoreLowerbound;
                }
            }
            if (station.isVeinCollector && data.tmpMineralCount > 0)
                station.storage[0].count += data.tmpMineralCount;
            else
                station.warperCount += data.tmpWarperCount;
        }
    }
}
