# AlterTickrate

Change buildings update priod from 1 tick to x ticks and scale the progress accordingly to reduce CPU calculation without affecting game pace.  
Warning: The mod is still in development state. Recommend to make a backup save before using it.  
Because this mod change production workflow, if it does not work as intend, it may diable Milkyway upload.  

![compare](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AlterTickrate/doc/compare.png)  
In the test file by default settings, Belt time reduce by 50%, Sorter time reduce by 50%,  Power System reduce by 73%, Various Facility reduce by 80%, Storage by 50%. Overall the UPS is doubled.  
The button on stat - performance test page can switch on/off the mod.  

## How does it work

This mod is inspired by [Global Tick Time Scale](https://mods.factorio.com/mod/GTTS). This mod decouple facilities simulations from gametick so they can update in a lower rate.  
For example, an Mk.II assembler is producing Mk.I proliferator at 120/min speed:  
![demo1](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AlterTickrate/doc/demo1.gif)  
In the vanilla version (left side), the assembler is calculated every tick:  
30 updates per cycle, the progress increases 1/30 each times.  
In the modded version (right side), the assembler is set to calculate once every 5 ticks:  
6 updates per cycle, and the progress increases 5/30 each times.  
As long as the period is multiple of 5, the mod can maintain the same production speed as the vanilla version and reduce the amount of computation by four-fifths.

## Settings

Run the game one time with the mod to generate `starfi5h.plugin.AlterTickrate.cfg` file.  
The update period (update every x ticks) of each group can be configured:     
`FacilityUpdate` - (Default: 5) Recommend to set as factor of 20.  
`BeltUpdate` - (Default: 2) Max limit: 2.  
`StorageUpdate` - (Default: 2)  
`SorterUpdate` - (Default: 2)  

## Mod Compatibility
Compat: NebulaMultiplayerMod, DSPOptimizations  
Incompat: SampleAndHoldSim, Blackbox  
If there are belt speed changing mods (GenesisBook, BetterMachines, BeltSpeedEnhancement), the belt feature will be disabled.  

----

# AlterTickrate

目标: 改变建筑的更新频率, 在不偏离原版计算规则的条件下减少CPU运算量。  
此mod处于早期测试阶段，使用前建议备份存档。另外由于参与了生产过程，想上榜请自行考虑风险。  
在性能测试面板点击AlterTick按钮可以开启或关闭mod。  

## 运作原理

将目标建筑的更新频率从每帧更新1次, 改为每x帧运算1次且每次进度增加为x倍。  
此mod的灵感来自于[Global Tick Time Scale](https://mods.factorio.com/mod/GTTS)。  
![demo1](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AlterTickrate/doc/demo1.gif)  
以图为例, 增产剂I的生产速度为每分钟120= 每秒2 = 每帧1/30  
在原版中, 每1帧运算1次: 每个周期共运算30次, 每次增加1/30进度  
在mod中, 设定每5帧运算1次: 每个周期运算6次, 每次增加5/30进度  
只要确保周期为5的倍速, mod就能保持和原版相同的生产速度, 并将运算量减少五分之四。  

### 与SampleAndHoldSim比较

同样都是试图减少更新频率并维持原速，SampleAndHoldSim是以"倍增产物"作为补偿，而AlterTickrate则是"倍增进度"作为补偿。因此AlterTickRate的近似结果更接近原版，并有以下优缺点:

优点
- (消耗原料,生产货物)的数量对应关系正确, 不会虚空造物/消耗
- 工厂内部的货物数量正常, 改变速度时不会有残值影响
- 短时间内的统计更接近原版结果, 产线不稳定也可以使用

缺点
- 减少运算的效果相对有限
- 部分设施的动画更新频率(分检器)下降
- 在电力不足时, 效率可能会因为取整问题下降
- 可容忍的设施速度有一定上限, 和改变设施速度(例:创世之书)的mod兼容性不佳

## 设置参数

在`starfi5h.plugin.AlterTickrate.cfg`可调整设施更新的周期(每x帧运算1次)。  
安装和配置文件请参考其他mod的说明。

### 电力与生产设施

`FacilityUpdate`: 默认为5。建议设为20(三级制造台增产剂I: 60*0.5/1.5)的因数。  
生产设施补偿: 矿机, 组装机, 弹射器, 发射井进度x倍。   
电力设施补偿: 火力燃料消耗进度x倍, 充/放电电量x倍, 光子进度x倍。  
在空闲的逻辑帧会利用计算好的状态来继续更新设施的动画。  

### 传送带

`BeltUpdate`: 默认为2, 最大值为2。
原版最高速为蓝带(30每秒 = 0.5每帧), 可允许每2帧更新一次。  
为了使用者体验, 在本地星球的传送带仍为每1帧更新1次。  
在更改传送带速度的mod同时存在时, 此项功能将停用。  

### 仓储/入塔/出塔
`StorageUpdate`: 默认为2。建议设为和传送带的参数相同。

### 分检器
`SorterUpdate`: 默认为2。  
超过2时会造成机率性漏接/漏放, 造成混带失效或着满带压缩程度降低。  

## Mod兼容性

相容: 联机mod(NebulaMultiplayerMod), 优化mod(DSPOptimizations)。  
不相容: SampleAndHoldSim, Blackbox。  
GenesisBook, BetterMachines或BeltSpeedEnhancement等修改传送带的mod存在时, 将取消传送带抽帧功能。

----

## Changelog

v0.1.0 - Initial release. (DSP 0.9.27.15466)  