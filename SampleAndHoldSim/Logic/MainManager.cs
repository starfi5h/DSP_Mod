using System;
using System.Collections.Generic;

namespace SampleAndHoldSim
{
    public static class MainManager
    {
        public static int MaxFactoryCount { get; set; } = 100;
        public static bool FocusLocalFactory { get; set; } = true;
        public static List<FactoryManager> Factories { get; } = new List<FactoryManager>();
        public static Dictionary<int, FactoryManager> Planets { get; } = new Dictionary<int, FactoryManager>();

        static int factoryCursor;
        static int remainCount;

        public static void Init()
        {
            Factories.Clear();
            Planets.Clear();
            factoryCursor = 0;
            remainCount = GameMain.data.factoryCount;
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
            int step = workFactoryCount;
            int workIndex = 0;
            int idleIndex = 0;
            int localId = FocusLocalFactory ? (GameMain.localPlanet?.factoryIndex ?? -1) : -1;
            if (localId != -1)
            {
                workFactories[workIndex++] = GameMain.data.factories[localId];
                Factories[localId].IsActive = true;
                Factories[localId].IsNextIdle = false;
                Factories[localId].IsNextWork = false;
                step--;
                if (FocusLocalFactory && MaxFactoryCount == 1 && GameMain.data.factoryCount > 1)
                {
                    // There are remote planet factories unreachable
                    Log.Debug("Remote factories unreachable, set MaxFactoryCount from 1 to 2");
                    MaxFactoryCount = 2;
                    workFactoryCount = 2;
                    step = 1;
                    UIcontrol.OnFactroyCountChange();
                }
            }

            if (step < remainCount)
            {
                remainCount -= step;
            }
            else
            {
                workFactoryCount = remainCount;
                remainCount = GameMain.data.factoryCount;
                if (localId != -1)
                {
                    workFactoryCount++;
                    remainCount--;
                }
            }
            int idleFactoryCount = GameMain.data.factoryCount - workFactoryCount;

            int newCursor = 0;
            int nextIdleCount = 0;
            int nextWorkCount = 0;

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

            /*
            string str = $"count{workFactoryCount}: "; // for debug
            for (workIndex = 0; workIndex < workFactoryCount; workIndex++)
                str += workFactories[workIndex].index + " ";
            Log.Debug(str);
            */

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
