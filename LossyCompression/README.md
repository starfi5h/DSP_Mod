# Lossy Compression  

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/LossyCompression/img/demo1.png)  
Compress belt & cargo data(-75%), dyson shells(-90%) and solar sails to reduce save size.  
There is a toggle button in performance test panel to activate/deactivate compression.  
Suggest to backup your saves before using this mod.   

## Configuration

Run the game one time to generate `starfi5h.plugin.LossyCompression.cfg` file.  

- `CargoPath` (Default: `fasle`)  
Lossy compress for belts & cargo data. Position and orientation will be slightly distorted.  
The compressed data is stored in game save so the new save can only be opened with this mod.   

- `DysonShell` (Default:`true`)  
Lossless compress for dyson shells. It will take some extra time to rebuild models during loading.  
The compressed data is stored in separated file `.moddsv`.  
The new save can load without this mod or the file, it will appear like there is no shells.  

- `DysonSwarm` (Default:`true`)  
Lossy compress for solar sails. Sail life and position will be distorted, absorbing sails will reset.   
The compressed data is stored in separated file `.moddsv`.  
The new save can load without this mod or the file, it will appear like there is no sails.  

## Compatibility  

- (✅) CompressSave  
- (✅) DSPOptimizations    
- (✅) NebulaMultiplayer - Note: Sails compression is disabled in MP.  
----
# 有损/无损压缩

提供传送带(-75%), 戴森壳(-90%)及太阳帆的存档数据压缩。  
在性能测试面板可以启用/停用压缩。  
使用前建议先备份存档。游戏改版之前也建议另存原档避免mod失效。  

## 設置
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.LossyCompression` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.LossyCompression.cfg`文件  
  
- `CargoPath` (默认停用 `fasle`)  
有损式压缩传送带数据。位置和方向会些微失真。   
启用后新的存档必须有mod才能开启。   

- `DysonShell` (默认启用 `true`)  
无损式压缩戴森壳数据。代价是载入时需要重建模型。  
压缩的数据会另外存在`.moddsv`档案中。  
在没有mod或档案时存档依然可以开启, 只是会丢失所有的壳。  

- `DysonSwarm` (默认启用 `true`)  
有损式压缩太阳帆数据。大致的寿命分布会保存，位置会失真，吸收中太阳帆会重置。  
压缩的数据会另外存在`.moddsv`档案中。  
在没有mod或档案时存档依然可以开启, 只是会丢失所有的太阳帆。  


## MOD相容性  

- (✅) CompressSave(LZ4压缩)  
- (✅) DSPOptimizations(优化mod)  
- (✅) NebulaMultiplayer(联机mod) - 在多人模式时将停用太阳帆压缩  

----

## Changelog

#### v0.1.1
\- Change to released version. (0.9.27.14553)  

#### v0.1.0  
\- (DEBUG VERSION)  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  