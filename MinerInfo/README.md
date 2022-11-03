# MinerInfo

Show current miner output and working ratio when mouse over a miner.  
When the Vein Distribution Details Display is on, add an extra line of text to the Vein Group label with the miners max output rate (either per second or per minute).  
Inspired by brotchie's [MinerInfo](https://dsp.thunderstore.io/package/brotchie/MinerInfo/).  


## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `MinerInfo.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.MinerInfo.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  

|| Default | Description |
| :----- | :------ | :---------- |
| `ShowItemsPerSecond`    | true  | If true, display unit per second. If false, display unit per minute. |
| `ShowVeinMaxMinerOutput`   | true  | Show the maximum output by all miners on a vein. |

----

# 矿机信息

当鼠标悬停在矿机上时，显示当前矿机產出和工作比率。显示一条矿脉上所有采矿机最大产出的总和。  
从brotchie的[MinerInfo](https://dsp.thunderstore.io/package/brotchie/MinerInfo/)启发.  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.MinerInfo` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.MinerInfo.cfg`文件  

|| 默认 | 描述|
| :----- | :------ | :---------- |
| `ShowItemsPerSecond`     | true  | 切换显示单位(true=每秒,false=每分钟) |
| `ShowVeinMaxMinerOutput` | true  | 显示一条矿脉上所有采矿机最大产出的总和。 |

----

## Changelog

v1.0.0 - Initial release. (DSPv0.9.27.15033)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  