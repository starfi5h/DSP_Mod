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
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        static Harmony harmony;

        public static bool EnableReplaceStation = true;
        public static bool EnableUIBlueprintOpt = true;
        public static bool EnableRemoveGC = true;
        public static bool EnableGhost = true;

        public void Start() // Wait until all mods are awake
        {
            Log = Logger;
            harmony = new(GUID);
            
            
            if (EnableReplaceStation)
                harmony.PatchAll(typeof(ReplaceStationLogic));

            if (EnableUIBlueprintOpt)
                harmony.PatchAll(typeof(UIBlueprint_Patch));

            if (EnableGhost)
            {
                harmony.PatchAll(typeof(BuildTool_Patch));
                harmony.Patch(AccessTools.Method(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CreatePrebuilds)), null, null,
                    new HarmonyMethod(AccessTools.Method(typeof(RemoveGC_Patch), nameof(RemoveGC_Patch.RemoveGC_Transpiler))));
            }
            else
            {
                if (EnableRemoveGC)
                    harmony.PatchAll(typeof(RemoveGC_Patch));
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
