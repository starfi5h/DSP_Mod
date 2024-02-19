using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using System;
using System.Reflection;

namespace RailgunsRetargetMini
{
    public static class NebulaCompat
    {
        public const string GUID = "dsp.nebula-multiplayer";
        public static bool IsMultiplayer { get; private set; }
        public static bool IsClient { get; private set; }

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                Patch();
                Plugin.Log.LogInfo("Nebula Compatibility - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Nebula Compatibility failed!");
                Plugin.Log.LogError(e);
            }
        }

        public static void Patch()
        {
            if (!NebulaModAPI.NebulaIsInstalled)
                return;

            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
            NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
        }

        public static void OnMultiplayerGameStarted()
        {
            IsMultiplayer = NebulaModAPI.IsMultiplayerActive;
            IsClient = NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
        }

        public static void OnMultiplayerGameEnded()
        {
            IsMultiplayer = false;
            IsClient = false;
        }

        public static void SendPacket(in StarData starData, int orbitId)
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new EjectorOrbitChangePacket(starData, orbitId));
        }
    }    

    internal class EjectorOrbitChangePacket
    {
        public int StarId { get; set; }
        public int OrbitId { get; set; }
        public string Message { get; set; }

        public EjectorOrbitChangePacket() { }
        public EjectorOrbitChangePacket(in StarData starData, int orbitId, string message = "")
        {
            StarId = starData.id;
            OrbitId = orbitId;
            Message = message;
        }
    }


    [RegisterPacketProcessor]
    internal class PauseNotificationProcessor : BasePacketProcessor<EjectorOrbitChangePacket>
    {
        public override void ProcessPacket(EjectorOrbitChangePacket packet, INebulaConnection conn)
        {
            StarData starData = GameMain.galaxy.StarById(packet.StarId);
            int orbitId = packet.OrbitId;
            
            if (orbitId < 0)
            {                
                if (IsHost)
                {
                    // Return info to request client
                    conn.SendPacket(new EjectorOrbitChangePacket(starData, orbitId, UIPatch.GetStatus(starData)));
                }
                else
                {
                    // Show ejectors count on all planets in the system
                    UIPatch.ShowMessage(packet.Message, starData, -orbitId);
                }
            }
            else
            {
                UIPatch.SetOrbit(starData, orbitId);
                // Broadcast change to all clients
                if (IsHost)
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }
        }
    }
}
