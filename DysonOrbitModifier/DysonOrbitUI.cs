﻿// Dyson Sphere Program is developed by Youthcat Studio and published by Gamera Game.

using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.Events;
using BepInEx.Logging;


namespace DysonOrbitModifier
{
    class DysonOrbitUI : BaseUnityPlugin
    {
        public static float minOrbitRadiusMultiplier;
        public static float maxOrbitRadiusMultiplier;
        public static float maxOrbitAngularSpeed;

        public static bool modOrbitMode;
        public static bool modLayerMode;

        public static Slider modSlider0;
        public static InputField modInput0;
        public static Text modText0;
        public static bool sliderEventLock;
        public static UIDysonPanel that;

        public static ManualLogSource logger;

        public static void Free()
        {
            if (modSlider0)
                Destroy(modSlider0.gameObject);
            if (modInput0)
                Destroy(modInput0.gameObject);
            if (modText0)
                Destroy(modText0.gameObject);
            that = null;
        }



        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnOpen")]
        public static void UIDysonPanel__OnOpen(UIDysonPanel __instance)
        {
            modOrbitMode = modLayerMode = false;
            try
            {
                that = __instance;
                GameObject obj = null;
                string dir = "UI Root/Always on Top/Overlay Canvas - Top/Dyson Editor Top/info-group/screen-group/add-panel/";

                if (!modSlider0)
                {
                    CreateObject(ref obj, dir + "bar-slider-1", "bar-slider-3", new Vector3(-120f, 59f, 0f));
                    modSlider0 = obj.GetComponent<Slider>();
                    modSlider0.maxValue = maxOrbitAngularSpeed;
                    modSlider0.minValue = 0.0f;
                    modSlider0.onValueChanged.AddListener(new UnityAction<float>(delegate { OnModSlider0Change(); }));
                }
                if (!modInput0)
                {
                    CreateObject(ref obj, dir + "bar-value-1", "bar-value-3", new Vector3(230f, 59f, 0f));
                    modInput0 = obj.GetComponent<InputField>();
                    modInput0.onEndEdit.AddListener(new UnityAction<string>(delegate { OnModInput0ValueEnd(); }));
                }
                if (!modText0)
                {
                    CreateObject(ref obj, dir + "bar-label", "bar-label-3", new Vector3(-230f, 74f, 0f), "Rotation speed".Translate());
                    modText0 = obj.GetComponent<Text>();
                }

            }
            catch (Exception e)
            {
                logger.LogError("UI Component load error");
                logger.LogError(e.ToString());
            }            
        }

        // credit to https://github.com/fezhub/DSP-Mods/blob/main/DSP_SphereProgress/SphereProgress.cs

        public static void CreateObject(ref GameObject obj, string path, string name, Vector3 lPos, String text = "")
        {

            GameObject target = GameObject.Find(path);
            if (target == null)
            {
                logger.LogDebug(path + " : Object not found");
                return;
            }
            obj = Instantiate(target, target.transform.parent);
            obj.transform.localPosition = lPos;
            obj.name = name;
            if (text != "")
            {
                obj.GetComponentInChildren<Text>().text = text.Translate();
                obj.GetComponentInChildren<Localizer>().stringKey = text.Translate();
            }
        }

        public static void OnModSlider0Change()
        {
            if (sliderEventLock)
                return;
            sliderEventLock = true;
            float val = modSlider0.value;
            val = Mathf.Round(val / 0.001f) * 0.001f;
            modSlider0.value = val;
            modInput0.text = val.ToString("0.000##");
            sliderEventLock = false;
        }

        public static void OnModInput0ValueEnd()
        {
            sliderEventLock = true;
            string val = modInput0.text;
            if (val == "-1") //Reset angular speed to original value using current radius
                modSlider0.value = Mathf.Sqrt(that.viewDysonSphere.gravity / that.addSlider0.value) / that.addSlider0.value * 57.29578f;
            else if (float.TryParse(val, out float result))
                modSlider0.value = Mathf.Clamp(result, modSlider0.minValue, modSlider0.maxValue);
            modInput0.text = modSlider0.value.ToString();
            sliderEventLock = false;
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "OnAddSlider0Change")]
        public static void UIDysonPanel_OnAddSlider0Change(UIDysonPanel __instance)
        {
            sliderEventLock = true;
            if (modSlider0 != null)
            {
                modSlider0.value = Mathf.Sqrt(__instance.viewDysonSphere.gravity / __instance.addSlider0.value) / __instance.addSlider0.value * 57.29578f; //change angular speed along with radius
                modInput0.text = modSlider0.value.ToString();
            }
            sliderEventLock = false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnAddOkClick")]
        public static void UIDysonPanel_OnAddOkClick(UIDysonPanel __instance)
        {
            try
            {
                float radius = __instance.addSlider0.value;
                float inclination = __instance.addSlider1.value;
                float longitude = __instance.addSlider2.value;
                Quaternion rotation = Quaternion.Euler(0.0f, -longitude, -inclination);
                float angularSpeed = modSlider0.value;

                if (modOrbitMode)
                {
                    modOrbitMode = false;
                    SphereLogic.ChangeSwarm(__instance.viewDysonSphere, __instance.orbitSelected, radius, rotation);
                }
                else if (modLayerMode)
                {
                    modLayerMode = false;
                    SphereLogic.ChangeLayer(__instance.viewDysonSphere, __instance.layerSelected, radius, rotation, angularSpeed);
                }
                modSlider0.value = 0;
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
            __instance.UpdateSelectionVisibleChange();
        }



        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnSwarmOrbitAddClick")]
        public static bool UIDysonPanel_OnSwarmOrbitAddClick(UIDysonPanel __instance)
        {

            if (__instance.orbitSelected == 0)
                return true;

            __instance.brushMode = UIDysonPanel.EBrushMode.None;
            //__instance.orbitSelected stay the same
            __instance.layerSelected = 0;
            __instance.nodeSelected = 0;
            __instance.frameSelected = 0;
            __instance.shellSelected = 0;
            modOrbitMode = !modOrbitMode;
            modLayerMode = false;
            __instance.UpdateSelectionVisibleChange();

            SailOrbit orbit = __instance.viewDysonSphere.swarm.orbits[__instance.orbitSelected];
            SphereLogic.ConvertQuaternion(orbit.rotation, out float inclination, out float longitude);
            __instance.addSlider0.value = orbit.radius;            
            __instance.addSlider1.value = inclination;
            __instance.addSlider2.value = longitude;

            __instance.addInput0.text = __instance.addSlider0.value.ToString("0");
            __instance.addInput1.text = __instance.addSlider1.value.ToString("0.0##");
            __instance.addInput2.text = __instance.addSlider2.value.ToString("0.0##");

            return false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnShellLayerAddClick")]
        public static bool UIDysonPanel_OnShellLayerAddClick(UIDysonPanel __instance)
        {

            if (__instance.layerSelected == 0)
                return true;

            __instance.brushMode = UIDysonPanel.EBrushMode.None;
            __instance.orbitSelected = 0;
            //__instance.layerSelected stay the same
            __instance.nodeSelected = 0;
            __instance.frameSelected = 0;
            __instance.shellSelected = 0;
            modLayerMode = !modLayerMode;
            modOrbitMode = false;
            __instance.UpdateSelectionVisibleChange();

            DysonSphereLayer layer = __instance.viewDysonSphere.GetLayer(__instance.layerSelected);
            SphereLogic.ConvertQuaternion(layer.orbitRotation, out float inclination, out float longitude);
            __instance.addSlider0.value = layer.orbitRadius;
            __instance.addSlider1.value = inclination;
            __instance.addSlider2.value = longitude;
            sliderEventLock = true; //preserve original angularSpeed
            modSlider0.value = layer.orbitAngularSpeed;
            sliderEventLock = false;

            __instance.addInput0.text = __instance.addSlider0.value.ToString("0");
            __instance.addInput1.text = __instance.addSlider1.value.ToString("0.0##");
            __instance.addInput2.text = __instance.addSlider2.value.ToString("0.0##");
            modInput0.text = modSlider0.value.ToString("0.000##");

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "_OnLateUpdate")]
        public static void UIDysonPanel__OnLateUpdate(UIDysonPanel __instance)
        {
            if (__instance.viewDysonSphere != null)
            {
                int status = 0;
                if (modOrbitMode)
                {
                    status = SphereLogic.CheckSwarmRadius(__instance.viewDysonSphere, __instance.addSlider0.value);
                    __instance.addSlider0Fg.color = status == 0 ? __instance.normalColor3 : __instance.redColor3;
                    Quaternion quaternion = Quaternion.Euler(0.0f, -__instance.addSlider2.value, -__instance.addSlider1.value);
                    __instance.orbitPreview.state = status == 0 ? 0 : 1;
                    __instance.orbitPreview.realRadius = (double)__instance.addSlider0.value;
                    __instance.orbitPreview.rotation = quaternion;
                }
                else if (modLayerMode)
                {
                    status = SphereLogic.CheckLayerRadius(__instance.viewDysonSphere, __instance.layerSelected, __instance.addSlider0.value);
                    __instance.addSlider0Fg.color = status == 0 ? __instance.normalColor3 : __instance.redColor3;
                    __instance.layerPreview.state = status == 0 ? 0 : 1;
                    Quaternion quaternion = Quaternion.Euler(0.0f, -__instance.addSlider2.value, -__instance.addSlider1.value);
                    __instance.layerPreview.realRadius = (double)__instance.addSlider0.value;
                    __instance.layerPreview.rotation = quaternion;
                }
                if (status != 0)
                {
                    //logger.LogDebug(status);
                    switch (status)
                    {
                        case -2:
                            __instance.addPanelError = "靠近行星轨道"; break;
                        case -1:
                            __instance.addPanelError = "靠近戴森壳层"; break;
                        default:
                            __instance.addPanelError = "半径超出范围"; break;
                    }
                    __instance.addPanelErrorTip.offset.x = (float)(139.0 + (double)__instance.addSlider0.normalizedValue * 230.0);
                    __instance.addPanelErrorTip.SetText(__instance.addPanelError);
                }
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UIDysonPanel), "UpdateSelectionVisibleChange")]
        public static void UIDysonPanel_UpdateSelectionVisibleChange(UIDysonPanel __instance, UIButton ___orbitAddButton, UIButton ___layerAddButton)
        {
            try
            {
                //logger.LogDebug("UpdateSelectionVisibleChange swarm:[" + __instance.orbitSelected + "] layer:[" + __instance.layerSelected + "]");
                if (__instance.orbitSelected == 0)
                    ___orbitAddButton.GetComponentInChildren<Text>().text = "Add Orbit".Translate();
                else
                {
                    ___orbitAddButton.GetComponentInChildren<Text>().text = "Modify Orbit".Translate();
                    ___orbitAddButton.button.interactable = true;
                }
                if (__instance.layerSelected == 0)
                    ___layerAddButton.GetComponentInChildren<Text>().text = "Add Layer".Translate();
                else
                {
                    ___layerAddButton.GetComponentInChildren<Text>().text = "Modify Layer".Translate();
                    ___layerAddButton.button.interactable = true;
                }

                if (modOrbitMode)
                {
                    __instance.addTitleText.text = "Modify Orbit".Translate();
                    __instance.addPanel.SetActive(true);
                    __instance.orbitPreview._Open();
                    __instance.layerPreview._Close();
                    ___orbitAddButton.highlighted = true;
                    __instance.addOkButton.GetComponentInChildren<Text>().text = "Modify".Translate();
                    __instance.addSlider0.minValue = __instance.viewDysonSphere.minOrbitRadius * minOrbitRadiusMultiplier;
                    __instance.addSlider0.maxValue = __instance.viewDysonSphere.maxOrbitRadius * maxOrbitRadiusMultiplier;
                }
                else if (!modLayerMode)
                {
                    __instance.addOkButton.GetComponentInChildren<Text>().text = "Create".Translate();
                    __instance.addSlider0.minValue = __instance.viewDysonSphere.minOrbitRadius;
                    __instance.addSlider0.maxValue = __instance.viewDysonSphere.maxOrbitRadius;
                }

                if (modLayerMode)
                {
                    __instance.addTitleText.text = ""; //Hide Title so that it won't block silder
                    __instance.addPanel.SetActive(true);
                    __instance.orbitPreview._Close();
                    __instance.layerPreview._Open();
                    ___layerAddButton.highlighted = true;
                    __instance.addOkButton.GetComponentInChildren<Text>().text = "Modify".Translate();
                    modSlider0.gameObject.SetActive(true);
                    modInput0.gameObject.SetActive(true);
                    modText0.gameObject.SetActive(true);
                    __instance.addSlider0.minValue = __instance.viewDysonSphere.minOrbitRadius * minOrbitRadiusMultiplier;
                    __instance.addSlider0.maxValue = __instance.viewDysonSphere.maxOrbitRadius * maxOrbitRadiusMultiplier;

                }
                else
                {
                    if (modSlider0 != null)
                    {
                        modSlider0?.gameObject.SetActive(false);
                        modInput0?.gameObject.SetActive(false);
                        modText0?.gameObject.SetActive(false);
                    }
                    __instance.addOkButton.button.interactable = true;
                }

            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnAddCancelClick")]
        public static void UIDysonPanel_OnAddCancelClick()
        {
            modOrbitMode = modLayerMode = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnSwarmOrbitButtonClick")]
        public static void UIDysonPanel_OnSwarmOrbitButtonClick()
        {
            modOrbitMode = modLayerMode = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "OnShellLayerButtonClick")]
        public static void UIDysonPanel_OnShellLayerButtonClick()
        {
            modOrbitMode = modLayerMode = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "UpdateToolbox")]
        public static bool UIDysonPanel_UpdateToolbox(UIDysonPanel __instance)
        {
            if (modLayerMode)
            {
                __instance.toolbox.SetActive(false);
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDysonPanel), "_OnClose")]
        public static void UIDysonPanel__Close(UIDysonPanel __instance)
        {
            modOrbitMode = modLayerMode = false;
            that = null;
            /*
            for (int i = 1; i < __instance.viewDysonSphere.layersIdBased.Length; i++)
            {
                DysonSphereLayer layer = __instance.viewDysonSphere.layersIdBased[i];
                if (layer != null && layer.id == i && layer.nodeCount != 0)
                {
                    SphereLogic.ValueCorrection(layer);
                }
            }
            */
        }

    }
}