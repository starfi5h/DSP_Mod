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

#### 4\. Preset Speed Slots (Hotkeys)
  - **What it does:** Four hotkeys switch between speed presets: `Pause` toggles the tactical pause, `Ctrl+Home` resets to standard 1x speed (100%) and memorizes the current slider value, `Ctrl+End` restores that memorized slider speed, and `Ctrl+Ins` restores the last frame-rate setting. All of them can be changed in the config (`KeyPause` / `KeyFullSpeed` / `KeySliderSpeed` / `KeyFrameRate`).
  - **Note:** Pressing any non-pause preset key while in tactical pause first releases the pause. Pressing `Pause` again resumes exactly the speed that was active before the pause (frame-rate presets restore their exact value). The presets are unavailable during the background-autosave freeze, and in multiplayer they are host-only.

#### 5\. Speed Indicator
  - **What it does:** The bottom-right speed indicator shows the actual speed together with the frame speed below the speed buttons (for example `60F/s 1x`); it is hidden while paused.

## Other Utility Features

  - **Background Autosave:** Runs the autosave process on a background thread to prevent the game from freezing. The game will briefly enter the bullet-time pause mode (mecha can still move) to ensure data consistency during the save.  
  Note: This function has risk potential, so it's better to test it first.  
  - **Fast Main Menu Loading:** Speeds up loading into the main menu by skipping the 3D planet model rendering on the title screen.
  - **Reduce Stuttering:** Includes an option to remove the game's forced garbage collection (GC) when using build tools, which can help reduce stuttering when placing many buildings.
  - **Save Hotkey:** You can bind hotkeys for immediate save (`KeyImmediateSave`). The immediate-save hotkey writes a save instantly and reports the result in the log.
  - **Customizable Pause Status Text:** The status label shown while in Tactical Pause mode can be changed via `StatusTextPause`, and its vertical position relative to the autosave text can be adjusted with `StatusTextHeightOffset`.


## How to Use

It is recommended to install it through [r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/) or [GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/).  

  - **Speed Controls:** Use the buttons in the bottom-right corner to Pause, Resume (or reset to 1x), and Speed Up. You can also assign hotkeys (`KeySpeedUp` / `KeySpeedDown`) for the same actions.
  - **Tactical Pause:** Press the `Pause` / `Break` key to toggle.
  - **Bullet-Time Slider:** Open the Performance Statistics panel to find and adjust the World Speed slider.
  - **Preset Speed Slots:** `Pause` tactical pause / `Ctrl+Home` 100% (memorize slider) / `Ctrl+End` memorized slider speed / `Ctrl+Ins` frame-rate preset.
  - **Background Autosave Toggle:** Open the Performance Statistics panel to enable background autosave on the top-right checkbox.  
  - **View Performance:** Press `Shift + F12` to see the real-time FPS and UPS in the top-left corner.

## Configuration

After running the game once with the mod installed, a config file will be generated. You can edit it in two ways:

  - **Via Mod Manager (Recommended):** In your mod manager (e.g., r2modman), go to the "Config editor" section, find `com.starfi5h.plugin.BulletTime`, and click "Edit Config".
  - **Manually:** Navigate to `BepInEx\config\` in your game folder and open the `com.starfi5h.plugin.BulletTime.cfg` file with a text editor.

*Changes will take effect after you restart the game.*

| Key | Description | Default |
|---|---|---|
| `KeyAutosave` | Keyboard shortcut for scheduling auto save. | `F10 + LeftShift` |
| `KeyImmediateSave` | Keyboard shortcut for immediate auto save. | `F10 + LeftControl + LeftAlt` |
| `KeySpeedUp` | Keyboard shortcut for speed up. | `None` |
| `KeySpeedDown` | Keyboard shortcut for speed down. | `None` |
| `KeyPause` | Hotkey for toggling Tactical Pause mode. | `Pause`｜`Break` |
| `KeyStepOneFrame` | Hotkey to advance 1 frame in pause mode. | `None` |
| `KeyFullSpeed` | Hotkey for the 100% speed preset (standard 1x speed). | `Ctrl + Home` |
| `KeyFrameRate` | Hotkey for the frame-rate preset (restore the last frame-rate setting). | `Ctrl + Ins` |
| `KeySliderSpeed` | Hotkey to restore the memorized slider speed. | `Ctrl + End` |
| `EnableMechaFunc` | If true, your mecha can move in Tactical Pause mode. | `false` |
| `EnableBackgroundAutosave` | Run autosave in the background. | `false` |
| `EnableHotkeyAutosave` | Enable hotkey to trigger autosave. | `false` |
| `StartingSpeed` | Game speed (in percent) when the game begins. | `100` |
| `FrameRate` | Actual speed of the frame-rate preset, in frames per second (0.06-6000 = 0.001x-100x); kept across saves. | `60` |
| `SliderSpeed` | Memorized slider speed of the slider preset, in percent (1-100); updated automatically by Ctrl+Home. | `100` |
| `EnableFastLoading` | Skip planet rendering on title screen for faster loading. | `true` |
| `RemoveGC` | Remove force garbage collection of build tools to reduce stutter. | `true` |
| `MaxSpeedupScale` | Maximum game speed multiplier for the speedup button. | `10` |
| `MaxSimulationSpeed` | In outer space, shift-click to set the simulation speed to this value. | `10.0` |
| `StatusTextHeightOffset` | Height of Status text relative to auto save text. | `100` |
| `StatusTextPause` | Status text when in pause mode. | `Bullet Time` |

## Multiplayer Features (Nebula)

  - When a player is joining or the host is saving, the game will enter a pause mode (where players can only move) and resume automatically when finished.
  - If the game gets stuck in pause mode, the host can manually resume it by moving the World Speed slider.
  - Both host and clients can use the pause hotkey. Game speedup is only synced if the "SyncUPS" multiplayer option is enabled.
  - The preset speed-slot hotkeys and the world speed slider are host-only.
  - Players can stop/resume Dyson Sphere rotation in the editor.

## Compatibility

  - [✅] GalacticScale
  - [✅] NebulaMultiplayer
  - [✅] LDBTool (require NebulaAPI installed too)

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

#### 4\. 预设档位热键
  - **功能介绍：** 四个热键在速度档位之间切换：`Pause` 切换战术暂停，`Ctrl+Home` 恢复标准 1 倍速（100%）并自动记忆当前滑块值，`Ctrl+End` 恢复记忆的滑块值，`Ctrl+Ins` 恢复上次帧速率设定值。所有按键都可在配置文件中修改（`KeyPause` / `KeyFullSpeed` / `KeySliderSpeed` / `KeyFrameRate`）。
  - **暂停恢复：** 再次按下 `Pause` 解除暂停时，精确回到暂停前的速度——帧速率档恢复精确的帧速率设定值，不退化为滑块等效百分比。
  - **请注意：** 在战术暂停中按下任意非暂停档热键，会先解除暂停再切换档位。后台自动存档时停期间预设档位不可用；联机时预设档位仅主机可用。

#### 5\. 速度指示
  - **功能介绍：** 右下角速度指示显示在加速按钮下方，同时显示实际速度与帧速度（例如 `60F/s 1x`）；暂停时隐藏。

## 其他实用功能

  - **后台自动存档：** 将自动存档功能放到后台线程执行，避免游戏存档时停止回应。为了确保数据一致，存档期间游戏会短暂进入时停模式（机甲可移动），阻止机甲和工厂的互动。这项功能有一定的风险，使用前建议先测试。  
  - **快速载入主菜单：** 跳过主菜单界面的星球模型渲染，加快进入游戏主菜单的速度。
  - **减少卡顿：** 提供移除建筑工具强制内存回收（GC）的选项，有助于缓解在放置大量建筑时可能发生的瞬间卡顿。
  - **存档热键：** 除了画面上的按钮外，你还可以自行绑定「立即存档」热键（`KeyImmediateSave`）。立即存档热键会立刻写入存档，并在日志中输出保存结果。
  - **自定义暂停提示文字：** 战术暂停模式中显示的状态文字可通过 `StatusTextPause` 修改；它与自动存档提示之间的垂直高度差可用 `StatusTextHeightOffset` 调整。


## 如何使用

建议透过模组管理器[r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/)或[GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/)安装。  

  - **速度控制：** 点击屏幕右下角的按钮进行暂停、恢复（或重置为1倍速）和加速。你也可以为这些操作绑定热键（`KeySpeedUp` / `KeySpeedDown`）。  
  - **战术暂停：** 按下 `Pause` / `Break` 键切换。
  - **子弹时间滑块：** 打开游戏内的“性能测试”面板，即可在上方找到并调整“世界速度”滑块。
  - **预设档位热键：** `Pause` 战术暂停 / `Ctrl+Home` 100%（记忆滑块）/ `Ctrl+End` 记忆的滑块速度 / `Ctrl+Ins` 帧速率档。
  - **后台自动保存**：打开性能统计面板，在右上角的选框上启用后台自动保存。
  - **查看性能：** 按 `Shift + F12` 可以在左上角查看实时的 FPS 和 UPS。

## 组态设定

安装 Mod 并运行过一次游戏后，将会生成配置文件。你可以通过以下两种方式修改：

  - **通过Mod管理器（推荐）：** 在你的 Mod 管理器中（如 r2modman），于左侧菜单进入“Config editor”，找到 `com.starfi5h.plugin.BulletTime` 并点击“Edit Config”。
  - **手动修改：** 前往游戏根目录下的 `BepInEx\config\` 文件夹，用文本编辑器打开 `com.starfi5h.plugin.BulletTime.cfg` 文件。

*所有修改将在重启游戏后生效。*

| 选项名称 | 功能描述 | 默认值 |
|---|---|---|
| `KeyAutosave` | 触发排程自动存档的热键。 | `F10 + LeftShift` |
| `KeyImmediateSave` | 立即自动存档的热键。 | `F10 + LeftControl + LeftAlt` |
| `KeySpeedUp` | 加速的热键。 | `None` |
| `KeySpeedDown` | 减速的热键。 | `None` |
| `KeyPause` | 切换战术暂停模式的热键。 | `Pause`｜`Break` |
| `KeyStepOneFrame` | 在暂停模式下，让游戏前进1帧的热键。 | `None` |
| `KeyFullSpeed` | 切换到 100% 速度档（标准 1 倍速）的热键。 | `Ctrl + Home` |
| `KeyFrameRate` | 切换到帧速率档的热键（恢复上次帧速率设定值）。 | `Ctrl + Ins` |
| `KeySliderSpeed` | 切换到滑块减速模式的热键（恢复记忆的滑块值）。 | `Ctrl + End` |
| `EnableMechaFunc` | 设为 true 时，你的机甲能在战术暂停模式中移动。 | `false` |
| `EnableBackgroundAutosave` | 在后台执行自动存档。 | `false` |
| `EnableHotkeyAutosave` | 允许用热键触发自动存档。 | `false` |
| `StartingSpeed` | 游戏开始时的默认世界速度（百分比）。 | `100` |
| `FrameRate` | 帧速率档的实际设定速率，单位为帧每秒（0.06-6000，对应 0.001x-100x）。游戏内调整后自动保存，跨存档保持。 | `60` |
| `SliderSpeed` | 滑块减速模式的速度百分比（1-100）。按 Ctrl+Home 时自动记忆当前滑块值，自动保存，跨存档保持 | `100` |
| `EnableFastLoading` | 跳过标题画面的星球渲染以加快载入。 | `true` |
| `RemoveGC` | 移除建筑工具的强制内存回收以减少卡顿。 | `true` |
| `MaxSpeedupScale` | 加速按钮的最大游戏速度倍率。 | `10` |
| `MaxSimulationSpeed` | 在外太空时,可以shift+点击快速达到此指定倍率 | `10.0` |
| `StatusTextHeightOffset` | 状态提示相对于自动存档提示的高度。 | `100` |
| `StatusTextPause` | 暂停时的状态提示文字。 | `Bullet Time` |

## 联机功能 (Nebula)

  - 当有玩家加入或主机存档时，游戏会自动进入时停模式（此模式下玩家只可移动），并在完成后自动恢复。
  - 如果游戏意外卡在时停模式，主机可以通过拖动世界速度滑块来手动恢复游戏运行。
  - 主机和客户端都可以使用暂停热键。加速功能只有在联机选项“SyncUPS”启用时才会同步。
  - 预设档位热键与世界速度滑块仅主机可用。
  - 玩家可以在戴森球编辑器中暂停或恢复戴森球的旋转。

## 相容性

  - [✅] GalacticScale
  - [✅] NebulaMultiplayer
  - [✅] LDBTool （如果安装了此mod, 那就需要同时安装 NebulaAPI 避免报错）

-----

#### Acknowledgements

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.
\<a href="https://www.flaticon.com/free-icons/pause-button" title="pause-button icons"\>Pause-button icons created by Uniconlabs - Flaticon\</a\>
Speed button UI desgin from [DspGameSpeed](https://thunderstore.io/c/dyson-sphere-program/p/dsp-mods/DspGameSpeed/)