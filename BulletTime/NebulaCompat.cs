using BulletTime;
using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;
using UnityEngine;

namespace Compatibility
{
    public static class NebulaCompat
    {
        public static bool Enable;
        public static bool IsMultiplayerActive;
        public static bool IsClient;

        static Action<int> onPlanetLoadRequest;
        static Action<int> onPlanetLoadFinished;

        public static void Init(Harmony harmony)
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            onPlanetLoadRequest = planetId => NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.FactoryRequest, planetId));
            onPlanetLoadFinished = planetId => NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Resume, planetId));
            NebulaModAPI.OnPlanetLoadRequest += onPlanetLoadRequest;
            NebulaModAPI.OnPlanetLoadFinished += onPlanetLoadFinished;

            System.Type type = AccessTools.TypeByName("NebulaWorld.GameStates.GameStatesManager");
            harmony.Patch(type.GetProperty("RealGameTick").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealGameTick")));
            harmony.Patch(type.GetProperty("RealUPS").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealUPS")));
            harmony.Patch(type.GetMethod("NotifyTickDifference"), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("NotifyTickDifference")));
            System.Type world = AccessTools.TypeByName("NebulaWorld.SimulatedWorld");
            harmony.Patch(world.GetMethod("OnPlayerJoining"), new HarmonyMethod(typeof(NebulaPatch).GetMethod("OnPlayerJoining")));
            harmony.Patch(world.GetMethod("OnAllPlayersSyncCompleted"), new HarmonyMethod(typeof(NebulaPatch).GetMethod("OnAllPlayersSyncCompleted")));

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
            IsClient = IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
        }

        public static void SendPacket(PauseEvent pauseEvent)
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(pauseEvent, GameMain.localPlanet?.id ?? 0));
        }
    }

    
    public class NebulaPatch
    {
        public static void RealGameTick(ref long __result)
        {
            if (BulletTimePlugin.State.StoredGameTick != 0)
                __result = BulletTimePlugin.State.StoredGameTick;
        }

        public static void RealUPS(ref float __result)
        {
            if (!BulletTimePlugin.State.Pause)
                __result *= (1f - BulletTimePlugin.State.SkipRatio) / 100f;
            Log.Debug($"{1f - BulletTimePlugin.State.SkipRatio:F3} UPS:{__result}");
        }

        public static void NotifyTickDifference(float delta)
        {            
            if (!BulletTimePlugin.State.Pause)
            {
                float ratio = Mathf.Clamp(1 + delta / (float)FPSController.currentUPS, 0.01f, 1f);
                BulletTimePlugin.State.SetSpeedRatio(ratio);
                Log.Debug($"{delta:F4} RATIO:{ratio}");
            }
        }

        public static bool OnPlayerJoining(string Username)
        {
            IngameUI.ShowStatus(Username + " joining the game");
            BulletTimePlugin.State.SetPauseMode(true);
            GameMain.isFullscreenPaused = true;
            return false;
        }

        public static void OnAllPlayersSyncCompleted()
        {
            BulletTimePlugin.State.SetPauseMode(false);
            IngameUI.ShowStatus("");            
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
        public string Username { get; set; }
        public PauseEvent Event { get; set; }
        public int PlanetId { get; set; }

        public PauseNotificationPacket() { }
        public PauseNotificationPacket(PauseEvent pauseEvent, int planetId = 0)
        {
            Username = NebulaModAPI.MultiplayerSession.LocalPlayer.Data.Username;
            Event = pauseEvent;
            PlanetId = planetId;
        }
    }

    public enum PauseEvent
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
                if (packet.Event == PauseEvent.Resume || packet.Event == PauseEvent.FactoryRequest)
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                else
                    return;
            }

            switch (packet.Event)
            {
                case PauseEvent.Resume:
                    BulletTimePlugin.State.SetPauseMode(false);
                    IngameUI.ShowStatus("");
                    Log.Debug("Resume");
                    break;

                case PauseEvent.Pause:
                    BulletTimePlugin.State.SetPauseMode(true);
                    //IngameUI.ShowStatus("Game pause by host");
                    Log.Debug("Pause");
                    break;

                case PauseEvent.Save:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus("Host is saving game...");
                    Log.Debug("Save");
                    break;

                case PauseEvent.FactoryRequest:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus($"{packet.Username} arriving {GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName}");
                    Log.Debug("FactoryRequest");
                    break;

                default:
                    Log.Warn("None");
                    break;
            }
        }
    }
    


}
