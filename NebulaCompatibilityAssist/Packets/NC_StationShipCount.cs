using NebulaAPI;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_StationShipCount
    {
        public int PlanetId { get; set; }
        public int[] StationIds { get; set; }
        public int[] WarperCount { get; set; }
        public int[] DroneCount { get; set; }
        public int[] ShipCount { get; set; }

        public NC_StationShipCount() { }
        public NC_StationShipCount(StationComponent station, int planetId)
            : this(new StationComponent[1] {station}, planetId) { }
        public NC_StationShipCount(StationComponent[] stations, int planetId)
        {
            PlanetId = planetId;
            StationIds = new int[stations.Length];
            WarperCount = new int[stations.Length];
            DroneCount = new int[stations.Length];
            ShipCount = new int[stations.Length];

            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i] != null)
                {
                    StationComponent station = stations[i];

                    StationIds[i] = station.id;
                    WarperCount[i] = station.warperCount;
                    DroneCount[i] = station.idleDroneCount + station.workDroneCount;
                    ShipCount[i] = station.idleShipCount + station.workShipCount;
                }
            }
        }
    }

    [RegisterPacketProcessor]
    internal class NC_StationItemCountProcessor : BasePacketProcessor<NC_StationShipCount>
    {
        public override void ProcessPacket(NC_StationShipCount packet, INebulaConnection conn)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;

            Log.Debug($"Update stations ship count on {packet.PlanetId}: {packet.StationIds.Length}");

            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 0; i < packet.StationIds.Length; i++)
            {
                if (packet.StationIds[i] == 0) continue;
                StationComponent station = pool[packet.StationIds[i]];
                if (station == null)
                {
                    Log.Warn($"Station {i} doesn't exist!");
                    continue;
                }

                station.warperCount = packet.WarperCount[i];
                station.idleDroneCount = packet.DroneCount[i] - station.workDroneCount;
                station.idleShipCount = packet.ShipCount[i] - station.workShipCount;
            }

            // Refresh station window if it is veiwing the changing factory
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            if (stationWindow != null && stationWindow.active)
            {
                if (stationWindow.factory == factory)
                {
                    stationWindow.OnStationIdChange();
                }
            }
        }
    }

}
