# DeliverySlotsTweaks

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/DeliverySlotsTweaks/img/demo1.jpg)

1. Let replicator and build tools use items in logistic slots.
2. When logistic bots deliver items to mecha, let logistic slots fill first.   
3. Change the parameters of logistic slots (size, stack size).  
4. Overwrite default item stack size count in mecha inventory and fuel chamber.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `DeliverySlotsTweaks.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.DeliverySlotsTweaks.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  
The changes will take effects after reboost, or go to game settings and click 'Apply' button.  
Some effects will last in save after the mod is disabled.  
When reducing ColCount, please clean up logistic slots first to prevent hidden slots not being accessible.  

| | Default | Description |
| :----- | :------ | :---------- |
| DeliveryPackage | | |
| `UseLogisticSlots`     | true | Let replicator and build tools use items in logistic slots. Take effect affter reloading the game. |
| `AutoRefillFuel`       | false | Allow fuel chamber to also take from logistics slots. |
| `AutoRefillWarper`     | false | Auto refill space warper from inventory and logistics slots. |
| `ColCount`             | 0    | No Change:0 TechMax:3 Limit:5 |
| `StackSizeMultiplier`  | 0    | No Change:0 TechMax:10 |
| `DeliveryFirst`        | true | When logistic bots send items to mecha, send them to delivery slots first. |
| `SortToDelieverySlots` | false | When sorting inventory, send them to delivery slots first. |
| PlayerPackage | | |
| `StackSize`            | 0    | Unify & overwirte item stack size in mecha inventory. Load game with this value ≤ 0 will skip this patch. |
| `StackMultiplier`      | 0    | Apply multiplier for stack size in inventory. Load game with this value ≤ 0 will skip this patch. |
| BuildTool | | |
| `EnableArchitectMode`  | false | Build without requirement of items (infinite buildings) |
| `EnableFastReplicator` | true | Right click on hotbar to queue the building in replicator |

----

# 物流清单修改

1. 改变物流清单的逻辑行为, 使手动制造和建筑工具也可以使用物流清单内的物品。  
2. 当配送物品至机甲时, 优先补充物流清单的栏位。  
3. 改变物流清单的参数(列,堆叠数量)。  
4. 覆蓋机甲背包及燃烧室中的物品堆疊上限。  

## 配置   
配置文件(.cfg)需要先运行过游戏一次才会出现。  
在修改完配置文件后重启游戏, 或进入游戏设置, 点击'应用设置'即可立即套用新的数值设定。  
部分修改后的效果在停用mod后依然会存在。   

管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.DeliverySlotsTweaks` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.DeliverySlotsTweaks.cfg`文件  
当减少物流清单容量时, 请先清空清单避免无法存取的残留栏位影响物流  

| | Default | Description |
| :----- | :------ | :---------- |
| DeliveryPackage | | |
| `UseLogisticSlots`     | true | 使手动制造和建筑工具可以使用物流清单内的物品。重启游戏后生效 |
| `AutoRefillFuel`       | false | 自动补充燃料时也会使用物流清单内的物品 |
| `AutoRefillWarper`     | false | 从背包和物流清单自动补充翘曲器 |
| `ColCount`             | 0    | 物流清单容量-列(不变:0 原版科技:3 最高上限:5) |
| `StackSizeMultiplier`  | 0    | 物流清单物品堆叠倍率(不变:0 原版科技:10) |
| `DeliveryFirst`        | true | 配送机会优先将物品送入物流清单的栏位 |
| `SortToDelieverySlots` | false | 整理背包时会先将物品送入物流清单的栏位 |
| PlayerPackage | | |
| `StackSize`            | 0    | 统一覆蓋机甲背包中的物品堆疊上限。当此值≤0时载入游戏将不会套用修改,直到游戏重启 |
| `StackMultiplier`      | 0    | 修改玩家背包中的物品堆疊倍率。当此值≤0时载入游戏将不会套用修改,直到游戏重启 |
| BuildTool | | |
| `EnableArchitectMode`  | false | 建筑师模式:建造无需物品 |
| `EnableFastReplicator` | true | 右键单击快捷栏中的建筑可直接在合成器排程制造 |

## MOD兼容性
RebindBuildBar(1.0.4): 可改工具列会显示背包+物流清单的建筑数目  
BlueprintTweaks(1.6.4): 使用带地基的蓝图时允许使用物流栏内的物品  
Auxilaryfunction(2.8.2): 飞往未完成建筑也会使用物流栏位的物品  
Multifunction_mod(3.4.5): 建筑师模式开启时, 工具列将显示建筑数目为999并且不再消耗  
NebulaMultiplayerMod(0.9.0): 其他玩家发起的建筑事件不会消耗本地背包中的建筑  
  
CheatEnabler(2.3.26): 建筑师模式兼容不再同步, 请自行打开本mod设置的建筑师模式  
BuildBarTool(1.0.1): 第二建造栏未支援此mod的功能  
可能会和改机甲物流背包的mod冲突  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  