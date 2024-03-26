# AudioReplacer

![Settings](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AudioReplacer/img/settings.png)  
In Settings - Audio tab, there is a mod UI.  
The upper part is path selection and load/unload custom audio from the target folder.  
The lower part are drop-down list and search input that can veiw and play each custom audio.  

## Audio Name
When load from folder, it will take the file name and try to find the audio with the same name in `LDB.audios` then replace it.  
You can install [AutoMute](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/AutoMute/) to get the current available audio name in the game.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `AutoMute.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `starfi5h.plugin.AudioReplacer.cfg` file.  

- `AudioFolderPath` - The folder to load custom audio files when game startup.

----

## 音频名称
当从文件夹加载时，它将获取文件名并尝试在`LDB.audios`中查找同名的音频并替换。  
您可以安装[AutoMute](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/AutoMute/)来获取游戏中当前可用的音频名称。  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要点击游戏内'应用设置'的按钮才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.AudioReplacer` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.AudioReplacer.cfg`文件  

- `AudioFolderPath` - 自定义音频文件的文件夹 (游戏启动时加载)  

----

<a href="https://www.flaticon.com/free-icons/volume" title="volume icons">Volume icons created by SumberRejeki - Flaticon</a>