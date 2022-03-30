using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PluginsAnalyzer
{
    //[BepInPlugin(GUID, NAME, VERSION)]
    public class PluginsAnalyzerPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.starfi5h.plugin.PluginsAnalyzer";
        public const string NAME = "PluginsAnalyzer";
        public const string VERSION = "0.0.1";

        public void Awake()
        {
            Log.Init(Logger);
            Analysis();
        }
        public void OnDestroy()
        {
        }

        public void Analysis()
        {
            List<PatchRecord> records = new List<PatchRecord>();
            foreach (MethodBase methodBase in PatchProcessor.GetAllPatchedMethods())
            {
                if (methodBase.DeclaringType.FullName.StartsWith("System"))
                    continue;

                Patches patchInfo = PatchProcessor.GetPatchInfo(methodBase);

                if (patchInfo.Owners.Count == 1)
                    continue;

                records.Add(new PatchRecord(methodBase));
            }

            foreach (PatchRecord record in records)
            {
                record.Print();
            }
        }

    }


    public class PatchRecord
    {
        string fullname;
        string declareType;
        List<string> moduleNames;
        string functions;

        public PatchRecord(MethodBase methodBase)
        {
            fullname = $"{methodBase.DeclaringType.FullName}.{methodBase.Name}";
            declareType = methodBase.ToString();
            Patches patchInfo = PatchProcessor.GetPatchInfo(methodBase);
            StringBuilder functionSb = new StringBuilder();
            moduleNames = new List<string>();
            RecordPatches(functionSb, " Finalizer", patchInfo.Finalizers);
            RecordPatches(functionSb, "    Prefix", patchInfo.Prefixes);
            RecordPatches(functionSb, "   Postfix", patchInfo.Postfixes);
            RecordPatches(functionSb, "Transpiler", patchInfo.Transpilers);
            functions = functionSb.ToString();
        }

        private void RecordPatches(StringBuilder functionSb, string prefix, ReadOnlyCollection<Patch> buckets)
        {
            for (int i = 0; i < buckets.Count; ++i)
            {
                Patch patch = buckets[i];

                if (!moduleNames.Contains(patch.PatchMethod.Module.Name))
                {
                    moduleNames.Add(patch.PatchMethod.Module.Name);
                }

                functionSb.Append(prefix)
                    .Append("[")
                    .Append(patch.index)
                    .Append("]: ")
                    .Append(patch.PatchMethod.FullDescription())
                    .Append('\n');
            }
        }


        public void Print()
        {
            Log.Warn(fullname);
            Log.Info(declareType);
            string dllName = String.Join(", ", moduleNames.ToArray());
            Log.Debug($"Mods: {dllName}\n{functions}");
        }

    }

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
    }
}
