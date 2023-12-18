# Bullet Time

![Pause Mode](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo1.gif)  

1. Let user slow down game speed to reduce CPU workload, so FPS may improve if it is slowed down by UPS.  
2. Run autosave in the background to make the game stay responsive.  
3. Skip planet modeling for cover to speed up main menu loading.   

## Feature

### Game speed adjustment  
Slow game speed down to lower calculation workload. Improve FPS in late game.  
The speed adjustment affects everything but the mecha, the mecha will still in normal speed.  
The control slider is on in-game statistic performance panel.  
When speed is set to 0, the game will enter pause mode, game tick and factories will stop.  
  
### Background autosave  
Run autosave on background thread so the game won't freeze.  
To make sure factory data is consistent, the game will enter pause mode during autosave.  
The interaction with the factory is prohibited during the time exporting local factory data.  

### Async blueprint loading
When clicking on a blueprint file, it will be loaded asynchronously to prevent UI from freezing.   
The loading time is proportional to the size of the blueprint. After the loading is complete, the blueprint can be used by double-clicking.  

## Configuration

Run the game one time to generate `com.starfi5h.plugin.BulletTime.cfg` file.  
Key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html).   

- `EnableBackgroundAutosave`  
Run autosave in background. Besides config file, it can toggle in stat - performance panel. (Default:`false`)  

- `KeyAutosave`  
Keyboard shortcut for auto-save. (Default:`F10 + LeftShift`)  

- `KeyPause`  
Hotkey for toggling pause. (Default:`Pause｜Break`)  

- `StartingSpeed`  
Game speed when the game begin. range:0-100  (Default:`100`)  

- `EnableFastLoading`  
Increase main menu loading speed. (Default:`true`)  

- `RemoveGC`  
Remove force garbage collection of build tools. (Default:`true`)  

- `UIBlueprintAsync`  
Optimize blueprint UI to reduce freezing time. (Default:`false`)  

- `EnableFastLoading`  
Minimum UPS in client of multiplayer game. (Default:`50.0`)  


## Compatibility

- [✅] GalacticScale  
- [✅] NebulaMultiplayer  

### Extra Functions in Nebula Multiplayer  

- When a player is joining or requesting factory data, unfreeze and enter pasue mode (the player can move).  
- When host is saving or manually entering pause mode, the client will enter pause mode too.  
- If clients disconnect during pausing, the host can manually resume the game by changing the slider.  
- Players can stop/resume dyson sphere rotation in the editor at top-left button.  


----

![UIBlueprintAsync](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo2.gif)  

## 调整游戏速度

在性能测试面板可以调整游戏速度(0~100%)，只影响机甲以外的世界，机甲仍保持正常速度。  
速度为0或启用热键时进入时停模式，gameTick会在离开时停模式后恢复。  

## 背景自动保存

在背景自动保存的期间，游戏会进入时停模式。写入当地工厂時，玩家和唯读的工厂互动会被阻止。  
使用这项功能时建议先测试。

## 蓝图异步载入

当单击蓝图档案时, 会采用非同步载入的方式避免介面卡顿。  
载入的时间和蓝图的大小成正比。在载入完成后双击才能使用蓝图。  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.BulletTime` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.BulletTime.cfg`文件  

- `EnableBackgroundAutosave`  
在背景执行自动保存。除了配置文件之外，它还可以在统计-性能测试面板中切换。 (默认为关闭`false`)  

- `KeyAutosave`  
自动存档的热键组合 (默认为`F10 + LeftShift`)  

- `KeyPause`
时停模式的热键 (默认为`Pause｜Break`)  
  
- `StartingSpeed`   
开始时的游戏速度，范围: 0-100 (默认为`100`)  

- `EnableFastLoading`  
加快载入主选单 (默认为开启`true`)  

- `RemoveGC`  
移除建筑工具的强制内存回收 (默认为开启`true`)  
[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)：如果启用了`RemoveGC`, 不要在游戏中切换CheatEnabler的“建造秒完成”和“建筑师模式”功能, 请于游戏外部修改配置文件。  

- `UIBlueprintAsync`  
使蓝图非同步载入,减少卡顿时间 (默认为关闭`false`)  

- `MinimumUPS`  
联机mod-客户端的最小逻辑帧 (默认为`50.0`)  


## 联机功能  

- 当玩家加入、请求工厂数据、存檔時，进入时停模式。在载入完成后恢复。  
- 如果游戏卡在时停模式，主机可以通过拖动滑块来恢复游戏运行。 
- 玩家可以在编辑器左上角的按钮中停止/恢复戴森球旋转。  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
<a href="https://www.flaticon.com/free-icons/pause-button" title="pause-button icons">Pause-button icons created by Uniconlabs - Flaticon</a>