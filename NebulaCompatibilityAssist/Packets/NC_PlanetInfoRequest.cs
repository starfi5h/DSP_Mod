using NebulaAPI;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_PlanetInfoRequest
    {
    }

    [RegisterPacketProcessor]
    internal class NC_PlanetInfoRequestProcessor : BasePacketProcessor<NC_PlanetInfoRequest>
    {
        public override void ProcessPacket(NC_PlanetInfoRequest packet, INebulaConnection conn)
        {
            if (IsClient) return;

            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                // Send infomation of planet that has factory on it
                conn.SendPacket(new NC_PlanetInfoData(GameMain.data.factories[i].planet));
            }
        }
    }
}
