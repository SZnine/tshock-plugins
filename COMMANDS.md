# Commands

这份表只列当前仓库内 3 个插件的常用命令。

## PermanentPotionBuffs

配置文件：`tshock/PermanentPotionBuffs.json`

- `/ppb reload`
  - 重载插件配置。
- `/ppb status`
  - 查看自己的药水/食物状态。
- `/ppb status 玩家名`
  - 查看指定玩家的药水/食物状态。

## AutoHerbFarm

配置文件：`tshock/AutoHerbFarm.json`

- `/ahf reload`
  - 重载插件配置。
- `/ahf status`
  - 查看当前选区和农场状态。
- `/ahf list`
  - 列出所有已保存农场。
- `/ahf pos1 [x y]`
  - 设置农场区域第一个角。
- `/ahf pos2 [x y]`
  - 设置农场区域第二个角。
- `/ahf chest [x y]`
  - 设置输出箱子。
- `/ahf add <name>`
  - 按当前选区和箱子保存一个农场。
- `/ahf set <name> growthchance <value|default>`
  - 设置农场成长概率。
- `/ahf set <name> growthpasses <value|default>`
  - 设置每轮成长尝试次数。
- `/ahf set <name> regrowscans <value|default>`
  - 设置收获后的再生成冷却。
- `/ahf set <name> ignorerules <true|false>`
  - 设置是否忽略自然成熟条件。
- `/ahf enable <name> [true|false]`
  - 启用或停用农场。
- `/ahf remove <name>`
  - 删除农场。

## NpcManagementTweaks

配置文件：`tshock/NpcManagementTweaks.json`

- `/nmt reload`
  - 重载插件配置。
- `/nmt status`
  - 查看当前渔夫相关运行状态。

## 建议

- 先改配置文件，再用对应的 `reload` 命令生效。
- `AutoHerbFarm` 建议先做一小块测试农场，再扩大范围。
- `NpcManagementTweaks` 当前命令面主要是管理与状态查看，不提供复杂的游戏内配置入口。
