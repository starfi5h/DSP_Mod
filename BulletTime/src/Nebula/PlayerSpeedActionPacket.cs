using NebulaAPI.Networking;
using NebulaAPI.Packets;
using NebulaWorld;

namespace BulletTime.Nebula
{
    internal class PlayerSpeedActionPacket
    {
        public string Username { get; set; }
        public EPlayerSpeedAction Action { get; set; }
        public float Value { get; set; }

        public PlayerSpeedActionPacket() { }
        public PlayerSpeedActionPacket(EPlayerSpeedAction action, string username)
        {
            Action = action;
            Username = username;
        }
    }

    public enum EPlayerSpeedAction
    {
        Deny,
        Pause,
        Resume,
        SpeedSet,
        SpeedUp,
        SpeedMax,
        SpeedReset
    }

    [RegisterPacketProcessor]
    internal class PlayerActionPacketProcessor : BasePacketProcessor<PlayerSpeedActionPacket>
    {
        public override void ProcessPacket(PlayerSpeedActionPacket packet, INebulaConnection conn)
        {
            NebulaCompat.IsIncomingPacket = true;
            if (IsHost)
            {
                double fixUPS = FPSController.instance.fixUPS;
                switch (packet.Action)
                {
                    case EPlayerSpeedAction.SpeedUp:
                        fixUPS = fixUPS == 0.0 ? 120.0 : fixUPS + 60.0;
                        break;
                    case EPlayerSpeedAction.SpeedMax:
                        fixUPS = 60.0 * BulletTimePlugin.MaxSpeedupScale.Value;
                        break;
                    case EPlayerSpeedAction.SpeedReset:
                        fixUPS = 0.0;
                        break;
                }

                if (fixUPS != FPSController.instance.fixUPS)
                {
                    packet.Action = EPlayerSpeedAction.SpeedSet;
                    packet.Value = (float)(fixUPS == 0.0 ? 1.0 : fixUPS / 60.0);
                    FPSController.SetFixUPS(fixUPS);
                    IngameUI.SetSpeedRatioText();
                }
                // Relay the packet to all clients
                Multiplayer.Session.Network.SendPacket(packet);
            }
            ShowPacket(packet);
            NebulaCompat.IsIncomingPacket = false;
        }

        public static void ShowPacket(PlayerSpeedActionPacket packet)
        {
            switch (packet.Action)
            {
                case EPlayerSpeedAction.Deny:
                    NebulaCompat.ShowMessageInChat("The server rejects the request.".Translate());
                    return;

                case EPlayerSpeedAction.Pause:
                    NebulaCompat.ShowMessageInChat(string.Format("{0} pause the game".Translate(), packet.Username));
                    return;

                case EPlayerSpeedAction.Resume:
                    NebulaCompat.ShowMessageInChat(string.Format("{0} resume the game".Translate(), packet.Username));
                    return;

                case EPlayerSpeedAction.SpeedSet:
                    NebulaCompat.ShowMessageInChat(string.Format("{0} set game speed = {1:F1}".Translate(), packet.Username, packet.Value));
                    return;
            }
        }
    }
}
