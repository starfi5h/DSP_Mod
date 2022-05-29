# Bullet Time

![Pause Mode](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/BulletTime/img/demo1.gif)  

Let user slow down game speed to reduce workload, so FPS may improve.  
Run autosave in the background.  

## Feature

### Game speed adjustment  
Slow game speed down to lower calculation workload. Improve FPS in late game.  
The speed adjustment only affects factories, the mecha will still in normal speed.  
The control slider is on in-game statistic performance panel.  
When speed is set to 0, the game will enter pause mode, game tick and factories will stop.  
  
### Background autosave  
Run autosave on background thread so the game won't freeze.  
To make sure factory data is consistent, the game will enter pause mode during autosave.  
The interaction with the factory is prohibited during the time exporting local factory data.  


## Configuration

Run the game one time to generate `com.starfi5h.plugin.BulletTime.cfg` file.  
Key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html).   

- `EnableBackgroundAutosave`  
Run autosave in background. (Default:`true`)  

- `KeyAutosave`  
Keyboard shortcut for auto-save. (Default:`F10 + LeftShift`)  
  
- `StartingSpeed`  
Game speed when the game begin. range:0-100  (Default:`100`)  

- `EnableFastLoading`  
Increase main menu loading speed. (Default:`true`)  


## Compatibility

- [✅] GalacticScale  
- [✅] NebulaMultiplayer  

### Extra Functions in Nebula Multiplayer  

- When a player is joining or requesting factory data, unfreeze and enter pasue mode (the player can move).  
- When host is saving or manually entering pause mode, the client will enter pause mode too.  
- If clients disconnect during pausing, the host can manually resume the game by changing the slider.  
- Players can stop/resume dyson sphere rotation in the editor at top-left button.  


----

## 调整游戏速度

在性能测试面板可以调整游戏速度(0~100%)，只影响工厂，机甲仍保持正常速度。  
速度为0时进入时停模式，gameTick会在离开时停模式后恢复。  

## 背景自动保存

在背景自动保存的期间，游戏会进入时停模式。写入当地工厂時，玩家和唯读的工厂互动会被阻止。  
使用这项功能时建议先测试。

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.BulletTime` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.BulletTime.cfg`文件  

- `EnableBackgroundAutosave`  
在背景执行自动保存。 (预设为开启`true`)  

- `KeyAutosave`  
自动存档的热键组合 (预设为`F10 + LeftShift`)  
  
- `StartingSpeed`   
开始时的游戏速度，范围: 0-100 (预设为`100`)  

- `EnableFastLoading`  
加快载入主选单 (预设为开启)  


## 联机功能  

- 当玩家加入、请求工厂数据、存檔時，进入时停模式。在载入完成后恢复。  
- 如果游戏卡在时停模式，主机可以通过拖动滑块来恢复游戏运行。 
- 玩家可以在编辑器左上角的按钮中停止/恢复戴森球旋转。  
----

## Changelog

#### v1.2.6
\- Change `KeyAutosave` from KeyCode to KeyboardShortcut  
\- Small tweak to backgroud autosave. (Game version 0.9.26.12201)  

#### v1.2.5
\- Add EnableFastLoading config option. (Game version 0.9.25.11996)  
\- (Nebula) Fix an issue that sometimes when client disconnect, the host will enter pause state.  

#### v1.2.4
\- (Nebula) Resume from pause when a client disconnect during loading a factory.  

#### v1.2.3
\- (Nebula) Fix host sometimes hangs in pause mode when loading factories. Now manual saving will reset pause states.   
\- Make block image in background autosave transparent.  

#### v1.2.2
\- (Nebula) Enable dyson sphere rotation start/stop button in editor.   
\- (Nebula) Handle multiple pause events that happen at the same time.  

#### v1.2.1
\- Show game speed in FPS indicator (Shift + F12)  
\- Fix camera & mecha movement speed in low speed.  

#### v1.2.0
\- (Nebula) Add support for multiplayer.  


#### v1.1.0
\- Add StartingSpeed config option.  
\- Only block interaction during exporting local factory.  

#### v1.0.2  
\- Initial release. (Game version 0.9.24.11286)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
<a href="https://www.flaticon.com/free-icons/pause-button" title="pause-button icons">Pause-button icons created by Uniconlabs - Flaticon</a>