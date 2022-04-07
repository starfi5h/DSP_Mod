# ReorderTechQueue  


![click-and-drag](https://raw.githubusercontent.com/starfi5h/DSP_Mod/master/ReorderTechQueue/img/demo1.gif)  
Rearrange research queue by pressing left click and dragging the icon to the target position.  
If there are techs depend on selecting tech, it can't move to position behind them.  
If selecting tech move to position in front of its prerequisites, the tech will get removed.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `ReorderTechQueue.dll` in `BepInEx/plugins` folder.

## Configuration

Run the game one time to generate `com.starfi5h.plugin.ReorderTechQueue.cfg` file.  

- `TechQueueLength`  
Length of reserach queue. (Default:`8`)  

----

## Changelog

#### v1.1.0
\- Add a config option to change research queue length.  
\- Fix a bug that changing a tech will remove all dependent techs behind it.  

#### v1.0.0  
\- Initial release. (Game Version 0.9.24.11286)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  