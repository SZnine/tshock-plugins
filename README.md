# TShock Plugins

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的插件仓库。

作者：`鱼仔仔面`

## 下载

当前发布版本：

- [v0.1.0 Release](https://github.com/SZnine/tshock-plugins/releases/tag/v0.1.0)

可直接下载：

- `PermanentPotionBuffs.dll`
- `AutoHerbFarm.dll`
- `NpcManagementTweaks.dll`
- `COMMANDS.md`
- `tshock-plugins-v0.1.0.zip`

## 当前插件

### PermanentPotionBuffs

- 药水与食物常驻增益
- 普通药水时长双倍
- 普通药水死亡剩余时间继承

说明：`plugins/PermanentPotionBuffs/README.md`

### AutoHerbFarm

- 草药农场自动收获、入箱、重种
- 支持游戏内配置农场区域
- 支持按农场单独调整成长参数

说明：`plugins/AutoHerbFarm/README.md`

### NpcManagementTweaks

- 城镇 NPC 白天回家与回家瞬移优化
- 旅商到访规则调整
- 渔夫任务每日提交次数扩展

说明：`plugins/NpcManagementTweaks/README.md`

## 安装

1. 下载需要的 DLL。
2. 放入你的 TShock 服务器 `ServerPlugins/` 目录。
3. 启动服务器，让插件自动生成配置文件。
4. 按插件各自 README 或 `COMMANDS.md` 调整配置与命令。

## 仓库内容

这个仓库只保留：

- 插件源码
- 编译依赖
- 简明命令表

不包含：

- 运行中的 TShock 服务器本体
- 地图、数据库、日志、运行配置

## 后续

未来新增插件会继续放在 `plugins/` 下，并在 release 中单独提供对应 DLL。
