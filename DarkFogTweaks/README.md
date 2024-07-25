# DarkFogTweaks 黑雾数值调整

Provide many multipliers to change enemy unit settings (hp, attack, speed, etc.) and behaviors.  
提供许多乘数来改变敌方单位设置（生命值、攻击力、速度等）和行为。  

----

## Installation 安装

Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `DarkFogTweaks.dll` in `BepInEx/plugins` folder.  
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`DarkFogTweaks.dll`放入`BepInEx/plugins`文件夹。  

## Configuration 配置文件

Run the game one time to generate `BepInEx\config\starfi5h.plugin.DarkFogTweaks.cfg` file.  
If you're using mod manager, you can find the file in its Config editor.  
The changes will take effects after reboost, or go to game settings and click 'Apply' button.  

管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.DarkFogTweaks` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.DarkFogTweaks.cfg`文件  
配置文件(.cfg)需要先运行过游戏一次才会出现。  
在修改完配置文件后重启游戏, 或进入游戏设置, 点击'应用设置'即可立即套用新的数值设定。  


```
## Settings file was created by plugin DarkFogTweaks
## Plugin GUID: starfi5h.plugin.DarkFogTweaks

[Behavior]

## Send as many unit as this value when base assault (max: 180)
# Setting type: Int32
# Default value: 0
Base_AssaultUnitCount = 0

## Send as many unit as this value when hive assault (max: 1440)
# Setting type: Int32
# Default value: 0
Hive_AssaultUnitCount = 0

## Relay will try to land on base first
# Setting type: Boolean
# Default value: false
Relay_FillPitFirst = false

## Relay will try to land on player's local planet first
# Setting type: Boolean
# Default value: false
Relay_ToPlayerPlanetFirst = false

## Multiplier of threat increase when attacking space enemies
# Setting type: Single
# Default value: 1
Space_ThreatFactor = 1

## Extra multiplier to exp gain
# Setting type: Single
# Default value: 1
All_ExpFactor = 1

[Buildings]

## Increase building speed and reduce cost
# Setting type: Int32
# Default value: 1
BuildingSpeedFactor = 1

## vanilla: +180
# Setting type: Int32
# Default value: 0
BaseExtraMatterGen = 0

## vanilla: -5400 (5.4MW)
# Setting type: Int32
# Default value: 0
BaseExtraEnergyGen = 0

## vanilla: +0
# Setting type: Int32
# Default value: 0
HiveExtraMatterGen = 0

## vanilla: +480000 (480MW)
# Setting type: Int32
# Default value: 0
HiveExtraEnergyGen = 0

[EnemyUnitFactor]

## If not null, the modifications will only apply to the whiltelist ids, separated by comma.
## ProtoIds: 300 Raider, 301 Ranger, 302 Guardian, 285 Lancer, 284 Humpback, 283 Eclipse Fortress
# Setting type: String
# Default value: 
WhiteListProtoIds = 

# Setting type: Single
# Default value: 1
HpMax = 1

## Hp upgrade per level
# Setting type: Single
# Default value: 1
HpInc = 1

## Hp recovery overtime
# Setting type: Single
# Default value: 1
HpRecover = 1

## Including base speed, max speed and acceleration
# Setting type: Single
# Default value: 1
MovementSpeed = 1

# Setting type: Single
# Default value: 1
SensorRange = 1

## The distance to engage and keep on firing
# Setting type: Single
# Default value: 1
EngageRange = 1

# Setting type: Single
# Default value: 1
AttackRange = 1

# Setting type: Single
# Default value: 1
AttackDamage = 1

## Damage upgrade per level
# Setting type: Single
# Default value: 1
AttackDamageInc = 1

# Setting type: Single
# Default value: 1
AttackCoolDownSpeed = 1

## Damage cooldown speed upgrade per level
# Setting type: Single
# Default value: 1
AttackCoolDownSpeedInc = 1

```

## Changelog

v0.0.2 - Add config `WhiteListProtoIds`.  
v0.0.1 - Initial release. (DSPv0.10.30.22292)  

----

## Acknowledgements

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  