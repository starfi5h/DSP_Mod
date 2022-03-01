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
        public static bool Enable { get; set; }
        public static bool NebulaIsInstalled { get; private set; }
        public static bool IsMultiplayerActive { get; private set; }
        public static bool IsClient { get; private set; }

        public static void Init(Harmony harmony)
        {
            try
            {
                NebulaIsInstalled = NebulaModAPI.NebulaIsInstalled;
                if (!NebulaIsInstalled)
                    return;

                NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
                NebulaModAPI.OnPlanetLoadRequest += OnFactoryLoadRequest;
                NebulaModAPI.OnPlanetLoadFinished += OnFactoryLoadFinished;

                System.Type world = AccessTools.TypeByName("NebulaWorld.SimulatedWorld");
                System.Type type = AccessTools.TypeByName("NebulaWorld.GameStates.GameStatesManager");
                harmony.Patch(world.GetMethod("OnPlayerJoining"), new HarmonyMethod(typeof(NebulaPatch).GetMethod("OnPlayerJoining")));
                harmony.Patch(world.GetMethod("OnAllPlayersSyncCompleted"), new HarmonyMethod(typeof(NebulaPatch).GetMethod("OnAllPlayersSyncCompleted")));
                harmony.Patch(type.GetProperty("RealGameTick").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealGameTick")));
                harmony.Patch(type.GetProperty("RealUPS").GetGetMethod(), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("RealUPS")));
                harmony.Patch(type.GetMethod("NotifyTickDifference"), null, new HarmonyMethod(typeof(NebulaPatch).GetMethod("NotifyTickDifference")));
                harmony.PatchAll(typeof(NebulaPatch));

                Log.Info("Nebula Compatibility is ready.");
            }
            catch (Exception e)
            {
                Log.Error("Nebula Compatibility failed!");
                Log.Error(e);
            }
        }

        public static void Dispose()
        {
            if (NebulaIsInstalled)
            {
                NebulaModAPI.OnPlanetLoadRequest -= OnFactoryLoadRequest;
                NebulaModAPI.OnPlanetLoadFinished -= OnFactoryLoadFinished;
            }
        }

        public static void OnGameMainBegin()
        {
            IsMultiplayerActive = NebulaModAPI.IsMultiplayerActive;
            IsClient = IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
        }

        public static void OnFactoryLoadRequest(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.FactoryRequest, planetId);
        }

        public static void OnFactoryLoadFinished(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.Resume, planetId);
        }

        public static void SendPacket(PauseEvent pauseEvent, int planetId = 0)
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(pauseEvent, planetId));
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
            Log.Dev($"{1f - BulletTimePlugin.State.SkipRatio:F3} UPS:{__result}");
        }

        public static void NotifyTickDifference(float delta)
        {            
            if (!BulletTimePlugin.State.Pause)
            {
                float ratio = Mathf.Clamp(1 + delta / (float)FPSController.currentUPS, 0.01f, 1f);
                BulletTimePlugin.State.SetSpeedRatio(ratio);
                Log.Dev($"{delta:F4} RATIO:{ratio}");
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
            if (NebulaCompat.IsMultiplayerActive)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Save));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Postfix()
        {
            if (NebulaCompat.IsMultiplayerActive)
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
                    Log.Dev("Resume");
                    break;

                case PauseEvent.Pause:
                    BulletTimePlugin.State.SetPauseMode(true);
                    Log.Dev("Pause");
                    break;

                case PauseEvent.Save:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus("Host is saving game...");
                    Log.Dev("Save");
                    break;

                case PauseEvent.FactoryRequest:
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus($"{packet.Username} arriving {GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName}");
                    Log.Dev("FactoryRequest");
                    break;

                default:
                    Log.Warn("PauseNotificationPacket: None");
                    break;
            }
        }
    }
}
