using System;

namespace MimicSimulation
{
    public partial class FactoryManager
    {
        int[] tmpVeinAmount;

        public void VeinWorkBegin()
        {
            if (IsNextIdle)
            {
                if (tmpVeinAmount == null || tmpVeinAmount.Length != factory.veinPool.Length)
                    tmpVeinAmount = new int[factory.veinPool.Length];
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] = factory.veinPool[i].amount;
            }
        }

        public void VeinWorkEnd()
        {
            if (IsNextIdle)
            {
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] = factory.veinPool[i].amount - tmpVeinAmount[i];
            }
        }

        public void VeinIdleEnd()
        {
            if (tmpVeinAmount != null)
            {
                int length = Math.Min(tmpVeinAmount.Length, factory.veinCursor);
                for (int i = 0; i < length; i++)
                {
                    if (tmpVeinAmount[i] != 0)
                    {
                        factory.veinPool[i].amount += tmpVeinAmount[i];
                        factory.planet.veinGroups[factory.veinPool[i].groupIndex].amount += tmpVeinAmount[i];
                    }
                }
            }
        }
    }
}
