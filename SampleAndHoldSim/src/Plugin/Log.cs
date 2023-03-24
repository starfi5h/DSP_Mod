using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SampleAndHoldSim
{
    public static class Log
    {
        private static ManualLogSource _logger;
        private static int count;
        public static void Init(ManualLogSource logger) =>
            _logger = logger;
        public static void Error(object obj) =>
            _logger.LogError(obj);
        public static void Warn(object obj) =>
            _logger.LogWarning(obj);
        public static void Info(object obj) =>
            _logger.LogInfo(obj);
        public static void Debug(object obj) =>
            _logger.LogDebug(obj);

        public static void Print(int period, object obj)
        {
            if ((count++) % period == 0)
                _logger.LogDebug(obj);
        }

        public static void PrintInstruction(IEnumerable<CodeInstruction> instructions, int start, int end)
        {
            int count = -1;
            foreach (var i in instructions)
            {
                if (count++ <= start)
                    continue;
                if (count >= end)
                    break;

                if (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt)
                    Log.Warn($"{count,2} {i}");
                else if (i.IsLdarg())
                    Log.Info($"{count,2} {i}");
                else
                    Log.Debug($"{count,2} {i}");
            }
        }
    }
}
