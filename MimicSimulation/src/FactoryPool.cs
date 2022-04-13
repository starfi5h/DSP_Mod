using System;
using System.Collections.Generic;

namespace MimicSimulation
{
    public static class FactoryPool
    {
        public static int MaxFactoryCount { get; set; } = 100;
        public static List<FactoryData> Factories { get; } = new List<FactoryData>();

        public static void Init()
        {
            Factories.Clear();
        }

        public static void SetFactories()
        {
            for (int i = Factories.Count; i < GameMain.data.factoryCount; i++)
            {
                Factories.Add(new FactoryData(i, GameMain.data.factories[i]));
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
