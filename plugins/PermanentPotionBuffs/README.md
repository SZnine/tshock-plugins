# PermanentPotionBuffs

这是一个按 `TShock v6.1.0` 思路写的插件骨架，目标功能是：

- 玩家只要在支持的存储位置里放够指定数量的药水，就自动获得对应 Buff
- 支持统计这些位置里的药水：
  - 身上背包
  - 猪猪存钱罐 `Piggy Bank`
  - 保险库 `Safe`
  - 铁卫熔炉/防御者熔炉 `Defender's Forge`
  - 虚空保险库 `Void Vault`
- 玩家死亡后，复活后会自动重新补上 Buff
- 如果药水被拿走，Buff 会自动取消

## 对应版本

这份骨架按 `TShock v6.1.0` 来做。根据官方发布页：

- 发布日期：`2026-03-11`
- 标题：`TShock 6.1 for Terraria 1.4.5.6`
- 说明里提到：升级到 `OTAPI 3.3.11`

官方来源：

- <https://github.com/Pryaxis/TShock/releases/tag/v6.1.0>

## 当前实现方式

插件不依赖“只在复活事件里补 Buff”这种单点逻辑，而是采用低频扫描：

1. 每隔一小段时间扫描在线玩家
2. 汇总玩家背包和 4 个扩展存储里的药水数量
3. 如果某种药水总数大于等于配置要求，持续给对应 Buff 补时间
4. 玩家死亡时 Buff 会自然消失
5. 玩家复活后，下一次扫描会重新补上

这种做法更稳，原因是：

- 不容易因为版本事件名变化而失效
- 不依赖某个特定网络包
- 死亡、复活、重连都能统一处理

## 默认配置

默认会生成：

- 路径：`tshock/PermanentPotionBuffs.json`

默认示例规则：

- 铁皮药水 `Ironskin Potion`
- 拾心药水 `Heartreach Potion`
- 光芒药水 `Shine Potion`
- 挖矿药水 `Mining Potion`
- 所有食物类 `All Food Items`

前四种是固定药水，第五条是“所有食物类”规则：

- 只要某一种食物在支持的容器里累计达到 `5` 个
- 插件就会根据那种食物自带的 Buff 自动给玩家补上食物增益
- 如果同时满足多种食物，会优先选择更高级的食物 Buff

## 配置格式

```json
{
  "ScanIntervalTicks": 60,
  "RefreshDurationTicks": 180,
  "RequiredStack": 5,
  "Rules": [
    {
      "Name": "Ironskin Potion",
      "Type": 0,
      "ItemId": 2322,
      "BuffId": 5
    },
    {
      "Name": "All Food Items",
      "Type": 1,
      "ItemId": 0,
      "BuffId": 0
    }
  ]
}
```

字段说明：

- `ScanIntervalTicks`：扫描间隔，`60` 大约是 1 秒
- `RefreshDurationTicks`：每次补 Buff 的持续时间
- `RequiredStack`：达到永久增益所需的药水数量，当前按你的需求默认是 `5`
- `Rules`：规则列表
- `Type = 0`：固定药水/固定 Buff
- `Type = 1`：食物类规则

## 管理命令

- `/ppb reload`：重载配置
- `/ppb status`
- `/ppb status 玩家名`

`status` 用来查看某个玩家每种药水当前统计了多少瓶，以及 Buff 是否已启用。

食物规则的显示逻辑是：

- 看当前满足条件的最佳食物堆叠数
- 看最终会应用哪一个食物 Buff

## 编译方式

当前工作区里没有安装 `.NET SDK`，只有 runtime，所以我这边没有直接完成本地编译验证。

本机状态：

- 当前时间：`2026-03-28`
- `dotnet --info` 显示：`No SDKs were found.`

要编译这个插件，你需要至少补齐：

1. `.NET 9 SDK`
2. TShock 6.1.0 服务器文件
3. 让项目里的 `TShockServerPath` 指向服务器目录

项目默认会从这里找依赖：

```text
plugins/PermanentPotionBuffs/../../server
```

也就是：

```text
server/TerrariaServer.exe
server/TerrariaServerAPI.dll
server/TShockAPI.dll
```

如果你的服务器不在这个目录，可以在编译时传：

```powershell
dotnet build .\plugins\PermanentPotionBuffs\PermanentPotionBuffs.csproj -p:TShockServerPath=D:\path\to\tshock-server
```

## 下一步建议

这版已经把核心逻辑和配置入口搭好了，下一步最值得做的是：

1. 用 `v6.1.0` 实际 DLL 编译一次
2. 进服验证猪猪、保险库、虚空保险库、死亡复活这几条链路
3. 再决定要不要补：
   - 开关命令
   - 生效提示
   - 仅登录后生效
   - 仅部分权限组生效
