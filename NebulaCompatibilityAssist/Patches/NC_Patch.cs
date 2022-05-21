using NebulaAPI;
using HarmonyLib;
using System.Reflection;
using System;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NC_Patch
    {
        public static Action OnLogin;

        public static void OnAwake()
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            Plugin.Harmony.PatchAll(typeof(NC_Patch));
#if DEBUG
            Init();
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            Harmony harmony = Plugin.Harmony;
            LSTM.Init(harmony);
            DSPMarker.Init(harmony);
            DSPStarMapMemo.Init(harmony);
            DSPBeltReverseDirection.Init(harmony);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                Log.Debug("OnLogin");
                OnLogin?.Invoke();
            }
        }

    }
}
