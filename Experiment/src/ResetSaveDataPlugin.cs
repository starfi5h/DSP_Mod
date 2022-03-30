using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;


namespace ResetSaveData
{
    //[BepInPlugin("com.starfi5h.plugin.ResetSaveData", "ResetSaveData", "0.1.0")]
    public class ResetSaveDataPlugin : BaseUnityPlugin
    {
        Harmony harmony;
        internal void Start()
        {
            harmony = new Harmony("com.starfi5h.plugin.ResetSaveData");
            Log.Init(Logger);
            TryPatch(typeof(DeleteShell));
        }

        void TryPatch(Type type)
        {
            try
            {
                harmony.PatchAll(type);
            }
            catch (Exception e)
            {
                Logger.LogError($"Patch {type.Name} error");
                Logger.LogError(e);
            }
        }

        internal void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    public static class Log
    {
        private static ManualLogSource _logger;
        public static void Init(ManualLogSource logger) =>
            _logger = logger;
        public static void LogError(object obj) =>
            _logger.LogError(obj);
        public static void LogWarning(object obj) =>
            _logger.LogWarning(obj);
        public static void LogInfo(object obj) =>
            _logger.LogInfo(obj);
        public static void LogDebug(object obj) =>
            _logger.LogDebug(obj);
    }

	class DeleteShell
	{
		static int version;

		[HarmonyPrefix, HarmonyPatch(typeof(DysonSphere), "Import")]
		public static void DysonSphere_Import_Prefix(out HighStopwatch __state)
		{
			__state = new HighStopwatch();
			__state.Begin();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(DysonSphere), "Import")]
		public static void DysonSphere_Import_Prefix(DysonSphere __instance, HighStopwatch __state)
		{
			for (int layerId = 1; layerId < __instance.layersIdBased.Length; layerId++)
			{
				if (__instance.layersIdBased[layerId] == null)
					continue;

				DysonSphereLayer layer = __instance.layersIdBased[layerId];
				for (int index = 1; index < layer.nodeCursor; ++index)
				{
					DysonNode node = layer.nodePool[index];
					if (node != null && node.id == index)
					{
						node.shells.Clear();
						node.RecalcCpReq();
					}
				}
				for (int index = 1; index < layer.shellCursor; ++index)
				{
					DysonShell shell = layer.shellPool[index];
					if (shell != null && shell.id == index)
						layer.RemoveDysonShell(shell.id);
				}

				//layer.shellPool = null;
				//layer.shellRecycle = null;
				//layer.SetShellCapacity(64);
				//layer.shellCursor = 0;
				//layer.shellRecycleCursor = 0;
			}
			Log.LogDebug($"{__state.duration,10}s Version[{version}] Sphere {__instance.starData.displayName} imported.");
		}



		[HarmonyPrefix, HarmonyPatch(typeof(DysonShell), "Import")]
		public static bool DysonShell_Import_Prefix(DysonShell __instance, BinaryReader r, DysonSphere dysonSphere)
		{
			__instance.SetEmpty();
			int peekChar = r.PeekChar();
			if (peekChar == 'X')
			{
				version = (int)peekChar;
				r.ReadChar();
				ShellImport_SphereSaveFix_X(__instance, r);
			}
			version = r.ReadInt32();
			if (version == 0)
			{
				ShellImport_0(__instance, r);
			}
			else
			{
				ShellImport_1(__instance, r);
			}
			return false;
		}





		static void ShellImport_1(DysonShell shell, BinaryReader r)
		{
			shell.SetEmpty();
			shell.id = r.ReadInt32();
			shell.protoId = r.ReadInt32();
			shell.layerId = r.ReadInt32();
			shell.randSeed = r.ReadInt32();
			int num13 = r.ReadInt32();
			for (int num14 = 0; num14 < num13; num14++)
			{
				r.ReadSingle();
				r.ReadSingle();
				r.ReadSingle();
			}
			int num15 = r.ReadInt32();
			for (int num16 = 0; num16 < num15; num16++)
			{
				r.ReadInt32();
			}
			int vertexCount = r.ReadInt32();
			r.ReadInt32();
			int num19 = r.ReadInt32();
			r.ReadDouble();
			for (int num22 = 0; num22 < num19; num22++)
			{
				r.ReadInt64();
			}
			num19 = r.ReadInt32();
			for (int num24 = 0; num24 < num19; num24++)
			{
				r.ReadInt16();
				r.ReadInt16();
			}
			num19 = r.ReadInt32();
			if (vertexCount > 65500)
			{
				for (int num25 = 0; num25 < num19; num25++)
				{
					r.ReadInt32();
				}
			}
			else
			{
				for (int num26 = 0; num26 < num19; num26++)
				{
					r.ReadUInt16();
				}
			}
			num19 = r.ReadInt32();
			for (int num27 = 0; num27 < num19; num27++)
			{
				r.ReadUInt16();
			}
			num19 = r.ReadInt32();
			for (int num28 = 0; num28 < num19; num28++)
			{
				r.ReadUInt32();
			}
			num19 = r.ReadInt32();
			for (int num30 = 0; num30 < num19; num30++)
			{
				r.ReadInt32();
			}
			num19 = r.ReadInt32();
			for (int num31 = 0; num31 < num19; num31++)
			{
				r.ReadInt32();
			}
			r.ReadInt32();
			int vertRecycleCursor = r.ReadInt32();
			for (int num32 = 0; num32 < vertRecycleCursor; num32++)
			{
				r.ReadInt32();
			}
		}

		static void ShellImport_0(DysonShell shell, BinaryReader r)
		{
			shell.SetEmpty();
			shell.id = r.ReadInt32();
			shell.protoId = r.ReadInt32();
			shell.layerId = r.ReadInt32();
			shell.randSeed = r.ReadInt32();
			int num2 = r.ReadInt32();
			for (int i = 0; i < num2; i++)
			{
				r.ReadSingle();
				r.ReadSingle();
				r.ReadSingle();
			}
			int num3 = r.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				r.ReadInt32();
			}
			r.ReadInt32();
			r.ReadInt32();
			int num5 = r.ReadInt32();
			for (int l = 0; l < num5; l++)
			{
				r.ReadSingle();
				r.ReadSingle();
				r.ReadSingle();
			}
			num5 = r.ReadInt32();
			for (int m = 0; m < num5; m++)
			{
				r.ReadInt32();
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int n = 0; n < num5; n++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num6 = 0; num6 < num5; num6++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num7 = 0; num7 < num5; num7++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num8 = 0; num8 < num5; num8++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num9 = 0; num9 < num5; num9++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num10 = 0; num10 < num5; num10++)
			{
				r.ReadInt32();
			}
			num5 = r.ReadInt32();
			for (int num11 = 0; num11 < num5; num11++)
			{
				r.ReadUInt32();
			}
			r.ReadInt32();
			int num12 = r.ReadInt32();
			for (int num13 = 0; num13 < num12; num13++)
			{
				r.ReadInt32();
			}
		}

		static void ShellImport_SphereSaveFix_X(DysonShell shell, BinaryReader r)
		{
			shell.SetEmpty();
			shell.id = r.ReadInt32();
			shell.protoId = r.ReadInt32();
			shell.layerId = r.ReadInt32();
			shell.randSeed = r.ReadInt32();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				r.ReadSingle();
				r.ReadSingle();
				r.ReadSingle();
			}
			int num2 = r.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				r.ReadInt32();
			}
			r.ReadInt32(); //vertexCount
			r.ReadInt32(); //triangleCount
			r.ReadInt32(); //vertsLength
			r.ReadInt32(); //pqArrLength
			r.ReadInt32(); //trisLength
			r.ReadInt32(); //vAdjsLength
			r.ReadInt32(); //vertAttrLength
			r.ReadInt32(); //vertsqLength
			r.ReadInt32(); //vertsqOffsetLength

			r.ReadInt32();
			int num5 = r.ReadInt32();
			for (int num11 = 0; num11 < num5; num11++)
			{
				r.ReadInt32();
			}
		}

	}
}
