# BuildToolOpt

![Hologram](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BuildToolOpt/img/demo1.jpg)  
Provides optimization and extra QoL features to build tool and blueprint UI.

## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.BuildToolOpt.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    

```
## Settings file was created by plugin BuildToolOpt v1.0.0
## Plugin GUID: starfi5h.plugin.BuildToolOpt

[BuildTool]

## Remove c# garbage collection of build tools to reduce lag
## 移除建筑工具的强制内存回收以减少铺设时卡顿
# Setting type: Boolean
# Default value: true
RemoveGC = true

## Directly replace old station with new one in hand
## 可直接替换物流塔
# Setting type: Boolean
# Default value: true
ReplaceStation = true

## Place white holograms when lacking of item
## 即使物品不足也可以放置建筑虚影
# Setting type: Boolean
# Default value: true
EnableHologram = true

[UI]

## Optimize blueprint UI to reduce lag time
## 优化蓝图UI减少卡顿
# Setting type: Boolean
# Default value: true
UIBlueprintOpt = true
```

## Compatibility  

[NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)  
ReplaceStation and EnableHologram will be disabled.  
当与联机mod共用时, 替换物流塔和建筑虚影功能将暂停使用  
  
[CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/)  
When using ReplaceStation, please disable `Finish build immediately` in CheatEnabler first.  
使用替换物流塔功能时请先关闭CE的`建造秒完成`功能, 否则会造成错误  
