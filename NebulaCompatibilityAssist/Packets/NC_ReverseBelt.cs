using NebulaAPI;
using NebulaCompatibilityAssist.Patches;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_ReverseBelt
    {
        public int PlanetId { get; set; }
        public int BeltId { get; set; }
        
        public NC_ReverseBelt() { }
        public NC_ReverseBelt(int planetId, int beltId)
        {
            PlanetId = planetId;
            BeltId = beltId;
        }
    }

    [RegisterPacketProcessor]
    internal class NC_ReverseBeltProcessor : BasePacketProcessor<NC_ReverseBelt>
    {
        public override void ProcessPacket(NC_ReverseBelt packet, INebulaConnection conn)
        {            
            if (IsHost) // Broadcast changes to other users
            {
                int starId = GameMain.galaxy.PlanetById(packet.PlanetId).star.id;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToStarExclude(packet, starId, conn); 
            }
            DSPBeltReverseDirection.ReverseBeltRemote(packet.PlanetId, packet.BeltId);
        }
    }
}
