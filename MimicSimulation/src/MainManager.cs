using System;
using System.Collections.Generic;

namespace MimicSimulation
{
    public static class MainManager
    {
        public static int MaxFactoryCount { get; set; } = 100;
        public static bool ActiveLocalFactory { get; set; }
        public static List<FactoryManager> Factories { get; } = new List<FactoryManager>();
        public static Dictionary<int, FactoryManager> Planets { get; } = new Dictionary<int, FactoryManager>();

        static int factoryCursor;

        public static void Init()
        {
            Factories.Clear();
            Planets.Clear();
            factoryCursor = 0;
        }

        public static int SetFactories(PlanetFactory[] workFactories, PlanetFactory[] idleFactories)
        {
            for (int index = Factories.Count; index < GameMain.data.factoryCount; index++)
            {
                FactoryManager manager = new FactoryManager(index, GameMain.data.factories[index]);
                Factories.Add(manager);
                Planets.Add(GameMain.data.factories[index].planetId, manager);
            }
            int workFactoryCount = Math.Min(MaxFactoryCount, GameMain.data.factoryCount);
            int idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;
            int newCursor = 0;
            int workIndex = 0;
            int idleIndex = 0;
            int nextIdleCount = 0;
            int nextWorkCount = 0;
            int localId = ActiveLocalFactory ? (GameMain.localPlanet?.factoryIndex ?? -1) : -1;
            if (localId != -1)
            {
                workFactories[workIndex++] = GameMain.data.factories[localId];
                Factories[localId].IsActive = true;
                Factories[localId].IsNextIdle = false;
            }

            int i = factoryCursor;
            do
            {
                i = (++i) % GameMain.data.factoryCount;
                if (i == localId)
                    continue;

                if (workIndex < workFactoryCount)
                {
                    workFactories[workIndex++] = GameMain.data.factories[i];
                    Factories[i].IsActive = true;
                    Factories[i].IsNextIdle = nextIdleCount++ < idleFactoryCount;
                    Factories[i].IsNextWork = false;
                    newCursor = i;
                }
                else
                {
                    idleFactories[idleIndex++] = GameMain.data.factories[i];
                    Factories[i].IsActive = false;
                    Factories[i].IsNextWork = nextWorkCount++ < workFactoryCount;
                    Factories[i].IsNextIdle = false;
                }
            } while (i != factoryCursor);
            factoryCursor = newCursor;

            return workFactoryCount;
        }

        public static bool TryGet(int index, out FactoryManager factoryData)
        {
            factoryData = null;
            if (index <= Factories.Count)
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
        public bool IsNextIdle; // will idle next tick
        public bool IsNextWork; // will work next tick
        public int Index;
        readonly PlanetFactory factory;

        public FactoryManager(int index, PlanetFactory factory)
        {
            Index = index;
            this.factory = factory;
        }
    }
}
