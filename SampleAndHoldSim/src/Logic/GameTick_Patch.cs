using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.GameTick))]
        static void PowerSystem_Gametick(PowerSystem __instance, ref long time)
        {
            if (MainManager.TryGet(__instance.factory.index, out var manager))
            {
                // Fix len consumption rate in idle factory
                // bool useCata = time % 10L == 0L;
                if (manager.IsNextIdle)
                    time /= MainManager.UpdatePeriod;
                else if (__instance.factory.index == MainManager.FocusFactoryIndex)
                    time = GameMain.gameTick; // Restore real time for focused local factory
            }
        }

        /// <summary>
        /// Corrective pass for local factory lab output in parallel mode.
        /// Called from ThreadManager_Patch.OnPhaseEnd after all threads finish FactoryLabOutput.
        /// The parallel method uses divided timei for num2, which is wrong for the focused local factory.
        /// This runs UpdateOutputToNext for labs that were skipped due to incorrect num2.
        /// </summary>
        public static void FixLocalLabOutput()
        {
            if (MainManager.FocusFactoryIndex < 0 || MainManager.UpdatePeriod <= 1) return;

            int correctNum = (int)(GameMain.gameTick & 3L);
            int wrongNum = (int)((GameMain.gameTick / MainManager.UpdatePeriod) & 3L);
            if (correctNum == wrongNum) return;

            var factory = GameMain.data.factories[MainManager.FocusFactoryIndex];
            var labPool = factory.factorySystem.labPool;
            int labCursor = factory.factorySystem.labCursor;

            for (int i = 1; i < labCursor; i++)
            {
                ref LabComponent lab = ref labPool[i];
                if (lab.id == i && (i & 3) == correctNum && lab.nextLabId > 0)
                    lab.UpdateOutputToNext(labPool);
            }
        }
    }
}
