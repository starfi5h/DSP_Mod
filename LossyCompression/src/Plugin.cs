﻿using BepInEx;
using BepInEx.Logging;
using crecheng.DSPModSave;
using HarmonyLib;
using System;
using System.IO;

namespace LossyCompression
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [BepInDependency(ModCompatibility.DSPOptimizations.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModCompatibility.NebulaAPI.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin, IModCanSave
    {
        public const string GUID = "starfi5h.plugin.LossyCompression";
        public const string NAME = "LossyCompression";
        public const string VERSION = "0.2.1";
        public const int FORMAT_VERSION = 1;

        public static Plugin Instance { get; private set; }
        public static bool Enable { get; set; } = true;
        static int enableFlags = 0;

        Harmony harmony;

        public void Awake()
        {
            Instance = this;
            Log.LogSource = Logger;

            var LazyLoad = Config.Bind<bool>("Advance", "LazyLoad", false, "Delay generation of shell model until viewing\n延迟载入戴森壳的模型");
            var CargoPath = Config.Bind<bool>("Dependent", "CargoPath", false, "Lossy compress for belts & cargo\n有损压缩传送带数据（将更改原档）");
            var DysonShell = Config.Bind<bool>("Independent", "DysonShell", true, "Lossless compress for dyson shells\n无损压缩戴森壳面");
            var DysonSwarm = Config.Bind<bool>("Independent", "DysonSwarm", true, "Lossy compress for dyson swarm\n有损压缩太阳帆");

            enableFlags += CargoPath.Value ? 1 : 0;
            enableFlags += DysonShell.Value ? 2 : 0;
            enableFlags += DysonSwarm.Value ? 4 : 0;
            LazyLoading.Enable = LazyLoad.Value;
            SetEnables(enableFlags);
            DysonShellCompress.IsMultithread = true;
            DysonSwarmCompress.IsMultithread = true;

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(CargoPathCompress));
            harmony.PatchAll(typeof(DysonShellCompress));
            harmony.PatchAll(typeof(DysonSwarmCompress));
            try
            {
                harmony.PatchAll(typeof(LazyLoading));
            }
            catch(Exception e)
            {
                Log.Error("Lazy load patching fail! The function is now disabled");
                Log.Error(e);
                LazyLoading.Enable = false;
            }
            harmony.PatchAll(typeof(UIcontrol));

            ModCompatibility.DSPOptimizations.Init(harmony);
            ModCompatibility.NebulaAPI.Init(harmony);

#if DEBUG
            harmony.PatchAll(typeof(DebugPatch));
            UIcontrol.Init();
#endif
        }

        public void OnDestroy()
        {
            UIcontrol.OnDestory();
            harmony.UnpatchSelf();
        }

        public static int GetEnables()
        {
            int mask = 0;
            if (CargoPathCompress.Enable)
                mask |= 1;
            if (DysonShellCompress.Enable)
                mask |= 2;
            if (DysonSwarmCompress.Enable)
                mask |= 4;
            return mask;
        }

        public static void SetEnables(int mask)
        {
            CargoPathCompress.Enable = (mask & 1) != 0;
            DysonShellCompress.Enable = (mask & 2) != 0;
            DysonSwarmCompress.Enable = (mask & 4) != 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameData), nameof(GameData.Export))]
        public static void BeforeGameExport()
        {
            if (!Enable)
            {
                enableFlags = GetEnables();
                SetEnables(0);
                Log.Info("Normal compression");
            }
        }

        public void Export(BinaryWriter w)
        {
            if (GameMain.instance.isMenuDemo) return;
            if (Enable)
            {
                string text = "Format version:" + FORMAT_VERSION + " Compress version:";
                if (CargoPathCompress.Enable) text += " CargoPath(v" + (CargoPathCompress.EncodedVersion - 60) + ")";
                if (DysonShellCompress.Enable) text += " DysonShell(v" + DysonShellCompress.EncodedVersion + ")";
                if (DysonSwarmCompress.Enable) text += " DysonSwarm(v" + DysonSwarmCompress.EncodedVersion + ")";
                Log.Warn(text);

                w.Write(FORMAT_VERSION);
                DysonShellCompress.Export(w);
                DysonSwarmCompress.Export(w);
            }
            else
            {
                w.Write(0);
                SetEnables(enableFlags);
            }
        }

        public void Import(BinaryReader r)
        {
            LazyLoading.Reset();
            if (GameMain.instance.isMenuDemo) return;

            int format_version = r.ReadInt32();
            if (format_version == 1)
            {
                Log.Info($"Import format version: " + format_version);
                DysonShellCompress.Import(r);
                DysonSwarmCompress.Import(r);
                UIRoot.instance.uiGame.statWindow.performancePanelUI.RefreshDataStatTexts();

                if (ModCompatibility.AfeterImport != null)
                {
                    Log.Info("Processing compatibility process...");
                    ModCompatibility.AfeterImport.Invoke();
                }
            }
        }

        public void IntoOtherSave()
        {
            LazyLoading.Reset();
            if (GameMain.instance.isMenuDemo) return;
        }
    }

    public static class Log
    {
        public static ManualLogSource LogSource;
        public static void Error(object obj) => LogSource.LogError(obj);
        public static void Warn(object obj) => LogSource.LogWarning(obj);
        public static void Info(object obj) => LogSource.LogInfo(obj);
        public static void Debug(object obj) => LogSource.LogDebug(obj);
    }
}