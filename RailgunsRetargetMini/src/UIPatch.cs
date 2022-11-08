using HarmonyLib;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RailgunsRetargetMini
{
    public class UIPatch
    {
        static Button ejectorInfoBtn;

        [HarmonyPrefix, HarmonyPatch(typeof(UIDESwarmOrbitInfo), "_OnRegEvent")]
        public static void OnRegEvent()
        {
            if (ejectorInfoBtn == null)
            {
                GameObject go = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/swarm-orbit-group/icon");
                ejectorInfoBtn = go.AddComponent<Button>();
            }
            if (ejectorInfoBtn != null)
            {
                ejectorInfoBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(Onclick));
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIDESwarmOrbitInfo), "_OnUnregEvent")]
        public static void OnUnregEvent()
        {
            if (ejectorInfoBtn != null) 
            {
                ejectorInfoBtn.onClick.RemoveAllListeners();
            }
        }

        public static void OnDestory()
        {
            Object.Destroy(ejectorInfoBtn);
            ejectorInfoBtn = null;
        }

        public static void Onclick()
        {
            StarData starData = UIRoot.instance.uiGame.dysonEditor.selection.viewStar;
            int orbitId = UIRoot.instance.uiGame.dysonEditor.selection.selectedSwarmOrbitIds[0];

            if (NebulaCompat.IsClient)
                NebulaCompat.SendPacket(starData, -orbitId);
            else
                ShowMessage(GetStatus(starData), starData, orbitId);
        }

        public static void ShowMessage(string message, StarData starData, int orbitId)
        {
            UIMessageBox.Show("Railguns Retarget Mini", message,
                "Cancel", "Unset All", "Set All", UIMessageBox.INFO,
                null,
                () => { OnResponse(starData, 0); },
                () => { OnResponse(starData, orbitId); });
        }

        public static void OnResponse(StarData starData, int orbitId)
        {
            if (orbitId == 0 && Configs.ForceRetargeting == true)
            {
                UIMessageBox.Show("Railguns Retarget Mini", "Cannot unset because config.ForceRetargeting = true.", "OK", UIMessageBox.WARNING, null);
                return;
            }
            SetOrbit(starData, orbitId);
            if (NebulaCompat.IsMultiplayer)
                NebulaCompat.SendPacket(starData, orbitId);
        }

        public static void SetOrbit(StarData starData, int orbitId)
        {
            int ejectorCount = 0;
            foreach (var planet in starData.planets)
            {
                if (planet.factory != null)
                {
                    FactorySystem factorySystem = planet.factory.factorySystem;
                    for (int i = 1; i < factorySystem.ejectorCursor; i++)
                    {
                        if (factorySystem.ejectorPool[i].id == i)
                        {
                            factorySystem.ejectorPool[i].orbitId = orbitId;
                            ++ejectorCount;
                        }
                    }
                }
            }
            Plugin.Log.LogInfo($"Set {ejectorCount} ejectors in {starData.displayName} to orbit {orbitId}");
        }

        public static string GetStatus(StarData starData)
        {
            if (starData == null)
                return "";

            int[] setCount = new int[starData.planets.Length];
            int[] unsetCount = new int[starData.planets.Length];
            int totalCount = 0;

            for (int pid = 0; pid < starData.planets.Length; pid++)
            {
                if (starData.planets[pid] != null && starData.planets[pid].factory != null)
                {
                    FactorySystem factorySystem = starData.planets[pid].factory.factorySystem;
                    for (int i = 1; i < factorySystem.ejectorCursor; i++)
                    {
                        if (factorySystem.ejectorPool[i].id == i)
                        {
                            if (factorySystem.ejectorPool[i].orbitId > 0)
                                setCount[pid]++;
                            else
                                unsetCount[pid]++;
                            totalCount++;
                        }
                    }
                }
            }

            StringBuilder sb = new();
            sb.Append("EM-Rail Ejectors status: Total count ").Append(totalCount).AppendLine();
            for (int pid = 0; pid < starData.planets.Length; pid++)
            {
                if ((setCount[pid] + unsetCount[pid]) == 0)
                    continue;
                sb.Append(starData.planets[pid].displayName).Append(": Set ").Append(setCount[pid]).Append(", Unset ").Append(unsetCount[pid]).AppendLine();
            }
            return sb.ToString();
        }
    }
}
