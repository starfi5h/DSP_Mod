using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace AutoMute
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AutoMute";
        public const string NAME = "AutoMute";
        public const string VERSION = "1.1.1";

        internal static Plugin Instance;
        internal Harmony harmony;
        internal ConfigEntry<bool> MuteInBackground;
        internal ConfigEntry<string> MuteBuildingIds;
        internal ConfigEntry<string> MuteList;
        
        // Audio volume backups
        static float? OriginalVolume = null;
        readonly static Dictionary<string, float> AudioVolumes = new Dictionary<string, float>();
        readonly static Dictionary<int, float> ModelVolumes = new Dictionary<int, float>();

        public void BindConfig()
        {            
            MuteInBackground = Config.Bind("- General -", "Mute In Background", true, "Enable to mute the game when in the background, i.e. alt-tabbed.\n游戏在后台时自动静音，切换到前台时恢复");
            MuteBuildingIds = Config.Bind("- General -", "Mute Building Ids", "", "The ids of building to mute, separated by white spaces.\n消除指定建筑的音讯。输入:建筑物品id, 以空白分隔。");
            MuteList = Config.Bind("- General -", "MuteList", "", "The list of audio name to mute, separated by white spaces. Check mod page wiki for available names.\n消除指定的音讯。输入:音讯名称, 以空白分隔(名称可以在mod页面wiki查询)");
        }

        internal void Awake()
        {
            BindConfig();
            Instance = this;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));

            // Suppress the sound when finishing craft
            var method = AccessTools.Method(typeof(UIMechaMoveTip), "OnForgeTaskDelivery");
            if (method != null) harmony.Patch(method, new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(Suppress))));
#if !DEBUG
        }
#else
            //Print();
        }

        void Print()
        {
            string str = "\n| Name | ClipPath | Note |\n| --- | --- | --- |\n";
            foreach (var audioProto in LDB.audios.dataArray)
            {
                str += $"{audioProto.name} | {audioProto.ClipPath} |  |\n";
            }
            Logger.LogDebug(str);
        }

        internal void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
#endif

        internal void OnApplicationFocus(bool hasFocus)
        {
            // origin from https://github.com/BepInEx/BepInEx.Utility/blob/master/BepInEx.MuteInBackground/MuteInBackground.cs
            if (hasFocus)
            {
                if (OriginalVolume != null)
                    AudioListener.volume = (float)OriginalVolume;
                OriginalVolume = null;
            }
            else if (MuteInBackground.Value)
            {
                OriginalVolume = AudioListener.volume;
                AudioListener.volume = 0;
            }
        }

        static bool Suppress()
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.OnApplyClick))]
        internal static void OnApplyClick()
        {
            Instance.Config.Reload(); // Refresh config file when click
            ChangeVolumes(); // Apply changes to LDB.audios
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")]
        [HarmonyAfter("me.xiaoye97.plugin.Dyson.LDBTool")]
        internal static void ChangeVolumes()
        {
            ChangeAudioVolumes();
            ChangeBuildingVolumes();

            if (AudioVolumes.Count + ModelVolumes.Count > 0)
            {
                Instance.Logger.LogDebug($"Mute {AudioVolumes.Count} audios and {ModelVolumes.Count} buildings.");
            }
        }

        static void ChangeAudioVolumes()
        {
            foreach (var pair in AudioVolumes) // Restore original volumes
            {
                var audioProto = LDB.audios[pair.Key];
                if (audioProto != null)
                {
                    audioProto.Volume = pair.Value;
                    //Instance.Logger.LogDebug($"Restore {pair.Key}: {pair.Value}");
                }
            }
            AudioVolumes.Clear();

            foreach (var audioName in Instance.MuteList.Value.Split(new char[] { ' ', ',', '\n'}))
            {
                if (audioName.IsNullOrWhiteSpace()) continue;
                var audioProto = LDB.audios[audioName];
                if (audioProto != null)
                {
                    AudioVolumes.Add(audioName, audioProto.Volume); // Backup for original volumes
                    //Instance.Logger.LogDebug($"Mute {audioName}: {audioProto.Volume}");
                    audioProto.Volume = 0;
                }
                else
                {
                    Instance.Logger.LogWarning("Can't find audio name: " + audioName);
                }
            }
        }

        static void ChangeBuildingVolumes()
        {
            foreach (var pair in ModelVolumes) // Restore original volumes
            {
                var model = LDB.models.Select(pair.Key);
                if (model != null)
                {
                    model.prefabDesc.audioVolume = pair.Value;
                    //Instance.Logger.LogDebug($"Restore {pair.Key}: {pair.Value}");
                }
            }
            ModelVolumes.Clear();

            foreach (var str in Instance.MuteBuildingIds.Value.Split(new char[] { ' ', ',' }))
            {
                if (str.IsNullOrWhiteSpace()) continue;
                if (int.TryParse(str, out int itemId))
                {
                    var item = LDB.items.Select(itemId);
                    if (item == null)
                    {
                        Instance.Logger.LogWarning($"Can't find item {itemId}");
                    }
                    string itemName = item.Name;
                    int modelIndex = item.ModelIndex;
                    var model = LDB.models.Select(modelIndex);
                    if (model != null)
                    {
                        //Instance.Logger.LogDebug($"Mute [{itemId}]{itemName}: {model.prefabDesc.audioVolume}");
                        ModelVolumes.Add(modelIndex, model.prefabDesc.audioVolume);
                        model.prefabDesc.audioVolume = 0;
                    }
                }
                else
                {
                    Instance.Logger.LogWarning($"Can't parse \"{str}\" to int");
                }
            }
        }
    }
}
