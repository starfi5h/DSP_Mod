using BepInEx;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using NAudio.Wave;
using System;
using BepInEx.Configuration;

[assembly: AssemblyTitle(AudioReplacer.AudioReplacerPlugin.NAME)]
[assembly: AssemblyVersion(AudioReplacer.AudioReplacerPlugin.VERSION)]

namespace AudioReplacer
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class AudioReplacerPlugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AudioReplacer";
        public const string NAME = "AudioReplacer";
        public const string VERSION = "0.1.0";
        public static AudioReplacerPlugin Instance;

        public Harmony harmony;
        public readonly Dictionary<string, AudioEntry> ModifyAudio = new();
        public string lastWarningMsg = "";
        public string lastInfoMsg = "";
        public ConfigEntry<string> AudioFolderPath;
        public readonly HashSet<string> RegisteredFolders = new(); // dir

        private bool isLoaded;

        public class AudioEntry
        {
            public AudioClip originAudioClip;
            public float originVolume;
            public string filePath;
        }

        public void Awake()
        {
            Instance = this;
            AudioFolderPath = AudioFolderPath = Config.Bind("- General -", "AudioFolderPath", "",
                "The folder to load custom audio files when game startup.\n自定义音频文件的文件夹 (游戏启动时加载)");

            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(AudioReplacerPlugin));
            try
            {
                harmony.PatchAll(typeof(IngameUI));
            }
            catch (Exception e)
            {
                Logger.LogWarning("IngameUI patch fail!");
                Logger.LogWarning(e);
            }

#if !DEBUG
        }
#else
            UnloadAudioFromDirectory();
            LoadAudioFromDirectory(AudioFolderPath.Value);
        }

        public void OnDestroy()
        {
            IngameUI.OnDestory();
            UnloadAudioFromDirectory();
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif

        [HarmonyAfter("me.xiaoye97.plugin.Dyson.LDBTool")]
        [HarmonyPostfix, HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        public static void Init()
        {
            if (Instance.isLoaded) return;
                
            foreach (var dir in Instance.RegisteredFolders)
                LoadAudioFromDirectory(dir);
            LoadAudioFromDirectory(Instance.AudioFolderPath.Value);
            Instance.isLoaded = true;
        }

        public static int UnloadAudioFromDirectory(string dir = "")
        {
            //Instance.Logger.LogDebug("UnloadAudioFromDirectory " + dir);
            var cleanAll = string.IsNullOrEmpty(dir);
            var audioCount = 0;
            foreach (var pair in Instance.ModifyAudio) // Restore original volumes
            {
                var audioProto = LDB.audios[pair.Key];
                if (audioProto == null) continue;
                if (!cleanAll && !pair.Value.filePath.StartsWith(dir)) continue;
                audioProto.audioClip = pair.Value.originAudioClip;
                audioProto.Volume = pair.Value.originVolume;
                audioCount++;
            }
            if (cleanAll)
            {
                Instance.ModifyAudio.Clear();
            }

            Instance.LogInfo($"Unload {audioCount} files. ");
            return audioCount;
        }

        public static int LoadAudioFromDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir))
            {
                Instance.LogInfo("Path is not set yet. ");
                return -1;
            }            
            if (!Directory.Exists(dir))
            {
                Instance.LogWarn("Path doesn't exist! " + dir);
                return -1;
            }
            Instance.Logger.LogDebug("LoadAudioFromDirectory " + dir);

            string[] audioFiles = Directory.GetFiles(dir);
            var audioCount = 0;
            for (int i = 0; i < audioFiles.Length; i++)
            {
                if (TryLoadAudioFile(audioFiles[i])) audioCount++;
            }

            Instance.LogInfo($"Load {audioCount}/{audioFiles.Length} files. ");
            return audioCount;
        }

        public static bool TryLoadAudioFile(string fullFilePath, string audioName = "")
        {
            if (string.IsNullOrEmpty(audioName))
            {
                audioName = Path.GetFileNameWithoutExtension(fullFilePath);
            }
            var fileExtension = Path.GetExtension(fullFilePath).ToLowerInvariant();

            if (LDB.audios[audioName] == null)
            {
                Instance.LogWarn($"{audioName} doesn't exist in LDB.audios");
                return false;
            }
            if (LDB.audios[audioName].Volume == 0.0f) // Mute by AutoMute
            {
                Instance.LogWarn($"Skip {Path.GetFileName(fullFilePath)} because {audioName} is muted!");
                return false;
            }

            try
            {
                switch (fileExtension)
                {
                    case ".mp3":
                        Instance.GetMP3AudioClip(fullFilePath, audioName);
                        break;

                    case ".wav":
                    case ".ogg":
                        Instance.StartCoroutine(Instance.GetAudioClip(fullFilePath, audioName));
                        break;

                    default:
                        Instance.LogWarn($"Unsupport format {fileExtension}");
                        return false;
                }
            }
            catch (Exception e)
            {
                Instance.LogWarn($"Error when loading {Path.GetFileName(fullFilePath)}");
                Instance.Logger.LogWarning(e);
                return false;
            }
            return true;
        }

        private void GetMP3AudioClip(string fullFilePath, string audioName)
        {
            var reader = new Mp3FileReader(fullFilePath);

            // Read audio data into a buffer
            int length = (int)reader.Length;
            byte[] buffer = new byte[length];
            int bytesRead = reader.Read(buffer, 0, length);

            // Convert audio data to Unity's AudioClip format
            AudioClip audioClip = AudioClip.Create(audioName, bytesRead / 2, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, false);
            audioClip.SetData(ConvertBytesToFloat(buffer), 0);

            ReplaceAudioClip(audioClip, fullFilePath, audioName);
        }

        private static float[] ConvertBytesToFloat(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2];
            for (int i = 0; i < floatArray.Length; i++)
            {
                short sample = (short)((byteArray[i * 2 + 1] << 8) | byteArray[i * 2]);
                floatArray[i] = sample / 32768f;
            }
            return floatArray;
        }

        private IEnumerator GetAudioClip(string fullPath, string audioName)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.UNKNOWN);
            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                Logger.LogWarning("Error when getting " + fullPath);
                Logger.LogWarning(www.error);
            }
            else
            {
                var audioClip = DownloadHandlerAudioClip.GetContent(www);
                ReplaceAudioClip(audioClip, fullPath, audioName);
            }            
            yield break;
        }

        private void ReplaceAudioClip(AudioClip audioClip, string fullFilePath, string audioName)
        {
            var audioProto = LDB.audios[audioName];

            if (audioProto == null)
            {
                LogWarn($"Skip {Path.GetFileName(fullFilePath)} because {audioName} is missing!");
                return;
            }

            if (ModifyAudio.TryGetValue(audioName, out var value))
            {
                Logger.LogDebug($"Unload [{audioName}]: {value.filePath}");
                audioProto.audioClip = value.originAudioClip;
                audioProto.Volume = value.originVolume;
                ModifyAudio.Remove(audioName);
            }

            Logger.LogDebug($"Load [{audioName}]: {Path.GetFileName(fullFilePath)}");
            var record = new AudioEntry
            {
                originAudioClip = audioProto.audioClip,
                originVolume = audioProto.Volume,
                filePath = fullFilePath
            };
            audioProto.audioClip = audioClip;
            ModifyAudio.Add(audioName, record);

            // Reload planet audio. Apply after restarting the game or reloading the planet.
            AudioData.LoadStatic();
        }

        internal void LogInfo(string str)
        {
            Logger.LogInfo(str);
            lastInfoMsg = str;
        }

        internal void LogWarn(string str)
        {
            Logger.LogWarning(str);
            lastWarningMsg = str;
        }
    }
}
