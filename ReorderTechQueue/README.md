# ReorderTechQueue  


![click-and-drag](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/ReorderTechQueue/img/demo1.gif)  
Rearrange research queue by pressing left-click and dragging the icon to the target position.  
If there are techs depending on selecting tech, it can't move to position behind them.  
If selecting tech moves to position in front of its prerequisites, the tech will get removed.  

![navi-button](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/ReorderTechQueue/img/demo2.png)  
Add a button to navigate to implicit required tech.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `ReorderTechQueue.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `com.starfi5h.plugin.ReorderTechQueue.cfg` file.  

- `TechQueueLength`  
Length of research queue. (Default:`8`)  

----

## Changelog

#### v1.2.0
\- Add navigate button to ImplicitPreTechRequired.  
\- Allow tech to be queued even if the metadata requirement is not met. (0.10.29.22015)  

#### v1.1.1
\- Fix a bug that moving an infinite tech will enqueue duplicate tech.  

#### v1.1.0
\- Add a config option to change research queue length.  
\- Fix a bug that changing a tech will remove all dependent techs behind it.  

#### v1.0.0  
\- Initial release. (Game Version 0.9.24.11286)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  