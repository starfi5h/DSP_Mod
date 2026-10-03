# NoGlobeClick

Prevents right-clicking on the globe map from placing a location marker unless Shift is held.  
防止在星球视图右键放置定位标记，改为只有按住Shift时才会生效。  
  
Fixes a God Mode issue where WASD cancels the current movement command.  
修复上帝模式下WASD会意外取消移动指令的问题。  

## Features / 功能特点

* **Prevent Accidental Globe Clicks (防止星球视图误操作)**

  * Right-clicking on the planet globe map will no longer mark location **unless you hold Shift**.

  * 在星球视图中右键点击时，将不再触发定位标记，**除非按住 Shift 键**。

* **Disable WASD Order Interruption in God Mode (上帝模式 WASD 不打断指令)**

  * Pressing movement keys (WASD) in God mode will no longer cancel your current queued orders.

  * 在 上帝模式 按下方向键（WASD）时，不会再自动打断已排队的移动指令。

## Installation / 安装说明

### Via Mod Manager (Recommended) / 使用 Mod 管理器（推荐）

1. Install via [r2modman](https://dsp.thunderstore.io/package/ebony/r2modman/) or Thunderstore Mod Manager.

2. Search for **NoGlobeClick** and click **Download**.

### Manual Installation / 手动安装

1. Install [BepInEx](https://dsp.thunderstore.io/package/xiaoye97/BepInEx/).

2. Drag `NoGobleClick.dll` into your `BepInEx/plugins` directory.