using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    // 在GameLogic.OnFactoryFrameBegin後, timei = GameMain.timei / scale
    // 對於全域和本地focus工廠, 我們需要把計算用的時間還原成GameMain.timei
    public class GameTick_Patch
    {
        // 參考GameMain.gameTick的值要適當的調整, 避免直接使用
        public static long GetGameTick(FactorySystem factorySystem)
        {
            // Return the modified gameTick
            int scale = factorySystem.factory.planetId == MainManager.FocusPlanetId ? 1 : MainManager.UpdatePeriod;
            return GameMain.gameTick / scale;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.GameTick))]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.GameTick))]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.BulletGameTick))]
        [HarmonyPatch(typeof(DysonSwarm), nameof(DysonSwarm.ExecuteDeferredAddSolarSail))] // 修正AddSolarSail的expiryTime
        [HarmonyPatch(typeof(TrafficStatistics), nameof(TrafficStatistics.GameTick_Parallel))] // 統計的ILS進出淡條
        [HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.GameTick))] // 黑霧巢穴太空位置更新
        static void Time_Correct(ref long time)
        {
            time = GameMain.gameTick;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(typeof(DysonSphere), nameof(DysonSphere.GameTick))]
        static void GameTick_Correct(ref long gameTick)
        {
            // 不能直接改GameLogic.timei, 會報錯
            gameTick = GameMain.gameTick;
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(TrashSystem), nameof(TrashSystem.AddTrashFromGroundEnemy))]
        public static void AddTrashFromGroundEnemy_Prefix(PlanetFactory factory, ref int life)
        {
            // Scale the life (1800) of dark fog drop on remote planets
            if (factory.planetId != MainManager.FocusPlanetId)
                life *= MainManager.UpdatePeriod;
        }


        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        [HarmonyPatch(typeof(PlanetATField), nameof(PlanetATField.GameTick))]
        static void LocalTick_Correct(ref long tick, bool isActive)
        {
            if (isActive && MainManager.FocusLocalFactory)
            {
                tick = GameMain.gameTick;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(typeof(CombatGroundSystem), nameof(CombatGroundSystem.GameTick))]
        [HarmonyPatch(typeof(CombatGroundSystem), nameof(CombatGroundSystem.PostGameTick))]
        static void LocalTick_Correct(ref long tick, PlanetFactory ___factory)
        {
            if (___factory.index == MainManager.FocusFactoryIndex)
            {
                tick = GameMain.gameTick;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.GameTickLogic_Turret))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.GameTickLogic_Unit))]
        static void LocalGameTick_Correct(ref long gameTick, PlanetFactory ___factory)
        {
            if (___factory.index == MainManager.FocusFactoryIndex)
            {
                gameTick = GameMain.gameTick;
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.VeryHigh)]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static void PowerSystem_Gametick(PowerSystem __instance, ref long time)
        {
            if (__instance.factory.index == MainManager.FocusFactoryIndex)
            {
                // Restore real time for focused local factory
                time = GameMain.gameTick;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.PostGameTick))]
        static void OnEnemyDFGroundSystemPostGameTick(EnemyDFGroundSystem __instance)
        {
            if (__instance.factory.index != MainManager.FocusFactoryIndex) return;

            // 擷取EnemyDFGroundSystem.GameTickLogic_Unit
            // 對於本地工廠, 需要60tick更新一次enemyData的hash
            // 這樣無人攻擊機的DiscoverLocalEnemy才能找到目標
            ref EnemyData[] ptr = ref __instance.factory.enemyPool;
            if (__instance.units.count == 0) return;


            int gene = (int)(GameMain.gameTick % 60L);
            EnemyUnitComponent[] buffer = __instance.units.buffer;
            int cursor = __instance.units.cursor;
            var hashSystem = __instance.factory.hashSystemDynamic;

            for (int i = 1; i < cursor; i++)
            {
                if (i % 60 == gene)
                {
                    ref EnemyUnitComponent ptr2 = ref buffer[i];
                    if (ptr2.id != i) continue;                    
                    ref EnemyData ptr3 = ref ptr[ptr2.enemyId];
                    ptr3.hashAddress = hashSystem.UpdateObjectHashAddress(ptr3.hashAddress, ptr3.id, ptr3.pos, EObjectType.Enemy);
                }
            }

        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTick))]
        static void FactorySystemGameTick_Prefix(FactorySystem __instance, ref long time)
        {
            // Fix ejector auto reorbit
            time = GetGameTick(__instance);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickLabOutputToNext))]
        static IEnumerable<CodeInstruction> GameTickLabOutput_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // frequency of item transition across levels in stacked Labs now reduce to 1/5 tick after game 0.10.31
                // Replace: (int)(GameMain.gameTick % 5L)
                // To: (int)(GetGameTick(this) % 5L)

                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_gameTick"));

                if (codeMatcher.IsInvalid) // game version before 0.10.31.24646
                {
                    Log.Warn("GameTickLabOutput_Transpiler: Can't find get_gameTick!");
                    return instructions;
                }

                codeMatcher.Set(OpCodes.Nop, null)
                    .Advance(1).Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameTick_Patch), nameof(GetGameTick))));

                return codeMatcher.InstructionEnumeration();
            }
            catch
            {
                Log.Error("Transpiler FactorySystem.GameTickLabOutputToNext failed.");
                return instructions;
            }
        }

        /// <summary>
        /// Corrective pass for local factory lab output in parallel mode.
        /// Called from ThreadManager_Patch.OnPhaseEnd after all threads finish FactoryLabOutput.
        /// The parallel method uses divided timei for num2, which is wrong for the focused local factory.
        /// This runs UpdateOutputToNext for labs that were skipped.
        /// </summary>
        public static void FixLocalLabOutput()
        {
            // 對於多線程LabOutputToNext的補救方法: 將本地星球的研究站上下傳遞補齊
            if (MainManager.FocusFactoryIndex < 0 || MainManager.FocusFactoryIndex >= GameMain.data.factories.Length) return;
            var localFactory = GameMain.data.factories[MainManager.FocusFactoryIndex];

            int gene = (int)(GameMain.gameTick & 3L);
            var factorySystem = localFactory?.factorySystem;
            if (factorySystem == null) return;
            for (int i = 1; i < factorySystem.labCursor; i++)
            {
                ref LabComponent ptr = ref factorySystem.labPool[i];
                if (ptr.id == i)
                {
                    if ((i & 3) == gene && ptr.nextLabId > 0)
                    {
                        // 這個做法沒有檢查原版已重複的搬運, 但可以保證本地的lab 每 3 tick 必更新一次
                        ptr.UpdateOutputToNext(factorySystem.labPool);
                    }
                }
            }
        }
    }
}
