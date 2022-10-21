# Nebula Compatibility Assist

[Spreadsheet for Nebula compatible mods list](https://docs.google.com/spreadsheets/d/193h6sISVHSN_CX4N4XAm03pQYxNl-UfuN468o5ris1s)  
Support Nebula multiplayer mod compatibility for following mods.  
DSP Belt Reverse Direction, MoreMegaStructure are required to install on both client and host.  

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- Client can now see all ILS stations when choosing system/global tab.  

### [DSPTransportStat](https://dsp.thunderstore.io/package/IndexOutOfRange/DSPTransportStat/)
- Client can now see all ILS stations when chaning filter conditions.  
- Client can't open remote station window yet.  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- Client can now see vein amount and power status on planets not loaded yet.  

### [DSPMarker](https://dsp.thunderstore.io/package/appuns/DSPMarker/)
- Markers now sync when players click apply or delete button.  
- Fix red error when exiting game ([issue#8](https://github.com/appuns/DSPMarker/issues/8))   
- Fix icon didn't refresh when arriving another planet.  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- Memo now sync when players add/remove icons, or finish editing text area.  

### [DSP Belt Reverse Direction](https://dsp.thunderstore.io/package/GreyHak/DSP_Belt_Reverse_Direction/)
- Now reverse direction will sync correctly.  
  Special thanks to GreyHak for permission to use his code.  

### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- Free mecha appearance now sync correctly.  

### [MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/)
- Sync data when player change mega structure type in the editor.

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- Sync station configuration and drone, ship, warper count.  
- Fix advance miner power usage abnormal ([issue#17](https://github.com/Pasukaru/DSP-Mods/issues/17))  
- Note: AutoStationConfig v1.4.0 is not compatible with DSP v0.9.27

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/)
- Sync auto station config functions.  
- Sync planetary item fill (ships, fuel) functions.  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- Fix client crash when leaving a system.  

----

# 联机兼容支援

[联机兼容的模组列表](https://docs.google.com/spreadsheets/d/193h6sISVHSN_CX4N4XAm03pQYxNl-UfuN468o5ris1s) 绿勾=无问题, 蓝勾=需两端皆安装, 红标=有严重冲突  
有些mod和联机模组Nebula multiplayer mod有兼容性问题, 此模组提供以下MOD的兼容支援: 

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- 让客户端显示所有星际物流塔的内容  

### [DSPTransportStat](https://dsp.thunderstore.io/package/IndexOutOfRange/DSPTransportStat/)
- 让客户端显示所有星际物流塔的内容  
- 客户端目前无法打开非本地的物流塔  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- 让客户端能显示未载入星球的资源储量和电力状态  

### [DSPMarker](https://dsp.thunderstore.io/package/appuns/DSPMarker/)
- 同步地图标记  
- 修复离开游戏时的错误 ([issue#8](https://github.com/appuns/DSPMarker/issues/8))  
- 修复到达另一个星球标记没更新的bug  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- 同步星球註記  

### [DSP Belt Reverse Direction](https://dsp.thunderstore.io/package/GreyHak/DSP_Belt_Reverse_Direction/)
- 同步传送带反转方向
  
### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- 同步免费的机甲外观  

### [MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/)
- 当巨构类型更改时同步资料  

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- 同步物流站自动配置  
- 修复大矿机能耗异常的问题 ([issue#17](https://github.com/Pasukaru/DSP-Mods/issues/17))  
- 注意：AutoStationConfig v1.4.0 与 游戏版本v0.9.27 不兼容  

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/) [辅助多功能mod](https://www.bilibili.com/video/BV1SS4y1X75n)
- 同步物流站自动配置相关功能  
- 同步一键填充星球上的飞机飞船翘曲器、燃料  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- 修复客户端离开星系会使游戏崩溃的错误  

主要是让主机和客户端显示的内容可以一致，或著修復建築不同步的問題。  
DSP Belt Reverse Direction、MoreMegaStructure必须要两端都得安装。  
热修联机0.8.11版中配送运输机出错的bug。  

----

## Changelog

#### v0.1.5 (NebulaMultiplayerMod 0.8.11)  
\- Fix mod data doesn't sync correctly for another clients.  
\- Fix client mecha spawning position.  

#### v0.1.4 (NebulaMultiplayerMod 0.8.11)  
\- Hotfix for nebula 0.8.11 about host sometimes get error when client request logistic on other planets.  
\- Hotfix for GS2 star detail doesn't display correctly for clients.  

#### v0.1.3 (NebulaMultiplayerMod 0.8.11)
\- Hotfix for nebula 0.8.11 about logistic bots errors.  
\- Fix client error when host reverse belts on a remote planet.  

#### v0.1.2 (NebulaMultiplayerMod 0.8.10)
\- Support DSPOptimizations  

#### v0.1.1 (NebulaMultiplayerMod 0.8.10)
\- Support AutoStationConfig, Auxilaryfunction.  
\- Fix advance miner power usage abnormal of AutoStationConfig.   

#### v0.1.0 (NebulaMultiplayerMod 0.8.8)
\- Support DSPTransportStat, PlanetFinder, DSPFreeMechaCustom, MoreMegaStructure.  
\- Fix DSPMarker didn't refresh marker when local planet changed.  

#### v0.0.1  
\- Initial release. (Game Version 0.9.25.12201)

----

<a href="https://www.flaticon.com/free-icons/puzzle" title="puzzle icons">Puzzle icons created by Freepik - Flaticon</a>