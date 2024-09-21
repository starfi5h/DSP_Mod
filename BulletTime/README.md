# Bullet Time

Decouple the mecha (player actions) from the world simulation.  
1. Provide speed control buttons at bottom right corner to set game speed from 0x to 4x.  
2. Provide hotkey to pause the game and view the freeze in-game world.  
3. Let user slow down world speed to reduce CPU workload, so FPS may improve if it is slowed down by UPS.  
4. Run autosave in the background to make the game stay responsive.  
5. Skip planet modeling for cover to speed up main menu loading.   

## Feature

### Speed control buttons
![Speed control buttons](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/speedbuttons.png)  
- Pause: Toggle tactical pause mode.
- Resume: Reset game speed back to 1x.
- Speed Up: Increase game speed (max 10x).  
The speed adjustment is done by setting the target UPS goal. Hardware still limits the real game speed.  
You can shift + f12 to view the real FPS/UPS at the top-left. The default 1x speed UPS is 60 tick/s.  

### Tactical pause mode
![Tactical pause](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo3.gif)  
When pressing `KeyPause` key, the game will enter tactical pause mode (true pause). The following actions are allowed:
- Freely move camera and inspect in-game objects.  
- Queue up mecha RTS order and mecha replicator.  
- Change settings of buildings.  
- Place down blueprints.  

Mecha activity is disabled in this true pause mode. To enable, set `EnableMechaFunc` to true.  

### World speed adjustment  
![World speed adjustment](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo1.gif)  
Reduce game speed to lower the calculation workload and improve FPS in the late game.  
This adjustment affects everything except the mecha, which will continue at normal speed.  
The control slider is available in the in-game performance statistics panel.  
When speed is set to 0, the game will enter pause mode, and game ticks and factories will stop.
  
### Background autosave  
Run autosave on a background thread to prevent game freezes.  
The game will enter pause mode during autosave to ensure factory data consistency.  
Interaction with the factory is prohibited during local factory data export.

## Configuration

Run the game one time to generate `com.starfi5h.plugin.BulletTime.cfg` file.  
Key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html).   

- `KeyAutosave`  
Keyboard shortcut for auto-save. (Default:`F10 + LeftShift`)  

- `KeyPause`  
Hotkey for toggling pause mode. (Default:`Pause｜Break`)  

- `EnableMechaFunc`  
When enabled, mecha will be able to move in pause mode and projectiles will fly at normal speed. (Default:`fasle`)    

- `EnableBackgroundAutosave`  
Run autosave in the background. This can also be toggled in the performance test panel. (Default:`false`)  

- `StartingSpeed`  
Game speed when the game begins. range:0-100  (Default:`100`)  

- `EnableFastLoading`  
Increase main menu loading speed. (Default:`true`)  

- `RemoveGC`  
Remove force garbage collection of build tools. (Default:`true`)  

- `MaxSpeedupScale`
Maximum game speed multiplier for speedup button. (Default:`10`)  

## Compatibility

- [✅] GalacticScale  
- [✅] NebulaMultiplayer  

### Extra Functions in Nebula Multiplayer  

- When a player is joining or requesting factory data, unfreeze and enter pasue mode (the player can move).  
- When host is saving or manually entering pause mode, the client will enter pause mode too.  
- If clients disconnect during pausing, the host can manually resume the game by changing the slider.  
- Players can stop/resume dyson sphere rotation in the editor at top-left button.  
- Both host and client can stop the game using hotkey pause. Speed up is only available to host.   


----

# BulletTime 子弹时间-游戏速度控制mod

将机甲与世界的更新逻辑解离，使两者可以以不同的时间流速运行

## 功能

### 游戏速度倍率调整
![Speed control buttons](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/speedbuttons.png)  
- 暂停：切换战术暂停模式。
- 恢复：将游戏速度重置为 1 倍。
- 加速：提高游戏速度（最高 10 倍）。  
速度调整是通过设置目标 UPS 来完成的。实际上的游戏速度由硬体性能决定。  
您可以按 Shift + f12 在左上角查看实际 FPS/UPS。默认1倍速 UPS 为 60 tick/s。  


### 战术暂停

按下`KeyPause`键时，游戏将进入战术暂停模式。允许以下操作：
- 自由移动摄像头并检查游戏内物体。
- 队列机甲 RTS 动作指令和手搓。
- 更改建筑物设置。
- 放置蓝图。

在战术暂停模式下，机甲移动以及动作将被禁用。要启用，请将`EnableMechaFunc`设置为 true。

### 调整世界速度

在性能测试面板可以调整世界速度(0~100%)，只影响机甲以外的世界，机甲仍保持正常速度。  
速度为0或启用热键时进入时停模式，gameTick会在离开时停模式后恢复。  

### 後台自动保存

在後台自动保存的期间，游戏会进入时停模式。写入当地工厂時，玩家和唯读的工厂互动会被阻止。  
使用这项功能时建议先测试。

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.BulletTime` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.BulletTime.cfg`文件  

- `KeyAutosave`  
自动存档的热键组合 (默认为`F10 + LeftShift`)  

- `KeyPause`  
暂停模式(世界停止+画面提示)的热键 (默认为`Pause｜Break`)  
  
- `EnableMechaFunc`  
启用后，机甲能够在暂停模式中活动。弹射物将以正常速度飞行。 (默认为关闭`false`)    

- `EnableBackgroundAutosave`  
在背景执行自动保存。可以在统计-性能测试面板中切换。 (默认为关闭`false`)  

- `StartingSpeed`   
开始时的游戏速度，范围: 0-100 (默认为`100`)  

- `EnableFastLoading`  
加快载入主选单 (默认为开启`true`)  

- `RemoveGC`  
移除建筑工具的强制内存回收 (默认为开启`true`)  

- `MaxSpeedupScale`
加速按钮的最大游戏速度倍率 (默认为`10`)  

## 联机功能  

- 当玩家加入、请求工厂数据、存檔時，进入时停模式。在载入完成后恢复。  
- 如果游戏卡在时停模式，主机可以通过拖动滑块来恢复游戏运行。 
- 玩家可以在编辑器左上角的按钮中停止/恢复戴森球旋转。  
- 加速只有主机可以使用。暂停/恢复的功能主机和客机都可以使用。  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
<a href="https://www.flaticon.com/free-icons/pause-button" title="pause-button icons">Pause-button icons created by Uniconlabs - Flaticon</a>  
Speed button UI desgin from [DspGameSpeed](https://thunderstore.io/c/dyson-sphere-program/p/dsp-mods/DspGameSpeed/)  