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
            }
        }
    }
}
