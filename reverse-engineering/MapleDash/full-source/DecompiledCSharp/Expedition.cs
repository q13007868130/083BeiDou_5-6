using System;
using System.Collections.Generic;

public sealed class Expedition
{
	public static Func<int> LocalPlayerId;

	public static Action<int, string> Notice;

	public static Action<int, string> DropMessage;

	public static Action<int> ShowClock;

	public static Action HideClock;

	public static Action<int, int> ChangeMap;

	public static Func<string, bool> ConfigBool;

	public static Action<string> Log;

	public static Func<long> Clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private readonly Dictionary<int, string> members = new Dictionary<int, string>();

	private readonly List<int> memberOrder = new List<int>();

	private readonly List<int> banned = new List<int>();

	private readonly Dictionary<string, string> props = new Dictionary<string, string>();

	private readonly List<string> bossLogs = new List<string>();

	public readonly ExpeditionType Type;

	public readonly int LeaderId;

	public readonly string LeaderName;

	public readonly int StartMapId;

	public readonly int MinSize;

	public readonly int MaxSize;

	private readonly bool silent;

	public bool Registering { get; private set; }

	public bool Silent => silent;

	public long StartTime { get; private set; }

	public long RegistrationEndAt { get; private set; }

	public bool Disposed { get; private set; }

	public int MemberCount => members.Count;

	public IReadOnlyList<string> BossLogs => bossLogs;

	public Expedition(int leaderId, string leaderName, int startMapId, ExpeditionType type, bool silent = false, int minPlayers = 0, int maxPlayers = 0)
	{
		LeaderId = leaderId;
		LeaderName = leaderName ?? "";
		StartMapId = startMapId;
		Type = type;
		this.silent = silent;
		ExpeditionTypes.Stats(type, out var minSize, out var maxSize, out var _, out var _, out var _);
		MinSize = ((minPlayers != 0) ? minPlayers : minSize);
		MaxSize = ((maxPlayers != 0) ? maxPlayers : maxSize);
		members[leaderId] = LeaderName;
		memberOrder.Add(leaderId);
	}

	public bool IsLeader(int id)
	{
		return id == LeaderId;
	}

	public bool IsInProgress()
	{
		return !Registering;
	}

	public bool Contains(int id)
	{
		if (!members.ContainsKey(id))
		{
			return IsLeader(id);
		}
		return true;
	}

	public long GetStartTime()
	{
		return StartTime;
	}

	public List<KeyValuePair<int, string>> GetMemberList()
	{
		List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
		foreach (int item in memberOrder)
		{
			if (item != LeaderId && members.TryGetValue(item, out var value))
			{
				list.Add(new KeyValuePair<int, string>(item, value));
			}
		}
		if (members.TryGetValue(LeaderId, out var value2))
		{
			list.Insert(0, new KeyValuePair<int, string>(LeaderId, value2));
		}
		return list;
	}

	public Dictionary<int, string> GetMembers()
	{
		return new Dictionary<int, string>(members);
	}

	public void SetProperty(string key, string value)
	{
		props[key] = value;
	}

	public string GetProperty(string key)
	{
		if (!props.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void BeginRegistration()
	{
		Registering = true;
		int num = ExpeditionTypes.RegistrationMinutes(Type);
		ShowClock?.Invoke(num * 60);
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] [" + LeaderName + "] 已经开启了远征队挑战. 请抓紧时间注册进入远征队！！");
		}
		RegistrationEndAt = Clock() + (long)num * 60000L;
	}

	public void FinishRegistration()
	{
		Registering = false;
	}

	public void Start()
	{
		FinishRegistration();
		HideClock?.Invoke();
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] 远征队已经开始! 勇敢的英雄们，祝你们好运！");
		}
		StartTime = Clock();
	}

	public string AddMember(int id, string name)
	{
		if (!Registering)
		{
			return "抱歉, 里边的远征队已经进行中. 不能再加入远征队了!";
		}
		if (banned.Contains(id))
		{
			return "抱歉, 你已经被远征队长 #b" + LeaderName + "#k 列入了黑名单，不能加入该队伍.";
		}
		if (members.Count >= MaxSize)
		{
			return "抱歉, 远征队人数已满!";
		}
		if (!ExpeditionBossLog.AttemptBoss(id, Type, log: false))
		{
			return "抱歉, 你今日的进入次数已经用完! 请明天在进行尝试...";
		}
		Attach(id, name);
		ShowClock?.Invoke((int)((RegistrationEndAt - Clock()) / 1000));
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] " + name + " 玩家加入了远征队!");
		}
		return "你已经成功加入了远征队!";
	}

	public int AddMemberInt(int id, string name)
	{
		if (!Registering)
		{
			return 1;
		}
		if (banned.Contains(id))
		{
			return 2;
		}
		if (members.Count >= MaxSize)
		{
			return 3;
		}
		Attach(id, name);
		ShowClock?.Invoke((int)((RegistrationEndAt - Clock()) / 1000));
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] " + name + " 玩家已经加入了远征队!");
		}
		return 0;
	}

	private void Attach(int id, string name)
	{
		if (!members.ContainsKey(id))
		{
			memberOrder.Add(id);
		}
		members[id] = name ?? "";
	}

	public bool RemoveMember(int id)
	{
		if (!members.TryGetValue(id, out var value))
		{
			return false;
		}
		members.Remove(id);
		memberOrder.Remove(id);
		HideClock?.Invoke();
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] " + value + " 已经离开了远征队.");
			if (IsLocal(id))
			{
				DropMessage?.Invoke(6, "[远征队] 你已经离开了该远征队.");
			}
		}
		return true;
	}

	public void Ban(int id)
	{
		if (banned.Contains(id))
		{
			return;
		}
		string text = (members.TryGetValue(id, out var value) ? value : "");
		banned.Add(id);
		members.Remove(id);
		memberOrder.Remove(id);
		if (!silent)
		{
			Notice?.Invoke(6, "[远征队] " + text + " 玩家已经被列入了黑名单中.");
		}
		if (IsLocal(id))
		{
			HideClock?.Invoke();
			if (!silent)
			{
				DropMessage?.Invoke(6, "[远征队] 你已经被列入该远征队的黑名单中.");
			}
			if (Type == ExpeditionType.ARIANT || Type == ExpeditionType.ARIANT1 || Type == ExpeditionType.ARIANT2)
			{
				ChangeMap?.Invoke(980000000, 0);
			}
		}
	}

	private static bool IsLocal(int id)
	{
		if (LocalPlayerId != null)
		{
			return LocalPlayerId() == id;
		}
		return true;
	}

	public void MonsterKilled(int mobId, string mobName)
	{
		if (ExpeditionBossLog.IsExpeditionBoss(mobId))
		{
			long num = Clock() - StartTime;
			int num2 = (int)(num / 1000) % 60;
			int num3 = (int)(num / 60000 % 60);
			bossLogs.Add("> " + mobName + " was killed after " + num3 + " 分钟 " + num2 + " 秒");
		}
	}

	public bool IsExpeditionTeamTogether()
	{
		return true;
	}

	public void Dispose(bool log)
	{
		if (!Disposed)
		{
			Disposed = true;
			HideClock?.Invoke();
			if (log && !Registering)
			{
				WriteBossLog();
			}
			ExpeditionRegistry.Detach(this);
		}
	}

	private void WriteBossLog()
	{
		if (bossLogs.Count != 0 && Log != null)
		{
			List<string> list = new List<string>
			{
				"[" + Type.ToString() + "] 的远征队",
				"队长: " + LeaderName,
				"击杀BOSS"
			};
			list.AddRange(bossLogs);
			Log(string.Join("\n", list));
		}
	}
}
