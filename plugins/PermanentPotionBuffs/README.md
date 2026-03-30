# PermanentPotionBuffs

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的药水与食物增益插件。

作者：`鱼仔仔面`

## 功能

- 所有可识别的增益药水与食物都可参与判定
- 同种药水或食物累计达到 `30` 个时获得常驻增益
- 普通手动使用的非常驻药水持续时间按原版 `2 倍` 处理
- 普通手动使用的非常驻药水死亡后尝试继承死亡前剩余时间
- 常驻增益会在玩家可访问容器中持续检测并自动补时

## 统计范围

- 身上背包
- 猪猪存钱罐 `Piggy Bank`
- 保险库 `Safe`
- 防御者熔炉 `Defender's Forge`
- 虚空保险库 `Void Vault`

## 配置

配置文件位置：

- `tshock/PermanentPotionBuffs.json`

当前关键字段：

- `ScanIntervalTicks`
- `RequiredStack`
- `PotionDurationMultiplier`
- `EnableAllPotions`
- `EnableAllFoods`

默认值：

- `RequiredStack = 30`
- `PotionDurationMultiplier = 2`

## 命令

- `/ppb reload`
- `/ppb status`
- `/ppb status 玩家名`

## 说明

- 常驻增益和普通药水增强是两套独立逻辑。
- 当前版本不再使用早期固定 5 种药水、5 瓶阈值的旧规则。
- 实际行为以当前源码与配置文件为准。
