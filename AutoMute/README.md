# AutoMute 

1. Automatically mute sounds when game loses focus, i.e. alt-tab to background.   
2. Mute user-specified building working sounds.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `AutoMute.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `starfi5h.plugin.AutoMute.cfg` file.  


- `Mute In Background` - Whether to mute the game when in the background, i.e. Alt-tabbed.  (Default:`true`)  
- `Mute Building Ids` - The [item IDs](https://dsp-wiki.com/Modding:Items_IDs) of building to mute, separated by comma.  
For example, to mute Ray receiver and Artificial star, type `2208, 2210`  

----

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.AutoMute` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.AutoMute.cfg`文件  

- `Mute In Background` 当游戏失去焦点，在背景执行时静音。 (预设为开启)  
- `Mute Building Ids` 输入物品ID即可静音该建筑。以逗号分隔。

----

## Changelog

#### v1.0.0
\- Initial release.  

#### v0.1.0  
\- Beta test. (Game Version 0.9.25.12201)

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  

<a href="https://www.flaticon.com/free-icons/mute" title="mute icons">Mute icons created by SumberRejeki - Flaticon</a>