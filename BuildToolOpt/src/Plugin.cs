using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(BuildToolOpt.Plugin.GUID)]
[assembly: AssemblyProduct(BuildToolOpt.Plugin.NAME)]
[assembly: AssemblyVersion(BuildToolOpt.Plugin.VERSION)]

namespace BuildToolOpt
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.BuildToolOpt";
        public const string NAME = "BuildToolOpt";
        public const string VERSION = "1.0.7";

        public static ManualLogSource Log;
        static Harmony harmony;

        public static bool EnableRemoveGC = true;
        public static bool EnableGCwhenPause = false;
        public static bool EnableReplaceStation = true;
        public static bool EnableHologram = false;
        public static bool EnableUIBlueprintOpt = true;
        public static bool EnableClipboardPaste = true;

        public void Start() // Wait until all mods are awake
        {
            Log = Logger;
            harmony = new(GUID);

            EnableRemoveGC = Config.Bind("BuildTool", "RemoveGC", true, "Remove c# garbage collection of build tools to reduce lag\n移除建筑工具的强制内存回收以减少铺设时卡顿").Value;
            EnableGCwhenPause = Config.Bind("BuildTool", "GC when pause", false, "Trigger garbage collection when game pause (esc menu). Enable this option if memory usage grow rapidly.\n在游戏暂停时(Esc)回收内存。当内存占用快速增长时可启用此选项手动释放").Value;
            EnableReplaceStation = Config.Bind("BuildTool", "ReplaceStation", true, "Directly replace old station with new one in hand\n可直接替换物流塔").Value;
            EnableHologram = Config.Bind("BuildTool", "EnableHologram", false, "Place white holograms when lacking of item\n即使物品不足也可以放置建筑虚影").Value;
            EnableUIBlueprintOpt = Config.Bind("UI", "UIBlueprintOpt", true, "Optimize blueprint UI to reduce lag time\n优化蓝图UI减少卡顿").Value;
            EnableClipboardPaste = Config.Bind("UI", "ClipboardPaste", true, "Directly parse blueprint data from clipboard when Ctrl + V\n热键粘贴蓝图时,直接读取剪切板").Value;
            Compatibility.Init(harmony);

            if (EnableReplaceStation)
                harmony.PatchAll(typeof(ReplaceStationLogic));

            if (EnableUIBlueprintOpt)
                harmony.PatchAll(typeof(UIBlueprint_Patch));

            if (EnableHologram)
            {
                harmony.PatchAll(typeof(BuildTool_Patch));
                harmony.PatchAll(typeof(BuildTool_Inserter_Patch));
                if (EnableRemoveGC)
                {
                    harmony.Patch(AccessTools.Method(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds)), null, null,
                    new HarmonyMethod(AccessTools.Method(typeof(RemoveGC_Patch), nameof(RemoveGC_Patch.RemoveGC_Transpiler))));
                    harmony.Patch(AccessTools.Method(typeof(DSPGame), nameof(DSPGame.EndGame)), null,
                    new HarmonyMethod(AccessTools.Method(typeof(RemoveGC_Patch), nameof(RemoveGC_Patch.GCCollect))));
                }
            }
            else
            {
                if (EnableRemoveGC)
                    harmony.PatchAll(typeof(RemoveGC_Patch));
            }

            if (EnableGCwhenPause)
            {
                harmony.Patch(AccessTools.Method(typeof(GameMain), nameof(GameMain.Pause)), null,
                new HarmonyMethod(AccessTools.Method(typeof(RemoveGC_Patch), nameof(RemoveGC_Patch.GCCollect))));
            }

            if (EnableClipboardPaste)
            {
                harmony.PatchAll(typeof(PlayerController_Patch));
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
