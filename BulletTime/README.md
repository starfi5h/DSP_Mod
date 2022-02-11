# Bullet Time

![Paue Mode](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/BulletTime/img/demo1.gif)  
Reduce simulation rate to imporve FPS. Let player can move during auto-save.  
调整模拟頻率以提高幀數。在自动保存过程中让玩家可以移动。  

## Configuration

Run the game one time to generate `.cfg` file. Key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html). 

- `EnableBackgroundAutosave`  
Do auto-save in background. (Default:`false`)  
The game will enter read-only mode during background auto-save, game tick and factories will stop. Player can still move and inspect stat, but can't interaction with factory.    
在背景执行自动保存。 (预设为关闭)  
在背景自动保存的期间，游戏会暂停并进入唯读模式，玩家无法和唯读的工厂互动。  


- `KeyAutosave`  
Hotkey for auto-save. (Default:`F10`)  
自动存档的热键(预设为F10)  


----

## Changelog

#### v1.0.0  
\- Initial release. (Game Version 0.9.24.11286)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  