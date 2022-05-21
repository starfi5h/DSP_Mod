using NebulaAPI;
using HarmonyLib;
using System;
using NebulaCompatibilityAssist.Packets;
using System.Collections.Generic;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSPBeltReverseDirection
    {
        public const string NAME = "DSP Belt Reverse Direction";
        public const string GUID = "greyhak.dysonsphereprogram.beltreversedirection";
        public const string VERSION = "1.1.6";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID))
                return;

            try
            {
                // Send packet when players click the button
                System.Type targetType = AccessTools.TypeByName("DSPBeltReverseDirection.DSPBeltReverseDirection");
                var ReverseBelt = AccessTools.Method(targetType, "ReverseBelt");
                harmony.Patch(ReverseBelt, new HarmonyMethod(typeof(DSPBeltReverseDirection).GetMethod("ReverseBeltLocal")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                Log.Debug(e);
            }
        }

        public static bool ReverseBeltLocal()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return true;
            
            PlanetFactory factory = UIRoot.instance.uiGame.beltWindow.factory;
            int beltId = UIRoot.instance.uiGame.beltWindow.beltId;
            NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ReverseBelt(factory.planetId, beltId));

            // To make Multiplayer.Session.Factories.OnNewSetInserterInsertTarget can go off
            NebulaModAPI.MultiplayerSession.Factories.PacketAuthor = NebulaModAPI.MultiplayerSession.LocalPlayer.Id;
            ReverseBelt_Modified(factory, beltId);
            NebulaModAPI.MultiplayerSession.Factories.PacketAuthor = NebulaModAPI.AUTHOR_NONE;
            return false;
        }

        public static void ReverseBeltRemote(int planetId, int beltId)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(planetId).factory;
            if (factory == null) return;
            if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
            {
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = planetId;
                NebulaModAPI.MultiplayerSession.Factories.AddPlanetTimer(planetId);
            }

            ReverseBelt_Modified(factory, beltId);
            NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;
        }

        // Below code are modified version of ReverseBelt, originated by GreyHak
        // https://github.com/GreyHak/dsp-belt-reverse

        // Copyright (c) 2021, Aaron Shumate
        // All rights reserved.
        //
        // This source code is licensed under the BSD-style license found in the
        // LICENSE.txt file in the root directory of this source tree. 
        //
        // Dyson Sphere Program is developed by Youthcat Studio and published by Gamera Game.

        public const int BELT_INPUT_SLOT = 1;
        public const int BELT_OUTPUT_SLOT = 0;

        struct ReverseConnection
        {
            public int targetId;
            public int outputId;
            public int inputId0;
            public int inputId1;
            public int inputId2;
        }

        static void ReverseBelt_Modified(PlanetFactory factory, int beltId)
        {
            int selectedBeltId = beltId; // Modified
            CargoTraffic cargoTraffic = factory.cargoTraffic; // Modified
            BeltComponent selectedBeltComponent = cargoTraffic.beltPool[selectedBeltId];
            CargoPath cargoPath = cargoTraffic.pathPool[selectedBeltComponent.segPathId];

            if (cargoPath.belts.Count > 1)
            {
                Log.Debug("Reverse!");

                bool grabbedItemsFlag = true;
                List<int> cargoIds = new List<int>();
                for (int index = 0; index + 9 < cargoPath.bufferLength;)
                {
                    if (cargoPath.buffer[index] == 0)
                    {
                        cargoIds.Add(0);
                        ++index;
                    }
                    else if (
                        (cargoPath.buffer[index + 0] == 246) ||
                        (cargoPath.buffer[index + 1] == 247) ||
                        (cargoPath.buffer[index + 2] == 248) ||
                        (cargoPath.buffer[index + 3] == 249) ||
                        (cargoPath.buffer[index + 4] == 250) ||
                        (cargoPath.buffer[index + 9] == byte.MaxValue))
                    {
                        int extractedCargoId = (int)
                            (cargoPath.buffer[index + 5] - 1 +
                            (cargoPath.buffer[index + 6] - 1) * 100) +
                            (int)(cargoPath.buffer[index + 7] - 1) * 10000 +
                            (int)(cargoPath.buffer[index + 8] - 1) * 1000000;
                        cargoIds.Add(extractedCargoId);
                        index += 10;
                    }
                    else
                    {
                        Log.Warn("Unable to identify items on the belt.");
                        grabbedItemsFlag = false;
                        break;
                    }
                }
                if (grabbedItemsFlag)
                {
                    Array.Clear(cargoPath.buffer, 0, cargoPath.bufferLength);
                }

                int firstBeltId = cargoPath.belts[0];
                int lastBeltId = cargoPath.belts[cargoPath.belts.Count - 1];
                BeltComponent firstBelt = cargoTraffic.beltPool[firstBeltId];
                BeltComponent lastBelt = cargoTraffic.beltPool[lastBeltId];

                // For machine connections we reference PlanetFactory.CreateEntityLogicComponents
                // These include splitter, miner, tank, fractionate, powerExchanger (and station)
                factory.ReadObjectConn(firstBelt.entityId, BELT_INPUT_SLOT, out bool unusedFlag, out int entityIdOfMachineOutputting, out int slotOfMachineOutputting);
                factory.ReadObjectConn(lastBelt.entityId, BELT_OUTPUT_SLOT, out unusedFlag, out int entityIdOfMachineGettingInput, out int slotOfMachineGettingInput);
                // Note: "Machine" at this point is likely to just be another conveyer.

                List<ReverseConnection> reverseConnections = new List<ReverseConnection>();
                for (int beltIdx = cargoPath.belts.Count - 1; beltIdx >= 0; --beltIdx)
                {
                    BeltComponent thisBelt = cargoTraffic.beltPool[cargoPath.belts[beltIdx]];
                    Log.Debug((beltIdx > 0 ? cargoTraffic.beltPool[cargoPath.belts[beltIdx - 1]].id.ToString() : "start") + " -> " + thisBelt.id.ToString() + " -> " + (beltIdx + 1 < cargoPath.belts.Count ? cargoTraffic.beltPool[cargoPath.belts[beltIdx + 1]].id.ToString() : "end"));
                    Log.Debug("   outputId=" + thisBelt.outputId.ToString() + ", backInputId=" + thisBelt.backInputId.ToString() + ", leftInputId=" + thisBelt.leftInputId.ToString() + ", rightInputId=" + thisBelt.rightInputId.ToString());

                    if (beltIdx == cargoPath.belts.Count - 1 && thisBelt.outputId != 0 && cargoTraffic.beltPool[thisBelt.outputId].segPathId != thisBelt.segPathId)
                    {
                        // About to break a primary segment, so consider this belt as having no output
                        thisBelt.outputId = 0;
                    }

                    ReverseConnection reverseConnection = new ReverseConnection
                    {
                        targetId = thisBelt.id,
                        outputId = thisBelt.mainInputId
                    };
                    Log.Debug("      targetId=" + reverseConnection.targetId.ToString() + ", outputId=" + reverseConnection.outputId.ToString());

                    List<int> inputs = new List<int>();
                    if (thisBelt.outputId != 0) inputs.Add(thisBelt.outputId);
                    if (thisBelt.backInputId != 0 && thisBelt.backInputId != reverseConnection.outputId) inputs.Add(thisBelt.backInputId);
                    if (thisBelt.leftInputId != 0 && thisBelt.leftInputId != reverseConnection.outputId) inputs.Add(thisBelt.leftInputId);
                    if (thisBelt.rightInputId != 0 && thisBelt.rightInputId != reverseConnection.outputId) inputs.Add(thisBelt.rightInputId);
                    if (inputs.Count > 0) reverseConnection.inputId0 = inputs[0];
                    if (inputs.Count > 1) reverseConnection.inputId1 = inputs[1];
                    if (inputs.Count > 2) reverseConnection.inputId2 = inputs[2];
                    Log.Debug("      inputId0=" + reverseConnection.inputId0.ToString() + ", inputId1=" + reverseConnection.inputId1.ToString() + ", inputId2=" + reverseConnection.inputId2.ToString());

                    reverseConnections.Add(reverseConnection);
                }

                // The order of this loop can be swaped, and still performs the same change.
                foreach (ReverseConnection reverseConnection in reverseConnections)
                {
                    cargoTraffic.AlterBeltConnections(reverseConnection.targetId, reverseConnection.outputId, reverseConnection.inputId0, reverseConnection.inputId1, reverseConnection.inputId2);

                    int entityIdOfThisBelt = cargoTraffic.beltPool[reverseConnection.targetId].entityId;
                    int entityIdOfOutputBelt = reverseConnection.outputId == 0 ? 0 : cargoTraffic.beltPool[reverseConnection.outputId].entityId;
                    int entityIdOfMainInputBelt = reverseConnection.inputId0 == 0 ? 0 : cargoTraffic.beltPool[reverseConnection.inputId0].entityId;

                    // This loop will disconnect all inserters.  It's based on PlanetFactory.RemoveEntityWithComponents() and PlanetFactory.ApplyEntityDisconnection()
                    for (int slotIdx = 0; slotIdx < 16; slotIdx++)
                    {
                        factory.ReadObjectConn(entityIdOfThisBelt, slotIdx, out bool flag, out int otherEntityId, out int otherSlotId);
                        if (otherEntityId > 0)
                        {
                            int inserterId = factory.entityPool[otherEntityId].inserterId;
                            if (inserterId > 0)  // Is otherEntityId an inserter entity?
                            {
                                if (factory.factorySystem.inserterPool[inserterId].insertTarget == entityIdOfThisBelt)
                                {
                                    Log.Debug($"Disconnecting inserter insert target {inserterId} from {entityIdOfThisBelt}");
                                    factory.factorySystem.SetInserterInsertTarget(inserterId, 0, 0);
                                }
                                if (factory.factorySystem.inserterPool[inserterId].pickTarget == entityIdOfThisBelt)
                                {
                                    Log.Debug($"Disconnecting inserter pick target {inserterId} from {entityIdOfThisBelt}");
                                    factory.factorySystem.SetInserterPickTarget(inserterId, 0, 0);
                                }
                            }
                        }
                    }

                    factory.ClearObjectConn(entityIdOfThisBelt);
                    factory.WriteObjectConnDirect(entityIdOfThisBelt, BELT_OUTPUT_SLOT, true, entityIdOfOutputBelt, BELT_INPUT_SLOT);
                    factory.WriteObjectConnDirect(entityIdOfThisBelt, BELT_INPUT_SLOT, false, entityIdOfMainInputBelt, BELT_OUTPUT_SLOT);
                    factory.OnBeltBuilt(entityIdOfThisBelt);  // This reconnects the inserters
                }

                if (entityIdOfMachineOutputting > 0)
                {
                    EntityData entityOfMachineOutputting = factory.entityPool[entityIdOfMachineOutputting];
                    if (entityOfMachineOutputting.splitterId != 0)
                    {
                        Log.Debug("      Belt receiving input from splitter " + entityOfMachineOutputting.splitterId.ToString());
                        cargoTraffic.ConnectToSplitter(entityOfMachineOutputting.splitterId, firstBeltId, slotOfMachineOutputting, true);
                    }
                    else if (entityOfMachineOutputting.minerId != 0)
                    {
                        Log.Debug("      Belt receiving input from miner " + entityOfMachineOutputting.minerId.ToString());
                        factory.factorySystem.SetMinerInsertTarget(entityOfMachineOutputting.minerId, 0);
                    }
                    else if (entityOfMachineOutputting.tankId != 0)
                    {
                        Log.Debug("      Belt receiving input from tank " + entityOfMachineOutputting.tankId.ToString());
                        factory.factoryStorage.SetTankBelt(entityOfMachineOutputting.tankId, firstBeltId, slotOfMachineOutputting, false);
                    }
                    else if (entityOfMachineOutputting.fractionatorId != 0)
                    {
                        Log.Debug("      Belt receiving input from fractionator " + entityOfMachineOutputting.fractionatorId.ToString());
                        factory.factorySystem.SetFractionatorBelt(entityOfMachineOutputting.fractionatorId, firstBeltId, slotOfMachineOutputting, false);
                    }
                    else if (entityOfMachineOutputting.powerExcId != 0)
                    {
                        Log.Debug("      Belt receiving input from power exchanger " + entityOfMachineOutputting.powerExcId.ToString());
                        factory.powerSystem.SetExchangerBelt(entityOfMachineOutputting.powerExcId, firstBeltId, slotOfMachineOutputting, false);
                    }
                    else if (entityOfMachineOutputting.stationId != 0)
                    {
                        Log.Debug("      Belt receiving input from station " + entityOfMachineOutputting.stationId.ToString());
                        factory.ApplyEntityInput(entityIdOfMachineOutputting, firstBelt.entityId, slotOfMachineOutputting, slotOfMachineOutputting, 0);
                        Log.Debug("         Station now set to " + factory.transport.stationPool[entityOfMachineOutputting.stationId].slots[slotOfMachineOutputting].dir.ToString());
                    }
                    factory.WriteObjectConnDirect(firstBelt.entityId, BELT_OUTPUT_SLOT, true, entityIdOfMachineOutputting, slotOfMachineOutputting);
                    factory.WriteObjectConnDirect(entityIdOfMachineOutputting, slotOfMachineOutputting, false, firstBelt.entityId, BELT_OUTPUT_SLOT);
                }
                if (entityIdOfMachineGettingInput > 0)
                {
                    EntityData entityOfMachineGettingInput = factory.entityPool[entityIdOfMachineGettingInput];
                    if (entityOfMachineGettingInput.splitterId != 0)
                    {
                        Log.Debug("      Belt outputting to splitter " + entityOfMachineGettingInput.splitterId.ToString());
                        cargoTraffic.ConnectToSplitter(entityOfMachineGettingInput.splitterId, lastBeltId, slotOfMachineGettingInput, false);
                    }
                    else if (entityOfMachineGettingInput.minerId != 0)
                    {
                        Log.Debug("      ERROR: Belt outputting to miner " + entityOfMachineGettingInput.minerId.ToString());
                    }
                    else if (entityOfMachineGettingInput.tankId != 0)
                    {
                        Log.Debug("      Belt outputting to tank " + entityOfMachineGettingInput.tankId.ToString());
                        factory.factoryStorage.SetTankBelt(entityOfMachineGettingInput.tankId, lastBeltId, slotOfMachineGettingInput, true);
                    }
                    else if (entityOfMachineGettingInput.fractionatorId != 0)
                    {
                        Log.Debug("      Belt outputting to fractionator " + entityOfMachineGettingInput.fractionatorId.ToString());
                        factory.factorySystem.SetFractionatorBelt(entityOfMachineGettingInput.fractionatorId, lastBeltId, slotOfMachineGettingInput, true);
                    }
                    else if (entityOfMachineGettingInput.powerExcId != 0)
                    {
                        Log.Debug("      Belt outputting to power exchanger " + entityOfMachineGettingInput.powerExcId.ToString());
                        factory.powerSystem.SetExchangerBelt(entityOfMachineGettingInput.powerExcId, lastBeltId, slotOfMachineGettingInput, true);
                    }
                    else if (entityOfMachineGettingInput.stationId != 0)
                    {
                        Log.Debug("      Belt outputting to station " + entityOfMachineGettingInput.stationId.ToString());
                        factory.ApplyEntityOutput(entityIdOfMachineGettingInput, lastBelt.entityId, slotOfMachineGettingInput, slotOfMachineGettingInput, 0);
                        Log.Debug("         Station now set to " + factory.transport.stationPool[entityOfMachineGettingInput.stationId].slots[slotOfMachineGettingInput].dir.ToString());
                    }
                    factory.WriteObjectConnDirect(lastBelt.entityId, BELT_INPUT_SLOT, false, entityIdOfMachineGettingInput, slotOfMachineGettingInput);
                    factory.WriteObjectConnDirect(entityIdOfMachineGettingInput, slotOfMachineGettingInput, true, lastBelt.entityId, BELT_INPUT_SLOT);
                }

                if (grabbedItemsFlag)
                {
                    CargoPath newCargoPath = cargoTraffic.pathPool[cargoTraffic.beltPool[beltId].segPathId]; // Modified

                    int index = 4;
                    for (int cargoIdIdx = cargoIds.Count - 1; cargoIdIdx >= 0; --cargoIdIdx)
                    {
                        int insertCargoId = cargoIds[cargoIdIdx];
                        if (insertCargoId == 0)
                        {
                            index++;
                        }
                        else
                        {
                            if (index + 10 > newCargoPath.bufferLength)
                            {
                                Log.Info("New cargo path is not large enough to fit all the items from the original path.  Sending item to Icarus' inventory.");

                                CargoContainer cargoContainer = cargoPath.cargoContainer;
                                Cargo cargo = cargoContainer.cargoPool[insertCargoId];
                                int cargoItem = cargo.item;

                                cargoContainer.RemoveCargo(insertCargoId);
                                if (GameMain.mainPlayer.package.AddItemStacked(cargoItem, 1, 1, out int remainInc) == 1)
                                {
                                    UIItemup.Up(cargoItem, 1);
                                }
                            }
                            else
                            {
                                newCargoPath.InsertCargoDirect(index, insertCargoId);
                                index += 10;
                            }
                        }
                    }
                }

                cargoTraffic.RemoveCargoPath(cargoPath.id);

                // Audio comes from LDB.audios.  Good built-in choices are "warp-end" or "ui-click-2" (the upgrade sound).
                // Modified: play sound only for the player click the button
                if (NebulaModAPI.MultiplayerSession.Factories.PacketAuthor == NebulaModAPI.MultiplayerSession.LocalPlayer.Id)
                {
                    VFAudio.Create("ui-click-2", null, GameMain.mainPlayer.factory.entityPool[selectedBeltComponent.entityId].pos, true);
                }

            }            
        }
    }
}
