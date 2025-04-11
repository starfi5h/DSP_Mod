using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(MechaAnimMod.Plugin.NAME)]
[assembly: AssemblyProduct(MechaAnimMod.Plugin.NAME)]
[assembly: AssemblyVersion(MechaAnimMod.Plugin.VERSION)]

namespace MechaAnimMod
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.MechaAnimMod";
        public const string NAME = "MechaAnimMod";
        public const string VERSION = "1.1.0";

        public static ManualLogSource Log;
        static Harmony harmony;
        static int idleAnimIndex = 0;
        static int lastIdleAnimSecond;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Start))]
        static void CopyRtsPoserParameters(GameCamera __instance)
        {
            var buildPoser = __instance.buildPoser;
            var rtsPoser = __instance.rtsPoser;

            buildPoser.distMin = rtsPoser.distMin; // 3.7 => 1
            buildPoser.distMax = rtsPoser.distMax; // 120
            buildPoser.normalFov = rtsPoser.normalFov; // 38 => 36
            buildPoser.damp = rtsPoser.damp; // 0.15
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.DetermineIdleAnims))]
        static void DetermineIdleAnims(PlayerAnimator __instance)
        {
            if (__instance.idleSeconds - lastIdleAnimSecond <= 6)
            {
                // overwrite the playing idle animation with our own
                __instance.idleAnimIndex = idleAnimIndex;
            }
            if (VFInput.control && !VFInput.inFullscreenGUI)
            {
                for (int i = 0; i < __instance.idles.Length; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Keypad0 + i))
                    {
                        if (__instance.idleTime > 1f)
                        {
                            __instance.idleTime = 0.999f;
                        }
                        if (i == idleAnimIndex) // Clean up exiting state to restart
                        {
                            for (int j = 0; j < __instance.idles.Length; j++)
                            {
                                __instance.idles[j].weight = 0f;
                                __instance.idles[j].normalizedTime = 0f;
                            }
                        }
                        idleAnimIndex = i;
                        lastIdleAnimSecond = __instance.idleSeconds;
                        __instance.idleAnimIndex = idleAnimIndex;
                        __instance.lastIdleAnimSecond = lastIdleAnimSecond;
                        return;
                    }
                }
            }
        }
    }
}
