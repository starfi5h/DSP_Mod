using DSPCalculator.Logic;
using DSPCalculator.UI;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace BuildToolOpt
{
    public class Compatibility
    {
        public static void Init(Harmony harmony)
        {
            Nebula_Patch.Init();
            CheatEnabler_Patch.Init(harmony);
            DSPCalculator_Patch.Init(harmony); // Can't test it in debug mode due to type loading
        }

        public static class Nebula_Patch
        {
            public const string GUID = "dsp.nebula-multiplayer";

            public static void Init()
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _)) return;

                Plugin.EnableReplaceStation = false;
                Plugin.EnableHologram = false;
                Plugin.EnableStationBuildOptimize = false;
                Plugin.Log.LogDebug("Nebula: Disable replace station and hologram function");
            }
        }

        public static class CheatEnabler_Patch
        {
            public const string GUID = "org.soardev.cheatenabler";
            
            public static void Init(Harmony harmony)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo)) return;

                try
                {
                    Assembly assembly = pluginInfo.Instance.GetType().Assembly;
                    var classType = assembly.GetType("CheatEnabler.Patches.FactoryPatch");
                    harmony.Patch(AccessTools.Method(classType, "ArrivePlanet"),
                        new HarmonyMethod(AccessTools.Method(typeof(CheatEnabler_Patch), nameof(ArrivePlanet_Prefix))));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("CheatEnabler compatibility failed! Last working version: 2.4.0");
                    Plugin.Log.LogWarning(e);
                }
            }

            // https://github.com/soarqin/DSP_Mods/blob/master/CheatEnabler/Patches/FactoryPatch.cs
            internal static bool ArrivePlanet_Prefix()
            {
                return !ReplaceStationLogic.IsReplacing;
            }
        }

        public static class DSPCalculator_Patch
        {
            public const string GUID = "com.GniMaerd.DSPCalculator";

            public static void Init(Harmony harmony)
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;

                try
                {
                    harmony.PatchAll(typeof(Warpper));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("DSPCalculator compatibility failed! Last working version: 0.5.19");
                    Plugin.Log.LogWarning(e);
                }
            }
            private class Warpper
            {
#if !DEBUG
                [HarmonyPostfix]
                [HarmonyPatch(typeof(UIItemNode), MethodType.Constructor, new Type[] { typeof(ItemNode), typeof(UICalcWindow) })]
                public static void UIItemNode_Postfix(UIItemNode __instance)
                {
                    if (__instance.assemblerIconObj == null) return;
                    __instance.assemblerIconObj.GetComponent<Button>().onClick.AddListener(() => { OnAssemblerIconClick(__instance); });
                }

                public static void OnAssemblerIconClick(UIItemNode @this)
                {
                    if (!VFInput.shift || !VFInput.readyToBuild) return;
                                        
                    var recipeInfo = @this.itemNode.mainRecipe;
                    if (recipeInfo == null) return;
                    int protoId = recipeInfo.assemblerItemId;
                    int recipeId = recipeInfo.recipeNorm.oriProto.ID;
                    bool forceAccMode = !(recipeInfo.isInc && recipeInfo.canInc);

                    Plugin.Log.LogDebug($"SetHandBuilding protoId={protoId} recipeId={recipeId} forceAcc={forceAccMode}");
                    SetHandBuilding(protoId, recipeId, forceAccMode);
                }

                public static void SetHandBuilding(int protoId, int recipeId, bool forceAccMode)
                {
                    var player = GameMain.mainPlayer;
                    if (player == null || protoId <= 0) return;

                    // VFInput._copyBuilding
                    if (player.inhandItemId > 0) player.SetHandItems(0, 0, 0);
                    player.SetHandItems(protoId, 0, 0);
                    player.controller.cmd.type = ECommand.Build;
                    VFInput.UseMouseLeft();

                    //Set the required building parameters in template
                    //ref: BuildingParameters.template.CopyFromFactoryObject(num, factory, true, false);
                    BuildingParameters.template.SetEmpty();
                    BuildingParameters.template.type = BuildingType.Other;
                    BuildingParameters.template.itemId = protoId;
                    BuildingParameters.template.modelIndex = LDB.items.Select(protoId).ModelIndex;

                    if (recipeId > 0)
                    {
                        var recipeType = LDB.recipes.Select(recipeId).Type;
                        switch (recipeType)
                        {
                            case ERecipeType.Research:
                                BuildingParameters.template.type = BuildingType.Lab;
                                BuildingParameters.template.recipeId = recipeId;
                                BuildingParameters.template.recipeType = recipeType;
                                BuildingParameters.template.mode0 = 1;
                                BuildingParameters.template.mode1 = forceAccMode ? 1 : 0;
                                UIRealtimeTip.Popup(LDB.recipes.Select(recipeId).name + " " + (forceAccMode ? "加速生产".Translate() : "额外产出".Translate()), false);
                                break;

                            default:
                                // Given that GenesisBook add more ERecipeType, we will assume all the other recipe type are belong to Assembler building type
                                BuildingParameters.template.type = BuildingType.Assembler;
                                BuildingParameters.template.recipeId = recipeId;
                                BuildingParameters.template.recipeType = recipeType;
                                BuildingParameters.template.parameters = new int[1];
                                BuildingParameters.template.parameters[0] = forceAccMode ? 1 : 0;
                                UIRealtimeTip.Popup(LDB.recipes.Select(recipeId).name + " " + (forceAccMode ? "加速生产".Translate() : "额外产出".Translate()), false);
                                break;
                        }
                    }
                    player.controller.actionBuild.NotifyTemplateChange();
                }
#endif
            }

        }
    }
}
