# NpcManagementTweaks

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的 NPC 管理类插件。

作者：`鱼仔仔面`

## 功能

- 白天允许城镇 NPC 回家
- 城镇 NPC 在合适条件下直接瞬移回住房点
- 旅商每天按配置概率到访
- 渔夫任务支持每日多次提交

## 配置

配置文件位置：

- `tshock/NpcManagementTweaks.json`

配置分组：

- `TownNpcs`
  - `Enabled`
  - `AllowDaytimeGoHome`
  - `TeleportDirectlyHome`
  - `ScanIntervalTicks`
  - `TeleportDistanceTiles`
- `TravelingMerchant`
  - `Enabled`
  - `ArrivalChanceNumerator`
  - `ArrivalChanceDenominator`
  - `ExtraShopSlots`
  - `MergeShopRerolls`
- `Angler`
  - `Enabled`
  - `ScanIntervalTicks`
  - `DailyTurnInLimit`

## 命令

- `/nmt reload`
- `/nmt status`

## 说明

- 当前插件只包含 NPC 管理相关逻辑。
- 不包含树木、草药、多线钓鱼等旧世界机制内容。
- 渔夫功能当前是“每日可提交多次”的实现，不是批量交鱼奖励模型。
