# RailgunsRetargetMini

This is a simplified version of [RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/) as temporary replacement.   
This mod will try to change targeting orbit of EM-Rail ejectors when the current one is not reachable.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `RailgunsRetargetMini.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  

- `ForceRetargeting` - Retarget orbit for unset ejctors. (Default:`false`)  
- `Method` - Which retarget algorithm should use. Method 1 is more UPS friendly but ejectors will swing at night. (Default:`1`)  
- `RotatePeriod` - (Method1) Rotate to next enabled orbit every x ticks when unreachable. (Default:`60`)  
- `CheckPeriod` - (Method2) Check reachable orbits every x ticks.  (Default:`120`)  

----

# 电磁炮自动换轨

这个mod是[RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/)的暂时替代品。  
每`60`个逻辑帧，会检查有子弹的电磁弹射器。如果当前轨道无法发射，会尝试寻找可用的轨道并切换。  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.RailgunsRetargetMini` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg`文件  

- `ForceRetargeting` - 使未设置的电磁弹射器自动换轨 (默认:`false`)  
- `Method` - 使用哪种算法(1或2)。算法1比较省运算资源, 但是弹射器在晚上会一直切换轨道。(默认:`1`)
- `RotatePeriod` - (算法1) 无法发射时,每x祯切换至下一个轨道。 (默认:`60`) 
- `CheckPeriod` - (算法2) 无法发射时,每x祯检查所有轨道是否有可发射轨道并切换。(默认:`120`)  

----

## Changelog

#### v1.1.0 (game version 0.9.26.13034)
\- Add a new method to switch orbit.  
\- Add ForceRetargeting, Method, RotatePeriod config options.  

#### v1.0.0 (game version 0.9.26.12913)  
\- Initial release. 

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  