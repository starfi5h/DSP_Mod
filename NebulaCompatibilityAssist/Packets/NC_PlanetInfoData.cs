using NebulaAPI;
using NebulaCompatibilityAssist.Patches;

namespace NebulaCompatibilityAssist.Packets
{
    public class NC_PlanetInfoData
    {
        public int PlanetId { get; set; }
        public long EnergyCapacity { get; set; }
        public long EnergyRequired { get; set; }
        public long EnergyExchanged { get; set; }
        public int NetworkCount { get; set; }

        public NC_PlanetInfoData() { }
        public NC_PlanetInfoData(in PlanetData planet)
        {
            PlanetId = planet.id;
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
                        EnergyExchanged += powerNetwork.energyExchanged;
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

            PlanetFinder.OnReceive(packet);
        }
    }
}
