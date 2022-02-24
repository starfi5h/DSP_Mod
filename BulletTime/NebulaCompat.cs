using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;
using UnityEngine.UI;
using BulletTime;

namespace Compatibility
{
    public static class NebulaCompat
    {
        public static bool Enable;
        public static bool IsMultiplayerActive;

        static Action<int> onPlanetLoadRequest;
        static Action<int> onPlanetLoadFinished;

        public static void Init(Harmony harmony)
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            onPlanetLoadRequest = planetId => NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.FactoryRequest));
            onPlanetLoadFinished = planetId => NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Resume));
            NebulaModAPI.OnPlanetLoadRequest += onPlanetLoadRequest;
            NebulaModAPI.OnPlanetLoadFinished += onPlanetLoadFinished;

            System.Type type = AccessTools.TypeByName("NebulaWorld.GameStates.GameStatesManager");
            harmony.Patch(type.GetProperty("RealGameTick").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealGameTick")));
            harmony.Patch(type.GetProperty("RealUPS").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealUPS")));
            harmony.Patch(type.GetMethod("NotifyTickDifference"), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("NotifyTickDifference")));

            Log.Info("Nebula Compatibility is ready.");            
        }

        public static void Dispose()
        {
            NebulaModAPI.OnPlanetLoadRequest -= onPlanetLoadRequest;
            NebulaModAPI.OnPlanetLoadFinished -= onPlanetLoadFinished;            
        }

        public static void SetIsMultiplayerActive()
        {
            IsMultiplayerActive = NebulaModAPI.IsMultiplayerActive;
        }
    }

    
    public class NebulaPatch
    {

        public static void RealGameTick(ref long __result)
        {
            
        }

        public static void RealUPS(ref float __result)
        {
            Log.Debug(__result);
        }

        public static void NotifyTickDifference(float delta)
        {
            Log.Debug(delta);
        }


        [HarmonyPrefix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Prefix()
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Save));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Postfix()
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Resume));
        }
    }

    internal class PauseNotificationPacket
    {
        public PauseEvent Event { get; set; }
        public ushort PlayerId { get; set; }
        public int PlanetId { get; set; }

        public PauseNotificationPacket() { }
        public PauseNotificationPacket(PauseEvent pauseEvent, ushort playerId = 0, int planetId = 0)
        {
            Event = pauseEvent;
            PlayerId = playerId;
            PlanetId = planetId;
        }
    }

    internal enum PauseEvent
    {
        None,
        Resume,
        Pause,
        Save,
        FactoryRequest
    }

    [RegisterPacketProcessor]
    internal class PauseNotificationProcessor : BasePacketProcessor<PauseNotificationPacket>
    {
        public override void ProcessPacket(PauseNotificationPacket packet, INebulaConnection conn)
        {
            if (IsHost)
            {
                if (packet.Event == PauseEvent.Resume || packet.Event == PauseEvent.Resume)
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                else
                    return;
            }

            INebulaPlayer player = NebulaModAPI.MultiplayerSession.Network?.PlayerManager?.GetPlayer(conn);
            if (player == null)
                player = NebulaModAPI.MultiplayerSession.Network?.PlayerManager?.GetSyncingPlayer(conn);
            string username = player?.Data.Username ?? "";

            switch (packet.Event)
            {
                case PauseEvent.Resume:
                    BulletTimePlugin.State.SetPauseMode(false);
                    IngameUI.ShowStatus("");
                    break;

                case PauseEvent.Pause:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus("Host is pasued...");
                    break;

                case PauseEvent.Save:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus("Host is saving game...");
                    break;

                case PauseEvent.FactoryRequest:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus($"{username} arriving {GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName}");
                    break;
            }
        }
    }
    


}
