using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NC_Patch
    {
        public static Action OnLogin;
        public static string RequriedPlugins = ""; // plugins required to install on both end
        public static string ErrorMessage = "";
        public static bool initialized = false;

        public static void OnAwake()
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            Plugin.Instance.Harmony.PatchAll(typeof(NC_Patch));
#if DEBUG
            Init();
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            if (initialized) return;

            Harmony harmony = Plugin.Instance.Harmony;
            LSTM.Init(harmony);
            DSPMarker.Init(harmony);
            DSPStarMapMemo.Init(harmony);
            DSPBeltReverseDirection.Init(harmony);
            DSPTransportStat_Patch.Init(harmony);
            PlanetFinder.Init(harmony);
            MoreMegaStructure.Init(harmony);
            DSPFreeMechaCustom.Init(harmony);
            AutoStationConfig.Init(harmony);
            Auxilaryfunction.Init(harmony);
            DSPOptimizations.Init(harmony);
            NebulaHotfix.Init(harmony);

            if (ErrorMessage != "")
            {
                ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                UIMessageBox.Show("Nebula Compatibility Assist Error", ErrorMessage, "确定".Translate(), 3);
            }
            initialized = true;
            Plugin.Instance.Version = PluginInfo.PLUGIN_VERSION + RequriedPlugins;
            Log.Debug($"Version: {Plugin.Instance.Version}");
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
