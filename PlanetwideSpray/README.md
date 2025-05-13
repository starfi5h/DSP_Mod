# PlanetwideSpray

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/PlanetwideSpray/img/demo1.jpg)  

If a spray coaster is set without the belt underneath, it will turn into planetwide sprayer.  
The production facilities (*1) on entire planet will get their **raw materials** proliferated.  
By default, the products of those machine will not proliferate, unless config `Spray All Cargo` is set to true.  
With this mod, the direct insertion builds can get proliferation too.  

(*1) Support facilities list:  
1. Smelting, Assembler, Refinery, Chemical facilities that use sorters to input
2. Research Lab, Fuel Power Generator that use sorters to input
3. Fractionator: materials go through will be spray
4. Turret: bullets go into turrets will be spray

如果喷涂机的下方没有货物传送带，它将变成全球喷涂机。  
整个星球上的生产设施(*1)的**原料**将会喷涂增产剂。  
默认生产设施的产物不会喷涂, 除非将配置的`Spray All Cargo`开启。  
此mod可以让不使用传送带的产线也能享受到增产剂的效果。  

(*1) 支援的生产设备:  
1. 用爪子输入原料的熔炉, 制造台, 精炼厂, 化工厂
2. 用爪子输入原料的研究站, 燃料发电机
3. 分馏塔: 经过分馏塔的原料将被喷涂
3. 炮塔: 输入炮塔的弹药将会被喷涂

## Cheat Mode 作弊模式

In cheat mode, the following cargo will receive free proliferator points according to `Force Proliferator Level`:  
1. facilities that use sorters to input
2. feed by belt: turret, fractionator, power exchanger, liquid tank
3. input by belt: logistic station  
  
Planetwide sprayer patches will not apply in cheat mode.  

在作弊模式中, 以下的货物(原料)将依照增产等级免费得到增产点数:
1. 用爪子输入  
2. 传送带供给: 炮塔, 分馏塔, 能量枢纽, 储液罐  
3. 传送带输入: 物流站(以及创世之书的巨型工厂)  
  
作弊模式不会消耗增产剂, 因此全球喷涂机将不会作用  

## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.PlanetwideSpray.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。  
 
```
## Settings file was created by plugin PlanetwideSpray
## Plugin GUID: starfi5h.plugin.PlanetwideSpray

[Cheat]

## Spray everything insert by sorters if this value > 0
## (作弊选项)当此值>0, 使分捡器抓取的货物皆为此增产等级
# Setting type: Int32
# Default value: 0
# Acceptable value range: From 0 to 10
Force Proliferator Level = 0

[General]

## Spray every item transfer by sorters (including products)
## 喷涂任何分捡器抓取的货物(包含产物)
# Setting type: Boolean
# Default value: false
Spray All Cargo = false

## Spray every item flow into station or mega assemblers(GenesisBook mod)
## 喷涂流入物流塔/塔厂(创世之书mod)的货物
# Setting type: Boolean
# Default value: false
Spray Station Input = false

## Spray every item flow into fractionator
## 喷涂经过分馏塔的原料
# Setting type: Boolean
# Default value: true
Spray Fractionator = true

## Spray every item flow into turret
## 喷涂输入防御塔的弹药
# Setting type: Boolean
# Default value: true
Spray Turret = true

## Spray every item flow into fuel power generator
## 喷涂输入燃料发电机的燃料
# Setting type: Boolean
# Default value: false
Spray Fuel Power Generator = false
```

## Changelog

- v1.1.6 - Add config `Spray Fuel Power Generator`. Fix a bug that blocking sorters consume more proliferator.  
- v1.1.5 - Support fuel power generator.  
- v1.1.4 - Add config `Spray Fractionator`, `Spray Turret`. (DSP-0.10.32.25595)  
- v1.1.3 - Add config `Spray Station Input`. (DSP-0.10.31.24646)  
- v1.1.2 - Fix NRE in SpraycoaterGameTick_Prefix.  
- v1.1.1 - Spray more transportations in cheat mode. (DSP-0.10.30.22292)  
- v1.1.0 - Support fractionator and turret.  
- v1.0.1 - Add config `Spray All Cargo`.   
- v1.0.0 - Initial release. (DSP-0.10.29.22015)  