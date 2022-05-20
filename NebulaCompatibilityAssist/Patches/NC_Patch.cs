using NebulaAPI;
using HarmonyLib;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NC_Patch
    {
        public static void Init(Harmony harmony)
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            LSTM.Init(harmony);
        }
    }
}
