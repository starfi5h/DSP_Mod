# SphereEditorTools

![Symmetric Building Tool](https://github.com/starfi5h/DSP_Mod/blob/master/SphereEditorTools/img/demo1.gif?raw=true)
![Copy and hide layers with GUI](https://github.com/starfi5h/DSP_Mod/blob/master/SphereEditorTools/img/demo2.gif?raw=true)

A QoL mod aims to improve dyson sphere editing experience.  

## Feature  

### Custom Hotkeys
When the building plan toolbox is displayed, you can use the hotkeys to switch between different tools.  
All hotkeys used in this mod are customizable in configuration, key name can be found in [Unity manual - InputManager](https://docs.unity3d.com/Manual/class-InputManager.html).  

### Symmetric Building Tool
You can now build or remove multiple entities at once!  
Symmetric tool can be toggle with `tab`.  There are two types of symmetry in the tool:   
Mirror symmetry: When it is activated (hotkey `m`), it will create corresponding reflections on the other side of the equator.  
Rotational symmetry: Numpad `[+]`/`[-]` to increase/decrease the number of brushes on the same latitude, ranging from 1 to 90.  

### Orbit Modification Tool
Anchor Mode: When enabled, the selected layer rotation will stop, the grid orientation will be changed immediately when the orbit is modified and the position of structures will remain unchanged. It can fix the grid misalignment problem.  
Angular speed: Change the angular speed of selected layer. Input empty string will reset the orbit angular speed.

### Hide Object
When mask is enabled in the GUI window, a black mask will appear in the background.  
You can also use hotkey `h` to hide dyson swarm and the star.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `SphereEditorTools.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate .cfg file. Restart the game to make the changes take effect.  
Manual download .cfg file loaction: `BepInEx\config\com.starfi5h.plugin.SphereEditorTools.cfg`  
If you're using [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), go to Config editor to change the cfg file.  

### General  
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableToolboxHotkey`    | true  | Switch between build plan tools with hotkeys. |
| `EnableDisplayOptions`   | true  | Enable display control of star and black mask. |
| `EnableSymmetryTool`     | true  | Enable mirror and rotation symmetry of building tools. |
| `EnableToolboxHotkey`    | true  | Enable dyson sphere layer orbit modifiy tool. |
| `EnableNonemptyList`     | true  | Dropdown list only shows dysonspheres that are not empty. |

### GUI
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableGUI`      | true  | Display a toolbox window. |
| `WindowPosition` | 300, 250 | Position of the window. Format: x,y |

### Hotkeys  
|| Default | Description |
| :-------------- | :------ | :------------ |
| Toolbox |
| `KeySelect`     | space   | Inspect |
| `KeyNode`       | q       | Build Node |
| `KeyFrameGeo`   | w       | Build Frame(Geodesic) |
| `KeyFrameEuler` | e       | Build Frame(Euler) |
| `KeyShell`      | r       | Build Shell |
| `KeyGrid`       | g       | Toggle Grid |
| Visibility |
| `KeyShowAllLayers` | `    | Toggle show all layers mode |
| `KeyHideMode`      | h    | Toggle mask & star hide mode |
| Symmetry Tool |
| `KeySymmetryTool`  | tab  | Toggle symmetry tool |
| `KeyMirroring`     | m    | Toggle mirroring mode |
| `KeyRotationInc`   | [+]  | Increase the degree of rotational symmetry |
| `KeyRotationDec`   | [-]  | Decrease the degree of rotational symmetry |
| Copy & paste |
| `KeyLayerCopy`   | page up  | Copy the selected layer |
| `KeyLayerPaste`   | page down  | Paste to the selected layer |  
  
  
----
# 画球辅助工具

增加戴森球编辑器的功能，改善画球的游戏体验。  

## 功能  

### 自定义热键
当工具箱显示时，可以用热键来切换不同的建造工具。其他功能也可以用热键操控。  
这个MOD用到的热键都可以在设置中自定义，键名可以在[Unity手册 - Input Manager](https://docs.unity3d.com/cn/2021.1/Manual/class-InputManager.html)中找到。


### 对称建设工具  
启用后，可以用热键`tab`開關，开启时能同时用多个笔刷建造或拆除。  
镜像对称以赤道面對稱，开启时(`m`)会在赤道另一侧半球产生对应的笔刷。  
旋转对称以自转轴对称，数值表示在同一个纬线上有多少个笔刷，范围在1-120(数字键盘`[+][-]`增减)。  

### 轨道修改工具  
锚定节点：启用后，所选层停止旋转，修改轨道时将会立即改变网格位置，节点位置保持不变。可以修复网格错位问题。  
角速度：改变所选层的角速度。输入空字符串将重置轨道的角速度。  

### 隐藏物体  
在操作窗口中启用遮罩后，会出现一个黑色遮罩并挡住戴森球体的背部部分。  
热键`h`可以切换恒星和遮罩的显示状态。  


## 安装
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`SphereEditorTools.dll`放入`BepInEx/plugins`文件夹。


## 设置

.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SphereEditorTools` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.SphereEditorTools.cfg`文件 

### 功能选项  
|| 默认 | 描述|
| :----- | :------ | :---------- |
| `EnableToolboxHotkey`    | true  | 启用工具箱热键 |
| `EnableDisplayOptions`   | true  | 启用显示控制(恒星/黑色遮罩) |
| `EnableSymmetryTool`     | true  | 启用对称建造工具(镜像/旋转) |
| `EnableOrbitTool`        | true  | 启用壳层轨道工具 |
| `EnableNonemptyList`     | true  | 下拉列表中仅显示非空的戴森球 |
  
### GUI
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableGUI`      | true  | 启用图形操作窗口 |
| `WindowPosition` | 300, 250 | 窗口的位置 格式: x,y |
	
### 热键  
|| 默认 | 描述 |
| :-------------- | :------ | :------------ |
| 工具箱 |
| `KeySelect`     | space   | 查看 |
| `KeyNode`       | q       | 修建节点 |
| `KeyFrameGeo`   | w       | 修建测地线框架 |
| `KeyFrameEuler` | e       | 修建经纬度框架 |
| `KeyShell`      | r       | 修建壳 |
| `KeyGrid`       | g       | 切换网格 |
| 可见度 |
| `KeyHideMode`      | h    | 切换遮罩與恒星顯示模式 |
| 对称工具 |
| `KeySymmetryTool`  | tab  | 开关对称建造工具 |
| `KeyMirroring`     | m    | 切换镜像对称模式 |
| `KeyRotationInc`   | [+]  | 增加旋转对称的个数 |
| `KeyRotationDec`   | [-]  | 减少旋转对称的个数 |

----

## Changelog


### v2.1.0
\- Add EnableNonemptyList option.  
\- Change default hotkeys so they won't interfere number input.  
\- Selecting in symmetry tool now act like pressing LeftCtrl.  

### v2.0.0 - Adjustment to new editor in 0.9.24  
\- Remove delete, hide and copy layer functions.  
\- Add orbit modifiaction tool.  


### v1.2.1
\- Improve some wording. (Game version 0.8.23.9989)  
\- Change the maximum degree of rotational symmetry from 60 to 120  
\- Change "<<" and ">>" GUI buttons to decrease/increase degree by 10  
\- Fix an error caused by deleting a layer when there is a node under the cursor  


#### v1.2.0
\- Add a small GUI window. (Game version 0.8.20.8092)   
\- Add mask, single layer copy & paste function  
\- Mirror mode has 3 options now: None, Equatorial, Antipodal  
\- Fix flicker issue of symmetric tool  
\- Fix node brushes building condition check  

#### v1.1.0
\- Add symmetric tool. (Game version 0.7.18.7189)  

#### v1.0.0  
\- Initial Release. (Game version 0.7.18.7103)  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
