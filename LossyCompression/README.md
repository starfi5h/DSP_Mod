# Lossy Compression  

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/LossyCompression/img/demo1.png)  
Compress belt & cargo data(-75%), dyson shells(-90%) and solar sails to reduce save size.  
There is a toggle button in performance test panel to activate/deactivate compression.  
Suggest to backup your saves before using this mod.   

## Configuration

Run the game one time to generate `starfi5h.plugin.LossyCompression.cfg` file.  

- `CargoPath` (Default: `false`)  
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

- `LazyLoad` (Default:`false`)  
Delay generation of shell model until needed (veiwing, vanilla exporting).  
Can reduce RAM usage by disabling layer display in Dyson editor panel.  

- `ReduceRAM` (Default:`false`)  
Further reduce RAM usage when lazy load is enabled by deleting shell model data after vanilla save export.  
Recommend to enable it when there are too many shells in a save.  

## Compatibility  

- (✅) [CompressSave](https://dsp.thunderstore.io/package/soarqin/CompressSave/)  
- (✅) [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)  
- (✅) [NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)  

----
# 有损/无损压缩

提供传送带(-75%), 戴森壳(-90%)及太阳帆的存档数据压缩。  
在性能测试面板可以启用/停用压缩。  
使用前建议先备份存档。游戏改版之前也建议另存原档避免mod失效。  

## 設置
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.LossyCompression` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.LossyCompression.cfg`文件  
  
- `CargoPath` (默认停用 `false`)  
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

- `LazyLoad` (默认停用 `false`)  
延迟载入戴森壳的模型，只有在需要时(看到模型, 写入原生存档)才会产生模型。  
因此只要在戴森球面板隐藏壳层显示就不会载入，可以减少内存的使用量。  

- `ReduceRAM` (默认停用 `false`)  
当启用延迟加载时，在写入原生存档后删除戴森球模型数据。  
可以进一步减少内存使用量。  

## MOD相容性  

- (✅) [CompressSave](https://dsp.thunderstore.io/package/soarqin/CompressSave/)(LZ4压缩)  
- (✅) [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)(优化mod)  
- (✅) [NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)(联机mod)

----

## Changelog

#### v0.2.3  
\- Add `ReduceRAM` config to further reduce RAM usage.  
\- Enable LazyLoad function in vanilla save.  

#### v0.2.2
\- Fix error in multiplayer shell decompression.  

#### v0.2.1
\- Fix error when using LazyLoad with DSPOptimizations.  
\- Fix crash when using BulletTime background save.  

#### v0.2.0  
\- Add `LazyLoad` function to reduce RAM usage.  
\- Add multithreading for swarm generation.  
\- Fix dyson swarm compression for multiplayer.  

#### v0.1.1
\- Change to released version. (DSP 0.9.27.14553)  

#### v0.1.0  
\- (DEBUG VERSION)  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  