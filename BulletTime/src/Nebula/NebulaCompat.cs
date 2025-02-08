using HarmonyLib;
using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BulletTime.Nebula
{
    public static class NebulaCompat
    {
        public const string APIGUID = "dsp.nebula-multiplayer-api";
        public const string GUID = "dsp.nebula-multiplayer";
        public static bool NebulaIsInstalled { get; private set; }
        public static bool IsMultiplayerActive { get; private set; }
        public static bool IsClient { get; private set; }

        // Pause states
        public static bool IsPlayerJoining { get; set; }
        public static List<(string username, int planetId)> LoadingPlayers { get; } = new List<(string, int)>();
        public static bool DysonSpherePaused { get; set; }

        public static void Init(Harmony harmony)
        {
            try
            {
                NebulaIsInstalled = NebulaModAPI.NebulaIsInstalled;
                if (!NebulaIsInstalled)
                    return;

                NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
                NebulaModAPI.OnMultiplayerGameStarted += OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded += OnMultiplayerGameEnded;
                NebulaModAPI.OnPlanetLoadRequest += OnFactoryLoadRequest;
                NebulaModAPI.OnPlanetLoadFinished += OnFactoryLoadFinished;
                NebulaModAPI.OnPlayerLeftGame += (player) =>
                {
                    LoadingPlayers.RemoveAll(x => x.username == player.Username);
                    DetermineCurrentState();
                };
                harmony.PatchAll(typeof(NebulaPatch));

                Log.Debug("Nebula Compatibility OK");
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
                NebulaModAPI.OnMultiplayerGameStarted -= OnMultiplayerGameStarted;
                NebulaModAPI.OnMultiplayerGameEnded -= OnMultiplayerGameEnded;
                NebulaModAPI.OnPlanetLoadRequest -= OnFactoryLoadRequest;
                NebulaModAPI.OnPlanetLoadFinished -= OnFactoryLoadFinished;
            }
        }

        public static void OnMultiplayerGameStarted()
        {
            IsMultiplayerActive = NebulaModAPI.IsMultiplayerActive;
            IsClient = IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
            IsPlayerJoining = false;
            LoadingPlayers.Clear();
            DysonSpherePaused = false;
            GameStateManager.EnableMechaFunc = false;
            NebulaPatch.SetProgessMode(false);
        }

        public static void OnMultiplayerGameEnded()
        {
            IsMultiplayerActive = false;
            IsClient = false;
            IsPlayerJoining = false;
            LoadingPlayers.Clear();
            DysonSpherePaused = false;
            GameStateManager.EnableMechaFunc = BulletTimePlugin.EnableMechaFunc.Value;
        }

        private static void OnFactoryLoadRequest(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.FactoryRequest, planetId);
        }

        private static void OnFactoryLoadFinished(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.FactoryLoaded, planetId);
        }

        public static void DetermineCurrentState()
        {
            if (LoadingPlayers.Count == 0 && !IsPlayerJoining)
            {
                // Back to host manual setting
                if (GameStateManager.ManualPause)
                {
                    GameStateManager.SetPauseMode(true);
                    GameStateManager.SetSyncingLock(false);
                    IngameUI.ShowStatus(BulletTimePlugin.StatusTextPause.Value);
                    SendPacket(PauseEvent.Pause);
                }
                else
                {
                    GameStateManager.SetPauseMode(false);
                    GameStateManager.SetSyncingLock(false);
                    IngameUI.ShowStatus("");
                    SendPacket(PauseEvent.Resume);
                }
            }
            else if (LoadingPlayers.Count > 0)
            {
                // There are still some player loading factories, wait for them to finish
                var packet = new PauseNotificationPacket(PauseEvent.FactoryRequest, LoadingPlayers[0].username, LoadingPlayers[0].planetId);
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }
        }


        public static void SendPacket(PauseEvent pauseEvent, int planetId = 0)
        {
#if DEBUG
            Log.Debug($"SendPacket " + pauseEvent);
#endif
            string username = NebulaModAPI.MultiplayerSession.LocalPlayer.Data.Username;
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(pauseEvent, username, planetId));
            if (pauseEvent == PauseEvent.Resume) //UI-slider manual resume
            {
                LoadingPlayers.Clear();
                IsPlayerJoining = false;
            }
        }

        public static void SetPacketProcessing(bool enable)
        {
            Log.Info("SetPacketProcessing: " + enable);
            NebulaModAPI.MultiplayerSession.Network.PacketProcessor.EnablePacketProcessing = enable;
        }
    }
}
