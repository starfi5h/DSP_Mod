using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SphereEditorTools
{
    class VisualEffect
    {
        static List<Tuple<int, int>> sequenceList;

        [HarmonyTranspiler, HarmonyPatch(typeof(DysonSphere), "GameTick")]
        static IEnumerable<CodeInstruction> DysonSphere_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                //change roation after DysonLayer.GameTick() finish in for-loop
                var matcher = new CodeMatcher(instructions)
                    .MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DysonSphereLayer), "GameTick")))
                    .MatchForward(true, new CodeMatch(OpCodes.Blt))
                    .Advance(1)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(VisualEffect), "ChangeLayersRotaiton"))
                    );
                return matcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.LogError(e);
                Log.LogError("DysonSphere_GameTick_Transpiler failed. Restore original IL");
                return instructions;
            }
        }

        public static void LoadSequence()
        {
            if (sequenceList == null)
            {
                sequenceList = new List<Tuple<int, int>>();
            }
            sequenceList.Clear();
            if (SphereEditorTools.EnableVisualEffect.Value)
            {
                try
                {
                    string str = "ChainedRotation enable. Pair: ";
                    foreach (var x in SphereEditorTools.VFXchainedSequence.Value.Split(','))
                    {
                        var y = x.Split('-');
                        int a = int.Parse(y[0].Trim());
                        int b = int.Parse(y[y.Length - 1].Trim());
                        sequenceList.Add(new Tuple<int, int>(a, b));
                        str += $"({a}-{b})";
                    }
                    Log.LogDebug(str);
                }
                catch (Exception ex)
                {
                    Log.LogWarning("chainedSequence parse error");
                    Log.LogWarning(ex);
                }
            }
            else
                Log.LogDebug("ChainedRotation disable");
        }

        static void ChangeLayersRotaiton(DysonSphere sphere)
        {
            if (SphereEditorTools.EnableVisualEffect.Value)
            {
                foreach (var tuple in sequenceList)
                {
                    DysonSphereLayer layer1 = sphere.GetLayer(tuple.Item1);
                    DysonSphereLayer layer2 = sphere.GetLayer(tuple.Item2);
                    if (layer1 != null && layer2 != null)
                    {
                        layer2.currentRotation = layer1.currentRotation * layer2.currentRotation;
                        layer2.nextRotation = layer1.nextRotation * layer2.nextRotation;
                    }
                }
            }
        }
    }
}
