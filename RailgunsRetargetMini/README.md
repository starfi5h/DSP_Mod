# RailgunsRetargetMini

This is a simplified version of [RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/) as temporary replacement.  
This mod will try to change targeting orbit of EM-Rail ejectors when the current one is not reachable.  
Testing orbit is quite computationally expensive, so this is done every `120` frames and only if ejector: 1. Has orbit set 2. Has bullets    

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `RailgunsRetargetMini.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  

- `CheckPeriod` - Check reachable orbits every x ticks.  (Default:`120`)  

----

# 电磁炮自动换轨

这个mod是[RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/)的暂时替代品。  
每`120`个逻辑帧，会检查已设置轨道且有子弹的电磁弹射器。如果当前轨道无法发射，会尝试寻找可用的轨道并切换。  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.RailgunsRetargetMini` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg`文件  

- `CheckPeriod` - 每x祯检查可用轨道一次 (默认:`120`)  

----

## Changelog

#### v1.0.0
\- Initial release. (Game version 0.9.26.12913)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  