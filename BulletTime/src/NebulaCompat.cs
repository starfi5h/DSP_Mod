using HarmonyLib;
using NebulaAPI;
using NebulaAPI.Networking;
using NebulaAPI.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BulletTime
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


    public class NebulaPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "RealGameTick", MethodType.Getter)]
        static void RealGameTick(ref long __result)
        {
            if (GameStateManager.StoredGameTick != 0)
                __result = GameStateManager.StoredGameTick;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "RealUPS", MethodType.Getter)]
        static void RealUPS(ref float __result)
        {
            if (!GameStateManager.Pause)
                __result *= (1f - GameStateManager.SkipRatio) / 100f;
            //Log.Dev($"{1f - GameStateManager.SkipRatio:F3} UPS:{__result}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "NotifyTickDifference")]
        static void NotifyTickDifference(float delta)
        {
            if (!GameStateManager.Pause)
            {
                float ratio = Mathf.Clamp(1 + delta / (float)FPSController.currentUPS, 0.01f, 1f);
                GameStateManager.SetSpeedRatio(ratio);
                //Log.Dev($"{delta:F4} RATIO:{ratio}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "OnPlayerJoining")]
        static bool OnPlayerJoining(string username)
        {
            NebulaCompat.IsPlayerJoining = true;
            GameMain.isFullscreenPaused = true; // Prevent other players from joining
            IngameUI.ShowStatus(string.Format("{0} joining the game".Translate(), username));
            GameStateManager.SetPauseMode(true);
            GameStateManager.SetSyncingLock(true); // TODO: Lock for only on the joining player's planet
            SetProgessMode(true); // Prepare to update joining player status
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "OnAllPlayersSyncCompleted")]
        static void OnAllPlayersSyncCompleted()
        {
            NebulaCompat.IsPlayerJoining = false;
            if (!NebulaCompat.IsClient)
            {
                NebulaCompat.DetermineCurrentState();
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
            }
            SetProgessMode(false); // Resume ping status
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Prefix()
        {
            if (NebulaCompat.IsMultiplayerActive)
                NebulaCompat.SendPacket(PauseEvent.Save);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        private static void SaveCurrentGame_Postfix(string saveName)
        {
            if (NebulaCompat.IsMultiplayerActive && !NebulaCompat.IsClient)
            {
                if (saveName != GameSave.AutoSaveTmp)
                {
                    // if it is not autosave (trigger by manual), reset all pause states
                    NebulaCompat.LoadingPlayers.Clear();
                    NebulaCompat.IsPlayerJoining = false;
                }
                if (saveName != GameSave.saveExt)
                {
                    NebulaCompat.DetermineCurrentState();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MechaLab), nameof(MechaLab.GameTick))]
        private static bool MechaLabGameTick_Prefix()
        {
            // Disable MechaLab during player joining
            return !NebulaCompat.IsMultiplayerActive || !NebulaCompat.IsPlayerJoining;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDESphereInfo), nameof(UIDESphereInfo._OnInit)), HarmonyAfter("dsp.nebula-multiplayer")]
        private static void UIDESphereInfo__OnInit()
        {
            UIDETopFunction topFunction = UIRoot.instance.uiGame.dysonEditor.controlPanel.topFunction;
            topFunction.pauseButton.button.interactable = true;
            SetDysonSpherePasued(NebulaCompat.DysonSpherePaused);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction._OnLateUpdate))]
        private static bool UIDETopFunction__OnLateUpdate()
        {
            return !NebulaCompat.IsMultiplayerActive;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDETopFunction), nameof(UIDETopFunction.OnPauseButtonClick))]
        private static bool OnPauseButtonClick()
        {
            if (NebulaCompat.IsMultiplayerActive)
            {
                SetDysonSpherePasued(!NebulaCompat.DysonSpherePaused);
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
                return false;
            }
            return true;
        }

        public static void SetDysonSpherePasued(bool state)
        {
            NebulaCompat.DysonSpherePaused = state;
            UIDETopFunction topFunction = UIRoot.instance.uiGame.dysonEditor.controlPanel.topFunction;
            topFunction.pauseButton.highlighted = !NebulaCompat.DysonSpherePaused;
            topFunction.pauseImg.sprite = (NebulaCompat.DysonSpherePaused ? topFunction.pauseSprite : topFunction.playSprite);
            topFunction.pauseText.text = (NebulaCompat.DysonSpherePaused ? "Click to resume rotating" : "Click to stop rotating").Translate();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(DysonSphereLayer), nameof(DysonSphereLayer.GameTick))]
        private static IEnumerable<CodeInstruction> DysonSphereLayer_GameTick(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            try
            {
                // When DysonSpherePaused is on, skip rotation part and jump to DysonSwarm swarm = this.dysonSphere.swarm;
                var codeMatcher = new CodeMatcher(instructions, iL)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphereLayer), "dysonSphere")),
                        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DysonSphere), "swarm"))
                    )
                    .CreateLabel(out Label label)
                    .Start()
                    .Insert(
                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(NebulaCompat), "DysonSpherePaused")),
                        new CodeInstruction(OpCodes.Brtrue_S, label)
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Error("DysonSphereLayer_GameTick Transpiler error");
                Log.Error(e);
                return instructions;
            }
        }

        #region Progress

        static int lastLength;
        static bool enablePingIndicatorUpdate = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NebulaWorld.GameStates.GameStatesManager), "UpdateBufferLength")]
        static void UpdateBufferLength_Postfix(int length)
        {
            if (lastLength == length || NebulaWorld.GameStates.GameStatesManager.FragmentSize <= 0) return; // Update only when length change

            // Broadcast the downloading progress to other players
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new ProgressUpdatePacket(NebulaWorld.GameStates.GameStatesManager.FragmentSize, (float)length / NebulaWorld.GameStates.GameStatesManager.FragmentSize));
            lastLength = length;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaWorld.SimulatedWorld), "UpdatePingIndicator")]
        static bool UpdatePingIndicator_Prefix()
        {
            return enablePingIndicatorUpdate;
        }

        public static void SetProgessMode(bool enable)
        {
            if (enable)
            {
                enablePingIndicatorUpdate = false;
            }
            else
            {
                enablePingIndicatorUpdate = true;
                if (NebulaModAPI.MultiplayerSession.IsServer) NebulaWorld.Multiplayer.Session.World.HidePingIndicator();
            }
        }

        public static void SetProgressTest(int fragmentSize, float percentage)
        {
            var enable = enablePingIndicatorUpdate;
            enablePingIndicatorUpdate = true;
            NebulaWorld.Multiplayer.Session.World.UpdatePingIndicator($"Progress {fragmentSize / 1000:n0} KB ({percentage:P1})");
            enablePingIndicatorUpdate = enable;
        }

        #endregion
    }

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
                    }
                    break;

                case PauseEvent.DysonSpherePaused:
                    NebulaPatch.SetDysonSpherePasued(true);
                    if (IsHost)
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    break;

                case PauseEvent.DysonSphereResume:
                    NebulaPatch.SetDysonSpherePasued(false);
                    if (IsHost)
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    break;
            }
        }
    }

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
