using HarmonyLib;
using UnityEngine;

namespace SphereEditorTools
{
    class HideLayer : MonoBehaviour
    {
        static int displayMode;
        public static bool EnableMask;
        static GameObject blackmask;

        public static void Free()
        {
            if (blackmask != null)
            {
                Destroy(blackmask);
                blackmask = null;
            }
        }

        public static void SetDisplayMode(int mode)
        {
            displayMode = mode; //0:normal 1,3: hide swarm 2,3: hide star
            GameObject.Find("UI Root/Dyson Map/Star")?.SetActive(displayMode < 2);            
        }

        public static void SetMask(bool enable)
        {
            if (blackmask == null)
            {
                var go = GameObject.Find("UI Root/Dyson Map/Star/black-mask");
                blackmask = Instantiate(go, go.transform.parent.parent);
                Destroy(blackmask.GetComponent<UnityEngine.SphereCollider>());
                var go2 = GameObject.Find("UI Root/Dyson Map/preview/compass");
                blackmask.transform.localScale = new Vector3(go2.transform.localScale.x, go2.transform.localScale.y, 0f); ;
            }
            blackmask.SetActive(enable);
            EnableMask = enable;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DysonMapCamera), nameof(DysonMapCamera._OnLateUpdate))]
        public static void UpdateMask(DysonMapCamera __instance)
        {
            Log.LogPeriod(__instance.editorCamera.transform.rotation);
            if (EnableMask && blackmask != null)
            {
                blackmask.transform.rotation = __instance.editorCamera.transform.rotation;
            }
        }
    }
}
