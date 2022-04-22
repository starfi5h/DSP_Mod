using BulletTime;
using HarmonyLib;
using NebulaAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Compatibility
{
    public static class NebulaCompat
    {
        public static bool Enable { get; set; }
        public static bool NebulaIsInstalled { get; private set; }
        public static bool IsMultiplayerActive { get; private set; }
        public static bool IsClient { get; private set; }

        // Pause states
        public static bool IsPlayerJoining { get; set; }
        public static List<string> LoadingPlayers { get; } = new List<string>();
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
                    LoadingPlayers.RemoveAll(x => x == player.Username);
                    DetermineCurrentState();
                };

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
        }

        public static void OnMultiplayerGameEnded()
        {
            IsMultiplayerActive = false;
            IsClient = false;
            IsPlayerJoining = false;
            LoadingPlayers.Clear();
            DysonSpherePaused = false;
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
                if (BulletTimePlugin.State.ManualPause)
                {
                    BulletTimePlugin.State.SetPauseMode(true);
                    SendPacket(PauseEvent.Pause);
                }
                else
                {
                    BulletTimePlugin.State.SetPauseMode(false);
                    IngameUI.ShowStatus("");
                    SendPacket(PauseEvent.Resume);
                }
            }
            else if (LoadingPlayers.Count > 0)
            {
                // There are still some player loading factories
                var player = NebulaModAPI.MultiplayerSession.Network.PlayerManager.GetConnectedPlayerByUsername(LoadingPlayers[0]);
                var packet = new PauseNotificationPacket(PauseEvent.FactoryRequest, player.Data.Username, player.Data.LocalPlanetId);
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }
        }


        public static void SendPacket(PauseEvent pauseEvent, int planetId = 0)
        {
            string username = NebulaModAPI.MultiplayerSession.LocalPlayer.Data.Username;
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(pauseEvent, username, planetId));
            if (pauseEvent == PauseEvent.Resume) //UI-slider manual resume
            {
                LoadingPlayers.Clear();
                IsPlayerJoining = false;
            }
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
                //Log.Dev($"{delta:F4} RATIO:{ratio}");
            }
        }

        public static bool OnPlayerJoining(string Username)
        {
            NebulaCompat.IsPlayerJoining = true;
            IngameUI.ShowStatus(string.Format("{0} joining the game".Translate(), Username));
            BulletTimePlugin.State.SetPauseMode(true);
            return false;
        }

        public static void OnAllPlayersSyncCompleted()
        {
            NebulaCompat.IsPlayerJoining = false;
            if (!NebulaCompat.IsClient)
            {
                NebulaCompat.DetermineCurrentState();
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
            }
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

        [HarmonyPostfix, HarmonyPatch(typeof(UIDESphereInfo), nameof(UIDESphereInfo._OnInit)), HarmonyAfter("dsp.nebula - multiplayer")]
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
            topFunction.pauseText.text = (NebulaCompat.DysonSpherePaused ? "Dyson sphere is stopped" : "Dyson sphere is rotating").Translate();
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
            Log.Dev(packet.Event);
            switch (packet.Event)
            {
                case PauseEvent.Resume: //Client
                    BulletTimePlugin.State.SetPauseMode(false);
                    IngameUI.ShowStatus("");
                    break;

                case PauseEvent.Pause: //Client
                    if (IsClient)
                    {
                        BulletTimePlugin.State.SetPauseMode(true);
                        IngameUI.ShowStatus("");
                    }
                    break;

                case PauseEvent.Save: //Client
                    if (IsClient)
                    {
                        BulletTimePlugin.State.SetPauseMode(true);
                        IngameUI.ShowStatus("Host is saving game...".Translate());
                    }
                    break;

                case PauseEvent.FactoryRequest: //Host, Client
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus(string.Format("{0} arriving {1}".Translate(), packet.Username, GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName));
                    if (IsHost)
                    {
                        NebulaCompat.LoadingPlayers.Add(packet.Username);
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    }
                    break;

                case PauseEvent.FactoryLoaded: //Host
                    if (IsHost)
                    {
                        // It is possible that FactoryLoaded is return without FactoryRequest
                        NebulaCompat.LoadingPlayers.RemoveAll(x => x == packet.Username);
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
}
