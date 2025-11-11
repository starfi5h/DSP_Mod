# Bullet Time - Game Speed Control Mod

Take full control of time in Dyson Sphere Program\! This mod allows you to speed up, slow down, or completely pause the game to fit your playstyle.  
This is quality of life mod. It doesn't affect achievements or metadata.

## Key Features

By decoupling the mecha (player actions) from the world simulation, Bullet Time offers three main ways to control the flow of time:

#### 1\. Overall Game Speed (Speed Buttons)
![Speed control buttons](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/speedbuttons.png)  
  - **What it does:** Use the buttons at the bottom-right of your screen to change the entire game's speed, from 0x (pause) up to a 10x multiplier. This affects you, your factories, and everything in the universe.
  - **Best for:** Speeding up long interstellar flights, waiting for resources to accumulate, or fast-forwarding through early-game tasks.
  - **Note:** The speed adjustment is done by setting the target UPS (Updates Per Second, the game's simulation speed). The default 1x speed is 60 UPS. Your actual maximum speed is still limited by your PC's hardware performance.

#### 2\. Tactical Pause Mode (Hotkey)
![Tactical pause](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo3.gif)  
  - **What it does:** Press the `Pause` key to "hard freeze" the game. In this mode, the world and your mecha stop completely, but you can still freely move the camera, queue up construction, place blueprints, and change building settings.
  - **Best for:** View and plan your factory without time pressure. Or observe frame-by-frame.
  - **By default:** Your mecha actions are disabled in this mode. You can change this in the configuration (`EnableMechaFunc`).  
You can also advance the game by a single frame using the `KeyStepOneFrame` hotkey. It is recommended to use the step function when the mecha has stopped moving.  

#### 3\. Bullet-Time Mode (World Speed Slider)
![World speed adjustment](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo1.gif)  
  - **What it does:** In the in-game **Performance Statistics Panel**, you'll find a "World Speed" slider. Lowering this slows down everything *except* your mecha. You continue to move, build, and interact at normal speed while the world operates in slow motion.
  - **Best for:** Improving FPS in massive late-game factories by reducing the CPU's calculation load. It's also great for enjoying a "Sandevistan"-like experience where you have more time to react.
  - **If set to 0%:** This creates a unique time-stop where your factories halt, but you can still move around freely.

## Other Utility Features

  - **Background Autosave:** Runs the autosave process on a background thread to prevent the game from freezing. The game will briefly enter the bullet-time pause mode (mecha can still move) to ensure data consistency during the save.  
  Note: This function has risk potential, so it's better to test it first.  
  - **Fast Main Menu Loading:** Speeds up loading into the main menu by skipping the 3D planet model rendering on the title screen.
  - **Reduce Stuttering:** Includes an option to remove the game's forced garbage collection (GC) when using build tools, which can help reduce stuttering when placing many buildings.

## How to Use

It is recommended to install it through [r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/) or [GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/).  

  - **Speed Controls:** Use the buttons in the bottom-right corner to Pause, Resume (or reset to 1x), and Speed Up.
  - **Tactical Pause:** Press the `Pause` / `Break` key to toggle.
  - **Bullet-Time Slider:** Open the Performance Statistics panel to find and adjust the World Speed slider.
  - **Background Autosave Toggle:** Open the Performance Statistics panel to enable background autosave on the top-right checkbox.  
  - **View Performance:** Press `Shift + F12` to see the real-time FPS and UPS in the top-left corner.

## Configuration

After running the game once with the mod installed, a config file will be generated. You can edit it in two ways:

  - **Via Mod Manager (Recommended):** In your mod manager (e.g., r2modman), go to the "Config editor" section, find `com.starfi5h.plugin.BulletTime`, and click "Edit Config".
  - **Manually:** Navigate to `BepInEx\config\` in your game folder and open the `com.starfi5h.plugin.BulletTime.cfg` file with a text editor.

*Changes will take effect after you restart the game.*

| Key | Description | Default |
|---|---|---|
| `KeyAutosave` | Keyboard shortcut for auto-save. | `F10 + LeftShift` |
| `KeyPause` | Hotkey for toggling Tactical Pause mode. | `Pause`｜`Break` |
| `KeyStepOneFrame` | Hotkey to advance 1 frame in pause mode. | `None` |
| `EnableMechaFunc` | If true, your mecha can move in Tactical Pause mode. | `false` |
| `EnableBackgroundAutosave` | Run autosave in the background. | `false` |
| `EnableHotkeyAutosave` | Enable hotkey to trigger autosave. | `false` |
| `StartingSpeed` | Game speed (in percent) when the game begins. | `100` |
| `EnableFastLoading` | Skip planet rendering on title screen for faster loading. | `true` |
| `RemoveGC` | Remove force garbage collection of build tools to reduce stutter. | `true` |
| `MaxSpeedupScale` | Maximum game speed multiplier for the speedup button. | `10` |
| `MaxSimulationSpeed` | In outer space, shift-click to set the simulation speed to this value. | `10.0` |

## Multiplayer Features (Nebula)

  - When a player is joining or the host is saving, the game will enter a pause mode (where players can only move) and resume automatically when finished.
  - If the game gets stuck in pause mode, the host can manually resume it by moving the World Speed slider.
  - Both host and clients can use the pause hotkey. Game speedup is only synced if the "SyncUPS" multiplayer option is enabled.
  - Players can stop/resume Dyson Sphere rotation in the editor.

## Compatibility

  - [✅] GalacticScale
  - [✅] NebulaMultiplayer

-----

-----

# BulletTime 子弹时间-游戏速度控制MOD

完全掌控你的游戏时间！这个 Mod 让你可以在《戴森球计划》中自由地加速、减速或暂停游戏。  
(便利性mod，不会影响成就和元数据)  

## 核心功能详解

本 Mod 提供三种主要的时间控制方式：

#### 1\. 全局游戏速度（速度按钮）
![Speed control buttons](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/speedbuttons.png)  
  - **功能介绍：** 使用屏幕右下角的按钮，将整个游戏的速度在 0 倍（暂停）到最高 10 倍之间调整。这会同时影响你的机甲和游戏世界中的一切。
  - **适用场景：** 加速漫长的星际飞行、等待资源积累，或快速推进游戏前期进程。
  - **请注意：** 速度调整的原理是设定游戏的 UPS（Updates Per Second，每秒更新次数）目标。默认 1 倍速为 60 UPS。你的实际最大速度仍然受电脑硬件性能的限制。

#### 2\. 战术暂停模式（热键）
![Tactical pause](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo3.gif)  
  - **功能介绍：** 按下 `Pause` 键，将游戏“完全冻结”。在此模式下，世界和你的机甲都会停止，但你仍然可以自由移动视角、下达建造指令、放置蓝图和更改建筑设置。
  - **适用场景：** 不受时间压力地查看和规划您的工厂。或者逐帧观察游戏运行。
  - **默认状态：** 在此模式下机甲无法移动。你可以在设置中（`EnableMechaFunc`）更改此项。  
你也可以使用 `KeyStepOneFrame` 热键来让游戏前进一个逻辑帧。建议在机甲停止移动时再使用步进功能。  

#### 3\. 子弹时间模式（世界速度滑块）
![World speed adjustment](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/BulletTime/img/demo1.gif)  
  - **功能介绍：** 在游戏内的 **性能测试面板** 中，可以找到一个“世界速度”滑块。调低它，只会让世界（工厂、运输机、建造无人机等）的运行速度变慢，而你的机甲移动和建造等操作则维持正常速度。
  - **适用场景：** 在后期超大型工厂导致帧率（FPS）下降时，降低世界速度可以减轻 CPU 负担，让游戏运行更流畅。或是享受“黑客帝国”一般，周围一切慢动作的感觉。
  - **速度为 0 时：** 这会创造一个独特的时停效果，所有工厂都将停止运作，但你依然可以自由行动。

## 其他实用功能

  - **后台自动存档：** 将自动存档功能放到后台线程执行，避免游戏存档时停止回应。为了确保数据一致，存档期间游戏会短暂进入时停模式（机甲可移动），阻止机甲和工厂的互动。这项功能有一定的风险，使用前建议先测试。  
  - **快速载入主菜单：** 跳过主菜单界面的星球模型渲染，加快进入游戏主菜单的速度。
  - **减少卡顿：** 提供移除建筑工具强制内存回收（GC）的选项，有助于缓解在放置大量建筑时可能发生的瞬间卡顿。

## 如何使用

建议透过模组管理器[r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/)或[GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/)安装。  

  - **速度控制：** 点击屏幕右下角的按钮进行暂停、恢复（或重置为1倍速）和加速。
  - **战术暂停：** 按下 `Pause` / `Break` 键切换。
  - **子弹时间滑块：** 打开游戏内的“性能测试”面板，即可在上方找到并调整“世界速度”滑块。
  - **后台自动保存**：打开性能统计面板，在右上角的选框上启用后台自动保存。
  - **查看性能：** 按 `Shift + F12` 可以在左上角查看实时的 FPS 和 UPS。

## 组态设定

安装 Mod 并运行过一次游戏后，将会生成配置文件。你可以通过以下两种方式修改：

  - **通过Mod管理器（推荐）：** 在你的 Mod 管理器中（如 r2modman），于左侧菜单进入“Config editor”，找到 `com.starfi5h.plugin.BulletTime` 并点击“Edit Config”。
  - **手动修改：** 前往游戏根目录下的 `BepInEx\config\` 文件夹，用文本编辑器打开 `com.starfi5h.plugin.BulletTime.cfg` 文件。

*所有修改将在重启游戏后生效。*

| 选项名称 | 功能描述 | 默认值 |
|---|---|---|
| `KeyAutosave` | 触发自动存档的热键。 | `F10 + LeftShift` |
| `KeyPause` | 切换战术暂停模式的热键。 | `Pause`｜`Break` |
| `KeyStepOneFrame` | 在暂停模式下，让游戏前进1帧的热键。 | `None` |
| `EnableMechaFunc` | 设为 true 时，你的机甲能在战术暂停模式中移动。 | `false` |
| `EnableBackgroundAutosave` | 在后台执行自动存档。 | `false` |
| `EnableHotkeyAutosave` | 允许用热键触发自动存档。 | `false` |
| `StartingSpeed` | 游戏开始时的默认世界速度（百分比）。 | `100` |
| `EnableFastLoading` | 跳过标题画面的星球渲染以加快载入。 | `true` |
| `RemoveGC` | 移除建筑工具的强制内存回收以减少卡顿。 | `true` |
| `MaxSpeedupScale` | 加速按钮的最大游戏速度倍率。 | `10` |
| `MaxSimulationSpeed` | 在外太空时,可以shift+点击快速达到此指定倍率 | `10.0` |

## 联机功能 (Nebula)

  - 当有玩家加入或主机存档时，游戏会自动进入时停模式（此模式下玩家只可移动），并在完成后自动恢复。
  - 如果游戏意外卡在时停模式，主机可以通过拖动世界速度滑块来手动恢复游戏运行。
  - 主机和客户端都可以使用暂停热键。加速功能只有在联机选项“SyncUPS”启用时才会同步。
  - 玩家可以在戴森球编辑器中暂停或恢复戴森球的旋转。

## 相容性

  - [✅] GalacticScale
  - [✅] NebulaMultiplayer

-----

#### Acknowledgements

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.
\<a href="https://www.flaticon.com/free-icons/pause-button" title="pause-button icons"\>Pause-button icons created by Uniconlabs - Flaticon\</a\>
Speed button UI desgin from [DspGameSpeed](https://thunderstore.io/c/dyson-sphere-program/p/dsp-mods/DspGameSpeed/)