# BuildToolOpt

![Hologram](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BuildToolOpt/img/demo1.jpg)  
Provides optimization and extra QoL features to build tool and blueprint UI.
对建筑工具和蓝图 UI 提供优化和额外的便利功能。  

![ReplaceStation](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BuildToolOpt/img/demo1.gif)  
替换升级物流塔相当于作了以下的步骤:
1. 复制物流塔设定
2. 将塔内的所有物品取出
3. 拆除旧塔
4. 放上贴上旧塔设定的新塔
5. 重新连上传送带
6. 尝试将物品放回塔内，无法放入的则收进背包  

## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.BuildToolOpt.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    

```
## Settings file was created by plugin BuildToolOpt
## Plugin GUID: starfi5h.plugin.BuildToolOpt

[BuildTool]

## Remove c# garbage collection of build tools to reduce lag
## 移除建筑工具的强制内存回收以减少铺设时卡顿
# Setting type: Boolean
# Default value: true
RemoveGC = true

## Trigger garbage collection when game pause (esc menu). Enable this option if memory usage grow rapidly.
## 在游戏暂停时(Esc)回收内存
# Setting type: Boolean
# Default value: false
GC when pause = false

## Directly replace old station with new one in hand
## 可直接替换物流塔
# Setting type: Boolean
# Default value: true
ReplaceStation = true

## Place white holograms when lacking of item
## 即使物品不足也可以放置建筑虚影
# Setting type: Boolean
# Default value: false
EnableHologram = false

## Optimize RefreshTraffic to reduce lag when placing stations (experimental)
## 优化RefreshTraffic以减少放置物流塔的卡顿 (实验性)
# Setting type: Boolean
# Default value: false
EnableStationBuildOptimize = false

[UI]

## Optimize blueprint UI to reduce lag time
## 优化蓝图UI减少卡顿
# Setting type: Boolean
# Default value: true
UIBlueprintOpt = true

## Directly parse blueprint data from clipboard when Ctrl + V
## 热键粘贴蓝图时,直接读取剪切板
# Setting type: Boolean
# Default value: true
ClipboardPaste = true
```

## Compatibility  

[NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)  
ReplaceStation, EnableHologram and EnableStationBuildOptimize will be disabled.  
当与联机mod共用时, 替换物流塔和建筑虚影功能将暂停使用  
  
[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/) (v2.3.26)  
When replacing a station, `Finish build immediately` in CheatEnabler will be temporarily disabled until replace is done.  
使用替换物流塔功能时mod会暂时关闭CE的`建造秒完成`功能直到替换完成  

[DSPCalculator](https://thunderstore.io/c/dyson-sphere-program/p/jinxOAO/DSPCalculator/) (v0.5.11)  
按住Shift键并单击建筑物图标将复制该建筑物虚影及其配方设置至手中。  
Shift-clicking on building icon will copy that building with recipe settings to hand.  

[BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/) (v1.6.8)  
When the mod is not enabled and CommonAPI is enabled, add toggle blueprint view mode hotkey (`J`) and enable changing building tier in bluepirnt pannel.  
当 蓝图增强MOD不启用 并且 启用了 CommonAPI 时，添加切换蓝图视图模式热键(`J`)并启用在蓝图面板中更改建筑层级。  