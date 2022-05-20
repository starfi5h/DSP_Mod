using NebulaAPI;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_StationStorageRequest
    {
    }

    [RegisterPacketProcessor]
    internal class StationStorageRequestProcessor : BasePacketProcessor<NC_StationStorageRequest>
    {
        public override void ProcessPacket(NC_StationStorageRequest packet, INebulaConnection conn)
        {
            if (IsHost)
                conn.SendPacket(new NC_StationStorageReponse());
        }
    }
}
