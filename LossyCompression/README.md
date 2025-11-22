# Lossy Compression (有损压缩)

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/LossyCompression/img/demo1.jpg)

Compress belt & cargo data (-80%), dyson shells (-85%), and solar sails to drastically reduce save file size and improve I/O performance.  
Includes **Lazy Loading** and **Multi-threading** to optimize RAM usage and loading speed.

提供传送带(-80%), 戴森壳(-85%)及太阳帆的存档数据压缩。在性能测试面板可以启用/停用压缩。   
游戏改版之前建议另存原档避免mod失效。若游戏改版后有无法正常读取的压缩存档, 请先回滚游戏版本再储存成未压缩(原版)存档。  

> [!WARNING]
> **BACKUP YOUR SAVE BEFORE USE! / 使用前务必备份存档！**  
>  
> While Dyson Shell/Swarm compression is safe (stored externally), enabling **CargoPath (Belt) compression** modifies the internal save structure.  
> Saves created with CargoPath compression **CANNOT** be loaded without this mod.
> 
> 戴森壳/太阳帆压缩是安全的（外部存储），但启用 **CargoPath (传送带) 压缩** 会修改存档内部结构。
> 启用了传送带压缩的存档，**必须**安装本 Mod 才能读取，否则存档将损坏。

## Configuration (设置)

Run the game once to generate `BepInEx\config\starfi5h.plugin.LossyCompression.cfg`.

### ✅ Safe: Independent Data (安全功能：独立数据)

- **`DysonShell`** (Default: `true`)
    - **Effect:** **Lossless** compression (blueprint only). Geometry is rebuilt on load using CPU multithreading.
    - **Storage:** Data saved in a separate `.moddsv` file.
    - **Safety:** If mod is removed, the save loads fine, but **all Dyson Shells will vanish**.
    - **效果:** **无损**压缩戴森壳（仅保存蓝图）。载入时利用 CPU 重建模型。
    - **机制:** 数据存在独立的 `.moddsv` 文件中。若移除 Mod，存档可读取，但**戴森壳会消失**。

- **`DysonSwarm`** (Default: `true`)
    - **Effect:** **Lossy** compression (statistical distribution). Exact positions are not saved.
    - **Side Effect:** Sails are "regenerated" on load. Their positions will be randomized/reset each time.
    - **Safety:** Same as above. If removed, **all Solar Sails will vanish**.
    - **效果:** **有损**压缩太阳帆（保存统计分布）。不保存精确位置。
    - **副作用:** 每次读档时太阳帆位置会重置/随机生成。
    - **机制:** 若移除 Mod，存档可读取，但**太阳帆会消失**。

### ⚠️ Risk: Save Dependency (风险功能：存档依赖)

- **`CargoPath`** (Default: `false`)
    - **Effect:** **Lossy** compression for belts/cargo. Positions/rotations are slightly simplified.
    - **Risk:** **PERMANENT DEPENDENCY.** The compressed data is written directly into the main save file. If you uninstall the mod, the save will break unless you disable this option and re-save first.
    - **效果:** **有损**压缩传送带数据。位置和方向会些微简化。
    - **风险:** **永久依赖**。数据直接写入主存档。启用后，存档必须依赖此 Mod 才能开启。

### ⚡ Performance & RAM (性能与内存)

- **`MultiThreading`** (Default: `true`)
    - **Effect:** Uses multiple CPU threads to rebuild Shells (4x faster loading).
    - **Trade-off:** Significantly higher **RAM usage** during the loading screen.
    - **效果:** 启用多线程加速模型重建 (载入速度 4x)。代价是载入时内存 (RAM) 占用峰值更高。

- **`LazyLoad`** (Default: `true`)
    - **Effect:** Only generates shell models when you are viewing them or opening the editor.
    - **Benefit:** Massively reduces RAM usage for stars you are not currently looking at.
    - **效果:** **延迟载入**。只有在看见戴森球或打开面板时才生成模型。大幅降低闲置星系的内存占用。

- **`ReduceRAM`** (Default: `false`)
    - **Effect:** Aggressively deletes shell models from RAM immediately after saving.
    - **Trade-off:** Lowest RAM usage, but causes **CPU lag spikes during auto-save** (needs to regenerate vanilla data) and requires rebuilding models when viewing the sphere again.
    - **效果:** 存档后立即从内存删除模型。极致省内存，但会导致**存档时卡顿**（需临时生成原版数据），且再次查看时需重新生成。

## In-Game Controls (游戏内控制)

- **Performance Panel:** A toggle button `Compress - ON/OFF` is added to the performance test panel.
- **Usage:** You can switch compression modes dynamically. The setting applies to the **next save operation**.
- **性能面板:** 性能测试面板中新增 `Compress - ON/OFF` 按钮。
- **用法:** 可实时切换。设置将应用于**下一次存档**操作。

## Uninstall Guide (卸载指南)

1.  **If `CargoPath` is OFF (Default):**
    - Safe to remove directly. *Note: Shells/Sails in `.moddsv` will be lost.*
    - 若未启用传送带压缩：直接删除即可。*注意：储存在 `.moddsv` 中的壳/帆会消失。*

2.  **If `CargoPath` is ON:**
    - **DO NOT DELETE MOD YET.**
    - Load game -> Disable compression in config/UI -> **Save Game**.
    - Now it is safe to remove the mod.
    - 若已启用传送带压缩：**切勿直接删除。**
    - 进游戏 -> 关闭压缩功能 -> **保存游戏**（转回原版格式） -> 然后再删除 Mod。

## Compatibility (兼容性)

- (✅) [NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/): **Supported.** Syncs compressed data to clients, reducing network lag. (支持联机，大幅减少数据传输量)
- (✅) [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- (✅) [CompressSave](https://dsp.thunderstore.io/package/soarqin/CompressSave/)
- (⚠️) [SphereOpt](https://dsp.thunderstore.io/package/Andy/SphereOpt/): Will automatically disable `LazyLoad` to avoid conflict. (会自动关闭延迟载入以避免冲突)

----

#### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.