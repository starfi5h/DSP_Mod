using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;

namespace BulletTime.Nebula
{
    internal class ProgressUpdatePacket
    {
        public int FragmentSize { get; set; }
        public float Percentage { get; set; }

        public ProgressUpdatePacket() { }
        public ProgressUpdatePacket(int framentSize, float percentage)
        {
            FragmentSize = framentSize;
            Percentage = percentage;
        }
    }

    [RegisterPacketProcessor]
    internal class ProgressUpdateProcessor : BasePacketProcessor<ProgressUpdatePacket>
    {
        public override void ProcessPacket(ProgressUpdatePacket packet, INebulaConnection conn)
        {
            if (IsHost)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacketExclude(packet, conn);
                NebulaWorld.Multiplayer.Session.World.DisplayPingIndicator();
            }
            NebulaPatch.SetProgressTest(packet.FragmentSize, packet.Percentage);
        }
    }
}
