# Future Plugins

这个目录用于预留未来要加入仓库的功能插件位置。

## 作用

- 记录准备开发的插件方向
- 统一新插件的命名方式
- 在立项前区分“计划中”和“已实现”的项目

## 命名建议

新插件目录统一使用：

- `plugins/<PluginName>/`

其中：
- `<PluginName>` 使用 PascalCase
- 目录名、`.csproj`、主插件类名尽量保持一致

## 建议流程

1. 先在本目录记录计划中的插件名称与目标。
2. 确认开始开发后，再创建正式插件目录。
3. 开发完成后，把根 README 的“当前项目”部分补上对应说明。

## 当前预留方向

- `PlayerMechanicsTweaks`
- `CombatTweaks`
- `EconomyTweaks`
- `WorldAutomationTweaks`
