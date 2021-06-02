using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DysonOrbitModifier
{
    [BepInPlugin("com.starfi5h.plugin.DysonOrbitModifier", "DysonOrbitModifier", "1.0.0")]
    public class DysonOrbitModifier : BaseUnityPlugin
    {
        private Harmony harmony;
        private ConfigEntry<bool>[] configBool;
        private ConfigEntry<float>[] configFloat;
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("DysonOrbitModifier");
        internal void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.DysonOrbitModifier");
            try
            {

                harmony.PatchAll(typeof(DysonOrbitUI));
                ModTranslate.Init();
                configBool = new ConfigEntry<bool>[] {
                    Config.Bind<bool>("option", "EditNonemptySphere", false, "Allow to modify the layer with nodes/允許修改非空層軌道")};
                configFloat = new ConfigEntry<float>[]{
                     Config.Bind<float>("parameter", "minRadiusMultiplier", 1.0f, "最小軌道半徑的倍率"),
                     Config.Bind<float>("parameter", "maxRadiusMultiplier", 1.0f, "最大轨道半径的倍率"),
                     Config.Bind<float>("parameter", "maxAngularSpeed", 10.0f, "最大角速度")
                };
                DysonOrbitUI.EditNonemptySphere = configBool[0].Value;
                DysonOrbitUI.minOrbitRadiusMultiplier = configFloat[0].Value;
                DysonOrbitUI.maxOrbitRadiusMultiplier = configFloat[1].Value;
                DysonOrbitUI.maxOrbitAngularSpeed = configFloat[2].Value;
                logger.LogDebug($"EditNonemptySphere:{configBool[0].Value}, minRadiusX:{configFloat[0].Value}, maxRadiuX:{configFloat[1].Value}, maxAngularSpeed:{configFloat[2].Value}");

            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
        }

        internal void OnDestroy()
        {
            configBool = null;
            configFloat = null;
            DysonOrbitUI.Free();
            harmony.UnpatchSelf();  // For ScriptEngine hot-reloading
        }
    }
    public static class ModTranslate
    {
        public static Dictionary<string, string> TranslateDict = new Dictionary<string, string>();

        //擴充方法
        public static string Translate(this string s)
        {
            return Localization.language == Language.zhCN && ModTranslate.TranslateDict.ContainsKey(s) ? ModTranslate.TranslateDict[s] : s;
        }

        public static void Init()
        {
            TranslateDict.Clear();
            TranslateDict.Add("Create", "创造");
            TranslateDict.Add("Modify", "修改");
            TranslateDict.Add("Nonempty", "非空层");
            TranslateDict.Add("Add Orbit", "新增轨道");
            TranslateDict.Add("Add Layer", "新增层级");
            TranslateDict.Add("Modify Orbit", "修改轨道");
            TranslateDict.Add("Modify Layer", "修改层级");
            TranslateDict.Add("Orbit angular speed", "角速度");
        }
    }
}