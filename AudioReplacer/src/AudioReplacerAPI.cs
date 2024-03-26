namespace AudioReplacer
{
    public class AudioReplacerAPI
    {
        /// <summary>
        ///     Unload all audio from dir. If dir is null, unload all custom audio.
        /// </summary>
        /// <param name="dir">The directory of custom audio</param>
        public static int UnloadAudioFromDirectory(string dir = "")
        {
            return AudioReplacerPlugin.UnloadAudioFromDirectory(dir);
        }

        /// <summary>
        ///     Load all audio from dir. The replacing audio name is set to the file name.
        /// </summary>
        /// <param name="dir">The directory of custom audio</param>
        public static int LoadAudioFromDirectory(string dir)
        {
            return AudioReplacerPlugin.LoadAudioFromDirectory(dir);
        }

        /// <summary>
        ///     Load a custom audio file. If audioName is not specify, it will use the filename.
        /// </summary>
        /// <param name="fullFilePath">Full path to the custom audio file</param>
        /// <param name="audioName">The name of audioProto to replace</param>
        public static bool TryLoadAudioFile(string fullFilePath, string audioName = "")
        {
            return AudioReplacerPlugin.TryLoadAudioFile(fullFilePath, audioName);
        }
    }
}
