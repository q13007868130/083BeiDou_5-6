using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using Server.Field;
using UnityEngine;

public static class NativeGmCommandHost
{
	private static NativeCommandResult Extended(CharRecord c, string name, string[] a, string raw)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_1045: Unknown result type (might be due to invalid IL or missing references)
		//IL_115a: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		FieldDirector fieldDirector = GameLoop.I?.field;
		if ((Object)(object)playerCtrl == (Object)null || fieldDirector?.Map == null)
		{
			return NativeCommandResult.Fail("请先进入地图");
		}
		NativeCommandResult nativeCommandResult = new NativeCommandResult();
		DbStore db = GameSession.Server.Db;
		int x = Mathf.RoundToInt(playerCtrl.FeetPos.x * 40f);
		int y = Mathf.RoundToInt((0f - playerCtrl.FeetPos.y) * 40f);
		switch (name)
		{
		case "GachaCommand":
			NpcSession.Talk(9900001, "gachaponInfo");
			if (NpcSession.Active?.LastError != null)
			{
				throw new ArgumentException(NpcSession.Active.LastError);
			}
			nativeCommandResult.messages.Add("已打开扭蛋机奖励");
			return nativeCommandResult;
		case "JailCommand":
			Warp(fieldDirector, playerCtrl, 300000012, null);
			nativeCommandResult.messages.Add("已送入监狱地图 300000012");
			return nativeCommandResult;
		case "UnJailCommand":
			Warp(fieldDirector, playerCtrl, 100000000, null);
			nativeCommandResult.messages.Add("已离开监狱（100000000）");
			return nativeCommandResult;
		case "WarpMapCommand":
		case "WarpAreaCommand":
			Need(a, 1, "!" + name.Replace("Command", "").ToLowerInvariant() + " <地图ID>");
			Warp(fieldDirector, playerCtrl, N(a), null);
			nativeCommandResult.messages.Add("已传送到地图 " + N(a));
			return nativeCommandResult;
		case "WarpToLifeCommand":
		{
			Need(a, 1, "!warptolife <NPC或怪物ID>");
			int num2 = N(a);
			foreach (KeyValuePair<int, FieldMap> map in FieldCatalog.Maps)
			{
				FieldMap value = map.Value;
				bool flag = false;
				foreach (FieldNpc npc in value.npcs)
				{
					if (npc.id == num2)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					foreach (FieldSpawn spawn in value.spawns)
					{
						if (spawn.mob == num2)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					Warp(fieldDirector, playerCtrl, map.Key, null);
					nativeCommandResult.messages.Add("已在 " + map.Key + " " + value.name + " 找到 " + num2);
					return nativeCommandResult;
				}
			}
			throw new ArgumentException("已解析的地图里没有该生物：" + num2 + "（进图后地图才会解析）");
		}
		case "DebuffCommand":
		{
			NativeBuffs nativeBuffs = NativeBuffs.For(playerCtrl);
			int count = nativeBuffs.Visible.Count;
			nativeBuffs.Dispel();
			nativeCommandResult.messages.Add("已清除 " + count + " 个临时状态");
			return nativeCommandResult;
		}
		case "HealMapCommand":
			playerCtrl.hp = playerCtrl.hpMax;
			playerCtrl.mp = playerCtrl.mpMax;
			GameSession.SaveToDb();
			nativeCommandResult.messages.Add("血蓝已回满（本图只有你一个真人）");
			return nativeCommandResult;
		case "KillMapCommand":
			playerCtrl.hp = 0f;
			GameLoop.I?.OnPlayerDead();
			nativeCommandResult.messages.Add("已按源端语义结束本图玩家（自己）的生命");
			return nativeCommandResult;
		case "ItemVacCommand":
		case "ForceVacCommand":
		{
			if (GroundItem.AuthoritativePickup == null)
			{
				throw new ArgumentException("地面掉落桥未就绪，请重新进入地图");
			}
			List<MapItem> list = new List<MapItem>(MapItemService.DroppedItems(c.map));
			int num3 = 0;
			foreach (MapItem item in list)
			{
				if (GroundItem.AuthoritativePickup(item))
				{
					num3++;
				}
			}
			nativeCommandResult.messages.Add("已捡起 " + num3 + " / " + list.Count + " 件掉落");
			return nativeCommandResult;
		}
		case "ReloadDropsCommand":
			DropStore.Invalidate();
			nativeCommandResult.messages.Add("掉落表缓存已重载");
			return nativeCommandResult;
		case "ReloadShopsCommand":
			ScriptSource.ClearCache();
			nativeCommandResult.messages.Add("商店脚本缓存已重载");
			return nativeCommandResult;
		case "NpcCommand":
		case "PnpcCommand":
		case "PlayerNpcCommand":
		{
			Need(a, 1, "!" + name.Replace("Command", "").ToLowerInvariant() + " <NPCID>");
			int id = N(a);
			Runtime(fieldDirector.Map.id).npcs.Add(new MapNpcPosition
			{
				id = id,
				x = x,
				y = y,
				dynamic = true
			});
			nativeCommandResult.messages.Add("已在当前位置生成 NPC " + id + "（换图后消失）");
			return nativeCommandResult;
		}
		case "PnpcRemoveCommand":
		case "PlayerNpcRemoveCommand":
		case "EraseAllPNpcsCommand":
		{
			int num4 = Runtime(fieldDirector.Map.id).npcs.RemoveAll((MapNpcPosition mapNpcPosition) => mapNpcPosition.dynamic);
			if (num4 > 0)
			{
				fieldDirector.ReloadCurrentMap();
			}
			nativeCommandResult.messages.Add("已移除 " + num4 + " 个动态 NPC" + ((num4 > 0) ? "（重进本图后画面同步）" : ""));
			return nativeCommandResult;
		}
		case "SpawnAllPNpcsCommand":
			throw new ArgumentException("玩家 NPC 用真实角色 id 生成，本服假人不是 NPC 素材，无法按源端生成");
		case "PmobCommand":
		{
			Need(a, 1, "!pmob <怪物ID>");
			int num5 = N(a);
			FieldCatalog.LoadMob(num5);
			if (!FieldCatalog.Mobs.ContainsKey(num5))
			{
				throw new ArgumentException("怪物不存在：" + num5);
			}
			if ((Object)(object)fieldDirector.SpawnNativeMob(num5, ((Component)playerCtrl).transform.position) == (Object)null)
			{
				throw new ArgumentException("怪物生成失败：" + num5);
			}
			nativeCommandResult.messages.Add("已生成永久怪物 " + num5 + "（换图后消失）");
			return nativeCommandResult;
		}
		case "PmobRemoveCommand":
		{
			int num6 = 0;
			EnemyUnit[] array = EnemyUnit.All.ToArray();
			foreach (EnemyUnit enemyUnit in array)
			{
				if ((Object)(object)enemyUnit != (Object)null && !enemyUnit.dead && enemyUnit.fieldMode)
				{
					enemyUnit.TakeBotHit((int)c.id, 2147483647L, 0);
					num6++;
				}
			}
			nativeCommandResult.messages.Add("已清除本图 " + num6 + " 只怪物");
			return nativeCommandResult;
		}
		case "ZakumCommand":
		case "HorntailCommand":
		case "CakeCommand":
		{
			int num9 = ((name == "ZakumCommand") ? 8800000 : ((name == "HorntailCommand") ? 8810000 : 0));
			if (num9 == 0)
			{
				throw new ArgumentException("本移植没有巨型蛋糕 BOSS 的素材与血量配置，请用 !spawn <怪物ID>");
			}
			if ((Object)(object)fieldDirector.SpawnNativeMob(num9, ((Component)playerCtrl).transform.position) == (Object)null)
			{
				throw new ArgumentException("BOSS 生成失败：" + num9);
			}
			nativeCommandResult.messages.Add("已生成 " + ((name == "ZakumCommand") ? "扎昆主体 8800000" : "黑龙主体 8810000") + "（多部件联合体未迁移，部件请用 !spawn）");
			return nativeCommandResult;
		}
		case "OpenPortalCommand":
		case "ClosePortalCommand":
		{
			Need(a, 1, "!" + name.Replace("Command", "").ToLowerInvariant() + " <传送门名>");
			FieldPortal fieldPortal = fieldDirector.Map.Portal(a[0]) ?? fieldDirector.Map.Portal(a[0].ToUpperInvariant());
			if (fieldPortal == null)
			{
				throw new ArgumentException("本图没有这个传送门：" + a[0]);
			}
			fieldPortal.enabled = name == "OpenPortalCommand";
			nativeCommandResult.messages.Add("传送门 " + fieldPortal.name + " 已" + (fieldPortal.enabled ? "开启" : "关闭"));
			return nativeCommandResult;
		}
		case "NightCommand":
		{
			NativeWeatherService obj = GameServer.Local?.Weather ?? throw new ArgumentException("天气服务未启动");
			obj.Override(now: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sky: null, minute: 1320, duration: 1800000L);
			obj.Touch();
			nativeCommandResult.messages.Add("已按 22:00 夜色覆盖（30 分钟）");
			return nativeCommandResult;
		}
		case "MuteMapCommand":
			GameMute.MapMuted = !GameMute.MapMuted;
			nativeCommandResult.messages.Add("当前地图聊天已" + (GameMute.MapMuted ? "禁言" : "恢复"));
			return nativeCommandResult;
		case "ToggleCouponCommand":
		{
			bool flag2 = !GameConfigDao.GetBool(db, "rate_coupon_enabled", fallback: true);
			DbRow dbRow = db.Table("game_config").Find((DbRow r) => DbStore.Str(r, "config_code") == "rate_coupon_enabled");
			if (dbRow == null)
			{
				db.Table("game_config").Insert(new DbRow
				{
					["config_code"] = "rate_coupon_enabled",
					["config_type"] = "world",
					["config_value"] = (flag2 ? "1" : "0")
				});
			}
			else
			{
				dbRow["config_value"] = (flag2 ? "1" : "0");
			}
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("倍率卡已" + (flag2 ? "开启" : "关闭"));
			return nativeCommandResult;
		}
		case "ReloadHuntCommand":
			QuestCatalog.ClearCache();
			nativeCommandResult.messages.Add("狩猎任务配置已重载");
			return nativeCommandResult;
		case "ChatCommand":
			throw new ArgumentException("源端只切换 GM 频道的白字显示；本移植聊天窗没有 GM 频道分支，开关不会改变任何显示");
		case "ShowMoveLifeCommand":
			throw new ArgumentException("本移植的生物移动是 BotScheduler/原生控制器的本地状态机，没有可打印的移动包链路");
		case "DcCommand":
		case "DCAllCommand":
			nativeCommandResult.messages.Add("已断开当前会话（本服只有你一个在线角色）");
			GameSession.DeselectChar();
			GameLoop.I?.ExitToCharSelect();
			return nativeCommandResult;
		case "ShutdownCommand":
			nativeCommandResult.messages.Add("服务端进程结束");
			Application.Quit();
			return nativeCommandResult;
		case "ShowSessionsCommand":
			nativeCommandResult.messages.Add("账号 " + GameSession.AccountId + " / 角色 " + c.name + "(" + c.id + ") 地图 " + c.map);
			nativeCommandResult.messages.Add("假人 " + BotRegistry.Count() + " 只，本图 " + CountOnMap(fieldDirector.Map.id) + " 只");
			return nativeCommandResult;
		case "ShowPacketsCommand":
		case "MonitorCommand":
		case "MonitorsCommand":
			GmDiagnostics.PacketLog = !GmDiagnostics.PacketLog;
			nativeCommandResult.messages.Add("封包日志打印已" + (GmDiagnostics.PacketLog ? "开启" : "关闭") + "（输出到 Unity 日志）");
			return nativeCommandResult;
		case "ArtificialPlayerCommand":
			if (a.Length != 0 && (a[0] == "remove" || a[0] == "delete"))
			{
				int num8 = 0;
				foreach (BotCharacter item2 in new List<BotCharacter>(BotRegistry.OnMap(fieldDirector.Map.id)))
				{
					BotGeneration.RemoveBotFromServer(item2);
					num8++;
				}
				nativeCommandResult.messages.Add("已移除本图 " + num8 + " 只假人");
				return nativeCommandResult;
			}
			BotGeneration.CreateBot(x, y, fieldDirector.Map.id, ignoreEnvironmentLimit: true);
			nativeCommandResult.messages.Add("已在当前位置生成一只假人");
			return nativeCommandResult;
		case "BotMoveCommand":
		{
			Need(a, 3, "!move <假人名> <x> <y>");
			BotCharacter botCharacter = null;
			foreach (BotCharacter item3 in BotRegistry.OnMap(fieldDirector.Map.id))
			{
				if (string.Equals(item3.name, a[0], StringComparison.OrdinalIgnoreCase))
				{
					botCharacter = item3;
					break;
				}
			}
			if (botCharacter == null)
			{
				throw new ArgumentException("本图没有这个假人：" + a[0]);
			}
			BotGeneration.WarpBotToLocation(botCharacter, N(a, 1), N(a, 2), fieldDirector.Map.id);
			nativeCommandResult.messages.Add("已移动 " + botCharacter.name);
			return nativeCommandResult;
		}
		case "EnvironmentCommand":
			if (a.Length != 0 && a[0] == "limit")
			{
				if (a.Length > 1 && a[1] == "off")
				{
					BotGeneration.DisableEnvironmentBotLimit();
				}
				else
				{
					BotGeneration.EnableEnvironmentBotLimit();
				}
				nativeCommandResult.messages.Add("环境假人配额已" + ((a.Length > 1 && a[1] == "off") ? "关闭" : "开启"));
				return nativeCommandResult;
			}
			nativeCommandResult.messages.Add("假人总数 " + BotRegistry.Count() + "，本图 " + CountOnMap(fieldDirector.Map.id) + " 只" + (BotGeneration.EnvironmentBotLimitReached() ? "（已到环境配额）" : ""));
			return nativeCommandResult;
		case "TimerCommand":
		case "TimerMapCommand":
		case "TimerAllCommand":
		{
			int num = ((a.Length != 0) ? N(a) : 0);
			if (num <= 0)
			{
				throw new ArgumentException("用法：!" + name.Replace("Command", "").ToLowerInvariant() + " <秒>");
			}
			GmTimers.Add(GameSession.CharacterId, num);
			nativeCommandResult.messages.Add("已设置 " + num + " 秒计时，结束时会在聊天窗提示");
			return nativeCommandResult;
		}
		case "RipCommand":
			Need(a, 1, "!rip <角色名> [原因]");
			NativeSystemMessage.Drop(6, "[封禁公告] " + a[0] + ((a.Length > 1) ? ("：" + raw.Substring(raw.IndexOf(' ') + 1)) : " 已被封禁"));
			nativeCommandResult.messages.Add("公告已发送");
			return nativeCommandResult;
		case "FlyCommand":
			throw new ArgumentException("本移植的移动物理没有飞行状态（NativeMovementPhysics 只有走/跳/爬），该指令无从生效");
		case "MobSkillCommand":
			throw new ArgumentException("怪物技能只有 NativeMobController 的自动施放链路，没有「给全图怪物指定技能」的入口");
		case "BombCommand":
			throw new ArgumentException("炸弹技能属于怪物技能表，本移植只迁移了技能视觉效果，没有该技能的伤害结算");
		case "MaxEnergyCommand":
			throw new ArgumentException("道场（Mu Lung Dojo）系统未迁移，没有能量条可加");
		case "SeedCommand":
			throw new ArgumentException("妙月种子是源端自定义道具集，本移植没有该道具清单");
		case "FishDropCommand":
			throw new ArgumentException("钓鱼系统未迁移");
		case "MapOwnerClaimCommand":
			throw new ArgumentException("地图归属是多人玩法，本服只有你一个真人");
		case "PeCommand":
			throw new ArgumentException("「读文件回放封包」需要抓包文件与真实 socket 层，本移植是进程内传输");
		case "DevtestCommand":
			throw new ArgumentException("devtest.js 依赖源端脚本宿主的测试接口，本移植没有对应脚本");
		case "StartEventCommand":
		case "EndEventCommand":
		case "StartMapEventCommand":
		case "StopMapEventCommand":
		case "JoinEventCommand":
		case "LeaveEventCommand":
			throw new ArgumentException("源端活动（GameEvent 管理/官方地图活动）未迁移；野外 BOSS 事件由地图脚本自行触发");
		case "OPQCommands":
		case "ReactorCommands":
		case "TestDevCommand":
		case "TradeBotTestCommand":
		case "FMBotCommand":
		case "ArtificialFreeMarketCommand":
			throw new ArgumentException("这是源端 gm4 的开发测试指令，本移植没有对应的测试脚本入口");
		default:
			return NativeCommandResult.Fail("此命令尚未接入：" + name);
		}
	}

	private static MapRuntime Runtime(int mapId)
	{
		MapRuntime mapRuntime = MapService.Get(mapId);
		if (mapRuntime == null)
		{
			BotRuntime botRuntime = GameServer.Local?.Bots;
			if (botRuntime != null)
			{
				try
				{
					mapRuntime = botRuntime.GetMap(mapId);
				}
				catch (ArgumentException)
				{
					mapRuntime = null;
				}
			}
		}
		if (mapRuntime == null)
		{
			throw new ArgumentException("当前地图没有运行时几何，无法动态生成：请重新进入地图");
		}
		return mapRuntime;
	}

	private static int CountOnMap(int mapId)
	{
		int num = 0;
		foreach (BotCharacter item in BotRegistry.OnMap(mapId))
		{
			_ = item;
			num++;
		}
		return num;
	}

	private static int N(string[] a, int i = 0)
	{
		if (a.Length <= i)
		{
			throw new ArgumentException("缺少命令参数");
		}
		return NativeGmCommands.Number(a[i]);
	}

	private static void Need(string[] a, int count, string usage)
	{
		if (a.Length < count)
		{
			throw new ArgumentException("用法：" + usage);
		}
	}

	public static NativeCommandResult Execute(CharRecord c, string name, string[] a, string raw)
	{
		//IL_094a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0974: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1043: Unknown result type (might be due to invalid IL or missing references)
		//IL_1054: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		FieldDirector fieldDirector = GameLoop.I?.field;
		if ((Object)(object)playerCtrl == (Object)null || fieldDirector?.Map == null)
		{
			return NativeCommandResult.Fail("请先进入地图");
		}
		NativeCommandResult nativeCommandResult = new NativeCommandResult();
		DbStore db = GameSession.Server.Db;
		int id;
		switch (name)
		{
		case "OpenSymbolCommand":
			NativeSymbolWindow.Open();
			return nativeCommandResult;
		case "OpenHyperStatCommand":
			NativeHyperStatWindow.Open();
			return nativeCommandResult;
		case "OpenInnerAbilityCommand":
			NativeInnerAbilityWindow.Open();
			return nativeCommandResult;
		case "OpenVMatrixCommand":
			NativeVMatrixWindow.Open();
			return nativeCommandResult;
		case "OpenHexaMatrixCommand":
			NativeHexaMatrixWindow.Open();
			return nativeCommandResult;
		case "OpenFlameCommand":
			NativeEnhancementWindow.Open(4);
			return nativeCommandResult;
		case "OpenSoulCommand":
			NativeEnhancementWindow.Open(5);
			return nativeCommandResult;
		case "WarpCommand":
			Warp(fieldDirector, playerCtrl, N(a), (a.Length > 1) ? a[1] : null);
			break;
		case "GotoCommand":
		{
			if (a.Length == 0)
			{
				nativeCommandResult.messages.Add(string.Join(", ", NativeGmDestinations.Maps.Keys));
				return nativeCommandResult;
			}
			if (!NativeGmDestinations.Maps.TryGetValue(a[0], out var value2))
			{
				throw new ArgumentException("未知地点，可用地点：" + string.Join(", ", NativeGmDestinations.Maps.Keys));
			}
			Warp(fieldDirector, playerCtrl, value2, null);
			break;
		}
		case "PosCommand":
			nativeCommandResult.messages.Add("地图 " + fieldDirector.Map.id + "，x=" + Mathf.RoundToInt(playerCtrl.FeetPos.x * 40f) + "，y=" + Mathf.RoundToInt((0f - playerCtrl.FeetPos.y) * 40f));
			return nativeCommandResult;
		case "BossHpCommand":
		case "MobHpCommand":
		{
			EnemyUnit[] array = EnemyUnit.All.ToArray();
			foreach (EnemyUnit enemyUnit2 in array)
			{
				if ((Object)(object)enemyUnit2 != (Object)null && !enemyUnit2.dead && (name == "MobHpCommand" || enemyUnit2.IsBoss))
				{
					nativeCommandResult.messages.Add(enemyUnit2.fieldMobId + " " + enemyUnit2.label + " HP " + (enemyUnit2.NativeController?.CurrentHp ?? enemyUnit2.hp) + "/" + (enemyUnit2.NativeController?.MaximumHp ?? enemyUnit2.hpMax));
				}
			}
			if (nativeCommandResult.messages.Count == 0)
			{
				nativeCommandResult.messages.Add("当前地图没有符合条件的怪物");
			}
			return nativeCommandResult;
		}
		case "SpawnCommand":
		{
			id = N(a);
			int num = ((a.Length <= 1) ? 1 : N(a, 1));
			if (num < 1)
			{
				throw new ArgumentException("召唤数量必须大于0");
			}
			FieldCatalog.LoadMob(id);
			if (!FieldCatalog.Mobs.ContainsKey(id))
			{
				throw new ArgumentException("怪物不存在：" + id);
			}
			for (int num7 = 0; num7 < num; num7++)
			{
				if ((Object)(object)fieldDirector.SpawnNativeMob(id, ((Component)playerCtrl).transform.position) == (Object)null)
				{
					throw new ArgumentException("怪物生成失败：" + id);
				}
			}
			break;
		}
		case "PinkbeanCommand":
		case "PapCommand":
		case "PianusCommand":
			if (name == "PinkbeanCommand")
			{
				id = 8820001;
			}
			else
			{
				id = ((name == "PapCommand") ? 8500001 : 8510000);
			}
			if ((Object)(object)fieldDirector.SpawnNativeMob(id, ((Component)playerCtrl).transform.position) == (Object)null)
			{
				throw new ArgumentException("怪物生成失败：" + id);
			}
			break;
		case "KillAllCommand":
		{
			int num = 0;
			EnemyUnit[] array = EnemyUnit.All.ToArray();
			foreach (EnemyUnit enemyUnit in array)
			{
				if ((Object)(object)enemyUnit != (Object)null && !enemyUnit.dead && NativeCombatData.Int(enemyUnit.CombatStats, "friendly") == 0 && (enemyUnit.fieldMobId < 8810010 || enemyUnit.fieldMobId > 8810018))
				{
					enemyUnit.TakeBotHit((int)c.id, 2147483647L, 0);
					num++;
				}
			}
			nativeCommandResult.messages.Add("已攻击 " + num + " 只怪物");
			return nativeCommandResult;
		}
		case "BuffMeCommand":
		case "BuffMapCommand":
		case "EmpowerMeCommand":
		case "BuffCommand":
		case "HideCommand":
		case "UnHideCommand":
		{
			int[] array2;
			switch (name)
			{
			default:
				array2 = new int[1] { N(a) };
				break;
			case "HideCommand":
			case "UnHideCommand":
				array2 = new int[1] { 9101004 };
				break;
			case "EmpowerMeCommand":
				array2 = new int[17]
				{
					2311003, 2301004, 1301007, 4101004, 2001002, 1101007, 1005, 2301003, 5121009, 1111002,
					4111001, 4111002, 4211003, 4211005, 1321000, 2321004, 3121002
				};
				break;
			case "BuffMapCommand":
				array2 = new int[5] { 9101001, 9101002, 9101003, 9101008, 1005 };
				break;
			case "BuffMeCommand":
				array2 = new int[5] { 4101004, 2311003, 1301007, 2301004, 1005 };
				break;
			}
			int[] array3 = array2;
			for (int i = 0; i < array3.Length; i++)
			{
				int num4 = array3[i];
				if (num4 == 9101004 && NativeBuffs.For(playerCtrl).HasSource(num4))
				{
					NativeBuffs.For(playerCtrl).Cancel(num4);
					continue;
				}
				int num5 = SkillBook.MaxLevelOf(num4);
				if (num5 <= 0)
				{
					throw new ArgumentException("技能不存在：" + num4);
				}
				if (!NativeBuffs.For(playerCtrl).ApplySkill(num4, num5, NativeBuffs.Now))
				{
					throw new ArgumentException("技能效果无法应用：" + num4);
				}
			}
			if (name == "BuffMeCommand")
			{
				playerCtrl.hp = playerCtrl.hpMax;
				playerCtrl.mp = playerCtrl.mpMax;
			}
			GameSession.SaveToDb();
			break;
		}
		case "MusicCommand":
		{
			Need(a, 1, "!music <Bgm00/SleepyWood>");
			string text = null;
			string[] songs = NativeGmDestinations.Songs;
			foreach (string text2 in songs)
			{
				if (string.Equals(raw, text2, StringComparison.OrdinalIgnoreCase))
				{
					text = text2;
					break;
				}
			}
			if (text == null || !AudioManager.Has(text))
			{
				throw new ArgumentException("音乐不存在：" + raw);
			}
			AudioManager.PlayBgm(text);
			break;
		}
		case "ItemDropCommand":
		{
			id = N(a);
			int num8 = ((a.Length <= 1) ? 1 : N(a, 1));
			if (BotItemCatalog.Shared.Info(id) == null || num8 < 1 || num8 > 32767)
			{
				throw new ArgumentException("物品或数量无效");
			}
			bool flag = id / 1000 == 5000;
			if (flag && a.Length < 2)
			{
				throw new ArgumentException("用法：!drop <宠物ID> <天数>");
			}
			if (GameConfigDao.GetBool(db, "block_generate_cash_item") && BotItemCatalog.Int(BotItemCatalog.Shared.Info(id), "cash") != 0)
			{
				throw new ArgumentException("禁止生成现金物品");
			}
			InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanItem(id, flag ? 1 : num8);
			inventoryEntry.owner = c.name;
			if (flag)
			{
				inventoryEntry.petId = (int)db.Table("pets").Insert(new DbRow
				{
					["name"] = BotItemCatalog.Shared.Name(id),
					["level"] = 1L,
					["closeness"] = 0L,
					["fullness"] = 100L,
					["summoned"] = 0L,
					["flag"] = 0L
				});
				inventoryEntry.expiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)num8 * 86400000L;
				inventoryEntry.owner = "";
				db.Save();
			}
			if (c.gm < 3)
			{
				inventoryEntry.flag |= 328;
				inventoryEntry.owner = "TRIAL-MODE";
			}
			MapItemService.SpawnItemDrop(c.map, (int)c.id, (int)c.id, 0, inventoryEntry, playerCtrl.FeetPos.x * 40f, (0f - playerCtrl.FeetPos.y) * 40f, ffaDrop: true, playerDrop: true);
			break;
		}
		case "DropLimitCommand":
			nativeCommandResult.messages.Add("当前掉落 " + MapItemService.DroppedItemCount(c.map) + " / " + MapItemService.ItemLimitOnMap);
			return nativeCommandResult;
		case "ClearDropsCommand":
			MapItemService.ClearMap(c.map);
			break;
		case "LevelCommand":
		case "LevelProCommand":
		{
			int num6 = Mathf.Clamp(N(a), 1, (Profile.JobId >= 1000 && Profile.JobId < 2000) ? 120 : ExpTable.MaxLevel);
			if (name == "LevelCommand")
			{
				Profile.Data.level = num6 - 1;
				Profile.Data.experience = 0L;
			}
			LevelSystem.SetLevel(Profile.Data, num6);
			playerCtrl.ApplyProgression();
			playerCtrl.hp = playerCtrl.hpMax;
			playerCtrl.mp = playerCtrl.mpMax;
			GameSession.SaveToDb();
			break;
		}
		case "JobCommand":
			Need(a, 1, "!job [角色名] <职业ID>");
			Self(c, (a.Length > 1) ? a[0] : c.name);
			id = N(a, (a.Length > 1) ? 1 : 0);
			if (id < 0 || id >= 2200 || !JobDefs.IsValid(id))
			{
				throw new ArgumentException("职业不存在：" + id);
			}
			Profile.SetJob(id);
			playerCtrl.ApplyProgression();
			playerCtrl.RefreshFieldAppearance();
			GameSession.SaveToDb();
			break;
		case "MaxSkillCommand":
		case "ResetSkillCommand":
		{
			SkillBook.Ensure();
			List<int> list = new List<int>();
			JobDef jobDef = JobDefs.Get(0);
			if (jobDef != null && jobDef.allSkills != null)
			{
				list.AddRange(jobDef.allSkills);
			}
			JobDef jobDef2 = JobDefs.Get(Profile.JobId);
			if (jobDef2 != null && jobDef2.allSkills != null)
			{
				foreach (int allSkill in jobDef2.allSkills)
				{
					if (!list.Contains(allSkill))
					{
						list.Add(allSkill);
					}
				}
			}
			if (list.Count == 0)
			{
				throw new ArgumentException("当前职业没有可加的技能：" + Profile.JobId);
			}
			foreach (int item in list)
			{
				int num2 = SkillBook.MaxLevelOf(item);
				if (num2 > 0)
				{
					SkillProgress.SetLearned(item, (name == "MaxSkillCommand") ? num2 : 0);
				}
			}
			SkillProgress.SetLearned((Profile.JobId >= 2000 && Profile.JobId < 2200) ? 5001005 : 21001001, 0);
			nativeCommandResult.messages.Add(((name == "MaxSkillCommand") ? "技能全满：" : "技能清零：") + JobDefs.NameOf(Profile.JobId) + "（job " + Profile.JobId + "）共 " + list.Count + " 个");
			playerCtrl.ApplyProgression();
			GameSession.SaveToDb();
			break;
		}
		case "FaceCommand":
		case "HairCommand":
		{
			Need(a, 1, "!face / !hair [角色名] <ID>");
			Self(c, (a.Length > 1) ? a[0] : c.name);
			id = N(a, (a.Length > 1) ? 1 : 0);
			string text3 = ((name == "FaceCommand") ? "Face" : "Hair");
			if (((text3 == "Face") ? (id / 10000 != 2) : (id / 10000 != 3)) || WzAssets.FileRoot("Character/" + text3 + "/" + id.ToString("D8") + ".img") == null)
			{
				throw new ArgumentException("外观不存在：" + id);
			}
			if (text3 == "Face")
			{
				c.face = id;
			}
			else
			{
				c.hair = id;
			}
			CharactersDao.Save(db, c);
			db.Save();
			nativeCommandResult.changed = true;
			break;
		}
		case "SearchCommand":
		case "IdCommand":
		{
			Need(a, 2, "!search <item|map|mob|skill> <关键词>");
			string query = string.Join(" ", a, 1, a.Length - 1);
			int num = 0;
			if (a[0] == "item")
			{
				foreach (int itemId in BotItemCatalog.Shared.ItemIds)
				{
					string text4 = BotItemCatalog.Shared.Name(itemId);
					if (Match(itemId, text4, query))
					{
						nativeCommandResult.messages.Add(itemId + " " + text4);
						if (++num >= 100)
						{
							break;
						}
					}
				}
			}
			else if (a[0] == "map")
			{
				foreach (KeyValuePair<int, FieldMap> map in FieldCatalog.Maps)
				{
					if (Match(map.Key, map.Value.name, query))
					{
						nativeCommandResult.messages.Add(map.Key + " " + map.Value.name);
						if (++num >= 100)
						{
							break;
						}
					}
				}
			}
			else if (a[0] == "mob")
			{
				foreach (KeyValuePair<int, FieldMob> mob in FieldCatalog.Mobs)
				{
					if (Match(mob.Key, mob.Value.name, query))
					{
						nativeCommandResult.messages.Add(mob.Key + " " + mob.Value.name);
						if (++num >= 100)
						{
							break;
						}
					}
				}
			}
			else
			{
				if (!(a[0] == "skill"))
				{
					throw new ArgumentException("搜索类型：item、map、mob、skill");
				}
				foreach (int item2 in NativeSkillIds())
				{
					SkillData skillData = SkillBook.Get(item2);
					if (Match(item2, skillData?.name, query))
					{
						nativeCommandResult.messages.Add(item2 + " " + skillData?.name);
						if (++num >= 100)
						{
							break;
						}
					}
				}
			}
			if (num == 0)
			{
				nativeCommandResult.messages.Add("没有搜索结果");
			}
			return nativeCommandResult;
		}
		case "WhatDropsFromCommand":
		{
			id = N(a);
			if (!FieldCatalog.Drops.TryGetValue(id, out var value3))
			{
				nativeCommandResult.messages.Add("没有掉落记录");
				return nativeCommandResult;
			}
			{
				foreach (FieldDropEntry item3 in value3)
				{
					nativeCommandResult.messages.Add(item3.item + " " + BotItemCatalog.Shared.Name(item3.item) + " × " + item3.min + "~" + item3.max + " 概率 " + item3.chance + "/999999");
				}
				return nativeCommandResult;
			}
		}
		case "WhoDropsCommand":
			id = N(a);
			foreach (KeyValuePair<int, List<FieldDropEntry>> drop in FieldCatalog.Drops)
			{
				if (drop.Value.Exists((FieldDropEntry d) => d.item == id))
				{
					nativeCommandResult.messages.Add(drop.Key + " " + (FieldCatalog.Mobs.TryGetValue(drop.Key, out var value) ? value.name : ""));
				}
			}
			if (nativeCommandResult.messages.Count == 0)
			{
				nativeCommandResult.messages.Add("没有掉落记录");
			}
			return nativeCommandResult;
		case "ReloadEventsCommand":
			ScriptSource.ClearCache();
			break;
		case "ClearQuestCacheCommand":
			QuestCatalog.ClearCache();
			break;
		case "ClearQuestCommand":
			QuestCatalog.ClearCache(N(a));
			break;
		case "QuestStartCommand":
		case "QuestCompleteCommand":
		case "QuestResetCommand":
		{
			id = N(a);
			if (!QuestCatalog.Exists(id))
			{
				throw new ArgumentException("任务不存在：" + id);
			}
			int num3 = Profile.QuestStatus(id);
			if (name == "QuestStartCommand")
			{
				if (num3 != 0)
				{
					throw new ArgumentException("任务已开始或完成");
				}
				Profile.StartQuest(id);
			}
			else if (name == "QuestCompleteCommand")
			{
				if (num3 != 1)
				{
					throw new ArgumentException("任务尚未开始");
				}
				Profile.CompleteQuest(id);
				QuestNotice.ShowCompleteEffect();
			}
			else
			{
				Profile.ResetQuest(id);
			}
			GameSession.SaveToDb();
			break;
		}
		case "GmShopCommand":
			NativeNpcShopWindow.OpenShop(1337, fallback: false);
			if (!NativeNpcShopWindow.Visible)
			{
				throw new ArgumentException("GM商店1337无法打开");
			}
			break;
		case "GachaListCommand":
			NpcSession.Talk(9900001, "gachaponInfo");
			if (NpcSession.Active?.LastError != null)
			{
				throw new ArgumentException(NpcSession.Active.LastError);
			}
			break;
		case "ReloadMapCommand":
			fieldDirector.ReloadCurrentMap();
			break;
		case "ReloadPortalsCommand":
			ScriptSource.ClearCache();
			break;
		case "DisposeCommand":
		case "UnBugCommand":
			NpcSession.CloseActive();
			playerCtrl.CancelRush();
			playerCtrl.FinishHeldSkill();
			break;
		case "NoticeCommand":
			Need(a, 1, "!notice <内容>");
			NativeSystemMessage.Drop(6, raw);
			return nativeCommandResult;
		case "DebugCommand":
		case "CheckDmgCommand":
			if (a.Length != 0)
			{
				Self(c, a[0]);
			}
			nativeCommandResult.messages.Add(c.name + " Lv." + Profile.level + " Job." + Profile.JobId + " HP " + playerCtrl.hp + "/" + playerCtrl.hpMax + " MP " + playerCtrl.mp + "/" + playerCtrl.mpMax + " ATK " + playerCtrl.EffectiveAtk);
			return nativeCommandResult;
		default:
			return Extended(c, name, a, raw);
		}
		nativeCommandResult.messages.Add("命令执行完成");
		return nativeCommandResult;
	}

	private static bool Match(int id, string label, string query)
	{
		if (!id.ToString().Contains(query))
		{
			return (label ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static IEnumerable<int> NativeSkillIds()
	{
		ImgNode imgNode = WzAssets.FileRoot("String/Skill.img");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				if (int.TryParse(child.Name, out var result))
				{
					yield return result;
				}
			}
			yield break;
		}
		HashSet<int> seen = new HashSet<int>();
		foreach (JobDef item in JobDefs.All)
		{
			if (item.allSkills == null)
			{
				continue;
			}
			foreach (int allSkill in item.allSkills)
			{
				if (seen.Add(allSkill))
				{
					yield return allSkill;
				}
			}
		}
	}

	private static void Self(CharRecord c, string name)
	{
		if (!string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase) && c.id.ToString() != name)
		{
			throw new ArgumentException("角色不在线：" + name);
		}
	}

	private static void Warp(FieldDirector field, PlayerCtrl player, int id, string portal)
	{
		if (player.hp <= 0f)
		{
			throw new ArgumentException("死亡状态下不能传送");
		}
		FieldMap map = FieldCatalog.GetMap(id);
		if (map == null)
		{
			throw new ArgumentException("地图不存在：" + id);
		}
		int portalIndex = -1;
		if (portal != null && map.Portal(portal) == null && int.TryParse(portal, out var result))
		{
			portalIndex = result;
			portal = null;
		}
		field.WarpTo(id, portal, portalIndex);
	}
}
