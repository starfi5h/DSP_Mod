# Sample and Hold Simulation  

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/demo1.gif)  
Change how many planet factories run per tick, simulating idle factories input and output using values from last tick.  
The factories tick less but still make the same amount of items in ILS with larger gap in time.  
Recommend to set focus local factory to true and not set cycle time too high to have a better experience.  

Warning: Because this mod manipulate stats data, if it does not work as intend, it may diable Milkyway upload.  

![UI](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/UI1.jpg)  
Additional UI for displaying change rate of resources. Configurable in options.  

## How does it work
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/time_chart.png)  
User can set how many planet factories can work during a game tick, the rest will be put into the idle state. For working factories, the factories will run as normal. For idle factories, simulate the input/output by value changes of the last active tick.  
In the example chart, the upper one is the original game which runs 3 factories per tick, and their factory cycles are 4/3/2. The lower one set cycle time = 3 ticks so there is only 1 factory run per tick, and it now takes 3 times to complete a full factory cycle.  

### Factory Input:  
- Mineral amount decrease in veins  
- Logistic stations storage decrease by belt output ports  

### Factory Output:  
- Logistic stations storage increase by belt input ports  
- Research hash upload  
- Statistics data (production, power)  
- Ejector bullets & silo rockets  

![normal vs sim](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/demo3.gif)  
Simulation in action. Above: normal game, cycle = 1. Below: mod enable, cycle = 2.  
In the gif, both vein amount go from 100 to 90, and station storage go from 55 to 65.  
  
![stats](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/stats1.jpg)  
The production throughput will catch up with original one in long term if it is stable. In short term there are some differences, for example statistic data will be more sparse. Also local conponents inside the factory will be slower, so storage boxes or tanks will have fewer items than vanilla.  

## Configuration

Run the game one time to generate `com.starfi5h.plugin.SampleAndHoldSim.cfg` file.  

- `MaxFactoryCount` (Default:`100`)  
Maximum number of factories allow to active and run per tick.  

- `EnableStationStorageUI` (Default:`true`)  
Display item count change rate in station storages in last 10 seconds.  

- `EnableVeinConsumptionUI` (Default:`true`)  
Display mineral consumption rate of vein group in last 30 seconds.  

- `UnitPerMinute` (Default:`false`)  
If true, show rate in unit per minute. otherwise show rate in unit per second.  

## Compatibility  

(✅) CommonAPI  
(✅) DSPOptimizations  
(⚠️) NebulaMultiplayer - Only host can use this.  
(⚠️) Blackbox - The production stats of blackbox will be multiplied.  

----
# 取样保持模拟

尝试用取样工厂输入/输出的方法来减少计算量。  
使用者可以在性能测试面板设定每个逻辑祯可以使多少星球运行，闲置的星球将会用上一个tick的值来模拟工厂的输入和输出。  
建议勾选Focus Local让本地工厂保持运行来维持游戏体验，运行工厂数调低到能让UPS>60就好。  
警告: 此mod会改动统计资料。虽然目的是为了近似原本游戏的数据，但是出错时可能会让存档无法上传银河系。  

## 运作原理
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/time_chart.png)  
此图中有三个星球工厂，星球A的工厂的物品数量变化是+2/+1/+0/-1，经过一个完整生产周期后最终会有2个物品。其余工厂同理。  
上方为原本游戏运行方式，每一祯有3个工厂运作，完整周期分别是4/3/2。  
下方为Mod改变之后的运作方式，每一祯有1个工厂运作，在闲置的期间(浅色格)会让数值套用上一次工作的变化，完整周期变为3倍-12/9/6。  
套用变化的只有工厂的输入和输出，工厂的内部元件会以低速运行。而戴森球系统和运输机则继续每祯都运行。  

### 工厂输入  
- 矿脉的矿物消耗  
- 物流塔流出减少的货物量  

### 工厂输出  
- 物流塔流入增加的货物量      
- 研究的哈希块上传量  
- 产物统计和电力统计  
- 射出的太阳帆和火箭  

![normal vs sim](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/SampleAndHoldSim/img/demo3.gif)  
实际演示，上图为正常游戏，下图为设置cycle=2。
下图的内部元件速率只有上图的一半，但是一次消耗石矿数目和进塔数目皆为上图的2倍，因此最终两输入和输出的速率一样:两者的石矿储量皆从100降到90，而物流塔内的石材数量皆从55增加到65。
  
如果工厂是稳定的，长期下来模拟的产量和真实的产量会相近，短期上统计的数据可能会变得稀疏，此外工厂内部元件(仓储,运输带)中货物的增加速率也会比原本的少。  

## 設置
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SampleAndHoldSim` -> Edit Config  
手动安装: 更改`BepInEx\config\com.starfi5h.plugin.SampleAndHoldSim.cfg`文件  
  
- `MaxFactoryCount` (Default:`100`)  
每个逻辑祯所能运行的最大工厂数量  

- `EnableStationStorageUI` (Default:`true`)  
显示过去10秒内物流塔货物的流入或流出速率  

- `EnableVeinConsumptionUI` (Default:`true`)  
显示过去30秒内矿脉的矿物消耗速率  

- `UnitPerMinute` (Default:`false`)  
true: 显示单位设为每分钟速率 false: 显示每秒速率  

----

## Changelog

#### v0.3.0
\- Add a config option to switch display unit (/s or /min)
\- Fix game crash when unlocking tech background.  

#### v0.2.1
\- Fix a bug that sometimes switching game with veinUI enable will get errors.  

#### v0.2.0  
\- Add EnableVeinConsumptionUI option.  
\- Fix error when removing stations.  

#### v0.1.1  
\- Fix veinGroup value changes.  

#### v0.1.0  
\- Initial release.  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  