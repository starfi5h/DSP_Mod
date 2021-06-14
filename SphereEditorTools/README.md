# SphereEditorTools

A QoL mod aims to improve dyson sphere editing experience.  

## Feature  

### Custom Hotkeys
When the building plan toolbox is displayed, you can use the hotkeys to switch between different tools.  
Hotkeys is customizable in configuration, key name can be found in [unity manual](https://docs.unity3d.com/Manual/class-InputManager.html).  

### Delete Layer
You can now dismantle the whole constructed layer with delete button.  
Deleting Layer 1 will deconstruct the objects on the layer without removing it.  

### Hide Layer
Toggle by "Show All Layer" button. When it is unchecked, only selected layer will be visible, other layers will be hidden.  
You can also use hotkey `KeyHideMode` (default `h`) to hide dyson swarm and the star.  
If `EnableHideOutside` is enable, those objects will temporarily retain their visibility settings until reopening dyson editor or viewing another dyson sphere.

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
| `EnableHideOutside` | false | Apply visibility changes to the game world temporarily. |
  
### Hotkeys  
|| Default | Description |
| :-------------- | :------ | :------------ |
| Toolbox|
| `KeySelect`     | 1       | Inspect |
| `KeyNode`       | 2       | Build Node |
| `KeyFrameGeo`   | 3       | Build Frame(Geodesic) |
| `KeyFrameEuler` | 4       | Build Frame(Euler) |
| `KeyShell`      | 5       | Build Shell |
| `KeyRemove`     | x       | Remove |
| `KeyGrid`       | r       | Toggle Grid |
|Visibility|
| `KeyShowAllLayers` | `    | Toggle show all layers mode |
| `KeyHideMode`      | h    | Toggle swarm & star hide mode |


## TODO
- Symmetric building tool

----
# 戴森球加強编辑工具

增加戴森球编辑器的功能，改善画球的游戏体验。  

## 功能  

### 自定义热键
当工具箱显示时，可以用热键来切换不同的建造工具。热键可以在设置中自定义，键名可以在[Unity手册](https://docs.unity3d.com/Manual/class-InputManager.html)中找到。

### 删除层级
已建立层级可以用删除按钮一键拆除。删除层级1只会将该层上的所有物件拆除，层级仍会保留。

### 隐藏层级
通过 "显示所有图层 "按钮进行切换。按钮没有勾选时，只有选取的层是可见的，其他层将被隐藏。  
热键`KeyHideMode`(默认`h`)可以隐藏太阳帆与恒星。  
`EnableHideOutside`启用时，这些物件会暂时保留隐藏的设定直到再次打开编辑器页面或查看另一个戴森球。

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
|可见度|
| `KeyShowAllLayers` | `    | 切换显示所有层 |
| `KeyHideMode`      | h    | 切换太阳帆与恒星隐藏模式 |
 
## 未来计画
- 对称建设工具

----

## Changelog

#### v1.0.0  
\- Initial Release. Game version 0.7.18.7103  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
Icon by [Mithun Raj](https://freeicons.io/geometric-ui-icons-2/vector-pen-icon-9873#)  
