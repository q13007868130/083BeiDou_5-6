# 废弃都市组队任务（Kerning PQ / KPQ）逻辑整理

> 废弃都市 = Kerning City。对应服务端事件脚本 `KerningPQ`，地图区间 `103000800`~`103000805`。
> 本服实际生效脚本（按 `scripts-zh-CN` 优先）：`gms-server/scripts-zh-CN/event/KerningPQ.js`，
> 引导 NPC `9020001`(Cloto)、招募 NPC `9020000`(Lakelis)、传送门 `kpq0`~`kpq4`。
> 英文基线在 `gms-server/scripts/`（逻辑一致，仅文案不同）。

---

## 1. 参与条件

| 项目 | 本服实际（zh-CN） | 原版基线（scripts/） |
|---|---|---|
| 最少人数 | **1**（已开 `use_enable_solo_expeditions`，脚本原值 3） | 3 |
| 最多人数 | **6** | 4 |
| 最低等级 | 21 | 21 |
| 最高等级 | `mxj_max_level`（服内最大等级，通常 200+） | 30 |
| 开 `use_enable_party_level_limit_lift` | 最高等级=`mxj_max_level` | — |
| 时间限制 | 30 分钟 | 30 分钟 |
| 同时进行的队伍数（lobby） | 1（同频道同时只放 1 队） | 1 |

- 必须由**队长**在废弃都市(`103000000`)找 Lakelis(`9020000`) 发起，且全队都在招募图、等级在 `[21, 最大等级]` 内。
- `getEligibleParty` 会过滤：必须有队长、人数在 `[minPlayers, maxPlayers]`、全员位于招募图且等级合规，否则整队不合格。
- 若同频道已有队伍在副本内，`startInstance` 返回失败，提示换频道或等待。

## 2. 地图与 NPC 一览

| 地图 ID | 用途 | 关键脚本 |
|---|---|---|
| `103000000` | 废弃都市（招募/退出点） | NPC `9020000` Lakelis |
| `103000800` | 第 1 关（问答 + 通行证） | NPC `9020001` Cloto / 传送门 `kpq0` |
| `103000801` | 第 2 关（绳索谜题） | Cloto / `kpq1` |
| `103000802` | 第 3 关（平台谜题） | Cloto / `kpq2` |
| `103000803` | 第 4 关（木桶谜题） | Cloto / `kpq3` |
| `103000804` | 第 5 关（Boss 狩猎） | Cloto / `kpq4` |
| `103000805` | 奖励关（自由刷怪） | Cloto |
| `103000890` | 副本出口（超时/退出落地） | — |

## 3. 整体流程

```
队长在 103000000 找 Lakelis 发起
        │  em.startInstance(party)
        ▼
全员进入 103000800（entryMap, portal 0）
        │
   ┌────┴───── 逐关通过 Cloto(NPC 9020001) 判定 ─────┐
   │  clearStage() → 置 "Nstageclear" → linkToNextStage 开 kpqN 传送门  │
   ▼
第1关(问答) → 第2关(绳索) → 第3关(平台) → 第4关(木桶) → 第5关(Boss)
        │  第5关队长集齐 10 张 pass → clearStage(5) + eim.clearPQ()
        ▼
进入奖励关 103000805（剩余时间自由刷怪、领奖）
        │  超时(30min) / 全员退出 / 队伍不足下限 → end() 全员回 103000890
        ▼
结算：按 setEventRewards 随机发奖 + 逐关经验
```

## 4. 关卡逐关机制

### 第 1 关 `103000800` — 问答换通行证
- **非队长**：与 Cloto 对话 → 随机抽一道一转/二转数值题（`stage1Questions`/`stage1Answers`，答案为 10/35/20/25/25/30/8）。
  手中需持有对应数量的 `4001007`(coupon) 交给 Cloto 验证；答对则获得 1 张 `4001008`(pass)，并在 `grid` 标记已答（防止重复答题）。
- **队长**：需收到 `4001008 × (队伍人数 − 1)`（即每人 1 张）。集齐后对话 Cloto → `clearStage(1)`，**开启通往 `103000801` 的传送门**，回收全部 pass，`eim.gridClear()`。
- `eim.gridCheck/gridInsert`：给每个队员一个答题槽位，避免重复作答刷 pass。

### 第 2 关 `103000801` — 绳索谜题
- 开局随机抽一组正确组合（`stage2Combos`，4 选 3：4 根绳索里哪 3 根正确）。
- 队长双击 Cloto 触发 `rectangleStages`：检查队员站位是否落在 `stage2Rects`（4 个矩形）中对应的 3 个正确绳索矩形内（需站中部，边缘不算）。
- 命中 → `clearStage(2)` 开 `kpq1` 通往 `103000802`；否则 `showWrongEffect` 提示换组合重试。

### 第 3 关 `103000802` — 平台谜题
- 随机抽正确组合（`stage3Combos`，5 选 3，共 10 种），判定区 `stage3Rects`（5 个矩形）。
- 同样需 3 名队员站对平台中部 → `clearStage(3)` 开 `kpq2` 通往 `103000803`。

### 第 4 关 `103000803` — 木桶谜题
- 随机抽正确组合（`stage4Combos`，6 选 3，共 20 种），判定区 `stage4Rects`（6 个矩形）。
- 3 名队员站对木桶 → `clearStage(4)` 开 `kpq3` 通往 `103000804`。

### 第 5 关 `103000804` — Boss 狩猎
- 地图内刷 Boss，击杀掉落 `4001008`(pass)。
- **队长**集齐 `4001008 × 10` 后对话 Cloto → 回收 10 张 pass，`clearStage(5)`，**并调用 `eim.clearPQ()`**（停止计时 + 标记副本已通关）。
- 随后 `kpq4` 传送门开启，通往奖励关 `103000805`。

### 奖励关 `103000805`
- 通关后队员再找 Cloto：因 `5stageclear` 已置，`stage>=5` 分支提示“恭喜通关并给小奖励”，随后 `giveEventReward` 发奖并落到奖励关。
- 剩余时间内自由刷怪攒经验；可经 Lakelis(`9020000`)「放弃区域」或超时退出副本。

## 5. 过关判定的核心机制（最容易看混的地方）

- **portal 脚本只“读”标志，不“写”标志**：`kpq0`~`kpq4` 各自检查 `eim.getProperty("Nstageclear") != null`，有才放行到下一关，否则提示 “The portal is not opened yet.”
- **真正写入 `Nstageclear` 的是引导 NPC `9020001`(Cloto)** 的 `clearStage(stage, eim, curMap)`：
  ```js
  function clearStage(stage, eim, curMap) {
      eim.setProperty(stage + "stageclear", "true");
      eim.showClearEffect(true);
      eim.linkToNextStage(stage, "kpq", curMap); // 开 kpq{stage} 传送门到下一图
  }
  ```
- 所以**关卡推进完全由玩家与 Cloto 的交互驱动**，不是靠 reactor 或 `monsterKilled`。`KerningPQ.js` 的 `monsterKilled/allMonstersDead` 为空是正常的。
- `linkToNextStage(stage,"kpq",curMap)` 的 `"kpq"` 前缀即对应 `kpq0`~`kpq4` portal 脚本文件名。

## 6. 专属道具（exclusive items）

- `4001007` coupon：第 1 关答题验证用（交给 Cloto 核对答案数量）。
- `4001008` pass：第 1 关非队长换得、第 5 关 Boss 掉落；队长集齐后用于过关。
- `setExclusiveItems([4001007, 4001008])`：标记为本副本独占道具，避免副本外掉落/重复获取。

## 7. 奖励

### 7.1 通关物品（随机发 1 件 / 人）
清本后每个队员在第 5 关(`103000804`)再找 Cloto 对话 → `eim.giveEventReward(player)` 从该池**随机抽 1 件**发放（需背包有空位），随后传送到奖励关 `103000805`。
池定义在 `setEventRewards(evLevel=1, itemSet, itemQty)`（`KerningPQ.js` 第 83~93 行）。以下名称已从 `wz/String.wz` 解码：

| 类别 | 物品 ID | 名称 | 数量 |
|---|---|---|---|
| 卷轴 | 2040505 | 全身铠甲防御卷轴 | 1 |
| 卷轴 | 2040514 | 全身铠甲智力卷轴 | 1 |
| 卷轴 | 2040502 | 全身铠甲敏捷卷轴 | 1 |
| 卷轴 | 2040002 | 头盔防御卷轴 | 1 |
| 卷轴 | 2040602 | 裤/裙防御卷轴 | 1 |
| 卷轴 | 2040402 | 上衣防御卷轴 | 1 |
| 卷轴 | 2040802 | 手套敏捷卷轴 | 1 |
| 耳环 | 1032009 | 黄方耳环 | 1 |
| 耳环 | 1032004 | 金耳环 | 1 |
| 耳环 | 1032005 | 红十字耳环 | 1 |
| 耳环 | 1032006 | 雷电耳环 | 1 |
| 耳环 | 1032007 | 祖母绿耳环 | 1 |
| 耳环 | 1032010 | 黄星耳环 | 1 |
| 耳环 | 1032002 | 蓝宝石耳环 | 1 |
| 帽子 | 1002026 | 褐色斗笠 | 1 |
| 帽子 | 1002089 | 绿色斗笠 | 1 |
| 帽子 | 1002090 | 蓝色斗笠 | 1 |
| 药水 | 2000003 | 蓝色药水 | 80 |
| 药水 | 2000001 | 橙色药水 | 80 |
| 药水 | 2000002 | 白色药水 | 80 |
| 药水 | 2000006 | 活力神水 | 50 |
| 食物 | 2022003 | 烤鳗鱼 | 5 |
| 食物 | 2022000 | 矿泉水 | 15 |
| 药水 | 2000004 | 特殊药水 | 15 |
| 材料 | 4003000 | 螺丝钉 | 30 |
| 矿石 | 4010000 | 青铜母矿 | 15 |
| 矿石 | 4010001 | 钢铁母矿 | 15 |
| 矿石 | 4010002 | 锂矿石母矿 | 15 |
| 矿石 | 4010003 | 朱矿石母矿 | 15 |
| 矿石 | 4010004 | 银的母矿 | 15 |
| 矿石 | 4010005 | 紫矿石母矿 | 8 |
| 矿石 | 4010006 | 黄金母矿 | 8 |
| 矿石 | 4010007 | 锂母矿 | 8 |
| 宝石 | 4020000 | 石榴石母矿 | 8 |
| 宝石 | 4020001 | 紫水晶母矿 | 8 |
| 宝石 | 4020002 | 海蓝石母矿 | 8 |
| 宝石 | 4020003 | 祖母绿母矿 | 8 |
| 宝石 | 4020004 | 蛋白石母矿 | 8 |
| 宝石 | 4020005 | 蓝宝石母矿 | 8 |
| 宝石 | 4020006 | 黄晶母矿 | 8 |
| 宝石 | 4020007 | 钻石母矿 | 3 |
| 宝石 | 4020008 | 黑水晶母矿 | 3 |

- 共 **42 个条目**，覆盖卷轴(7)、耳环(7)、帽子(3)、药水/食物(7)、材料/矿石/宝石(18)。
- 单刷（1 人）时：清本可得 **1 件随机物品** + 逐关经验（见下）。

### 7.2 逐关经验
`setEventClearStageExp([100, 200, 400, 800, 1500])`：每通一关由事件框架在 stage-clear 信号（`showClearEffect`/`linkToNextStage`）时结算对应关数经验。
- 全 5 关通过累计 **3000 经验/人**。

### 7.3 奖励关自由刷怪
通关进入 `103000805` 后，剩余时间内可自由击杀地图怪物攒经验（无额外物品掉落，纯刷怪收益）。

> 注：`giveRandomEventReward` 函数在脚本中已定义但本 PQ 实际走 Cloto 对话的 `giveEventReward(player)` 路径发奖。

## 8. 生命周期与失败处理

| 事件 | 行为 |
|---|---|
| `playerEntry` | 进入 `entryMap` 的 portal 0 |
| `scheduledTimeout`（30 分钟到） | 若已置 `9stageclear`(本 PQ 无此关，故走 else) → `end()` 全员回出口 |
| 队员死亡/掉线/离队(`playerRevive/Disconnected/leftParty`) | 若离队后队伍 < `minPlayers` 且未通关 → `end()` 全队退出；否则仅移除该玩家 |
| `changedMap`（跑到副本外） | 同上逻辑：队伍不足则 `end()`，否则仅 unregister |
| `changedLeader` / `disbandParty` | 未通关时队长离开或解散 → `end()` |
| `clearPQ` | 仅停计时 + 标记已通关，**不自动传送**；队伍自行经 `kpq4` 进奖励关 |
| `end` | 全员 `playerExit` → 回 `exitMap`(`103000890`)，`eim.dispose()` |

- `respawnStages`：每 15 秒对 `103000800`/`103000805` 调 `instanceMapRespawn()`，维持刷怪。

## 9. 本服注意事项（与官方原版差异 / 实际生效约束）

1. **人数上限放宽到 6**（原版 4），**等级上限跟随服内最大等级**（原版封顶 30）→ 本服 KPQ 实为「21 级以上全员可进」。
2. **进入门槛已放开为「最低 1 人 + 不限制 IP」**（2026-10-08 通过迁移 `V2.1.106__update_kpq_access.sql` 调整）：
   - `use_enable_solo_expeditions = true` → KPQ `minPlayers` 由 3 降为 1（脚本逻辑 `minPlayers = use_enable_solo_expeditions ? 1 : minPlayers`）。
   - `deterred_multi_client = false` → 登录期同 IP 多开检测整体跳过，组队不再要求成员 IP 互不相同。
   - ⚠️ **`deterred_multi_client` 是服务端全局开关**：置 `false` 后整服（不只是 KPQ，所有副本/地图）都允许同 IP 多开。若只想放开特定 IP，应改回 `true` 并把目标 IP 加入 `multi_open_whitelist_ip`（默认仅 `127.0.0.1` 本机豁免），而不是全局关闭。
   - 生效方式：重建并部署（Flyway 在启动时自动应用 `V2.1.106`）；或直接在运行库执行该迁移里的两条 `UPDATE game_config`。改完需重启服务端。
3. **关卡推进依赖 Cloto(9020001) 对话**：若某关 Cloto 脚本缺失/报错，对应 `Nstageclear` 永远不被置位，传送门 `kpqN` 永不开启 → 卡关。排查“进不去下一关”优先看 `9020001` 而非 `KerningPQ.js`。
3. **关卡推进依赖 Cloto(9020001) 对话**：若某关 Cloto 脚本缺失/报错，对应 `Nstageclear` 永远不被置位，传送门 `kpqN` 永不开启 → 卡关。排查“进不去下一关”优先看 `9020001` 而非 `KerningPQ.js`。
4. `clearPQ` 不清场传送：第 5 关通关后队伍需自行走 `kpq4` 进奖励关；奖励由 Cloto 在 `103000804` 对话结算（不是进奖励关自动发）。
