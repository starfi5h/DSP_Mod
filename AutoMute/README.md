# AutoMute 

1. Automatically mute when the game loses focus, i.e. alt-tab to the background.   
2. Mute user-specified building working sounds.  
3. Mute user-specified UI or world audio.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `AutoMute.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `starfi5h.plugin.AutoMute.cfg` file.  
After changing `.cfg` file, go to in-game settings and click 'Apply' to apply the new config.  

- `Mute In Background` - Enable to mute the game when in the background, i.e. alt-tabbed.  (Default:`true`)  
- `Mute Building Ids` - The [item IDs](https://dsp-wiki.com/Modding:Items_IDs) of building to mute, separated by white spaces.   
For example, to mute Ray receiver and Artificial star, key in `2208 2210`. Reload the planet to take effect.  
- `MuteList` - The list of audio name to mute, separated by white spaces. Check [mod page wiki](https://dsp.thunderstore.io/package/starfi5h/AutoMute/wiki/) for available names.  
For example, to mute UI button click sounds, key in `ui-click-0 ui-click-1 ui-click-2`  

----

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要点击游戏内'应用设置'的按钮才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.AutoMute` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.AutoMute.cfg`文件  

- `Mute In Background` 当游戏失去焦点(在背景执行)时静音。 (预设为开启)  
- `Mute Building Ids` 输入[物品ID](https://dsp-wiki.com/Modding:Items_IDs)即可静音该建筑。以逗号分隔。(需重新载入星球)
- `MuteList` 消除指定的音讯。输入:音效名称, 以空白分隔(名称可以在[mod页面wiki](https://dsp.thunderstore.io/package/starfi5h/AutoMute/wiki/)查询)

----

## Changelog

#### v1.1.0
\- Add config option `MuteList`  
\- Config changes now can apply when clicking in-game setting apply button.  

#### v1.0.0
\- Initial release.  

#### v0.1.0  
\- Beta test. (Game Version 0.9.25.12201)

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  

<a href="https://www.flaticon.com/free-icons/mute" title="mute icons">Mute icons created by SumberRejeki - Flaticon</a>