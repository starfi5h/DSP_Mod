using NebulaAPI;
using System;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_ModSaveData
    {
        public string Guid { get; set; }
        public byte[] Bytes { get; set; }
        
        public NC_ModSaveData() { }
        public NC_ModSaveData(string guid, byte[] bytes)
        {
            Guid = guid;
            Bytes = bytes;
        }
        public static Action<string, byte[]> OnReceive;
    }

    [RegisterPacketProcessor]
    internal class NC_ModSaveDataProcessor : BasePacketProcessor<NC_ModSaveData>
    {
        public override void ProcessPacket(NC_ModSaveData packet, INebulaConnection conn)
        {
            if (IsHost)
            {
                // Broadcast changes to other users
                // NebulaModAPI.MultiplayerSession.Network.SendPacketExclude(packet, conn); //this method is tempoarily broken before fix in 0.8.12
                var playerManager = NebulaModAPI.MultiplayerSession.Network.PlayerManager;
                INebulaPlayer player = playerManager.GetPlayer(conn);
                playerManager.SendPacketToOtherPlayers(packet, player);
            }
            Log.Debug($"Receive ModSave Data Packet - {packet.Guid}");
            NC_ModSaveData.OnReceive?.Invoke(packet.Guid, packet.Bytes);
        }
    }
}
