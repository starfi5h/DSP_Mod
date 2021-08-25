# DysonOrbitModifier

![Demo 1 sync v.s. not sync ](https://github.com/starfi5h/DSP_Mod/raw/master/DysonOrbitModifier/img/demo1.gif?raw=true)  
![Demo 2 visual effect](https://github.com/starfi5h/DSP_Mod/raw/master/DysonOrbitModifier/img/demo2.gif?raw=true) `chainedRotation` = true  

This mod lets you view and change parameters of a created orbit without deleting it.  
You can also edit settings in configuration to make a bigger dyson sphere.  
**Changing too much may prevent uploading to Milky Way.**  
Save the game before editing orbits in case of a potential crush.  


## Usage


Select the Dyson swarm/layer number and the add button will switch to the edit button.  
Click it to open the modify panel, you can change the radius, angle, rotation speed (layer only) of that orbit.  
If sync is toggled on, the entities in the orbit will move along with the grid.  
If sync is toggled off, the entities will stay at the same position.  
Enter `-1` in the rotation speed input field to restore the default rotation speed for that radius.  
Deselect the orbit number to make the edit button switches back to the add button.  

## Configuration

Run the game one time to generate `.cfg` file. Pause and resume the game to make the changes take effect.


- `modify panel setting`
    - `minRadiusMultiplier` : Multiplier of default minimum radius (Default : `1.0`)
    - `maxRadiusMultiplier` : Multiplier of default maximum radius (Default : `1.0`)
    - `maxAngularSpeed` : Maximum rotation speed (Default : `10.0`)
    - `correctOnChange`: Remove exceeding Structure Point/Cell Point right after entities are moved. (Default : `true`)  
      When changing into a smaller radius, existing SP/CP may exceed their new limits.  
      Disable it will let SP/CP stay at the original value until editor panel is closed.  

- `visual`
    - `chainedRotation` : Let some layers rotate chained together. (Default : `false`)  
    - `chainedSquence`: In each pair, the rotaion of former layer (a) will apply to the latter one (b). (Default : "5-4, 4-3, 3-2, 2-1")  

----
# 修改戴森球轨道

查看/修改现有轨道的参数，例如修改無法被删除的第一轨道。  
也可以调整设置的倍率来建造比原本限制更大的戴森球，但可能有风险。  
此MOD会修改游戏数据，**可能会无法上传银河系**，使用前请先存档以防游戏错误。  

## 使用方法

点选戴森云/球轨道号码，新增按钮会切换为修改按钮。  
按下后会打开修改面板，可修改该轨道的半径、角度、旋转速度(限戴森球层)。  
如果打开同步，轨道中的实体将与网格一起移动。  
如果关闭同步，实体将保持在同一位置。  
在旋转速度的栏位输入`-1`可以回到该半径的预设旋转速度。  
取消选取轨道号码使修改按钮切换回新增按钮。  

## 设置

`.cfg`文件需要先运行过游戏一次才会出现，修改后要暂停并重回游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.DysonOrbitModifier` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.DysonOrbitModifier.cfg`文件 

- `修改面板设置`    
    - `minRadiusMultiplier` : 最小轨道半径的倍率 (Default : `1.0`)  
    - `maxRadiusMultiplier` : 最大轨道半径的倍率 (Default : `1.0`)  
    - `maxAngularSpeed` : 最大旋转速度 (Default : `10.0`)  
    - `correctOnChange` : 移动物体后，立即移除超出的结构点数/细胞点数。(Default : `true`)  
       将半径调小时，每个组件中已有的结构点数(SP)/细胞点数(CP)可能超过它们的上限。  
       禁用此选项将让SP/CP保持在改变之前的数值，直到离开编辑面板。  

- `视觉效果`
    - `chainedRotation` : 让壳层连锁转动。可以用下面的字串指定连锁的顺序。 (Default : `false`)  
    - `chainedSquence`: 在每一对中，前一层的旋转将套用于后一层。(Default : "5-4, 4-3, 3-2, 2-1")     
 

----

## Changelog

#### V1.4.1
\- Fix a string error.

#### V1.4.0
\- Add UI sync toggle to replace config options. Game Version 0.8.20.7996  
\- Finish chained rotation visual effect.  


#### V1.3.0
\- Add syncPosition and syncAltitude configuration options.  
\- Implement hierarchical rotation function.  

#### V1.2.0  
\- Bugfix : CP request will calculate correctly when changing the radius now.  

#### V1.1.0
\- Implement structure moving function.

#### v1.0.0  
\- Initial Release. Game Version 0.7.18

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
Icon from https://freeicons.io/logos/cog-configuration-gear-settings-icon-38149
