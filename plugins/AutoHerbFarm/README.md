# AutoHerbFarm

`AutoHerbFarm` 是一个面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的自动草药农场插件。

## 当前功能

- 周期扫描配置区域内的草药
- 可选忽略原版成长限制，按本地概率推进草药成熟
- 成熟草药自动收获
- 收获物和种子直接塞进指定箱子
- 原地自动重种，形成闭环
- 支持多个农场区域
- 支持游戏内命令设置区域和箱子，不必手改配置
- 支持按农场单独调生长概率、成长尝试次数和再生冷却

## 生长模型

第二版之后增加了“最小再生冷却”。

这意味着一株草药被自动收获后，不会在下一轮扫描立刻重新成熟，而是至少等待一段扫描次数后，才进入下一轮自动成熟判定。这样节奏会更接近真实生长，而不是持续高速刷产物。

默认值：

- `ScanIntervalTicks = 300`
- `MatureChancePercent = 3`
- `GrowthPassesPerScan = 1`
- `MinimumRegrowScans = 120`

按默认配置，自动农场会比上一版慢很多。

## 配置文件

插件首次启动后会生成：

`server\tshock\AutoHerbFarm.json`

全局字段：

- `ScanIntervalTicks`: 扫描间隔
- `MatureChancePercent`: 忽略自然限制时，每次成长尝试的成熟概率
- `GrowthPassesPerScan`: 每次扫描做几轮成长尝试
- `MinimumRegrowScans`: 每次收获后至少等待多少轮扫描才能再次成熟
- `HerbYield`: 每次自动收获给几个草药
- `SeedYieldMin` / `SeedYieldMax`: 每次自动收获给几个种子
- `RequireChestSpaceForHarvest`: 箱子放不下时是否跳过本次收获

每个农场字段：

- `Left/Top/Right/Bottom`: 区域范围
- `ChestTileX/ChestTileY`: 输出箱子坐标
- `IgnoreNaturalGrowthRules`: 是否忽略原版成长条件
- `MatureChancePercent`: 单个农场覆盖全局成长概率，`-1` 表示跟随全局
- `GrowthPassesPerScan`: 单个农场覆盖全局成长尝试次数，`-1` 表示跟随全局
- `MinimumRegrowScans`: 单个农场覆盖全局再生冷却，`-1` 表示跟随全局
- `AllowedSupportTiles`: 允许的支撑地块，默认花盆和种植盆

## 命令

- `/ahf reload`
- `/ahf status`
- `/ahf list`
- `/ahf pos1 [x y]`
- `/ahf pos2 [x y]`
- `/ahf chest [x y]`
- `/ahf add <name>`
- `/ahf set <name> <growthchance|growthpasses|regrowscans|ignorerules> <value>`
- `/ahf enable <name> [true|false]`
- `/ahf remove <name>`

## 游戏内配置流程

1. 站到农场一个角，输入 `/ahf pos1`
2. 站到另一个角，输入 `/ahf pos2`
3. 站到输出箱子旁边，输入 `/ahf chest`
4. 输入 `/ahf add 农场名`
5. 用 `/ahf set` 微调成熟速度
6. 输入 `/ahf list` 或 `/ahf status` 检查结果

示例：

```text
/ahf set daybloom-farm growthchance 2
/ahf set daybloom-farm growthpasses 1
/ahf set daybloom-farm regrowscans 180
/ahf set daybloom-farm ignorerules true
```

恢复某项为全局默认值：

```text
/ahf set daybloom-farm growthchance default
/ahf set daybloom-farm growthpasses default
/ahf set daybloom-farm regrowscans default
```
