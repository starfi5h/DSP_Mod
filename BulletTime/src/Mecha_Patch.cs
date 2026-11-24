using BulletTime.Nebula;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BulletTime
{
    // Speeding up shrinks Time.fixedDeltaTime (FPSController.SetFixUPS), so there are more logic ticks per real second.
    // On a planet the mecha moves through its Unity rigidbody, which runs on the physics clock, but some mecha logic
    // still counts a fixed 1/60 s per tick. At 10x these go 10 times too fast:
    // - the double-tap take-off window (PlayerMove_Walk.flyUpChance, 0.8 s) closes in 0.08 s
    // - walking, flying and hovering cost 10 times the energy per second
    // - Shift + rotate turns a miner by 2.011694 degrees per tick
    // - the wanted flight altitude rises per tick, so the mecha enters space with almost no climb left and can drop back
    // Each fix below puts one of them back on the physics clock. Nothing changes at 1x or slower, in pause mode, or in multiplayer.
    class Mecha_Patch
    {
        const float Tick = 1f / 60f;
        const float RotateStep = 2.011694f; // BuildTool_Click.DeterminePreviews: yaw +/-= 2.011694f per tick
        const double SpaceEntryClimb = 12.0; // m/s the mecha still climbs when it enters space at 1x (measured 11-13)

        static float lastFlyUpChance;
        static EMovementState lastMovementState;

        // Physics time of one logic tick as a share of 1/60 s: 1 at 1x, 0.1 at 10x
        static float TickRatio => Time.fixedDeltaTime * 60f;

        static bool IsActive(out float ratio)
        {
            ratio = TickRatio;
            return ratio < 0.999f
                && BulletTimePlugin.FixMechaAtSpeedUp.Value
                && !GameStateManager.Pause
                && !NebulaCompat.IsMultiplayerActive;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        static void FixedUpdate_Postfix()
        {
            // GameMain.FixedUpdate runs at most one tick while the mecha is alive
            var player = GameMain.mainPlayer;
            if (player == null || player.controller == null || DSPGame.IsMenuDemo) return;
            bool active = IsActive(out float ratio);

            // Take-off window: the walk tick took 1/60 s off; give back the part that did not pass
            var walk = player.controller.actionWalk;
            if (walk != null)
            {
                float chance = walk.flyUpChance;
                if (active && chance > 0f && chance < lastFlyUpChance)
                {
                    chance = Mathf.Min(chance + Tick * (1f - ratio), lastFlyUpChance);
                    walk.flyUpChance = chance;
                }
                lastFlyUpChance = chance;
            }

            // Space entry: on the tick the mecha goes from flying into space, top up its climb to the 1x value
            var movementState = player.movementState;
            bool enteredSpace = movementState == EMovementState.Sail && lastMovementState == EMovementState.Fly;
            lastMovementState = movementState;
            if (active && enteredSpace && GameMain.localPlanet != null)
            {
                var planet = GameMain.localPlanet;
                VectorLF3 up = (player.uPosition - planet.uPosition).normalized;
                VectorLF3 ground = planet.GetUniversalVelocityAtLocalPoint(GameMain.gameTime, player.position);
                double climb = VectorLF3.Dot(player.uVelocity - ground, up);
                if (climb < SpaceEntryClimb) player.uVelocity += up * (SpaceEntryClimb - climb);
            }
        }

        // Energy: charge only the time that passed. Flight in space (Sail) is left alone: there the mecha moves a fixed
        // distance per tick, so its speed and cost already rise together.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMove_Walk), nameof(PlayerMove_Walk.UseWalkEnergy))]
        [HarmonyPatch(typeof(PlayerMove_Fly), nameof(PlayerMove_Fly.UseFlyEnergy))]
        [HarmonyPatch(typeof(PlayerMove_Drift), nameof(PlayerMove_Drift.UseDriftEnergy))]
        static void UseMoveEnergy_Prefix(ref double energy)
        {
            if (IsActive(out float ratio)) energy *= ratio;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMove_Fly), nameof(PlayerMove_Fly.UseThrustEnergy))]
        [HarmonyPatch(typeof(PlayerMove_Drift), nameof(PlayerMove_Drift.UseThrustEnergy))]
        static void UseThrustEnergy_Prefix(ref double dt)
        {
            if (IsActive(out float ratio)) dt *= ratio;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.DeterminePreviews))]
        static IEnumerable<CodeInstruction> DeterminePreviews_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Replace the free-rotation step of Shift + rotate (rotate and counter-rotate) with GetRotateStep()
            int count = 0;
            var codeMacher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldc_R4 && i.operand is float f && Mathf.Abs(f - RotateStep) < 1e-5f))
                .Repeat(matcher =>
                {
                    matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Mecha_Patch), nameof(GetRotateStep)));
                    count++;
                });
            if (count != 2) Log.Warn($"Mecha_Patch: found {count} rotate steps in BuildTool_Click.DeterminePreviews (expected 2)");
            return codeMacher.InstructionEnumeration();
        }

        static float GetRotateStep()
        {
            return IsActive(out float ratio) ? RotateStep * ratio : RotateStep;
        }
    }
}
