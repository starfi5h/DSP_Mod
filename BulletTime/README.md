# Bullet Time

![Paue Mode](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/BulletTime/img/demo1.gif)  

Let user slow down game speed to improve FPS.  Run autosave in the background.  
调整物理帧數以提高渲染帧數。在背景执行自动保存。

## Feature

### Game speed adjustment  
Slow game speed down to lower CPU usage. Improve FPS in late game.  
The speed adjustment only affects factories, the mecha will still in normal speed.  
The control slider is on in-game statistic performance panel.  
When speed is set to 0, the game will enter pause mode, game tick and factories will stop.  
Warning: Moving too fast in slow motion world may trigger movement abnormal check.  
  

### Background autosave  
Run autosave on another thread so the game won't freeze.  
To make sure factory data is consistent, the game will enter pause mode during autosave.  
The interaction with factory is restricted during the time exporting local factory data.  


## Configuration

Run the game one time to generate `com.starfi5h.plugin.BulletTime.cfg` file.  
Key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html).   

- `EnableBackgroundAutosave`  
Run autosave in background. (Default:`false`)  
Test first before enabling this function.  
在背景执行自动保存。 (预设为关闭)  
在背景自动保存的期间，游戏会暂停。写入所在工厂時，玩家无法和唯读的工厂互动。  
使用这项功能时建议先测试一次。  


- `KeyAutosave`  
Hotkey for auto-save. (Default:`F10`)  
自动存档的热键(预设为F10)  
  

- `StartingSpeed` (Default:`100`)  
Game speed when the game begin. range:0-100  
开始时的游戏速度 范围: 0-100  

## Compatibility

- [x] CompressSave  
- [x] GalacticScale  
- [ ] NebulaMultiplayer: Using pasue mode will casue desync.  

----

## Changelog

#### v1.1.0
\- Add StartingSpeed config option.  
\- Only block interaction during exporting local factory.  

#### v1.0.2  
\- Initial release. (Game Version 0.9.24.11286)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  