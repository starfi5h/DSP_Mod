using System;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {        
        int[] tmpVeinAmount;

        public void VeinWorkBegin()
        {
            if (GameMain.data.gameDesc.isInfiniteResource) return;

            if (IsNextIdle || Index == UIvein.ViewFactoryIndex)
            {
                if (tmpVeinAmount == null || tmpVeinAmount.Length != factory.veinPool.Length)
                {
                    Log.Debug($"VeinWorkBegin {tmpVeinAmount?.Length} -> {factory.veinPool.Length}");
                    tmpVeinAmount = new int[factory.veinPool.Length];
                }
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] = factory.veinPool[i].amount;
            }
        }

        public void VeinWorkEnd()
        {
            if (GameMain.data.gameDesc.isInfiniteResource) return;

            if (IsNextIdle || Index == UIvein.ViewFactoryIndex)
            {
                if (tmpVeinAmount == null || tmpVeinAmount.Length != factory.veinPool.Length)
                {
                    Log.Debug($"VeinWorkEnd {tmpVeinAmount?.Length} -> {factory.veinPool.Length}");
                    tmpVeinAmount = new int[factory.veinPool.Length];
                }
                else
                {
                    for (int i = 0; i < tmpVeinAmount.Length; i++)
                        tmpVeinAmount[i] -= factory.veinPool[i].amount; // consume amount (+)

                    if (Index == UIvein.ViewFactoryIndex)
                    {
                        UIvein.Record(factory, tmpVeinAmount);
                    }
                }
            }
        }

        public void VeinIdleEnd()
        {
            if (GameMain.data.gameDesc.isInfiniteResource) return;

            if (tmpVeinAmount != null)
            {
                int length = Math.Min(tmpVeinAmount.Length, factory.veinCursor);
                for (int i = 0; i < length; i++)
                {
                    if (tmpVeinAmount[i] != 0 && factory.veinPool[i].amount > 0)
                    {
                        int consumeAmount = Math.Min(tmpVeinAmount[i], factory.veinPool[i].amount);
                        short groupIndex = factory.veinPool[i].groupIndex;
                        factory.veinPool[i].amount -= consumeAmount;
                        factory.veinGroups[groupIndex].amount -= consumeAmount;
                        if (factory.veinPool[i].amount <= 0)
                        {
                            Log.Debug($"Factory[{Index}] ({factory.planet.displayName}): Remove vein {i}");
                            factory.RemoveVeinWithComponents(i);
                            factory.RecalculateVeinGroup(groupIndex);
                            factory.NotifyVeinExhausted();
                        }
                    }
                }
            }
        }
    }
}
