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
Mirror symmetry: When it is switch on (hotkey `m`), It will create corresponding reflections on the other side of the equator.  
Rotational symmetry: Numpad `[+]`/`[-]` to increase/decrease the number of brushes on the same latitude, ranging from 1 to 120.  

### Delete Layer
You can now dismantle the whole constructed layer with the delete button.  
Deleting Layer 1 will deconstruct all the objects on the layer, leaving only an empty layer.  

### Copy Layer
Copy a single layer to another empty layer. Hotkey `Page Up` for copying and `Page Down` for pasting.  

### Hide Layer
Toggle by "Show All Layer" button. When it is unchecked, only selected layer will be visible, other layers will be hidden.  
You can also use hotkey `h` to hide dyson swarm and the star.  
When `EnableHideOutside` is enabled, those objects will temporarily retain their visibility settings and show the changes in-game until reopening dyson editor or viewing another dyson sphere.  
When mask is enabled in the GUI window, a black mask will appear and block the inside part of the Dyson sphere.

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `SphereEditorTools.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate .cfg file. Restart the game to make the changes take effect.  
Manual download .cfg file loaction: `BepInEx\config\com.starfi5h.plugin.SphereEditorTools.cfg`  
If you're using [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), go to Config editor to change the cfg file.  

### General  
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableDeleteLayer`      | true  | Enable deletion of a constructed layer. |
| `EnableToolboxHotkey`    | true  | Switch between build plan tools with hotkeys. |
| `EnableHideLayer`        | true  | Hide unselected layers when not showing all layers. |
| `EnableHideOutside`      | false | Apply visibility changes to the game world temporarily. |
| `EnableSymmetryTool`      | true | Enable mirror and rotation symmetry of building tools. |

### GUI
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableGUI`      | true  | Show a simple window to use the tools. |
| `WindowPosition` | 300, 250 | Position of the window. Format: x,y |

### Hotkeys  
|| Default | Description |
| :-------------- | :------ | :------------ |
| Toolbox |
| `KeySelect`     | 1       | Inspect |
| `KeyNode`       | 2       | Build Node |
| `KeyFrameGeo`   | 3       | Build Frame(Geodesic) |
| `KeyFrameEuler` | 4       | Build Frame(Euler) |
| `KeyShell`      | 5       | Build Shell |
| `KeyRemove`     | x       | Remove |
| `KeyGrid`       | r       | Toggle Grid |
| Visibility |
| `KeyShowAllLayers` | `    | Toggle show all layers mode |
| `KeyHideMode`      | h    | Toggle swarm & star hide mode |
| Symmetry Tool |
| `KeySymmetryTool`  | tab  | Toggle symmetry tool |
| `KeyMirroring`     | m    | Toggle mirroring |
| `KeyRotationInc`   | [+]  | Increase the degree of rotational symmetry |
| `KeyRotationDec`   | [-]  | Decrease the degree of rotational symmetry |
| Copy & paste |
| `KeyLayerCopy`   | page up  | Copy the selected layer |
| `KeyLayerPaste`   | page down  | Paste to the selected layer |  
  
PS. The new official Dyson sphere edtior looks pretty awsome!  
  
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


### 删除层级
已建立层级可以用删除按钮一键拆除。删除层级1只会将该层上的所有物件拆除，层级仍会保留。  

### 復制层级
將單個层级複製到另一個空的层级。 熱鍵`Page Up`復制，`Page Down`粘貼。  

### 隐藏层级
通过"显示所有层"按钮进行切换。按钮没有勾选时，只有选取的层是可见的，其他层将被隐藏。  
热键`h`可以隐藏太阳帆与恒星。  
`EnableHideOutside`启用时，这些物件会暂时保留隐藏的设定直到再次打开编辑器页面或查看另一个戴森球。  
在操作窗口中啟用遮罩後，會出現一個黑色遮罩並擋住戴森球體的背部部分。

## 安装
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`SphereEditorTools.dll`放入`BepInEx/plugins`文件夹。


## 设置

.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SphereEditorTools` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.SphereEditorTools.cfg`文件 

### 功能选项  
|| 默认 | 描述|
| :----- | :------ | :---------- |
| `EnableDeleteLayer`      | true  | 启用已建立层级删除功能 |
| `EnableToolboxHotkey`    | true  | 启用工具箱热键 |
| `EnableHideLayer`        | true  | 启用层级隐藏功能 |
| `EnableHideOutside`      | false | 使隐藏效果暂时套用至外界 |
| `EnableSymmetryTool`     | true  | 启用对称建造工具(镜像/旋转) |
  
### GUI
|| Default | Description |
| :----- | :------ | :---------- |
| `EnableGUI`      | true  | 启用图形操作窗口 |
| `WindowPosition` | 300, 250 | 窗口的位置 格式: x,y |
	
### 热键  
|| 默认 | 描述 |
| :-------------- | :------ | :------------ |
| 工具箱 |
| `KeySelect`     | 1       | 查看 |
| `KeyNode`       | 2       | 修建节点 |
| `KeyFrameGeo`   | 3       | 修建测地线框架 |
| `KeyFrameEuler` | 4       | 修建经纬度框架 |
| `KeyShell`      | 5       | 修建壳 |
| `KeyRemove`     | x       | 移除 |
| `KeyGrid`       | r       | 切换网格 |
| 可见度 |
| `KeyShowAllLayers` | `    | 切换显示所有层 |
| `KeyHideMode`      | h    | 切换太阳帆与恒星隐藏模式 |
| 对称工具 |
| `KeySymmetryTool`  | tab  | 开关对称建造工具 |
| `KeyMirroring`     | m    | 开关镜像对称 |
| `KeyRotationInc`   | [+]  | 增加旋转对称的个数 |
| `KeyRotationDec`   | [-]  | 减少旋转对称的个数 |
| 复制粘贴 |
| `KeyLayerCopy`   | page up  | 复制选定的层级 |
| `KeyLayerPaste`   | page down  | 粘贴到选定的层级 |  


PS. 新的官方编辑器看起来很赞，这个MOD可以光荣退役了 :D  

----

## Changelog

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
