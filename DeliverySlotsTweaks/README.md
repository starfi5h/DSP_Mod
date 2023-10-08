# DeliverySlotsTweaks

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/DeliverySlotsTweaks/img/demo1.jpg)

1. Let replicator and build tools use items in logistic slots.
2. When logistic bots deliver items to mecha, Let logistic slots fill first.   
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
| `ColCount`             | 0    | No Change:0 TechMax:2 Limit:5 |
| `StackSizeMultiplier`  | 0    | No Change:0 TechMax:10 |
| `DeliveryFirst`        | true | When logistic bots send items to mecha, send them to delivery slots first. |
| PlayerPackage | | |
| `StackSize`            | 0    | Unify & overwirte item stack size in mecha inventory. Load game with this value ≤ 0 will skip this patch. |
| `StackMultiplier`      | 0    | Apply multiplier for stack size in inventory. Load game with this value ≤ 0 will skip this patch. |

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
| `ColCount`             | 0    | 物流清单容量-列(不变:0 原版科技:2 最高上限:5) |
| `StackSizeMultiplier`  | 0    | 物流清单物品堆叠倍率(不变:0 原版科技:10) |
| `DeliveryFirst`        | true | 配送机会优先将物品送入物流清单的栏位 |
| PlayerPackage | | |
| `StackSize`            | 0    | 统一覆蓋机甲背包中的物品堆疊上限。当此值≤0时载入游戏将不会套用修改,直到游戏重启 |
| `StackMultiplier`      | 0    | 修改玩家背包中的物品堆疊倍率。当此值≤0时载入游戏将不会套用修改,直到游戏重启 |

## MOD兼容性
RebindBuildBar: 可改工具列会显示背包+物流清单的建筑数目  
CheatEnabler, Multifunction_mod: 建筑师模式开启时, 工具列将显示建筑数目为999并且不再消耗  
NebulaMultiplayerMod: 其他玩家发起的建筑事件不会消耗本地背包中的建筑  

----

## Changelog

v1.2.3 - Fix Nebula, Multfunction_mod, CheatEnabler(v2.2.7) compat.  
v1.2.2 - UseLogisticSlots for blueprint paste. Add Multifunction_mod(ArchitectMode), RebindBuildBar compat.  
v1.2.1 - UseLogisticSlots for Auto Replenish. Add Nebula compat.  
v1.2.0 - Add `UseLogisticSlots`, `StackMultiplier` config options. Add CheatEnabler(ArchitectMode) compat.  
v1.1.1 - Apply `StackSize` setting to fuel chamber and warper slot.  
v1.1.0 - Add `StackSize` config option. Now can apply mod config changes in game settings.  
v1.0.1 - Fix a bug that some logistics  solts are not usable.  
v1.0.0 - Initial release. (DSPv0.9.27.15466)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  