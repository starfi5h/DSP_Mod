using System;

namespace SampleAndHoldSim
{
    public partial class FactoryManager
    {        
        int[] tmpVeinAmount;
        bool vaild;

        public void VeinWorkBegin()
        {
            if (GameMain.data.gameDesc.isInfiniteResource) return;

            if (IsNextIdle)
            {
                if (tmpVeinAmount == null || tmpVeinAmount.Length != factory.veinPool.Length)
                {
                    tmpVeinAmount = new int[factory.veinPool.Length];
                }
                for (int i = 0; i < factory.veinCursor; i++)
                    tmpVeinAmount[i] = factory.veinPool[i].amount;
                vaild = false;
            }
        }

        public void VeinWorkEnd()
        {
            if (GameMain.data.gameDesc.isInfiniteResource || tmpVeinAmount == null) return;

            if (IsNextIdle)
            {
                int length = Math.Min(tmpVeinAmount.Length, factory.veinCursor);
                for (int i = 0; i < length; i++)
                    tmpVeinAmount[i] -= factory.veinPool[i].amount; // consume amount (+)
                vaild = true; // other mods may skip VeinWorkEnd() so we need to check vaild of tmpVeinAmount
            }
        }

        public void VeinIdleEnd()
        {
            if (GameMain.data.gameDesc.isInfiniteResource || tmpVeinAmount == null) return;
            if (!vaild) return;

            int length = Math.Min(tmpVeinAmount.Length, factory.veinCursor);
            for (int i = 0; i < length; i++)
            {
                if (tmpVeinAmount[i] > 0  && tmpVeinAmount[i] <= 4) // Guard: Assume vein reduce amount < 240/s
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
