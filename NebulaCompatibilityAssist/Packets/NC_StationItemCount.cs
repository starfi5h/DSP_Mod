using NebulaAPI;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_StationItemCount
    {
        public int PlanetId { get; set; }
        public int StationId { get; set; }
        public int WarperCount { get; set; }
        public int IdleDroneCount { get; set; }
        public int IdleShipCount { get; set; }

        public NC_StationItemCount() { }
        public NC_StationItemCount(in StationComponent station, int planetId)
        {
            PlanetId = planetId;
            StationId = station.id;
            WarperCount = station.warperCount;
            IdleDroneCount = station.idleDroneCount;
            IdleShipCount = station.idleShipCount;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_StationItemCountProcessor : BasePacketProcessor<NC_StationItemCount>
    {
        public override void ProcessPacket(NC_StationItemCount packet, INebulaConnection conn)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;

            Log.Debug($"Update station {packet.StationId} on {packet.PlanetId}: {packet.WarperCount} {packet.IdleDroneCount} {packet.IdleShipCount}");
            StationComponent station = factory.transport.stationPool[packet.StationId];
            station.warperCount = packet.WarperCount;
            station.idleDroneCount = packet.IdleDroneCount;
            station.idleShipCount = packet.IdleShipCount;

            // Refresh station window if it is veiwing the changing station
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            if (stationWindow != null && stationWindow.active)
            {
                if (stationWindow.factory?.planetId == packet.PlanetId && stationWindow.stationId == packet.StationId)
                {
                    stationWindow.OnStationIdChange();
                }
            }
        }
    }

}
