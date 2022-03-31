/*
using HarmonyLib;

// Make compatible to CommonAPI custom components functions
// https://github.com/kremnev8/CommonAPI/blob/d39bf67004d26dd87c24346dd3c2d741a087d646/CommonAPI/Systems/PlanetExtensionSystem/Patches/PlanetExtensionHooks.cs

namespace ThreadOptimization
{
    class Compatiblity
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultithreadSystem), "PreparePowerSystemFactoryData")]
        [HarmonyPatch(typeof(MultithreadSystem), "PrepareAssemblerFactoryData")]
        [HarmonyPatch(typeof(MultithreadSystem), "PrepareTransportData")]
        internal static bool DummyFunctions()
        {
            return false;
        }
    }
}
*/