using System;
using System.Collections.Generic;

namespace Server.Bot.Types.Pq;

public static class PqChatLines
{
	private static readonly Dictionary<string, string[]> recruit = new Dictionary<string, string[]>
	{
		["KPQ"] = new string[12]
		{
			"废都下水道求组 缺一 速度", "q1 废都组队 来个会跳舞的", "废都求组 缺二 谁拉我一下", "组队任务 废都 缺一 我全程听话", "废都下水道 q2 有大号带", "求组废都 我能站绳子", "废都缺一 来个牧师稳稳的", "废都组队 缺二 通行证我出", "下水道求组 缺三 新手也行", "废都 q1 就差一个人了",
			"废都组队 我来开 缺一", "废都下水道 缺一 会长带队"
		},
		["LPQ"] = new string[12]
		{
			"玩具塔101求组 缺一", "q1 玩具101 缺二 上塔", "玩具塔 101 组队 缺一 我能砸箱子", "玩具塔101 求组 缺二 带钥匙", "101 缺一 来个能打的", "玩具塔组队 缺三 通行证我包", "玩具塔101 q2 速度进", "上塔求组 缺一 有牧师", "玩具塔 101 组队 缺一 我负责 5 层", "101 组队 缺二 别挂机",
			"玩具塔求组 缺一 我清白鼠", "玩具塔101 缺一 马上开"
		},
		["EPQ"] = new string[12]
		{
			"毒雾森林求组 缺一", "q1 毒雾 缺二 会打石头的来", "毒雾森林组队 缺一 我扛石头人", "毒雾 缺一 魔力石我拿", "森林组队 缺二 45 级以上", "毒雾森林 q2 缺人 速度", "毒雾求组 缺一 打到 BOSS 就散", "毒雾森林 缺三 新手也可以", "艾琳森林求组 缺一", "毒雾 组队 缺一 我负责砸石堆",
			"毒雾森林 缺一 求个牧师", "毒雾森林 缺二 马上开"
		},
		["PPQ"] = new string[12]
		{
			"海盗船求组 缺一", "q1 老海盗 缺二 55 以上", "海盗船组队 缺一 我打船长", "老海盗 缺一 三轮勋章我出", "海盗船 缺二 会清怪的来", "老海盗 q2 缺人", "海盗船求组 缺一 我负责开四个门", "老海盗 组队 缺三 新手也行", "海盗船 缺一 很快的", "老海盗求组 缺一 来个能扛的",
			"海盗船 组队 缺二 别掉线", "老海盗 缺一 马上进"
		}
	};

	private static readonly Dictionary<string, string[]> roles = new Dictionary<string, string[]>
	{
		["KPQ"] = new string[8] { "第二阶段我去挂绳子，你们等着", "我挂最左边那根，别跟我抢", "第三阶段我站中间的平台", "第四阶段木桶我来踩", "通行证我来数，你们别乱交", "我伤害高，最后 BOSS 我来拉", "券我捡，最后一起交给队长", "谁去另一边？我这边清完了" },
		["LPQ"] = new string[9] { "第一阶段白鼠黑鼠我包了", "我砸箱子最快，箱子给我", "第三阶段双鱼我拉", "5 层我一个人够，你们去 6 层", "第 6 关爬塔我走前面，跟紧我", "第 8 关我站左边箱子", "BOSS 我扛，你们输出", "钥匙我来拿，最后交给队长", "通行证我先攒着，够了给队长" },
		["EPQ"] = new string[8] { "先清 50 只苔藓木妖，我打左边", "毒刺我来敲，你们别碰", "迷宫我们分头走，找到 16 格喊我", "魔力石我砸出来给你们", "石头人三连我扛", "别碰 930000400 那张图，进去出不来", "我负责砸石堆，你们清小怪", "BOSS 血量很高，慢慢打" },
		["PPQ"] = new string[8] { "第一关小怪我来清", "三轮勋章我出，队长只管交", "四个门我一个一个来", "船长我拉，你们别站正面", "宝箱别急着开，开了 BOSS 更凶", "我去左边清怪，右边交给你", "先把船头打通", "掉线就完了，稳住" }
	};

	public static readonly string[] declineReasons = new string[8] { "不好意思，我已经在队里了。", "我等级还差一点，怕拖后腿。", "刚才被踢过一次，我换个队吧。", "我这边已经约好人了，下次吧。", "队长不是我，我得问下我们队长。", "我装备还没修，等我一下。", "人满了？那我先喊喊看。", "我背包满了，先清一下。" };

	public static readonly string[] idle = new string[12]
	{
		"这图人真多", "谁来收一下我的通行证", "刚出了个卷，可惜是 10%", "有人卖 60% 手套攻击吗", "站了半天没人组我", "这副本一天能刷几次啊", "我朋友说这里出好东西", "打字好累，有没有人带队", "网有点卡，别催", "谁有多余的药水，收一组",
		"刚被怪打死了，掉了经验", "这区的组队任务真难进"
	};

	private static readonly string[] answers = new string[12]
	{
		"+1", "我我我", "我来", "缺几个？", "带我", "求组", "我到了", "拉我", "算我一个", "我正好没事",
		"几级能进？", "等我跑过去"
	};

	public static readonly string[] beforeEnter = new string[4] { "满员了，走起", "人齐，我先进", "准备好药，别到时候没血", "都到齐了吗，进本了" };

	public static readonly string[] afterClear = new string[5] { "漂亮，一次过", "辛苦大家", "下次还组", "这波不亏", "我再去喊一队" };

	public static string Recruit(string key, Random random)
	{
		if (key != null && recruit.TryGetValue(key, out var value) && value.Length != 0)
		{
			return value[random.Next(value.Length)];
		}
		return idle[random.Next(idle.Length)];
	}

	public static string Role(string key, Random random)
	{
		if (key != null && roles.TryGetValue(key, out var value) && value.Length != 0)
		{
			return value[random.Next(value.Length)];
		}
		return idle[random.Next(idle.Length)];
	}

	public static string Decline(Random random)
	{
		return declineReasons[random.Next(declineReasons.Length)];
	}

	public static string Idle(Random random)
	{
		return idle[random.Next(idle.Length)];
	}

	public static string Answer(Random random)
	{
		return answers[random.Next(answers.Length)];
	}
}
