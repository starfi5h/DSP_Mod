﻿using NebulaAPI;
using NebulaCompatibilityAssist.Patches;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_PlanetInfoData
    {
        public int PlanetId { get; set; }
        public long[] VeinAmounts { get; set; }
        public long EnergyCapacity { get; set; }
        public long EnergyRequired { get; set; }
        public int NetworkCount { get; set; }

        public NC_PlanetInfoData() { }
        public NC_PlanetInfoData(in PlanetData planet)
        {
            PlanetId = planet.id;
            VeinAmounts = planet.veinAmounts;
            if (planet.factory?.powerSystem != null)
            {
                for (int i = 1; i < planet.factory.powerSystem.netCursor; i++)
                {
                    PowerNetwork powerNetwork = planet.factory.powerSystem.netPool[i];
                    if (powerNetwork != null && powerNetwork.id == i)
                    {
                        NetworkCount++;
                        EnergyCapacity += powerNetwork.energyCapacity;
                        EnergyRequired += powerNetwork.energyRequired;
                    }
                }
            }
        }
    }

    [RegisterPacketProcessor]
    internal class NC_PlanetInfoDataProcessor : BasePacketProcessor<NC_PlanetInfoData>
    {
        public override void ProcessPacket(NC_PlanetInfoData packet, INebulaConnection conn)
        {
            if (IsHost) return;

            PlanetData planet = GameMain.galaxy.PlanetById(packet.PlanetId);
            planet.veinAmounts = packet.VeinAmounts;
            PlanetFinder.OnReceive(packet);
        }
    }
}