using HarmonyLib;
using UnityEngine;

namespace BuildToolOpt
{
    class PlayerControlGizmo_Patch
    {
        static int inhandItemId;
        static float sqrtRange0;
        static float sqrtRange1;
        static readonly CircleGizmo[] prebuildRangeGizmo = new CircleGizmo[2];

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerControlGizmo), nameof(PlayerControlGizmo.SetPrebuildTarget))]
        public static void SetPrebuildTarget_Postfix(bool enable, Vector3 pos)
        {
            if (enable && prebuildRangeGizmo[0] == null)
            {
                for (int i = 0; i < 2; i++)
                {
                    prebuildRangeGizmo[i] = CircleGizmo.Create(1, Vector3.one, 0.1f);
                    prebuildRangeGizmo[i].autoRefresh = true;
                    prebuildRangeGizmo[i].multiplier = 1.0f;
                    prebuildRangeGizmo[i].alphaMultiplier = 0.8f;
                    prebuildRangeGizmo[i].fadeInScale = 1.3f;
                    prebuildRangeGizmo[i].fadeInTime = 0.13f;
                    prebuildRangeGizmo[i].fadeInFalloff = 0.5f;
                    prebuildRangeGizmo[i].color = Configs.builtin.gizmoColors[8];
                    prebuildRangeGizmo[i].Open();
                }
            }

            if (!enable && prebuildRangeGizmo[0] != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    prebuildRangeGizmo[i].Close();
                    prebuildRangeGizmo[i] = null;
                }
                inhandItemId = 0;
                sqrtRange0 = sqrtRange1 = 0f;
            }

            if (enable && prebuildRangeGizmo[0] != null)
            {
                if (GameMain.mainPlayer.inhandItemId != inhandItemId)
                {
                    inhandItemId = GameMain.mainPlayer.inhandItemId;
                    if (LDB.items.Select(inhandItemId)?.prefabDesc.isStation ?? false)
                    {
                        sqrtRange0 = 841f + 225f; //冗餘量, 調整視覺大小
                        sqrtRange1 = 225f + 9f;
                    }
                    else
                    {
                        sqrtRange0 = 0f;
                        sqrtRange1 = 0f;
                        prebuildRangeGizmo[0].position = prebuildRangeGizmo[1].position = Vector3.one;
                        prebuildRangeGizmo[0].radius = prebuildRangeGizmo[1].radius = 0.1f;
                    }
                    
                }
                if (GameMain.localPlanet != null)
                {
                    if (sqrtRange0 > 0)
                    {
                        float planetRadius = GameMain.localPlanet.realRadius;
                        float cosTheta = 1f - sqrtRange0 / (2f * planetRadius * planetRadius);
                        cosTheta = Mathf.Clamp(cosTheta, -1f, 1f);
                        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
                        prebuildRangeGizmo[0].position = pos / planetRadius * (planetRadius * cosTheta);
                        prebuildRangeGizmo[0].radius = planetRadius * sinTheta;
                    }
                    if (sqrtRange1 > 0)
                    {
                        float planetRadius = GameMain.localPlanet.realRadius;
                        float cosTheta = 1f - sqrtRange1 / (2f * planetRadius * planetRadius);
                        cosTheta = Mathf.Clamp(cosTheta, -1f, 1f);
                        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
                        prebuildRangeGizmo[1].position = pos / planetRadius * (planetRadius * cosTheta);
                        prebuildRangeGizmo[1].radius = planetRadius * sinTheta;
                    }
                }
            }
        }
    }
}
