using HarmonyLib;
using System;
using System.Threading;

namespace SampleAndHoldSim
{
    public class Station_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
        static void UpdateVeinCollection_Prefix(StationComponent __instance, ref int __state)
        {
            __state = __instance.storage[0].count;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
        static void UpdateVeinCollection_Postfix(StationComponent __instance, int __state, PlanetFactory factory)
        {
            if (MainManager.TryGet(factory.index, out FactoryManager manager))
            {
                if (manager.IsActive)
                {
                    manager.SetMineral(__instance, __instance.storage[0].count - __state);
                    ref var miner = ref factory.factorySystem.minerPool[__instance.minerId];
                    if (miner.productCount < 0) miner.productCount = 0; // Fix miners that have negative tmp storage
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.FactoryTransportGameTick))]
        static bool FactoryTransportGameTick_Postfix(GameLogic __instance)
        {
            // PlanetTransport的運行邏輯單獨隔離出來, 不論active或idle每幀都會執行
            DeepProfiler.BeginSample(DPEntry.Transport, -1, -1L);
            for (int i = 0; i < GameMain.data.factoryCount; i++) //改用GameMain.data.factoryCount
            {
                PlanetFactory planetFactory = GameMain.data.factories[i]; //改用GameMain.data.factories
                bool flag = __instance.localLoadedFactory == planetFactory;
                planetFactory.transport.GameTick(GameMain.gameTick, flag, false, -1); //改用GameMain.gameTick
            }
            DeepProfiler.EndSample(-1, -2L);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.FactoryTransportGameTick_Parallel))]
        static bool FactoryTransportGameTick_Parallel(GameLogic __instance, int threadOrdinal)
        {
            BatchTaskContext planetTransport = __instance.threadController.gameThreadContext.planetTransport;
            bool flag;
            do
            {
                DeepProfiler.BeginSample(DPEntry.Scheduling, threadOrdinal, -1L);
                flag = false;
                int num = -1;
                int num2 = Interlocked.Increment(ref planetTransport.batchCursor) - 1;
                if (num2 < GameMain.data.factoryCount)
                {
                    num = num2;
                    flag = true;
                }
                DeepProfiler.EndSample(threadOrdinal, -2L);
                if (flag)
                {
                    num = planetTransport.batchIndices[num];
                    PlanetFactory planetFactory = GameMain.data.factories[num];
                    bool flag2 = __instance.localLoadedFactory == planetFactory;
                    DeepProfiler.BeginSample(DPEntry.Transport, threadOrdinal, planetFactory.planetId);
                    planetFactory.transport.GameTick(GameMain.gameTick, flag2, true, threadOrdinal);
                    DeepProfiler.EndSample(threadOrdinal, -2L);
                }
            }
            while (flag);
            return false;
        }
    }

    public partial class FactoryManager
    {
        // 用以下的執行順序紀錄貨物在非物流期間的變動量:
        // StationBeforeTick()
        // ...接口入塔
        // StationBeforeTransport()
        // ...物流期(小飛機,船)
        // StationAfterTransport()
        // ...接口出塔,爪子等
        // StationAfterTick()

        private StationData[] stationDataPool;

        public void SetMineral(StationComponent station, int mineralCount)
        {
            if (stationDataPool == null || station.id >= stationDataPool.Length) return;
            var stationData = stationDataPool[station.id];
            if (stationData != null)
            {
                StationData.SetMineral(stationData, mineralCount);
            }
        }

        public void StationBeforeTick()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveBegin, false);
            }
        }

        public void StationBeforeTransport()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveBeforeTransport, false);
            }
        }

        public void StationAfterTransport()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveAfterTransport, false);
            }
        }

        public void StationAfterTick()
        {
            if (IsActive)
            {
                Traversal(StationData.ActiveEnd, Index == UIstation.ViewFactoryIndex);
            }
            else
            {
                Traversal(StationData.IdleEnd, false);
            }
        }

        public void EnsureStationDataPoolSize(int size)
        {
            if (stationDataPool == null || stationDataPool.Length < size)
            {
                // 擴容策略，保留舊數據
                var newPool = new StationData[size + 32];
                if (stationDataPool != null)
                    Array.Copy(stationDataPool, newPool, stationDataPool.Length);
                stationDataPool = newPool;
            }
        }

        private void Traversal(Action<StationData, StationComponent> action, bool record)
        {
            PlanetTransport transport = factory.transport;
            if (stationDataPool == null || stationDataPool.Length <= transport.stationCursor)
                EnsureStationDataPoolSize(transport.stationCursor);

            for (int stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                var station = transport.stationPool[stationId];
                if (station != null)
                {
                    // Lazy initialization
                    if (stationDataPool[stationId] == null)
                        stationDataPool[stationId] = new StationData(station);

                    action(stationDataPool[stationId], station);

                    if (record && stationId == UIstation.ViewStationId)
                        UIstation.Record(stationDataPool[stationId]);
                }
                else
                {
                    // 如果 station 被移除，清空 data 以釋放引用
                    if (stationDataPool[stationId] != null)
                        stationDataPool[stationId] = null;
                }
            }
        }
    }

    public class StationData
    {
        public int[] deltaCount;
        public int[] deltaInc;
        int[] tmpCount;
        int[] tmpInc;
        int tmpWarperCount;
        int tmpMineralCount;

        public StationData (StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            SetArray(length);
        }

        private void SetArray(int length)
        {
            tmpCount = new int[length];
            tmpInc = new int[length];
            deltaCount = new int[length];
            deltaInc = new int[length];
        }

        public static void SetMineral(StationData data, int mineralCount)
        {            
            data.tmpMineralCount = mineralCount;
        }

        public static void ActiveBegin(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length) data.SetArray(length);

            for (int i = 0; i < length; i++)
            {
                data.tmpCount[i] = station.storage[i].count;
                data.tmpInc[i] = station.storage[i].inc;
                data.deltaCount[i] = 0;
                data.deltaInc[i] = 0;
            }
        }

        public static void ActiveBeforeTransport(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length)
            {
                // 如果新的station在這個期間放置,則不紀錄變動
                data.SetArray(length);
                data.tmpWarperCount = 0;
                data.tmpMineralCount = 0;
                return;
            }

            for (int i = 0; i < length; i++)
            {               
                data.deltaCount[i] += station.storage[i].count - data.tmpCount[i];
                data.deltaInc[i] += station.storage[i].inc - data.tmpInc[i];
            }
        }

        public static void ActiveAfterTransport(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length) data.SetArray(length);

            for (int i = 0; i < length; i++)
            {
                data.tmpCount[i] = station.storage[i].count;
                data.tmpInc[i] = station.storage[i].inc;
            }
            data.tmpWarperCount = station.warperCount;
            // tmpMineralCount is set inside PlanetTransport.GameTick()
        }

        public static void ActiveEnd(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length)
            {
                // 如果新的station在這個期間放置,則不紀錄變動
                data.SetArray(length);
                data.tmpWarperCount = 0;
                data.tmpMineralCount = 0;
                return;
            }

            for (int i = 0; i < length; i++)
            {
                data.deltaCount[i] += station.storage[i].count - data.tmpCount[i];
                data.deltaInc[i] += station.storage[i].inc - data.tmpInc[i];

                // 在正常模式下, 一個物流塔一幀最多有16 slot * 4 stack = 64 cargo/tick的進出
                if (data.deltaCount[i] > 64)
                {
                    //Log.Debug($"station:{station.planetId}-{station.id} [{i}] itemId:{station.storage[i].itemId} delta:{data.deltaCount[i]}");
                    data.deltaCount[i] = 64;
                }
                else if (data.deltaCount[i] < -64)
                {
                    //Log.Debug($"station:{station.planetId}-{station.id} [{i}] itemId:{station.storage[i].itemId} delta:{data.deltaCount[i]}");
                    data.deltaCount[i] = -64;
                }

                // DEBUG for abnormal change
                // if (data.deltaCount[i] >= 1000)
                //    Log.Debug($"station: {station.planetId} - {station.id} [{i}] itemId:{station.storage[i].itemId} diff:{data.tmpCount[i]}");

            }
            data.tmpWarperCount = station.warperCount - data.tmpWarperCount;
        }

        public static void IdleEnd(StationData data, StationComponent station)
        {
            int length = station.storage?.Length ?? 0;
            if (length != data.tmpCount.Length) data.SetArray(length);

            for (int i = 0; i < length; i++)
            {
                station.storage[i].count += data.deltaCount[i];
                station.storage[i].inc += data.deltaInc[i];
                if (station.storage[i].count < MainManager.StationStoreLowerbound)
                {
                    //Log.Debug($"station{station.id} - store{i}: {station.storage[i].count}");
                    station.storage[i].count = MainManager.StationStoreLowerbound;
                }
            }
            if (station.isVeinCollector && data.tmpMineralCount > 0)
                station.storage[0].count += data.tmpMineralCount;
            else
                station.warperCount += data.tmpWarperCount;
        }
    }
}
