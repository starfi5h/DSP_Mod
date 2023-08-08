# DeliverySlotsTweaks

1. Let replicator and build tools use items in logistic slots.
2. When logistic bots deliver items to mecha, Let logistic slots fill first.   
3. Change the parameters of logistic slots (size, stack size).

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `DeliverySlotsTweaks.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.DeliverySlotsTweaks.cfg` file.  
If you're using mod manager, you can find the file in Config editor.  
The changes will take effects after reloading the save, and last in save even after the mod is disabled.  
When reducing ColCount, please clean up logistic slots first to prevent hidden slots not being accessible.  

| | Default | Description |
| :----- | :------ | :---------- |
| `ColCount`             | 0    | No Change:0 TechMax:2 Limit:5 |
| `StackSizeMultiplier`  | 0    | No Change:0 TechMax:10 |
| `DeliveryFirst`        | true | When logistic bots send items to mecha, send them to delivery slots first. |

----

# 物流清单修改

1. 改变物流清单的逻辑行为, 使手动制造和建筑工具也可以使用物流清单内的物品。
2. 当配送物品至机甲时, 优先补充物流清单的栏位。
3. 改变物流清单的参数(列,堆叠数量)。

## 配置   
配置文件需要先运行过游戏一次才会出现，修改后要重新载入存档才会生效。修改后的效果在停用mod后依然会存在。    
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.DeliverySlotsTweaks` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.DeliverySlotsTweaks.cfg`文件  
当减少物流清单容量时, 请先清空清单避免无法存取的残留栏位影响物流  

| | Default | Description |
| :----- | :------ | :---------- |
| `ColCount`             | 0    | 物流清单容量-列(不变:0 原版科技:2 最高上限:5) |
| `StackSizeMultiplier`  | 0    | 物流清单物品堆叠倍率(不变:0 原版科技:10) |
| `DeliveryFirst`        | true | 配送机会优先将物品送入物流清单的栏位 |

----

## Changelog

v1.0.1 - Fix a bug that some logistics  solts are not usable.  
v1.0.0 - Initial release. (DSPv0.9.27.15466)  

----

## Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  