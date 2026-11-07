using System;
using System.Collections.Generic;
using MapleDash.Js;

public sealed class NativeHuntTasks
{
	public sealed class Task
	{
		public int type;

		public int itemId;

		public int bossId;

		public int needCount;

		public string name = "";

		public string completedBy = "";

		public bool completed;

		public long start;

		public long end;

		public long completedAt;
	}

	public sealed class Reward
	{
		public int itemId;

		public int count;

		public double chance;

		public string name;
	}

	private readonly Func<string> config;

	private readonly List<Task> items = new List<Task>();

	private readonly List<Reward> rewards = new List<Reward>();

	private readonly Random random = new Random();

	public Func<long> Clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public Func<int, int> Choose;

	public Func<double> Roll;

	public Action<string> Notice;

	public Func<int, string> ItemName;

	public Func<int, string> MobName;

	private Task current = new Task();

	private bool running;

	private long next;

	public int Gold { get; private set; } = 100000;

	public int Exp { get; private set; } = 50000;

	public int Cash { get; private set; } = 100;

	public int Interval { get; private set; } = 3600;

	public IReadOnlyList<Reward> Rewards => rewards;

	public NativeHuntTasks(Func<string> config)
	{
		this.config = config;
		Reload();
	}

	private static int Int(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static string Text(Dictionary<string, object> row, string key)
	{
		if (!row.TryGetValue(key, out var value))
		{
			return null;
		}
		return value as string;
	}

	public void Reload()
	{
		running = false;
		items.Clear();
		rewards.Clear();
		Interval = 3600;
		string text = config();
		if (!string.IsNullOrEmpty(text))
		{
			Dictionary<string, object> dictionary = (MiniJsonDb.Parse(text) as Dictionary<string, object>) ?? throw new JsRuntimeException("Invalid hunt task configuration");
			if (dictionary.TryGetValue("HUNT_CONFIG", out var value))
			{
				Interval = Int(value as Dictionary<string, object>, "TASK_INTERVAL", 3600);
			}
			if (Interval <= 0)
			{
				throw new JsRuntimeException("Invalid hunt task interval");
			}
			if (dictionary.TryGetValue("HUNT_ITEMS", out var value2) && value2 is List<object> list)
			{
				foreach (Dictionary<string, object> item in list)
				{
					items.Add(new Task
					{
						type = Int(item, "type"),
						itemId = Int(item, "itemId"),
						bossId = Int(item, "bossId"),
						needCount = Int(item, "needCount"),
						name = Text(item, "name")
					});
				}
			}
			if (dictionary.TryGetValue("REWARD_INFO", out var value3) && value3 is Dictionary<string, object> dictionary2)
			{
				Gold = Int(dictionary2, "GOLD_REWARD", 100000);
				Exp = Int(dictionary2, "EXP_REWARD", 50000);
				Cash = Int(dictionary2, "CASH_REWARD", 100);
				if (dictionary2.TryGetValue("REWARD_ITEMS", out var value4) && value4 is List<object> list2)
				{
					foreach (Dictionary<string, object> item2 in list2)
					{
						rewards.Add(new Reward
						{
							itemId = Int(item2, "itemId"),
							count = Int(item2, "count", 1),
							name = Text(item2, "name"),
							chance = (item2.TryGetValue("chance", out var value5) ? Convert.ToDouble(value5) : 0.0)
						});
					}
				}
			}
		}
		Start();
	}

	public void Start()
	{
		if (items.Count != 0)
		{
			Publish();
			running = true;
			next = Clock() + (long)Interval * 1000L;
		}
	}

	public void Stop()
	{
		running = false;
	}

	public void Tick()
	{
		if (running && Clock() >= next)
		{
			Publish();
			next += (long)Interval * 1000L;
		}
	}

	private void Publish()
	{
		Task task = items[Choose?.Invoke(items.Count) ?? random.Next(items.Count)];
		string text = task.name;
		if (string.IsNullOrEmpty(text))
		{
			text = ((task.type != 2) ? (ItemName?.Invoke(task.itemId) ?? "未知道具") : (MobName?.Invoke(task.bossId) ?? "BOSS"));
		}
		long num = Clock();
		current = new Task
		{
			type = task.type,
			itemId = task.itemId,
			bossId = task.bossId,
			needCount = task.needCount,
			name = text,
			start = num,
			end = num + (long)Interval * 1000L
		};
		Notice?.Invoke("[狩猎任务] 新任务发布啦！\r\n" + ((task.type == 2) ? ("狩猎BOSS：" + text + "，\r\n第一个击杀该BOSS的玩家获得奖励！\r\n") : ("需要道具：" + text + " x" + task.needCount + "，\r\n全服只有一个玩家能够完成任务，先到先得！\r\n")) + "限时：" + Interval / 3600 + "小时");
	}

	public Task Current()
	{
		if (!current.completed && Clock() > current.end)
		{
			current = new Task();
		}
		return current;
	}

	public int CanComplete(int count)
	{
		if (current.type == 2 || current.completed || current.itemId == 0)
		{
			return 1;
		}
		if (Clock() > current.end)
		{
			return 2;
		}
		if (count >= current.needCount)
		{
			return 0;
		}
		return 3;
	}

	public void MarkCompleted(string name)
	{
		current.completed = true;
		current.completedBy = name;
		current.completedAt = Clock();
		CompletionNotice(name, null);
	}

	public bool MonsterKilled(int id, string player, Action award)
	{
		if (current.type != 2 || current.completed || Clock() > current.end || current.bossId != id)
		{
			return false;
		}
		current.completed = true;
		current.completedBy = player;
		current.completedAt = Clock();
		award();
		return true;
	}

	public bool RewardWon(Reward reward)
	{
		return (Roll?.Invoke() ?? random.NextDouble()) < reward.chance;
	}

	public void CompletionNotice(string player, IList<string> success)
	{
		Notice?.Invoke("[狩猎任务] 恭喜玩家 " + player + " 完成了狩猎任务！\r\n" + ((current.type == 2) ? ("狩猎BOSS：" + current.name + "\r\n") : ("完成道具：#v" + current.itemId + "# #z" + current.itemId + "# × " + current.needCount + "\r\n")) + "获得奖励：" + ((success != null && success.Count > 0) ? string.Join("、", success) : "金币10万、经验5万、点券100"));
	}

	private static void Bind(JsHostObject host, string method, Func<JsCall, JsValue> fn)
	{
		host.Set(method, JsFunction.FromNative(method, fn));
	}

	private static JsHostObject Strict(object value)
	{
		return new JsHostObject(value)
		{
			Missing = (string key) =>
			{
				throw new JsRuntimeException("Unmigrated HuntTaskAutoPublisher API: " + key);
			}
		};
	}

	private JsValue TaskHost(Task task)
	{
		JsHostObject jsHostObject = Strict(task);
		Bind(jsHostObject, "getType", (JsCall c) => new JsNumber(task.type));
		Bind(jsHostObject, "getItemId", (JsCall c) => new JsNumber(task.itemId));
		Bind(jsHostObject, "getBossId", (JsCall c) => new JsNumber(task.bossId));
		Bind(jsHostObject, "getNeedCount", (JsCall c) => new JsNumber(task.needCount));
		Bind(jsHostObject, "getItemName", (JsCall c) => new JsString(task.name));
		Bind(jsHostObject, "isBossTask", (JsCall c) => JsBoolean.Of(task.type == 2));
		Bind(jsHostObject, "isCompleted", (JsCall c) => JsBoolean.Of(task.completed));
		Bind(jsHostObject, "getCompletedByName", (JsCall c) => new JsString(task.completedBy));
		Bind(jsHostObject, "getCompletedTime", (JsCall c) => new JsNumber(task.completedAt));
		Bind(jsHostObject, "getStartTime", (JsCall c) => new JsNumber(task.start));
		Bind(jsHostObject, "getEndTime", (JsCall c) => new JsNumber(task.end));
		return jsHostObject;
	}

	public JsValue Type()
	{
		JsHostObject jsHostObject = Strict(this);
		JsHostObject host = Strict(this);
		Bind(jsHostObject, "getInstance", (JsCall c) => host);
		Bind(host, "getCurrentTask", (JsCall c) => TaskHost(Current()));
		Bind(host, "getTaskInterval", (JsCall c) => new JsNumber(Interval));
		Bind(host, "getHuntItemsSize", (JsCall c) => new JsNumber(items.Count));
		Bind(host, "getGoldReward", (JsCall c) => new JsNumber(Gold));
		Bind(host, "getExpReward", (JsCall c) => new JsNumber(Exp));
		Bind(host, "getCashReward", (JsCall c) => new JsNumber(Cash));
		Bind(host, "getRewardItems", (JsCall c) =>
		{
			JsArray jsArray = new JsArray();
			foreach (Reward reward in rewards)
			{
				JsHostObject jsHostObject2 = Strict(reward);
				Bind(jsHostObject2, "getItemId", (JsCall _) => new JsNumber(reward.itemId));
				Bind(jsHostObject2, "getCount", (JsCall _) => new JsNumber(reward.count));
				Bind(jsHostObject2, "getChance", (JsCall _) => new JsNumber(reward.chance));
				Bind(jsHostObject2, "getName", (JsCall _) => (reward.name != null) ? ((JsValue)new JsString(reward.name)) : ((JsValue)JsNull.Value));
				jsArray.Items.Add(jsHostObject2);
			}
			return jsArray;
		});
		Bind(host, "canCompleteTask", (JsCall c) =>
		{
			JsValue jsValue = c.Arg(0);
			JsValue v = c.Interp.Call(c.Interp.GetMember(jsValue, "getItemQuantity"), jsValue, new JsValue[2]
			{
				new JsNumber(current.itemId),
				JsBoolean.False
			});
			return new JsNumber(CanComplete(JsConvert.ToInt32(v)));
		});
		Bind(host, "markTaskCompleted", (JsCall c) =>
		{
			MarkCompleted(JsConvert.ToString(c.Arg(0)));
			return JsUndefined.Value;
		});
		Bind(host, "reload", (JsCall c) =>
		{
			Reload();
			return JsUndefined.Value;
		});
		Bind(host, "start", (JsCall c) =>
		{
			Start();
			return JsUndefined.Value;
		});
		Bind(host, "stop", (JsCall c) =>
		{
			Stop();
			return JsUndefined.Value;
		});
		return jsHostObject;
	}
}
