#if !DEBUG

using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using HarmonyLib;
using UnityEngine;

namespace BuildToolOpt
{
    [BepInPlugin(GUID, NAME, VERSION)]    
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInIncompatibility("org.kremnev8.plugin.BlueprintTweaks")]
    [CommonAPISubmoduleDependency(nameof(PickerExtensionsSystem), nameof(CustomKeyBindSystem), nameof(LocalizationModule))]
    public class PluginCAPIcompat : BaseUnityPlugin
    {
        // This compatible plugin only load when CommonAPI is present, and BlueprintTweaks is disabled
        public const string GUID = "starfi5h.plugin.BuildToolOpt.CAPIcompat";
        public const string NAME = "BuildToolOptCAPIcompat";
        public const string VERSION = "1.0.0";

        public void Awake() // RegisterTranslation要在Awake時註冊
        {
            var harmony = new Harmony(GUID);

            harmony.PatchAll(typeof(CameraFix_Patch));

            harmony.PatchAll(typeof(UIBlueprintComponentItem_Patch));
            CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.J, 0, ECombineKeyAction.OnceClick, false),
                conflictGroup = 3071,
                name = "ToggleBPViewModeBTO",
                canOverride = true
            });
            LocalizationModule.RegisterTranslation("KEYToggleBPViewModeBTO", "(BuildToolOpt) Toggle Blueprint View Mode", "(BuildToolOpt) 切换蓝图模式视角", "");
        }

        public void Update()
        {
            if (VFInput.inputing || GameMain.mainPlayer == null) return;

            if (GameMain.mainPlayer.controller.actionBuild.blueprintMode != EBlueprintMode.None) //在藍圖模式下
            {
                if (CustomKeyBindSystem.GetKeyBind("ToggleBPViewModeBTO").keyValue) //熱鍵切換視角
                {
                    CameraFix_Patch.Enable = !CameraFix_Patch.Enable;
                    Logger.LogDebug("Switch Blueprint Mode:" + CameraFix_Patch.Enable);
                }
            }
        }
    }
}
#endif