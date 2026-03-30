# TShock Plugins

一组面向 TShock 6.1.0 / Terraria 1.4.5.6 的自用插件源码仓库。

## Projects

- `PermanentPotionBuffs`
  - 背包与储物容器药水/食物增益规则
  - 普通药水时长增强
  - 常驻增益与复活继承逻辑
- `AutoHerbFarm`
  - 草药农场自动收获、入箱、重种
  - 支持游戏内配置农场区域和参数
- `NpcManagementTweaks`
  - NPC 住房与回家行为优化
  - 旅商到访规则调整
  - 渔夫任务每日提交次数扩展

## Repository Layout

- `plugins/`
  - 各插件源码项目
- `vendor/tshock-sdk/`
  - 编译所需的 TShock / OTAPI 引用 DLL
- `Tshock.sln`
  - 解决方案入口
- `Directory.Build.props`
  - 统一插件引用路径配置

## Build

要求：
- `.NET 9 SDK`

常用命令：

```powershell
dotnet build .\plugins\PermanentPotionBuffs\PermanentPotionBuffs.csproj -c Release
dotnet build .\plugins\AutoHerbFarm\AutoHerbFarm.csproj -c Release
dotnet build .\plugins\NpcManagementTweaks\NpcManagementTweaks.csproj -c Release
```

构建输出：
- `plugins/<ProjectName>/bin/Release/net9.0/`

## Deployment

将目标 DLL 复制到你的 TShock 服务器：

```text
ServerPlugins/
```

运行时服务器本体、地图、日志、数据库与配置不属于本仓库内容。
