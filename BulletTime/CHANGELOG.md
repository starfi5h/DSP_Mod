## Changelog

#### v1.5.2
\- Add config `UI`:`MaxSpeedupScale`  

#### v1.5.1 (DSP-0.10.30.23430)
\- Fix background button UI.  
\- Fix rockets don't dock into dyson nodes when dyson sphere rotation is paused.  

#### v1.5.0 (DSP-0.10.30.22350)
\- Add pause/resume/speedup speed control buttons.  

<details>
<summary>Previous Changelog</summary>

#### v1.4.8 (DSP-0.10.30.22292)
\- (Nebula) Update download progression for other players.  

#### v1.4.7 (DSP-0.10.29.21950)  
\- Fix crash when viewing remote hives in starmap during background autosave.  

#### v1.4.6
\- Update to NebulaAPI 2.0.0 (Nebula Multiplayer Mod 0.9.0)  
\- (Nebula) Fix the screen wrongly displaying "Saving" when a player joins.  
\- (Nebula) Fix UI window gets closed when resume event trigger.  

#### v1.4.5
\- Fix that host can't place building after client joining.  
\- Enable client to pause the game.  

#### v1.4.4
\- Properly stop animation when hotkey pause.  

#### v1.4.3
\- Prevent autosave when pausing.  

#### v1.4.2
\- Fix that replicator queue doesn't work in pause mode.  

#### v1.4.1
\- Fix error when enabling background autosave.  

#### v1.4.0
\- Add compat to Nebula pre-release version.  
\- Add config `Pause`:`EnableMechaFunc`  
\- Add config `UI`:`StatusTextHeightOffset`, `StatusTextPause`  
\- Remove config `Speed`:`UIBlueprintAsync`. This feature has been move to BuildToolOpt mod.  
\- Remove config `Multiplayer`:`MinimumUPS`  

#### v1.3.1
\- Fix error when creating a new game with dark fog enabled.  
\- Pause mode using pause hotkey will now let projectiles fire in normal speed and display a notification.  

#### v1.3.0
\- Adapt to game version 0.10.28.20829 For game version 0.9.27, please roll back to BulletTime v1.2.14.  
\- Add a toggle button to enable background auto feature in performance pannel. The default value is set to off now.  
\- Add config `Hotkey`-`KeyPause`, which will toggle pause mode by hitting the hotkey.  
\- Config option `KeyAutosave` has been move to `Hotkey` catagory.  

#### v1.2.14
\- Fix error by fast travel when pasueThisFrame. Fast travel to another planet is now disable during pause mode.  

#### v1.2.13
\- Fix a bug that corrupts large blueprint when editing its title or desc.  
\- `UIBlueprintAsync` default value is false now.  

#### v1.2.12
\- Fix a bug that Ctrl+V no longer load the previous blueprint.  

#### v1.2.11 (DSP0.9.27.15466)  
\- Add `UIBlueprintAsync` config option.  

#### v1.2.10  
\- Remove game speed indicator for 0.9.27.14546.  

#### v1.2.9
\- Add `RemoveGC`config option.  
\- Backward compatible with 0.9.26.13034.  

#### v1.2.8
\- Adapt to game version 0.9.27.14546.  

#### v1.2.7
\- (Nebula) Add `MinimumUPS` config option.  
\- Disable force GC in vanilla game when placing buildings.  

#### v1.2.6
\- Change `KeyAutosave` from KeyCode to KeyboardShortcut  
\- Small tweak to backgroud autosave. (Game version 0.9.26.12201)  

#### v1.2.5
\- Add EnableFastLoading config option. (Game version 0.9.25.11996)  
\- (Nebula) Fix an issue that sometimes when client disconnect, the host will enter pause state.  

#### v1.2.4
\- (Nebula) Resume from pause when a client disconnect during loading a factory.  

#### v1.2.3
\- (Nebula) Fix host sometimes hangs in pause mode when loading factories. Now manual saving will reset pause states.   
\- Make block image in background autosave transparent.  

#### v1.2.2
\- (Nebula) Enable dyson sphere rotation start/stop button in editor.   
\- (Nebula) Handle multiple pause events that happen at the same time.  

#### v1.2.1
\- Show game speed in FPS indicator (Shift + F12)  
\- Fix camera & mecha movement speed in low speed.  

#### v1.2.0
\- (Nebula) Add support for multiplayer.  


#### v1.1.0
\- Add StartingSpeed config option.  
\- Only block interaction during exporting local factory.  

#### v1.0.2  
\- Initial release. (Game version 0.9.24.11286) 

</details>