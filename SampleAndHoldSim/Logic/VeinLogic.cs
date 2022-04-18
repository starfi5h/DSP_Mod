using System;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {        
        int[] tmpVeinAmount;

        public void VeinWorkBegin()
        {
            if (IsNextIdle || Index == UIvein.ViewFactoryIndex)
            {
                if (tmpVeinAmount == null || tmpVeinAmount.Length != factory.veinPool.Length)
                    tmpVeinAmount = new int[factory.veinPool.Length];
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] = factory.veinPool[i].amount;
            }
        }

        public void VeinWorkEnd()
        {
            if (IsNextIdle || Index == UIvein.ViewFactoryIndex)
            {
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] -= factory.veinPool[i].amount; // consume amount (+)

                if (Index == UIvein.ViewFactoryIndex)
                {
                    UIvein.AdvanceCursor();
                    for (int i = 0; i < factory.veinCursor; i++)
                        UIvein.Record(factory.veinPool[i].groupIndex, tmpVeinAmount[i]);
                }
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
                        int consumeAmount = tmpVeinAmount[i] <= factory.veinPool[i].amount ? tmpVeinAmount[i] : factory.veinPool[i].amount;
                        factory.veinPool[i].amount -= consumeAmount;
                        factory.planet.veinGroups[factory.veinPool[i].groupIndex].amount -= consumeAmount;
                    }
                }
            }
        }
    }
}
