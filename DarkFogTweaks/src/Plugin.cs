using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(DarkFogTweaks.Plugin.NAME)]
[assembly: AssemblyVersion(DarkFogTweaks.Plugin.VERSION)]

namespace DarkFogTweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.DarkFogTweaks";
        public const string NAME = "DarkFogTweaks";
        public const string VERSION = "0.0.1";

        public static ManualLogSource Log;
        public static Plugin Instance;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            Instance = this;
            harmony = new Harmony(GUID);

            Patch_Behavior.LoadConfigs(Config);
            Patch_Building.LoadConfigs(Config);
            EnemyUnitScale.LoadConfigs(Config);
            harmony.PatchAll(typeof(Patch_Behavior));
            harmony.PatchAll(typeof(Patch_Building));
            harmony.PatchAll(typeof(Patch_Common));

#if DEBUG
            Patch_Common.OnApplyClick();
#endif
        }

#if DEBUG

        public static void PrintAll()
        {
            foreach (var modelProto in LDB.models.dataArray)
            {
                switch (modelProto.ObjectType)
                {
                    case EObjectType.Enemy:
                        if (modelProto.prefabDesc.isEnemyUnit)
                            EnemyUnitScale.Print(modelProto);
                        break;
                }
            }
        }

        public void OnDestroy()
        {
            Patch_Common.RestoreArray();
            EnemyUnitScale.RestoreAll();

            harmony.UnpatchSelf();
            harmony = null;
        }
#endif
    }
}
