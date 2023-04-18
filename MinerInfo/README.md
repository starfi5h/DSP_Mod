# MinerInfo

![Vein Type filter](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/MinerInfo/img/demo1.gif)  
1. Show current miner output and working ratio when mouse over a miner.  
2. Click on a vein icon on planet detial window to fliter vein node display. Click again or exit map view to resume.  
3. When the Vein Distribution Details Display is on, add an extra line of text to the Vein Group label with the miners theoretical max output rate.  
Inspired by brotchie's [MinerInfo](https://dsp.thunderstore.io/package/brotchie/MinerInfo/).  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `MinerInfo.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.MinerInfo.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  

|| Default | Description |
| :----- | :------ | :---------- |
| `ShowItemsPerSecond`    | true  | If true, display unit per second. |
| `ShowItemsPerMinute`    | false  | If true, display unit per minute. |
| `MaxMinerOutput-Enable` | true  | Show the maximum number of items per time period output by all miners on a vein. |
| `MaxMinerOutput-Text`   | Max Output:  | Prefix text show before numbers. |

----

# 矿机信息

1. 当鼠标悬停在矿机上时，显示当前矿机产能和工作比率。  
2. 点击星球信息面板的矿脉图示时, 会单独显示该种矿脉节点的信息。再点击一次即可退出过滤模式。  
3. 显示一条矿脉上所有采矿机最大产能的总和。  
从brotchie的[MinerInfo](https://dsp.thunderstore.io/package/brotchie/MinerInfo/)启发.  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.MinerInfo` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.MinerInfo.cfg`文件  

|| 默认 | 描述|
| :----- | :------ | :---------- |
| `ShowItemsPerSecond`    | true  | 显示每秒产能 |
| `ShowItemsPerMinute`    | false  | 显示每分钟产能 |
| `MaxMinerOutput-Enable` | true  | 显示一条矿脉上所有采矿机最大产能的总和 |
| `MaxMinerOutput-Text`   | Max Output:  | 最大产能字串 |

----

## Changelog

v1.1.0 - Add filter vein type feature. Enable showing both /s and /m in units. (DSPv0.9.27.15466)  
v1.0.0 - Initial release. (DSPv0.9.27.15033)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  