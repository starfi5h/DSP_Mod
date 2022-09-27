using NebulaAPI;
using System;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_StationStorageData
    {
        public static Action OnReceive;

        public int[] StationGId { get; set; }
        public int[] StorageLength { get; set; }
        public bool[] IsCollector { get; set; }

        public int[] ItemId { get; set; }
        public int[] Max { get; set; }
        public int[] Count { get; set; }
        public int[] Inc { get; set; }
        public int[] RemoteOrder { get; set; }
        public byte[] Logic { get; set; }

        public NC_StationStorageData()
        {
            StationComponent[] gStationPool = GameMain.data.galacticTransport.stationPool;
            List<int> stationGId = new ();
            List<int> storageLength = new ();
            List<bool> isCollector = new ();
            int arraySize = 0;
            int offset = 0;

            foreach (StationComponent stationComponent in gStationPool)
            {
                if (stationComponent != null)
                {
                    stationGId.Add(stationComponent.gid);
                    storageLength.Add(stationComponent.storage.Length);
                    isCollector.Add(stationComponent.isCollector);
                    arraySize += stationComponent.storage.Length;
                }
            }

            StationGId = stationGId.ToArray();
            StorageLength = storageLength.ToArray();
            IsCollector = isCollector.ToArray();
            ItemId = new int[arraySize];
            Max = new int[arraySize];
            Count = new int[arraySize];
            Inc = new int[arraySize];
            RemoteOrder = new int[arraySize];
            Logic = new byte[arraySize];

            for (int i = 0; i < stationGId.Count; i++)
            {
                StationComponent station = gStationPool[stationGId[i]];
                for (int j = 0; j < storageLength[i]; j++)
                {
                    StationStore stationStore = station.storage[j];
                    ItemId[offset + j] = stationStore.itemId;
                    Max[offset + j] = stationStore.max;
                    Count[offset + j] = stationStore.count;
                    Inc[offset + j] = stationStore.inc;
                    RemoteOrder[offset + j] = stationStore.remoteOrder;
                    Logic[offset + j] = (byte)stationStore.remoteLogic;
                }
                offset += storageLength[i];
            }
        }
    }

    [RegisterPacketProcessor]
    internal class NC_StationStorageReponseProcessor : BasePacketProcessor<NC_StationStorageData>
    {
        public override void ProcessPacket(NC_StationStorageData packet, INebulaConnection conn)
        {
            if (IsHost)
                return;

            int offset = 0;
            StationComponent[] gStationPool = GameMain.data.galacticTransport.stationPool;
            for (int i = 0; i < packet.StationGId.Length; i++)
            {
                StationComponent station = gStationPool[packet.StationGId[i]];
                if (station == null)
                {
                    Log.Warn($"Gid {packet.StationGId[i]} does not in client");
                    continue;
                }
                if (station.storage == null || station.storage.Length < packet.StorageLength[i])
                    station.storage = new StationStore[packet.StorageLength[i]];
                station.isCollector = packet.IsCollector[i];

                for (int j = 0; j < packet.StorageLength[i]; j++)
                {
                    station.storage[j].itemId = packet.ItemId[offset + j];
                    station.storage[j].max = packet.Max[offset + j];
                    station.storage[j].count = packet.Count[offset + j];
                    station.storage[j].inc = packet.Inc[offset + j];
                    station.storage[j].remoteOrder = packet.RemoteOrder[offset + j];
                    station.storage[j].remoteLogic = (ELogisticStorage)packet.Logic[offset + j];
                }
                offset += packet.StorageLength[i];
            }

            foreach (var station in gStationPool)
            {
                if (station != null && station.storage == null)
                {
                    Log.Warn($"Gid {station.gid} does not in server");
                }
            }

            NC_StationStorageData.OnReceive?.Invoke();
        }
    }

}
