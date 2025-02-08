using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;

namespace BulletTime.Nebula
{
    internal class PauseNotificationPacket
    {
        public string Username { get; set; }
        public PauseEvent Event { get; set; }
        public int PlanetId { get; set; }

        public PauseNotificationPacket() { }
        public PauseNotificationPacket(PauseEvent pauseEvent, string username, int planetId = 0)
        {
            Event = pauseEvent;
            Username = username;
            PlanetId = planetId;
        }
    }

    public enum PauseEvent
    {
        None,
        Resume,
        Pause,
        Save,
        FactoryRequest,
        FactoryLoaded,
        DysonSpherePaused,
        DysonSphereResume
    }

    [RegisterPacketProcessor]
    internal class PauseNotificationProcessor : BasePacketProcessor<PauseNotificationPacket>
    {
        public override void ProcessPacket(PauseNotificationPacket packet, INebulaConnection conn)
        {
            NebulaPatch.SetProgessMode(false);
            Log.Dev(packet.Event);
            switch (packet.Event)
            {
                case PauseEvent.Resume: //Host, Client
                    if (IsHost)
                    {
                        NebulaWorld.Multiplayer.Session.World.HidePingIndicator();
                        if (!GameStateManager.Interactable || !GameStateManager.Pause) return;

                        IngameUI.OnKeyPause(); //解除熱鍵時停
                    }
                    else
                    {
                        GameStateManager.ManualPause = false;
                        GameStateManager.SetPauseMode(false);
                        GameStateManager.SetSyncingLock(false); //解除工廠鎖定
                        IngameUI.SetHotkeyPauseMode(false);
                        IngameUI.ShowStatus("");
                    }
                    break;

                case PauseEvent.Pause: //Host, Client
                    if (IsHost)
                    {
                        if (!GameStateManager.Interactable || GameStateManager.Pause) return;

                        IngameUI.OnKeyPause(); //觸發熱鍵時停
                    }
                    else
                    {
                        GameStateManager.ManualPause = true;
                        GameStateManager.SetPauseMode(true);
                        GameStateManager.SetSyncingLock(false); //解除工廠鎖定
                        IngameUI.SetHotkeyPauseMode(true);
                    }
                    break;

                case PauseEvent.Save: //Client
                    if (IsClient)
                    {
                        GameMain.isFullscreenPaused = true;
                        GameStateManager.SetPauseMode(true);
                        GameStateManager.SetSyncingLock(true);
                        IngameUI.ShowStatus("Host is saving game...".Translate());
                    }
                    break;

                case PauseEvent.FactoryRequest: //Host, Client
                    GameStateManager.SetPauseMode(true);
                    GameStateManager.SetSyncingLock(GameMain.localPlanet?.id == packet.PlanetId);
                    IngameUI.ShowStatus(string.Format("{0} arriving {1}".Translate(), packet.Username, GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName));
                    if (IsHost)
                    {
                        NebulaCompat.LoadingPlayers.Add((packet.Username, packet.PlanetId));
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    }
                    NebulaPatch.SetProgessMode(packet.Username != NebulaModAPI.MultiplayerSession.LocalPlayer.Data.Username); // Prepare to update arriving player progress status for other players
                    break;

                case PauseEvent.FactoryLoaded: //Host
                    if (IsHost)
                    {
                        // It is possible that FactoryLoaded is return without FactoryRequest
                        NebulaCompat.LoadingPlayers.RemoveAll(x => x.username == packet.Username);
                        NebulaCompat.DetermineCurrentState();
                        NebulaWorld.Multiplayer.Session.World.HidePingIndicator();
                    }
                    break;

                case PauseEvent.DysonSpherePaused:
                    NebulaPatch.SetDysonSpherePasued(true);
                    if (IsHost)
                    {
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    }
                    break;

                case PauseEvent.DysonSphereResume:
                    NebulaPatch.SetDysonSpherePasued(false);
                    if (IsHost)
                    {
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                        NebulaWorld.Multiplayer.Session.World.HidePingIndicator();
                    }
                    break;
            }
        }
    }
}
