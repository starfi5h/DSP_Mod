using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


namespace DysonOrbitModifier
{
    [BepInPlugin("com.starfi5h.plugin.DysonOrbitModifier", "DysonOrbitModifier", "1.3.0")]
    public class DysonOrbitModifier : BaseUnityPlugin
    {
        private Harmony harmony;
        private ConfigEntry<bool>[] configBool;
        private ConfigEntry<float>[] configFloat;
        internal void Awake()
        {
            harmony = new Harmony("com.starfi5h.plugin.DysonOrbitModifier");

            DysonOrbitUI.logger = Logger;
            SphereLogic.logger = Logger;



            configBool = new ConfigEntry<bool>[] {
                Config.Bind<bool>("option", "syncPosition", true, "Move objects to the same position when the layer orientation is changed. \n当旋轉方向改变时，将壳上的物体移至同样座标的位置。"),
                Config.Bind<bool>("option", "syncAltitude", true, "Move objects to the same altitude when the layer radius is changed. \n当半径改变时，将壳上的物体移至同样高度的位置。"),
                Config.Bind<bool>("option", "correctOnChange", true, "Remove exceeding Structure Point/Cell Point right after entities are moved. \n移动物体后，立即移除超出的结构点数/细胞点数。"),
                Config.Bind<bool>("option", "hierarchicalRotation", false, "(experimental feature) Let outer layer rotation affects inside layers.\n(实验性功能)让外层壳旋转传递至内层。")
            };
            configFloat = new ConfigEntry<float>[]{
                Config.Bind<float>("modify panel setting", "minRadiusMultiplier", 1.0f, "Multiplier of minimum radius \n最小軌道半徑的倍率"),
                Config.Bind<float>("modify panel setting", "maxRadiusMultiplier", 1.0f, "Multiplier of maximum radius \n最大轨道半径的倍率"),
                Config.Bind<float>("modify panel setting", "maxAngularSpeed", 10.0f, "Maximum rotation speed \n最大旋轉速度")
            };
            SphereLogic.syncPosition = configBool[0].Value;
            SphereLogic.syncAltitude = configBool[1].Value;
            SphereLogic.correctOnChange = configBool[2].Value;
            DysonOrbitUI.minOrbitRadiusMultiplier = configFloat[0].Value;
            DysonOrbitUI.maxOrbitRadiusMultiplier = configFloat[1].Value;
            DysonOrbitUI.maxOrbitAngularSpeed = configFloat[2].Value;


            harmony.PatchAll(typeof(DysonOrbitUI));
            if (configBool[3].Value)
                harmony.PatchAll(typeof(HierarchicalRotation));
            ModTranslate.Init();            
        }

        internal void OnDestroy()
        {
            configBool = null;
            configFloat = null;
            DysonOrbitUI.Free();
            harmony.UnpatchSelf();  
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
            TranslateDict.Add("Add Orbit", "新增轨道");
            TranslateDict.Add("Add Layer", "新增层级");
            TranslateDict.Add("Modify Orbit", "修改轨道");
            TranslateDict.Add("Modify Layer", "修改层级");
            TranslateDict.Add("Rotation speed", "旋轉速度");
        }
    }
}