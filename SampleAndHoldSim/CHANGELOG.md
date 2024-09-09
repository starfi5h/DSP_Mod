## Changelog

#### v0.6.11
\- Scale kill stats for remote slowed planets.  

#### v0.6.10
\- Fix UI change for DSP-0.10.30.23430  

#### v0.6.9
\- Update for DSP-0.10.30.23292  
\- Remove Multfuntion_mod compat as the compat patch is outdated.  

#### v0.6.8
\- Fix IndexOutOfRangeException in `PowerExchangerComponent.CalculateActualEnergyPerTick`.  

#### v0.6.7
\- Add config `Combat` - `EnableRelayLanding` to enable/disable Dark Fog relay from landing.  
\- Add Auxilaryfunction compat for its stop factory feature.  

#### v0.6.6
\- Fix IndexOutOfRangeException in FactoryManager.SetMineral.   

#### v0.6.5
\- Add config `UI`:`WarnIncompat` to stop mod incompatibility warning showing up in the future.  


<details>
<summary>Previous Changelog</summary>

#### v0.6.4
\- Missiles and plasma cannons now have damage sclae up on remote planets.  
\- Relay will not land on planets with shield on remote star systems.  

#### v0.6.3
\- Discard the change to hives ticks. Now all space hives will not be affected and run in normal speed.  
\- Remote systems now have Lancers damage scale down to the ratio.  

#### v0.6.2
\- Bugfixes for index out of range error in UpdateHives and MainManager.TryGet.  

#### v0.6.1
\- Focus local now will focus on local star system, including every hives and planets in the system.  
\- Player space fleet now always run in normal speed.  
\- Fix time value error in enemy tick logic take make enemy behave abnormally.  

#### v0.6.0 (DSP-0.10.28.21014)
\- Adapt to game Dark Fog version. (The battle part still needs testing)  
\- `UpdatePeriod` default value is not set to 5.  
\- Remove TCFV and Multfuntion compat support.  

#### v0.5.7  
\- Remove vein logic.  
\- Update CheatEnabler compat to v2.3.1  
\- Update Multfuntion mod compat to v2.8.6  

#### v0.5.6  
\- Add CheatEnabler 2.2.0 compat.  
\- Fix PlanetMiner 3.0.7 compat.  

#### v0.5.5
\- Fix stats incorrect for Multfuntion mod planet miners.  

#### v0.5.4
\- Fix len consumption rate abnormal on idle factories.  
\- Fix IsNextIdle value of focus factories.  
\- Add PlanetMiner 3.0.7 compat.  

#### v0.5.3
\- Fix TCFV compat: fix that sheild doesn't regenerate on some planets.  

#### v0.5.2
\- Add Blackbox compat  
\- Hide station UI when flow rate = 0.  

#### v0.5.1
\- Fix error when player land on an unexplored planet.  
\- Reduce memory allocation to fix stuttering.  

#### v0.5.0 (DSP-0.9.27.15466)
\- Rework: Change logic from MaxFactoryCount to UpdatePeriod, add related config.    
\- Remove vein comsumption rate UI.  
\- Enable station storage to have negative values to prevent generating extra item.  
\- Fix compat with Auxilaryfunction. Add guard to prevent all vein disappear.  
\- Add compat with Multfuntion mod, TheyComeFromVoid.  

#### v0.4.2
\- Fix error when NebulaAPI is not enabled.  

#### v0.4.1
\- Fix vein UI errors cause by vein manipulation.  
\- Improve compatible with Nebula.  

#### v0.4.0
\- Fix ship delivery from other stations sometimes gets multiplied.  
\- Change UI settings to let users customize monitor time.  

#### v0.3.3
\- Fix compatibility for game version 0.9.26.13026  
\- Fix Advance Miner mining amount.  

#### v0.3.2
\- Fix compatibility for game version 0.9.26.12900

#### v0.3.1
\- Fix warper consume stat.  
\- Fix vein amount decrease in InfiniteResource.  
\- Copied rockets now find new target when the node is full.  

#### v0.3.0
\- Add a config option to switch display unit (/s or /min)
\- Fix game crash when unlocking tech background.  

#### v0.2.1
\- Fix a bug that sometimes switching game with veinUI enable will get errors.  

#### v0.2.0  
\- Add EnableVeinConsumptionUI option.  
\- Fix error when removing stations.  

#### v0.1.1  
\- Fix veinGroup value changes.  

#### v0.1.0  
\- Initial release.  

</details>