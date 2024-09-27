# LogisticsBotsTweaks

Allow players to change parameters of logistics bots and logistics distributors.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `LogisticsBotsTweaks.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.LogisticsBotsTweaks.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  
The changes will take effects after restarting the game, and last in save even when the mod is disabled.  

| | Default | Description |
| :----- | :------ | :---------- |
| `Bot` - `SpeedScale`                  | 1.0 | Scale of logistics bots flight speed. |
| `Bot` - `Capacity`                    | 0   | If > 0, Overwirte carrying capacity of logistics bots. |
| `Bot` - `DeliveryMaxAngle`            | 0   | If > 0, Overwirte distribution range of logistics bots. |
| `Distributor` - `MaxBotCount`         | 10  | Max logistics bots count. |
| `Distributor` - `MaxChargePowerScale` | 1.0 | Scale of max charge power. |

----

# 物流配送器参数调节

修改物流配送机和运输机的参数。  

## 配置   
配置文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。修改后的效果在停用mod后依然会存在。    
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.LogisticsBotsTweaks` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.LogisticsBotsTweaks.cfg`文件  

| | Default | Description |
| :----- | :------ | :---------- |
| `Bot` - `SpeedScale`                  | 1.0 | 物流配送机的飞行速度倍率 |
| `Bot` - `Capacity`                    | 0   | 覆写配送运输机运载量 |
| `Bot` - `DeliveryMaxAngle`            | 0   | 覆写配送范围(度) |
| `Distributor` - `MaxBotCount`         | 10  | 物流配送器的最大运输机数量 |
| `Distributor` - `MaxChargePowerScale` | 1.0 | 物流配送器的最大充电功率倍率 |

----

## Changelog

v1.1.0 - Add config Bot Capacity, DeliveryMaxAngle (DSPv0.10.30.23430)  
v1.0.0 - Initial release. (DSPv0.9.27.15466)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  