-- KPQ（废弃都市组队任务）进入门槛调整
-- 目标：最低可 1 人进入、不限制成员 IP（允许同 IP 多开组队）
-- 两个开关均在 game_config 表，由 GameConfig 在启动时加载。

-- 1) 副本任务允许单人进入：use_enable_solo_expeditions = true
--    KerningPQ.js 等脚本逻辑：minPlayers = use_enable_solo_expeditions ? 1 : minPlayers
--    置 true 后 KPQ 最低人数由 3 降为 1（maxPlayers 仍是 6）。
UPDATE game_config
SET config_value = 'true', update_time = CURRENT_TIMESTAMP
WHERE config_type = 'server'
  AND config_sub_type = 'Game Mechanics'
  AND config_code = 'use_enable_solo_expeditions';

-- 2) 取消登录期同 IP 多开检测：deterred_multi_client = false
--    SessionCoordinator 在 deterred_multi_client=false 时直接跳过校验，
--    不再限制「同一真实 IP 最多 max_accounts_per_user 个在线账号」。
--    ⚠️ 注意：这是【服务端全局】开关，会影响所有副本任务及整服的多开限制，
--       不是仅针对 KPQ。若只想放开特定 IP，应改回 true 并把目标 IP 加入
--       multi_open_whitelist_ip，而非全局关闭。
UPDATE game_config
SET config_value = 'false', update_time = CURRENT_TIMESTAMP
WHERE config_type = 'server'
  AND config_sub_type = 'Safe'
  AND config_code = 'deterred_multi_client';
