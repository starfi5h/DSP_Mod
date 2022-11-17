using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace LossyCompression
{
#if DEBUG
    class DebugPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.FixedUpdate))]
        private static void FixedUpdate_Postfix()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                DysonShellCompress.IsMultithread = !DysonShellCompress.IsMultithread;
                Log.Info($"DysonShellCompress.IsMultithread: {DysonShellCompress.IsMultithread}");
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                // swarm
                //Test((w) => DysonSwarmCompress.Export(w), (r) => DysonSwarmCompress.Import(r), RemoveAllSails);

                // cargo
                //TestCargoPath();
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                CargoPathCompress.Enable = !CargoPathCompress.Enable;
                DysonShellCompress.Enable = !DysonShellCompress.Enable;
                DysonSwarmCompress.Enable = !DysonSwarmCompress.Enable;
                Log.Info($"CargoPathCompress: {CargoPathCompress.Enable}");
                Log.Info($"DysonShellCompress: {DysonShellCompress.Enable}");
                Log.Info($"DysonSwarmCompress: {DysonSwarmCompress.Enable}");
            }
        }

        public static void RemoveAllSails()
        {
            for (int starIndex = 0; starIndex < GameMain.data.dysonSpheres.Length; starIndex++)
            {
                if (GameMain.data.dysonSpheres[starIndex] != null)
                    GameMain.data.dysonSpheres[starIndex].swarm.RemoveSailsByOrbit(-1);
            }
        }


        private static void Test(Action<BinaryWriter> encode, Action<BinaryReader> decode, Action middle = null)
        {
            HighStopwatch stopwatch = new HighStopwatch();
            double decode_time = 0, encode_time = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                stopwatch.Begin();
                BinaryWriter writer = new BinaryWriter(stream);
                encode(writer);
                encode_time = stopwatch.duration;

                stream.Seek(0, SeekOrigin.Begin);
                middle?.Invoke();

                stopwatch.Begin();
                BinaryReader reader = new BinaryReader(stream);
                decode(reader);
                decode_time = stopwatch.duration;
            }

            Log.Info($"Encode: {encode_time} s | Decode: {decode_time} s");
            UIRoot.instance.uiGame.statWindow.performancePanelUI.RefreshDataStatTexts();
        }

        private static void TestDysonShellCompress()
        {
            HighStopwatch stopwatch = new HighStopwatch();
            stopwatch.Begin();
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                DysonShellCompress.Export(writer);
                stream.Seek(0, SeekOrigin.Begin);
                BinaryReader reader = new BinaryReader(stream);
                DysonShellCompress.Import(reader);
            }
            Log.Info($"Time cost: {stopwatch.duration}s");
            UIRoot.instance.uiGame.statWindow.performancePanelUI.RefreshDataStatTexts();
        }

        private static void TestCargoPath()
        {
            CargoPathCompress.Reset();
            HighStopwatch stopwatch = new HighStopwatch();
            stopwatch.Begin();

            for (int i = 0; i < GameMain.data.factoryCount; i++)
            {
                EncodePlanetBelt(GameMain.data.factories[i].cargoTraffic);
            }

            Log.Info($"Time cost: {stopwatch.duration}s");
            CargoPathCompress.Print();
        }

        private static void EncodePlanetBelt(CargoTraffic cargoTraffic)
        {
            for (int m = 1; m < cargoTraffic.pathCursor; m++)
            {
                if (cargoTraffic.pathPool[m] != null && cargoTraffic.pathPool[m].id == m)
                {
                    CargoPath cargoPath = cargoTraffic.pathPool[m];
                    using (MemoryStream stream = new MemoryStream())
                    {
                        BinaryWriter writer = new BinaryWriter(stream);
                        CargoPathCompress.Encode(cargoPath, writer);
                        stream.Seek(0, SeekOrigin.Begin);
                        BinaryReader reader = new BinaryReader(stream);
                        CargoPathCompress.Decode(cargoPath, reader);
                    }
                }
            }
        }
    }
#endif
}