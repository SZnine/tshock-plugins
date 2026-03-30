# AutoHerbFarm

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的草药农场自动化插件。

作者：`鱼仔仔面`

## 功能

- 周期扫描配置区域内的草药
- 成熟草药自动收获
- 收获物与种子直接写入指定箱子
- 原地自动重种
- 支持忽略自然成熟条件
- 支持按农场单独覆盖成长参数

## 配置

配置文件位置：

- `tshock/AutoHerbFarm.json`

全局字段：

- `ScanIntervalTicks`
- `MatureChancePercent`
- `GrowthPassesPerScan`
- `MinimumRegrowScans`
- `HerbYield`
- `SeedYieldMin`
- `SeedYieldMax`
- `RequireChestSpaceForHarvest`
- `Farms`

农场字段：

- `Name`
- `Enabled`
- `Left/Top/Right/Bottom`
- `ChestTileX/ChestTileY`
- `IgnoreNaturalGrowthRules`
- `MatureChancePercent`
- `GrowthPassesPerScan`
- `MinimumRegrowScans`
- `AllowedSupportTiles`

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

## 使用流程

1. 用 `/ahf pos1` 和 `/ahf pos2` 选中农场区域。
2. 用 `/ahf chest` 指定输出箱子。
3. 用 `/ahf add <name>` 保存农场。
4. 用 `/ahf set` 调整成长概率、成长尝试次数和再生成冷却。
5. 用 `/ahf list` 或 `/ahf status` 查看当前状态。

## 说明

- 默认节奏是偏慢速的自动化模型，不是高速刷产物模型。
- 箱子空间不足时，是否跳过本次收获由 `RequireChestSpaceForHarvest` 控制。
- 实际支持的种植底座以 `AllowedSupportTiles` 为准，默认包含花盆和种植盆。
