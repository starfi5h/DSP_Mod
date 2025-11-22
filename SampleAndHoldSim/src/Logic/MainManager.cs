using System.Collections.Generic;

namespace SampleAndHoldSim
{
    /// <summary>
    /// 管理並追蹤所有工廠的狀態和週期性更新。
    /// </summary>
    public static class MainManager
    {
        /// <summary>
        /// 遊戲工廠更新的週期。
        /// 數值為 N 表示每 N 個遊戲刻度 (tick) 更新一次工廠。
        /// 預設值為 1 (每個刻度都更新)。
        /// </summary>
        public static int UpdatePeriod { get; set; } = 1;

        /// <summary>
        /// 是否優先專注於 (總是更新) 玩家當前所在星球上的工廠。
        /// 預設值為 <c>true</c>。
        /// </summary>
        public static bool FocusLocalFactory { get; set; } = true;

        /// <summary>
        /// 物流站儲存格的下限值 (負值)。
        /// </summary>
        public static int StationStoreLowerbound { get; private set; } = -64;

        /// <summary>
        /// 所有已知的工廠管理器 (<see cref="FactoryManager"/>) 列表。
        /// </summary>
        public static List<FactoryManager> Factories { get; } = new List<FactoryManager>();

        /// <summary>
        /// 以星球 ID 為鍵的工廠管理器 (<see cref="FactoryManager"/>) 字典。
        /// 用於透過星球 ID 快速查找對應的工廠管理器。
        /// </summary>
        public static Dictionary<int, FactoryManager> Planets { get; } = new Dictionary<int, FactoryManager>();

        /// <summary>
        /// 當前專注的星系索引。主要用於 DSP_Battle 相關邏輯。預設值為 -1。
        /// </summary>
        public static int FocusStarIndex { get; set; } = -1; // Use by DSP_Battle

        /// <summary>
        /// 當前專注的星球 ID。
        /// 只有在 <see cref="FocusLocalFactory"/> 為 <c>true</c> 且 <see cref="UpdatePeriod"/> > 1 時被設置。
        /// </summary>
        public static int FocusPlanetId { get; private set; } = -1;

        /// <summary>
        /// 當前專注的工廠索引。
        /// 只有在 <see cref="FocusLocalFactory"/> 為 <c>true</c> 且 <see cref="UpdatePeriod"/> > 1 時被設置。
        /// </summary>
        public static int FocusFactoryIndex { get; private set; } = -1;

        /// <summary>
        /// 初始化管理器，清除工廠列表並根據 <see cref="UpdatePeriod"/> 更新 <see cref="StationStoreLowerbound"/>。
        /// </summary>
        public static void Init()
        {
            Factories.Clear();
            Planets.Clear();
            StationStoreLowerbound = UpdatePeriod * -64; // 16 slot * 4 stack
            Log.Debug("UpdatePeriod = " + UpdatePeriod + ", FocusLocalFactory = " + FocusLocalFactory + ", StoreLowerbound = " + StationStoreLowerbound);
        }

        /// <summary>
        /// 設置哪些工廠應該在這個遊戲刻度執行工作 (workFactories)，哪些應該處於閒置 (idleFactories)。
        /// 並在需要時初始化新的 <see cref="FactoryManager"/>。
        /// </summary>
        /// <param name="workFactories">將被設置為工作狀態的工廠陣列。</param>
        /// <param name="idleFactories">將被設置為閒置狀態的工廠陣列。</param>
        /// <param name="workFactoryTimes">與 <paramref name="workFactories"/> 對應的時間參數陣列，用於工作計算。</param>
        /// <returns>實際被安排為工作狀態的工廠數量。</returns>
        public static int SetFactories(PlanetFactory[] workFactories, PlanetFactory[] idleFactories, long[] workFactoryTimes)
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
                if (Factories[i].factory != factories[i]) // sanity check
                {
                    Factories[i].factory = factories[i];
                }

                if ((FocusLocalFactory && (i == localFactoryId)))
                {
                    workFactories[workFactoryCount] = factories[i];
                    workFactoryTimes[workFactoryCount] = time;
                    workFactoryCount++;
                    Factories[i].IsActive = true;
                    Factories[i].IsNextIdle = false; // focused local factory always active
                }
                else if ((i + time) % UpdatePeriod == 0)
                {
                    workFactories[workFactoryCount] = factories[i];
                    workFactoryTimes[workFactoryCount] = time / UpdatePeriod; // scale down the time argument to fix % operations
                    workFactoryCount++;
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

            FocusFactoryIndex = (FocusLocalFactory && UpdatePeriod > 1) ? localFactoryId : -1;
            FocusPlanetId = (FocusLocalFactory && UpdatePeriod > 1 && GameMain.localPlanet != null) ? GameMain.localPlanet.id : -1;
            FocusStarIndex = (FocusLocalFactory && UpdatePeriod > 1 && GameMain.localStar != null) ? GameMain.localStar.index : -1;
            return workFactoryCount;
        }

        /// <summary>
        /// 嘗試根據工廠索引獲取對應的 <see cref="FactoryManager"/>。
        /// </summary>
        /// <param name="index">工廠的索引。</param>
        /// <param name="factoryData">如果找到，則返回對應的 <see cref="FactoryManager"/>；否則為 <c>null</c>。</param>
        /// <returns>如果找到對應的工廠管理器則返回 <c>true</c>；否則返回 <c>false</c>。</returns>
        public static bool TryGet(int index, out FactoryManager factoryData)
        {
            factoryData = null;
            if (0 <= index && index < Factories.Count) // Black simulate factory using index -1
            {
                factoryData = Factories[index];
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 用於管理單個星球工廠的狀態和相關數據。
    /// </summary>
    public partial class FactoryManager
    {
        public bool IsActive; // is working, excuting functions in GameLogic.GameTick()
        public bool IsNextIdle; // will turn into idle next tick
        public int Index;
        public PlanetFactory factory;

        public FactoryManager(int index, PlanetFactory factory)
        {
            Index = index;
            this.factory = factory;
        }
    }
}
