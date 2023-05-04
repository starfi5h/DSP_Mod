using System.Collections.Generic;

namespace SampleAndHoldSim
{
    public static class MainManager
    {
        public static int UpdatePeriod { get; set; } = 1;
        public static bool FocusLocalFactory { get; set; } = true;
        public static int StationStoreLowerbound { get; private set; } = -64;
        public static List<FactoryManager> Factories { get; } = new List<FactoryManager>();
        public static Dictionary<int, FactoryManager> Planets { get; } = new Dictionary<int, FactoryManager>();
        public static int FocusStarIndex { get; set; } = -1; // Use by DSP_Battle

        public static void Init()
        {
            Factories.Clear();
            Planets.Clear();
            StationStoreLowerbound = UpdatePeriod * -64; // 16 slot * 4 stack
            Log.Debug("UpdatePeriod = " + UpdatePeriod + ", FocusLocalFactory = " + FocusLocalFactory + ", StoreLowerbound = " + StationStoreLowerbound);
        }

        public static int SetFactories(PlanetFactory[] workFactories, PlanetFactory[] idleFactories)
        {
            for (int index = Factories.Count; index < GameMain.data.factoryCount; index++)
            {
                FactoryManager manager = new FactoryManager(index, GameMain.data.factories[index]);
                Factories.Add(manager);
                Planets.Add(GameMain.data.factories[index].planetId, manager);
            }

            int workFactoryCount = 0;
            int idleFactoryCount = 0;
            int time = (int)GameMain.gameTick;
            PlanetFactory[] factories = GameMain.data.factories;
            int localFactoryId = GameMain.localPlanet?.factory?.index ?? -1; // unexplored planet may not have factory on it
            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                if ((FocusLocalFactory && (i == localFactoryId)) || (factories[i].planetId / 100 - 1) == FocusStarIndex)
                {
                    workFactories[workFactoryCount++] = factories[i];
                    Factories[i].IsActive = true;
                    Factories[i].IsNextIdle = false; // focused local factory always active
                }
                else if ((i + time) % UpdatePeriod == 0)
                {
                    workFactories[workFactoryCount++] = factories[i];
                    Factories[i].IsActive = true;
                    Factories[i].IsNextIdle = UpdatePeriod > 1; // vanilla: UpdatePeriod = 1
                }
                else
                {
                    idleFactories[idleFactoryCount++] = factories[i];
                    Factories[i].IsActive = false;
                    Factories[i].IsNextIdle = false;
                }
            }

            return workFactoryCount;
        }

        public static bool TryGet(int index, out FactoryManager factoryData)
        {
            factoryData = null;
            if (0 <= index && index <= Factories.Count) // Black simulate factory using index -1
            {
                factoryData = Factories[index];
                return true;
            }
            return false;
        }
    }

    public partial class FactoryManager
    {
        public bool IsActive; // is working, excuting functions in GameData.GameTick()
        public bool IsNextIdle; // will turn into idle next tick
        public int Index;
        public readonly PlanetFactory factory;

        public FactoryManager(int index, PlanetFactory factory)
        {
            Index = index;
            this.factory = factory;
        }
    }
}
