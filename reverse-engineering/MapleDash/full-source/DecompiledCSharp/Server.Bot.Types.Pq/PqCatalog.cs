using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Pq;

public static class PqCatalog
{
	public const int PassCoupon = 4001007;

	public const int PassTicket = 4001008;

	public const int LudiPass = 4001022;

	public const int LudiKey = 4001023;

	public const int EllinStone = 4001163;

	public const int PirateLow = 4001120;

	public const int PirateMid = 4001121;

	public const int PirateHigh = 4001122;

	private static readonly Dictionary<string, PqDef> all;

	private static readonly int[] Stage1Answers;

	private static readonly int[][] Stage2Combos;

	private static readonly int[][] Stage3Combos;

	private static readonly int[][] Stage4Combos;

	public static IEnumerable<PqDef> All => all.Values;

	public static int Stage1Answer(int question)
	{
		if (question < 1 || question > Stage1Answers.Length)
		{
			return 0;
		}
		return Stage1Answers[question - 1];
	}

	public static int[] Combo(string property, int index)
	{
		int[][] array = property switch
		{
			"stg4Property" => Stage4Combos, 
			"stg3Property" => Stage3Combos, 
			"stg2Property" => Stage2Combos, 
			_ => null, 
		};
		if (array == null || index < 0 || index >= array.Length)
		{
			return null;
		}
		return array[index];
	}

	public static PqDef Get(string key)
	{
		if (key == null || !all.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public static PqDef ForMap(int mapId)
	{
		foreach (PqDef value in all.Values)
		{
			if (value.recruitMap == mapId)
			{
				return value;
			}
		}
		return null;
	}

	public static PqDef ByStageMap(int mapId)
	{
		foreach (PqDef value in all.Values)
		{
			foreach (PqStage stage in value.stages)
			{
				if (stage.mapId == mapId)
				{
					return value;
				}
			}
		}
		return null;
	}

	static PqCatalog()
	{
		all = new Dictionary<string, PqDef>();
		Stage1Answers = new int[7] { 10, 35, 20, 25, 25, 30, 8 };
		Stage2Combos = new int[4][]
		{
			new int[4] { 0, 1, 1, 1 },
			new int[4] { 1, 0, 1, 1 },
			new int[4] { 1, 1, 0, 1 },
			new int[4] { 1, 1, 1, 0 }
		};
		Stage3Combos = new int[10][]
		{
			new int[5] { 0, 0, 1, 1, 1 },
			new int[5] { 0, 1, 0, 1, 1 },
			new int[5] { 0, 1, 1, 0, 1 },
			new int[5] { 0, 1, 1, 1, 0 },
			new int[5] { 1, 0, 0, 1, 1 },
			new int[5] { 1, 0, 1, 0, 1 },
			new int[5] { 1, 0, 1, 1, 0 },
			new int[5] { 1, 1, 0, 0, 1 },
			new int[5] { 1, 1, 0, 1, 0 },
			new int[5] { 1, 1, 1, 0, 0 }
		};
		Stage4Combos = new int[20][]
		{
			new int[6] { 0, 0, 0, 1, 1, 1 },
			new int[6] { 0, 0, 1, 0, 1, 1 },
			new int[6] { 0, 0, 1, 1, 0, 1 },
			new int[6] { 0, 0, 1, 1, 1, 0 },
			new int[6] { 0, 1, 0, 0, 1, 1 },
			new int[6] { 0, 1, 0, 1, 0, 1 },
			new int[6] { 0, 1, 0, 1, 1, 0 },
			new int[6] { 0, 1, 1, 0, 0, 1 },
			new int[6] { 0, 1, 1, 0, 1, 0 },
			new int[6] { 0, 1, 1, 1, 0, 0 },
			new int[6] { 1, 0, 0, 0, 1, 1 },
			new int[6] { 1, 0, 0, 1, 0, 1 },
			new int[6] { 1, 0, 0, 1, 1, 0 },
			new int[6] { 1, 0, 1, 0, 0, 1 },
			new int[6] { 1, 0, 1, 0, 1, 0 },
			new int[6] { 1, 0, 1, 1, 0, 0 },
			new int[6] { 1, 1, 0, 0, 0, 1 },
			new int[6] { 1, 1, 0, 0, 1, 0 },
			new int[6] { 1, 1, 0, 1, 0, 0 },
			new int[6] { 1, 1, 1, 0, 0, 0 }
		};
		Add(Kerning());
		Add(Ludi());
		Add(Ellin());
		Add(Pirate());
	}

	private static void Add(PqDef def)
	{
		if (def != null)
		{
			all[def.key] = def;
		}
	}

	private static PqStep Clear(string say)
	{
		return new PqStep
		{
			action = PqAction.ClearMobs,
			say = say
		};
	}

	private static PqStep Stand(string say, params PqRect[] rects)
	{
		return new PqStep
		{
			action = PqAction.Stand,
			stands = new List<PqRect>(rects),
			need = Math.Min(3, rects.Length),
			say = say
		};
	}

	private static PqStep Give(int item, int count, string say)
	{
		return new PqStep
		{
			action = PqAction.GrantItem,
			itemId = item,
			itemCount = count,
			say = say
		};
	}

	private static int[] Ids(params int[] ids)
	{
		return ids ?? Array.Empty<int>();
	}

	private static PqStep Talk(int npc, string say, bool openOnly = false, string await = null, string value = null)
	{
		return new PqStep
		{
			action = PqAction.Talk,
			npc = npc,
			openOnly = openOnly,
			say = say,
			awaitProperty = await,
			awaitValue = value,
			timeoutMs = 60000L
		};
	}

	private static PqStep Collect(int item, int need, int[] mobs, int[] reactors, int[] maps, string say, long timeoutMs = 240000L)
	{
		return new PqStep
		{
			action = PqAction.BotCollect,
			itemId = item,
			need = need,
			ids = (mobs ?? Array.Empty<int>()),
			reactors = (reactors ?? Array.Empty<int>()),
			maps = (maps ?? Array.Empty<int>()),
			say = say,
			timeoutMs = timeoutMs
		};
	}

	private static PqStep HandOver(int item, int count, string say)
	{
		return new PqStep
		{
			action = PqAction.HandOver,
			itemId = item,
			itemCount = count,
			say = say
		};
	}

	private static string Cleared(int stageNo)
	{
		return stageNo + "stageclear";
	}

	private static PqDef Kerning()
	{
		PqDef pqDef = new PqDef();
		pqDef.key = "KPQ";
		pqDef.name = "废弃都市";
		pqDef.eventName = "KerningPQ";
		pqDef.recruitMap = 103000000;
		pqDef.entryMap = 103000800;
		pqDef.exitMap = 103000890;
		pqDef.recruitNpc = 9020000;
		pqDef.minLevel = 21;
		pqDef.keywords = new string[10] { "求组", "缺一", "缺二", "缺三", "q1", "q2", "组队", "废都", "下水道", "通行证" };
		PqDef pqDef2 = pqDef;
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000800,
			stageNo = 1,
			npc = 9020001,
			title = "第一阶段：收集通行证",
			steps = 
			{
				new PqStep
				{
					action = PqAction.BotAsk,
					npc = 9020001,
					timeoutMs = 20000L,
					say = "我先去 Cloto 那里领题"
				},
				new PqStep
				{
					action = PqAction.BotCollect,
					ids = new int[1] { 9300001 },
					timeoutMs = 300000L,
					say = "券我来捡，题目要几张就收几张"
				},
				new PqStep
				{
					action = PqAction.BotExchange,
					npc = 9020001,
					timeoutMs = 90000L,
					say = "券齐了，回 Cloto 换通行证"
				},
				new PqStep
				{
					action = PqAction.BotWait,
					npc = 9020001,
					say = "我在 Cloto 旁边等队长来收"
				},
				new PqStep
				{
					action = PqAction.HandOver,
					itemId = 4001008,
					say = "通行证交给队长"
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000801,
			stageNo = 2,
			title = "第二阶段：挂绳子",
			steps = 
			{
				new PqStep
				{
					action = PqAction.Talk,
					npc = 9020001,
					say = "队长去听第二阶段的规则"
				},
				new PqStep
				{
					action = PqAction.StageCheck,
					npc = 9020001,
					comboProperty = "stg2Property",
					say = "我按绳子组合挂上去",
					stands = 
					{
						new PqRect(-755, -132, 4, 218),
						new PqRect(-721, -340, 4, 166),
						new PqRect(-586, -326, 4, 150),
						new PqRect(-483, -181, 4, 222)
					}
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000802,
			stageNo = 3,
			title = "第三阶段：站平台",
			steps = 
			{
				new PqStep
				{
					action = PqAction.Talk,
					npc = 9020001,
					say = "队长去听第三阶段的规则"
				},
				new PqStep
				{
					action = PqAction.StageCheck,
					npc = 9020001,
					comboProperty = "stg3Property",
					say = "平台我站中间那一格",
					stands = 
					{
						new PqRect(608, -180, 140, 50),
						new PqRect(791, -117, 140, 45),
						new PqRect(958, -180, 140, 50),
						new PqRect(876, -238, 140, 45),
						new PqRect(702, -238, 140, 45)
					}
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000803,
			stageNo = 4,
			title = "第四阶段：踩木桶",
			steps = 
			{
				new PqStep
				{
					action = PqAction.Talk,
					npc = 9020001,
					say = "队长去听第四阶段的规则"
				},
				new PqStep
				{
					action = PqAction.StageCheck,
					npc = 9020001,
					comboProperty = "stg4Property",
					say = "我踩木桶",
					stands = 
					{
						new PqRect(910, -236, 35, 5),
						new PqRect(877, -184, 35, 5),
						new PqRect(946, -184, 35, 5),
						new PqRect(845, -132, 35, 5),
						new PqRect(910, -132, 35, 5),
						new PqRect(981, -132, 35, 5)
					}
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000804,
			stageNo = 5,
			npc = 9020001,
			title = "最后阶段：BOSS",
			steps = 
			{
				new PqStep
				{
					action = PqAction.KillBoss,
					say = "BOSS 我来拉，你们清小怪"
				},
				new PqStep
				{
					action = PqAction.BotCollect,
					ids = new int[3] { 9300000, 9300002, 9300003 },
					itemId = 4001008,
					need = 10,
					timeoutMs = 240000L,
					say = "通行证我来捡，凑 10 张"
				},
				new PqStep
				{
					action = PqAction.HandOver,
					itemId = 4001008,
					itemCount = 10,
					say = "10 张都交给队长"
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 103000805,
			stageNo = 6,
			title = "奖励阶段",
			steps = 
			{
				new PqStep
				{
					action = PqAction.Talk,
					npc = 9020002,
					say = "领完奖就回城"
				}
			}
		});
		return pqDef2;
	}

	private static PqDef Ludi()
	{
		PqDef pqDef = new PqDef();
		pqDef.key = "LPQ";
		pqDef.name = "玩具塔101";
		pqDef.eventName = "LudiPQ";
		pqDef.recruitMap = 221024500;
		pqDef.entryMap = 922010100;
		pqDef.exitMap = 922010000;
		pqDef.recruitNpc = 2040034;
		pqDef.minLevel = 35;
		pqDef.keywords = new string[10] { "求组", "缺一", "缺二", "缺三", "q1", "q2", "组队", "玩具", "101", "塔" };
		pqDef.extraMaps.Add(922010201);
		pqDef.extraMaps.Add(922010401);
		pqDef.extraMaps.Add(922010402);
		pqDef.extraMaps.Add(922010403);
		pqDef.extraMaps.Add(922010404);
		pqDef.extraMaps.Add(922010405);
		pqDef.extraMaps.Add(922010501);
		pqDef.extraMaps.Add(922010502);
		pqDef.extraMaps.Add(922010503);
		pqDef.extraMaps.Add(922010504);
		pqDef.extraMaps.Add(922010505);
		pqDef.extraMaps.Add(922010506);
		PqDef pqDef2 = pqDef;
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010100,
			stageNo = 1,
			title = "第1阶段：25 张通行证",
			steps = 
			{
				Talk(2040036, "队长先听红气球讲规则", openOnly: true),
				Collect(4001022, 25, Ids(9300005, 9300006), null, null, "白鼠黑鼠我包了，通行证我来捡", 300000L),
				HandOver(4001022, 25, "25 张都交给队长"),
				Talk(2040036, "队长去交 25 张", openOnly: false, Cleared(1))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010200,
			stageNo = 2,
			title = "第2阶段：15 张通行证",
			steps = 
			{
				Talk(2040037, "队长去听橙气球讲规则", openOnly: true),
				Collect(4001022, 15, null, Ids(2202003), Ids(922010200, 922010201), "箱子我来砸，砸开就有券", 300000L),
				HandOver(4001022, 15, "15 张给队长"),
				Talk(2040037, "队长去交 15 张", openOnly: false, Cleared(2))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010300,
			stageNo = 3,
			title = "第3阶段：32 张通行证",
			steps = 
			{
				Talk(2040038, "队长去听黄气球讲规则", openOnly: true),
				Collect(4001022, 32, Ids(9300007), Ids(2201001), null, "泡泡鱼和箱子我一起收拾", 360000L),
				HandOver(4001022, 32, "32 张齐了"),
				Talk(2040038, "队长去交 32 张", openOnly: false, Cleared(3))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010400,
			stageNo = 4,
			title = "第4阶段：6 张通行证",
			steps = 
			{
				Talk(2040039, "队长去听青气球讲规则", openOnly: true),
				Collect(4001022, 6, Ids(9300008, 9300014), null, Ids(922010401, 922010402, 922010403, 922010404, 922010405), "黑暗塔五间我分开跑", 300000L),
				HandOver(4001022, 6, "6 张给队长"),
				Talk(2040039, "队长去交 6 张", openOnly: false, Cleared(4))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010500,
			stageNo = 5,
			title = "第5阶段：24 张通行证",
			steps = 
			{
				Talk(2040040, "队长去听绿气球讲规则", openOnly: true),
				Collect(4001022, 24, null, Ids(2202003), Ids(922010501, 922010502, 922010503, 922010504, 922010505, 922010506), "六个子图我们分开砸箱子", 360000L),
				HandOver(4001022, 24, "24 张齐了"),
				Talk(2040040, "队长去交 24 张", openOnly: false, Cleared(5))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010600,
			stageNo = 6,
			title = "第6阶段：爬到塔顶",
			gateMs = 20000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010700,
			stageNo = 7,
			title = "第7阶段：3 张通行证",
			steps = 
			{
				Talk(2040042, "队长去听天蓝气球讲规则", openOnly: true),
				Collect(4001022, 3, Ids(9300010), Ids(2201002), null, "战甲鱼我来打", 300000L),
				HandOver(4001022, 3, "3 张给队长"),
				Talk(2040042, "队长去交 3 张", openOnly: false, Cleared(7))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010800,
			stageNo = 8,
			title = "第8阶段：五个箱子占位",
			steps = 
			{
				Talk(2040043, "队长先听蓝气球讲规则", openOnly: true),
				new PqStep
				{
					action = PqAction.Stand,
					say = "我们一人站一个箱子",
					need = 5,
					stands = 
					{
						new PqRect(-247, -232, 61, 25),
						new PqRect(-180, -233, 61, 25),
						new PqRect(-246, -194, 60, 25),
						new PqRect(-180, -195, 61, 26),
						new PqRect(-112, -196, 59, 26)
					}
				},
				Talk(2040043, "队长去按确定", openOnly: false, Cleared(8))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922010900,
			stageNo = 9,
			title = "第9阶段：BOSS 阿丽莎乐",
			steps = 
			{
				Talk(2040044, "队长先听紫气球讲规则", openOnly: true),
				Collect(4001023, 1, Ids(9300012), Ids(2201003), null, "祭坛我来砸，BOSS 交给我", 300000L),
				HandOver(4001023, 1, "钥匙给队长"),
				Talk(2040044, "队长去交钥匙", openOnly: false, Cleared(9))
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922011000,
			stageNo = 10,
			title = "奖励关：等开门",
			gateMs = 6000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 922011100,
			stageNo = 11,
			title = "奖励关：砸完箱子就撤",
			steps = 
			{
				new PqStep
				{
					action = PqAction.BreakReactors,
					say = "箱子我来砸，东西大家分"
				}
			}
		});
		return pqDef2;
	}

	private static PqDef Ellin()
	{
		PqDef pqDef = new PqDef();
		pqDef.key = "EPQ";
		pqDef.name = "毒雾森林";
		pqDef.eventName = "EllinPQ";
		pqDef.recruitMap = 300030100;
		pqDef.entryMap = 930000000;
		pqDef.exitMap = 930000800;
		pqDef.recruitNpc = 2133000;
		pqDef.minLevel = 45;
		pqDef.keywords = new string[10] { "求组", "缺一", "缺二", "缺三", "q1", "q2", "组队", "毒雾", "森林", "艾琳" };
		PqDef pqDef2 = pqDef;
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000000,
			stageNo = 1,
			title = "森林入口"
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000100,
			stageNo = 2,
			title = "森林初入：清掉 50 只毒苔藓木妖",
			steps = { Clear("这片苔藓木妖我来清") }
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000200,
			stageNo = 3,
			title = "变质的森林：砸断毒刺",
			steps = 
			{
				new PqStep
				{
					action = PqAction.BreakReactors,
					ids = Ids(3009000),
					need = 1,
					say = "毒刺我来敲"
				},
				Clear("顺手把守着的怪清掉")
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000300,
			stageNo = 4,
			title = "雾之丛：找出口",
			steps = { Talk(2133001, "队长去跟艾琳说话") }
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000500,
			stageNo = 5,
			title = "森林空地：拿紫色魔力石",
			steps = 
			{
				Collect(4001163, 1, null, Ids(3002001, 3002002), null, "石堆我来砸，魔力石给队长", 240000L),
				HandOver(4001163, 1, "魔力石给队长"),
				Talk(2133004, "队长带着石头去点艾琳")
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000600,
			stageNo = 6,
			title = "毒树丛：石头人三连",
			steps = 
			{
				new PqStep
				{
					action = PqAction.BreakReactors,
					ids = Ids(3001000),
					need = 1,
					say = "祭坛我来开"
				},
				Clear("石头人我来扛，变一次打一次")
			},
			gateMs = 8000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 930000700,
			stageNo = 7,
			title = "艾琳森林：领奖",
			steps = { Talk(2133001, "队长去领奖，领完就回城") }
		});
		return pqDef2;
	}

	private static PqDef Pirate()
	{
		PqDef pqDef = new PqDef();
		pqDef.key = "PPQ";
		pqDef.name = "老海盗";
		pqDef.eventName = "PiratePQ";
		pqDef.recruitMap = 251010404;
		pqDef.entryMap = 925100000;
		pqDef.exitMap = 925100700;
		pqDef.recruitNpc = 2094000;
		pqDef.minLevel = 55;
		pqDef.keywords = new string[9] { "求组", "缺一", "缺二", "缺三", "q1", "q2", "组队", "海盗", "船" };
		pqDef.extraMaps.Add(925100201);
		pqDef.extraMaps.Add(925100202);
		pqDef.extraMaps.Add(925100301);
		pqDef.extraMaps.Add(925100302);
		PqDef pqDef2 = pqDef;
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100000,
			stageNo = 1,
			title = "抵达海盗船的路：清怪",
			steps = { CleanMobs() }
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100100,
			stageNo = 2,
			title = "突破船头：三轮勋章",
			steps = 
			{
				Collect(4001120, 20, Ids(9300114, 9300115, 9300116), null, null, "新手勋章我来捡", 200000L),
				HandOver(4001120, 20, "20 个新手勋章给队长"),
				Talk(2094002, "队长交第一轮", openOnly: true, "stage2", "1"),
				Collect(4001121, 20, Ids(9300114, 9300115, 9300116), null, null, "升华勋章也够了", 200000L),
				HandOver(4001121, 20, "20 个升华勋章给队长"),
				Talk(2094002, "队长交第二轮", openOnly: true, "stage2", "2"),
				Collect(4001122, 20, Ids(9300114, 9300115, 9300116), null, null, "最后一轮资深勋章", 200000L),
				HandOver(4001122, 20, "20 个资深勋章给队长"),
				Talk(2094002, "队长交第三轮", openOnly: true, "stage2", "3")
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100200,
			stageNo = 3,
			title = "突破船甲1：清舱",
			steps = 
			{
				new PqStep
				{
					action = PqAction.ClearMobs,
					maps = Ids(925100200, 925100201),
					say = "子舱的怪我去清"
				}
			},
			gateMs = 12000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100300,
			stageNo = 4,
			title = "突破船甲2：清舱",
			steps = 
			{
				new PqStep
				{
					action = PqAction.ClearMobs,
					maps = Ids(925100300, 925100301),
					say = "这层交给我"
				}
			},
			gateMs = 12000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100400,
			stageNo = 5,
			title = "击退海盗：四个动力门",
			steps = 
			{
				new PqStep
				{
					action = PqAction.BreakReactors,
					ids = Ids(2519000, 2519001, 2519002, 2519003),
					need = 4,
					say = "四个门我一个个敲"
				},
				CleanMobs()
			},
			gateMs = 20000L
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100500,
			stageNo = 6,
			npc = 2094002,
			title = "海盗船长的威严：BOSS",
			steps = 
			{
				new PqStep
				{
					action = PqAction.KillBoss,
					say = "船长交给我"
				}
			}
		});
		pqDef2.stages.Add(new PqStage
		{
			mapId = 925100600,
			stageNo = 7,
			npc = 2094001,
			title = "无恙的感谢：领奖"
		});
		return pqDef2;
	}

	private static PqStep CleanMobs()
	{
		return new PqStep
		{
			action = PqAction.ClearMobs,
			say = "小怪我来清"
		};
	}
}
