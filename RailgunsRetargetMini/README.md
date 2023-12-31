# RailgunsRetargetMini

This is an updated version of [RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/).   

# Retarget orbit
![Ejector UI](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/RailgunsRetargetMini/doc/UI.jpg)  
This mod will automatically try to change the targeting orbit of EM-Rail ejectors when the current one is not reachable.  
If Force Retarget is set to true, all ejectors will try to retarget regardless if the orbit is set or not.  
If Force Retarget is set to false, only the ejectors that have configured the orbit will try to retarget.  

# Remote orbit control
![Remote set orbit](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/RailgunsRetargetMini/doc/demo1.gif)  
In dyson editor, select an orbit and click on the icon. A pop-up will show up and display the status of ejectors on the system.  
When clicking "Set All", all ejectors on the system will set the target orbit to the selected orbit.  
When clicking "Unset All", all ejectors on the system will set the target orbit to none and stop firing.  


## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `RailgunsRetargetMini.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  

- `ForceRetargeting` - Retarget orbit for unset ejectors. (Default:`true`)  
- `Method` - Which retarget algorithm to use. Method 1 is more UPS friendly but ejectors will swing at night. To disable auto retarget, set this value other than 1 or 2. (Default:`1`)  
- `RotatePeriod` - (Method1) Rotate to next enabled orbit every x ticks when unreachable. (Default:`60`)  
- `CheckPeriod` - (Method2) Check reachable orbits every x ticks.  (Default:`120`)  

----

# 电磁炮自动换轨

这个mod由[RailgunsRetargeting](https://dsp.thunderstore.io/package/brokenmass/RailgunsRetargeting/)启发。  
每`60`个逻辑帧，会检查有子弹的电磁弹射器。如果当前轨道无法发射，会尝试寻找可用的轨道并切换。  
  
在戴森球编辑器中，选择一个轨道并点击图标，将弹出窗口显示该星系中所有电磁炮的状态。  
点击"Set All"时，星系中所有的电磁炮会将目标轨道设置为选择的轨道。  
点击"Unset All"时，星系中所有的电磁炮会将目标轨道设置为无并停止发射。  

## 设置   
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.RailgunsRetargetMini` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.RailgunsRetargetMini.cfg`文件  

- `ForceRetargeting` - 使未设置的电磁弹射器自动换轨 (默认:`true`)  
- `Method` - 使用哪种算法(1或2)。算法1比较省运算资源, 但是弹射器在晚上会一直切换轨道。设置其他数值以取消自动换轨功能(默认:`1`)
- `RotatePeriod` - (算法1) 无法发射时,每x祯切换至下一个轨道。 (默认:`60`) 
- `CheckPeriod` - (算法2) 无法发射时,每x祯检查所有轨道是否有可发射轨道并切换。(默认:`120`)  

----

## Changelog

#### v1.3.1
\- Fix the TypeLoadException error in ejector window.  

#### v1.3.0 (DSP 0.10.28.21150)
\- Add a button in ejector window to toggle `ForceRetargeting`.

#### v1.2.0 (DSP 0.9.27.15033)
\- Add remote ejectors orbit control.  

#### v1.1.0 (DSP 0.9.26.13034)
\- Add a new method to switch orbit.  
\- Add ForceRetargeting, Method, RotatePeriod config options.  

#### v1.0.0 (DSP 0.9.26.12913)  
\- Initial release. 

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  