# TShock Plugins

面向 `TShock 6.1.0 / Terraria 1.4.5.6` 的插件源码仓库。

作者：`鱼仔仔面`

## 当前项目

### PermanentPotionBuffs

- 药水与食物常驻增益
- 普通药水时长双倍
- 普通药水死亡剩余时间继承

位置：`plugins/PermanentPotionBuffs`

### AutoHerbFarm

- 草药农场自动收获、入箱、重种
- 支持游戏内配置农场区域
- 支持按农场单独调整成长参数

位置：`plugins/AutoHerbFarm`

### NpcManagementTweaks

- 城镇 NPC 白天回家与回家瞬移优化
- 旅商到访规则调整
- 渔夫任务每日提交次数扩展

位置：`plugins/NpcManagementTweaks`

## 预留位置

为后续功能插件预留了统一入口：

- `plugins/_future/`

用途：
- 记录准备开发的插件方向
- 统一插件命名约定
- 避免未来继续把临时方案和正式项目混在一起

## 目录结构

- `plugins/`
  - 当前插件源码项目
  - 未来插件预留目录
- `vendor/tshock-sdk/`
  - 编译所需引用 DLL
- `Tshock.sln`
  - 解决方案入口
- `Directory.Build.props`
  - 统一引用路径配置

## 构建

要求：
- `.NET 9 SDK`

命令：

```powershell
dotnet build .\plugins\PermanentPotionBuffs\PermanentPotionBuffs.csproj -c Release
dotnet build .\plugins\AutoHerbFarm\AutoHerbFarm.csproj -c Release
dotnet build .\plugins\NpcManagementTweaks\NpcManagementTweaks.csproj -c Release
```

输出目录：

```text
plugins/<ProjectName>/bin/Release/net9.0/
```

## 部署

将目标 DLL 复制到你的 TShock 服务器：

```text
ServerPlugins/
```

运行时服务器本体、地图、日志、数据库与配置不包含在仓库中。
