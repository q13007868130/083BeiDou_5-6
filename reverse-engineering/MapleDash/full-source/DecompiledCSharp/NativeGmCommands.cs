using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Bot;
using Server.Bot.Trade;

public sealed class NativeGmCommands
{
	private readonly DbStore db;

	private readonly DateTime started = DateTime.UtcNow;

	public Func<CharRecord, string, string[], string, NativeCommandResult> Host;

	public NativeGmCommands(DbStore db)
	{
		this.db = db;
	}

	public static bool IsCommand(CharRecord actor, string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (text[0] != '@')
			{
				if (actor.gm > 0)
				{
					return text[0] == '!';
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public void EnsureSeed()
	{
		if (db.Table("command_info").rows.Count != 0)
		{
			return;
		}
		string[] array = "\nhelp|HelpCommand|0\ncommands|HelpCommand|0\ndroplimit|DropLimitCommand|0\ntime|TimeCommand|0\ncredits|StaffCommand|0\nuptime|UptimeCommand|0\ngacha|GachaCommand|0\ndispose|DisposeCommand|0\nchangel|ChangeLanguageCommand|0\nequiplv|EquipLvCommand|0\nshowrates|ShowRatesCommand|0\nrates|RatesCommand|0\nonline|OnlineCommand|0\ngm|GmCommand|0\nreportbug|ReportBugCommand|0\npoints|ReadPointsCommand|0\njoinevent|JoinEventCommand|0\nleaveevent|LeaveEventCommand|0\nranks|RanksCommand|0\nstr|StatStrCommand|0\ndex|StatDexCommand|0\nint|StatIntCommand|0\nluk|StatLukCommand|0\nenableauth|EnableAuthCommand|0\ntoggleexp|ToggleExpCommand|0\nmylawn|MapOwnerClaimCommand|0\nbosshp|BossHpCommand|1\nmobhp|MobHpCommand|1\nwhatdropsfrom|WhatDropsFromCommand|1\nwhodrops|WhoDropsCommand|1\nbuffme|BuffMeCommand|1\ngoto|GotoCommand|1\nrecharge|RechargeCommand|2\nwhereami|WhereaMiCommand|2\nhide|HideCommand|2\nunhide|UnHideCommand|2\nsp|SpCommand|2\nap|ApCommand|2\nempowerme|EmpowerMeCommand|2\nbuffmap|BuffMapCommand|2\nbuff|BuffCommand|2\nbomb|BombCommand|2\ndc|DcCommand|2\ncleardrops|ClearDropsCommand|2\nclearslot|ClearSlotCommand|2\nclearsavelocs|ClearSavedLocationsCommand|2\nwarp|WarpCommand|2\nwarphere|SummonCommand|2\nsummon|SummonCommand|2\nwarpto|ReachCommand|2\nreach|ReachCommand|2\nfollow|ReachCommand|2\ngmshop|GmShopCommand|2\nheal|HealCommand|2\nitem|ItemCommand|2\ndrop|ItemDropCommand|2\nlevel|LevelCommand|2\nlevelpro|LevelProCommand|2\nsetslot|SetSlotCommand|2\nsetstat|SetStatCommand|2\nmaxstat|MaxStatCommand|2\nmaxskill|MaxSkillCommand|2\nresetskill|ResetSkillCommand|2\nsearch|SearchCommand|2\njail|JailCommand|2\nunjail|UnJailCommand|2\njob|JobCommand|2\nunbug|UnBugCommand|2\nid|IdCommand|2\ngachalist|GachaListCommand|2\nloot|LootCommand|2\nmobskill|MobSkillCommand|2\nwarpmap|WarpMapCommand|2\nwarparea|WarpAreaCommand|2\ndebuff|DebuffCommand|3\nfly|FlyCommand|3\nspawn|SpawnCommand|3\nmutemap|MuteMapCommand|3\ncheckdmg|CheckDmgCommand|3\ninmap|InMapCommand|3\nreloadevents|ReloadEventsCommand|3\nreloaddrops|ReloadDropsCommand|3\nreloadportals|ReloadPortalsCommand|3\nreloadmap|ReloadMapCommand|3\nreloadshops|ReloadShopsCommand|3\nhpmp|HpMpCommand|3\nmaxhpmp|MaxHpMpCommand|3\nmusic|MusicCommand|3\nmonitor|MonitorCommand|3\nmonitors|MonitorsCommand|3\nignore|IgnoreCommand|3\nignored|IgnoredCommand|3\npos|PosCommand|3\ntogglecoupon|ToggleCouponCommand|3\ntogglewhitechat|ChatCommand|3\nfame|FameCommand|3\ngivenx|GiveNxCommand|3\ngivevp|GiveVpCommand|3\ngivems|GiveMesosCommand|3\ngiverp|GiveRpCommand|3\nexpeds|ExpedsCommand|3\nkill|KillCommand|3\nseed|SeedCommand|3\nmaxenergy|MaxEnergyCommand|3\nkillall|KillAllCommand|3\nnotice|NoticeCommand|3\nrip|RipCommand|3\nopenportal|OpenPortalCommand|3\ncloseportal|ClosePortalCommand|3\npe|PeCommand|3\nstartevent|StartEventCommand|3\nendevent|EndEventCommand|3\nstartmapevent|StartMapEventCommand|3\nstopmapevent|StopMapEventCommand|3\nonline2|OnlineTwoCommand|3\nban|BanCommand|3\nunban|UnBanCommand|3\nhealmap|HealMapCommand|3\nhealperson|HealPersonCommand|3\nhurt|HurtCommand|3\nkillmap|KillMapCommand|3\nnight|NightCommand|3\nnpc|NpcCommand|3\nface|FaceCommand|3\nhair|HairCommand|3\nstartquest|QuestStartCommand|3\ncompletequest|QuestCompleteCommand|3\nresetquest|QuestResetCommand|3\ntimer|TimerCommand|3\ntimermap|TimerMapCommand|3\ntimerall|TimerAllCommand|3\nreloadhunt|ReloadHuntCommand|3\nservermessage|ServerMessageCommand|4\nproitem|ProItemCommand|4\nseteqstat|SetEqStatCommand|4\nexprate|ExpRateCommand|4\nmesorate|MesoRateCommand|4\ndroprate|DropRateCommand|4\nbossdroprate|BossDropRateCommand|4\nquestrate|QuestRateCommand|4\ntravelrate|TravelRateCommand|4\nfishrate|FishingRateCommand|4\nitemvac|ItemVacCommand|4\nforcevac|ForceVacCommand|4\nzakum|ZakumCommand|4\nhorntail|HorntailCommand|4\npinkbean|PinkbeanCommand|4\npap|PapCommand|4\npianus|PianusCommand|4\ncake|CakeCommand|4\nplayernpc|PlayerNpcCommand|4\nplayernpcremove|PlayerNpcRemoveCommand|4\npnpc|PnpcCommand|4\npnpcremove|PnpcRemoveCommand|4\npmob|PmobCommand|4\npmobremove|PmobRemoveCommand|4\nwarptolife|WarpToLifeCommand|4\nfishdrop|FishDropCommand|4\nbot|ArtificialPlayerCommand|4\nmove|BotMoveCommand|4\nenv|EnvironmentCommand|4\nbetafmshop|ArtificialFreeMarketCommand|4\nfmbot|FMBotCommand|4\ntradebot|TradeBotTestCommand|4\ntest|TestDevCommand|4\nopq|OPQCommands|4\nreactor|ReactorCommands|4\ndebug|DebugCommand|5\nset|SetCommand|5\nshowpackets|ShowPacketsCommand|5\nshowmovelife|ShowMoveLifeCommand|5\nshowsessions|ShowSessionsCommand|5\niplist|IpListCommand|5\nsetgmlevel|SetGmLevelCommand|6\nwarpworld|WarpWorldCommand|6\nsaveall|SaveAllCommand|6\ndcall|DCAllCommand|6\nmapplayers|MapPlayersCommand|6\ngetacc|GetAccCommand|6\nshutdown|ShutdownCommand|6\nclearquestcache|ClearQuestCacheCommand|6\nclearquest|ClearQuestCommand|6\nsupplyratecoupon|SupplyRateCouponCommand|6\nspawnallpnpcs|SpawnAllPNpcsCommand|6\neraseallpnpcs|EraseAllPNpcsCommand|6\naddchannel|ServerAddChannelCommand|6\naddworld|ServerAddWorldCommand|6\nremovechannel|ServerRemoveChannelCommand|6\nremoveworld|ServerRemoveWorldCommand|6\ndevtest|DevtestCommand|6\n扭蛋奖励|GachaCommand|0\n全部加力量|StatStrCommand|0\n全部加敏捷|StatDexCommand|0\n全部加智力|StatIntCommand|0\n全部加运气|StatLukCommand|0\n装备信息|EquipLvCommand|0\n倍率|ShowRatesCommand|0\njk|DisposeCommand|0\n解卡|DisposeCommand|0\n帮助|HelpCommand|0\nsymbol|OpenSymbolCommand|3\nhyperstat|OpenHyperStatCommand|3\ninnerability|OpenInnerAbilityCommand|3\nflame|OpenFlameCommand|3\nsoul|OpenSoulCommand|3\nvmatrix|OpenVMatrixCommand|3\nhexa|OpenHexaMatrixCommand|3\n符文徽章|OpenSymbolCommand|3\n极限属性|OpenHyperStatCommand|3\n内在能力|OpenInnerAbilityCommand|3\n轮回星火|OpenFlameCommand|3\n灵魂武器|OpenSoulCommand|3\nV矩阵|OpenVMatrixCommand|3\nHEXA矩阵|OpenHexaMatrixCommand|3".Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Trim().Split('|');
			if (array2.Length == 3)
			{
				db.Table("command_info").Insert(new DbRow
				{
					["syntax"] = array2[0],
					["clazz"] = array2[1],
					["level"] = long.Parse(array2[2]),
					["default_level"] = long.Parse(array2[2]),
					["enabled"] = 1L
				});
			}
		}
		db.Save();
	}

	public NativeCommandResult Execute(CharRecord actor, string text)
	{
		EnsureSeed();
		if (actor.map == 300000012 && actor.gm == 0)
		{
			return NativeCommandResult.Fail("监狱中不能使用命令");
		}
		if (actor.gm == 0 && GameConfigDao.GetBool(db, "deterred_player_command"))
		{
			return NativeCommandResult.Fail("玩家命令已关闭");
		}
		string text2 = text.Substring(1).Trim().TrimStart('/');
		int num = text2.IndexOfAny(new char[2] { ' ', '/' });
		string syntax = ((num < 0) ? text2 : text2.Substring(0, num)).Trim().ToLowerInvariant();
		if (syntax.Length == 0)
		{
			return NativeCommandResult.Fail("用法：@<命令> [参数]，用 @help 查看可用命令");
		}
		string text3 = ((num < 0) ? "" : text2.Substring(num + 1).Trim());
		string[] a = text3.ToLowerInvariant().Split(new char[2] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
		DbRow dbRow = null;
		int rank;
		for (rank = 0; rank <= 6; rank++)
		{
			if (dbRow != null)
			{
				break;
			}
			dbRow = db.Table("command_info").Find((DbRow r) => DbStore.Int(r, "level") == rank && DbStore.Int(r, "enabled") != 0 && DbStore.Str(r, "syntax").ToLowerInvariant() == syntax);
		}
		if (dbRow == null)
		{
			return NativeCommandResult.Fail("命令不存在或已停用：" + syntax);
		}
		int num2 = DbStore.Int(dbRow, "level");
		if (actor.gm < num2)
		{
			return NativeCommandResult.Fail("没有使用此命令的权限：" + syntax + " 需要 GM" + num2 + "，当前 GM" + actor.gm);
		}
		string name = DbStore.Str(dbRow, "clazz");
		try
		{
			NativeCommandResult nativeCommandResult = Run(actor, name, a, text3);
			if (nativeCommandResult.ok && nativeCommandResult.changed)
			{
				db.Save();
			}
			return nativeCommandResult;
		}
		catch (ArgumentException ex)
		{
			return NativeCommandResult.Fail(ex.Message);
		}
		catch (OverflowException)
		{
			return NativeCommandResult.Fail("参数数值超出范围");
		}
		catch (Exception ex3)
		{
			return NativeCommandResult.Fail("指令执行失败：" + ex3.GetType().Name + " " + ex3.Message);
		}
	}

	public static int Number(string value)
	{
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			throw new ArgumentException("请输入有效整数：" + value);
		}
		return result;
	}

	private static void Need(string[] args, int count, string usage)
	{
		if (args.Length < count)
		{
			throw new ArgumentException("用法：" + usage);
		}
	}

	private CharRecord Target(CharRecord self, string value)
	{
		if (string.Equals(self.name, value, StringComparison.OrdinalIgnoreCase) || self.id.ToString(CultureInfo.InvariantCulture) == value)
		{
			return self;
		}
		throw new ArgumentException("角色不在线：" + value);
	}

	private NativeCommandResult Run(CharRecord c, string name, string[] a, string raw)
	{
		NativeCommandResult nativeCommandResult = new NativeCommandResult();
		switch (name)
		{
		case "HelpCommand":
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = "\nChangeLanguageCommand|更改语言\nDisposeCommand|解卡npc对话\nDropLimitCommand|查看当前地图最大掉落数\nEnableAuthCommand|重置PIC或PIN验证冷却时间\nEquipLvCommand|展示装备等级\nGachaCommand|展示扭蛋机奖励\nGmCommand|给gm发送消息\nHelpCommand|展示可用的gm指令\nJoinEventCommand|参加当前的活动\nLeaveEventCommand|离开当前活动\nMapOwnerClaimCommand|开启地图归属\nOpenSymbolCommand|打开符文徽章窗（ARC/AUT）\nOpenHyperStatCommand|打开极限属性窗\nOpenInnerAbilityCommand|打开内在能力窗\nOpenFlameCommand|打开轮回星火页签\nOpenSoulCommand|打开灵魂武器页签\nOpenVMatrixCommand|打开V矩阵窗\nOpenHexaMatrixCommand|打开HEXA矩阵窗\nOnlineCommand|展示所有在线的玩家信息\nRanksCommand|展示玩家排名信息\nRatesCommand|展示玩家倍率\nReadPointsCommand|展示全部点数\nReportBugCommand|发送漏洞信息\nShowRatesCommand|展示大区和玩家倍率\nStaffCommand|展示服务端参与贡献的人员\nStatDexCommand|将属性点分配到敏捷\nStatIntCommand|将属性点分配到智力\nStatLukCommand|将属性点分配到运气\nStatStrCommand|将属性点分配到力量\nTimeCommand|展示当前服务器时间\nToggleExpCommand|开启或关闭经验获取\nUptimeCommand|展示当前服务器已运行时间\nBossHpCommand|展示当前地图的boss血量\nBuffMeCommand|激活GM buff\nGotoCommand|传送到预设地图\nMobHpCommand|展示当前地图的怪物血量\nWhatDropsFromCommand|查询怪物掉落\nWhoDropsCommand|查询物品掉落\nApCommand|设置属性点\nBombCommand|对玩家使用炸弹技能\nBuffCommand|激活指定buff\nBuffMapCommand|全图激活GM buff\nClearDropsCommand|清空当前玩家的掉落\nClearSavedLocationsCommand|清空玩家所有的存档点\nClearSlotCommand|清空当前玩家的指定背包栏\nDcCommand|强制玩家下线\nEmpowerMeCommand|激活所有可用的buff\nGachaListCommand|展示扭蛋机奖励\nGmShopCommand|打开GM商店\nHealCommand|血蓝加满\nHideCommand|隐身\nIdCommand|根据ID在手册中查询\nItemCommand|生成物品到背包\nItemDropCommand|生成物品到地上\nJailCommand|把某人关进监狱\nJobCommand|玩家转职\nLevelCommand|自定义等级，直接更改\nLevelProCommand|自定义等级，一级一级升\nLootCommand|展示您拥有的所有物品\nMaxSkillCommand|技能全满\nMaxStatCommand|状态全满\nMobSkillCommand|给当前地图的所有怪物应用指定技能\nReachCommand|传送到指定玩家的位置\nRechargeCommand|充值所有能够充值的消耗品\nResetSkillCommand|重置技能\nSearchCommand|全局搜索\nSetSlotCommand|设置所有背包格子数\nSetStatCommand|设置角色全部属性\nSpCommand|设置技能点\nSummonCommand|传送指定玩家到您的位置\nUnBugCommand|解卡动作\nUnHideCommand|取消隐身\nUnJailCommand|把某人从监狱中释放\nWarpAreaCommand|把当前地图附近的玩家传送到指定的地图\nWarpCommand|传送到指定地图\nWarpMapCommand|把当前地图的所有玩家传送到指定的地图\nWhereaMiCommand|展示当前地图信息\nBanCommand|封禁玩家\nChatCommand|开关GM聊天模式\nCheckDmgCommand|展示某个玩家的状态和伤害\nClosePortalCommand|关闭当前地图的传送点\nDebuffCommand|给附近玩家上debuff\nEndEventCommand|结束当前正在进行的活动\nExpedsCommand|展示所有正在进行的远征\nFaceCommand|改变玩家的脸型\nFameCommand|设置玩家人气\nFlyCommand|开启或关闭飞行模式\nGiveMesosCommand|给玩家发放金币\nGiveNxCommand|给玩家发放点券\nGiveRpCommand|给玩家发放奖励点数\nGiveVpCommand|给玩家发放投票点数\nHairCommand|改变玩家的发型\nHealMapCommand|回满全图血蓝\nHealPersonCommand|回满玩家血蓝\nHpMpCommand|设置玩家HP/MP\nHurtCommand|伤害玩家到1HP\nIgnoreCommand|添加或移除玩家的自动封禁白名单\nIgnoredCommand|展示所有自动封禁白名单玩家\nInMapCommand|展示当前地图的所有玩家\nKillAllCommand|击杀当前地图所有怪物\nKillCommand|击杀玩家\nKillMapCommand|击杀当前地图的所有玩家\nMaxEnergyCommand|道场能量条加满\nMaxHpMpCommand|设置玩家最大HP/MP\nMonitorCommand|开启或关闭监控某个玩家的发包\nMonitorsCommand|开启或关闭监控所有玩家的发包\nMusicCommand|在当前地图播放音乐\nMuteMapCommand|开启或关闭禁言\nNightCommand|将当前地图设置为夜晚\nNoticeCommand|发送全服公告\nNpcCommand|在当前的位置生成一个NPC\nOnlineTwoCommand|展示所有在线玩家\nOpenPortalCommand|开启当前地图的传送点\nPeCommand|从文件中读取发包内容模拟客户端发送\nPosCommand|展示玩家当前位置信息\nQuestCompleteCommand|完成一个任务\nQuestResetCommand|重置一个任务\nQuestStartCommand|开始一个任务\nReloadDropsCommand|重载掉落\nReloadEventsCommand|重载活动\nReloadHuntCommand|重新加载狩猎任务配置并重新发布任务\nReloadMapCommand|重载地图\nReloadPortalsCommand|重载传送点\nReloadShopsCommand|重载商店\nRipCommand|发送全服封禁公告\nSeedCommand|掉落所有的妙月种子\nSpawnCommand|在当前位置生成怪物\nStartEventCommand|在当前地图开启一个活动\nStartMapEventCommand|开启当前地图的官方活动，仅活动地图有效\nStopMapEventCommand|停止当前地图的官方活动，仅活动地图有效\nTimerAllCommand|给当前大区设置一个计时器\nTimerCommand|给玩家设置一个计时器\nTimerMapCommand|给当前地图设置一个计时器\nToggleCouponCommand|开启或关闭倍率卡\nUnBanCommand|解封玩家\nArtificialFreeMarketCommand|Artificial FM Test.\nArtificialPlayerCommand|Artificial Player Commands Test.\nBossDropRateCommand|设置大区Boss掉落倍率\nBotMoveCommand|Bot Movement Commands Test.\nCakeCommand|在当前位置召唤特殊血量的巨型蛋糕BOSS\nDropRateCommand|设置大区掉落倍率\nEnvironmentCommand|Environment Commands.\nExpRateCommand|设置大区经验倍率\nFMBotCommand|FmBot Commands.\nFishDropCommand|钓鱼掉落物品配置\nFishingRateCommand|设置大区钓鱼倍率\nForceVacCommand|强制捡起全图掉落\nHorntailCommand|在当前位置召唤黑龙\nItemVacCommand|捡起全图掉落\nMesoRateCommand|设置大区金币倍率\nOPQCommands|OPQ bot dev commands. Try: !opq help\nPapCommand|在当前位置召唤帕普拉图斯\nPianusCommand|在当前位置召唤皮亚奴斯\nPinkbeanCommand|在当前位置召唤品克缤\nPlayerNpcCommand|在当前位置生成一个玩家NPC\nPlayerNpcRemoveCommand|移除一个玩家NPC\nPmobCommand|在当前位置生成一个永久怪物\nPmobRemoveCommand|移除当前地图的永久怪物\nPnpcCommand|在当前位置生成一个永久NPC\nPnpcRemoveCommand|移除当前地图的永久NPC\nProItemCommand|生成自定义属性的装备\nQuestRateCommand|设置大区任务倍率\nReactorCommands|Reactor inspection and bot-attack test commands.\nServerMessageCommand|设置服务端滚动消息\nSetEqStatCommand|将背包的所有装备设置成指定属性\nTestDevCommand|Template Commands Test.\nTradeBotTestCommand|Trade Bot Commands.\nTravelRateCommand|设置大区传送倍率\nWarpToLifeCommand|传送到生物所在地图，不包含玩家\nZakumCommand|在当前位置召唤扎昆\nDebugCommand|打印debug信息\nIpListCommand|打印所有玩家ip列表\nSetCommand|把传入的参数存到服务端变量中，用于后续测试\nShowMoveLifeCommand|打印生物移动信息开关\nShowPacketsCommand|打印服务端收包开关\nShowSessionsCommand|打印sessions信息\nClearQuestCacheCommand|重载任务\nClearQuestCommand|重载指定任务\nDCAllCommand|下线所有玩家\nDevtestCommand|运行devtest.js脚本，在不重启服务的情况下测试某些内容\nEraseAllPNpcsCommand|移除所有玩家NPC\nGetAccCommand|展示指定玩家的账号名\nMapPlayersCommand|展示当前地图所有玩家\nSaveAllCommand|玩家存档\nServerAddChannelCommand|给指定大区添加频道\nServerAddWorldCommand|添加新大区\nServerRemoveChannelCommand|移除指定大区的最后一个频道\nServerRemoveWorldCommand|移除最后一个大区\nSetGmLevelCommand|给玩家设置GM\nShutdownCommand|关服\nSpawnAllPNpcsCommand|将所有玩家在当前地图生成玩家NPC\nSupplyRateCouponCommand|倍率卡使用开关\nWarpWorldCommand|转区".Split('\n');
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				string[] array3 = array[num7].Trim().Split('|');
				if (array3.Length == 2)
				{
					dictionary[array3[0]] = array3[1];
				}
			}
			{
				foreach (DbRow row in db.Table("command_info").rows)
				{
					if (DbStore.Int(row, "enabled") != 0 && DbStore.Int(row, "level") <= c.gm && dictionary.TryGetValue(DbStore.Str(row, "clazz"), out var value2))
					{
						nativeCommandResult.messages.Add(((c.gm > 0) ? "!" : "@") + DbStore.Str(row, "syntax") + " — " + value2);
					}
				}
				return nativeCommandResult;
			}
		}
		case "TimeCommand":
			nativeCommandResult.messages.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			return nativeCommandResult;
		case "UptimeCommand":
			nativeCommandResult.messages.Add("服务器已运行：" + (DateTime.UtcNow - started).ToString("dd\\.hh\\:mm\\:ss"));
			return nativeCommandResult;
		case "ReadPointsCommand":
		{
			DbRow r = AccountsDao.FindById(db, c.accountid);
			string[] array = new string[5] { "nxCredit", "maplePoint", "nxPrepaid", "rewardpoints", "votepoints" };
			foreach (string text3 in array)
			{
				nativeCommandResult.messages.Add(text3 + "：" + DbStore.Long(r, text3, 0L));
			}
			return nativeCommandResult;
		}
		case "OnlineCommand":
		case "OnlineTwoCommand":
		case "MapPlayersCommand":
		case "InMapCommand":
			nativeCommandResult.messages.Add(c.name + " 等级 " + c.level + " 职业 " + c.job + " 地图 " + c.map);
			return nativeCommandResult;
		case "GetAccCommand":
			Need(a, 1, "!getacc <角色名>");
			Target(c, a[0]);
			nativeCommandResult.messages.Add(DbStore.Str(AccountsDao.FindById(db, c.accountid), "name"));
			return nativeCommandResult;
		case "WhereaMiCommand":
			nativeCommandResult.messages.Add("地图 ID：" + c.map);
			return nativeCommandResult;
		case "ApCommand":
		case "SpCommand":
		case "FameCommand":
		case "HpMpCommand":
		case "MaxHpMpCommand":
		{
			Need(a, 1, "!" + name.Replace("Command", "").ToLowerInvariant() + " [角色名] <数值>");
			CharRecord charRecord = ((a.Length > 1) ? Target(c, a[0]) : c);
			int num2 = Number(a[(a.Length > 1) ? 1u : 0u]);
			switch (name)
			{
			case "ApCommand":
				charRecord.ap = Clamp(num2, 0, MaxAp());
				break;
			case "SpCommand":
				charRecord.SetSp(JobBook.Of(charRecord.job), Clamp(num2, 0, MaxAp()));
				break;
			case "FameCommand":
				charRecord.fame = Clamp(num2, -32768, 32767);
				break;
			case "HpMpCommand":
				charRecord.hp = Clamp(num2, 0, charRecord.maxhp);
				charRecord.mp = Clamp(num2, 0, charRecord.maxmp);
				break;
			default:
				charRecord.maxhp = (charRecord.maxmp = Clamp(num2, 1, 30000));
				charRecord.hp = Math.Min(charRecord.hp, charRecord.maxhp);
				charRecord.mp = Math.Min(charRecord.mp, charRecord.maxmp);
				break;
			}
			Save(charRecord, nativeCommandResult);
			break;
		}
		case "SetStatCommand":
		case "MaxStatCommand":
		{
			int num2;
			if (name == "MaxStatCommand")
			{
				c.level = 255;
				c.exp = 0L;
				c.fame = 13337;
				c.maxhp = (c.maxmp = 30000);
				num2 = 32767;
			}
			else
			{
				Need(a, 1, "!setstat <数值>");
				num2 = Clamp(Number(a[0]), 4, 32767);
			}
			c.str = (c.dex = (c.int_ = (c.luk = num2)));
			Save(c, nativeCommandResult);
			break;
		}
		case "StatStrCommand":
		case "StatDexCommand":
		case "StatIntCommand":
		case "StatLukCommand":
		{
			int num13 = name switch
			{
				"StatIntCommand" => c.int_, 
				"StatDexCommand" => c.dex, 
				"StatStrCommand" => c.str, 
				_ => c.luk, 
			};
			int num2 = ((a.Length != 0) ? Math.Max(0, Math.Min(c.ap, Number(a[0]))) : Math.Max(0, Math.Min(c.ap, MaxAp() - num13)));
			if ((long)num13 + (long)num2 > MaxAp())
			{
				return NativeCommandResult.Fail("属性不能超过 " + MaxAp());
			}
			switch (name)
			{
			case "StatStrCommand":
				c.str += num2;
				break;
			case "StatDexCommand":
				c.dex += num2;
				break;
			case "StatIntCommand":
				c.int_ += num2;
				break;
			default:
				c.luk += num2;
				break;
			}
			c.ap -= num2;
			Save(c, nativeCommandResult);
			break;
		}
		case "GiveMesosCommand":
		{
			Need(a, 1, "!givems [角色名] <数量|max|min>");
			CharRecord charRecord = ((a.Length > 1) ? Target(c, a[0]) : c);
			string text = a[(a.Length > 1) ? 1u : 0u];
			long num;
			if (!(text == "max"))
			{
				if (!(text == "min"))
				{
					if (!long.TryParse(text, out var result))
					{
						throw new ArgumentException("无效金币数量");
					}
					num = result;
				}
				else
				{
					num = -2147483648L;
				}
			}
			else
			{
				num = 2147483647L;
			}
			long val = num;
			charRecord.meso = (int)Math.Max(0L, Math.Min(2147483647L, charRecord.meso + Math.Max(-2147483648L, Math.Min(2147483647L, val))));
			Save(charRecord, nativeCommandResult);
			break;
		}
		case "GiveNxCommand":
		case "GiveVpCommand":
		case "GiveRpCommand":
		{
			Need(a, 1, "!givenx [nx|mp|np] [角色名] <数量> / !givevp 或 !giverp [角色名] <数量>");
			string text4;
			if (name == "GiveVpCommand")
			{
				text4 = "votepoints";
			}
			else
			{
				text4 = ((name == "GiveRpCommand") ? "rewardpoints" : "nxCredit");
			}
			int num9 = 0;
			if (name == "GiveNxCommand" && a.Length > 1)
			{
				if (a[0] == "nx" || a[0] == "点券")
				{
					num9 = 1;
				}
				else if (a[0] == "mp" || a[0] == "抵用券")
				{
					text4 = "maplePoint";
					num9 = 1;
				}
				else if (a[0] == "np" || a[0] == "信用点")
				{
					text4 = "nxPrepaid";
					num9 = 1;
				}
			}
			CharRecord charRecord = ((a.Length - num9 > 1) ? Target(c, a[num9]) : c);
			int num2 = Number(a[a.Length - 1]);
			DbRow r = AccountsDao.FindById(db, charRecord.accountid);
			long num10 = DbStore.Long(r, text4, 0L) + num2;
			if (num10 < 0 || num10 > int.MaxValue)
			{
				return NativeCommandResult.Fail("点数余额超出范围");
			}
			r[text4] = num10;
			nativeCommandResult.changed = true;
			break;
		}
		case "HealCommand":
		case "HealMapCommand":
		case "HealPersonCommand":
		case "HurtCommand":
		case "KillCommand":
		case "KillMapCommand":
		{
			CharRecord charRecord = c;
			if (name == "HealPersonCommand" || name == "KillCommand")
			{
				Need(a, 1, "!healperson / !kill <角色名>");
				charRecord = Target(c, a[0]);
			}
			switch (name)
			{
			case "HurtCommand":
				if (a.Length != 0)
				{
					charRecord = Target(c, a[0]);
				}
				charRecord.hp = 1;
				break;
			case "KillCommand":
			case "KillMapCommand":
				charRecord.hp = 0;
				break;
			default:
				charRecord.hp = charRecord.maxhp;
				charRecord.mp = charRecord.maxmp;
				break;
			}
			Save(charRecord, nativeCommandResult);
			break;
		}
		case "SetGmLevelCommand":
		{
			Need(a, 2, "!setgmlevel <角色名> <0~6>");
			CharRecord charRecord = Target(c, a[0]);
			int num2 = Number(a[1]);
			if (num2 < 0 || num2 > 6)
			{
				throw new ArgumentException("GM 等级范围为0~6");
			}
			charRecord.gm = num2;
			Save(charRecord, nativeCommandResult);
			break;
		}
		case "ClearSavedLocationsCommand":
			db.Table("savedlocations").Delete((DbRow x) => DbStore.Int(x, "characterid") == c.id);
			nativeCommandResult.changed = true;
			break;
		case "SetSlotCommand":
		{
			Need(a, 1, "!setslot <24~96>");
			int num2 = Clamp(Number(a[0]) / 4 * 4, 4, 96);
			c.equipslots = (c.useslots = (c.setupslots = (c.etcslots = num2)));
			Save(c, nativeCommandResult);
			break;
		}
		case "ClearSlotCommand":
		{
			Need(a, 1, "!clearslot <equip|use|setup|etc|cash|all>");
			string[] array2 = new string[6] { "", "equip", "use", "setup", "etc", "cash" };
			int num8 = Array.IndexOf(new string[6] { "全部", "装备", "消耗", "设置", "其他", "现金" }, a[0]);
			if (num8 >= 0)
			{
				a[0] = ((num8 == 0) ? "all" : array2[num8]);
			}
			int type = Array.IndexOf(array2, a[0]);
			if (type < 1 && a[0] != "all")
			{
				throw new ArgumentException("背包栏名称无效");
			}
			HashSet<long> removed = new HashSet<long>();
			db.Table("inventoryitems").Delete((DbRow x) =>
			{
				int num17;
				if (DbStore.Int(x, "characterid") == c.id && InventoryDao.IsBagPosition(DbStore.Int(x, "position")))
				{
					if (!(a[0] == "all"))
					{
						num17 = ((DbStore.Int(x, "inventorytype") == type) ? 1 : 0);
						if (num17 == 0)
						{
							goto IL_0081;
						}
					}
					else
					{
						num17 = 1;
					}
					removed.Add(DbStore.Long(x, "inventoryitemid", 0L));
				}
				else
				{
					num17 = 0;
				}
				goto IL_0081;
				IL_0081:
				return (byte)num17 != 0;
			});
			db.Table("inventoryequipment").Delete((DbRow x) => removed.Contains(DbStore.Long(x, "inventoryitemid", 0L)));
			nativeCommandResult.changed = true;
			break;
		}
		case "LootCommand":
		{
			foreach (InventoryEntry item2 in CharService.Load(db, (int)c.id).inventory)
			{
				nativeCommandResult.messages.Add(item2.position + ": " + item2.itemId + " × " + item2.quantity);
			}
			return nativeCommandResult;
		}
		case "SaveAllCommand":
			db.Save();
			nativeCommandResult.messages.Add("角色已存档");
			return nativeCommandResult;
		case "RechargeCommand":
		{
			BotActor actor = new BotActor
			{
				id = (int)c.id,
				job = c.job,
				character = CharService.Load(db, (int)c.id)
			};
			TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
			foreach (DbRow item3 in InventoryDao.ItemsByCharacter(db, (int)c.id))
			{
				if (DbStore.Int(item3, "inventorytype") == 2 && DbStore.Int(item3, "position") > 0)
				{
					item3["quantity"] = (long)tradeItemRules.SlotMax(DbStore.Int(item3, "itemid"), actor);
				}
			}
			nativeCommandResult.changed = true;
			break;
		}
		case "SetEqStatCommand":
		{
			Need(a, 1, "!seteqstat <属性> [速度跳跃]");
			int num2 = Clamp(Number(a[0]), 0, 32767);
			int jump = ((a.Length > 1) ? Clamp(Number(a[1]), 0, 32767) : 0);
			foreach (DbRow item4 in InventoryDao.ItemsByCharacter(db, (int)c.id))
			{
				if (DbStore.Int(item4, "inventorytype") == 1 && InventoryDao.IsBagPosition(DbStore.Int(item4, "position")))
				{
					SetEquipment(item4, num2, jump);
				}
			}
			nativeCommandResult.changed = true;
			break;
		}
		case "ProItemCommand":
		{
			Need(a, 2, "!proitem <装备ID> <属性> [速度跳跃]");
			int num12 = Number(a[0]);
			int num2 = Clamp(Number(a[1]), 0, 32767);
			int jump = ((a.Length > 2) ? Clamp(Number(a[2]), 0, 32767) : 0);
			if (num12 / 1000000 != 1 || BotItemCatalog.Shared.Info(num12) == null)
			{
				throw new ArgumentException("装备不存在：" + num12);
			}
			InventoryEntry inventoryEntry2 = BotItemCatalog.Shared.CleanEquip(num12);
			inventoryEntry2.owner = c.name;
			inventoryEntry2.flag |= 8;
			inventoryEntry2.str = (inventoryEntry2.dex = (inventoryEntry2.int_ = (inventoryEntry2.luk = (inventoryEntry2.hp = (inventoryEntry2.mp = (inventoryEntry2.watk = (inventoryEntry2.matk = (inventoryEntry2.wdef = (inventoryEntry2.mdef = (inventoryEntry2.acc = (inventoryEntry2.avoid = num2)))))))))));
			inventoryEntry2.speed = (inventoryEntry2.jump = jump);
			Grant(c, inventoryEntry2, 1);
			nativeCommandResult.changed = true;
			break;
		}
		case "RatesCommand":
		case "ShowRatesCommand":
		{
			CommunityResponse communityResponse = new NativeCommunityService(db).Rates((int)c.id);
			nativeCommandResult.messages.Add("经验倍率：" + communityResponse.expRate + "，金币倍率：" + communityResponse.mesoRate + "，掉落倍率：" + communityResponse.dropRate);
			nativeCommandResult.messages.Add("Boss掉落倍率：" + GameConfigDao.GetFloat(db, "boss_drop_rate", 1.0) + "，任务倍率：" + GameConfigDao.GetFloat(db, "quest_rate", 1.0) + "，传送倍率：" + GameConfigDao.GetFloat(db, "travel_rate", 1.0));
			return nativeCommandResult;
		}
		case "ExpRateCommand":
		case "MesoRateCommand":
		case "DropRateCommand":
		case "BossDropRateCommand":
		case "QuestRateCommand":
		case "TravelRateCommand":
		case "FishingRateCommand":
		{
			Need(a, 1, "!exprate / !mesorate / !droprate / !bossdroprate / !questrate / !travelrate / !fishrate <倍率>");
			if (!double.TryParse(a[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) || double.IsNaN(result2) || double.IsInfinity(result2))
			{
				throw new ArgumentException("无效倍率");
			}
			string code = name switch
			{
				"TravelRateCommand" => "travel_rate", 
				"QuestRateCommand" => "quest_rate", 
				"BossDropRateCommand" => "boss_drop_rate", 
				"DropRateCommand" => "drop_rate", 
				"MesoRateCommand" => "meso_rate", 
				"ExpRateCommand" => "exp_rate", 
				_ => "fishing_rate", 
			};
			DbRow dbRow6 = db.Table("game_config").Find((DbRow x) => DbStore.Str(x, "config_code") == code);
			string value = Math.Max(1.0, result2).ToString(CultureInfo.InvariantCulture);
			if (dbRow6 == null)
			{
				db.Table("game_config").Insert(new DbRow
				{
					["config_code"] = code,
					["config_type"] = "world",
					["config_value"] = value
				});
			}
			else
			{
				dbRow6["config_value"] = value;
			}
			nativeCommandResult.changed = true;
			break;
		}
		case "ItemCommand":
		{
			Need(a, 1, "!item <物品ID> [数量]");
			int num6 = Number(a[0]);
			int num2 = ((a.Length <= 1) ? 1 : Number(a[1]));
			if (BotItemCatalog.Shared.Info(num6) == null)
			{
				throw new ArgumentException("物品不存在：" + num6);
			}
			if (GameConfigDao.GetBool(db, "block_generate_cash_item") && BotItemCatalog.Int(BotItemCatalog.Shared.Info(num6), "cash") != 0)
			{
				throw new ArgumentException("禁止生成现金物品");
			}
			bool flag = num6 / 1000 == 5000;
			if (flag && a.Length < 2)
			{
				throw new ArgumentException("用法：!item <宠物ID> <天数>");
			}
			if (num2 < 1 || num2 > 32767)
			{
				throw new ArgumentException("数量或天数范围为1~32767");
			}
			InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanItem(num6, flag ? 1 : num2);
			inventoryEntry.owner = c.name;
			if (c.gm < 3)
			{
				inventoryEntry.flag |= 264;
			}
			if (flag)
			{
				inventoryEntry.petId = 0;
				inventoryEntry.expiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)num2 * 86400000L;
			}
			Grant(c, inventoryEntry, flag ? 1 : num2);
			nativeCommandResult.changed = true;
			break;
		}
		case "StaffCommand":
			nativeCommandResult.messages.Add(GameConfigDao.Get(db, "staff_list", "北斗冒险岛服务端 / MapleDash 安卓移植（逆向依据见 MapleDash/docs 与 beidou-client-reverse）"));
			return nativeCommandResult;
		case "ChangeLanguageCommand":
			nativeCommandResult.messages.Add("客户端只提供简体中文（源端 zh-CN 资源与脚本），没有其它语言包");
			return nativeCommandResult;
		case "EnableAuthCommand":
			nativeCommandResult.messages.Add("本服未启用 PIC/PIN 二次验证，无需重置冷却");
			return nativeCommandResult;
		case "ExpedsCommand":
			nativeCommandResult.messages.Add("当前没有进行中的远征");
			return nativeCommandResult;
		case "IpListCommand":
			nativeCommandResult.messages.Add("127.0.0.1（本机单进程服务端，没有外部连接）");
			return nativeCommandResult;
		case "GmCommand":
			Need(a, 1, "!gm <内容>");
			SetConfig("gm_message_log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + c.name + "：" + raw);
			nativeCommandResult.messages.Add("本机只有你一个 GM，消息已记录");
			return nativeCommandResult;
		case "ReportBugCommand":
			Need(a, 1, "!reportbug <内容>");
			SetConfig("bug_report_log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + c.name + "：" + raw);
			nativeCommandResult.messages.Add("问题已记录，可在后台 game_config/bug_report_log 查看");
			return nativeCommandResult;
		case "EquipLvCommand":
		{
			int num15 = 0;
			foreach (InventoryEntry item5 in CharService.Load(db, (int)c.id).inventory)
			{
				if (item5.inventoryType == 1 && item5.position < 0)
				{
					Dictionary<string, object> dictionary2 = BotItemCatalog.Shared.Info(item5.itemId);
					int num16 = ((dictionary2 != null) ? BotItemCatalog.Int(dictionary2, "reqLevel") : 0);
					nativeCommandResult.messages.Add(((item5.position < 0) ? "[已装备] " : "") + BotItemCatalog.Shared.Name(item5.itemId) + " 需求等级 " + num16 + ((c.level < num16) ? "（当前等级不足）" : ""));
					num15++;
				}
			}
			if (num15 == 0)
			{
				nativeCommandResult.messages.Add("当前没有已装备的物品");
			}
			return nativeCommandResult;
		}
		case "RanksCommand":
		{
			List<DbRow> list2 = new List<DbRow>();
			foreach (DbRow row2 in db.Table("characters").rows)
			{
				list2.Add(row2);
			}
			list2.Sort((DbRow x, DbRow y) => DbStore.Int(y, "level").CompareTo(DbStore.Int(x, "level")));
			int num14 = 0;
			foreach (DbRow item6 in list2)
			{
				if (num14 >= 10)
				{
					break;
				}
				num14++;
				nativeCommandResult.messages.Add(num14 + ". " + DbStore.Str(item6, "name") + " Lv." + DbStore.Int(item6, "level") + " 职业 " + DbStore.Int(item6, "job"));
			}
			if (num14 == 0)
			{
				nativeCommandResult.messages.Add("没有角色记录");
			}
			return nativeCommandResult;
		}
		case "ToggleExpCommand":
		{
			bool flag2 = !GameConfigDao.GetBool(db, "exp_enabled", fallback: true);
			SetConfig("exp_enabled", flag2 ? "1" : "0");
			nativeCommandResult.messages.Add("经验获取已" + (flag2 ? "开启" : "关闭"));
			return nativeCommandResult;
		}
		case "IgnoreCommand":
		{
			Need(a, 1, "!ignore <角色名>");
			List<string> list = new List<string>(GameConfigDao.Get(db, "gm_ignore_list").Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			if (list.RemoveAll((string x) => string.Equals(x, a[0], StringComparison.OrdinalIgnoreCase)) > 0)
			{
				nativeCommandResult.messages.Add("已移出自动封禁白名单：" + a[0]);
			}
			else
			{
				list.Add(a[0]);
				nativeCommandResult.messages.Add("已加入自动封禁白名单：" + a[0]);
			}
			SetConfig("gm_ignore_list", string.Join(",", list.ToArray()));
			return nativeCommandResult;
		}
		case "IgnoredCommand":
		{
			string text2 = GameConfigDao.Get(db, "gm_ignore_list");
			if (string.IsNullOrWhiteSpace(text2))
			{
				nativeCommandResult.messages.Add("自动封禁白名单为空");
			}
			else
			{
				string[] array = text2.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string item in array)
				{
					nativeCommandResult.messages.Add(item);
				}
			}
			return nativeCommandResult;
		}
		case "BanCommand":
		{
			Need(a, 1, "!ban <角色名> [原因]");
			DbRow dbRow3 = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), a[0], StringComparison.OrdinalIgnoreCase));
			if (dbRow3 == null)
			{
				throw new ArgumentException("角色不存在：" + a[0]);
			}
			DbRow dbRow4 = AccountsDao.FindById(db, (int)DbStore.Long(dbRow3, "accountid", 0L));
			if (dbRow4 == null)
			{
				throw new ArgumentException("账号不存在");
			}
			dbRow4["banned"] = 1L;
			dbRow4["banreason"] = ((a.Length > 1) ? raw.Substring(raw.IndexOf(' ') + 1) : "GM封禁");
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("已封禁 " + DbStore.Str(dbRow3, "name") + "（账号 " + DbStore.Str(dbRow4, "name") + "）");
			return nativeCommandResult;
		}
		case "UnBanCommand":
		{
			Need(a, 1, "!unban <账号名|角色名>");
			DbRow dbRow = db.Table("accounts").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), a[0], StringComparison.OrdinalIgnoreCase));
			if (dbRow == null)
			{
				DbRow dbRow2 = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), a[0], StringComparison.OrdinalIgnoreCase));
				if (dbRow2 != null)
				{
					dbRow = AccountsDao.FindById(db, (int)DbStore.Long(dbRow2, "accountid", 0L));
				}
			}
			if (dbRow == null)
			{
				throw new ArgumentException("账号或角色不存在：" + a[0]);
			}
			dbRow["banned"] = 0L;
			dbRow["banreason"] = "";
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("已解封 " + DbStore.Str(dbRow, "name"));
			return nativeCommandResult;
		}
		case "SetCommand":
			Need(a, 2, "!set <变量名> <值>");
			SetConfig("gm_var_" + a[0], raw.Substring(raw.IndexOf(' ') + 1));
			nativeCommandResult.messages.Add("已写入服务端变量 " + a[0]);
			return nativeCommandResult;
		case "ServerMessageCommand":
		{
			Need(a, 1, "!servermessage <内容>");
			DbRow dbRow8 = db.Table("world_prop").Find(DbStore.Eq("id", c.world));
			if (dbRow8 == null)
			{
				SetConfig("server_message", raw);
			}
			else
			{
				dbRow8["server_message"] = raw;
				nativeCommandResult.changed = true;
			}
			nativeCommandResult.messages.Add("滚动公告已更新（重新进入选区可见）");
			return nativeCommandResult;
		}
		case "SupplyRateCouponCommand":
		{
			bool flag3 = !GameConfigDao.GetBool(db, "rate_coupon_enabled", fallback: true);
			SetConfig("rate_coupon_enabled", flag3 ? "1" : "0");
			nativeCommandResult.messages.Add("倍率卡使用已" + (flag3 ? "开启" : "关闭"));
			return nativeCommandResult;
		}
		case "ServerAddChannelCommand":
		{
			int num11 = ((a.Length != 0) ? Number(a[0]) : c.world);
			DbRow dbRow7 = db.Table("world_prop").Find(DbStore.Eq("id", (long)num11));
			if (dbRow7 == null)
			{
				throw new ArgumentException("大区不存在：" + num11);
			}
			dbRow7["channel_size"] = DbStore.Long(dbRow7, "channel_size", 3L) + 1;
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("大区 " + num11 + " 频道数 " + dbRow7["channel_size"]?.ToString() + "（重新进入选区可见）");
			return nativeCommandResult;
		}
		case "ServerRemoveChannelCommand":
		{
			int num4 = ((a.Length != 0) ? Number(a[0]) : c.world);
			DbRow dbRow5 = db.Table("world_prop").Find(DbStore.Eq("id", (long)num4));
			if (dbRow5 == null)
			{
				throw new ArgumentException("大区不存在：" + num4);
			}
			long num5 = DbStore.Long(dbRow5, "channel_size", 3L);
			if (num5 <= 1)
			{
				throw new ArgumentException("至少要保留一个频道");
			}
			dbRow5["channel_size"] = num5 - 1;
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("大区 " + num4 + " 频道数 " + dbRow5["channel_size"]?.ToString() + "（重新进入选区可见）");
			return nativeCommandResult;
		}
		case "ServerAddWorldCommand":
		{
			int num3 = 0;
			foreach (DbRow row3 in db.Table("world_prop").rows)
			{
				num3 = Math.Max(num3, (int)DbStore.Long(row3, "id", 0L) + 1);
			}
			db.Table("world_prop").Insert(new DbRow
			{
				["id"] = (long)num3,
				["flag"] = 0L,
				["server_message"] = "Welcome to MapleDash!",
				["event_message"] = "MapleDash",
				["recommend_message"] = "Welcome to MapleDash!",
				["channel_size"] = 3L,
				["exp_rate"] = 1.0,
				["meso_rate"] = 1.0,
				["drop_rate"] = 1.0,
				["boss_drop_rate"] = 1.0,
				["quest_rate"] = 1.0,
				["fishing_rate"] = 1.0,
				["travel_rate"] = 1.0,
				["enabled"] = 1L
			});
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("已添加大区 " + num3 + "（重新进入选区可见）");
			return nativeCommandResult;
		}
		case "ServerRemoveWorldCommand":
		{
			int last = -1;
			foreach (DbRow row4 in db.Table("world_prop").rows)
			{
				last = Math.Max(last, (int)DbStore.Long(row4, "id", 0L));
			}
			if (last <= 0)
			{
				throw new ArgumentException("至少保留一个大区");
			}
			db.Table("world_prop").Delete((DbRow x) => DbStore.Long(x, "id", 0L) == last);
			nativeCommandResult.changed = true;
			nativeCommandResult.messages.Add("已移除大区 " + last + "（重新进入选区可见）");
			return nativeCommandResult;
		}
		case "WarpWorldCommand":
		{
			Need(a, 1, "!warpworld <大区ID>");
			int num2 = Number(a[0]);
			if (db.Table("world_prop").Find(DbStore.Eq("id", (long)num2)) == null && num2 != c.world)
			{
				throw new ArgumentException("大区不存在：" + num2);
			}
			c.world = num2;
			Save(c, nativeCommandResult);
			nativeCommandResult.messages.Add("已切到大区 " + num2 + "（重新登录生效）");
			return nativeCommandResult;
		}
		case "SummonCommand":
		case "ReachCommand":
			Need(a, 1, "!" + name.Replace("Command", "").ToLowerInvariant() + " <角色名>");
			throw new ArgumentException("本服只有你一个在线真人，没有可传送的对象：" + a[0]);
		default:
			return Host?.Invoke(c, name, a, raw) ?? NativeCommandResult.Fail("此命令所需运行模块尚未接入：" + name);
		}
		nativeCommandResult.messages.Add("命令执行完成");
		return nativeCommandResult;
	}

	private void Grant(CharRecord c, InventoryEntry offered, int quantity)
	{
		BotItemCatalog shared = BotItemCatalog.Shared;
		FullCharacter fullCharacter = CharService.Load(db, (int)c.id);
		BotActor actor = new BotActor
		{
			id = (int)c.id,
			job = c.job,
			character = fullCharacter
		};
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in fullCharacter.inventory)
		{
			list.Add(item.Copy());
		}
		BotTradeService botTradeService = new BotTradeService(db, shared);
		int num = ((!offered.isEquip) ? 1 : quantity);
		for (int i = 0; i < num; i++)
		{
			if (!botTradeService.Rules.Receive(actor, list, offered))
			{
				throw new ArgumentException("背包空间不足");
			}
		}
		if (offered.itemId / 1000 == 5000)
		{
			int petId = (int)db.Table("pets").Insert(new DbRow
			{
				["name"] = shared.Name(offered.itemId),
				["level"] = 1L,
				["closeness"] = 0L,
				["fullness"] = 100L,
				["summoned"] = 0L,
				["flag"] = 0L
			});
			foreach (InventoryEntry item2 in list)
			{
				if (item2.itemRowId == 0L && item2.itemId == offered.itemId)
				{
					item2.petId = petId;
				}
			}
		}
		fullCharacter.inventory = list;
		botTradeService.Persist(actor);
	}

	private void SetEquipment(DbRow held, int stat, int speed)
	{
		DbRow dbRow = db.Table("inventoryequipment").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == DbStore.Long(held, "inventoryitemid", 0L));
		if (dbRow == null)
		{
			throw new ArgumentException("缺少装备属性记录");
		}
		string[] array = new string[12]
		{
			"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
			"acc", "avoid"
		};
		foreach (string key in array)
		{
			dbRow[key] = (long)stat;
		}
		dbRow["speed"] = (long)speed;
		dbRow["jump"] = (long)speed;
		held["flag"] = (long)(DbStore.Int(held, "flag") | 8);
	}

	private int MaxAp()
	{
		int num = GameConfigDao.GetInt(db, "max_ap");
		if (num <= 0)
		{
			return 32767;
		}
		return num;
	}

	private void SetConfig(string code, string value, string type = "world")
	{
		DbRow dbRow = db.Table("game_config").Find((DbRow x) => DbStore.Str(x, "config_code") == code);
		if (dbRow == null)
		{
			db.Table("game_config").Insert(new DbRow
			{
				["config_code"] = code,
				["config_type"] = type,
				["config_value"] = value
			});
		}
		else
		{
			dbRow["config_value"] = value;
		}
		db.Save();
	}

	private static int Clamp(int n, int min, int max)
	{
		return Math.Max(min, Math.Min(max, n));
	}

	private void Save(CharRecord c, NativeCommandResult r)
	{
		CharactersDao.Save(db, c);
		r.changed = true;
	}
}
