# AlterTickrate

Change buildings update priod from 1 tick to x ticks and scale the progress accordingly to reduce CPU calculation without affecting game pace.  

![compare](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AlterTickrate/doc/compare.jpg)  
In the test file, Sorter time reduce by 50%,  Power System reduce by 80%, Various Facility reduce by 70%, Storage reduce by 50%.  
The button on stat - performance test page (AlterTick - ON) can switch on/off the mod.  

Warning: The mod is still in development state. Recommend to make a backup save before using it.  

## How does it work

This mod is inspired by [Global Tick Time Scale](https://mods.factorio.com/mod/GTTS). This mod decouple facilities simulations from gametick so they can update in a lower rate.  
For example, an Mk.II assembler is producing Mk.I proliferator at 120/min speed:  
![demo1](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/AlterTickrate/doc/demo1.gif)  
In the vanilla version (left side), the assembler is calculated every tick:  
30 updates per cycle, the progress increases 1/30 each times.  
In the modded version (right side), the assembler is set to calculate once every 5 ticks:  
6 updates per cycle, and the progress increases 5/30 each times.  
As long as the period is multiple of 5, the mod can maintain the same production speed as the vanilla version and reduce the amount of computation by four-fifths.
  
This mod only changes facilities operating speed, it doesn't generate items from thin air. So it's safer than SampleAndHoldSim. At the same time the performance gain is not huge as SampleAndHoldSim.  

## Settings

Run the game one time with the mod to generate `starfi5h.plugin.AlterTickrate.cfg` file.  
The update period (update every x ticks) of each group can be configured:     

`Facility`-`PowerSystem` (Default: 10) Recommend to set as a factor of 30.  
`Facility`-`Facility` (Default: 10) Recommend to set as a factor of 20.  
`Lab`-`Produce` (Default: 10) Update producing lab every x ticks.  
`Lab`-`Research` (Default: 10) Update researching lab every x ticks.  
`Lab`-`Lift` (Default: 5) Transfer items in lab tower every x ticks.  
`Transport`-`Sorter` (Default: 2) Setting value higher than 2 may cause sorters to miss cargo.  
`Transport`-`Storage` (Default: 2) Recommend to set at the same value of belt.   
`Transport`-`Belt` (Default: 1) Update belt every x ticks.(Max:2) Lower update frequence may break some mixed belt design.  
`UI`-`SmoothProgress` (Default: false) Interpolates progress animation in UI.   

Kown issues:
- Oil Extrator will output oil in stack of 4 regardless of production rate. (Facility)  
- Lab towers sometimes can't run in full speed. (Lab)  

## Mod Compatibility
Compat: [NebulaMultiplayerMod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/), [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)  
Incompat: [SampleAndHoldSim](https://dsp.thunderstore.io/package/starfi5h/SampleAndHoldSim/), [Blackbox](https://dsp.thunderstore.io/package/Raptor/Blackbox/)  
If there are belt speed changing mods (GenesisBook, BetterMachines, BeltSpeedEnhancement), the belt feature will be disabled, `Belt` and `Storage` will be set to 1.  
If [LabOpt](https://dsp.thunderstore.io/package/soarqin/LabOpt/) is present, the lab feature will be disabled. All config values under `Lab` will be set to 1.  
When using mods that add new buildings, it's recommend to use SampleAndHoldSim for better compatibility.  

----

# AlterTickrate 降频mod

目标: 改变建筑的更新频率, 在不偏离原版计算规则的条件下减少CPU运算量。  
此mod尚未成熟，使用前建议备份存档。另外由于参与了生产过程，想上榜请自行考虑风险。  
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
- 部分设施的动画更新频率(传送带, 分检器)下降
- 在电力不足时, 效率可能会因为取整问题下降
- 可容忍的设施速度有一定上限, 和改变设施速度或加入新设施的mod(例:创世之书)兼容性不佳

## 设置参数

在`starfi5h.plugin.AlterTickrate.cfg`可调整设施更新的周期(每x帧运算1次)。  
安装和配置文件请参考其他mod的说明。  
当参数设为1时默认为原版, 不套用修改。  
默认的参数可以稳定运行这个[10万糖存档](https://www.bilibili.com/video/BV1so4y1679M/)。若发现产量大幅改变, 请尝试调小参数或关闭功能。  
降频功能稳定程度: 电力 > 仓储 > 生产设施 > 研究站(生产/科研) > 研究站(搬运) > 分捡器 > 传送带  

### 电力与生产设施

`Facility`-`PowerSystem`: 默认为10。  
建议设为30的因数。  
电力设施补偿: 燃料消耗进度, 能量枢纽:充/放电电量, 射线接收站:透镜,光子进度。  

`Facility`-`Facility`: 默认为10。  
建议设为10的因数。  
生产设施补偿: 矿机, 组装机, 弹射器, 发射井进度。  
在空闲的逻辑帧会利用计算好的状态来继续更新设施的动画。  
**已知问题: 原油萃取站输出为4堆叠**  

### 研究站

`Lab`-`Produce`: 默认为10。  
每x帧更新一次生产模式的研究站。  
当参数在2以上时, 需要启用搬运修改(`Lab`-`Lift`)否则塔内运力会不足。  

`Lab`-`Research`: 默认为10。  
每x帧更新一次科研模式的研究站。可以依照研究速度调整适合的倍率。  

`Lab`-`Lift`: 默认为5。  
每x帧搬运研究站塔内的物料。
在原版中每1帧最多搬运1个原料往上/产物往下。   
在修改后, 研究模式的塔每次可以搬运一半的糖往上, 生产模式的塔每次可以搬运(一半+1)的原料往上, (9-目前存量)的产物往下。  
改变之后可能会因为矩阵冗余变化而改变矩阵产量, 需要一些时间重新平衡。  
**已知问题: 塔中研究站有时无法跑满, 需要精准输出时不建议使用**  

### 传送带
`Transport`-`Belt`: 默认为1, 最大值为2。  
原版最高速为蓝带(30每秒 = 0.5每帧), 可允许每2帧更新一次。  
注意当传送带更新频率降低时, 可能会破坏某些混带的设计。  
在更改传送带速度的mod同时存在时, 此项功能将停用。  

### 仓储/入塔/出塔
`Transport`-`Storage`: 默认为2。  
建议设为和传送带的参数相同。  

### 分捡器
`Transport`-`Sorter`: 默认为2。  
超过2时爪子可能会漏接/漏放, 造成混带失效或着满带压缩程度降低。  

### 介面
`UI`-`SmoothProgress`: 默认为false。  
利用插植使进度圆圈动画平滑。代价是进度圆圈和实际时间有x帧的延迟。  
套用的介面: 熔炉, 制造台, 研究站(生产), 弹射器, 发射井, 发电厂。  

## Mod兼容性

相容: 联机mod([NebulaMultiplayerMod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)), 优化mod([DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/))。  
不相容: [SampleAndHoldSim](https://dsp.thunderstore.io/package/starfi5h/SampleAndHoldSim/), [Blackbox](https://dsp.thunderstore.io/package/Raptor/Blackbox/)。  
GenesisBook, BetterMachines或BeltSpeedEnhancement等修改传送带的mod存在时, 将取消传送带功能且`Belt`, `Storage`将设置为1。  
[LabOpt](https://dsp.thunderstore.io/package/soarqin/LabOpt/)存在时, 将取消研究站功能且`Lab`相关参数将设置为1避免冲突。  
使用添加新建筑的mod时，建议使用SampleAndHoldSim以获得更好的兼容性。  

----

## Changelog

#### v0.2.3
\- Fix request power of ray receivers.  
\- Add LabOpt compat.  

<details>
<summary>Previous Changelog</summary>

#### v0.2.2
\- Add UI-SmoothProgress config option.  
\- Add additional notification for incompat mods check (ItemProto/RecipeProto).  

#### v0.2.1  
\- Rework lab lift. Now it no longer limit to max 4.  
\- Fix ray receiver graviton lens usage.  
\- Fix mining machine output stack to match mining speed.  
\- Fix inserter wait idle tick.  

v0.2.0 - Rework to fix lab. Renew config settings.  
v0.1.2 - Fix power stat value. Fix local fractionators abnormal.  
v0.1.1 - Fix belt feature doesn't apply.  
v0.1.0 - Initial release. (DSP 0.9.27.15466)  

</details>