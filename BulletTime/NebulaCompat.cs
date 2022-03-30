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
        public static int PendingFactoryCount { get; set; }
        public static bool DysonSpherePaused { get; set; }


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
            IsPlayerJoining = false;
            PendingFactoryCount = 0;
            DysonSpherePaused = false;
        }

        public static void OnFactoryLoadRequest(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.FactoryRequest, planetId);
        }

        public static void OnFactoryLoadFinished(int planetId)
        {
            if (NebulaModAPI.MultiplayerSession.IsGameLoaded)
                SendPacket(PauseEvent.FactoryLoaded, planetId);
        }

        public static void SendPacket(PauseEvent pauseEvent, int planetId = 0)
        {
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(pauseEvent, planetId));
            if (pauseEvent == PauseEvent.Resume) //UI-slider manual resume
            {
                PendingFactoryCount = 0;
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
            BulletTimePlugin.State.SetPauseMode(false);
            IngameUI.ShowStatus("");
            if (!NebulaCompat.IsClient)
                NebulaCompat.SendPacket(NebulaCompat.DysonSpherePaused ? PauseEvent.DysonSpherePaused : PauseEvent.DysonSphereResume);
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
            if (NebulaCompat.IsMultiplayerActive)
            {
                if (saveName != GameSave.AutoSaveTmp)
                {
                    // if it is not autosave (trigger by manual), reset all pause states
                    NebulaCompat.PendingFactoryCount = 0;
                    NebulaCompat.IsPlayerJoining = false;
                }

                if (NebulaCompat.PendingFactoryCount <= 0 && !NebulaCompat.IsPlayerJoining)
                {
                    NebulaCompat.SendPacket(PauseEvent.Resume);
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
            switch (packet.Event)
            {
                case PauseEvent.Resume: //Client
                    BulletTimePlugin.State.SetPauseMode(false);
                    IngameUI.ShowStatus("");
                    Log.Dev("Resume");
                    break;

                case PauseEvent.Pause: //Client
                    BulletTimePlugin.State.SetPauseMode(true);
                    Log.Dev("Pause");
                    break;

                case PauseEvent.Save: //Client
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus("Host is saving game...".Translate());
                    Log.Dev("Save");
                    break;

                case PauseEvent.FactoryRequest: //Host, Client
                    BulletTimePlugin.State.SetPauseMode(true);
                    IngameUI.ShowStatus(string.Format("{0} arriving {1}".Translate(), packet.Username, GameMain.galaxy.PlanetById(packet.PlanetId)?.displayName));
                    if (IsHost)
                    {
                        NebulaCompat.PendingFactoryCount++;
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    }
                    Log.Dev("FactoryRequest");
                    break;

                case PauseEvent.FactoryLoaded: //Host
                    if (IsHost)
                    {
                        // It is possible that FactoryLoaded is return without FactoryRequest
                        NebulaCompat.PendingFactoryCount--;
                        Log.Debug($"Pending factory: {NebulaCompat.PendingFactoryCount}");
                        if (NebulaCompat.PendingFactoryCount <= 0 && !NebulaCompat.IsPlayerJoining)
                        {
                            NebulaCompat.PendingFactoryCount = 0;
                            BulletTimePlugin.State.SetPauseMode(false);
                            IngameUI.ShowStatus("");
                            NebulaModAPI.MultiplayerSession.Network.SendPacket(new PauseNotificationPacket(PauseEvent.Resume));
                        }
                    }
                    Log.Dev("FactoryLoaded");
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

                default:
                    Log.Warn("PauseNotificationPacket: None");
                    break;
            }
        }
    }
}
