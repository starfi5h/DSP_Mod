using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

// DSP Belt Reverse Direction is made by GreyHak
// https://github.com/GreyHak/dsp-belt-reverse/blob/main/DSPBeltReverseDirection.cs
// Copyright (c) 2021, Aaron Shumate

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPBeltReverseDirection
    {
        public const string NAME = "DSP Belt Reverse Direction";
        public const string GUID = "greyhak.dysonsphereprogram.beltreversedirection";
        public const string VERSION = "1.1.6";

        private static Action ReverseBelt_Modified;
        private static bool normal = true;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // Send packet when players click the button
                Type classType = assembly.GetType("DSPBeltReverseDirection.DSPBeltReverseDirection");
                var methodInfo = AccessTools.Method(classType, "ReverseBelt");
                var prefix = new HarmonyMethod(typeof(DSPBeltReverseDirection).GetMethod("ReverseBeltLocal_Prefix"));
                var postfix = new HarmonyMethod(typeof(DSPBeltReverseDirection).GetMethod("ReverseBeltLocal_Postfix"));
                var transplier = new HarmonyMethod(typeof(DSPBeltReverseDirection).GetMethod("ReverseBelt_Transpiler"));
                harmony.Patch(methodInfo, prefix, postfix, transplier);
                ReverseBelt_Modified = AccessTools.MethodDelegate<Action>(methodInfo);

                if (!normal) 
                    throw new Exception("ReverseBelt_Transpiler error");
                Log.Info($"{NAME} - OK");
                NC_Patch.RequriedPlugins += " +" + NAME;
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static PlanetFactory Factory { get; set; }
        public static int BeltId { get; set; }

        public static void ReverseBeltLocal_Prefix()
        {
            if (!NebulaModAPI.IsMultiplayerActive)
            {
                Factory = UIRoot.instance.uiGame.beltWindow.factory;
                BeltId = UIRoot.instance.uiGame.beltWindow.beltId;
                return;
            }
            if (!NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.Value)
            {
                Factory = UIRoot.instance.uiGame.beltWindow.factory;
                BeltId = UIRoot.instance.uiGame.beltWindow.beltId;
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ReverseBelt(Factory.planetId, BeltId));
                // To make Multiplayer.Session.Factories.OnNewSetInserterInsertTarget can go off
                NebulaModAPI.MultiplayerSession.Factories.PacketAuthor = NebulaModAPI.MultiplayerSession.LocalPlayer.Id;
            }
        }

        public static void ReverseBeltLocal_Postfix()
        {
            if (NebulaModAPI.IsMultiplayerActive)
            {
                NebulaModAPI.MultiplayerSession.Factories.PacketAuthor = NebulaModAPI.AUTHOR_NONE;
            }
        }

        public static void ReverseBeltRemote(int planetId, int beltId)
        {
            Factory = GameMain.galaxy.PlanetById(planetId).factory;
            BeltId = beltId;
            if (Factory == null) return;

            using (NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.On())
            {
                NebulaModAPI.MultiplayerSession.Factories.EventFactory = Factory;
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = planetId;
                if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    // Load planet model
                    NebulaModAPI.MultiplayerSession.Factories.AddPlanetTimer(planetId);
                }
                ReverseBelt_Modified.Invoke();
            }
            NebulaModAPI.MultiplayerSession.Factories.EventFactory = null;
            NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;
        }

        public static IEnumerable<CodeInstruction> ReverseBelt_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace : UIRoot.instance.uiGame.beltWindow.beltId
                // with    : DSPBeltReverseDirection.BeltId
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(UIBeltWindow), "get_beltId")))
                    .Repeat(matcher => matcher
                            .Advance(-3)
                            .RemoveInstructions(3)
                            .SetAndAdvance(OpCodes.Call, typeof(DSPBeltReverseDirection).GetProperty("BeltId").GetGetMethod())
                    );

                // replace : GameMain.mainPlayer.factory
                // with    : DSPBeltReverseDirection.Factory
                codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), "get_factory")))
                    .Repeat(matcher => matcher
                            .Advance(-1)
                            .RemoveInstructions(1)
                            .SetAndAdvance(OpCodes.Call, typeof(DSPBeltReverseDirection).GetProperty("Factory").GetGetMethod())
                    );

                // modify : VFAudio.Create("ui-click-2", null, GameMain.mainPlayer.factory.entityPool[selectedBeltComponent.entityId].pos, true);
                // (string, class [UnityEngine.CoreModule]UnityEngine.Transform, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, bool, int32, int32, int64)
                codeMatcher = new CodeMatcher(codeMatcher.InstructionEnumeration())
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "Create"))
                    .SetInstructionAndAdvance(Transpilers.EmitDelegate<Action<string, Transform, Vector3, bool, int, int, long>>
                    (
                        (name, transform, position, play, _, _, _) =>
                        {
                            // make audio only play on the player who click the button
                            if (!NebulaModAPI.IsMultiplayerActive || !NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.Value)
                                VFAudio.Create(name, transform, position, play);
                        })
                    )
                    .RemoveInstruction();

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("ReverseBelt_Transpiler fail!");
                Log.Dev(e);
                normal = false;
                return instructions;
            }
        }

    }
}
