# MimicSimulation (work in progress)


![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/MimicSimulation/img/demo1.gif)  
Change how many planet factories run per tick, simulating idle factories input and output.  

## How does it work
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/MimicSimulation/img/time_chart.png)  
User can set how many planet factories can work during a game tick, the rest will put into idle state. For working factories, the factories will run as normal. For idle factories, simulate the input/output of them during the last active tick and apply them.  

### Factory Input:  
- Vein amount decrease  
- Logistic stations storage decrease by belt output ports  


### Factory Output:  
- Logistic stations storage increase by belt input ports  
- Research hash upload  
- Statistics data (production, power)  
- Ejector bullets & silo rockets  

----

## Changelog

#### v0.0.1  
\- Initial testing.  

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  