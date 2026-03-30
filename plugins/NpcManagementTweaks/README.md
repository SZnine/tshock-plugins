# NpcManagementTweaks

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的 NPC 管理类插件。

作者：`鱼仔仔面`

## 当前功能

- 白天允许城镇 NPC 回家
- 城镇 NPC 在合适条件下可直接瞬移回住房点
- 旅商每天按配置概率到访
- 渔夫任务支持每日多次提交

## 配置文件

生成位置：

- `tshock/NpcManagementTweaks.json`

## 管理命令

- `/nmt reload`
- `/nmt status`

## 功能说明

### 1. Town NPC Housing

- 处理白天 NPC 回家限制
- 优化 NPC 返回住房点的节奏
- 在满足条件时直接将 NPC 放回住房位置

### 2. Traveling Merchant

- 控制旅商每日是否到访
- 当前规则是按配置概率判定每日到访

### 3. Angler

- 扩展渔夫任务的每日可提交次数
- 当前实现为每日可正常提交多次，超过上限后恢复原版“今日已完成”状态

## 说明

- 这个插件当前只保留 NPC 管理相关功能
- 不包含树木生长、草药加速、多线钓鱼等旧实验内容
- 旅商和渔夫规则以当前源码实现为准
