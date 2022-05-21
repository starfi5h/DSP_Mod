using NebulaAPI;
using System;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_ModSaveRequest
    {
        public string Guid { get; set; }
        
        public NC_ModSaveRequest() { }
        public NC_ModSaveRequest(string guid)
        {
            Guid = guid;
        }
        public static Action<string, INebulaConnection> OnReceive;
    }

    [RegisterPacketProcessor]
    internal class NC_ModSaveRequestProcessor : BasePacketProcessor<NC_ModSaveRequest>
    {
        public override void ProcessPacket(NC_ModSaveRequest packet, INebulaConnection conn)
        {
            NC_ModSaveRequest.OnReceive?.Invoke(packet.Guid, conn);
        }
    }
}
