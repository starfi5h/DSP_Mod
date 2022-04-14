using System;
using System.Collections.Generic;

namespace MimicSimulation
{
    public static class FactoryPool
    {
        public static int MaxFactoryCount { get; set; } = 100;
        public static float Ratio { get; set; } = 1f;
        public static List<FactoryData> Factories { get; } = new List<FactoryData>();
        public static Dictionary<int, FactoryData> Planets { get; } = new Dictionary<int, FactoryData>();


        public static void Init()
        {
            Factories.Clear();
            Planets.Clear();
        }

        public static void SetFactories()
        {
            if (MaxFactoryCount < GameMain.data.factoryCount)
                Ratio = MaxFactoryCount > 1 ? (float)GameMain.data.factoryCount / (MaxFactoryCount) : -1f;
            else
                Ratio = 1f;

            for (int i = Factories.Count; i < GameMain.data.factoryCount; i++)
            {
                Factories.Add(new FactoryData(i, GameMain.data.factories[i]));
                Planets.Add(GameMain.data.factories[i].planetId, Factories[i]);
            }
        }

        public static bool TryGet(int index, out FactoryData factoryData)
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

    public partial class FactoryData
    {
        public bool IsActive;
        public int Index;
        public PlanetFactory Factory;

        public FactoryData(int index, PlanetFactory factory)
        {
            Index = index;
            Factory = factory;
        }
    }
}
