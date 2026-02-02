# Sample and Hold Simulation  

This mod reduces the computational load of factories on planets where the player is not currently present. By changing how simulations update, significantly increase UPS (Updates Per Second) in late-game saves where the CPU struggles to simulate all planets simultaneously.  

## [模擬帧 - 修改游戏运算方式以提升逻辑帧率](https://b23.tv/BV1oB4y1X78J)
这个 Mod 是一个性能优化模组，旨在大幅降低玩家非所在星球上工厂的运算压力。通过改变游戏模拟机制，大幅提升大后期存档的 UPS（逻辑帧率），解决由于工厂规模过大导致的卡顿问题。 

## UI 
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/SampleAndHoldSim/img/demo.gif)  

The mod main control panel is on the statistics panel (P) - performance test.  
Ratio - The remote planets are updated every x ticks. The input field can set the value higher than 20.   
Focus Local - When enabled, the local planet will always be active. It can give a better gaming experience.  
**Warning: Because this mod manipulates stats data and item generation, it may disable Milkyway upload.**  

使用者可以在性能测试面板设定倍率(Ratio),决定远端星球更新的周期。在输入框可以输入超过20的值。  
勾选Focus Local可让本地工厂保持运行来维持游戏体验。  
**警告: 此mod会改动统计资料和虛空產物。可能会让存档无法上传银河系。**  

![UI](https://raw.githubusercontent.com/starfi5h/DSP_Mod/dev/SampleAndHoldSim/img/UI1.jpg)  
Additional UI for displaying flow-in/flow-out rate of cargo in stations. Configurable in options.  

## Difference with Base Game

The difference between vanilla behavior:  
- Stations' storage may have a negative item count to maintain the cargo balance.
- Lancers' attack damage will be scaled down in remote systems.  
- Turrets that attack Dark Fog space units will be scaled up damage in remote systems.  
- Dark Fog drops on remote planets will exist longer for BAB to pick up.  

和原版不同之处:  
- 为了保持内部与外部间的物品记帐平衡, 物流塔内的货物数量可能为负数  
  
目前对战斗系统进行了以下的修改, 可能会影响平衡:  
- 远程星系的枪骑攻击力依照倍率缩减, 避免远程星系的星球被攻破  
- 远程星球的导弹和电浆炮伤害依照倍率增加, 但可能有溢伤的问题  
- 远程星球的黑雾掉落会存在更久，好让战场分析基站可以即时捡取 

## Configuration

Run the game one time to generate `BepInEx\config\starfi5h.plugin.SampleAndHoldSim.cfg` file.  
You can also find it in mod manager's config editor.  

- `UpdatePeriod` (Default:`10`)  
Compute actual factory simulation every x ticks.  

- `SliderMaxUpdatePeriod` (Default:`20`)  
Max value of upate period slider.  

- `UIStationStoragePeriod` (Default:`600`)  
Display item flow in/out from belts in station storages in x ticks. 0 = no display  

- `UnitPerMinute` (Default:`false`)  
If true, show the rate per minute. otherwise, show the rate per second.  

- `EnableRelayLanding` (Default:`true`)  
If true, allow Dark Fog relays to land on planet (vanilla).  

## 設置
.cfg文件需要先运行过游戏一次才会出现，修改后要重启游戏才会生效。  
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.SampleAndHoldSim` -> Edit Config  
手动安装: 更改`BepInEx\config\starfi5h.plugin.SampleAndHoldSim.cfg`文件  
  
- `UpdatePeriod` (默认:`10`)  
更新周期: 每x逻辑帧运行一次实际计算    

- `SliderMaxUpdatePeriod` (默认:`20`)  
更新周期滑动条的最大值  

- `UIStationStoragePeriod` (默认:`600`)  
显示过去x帧内物流塔经传送带的货物的流入或流出速率, 0 = 不显示  

- `UnitPerMinute` (默认:`false`)  
true: 显示单位设为每分钟速率 false: 显示每秒速率  

- `EnableRelayLanding` (默认:`true`)  
true: 允许黑雾中继器登陆星球(原版逻辑) false: 不允许  

## Compatibility  

(✅) [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI/)  
(✅) [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)  
(✅) [NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) - SampleAndHoldSim will be diabled in client mode. Only host can use it.  
(🛠️) [CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/) - Fix the error when enabling 'skip bullet' function. Need to restart the game after switching.  
(🛠️) [Auxilaryfunction](https://thunderstore.io/c/dyson-sphere-program/p/blacksnipebiu/Auxilaryfunction/) - Temporarily set ratio to 1 when stopping factory.  
(⚠️) [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner/) - Fix mine rate on idle factories, let it not be affected by FPS.   
(⚠️) [GenesisBook](https://thunderstore.io/c/dyson-sphere-program/p/HiddenCirno/GenesisBook/) - Quantum box may have abnormal behavior.  
(⛔) [Multfuntion mod](https://dsp.thunderstore.io/package/blacksnipebiu/Multfuntion_mod/) - Some game-breaking features are not compatible.  
(⛔) [Weaver](https://thunderstore.io/c/dyson-sphere-program/p/Loom/Weaver/) - Not compatible.

## MOD相容性:
(✅) [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)(优化mod)  
(✅) [NebulaMultiplayer](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)(联机mod) - 只有主机可使用, 客户端会自动停用并关闭介面  
(🛠️) [CheatEnabler](https://dsp.thunderstore.io/package/soarqin/CheatEnabler/) - 修复启用'跳过子弹阶段'时造成的冲突。切换设置后需重启游戏才会套用兼容  
(🛠️) [Auxilaryfunction](https://thunderstore.io/c/dyson-sphere-program/p/blacksnipebiu/Auxilaryfunction/) - 在停止工厂时会将倍率暂时调为1避免产物异常    
(⚠️) [PlanetMiner](https://dsp.thunderstore.io/package/blacksnipebiu/PlanetMiner/) - 修复星球矿机速率不正确的问题, 使其不随FPS变动。    
(⚠️) [GenesisBook](https://thunderstore.io/c/dyson-sphere-program/p/HiddenCirno/GenesisBook/) - 量子箱的跨星球运输可能会异常。  
(⛔) [Multfuntion mod](https://dsp.thunderstore.io/package/blacksnipebiu/Multfuntion_mod/)(多功能OPmod) - 兼容问题: 在跳过子弹时,太阳帆的数量没有被倍增。其他某些改机制功能(星球矿机等)不兼容。  
(⛔) [Weaver](https://thunderstore.io/c/dyson-sphere-program/p/Loom/Weaver/) - 不兼容。  

----
# FAQ / 常见问题

## General Questions / 一般问题

### Q: What does this mod actually do? / 这个mod到底做什么？

**EN:** This mod improves game performance by reducing how often distant factories are calculated. Instead of updating every factory every tick, it updates them in rotation (e.g., every 10 ticks) and multiplies their input/output accordingly. Think of it like turning down the simulation frequency for planets you're not watching, while keeping the overall production the same.

**CN:** 这个mod通过减少远程工厂的计算频率来提高游戏性能。它不是每帧更新所有工厂，而是轮流更新（例如每10帧更新一次），并相应地倍增输入/输出。可以理解为降低你不关注的星球的模拟频率，同时保持总体产量不变。

---

### Q: Will this mod break my save file? / 这个mod会破坏我的存档吗？

**EN:** No, the mod is safe to add and remove from existing saves. However:
- Your save may become ineligible for Milky Way leaderboard uploads due to inaccuracy by approximate simulation  
- Always backup your saves before using any mod

**CN:** 不会，该mod可以安全地添加到现有存档或从中移除。但是：
- 由于数据操作的累积误差过大时，你的存档可能无法上传到银河系排行榜
- 使用任何mod前请务必备份存档

---

### Q: How much performance improvement can I expect? / 我能期待多大的性能提升？

**EN:** The more planets you have, the greater the benefit. Late-game megabases with 20+ developed planets see the more improvements.  
On the other hand, it doesn't have much improvement with early game <4 developed planets.  
Higher Ratio = more performance, less accuracy. Choose based on your tolerance for "spiky" behavior.  
For example, if your "Planet Factroy" takes 80% of the time, then setting ratio to 20 will make about 4x UPS.  
Adjust the ratio to have stable 60 logical frames per second. It's recommended not to set it above 100.  

| Aspect | Ratio 10 | Ratio 20 |
|--------|-----------|-----------|
| Accuracy | High | Medium |
| Statistics | Slightly sparse | Very sparse |
| Internal storage lag | Low | Noticeable |

**CN: 拥有的星球越多，收益越大。拥有20+开发星球的后期存档会看到显著的改善。  
相对地，对于早期游戏（已开发星球<4）来说，它并没有太大的改进。  
Ratio更高 = 更多性能，更低准确性。根据你对"波动"行为的容忍度选择。  
举例来说，如果你的“星球工厂”占用 80% 的时间，那么将Ratio设置为 20 将使 UPS 提高至约 4 倍。  
Ratio调到可以稳60逻辑帧就好。建议不要设超过100。  

| 方面 | Ratio 10 | Ratio 20 |
|------|---------|---------|
| 准确性 | 高 | 中 |
| 统计 | 略显稀疏 | 非常稀疏 |
| 内部存储延迟 | 少 | 明显 |

---

## Setup & Configuration / 设置与配置

### Q: Should I enable "Focus Local"? / 我应该启用"专注本地"吗？

**EN:** Yes, for a better user experience  
- Keeps your current planet running at full speed
- Maintains responsive gameplay when building/adjusting factories
- Other planets still benefit from reduced update frequency
- Downside: slight item residue when switching planets
Turn it off only if you want pure mathematical consistency across all planets.

**CN:** 是的，为了更好的游玩体验  
- 保持当前星球全速运行
- 建造/调整工厂时保持响应流畅
- 其他星球仍受益于降低的更新频率
- 缺点：切换星球时可能有轻微的物品残留
只有在需要所有星球纯粹数学一致性时才关闭。或着想观察闲置星球是怎么更新的。

---

### Q: How do I access the main mod settings? / 如何访问mod设置？

**EN:**
1. Press **P** to open Performance Statistics window
2. Look for the "Ratio" slider on the right side
3. Adjust the slider (1-20) or type a custom value in the input box
4. Toggle "Focus Local" checkbox as needed

Settings take effect immediately without restarting the game.

**CN:**
1. 按 **P** 打开性能统计窗口
2. 在右侧找到"Ratio"（更新周期）滑块
3. 调整滑块（1-20）或在输入框中输入自定义值
4. 根据需要切换"Focus Local"（专注本地）复选框

设置立即生效，无需重启游戏。

---

## Gameplay Impact / 游戏影响

### Q: Will my production rates be accurate? / 我的生产速率会准确吗？

**EN:** **Yes, in the long term.** 
- Short-term statistics may look "spiky" or uneven
- Over minutes, average production matches vanilla
- Logistics station flow rates remain consistent

The mod sacrifices short-term accuracy for long-term consistency and performance.

**CN:** **是的，长期来看。**
- 短期统计可能看起来"波动"或不均匀
- 经过几分钟后，平均产量与原版一致
- 物流塔流速保持一致

该mod牺牲短期准确性以换取长期一致性和性能。

---

### Q: Why do my logistics stations show negative item counts? / 为什么我的物流塔显示负数物品数量？

**EN:** This is **normal and expected behavior**. 
- Stations can temporarily go negative to maintain cargo balance
- The mod has built-in safeguards (default minimum: -64 × Update Period)
- Negative counts resolve naturally as logistics ships deliver items
- Does not affect actual item availability or production

Think of it as "credit" - the station owes items that will be delivered soon.

**CN:** 这是**正常且预期的行为**。
- 物流塔可以暂时为负数以保持货物平衡
- mod有内置保护机制（默认最小值：-64 × 更新周期）
- 负数会随着物流飞船运送物品自然解决
- 不影响实际物品可用性或生产

可以理解为"借用" - 物流塔欠的物品很快会被运送过来。

---

### Q: How does this affect Dyson Sphere construction? / 这如何影响戴森球建造？

**EN:** Dyson Sphere construction works normally:
- Solar sails are launched in batches when factories update
- Rockets are properly queued and delivered to nodes
- Construction speed matches vanilla over time
- Energy generation from completed shells is unaffected

You may see sails/rockets launch in bursts rather than continuously, but total throughput is identical.

**CN:** 戴森球建造正常工作：
- 工厂更新时太阳帆批量发射
- 火箭正确排队并运送到节点
- 建造速度随时间推移与原版一致
- 已完成壳层的能量产生不受影响

你可能会看到帆/火箭突发式发射而非连续发射，但总吞吐量相同。

---

### Q: What about Dark Fog combat? / 黑雾战斗怎么办？

**EN:** Combat mechanics are adjusted automatically:
- **Enemy damage** (Lancers) is scaled DOWN on remote planets to prevent destruction
- **Your damage** (turrets, missiles) is scaled UP to compensate
- **Drops** last longer on remote planets so you can collect them
- Focus Local planets have normal combat behavior

Combat balance may differ from vanilla - backup saves and report issues!

**CN:** 战斗机制会自动调整：
- **敌方伤害**（枪骑兵）在远程星球上按比例降低以防止被摧毁
- **你的伤害**（炮塔、导弹）按比例提高以补偿
- **掉落物**在远程星球上持续更长时间以便收集
- 专注本地星球具有正常战斗行为

战斗平衡可能与原版不同 - 请备份存档并报告问题！

---

## Troubleshooting / 故障排除

### Q: My statistics look weird/inconsistent / 我的统计数据看起来很奇怪/不一致

**EN:** This is usually cosmetic:
- Production graphs will appear "stepped" or sparse
- Wait 5-10 minutes for statistics to stabilize
- Verify total items produced/consumed over longer periods

If numbers are drastically wrong), report as a bug.

**CN:** 这通常只是视觉问题：
- 生产图表会显示"阶梯状"或稀疏
- 等待5-10分钟让统计数据稳定
- 验证较长时期内的总产量/消耗量

如果数字严重错误，请报告为bug。

---

### Q12: Some items are backing up/not producing / 某些物品堆积/不生产

**EN:** Check these common causes:
1. **Belt bottlenecks**: Slowed factory internals may reveal existing bottlenecks
2. **Unbalanced production**: Temporary imbalances resolve over time
3. **Logistics configuration**: Verify station settings for remote/local supply
4. **Update period too high**: Try reducing ratio to 5-10

Most issues resolve within a few production cycles.

**CN:** 检查这些常见原因：
1. **传送带瓶颈**：变慢的工厂内部可能暴露现有瓶颈
2. **生产不平衡**：临时不平衡会随时间解决
3. **物流配置**：验证物流塔的远程/本地供应设置
4. **更新周期过高**：尝试降低Ratio到5-10

大多数问题会在几个生产周期内解决。

---

### Q13: The mod isn't working / Mod不工作

**EN:** Troubleshooting steps:
1. Verify BepInEx is installed correctly (see console on game startup)
2. Check `BepInEx/LogOutput.log` for errors mentioning SampleAndHoldSim
3. Ensure config file exists: `BepInEx/config/starfi5h.plugin.SampleAndHoldSim.cfg`
4. Try setting Update Period to 1 (vanilla behavior) to test if mod loads
5. Check mod compatibility list - some mods conflict

If issues persist, report with log files and mod list.

**CN:** 故障排除步骤：
1. 验证BepInEx正确安装（启动游戏时查看控制台）
2. 检查 `BepInEx/LogOutput.log` 中提到SampleAndHoldSim的错误
3. 确保配置文件存在：`BepInEx/config/starfi5h.plugin.SampleAndHoldSim.cfg`
4. 尝试将更新周期设为1（原版行为）测试mod是否加载
5. 检查mod兼容性列表 - 某些mod会冲突

如果问题持续，请附带日志文件和mod列表报告。

---

## Advanced Questions / 进阶问题

### Q: How does the "sample and hold" algorithm work? / "采样保持"算法如何工作？

**EN:** Simplified explanation:
1. Each factory is assigned an update slot (e.g., Factory A = tick 0, 10, 20...)
2. On **active ticks**: Factory runs full simulation, recording input/output changes
3. On **idle ticks**: Factory doesn't simulate, but applies the recorded changes

Example with Period = 10:
- Active: Mine 10 ore, produce 10 items and send into the station storage
- Idle (9 ticks): Apply +10 items to station storage (held from last active tick)
- Result: Same long-term throughput with 90% less computation

**CN:** 简化说明：
1. 每个工厂分配一个更新时隙（例如，工厂A = 第0, 10, 20帧...）
2. **活跃帧**：工厂运行完整模拟，记录输入/输出变化
3. **空闲帧**：工厂不模拟，但应用记录的变化

周期 = 10 的例子：
- 活跃：开采10矿石，生产10物品，并送入物流塔存储
- 空闲（9帧）：向物流塔存储应用+10物品（保持自上次活跃帧）
- 结果：相同的长期吞吐量，计算量减少90%

