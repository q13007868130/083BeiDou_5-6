using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class NativeEventArenaService
{
	private abstract class Arena
	{
		public readonly int mapId;

		public readonly Dictionary<int, int> teams = new Dictionary<int, int>();

		public long startAt;

		public long endAt;

		public long clearedAt;

		public bool ended;

		public int winnerTeam = -1;

		public readonly List<string> notices = new List<string>();

		public abstract int Kind { get; }

		protected Arena(int mapId)
		{
			this.mapId = mapId;
		}

		public int TeamOf(int cid)
		{
			if (!teams.TryGetValue(cid, out var value))
			{
				return -1;
			}
			return value;
		}

		public void Notice(string msg)
		{
			notices.Add(msg);
		}
	}

	private sealed class SnowballArena : Arena
	{
		public sealed class Ball
		{
			public int position;

			public int hits = 3;

			public int snowmanHp = 1000;
		}

		public readonly Ball[] balls = new Ball[2]
		{
			new Ball(),
			new Ball()
		};

		public readonly Dictionary<int, long> lastAttack = new Dictionary<int, long>();

		public override int Kind => 1;

		public SnowballArena()
			: base(109060000)
		{
		}
	}

	private sealed class CoconutArena : Arena
	{
		public sealed class Nut
		{
			public int hits;

			public bool hittable = true;

			public long hitTime;
		}

		public readonly Nut[] nuts = new Nut[506];

		public int mapleScore;

		public int storyScore;

		public int bombings = 80;

		public int falling = 401;

		public int stopped = 20;

		public bool bonusUsed;

		public override int Kind => 2;

		public CoconutArena()
			: base(109080000)
		{
			for (int i = 0; i < 506; i++)
			{
				nuts[i] = new Nut();
			}
		}
	}

	public const int SnowballMap = 109060000;

	public const int CoconutMap = 109080000;

	public const int WinnerMap = 109050000;

	public const int ExitMap = 109050001;

	private const int SnowballDurationMs = 600000;

	private const int SnowballWarpDelayMs = 10000;

	private const int SnowballAttackIntervalMs = 500;

	private const int SnowmanHpMax = 1000;

	private const int HitsPerRoll = 3;

	private const int CoconutDurationMs = 300000;

	private const int CoconutBonusMs = 120000;

	private const int CoconutWarpDelayMs = 12000;

	private const int CoconutHitLockMs = 750;

	private const int CoconutCount = 506;

	private const int CountBombing = 80;

	private const int CountFalling = 401;

	private const int CountStopped = 20;

	private static readonly Random rng = new Random();

	private static Func<double> random = () => rng.NextDouble();

	private static readonly Stopwatch boot = Stopwatch.StartNew();

	private static Func<long> clock = () => boot.ElapsedMilliseconds;

	private static readonly Dictionary<int, Arena> arenas = new Dictionary<int, Arena>();

	private static readonly object gate = new object();

	public static void SetRandom(Func<double> r)
	{
		random = r ?? ((Func<double>)(() => rng.NextDouble()));
	}

	public static void SetClock(Func<long> c)
	{
		clock = ((c != null) ? c : ((Func<long>)(() => boot.ElapsedMilliseconds)));
	}

	public static void ResetForTest()
	{
		lock (gate)
		{
			arenas.Clear();
			random = () => rng.NextDouble();
			clock = () => boot.ElapsedMilliseconds;
		}
	}

	public static ArenaResponse Handle(DbStore db, ArenaRequest r)
	{
		if (r == null)
		{
			return new ArenaResponse
			{
				ok = false,
				error = "无效请求"
			};
		}
		lock (gate)
		{
			return r.operation switch
			{
				ArenaOperation.Sync => Sync(db, r.characterId), 
				ArenaOperation.Hit => Hit(db, r.characterId, r.param), 
				ArenaOperation.Leave => Leave(db, r.characterId), 
				_ => new ArenaResponse
				{
					ok = false,
					error = "无效操作"
				}, 
			};
		}
	}

	private static int MapOf(DbStore db, int cid)
	{
		return CharactersDao.FindById(db, cid)?.map ?? (-1);
	}

	private static ArenaResponse Sync(DbStore db, int cid)
	{
		int num = MapOf(db, cid);
		if (num != 109060000 && num != 109080000)
		{
			LeaveAll(db, cid);
			return new ArenaResponse
			{
				ok = true,
				kind = 0
			};
		}
		Arena arena = EnsureArena(num);
		Tick(arena);
		if (arena.TeamOf(cid) < 0 && !arena.ended)
		{
			arena.teams[cid] = arena.teams.Count % 2;
		}
		return Snapshot(arena, cid);
	}

	private static Arena EnsureArena(int map)
	{
		if (!arenas.TryGetValue(map, out var value) || (value.ended && clock() > value.clearedAt))
		{
			value = ((map == 109060000) ? ((Arena)new SnowballArena()) : ((Arena)new CoconutArena()));
			value.startAt = clock();
			value.endAt = value.startAt + ((map == 109060000) ? 600000 : 300000);
			arenas[map] = value;
		}
		return value;
	}

	private static ArenaResponse Leave(DbStore db, int cid)
	{
		LeaveAll(db, cid);
		return new ArenaResponse
		{
			ok = true,
			kind = 0
		};
	}

	private static void LeaveAll(DbStore db, int cid)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Arena> arena in arenas)
		{
			arena.Value.teams.Remove(cid);
			if (arena.Value.teams.Count == 0)
			{
				list.Add(arena.Key);
			}
		}
		foreach (int item in list)
		{
			arenas.Remove(item);
		}
	}

	private static ArenaResponse Hit(DbStore db, int cid, int param)
	{
		int key = MapOf(db, cid);
		if (!arenas.TryGetValue(key, out var value))
		{
			return new ArenaResponse
			{
				ok = false,
				error = "这里没有进行中的活动"
			};
		}
		Tick(value);
		if (value.ended)
		{
			return Snapshot(value, cid);
		}
		int num = value.TeamOf(cid);
		if (num < 0)
		{
			return new ArenaResponse
			{
				ok = false,
				error = "你不在活动队伍里"
			};
		}
		if (!(value is SnowballArena arena))
		{
			return HitCoconut((CoconutArena)value, db, cid, num, param);
		}
		return HitSnowball(arena, cid, num, param);
	}

	private static ArenaResponse HitSnowball(SnowballArena arena, int cid, int team, int what)
	{
		SnowballArena.Ball ball = arena.balls[team];
		SnowballArena.Ball ball2 = arena.balls[(team == 0) ? 1u : 0u];
		if (ball.snowmanHp == 0)
		{
			return Snapshot(arena, cid);
		}
		long num = clock();
		if (arena.lastAttack.TryGetValue(cid, out var value) && num - value < 500)
		{
			return Snapshot(arena, cid);
		}
		if (team != what % 2)
		{
			return new ArenaResponse
			{
				ok = false,
				error = "打错了目标"
			};
		}
		arena.lastAttack[cid] = num;
		int damage = 0;
		if (what < 2 && ball2.snowmanHp > 0)
		{
			damage = 10;
		}
		else if (what == 2 || what == 3)
		{
			damage = ((random() < 0.03) ? 45 : 15);
		}
		if (what >= 0 && what <= 4)
		{
			RollHit(arena, ball, ball2, what, damage);
		}
		return Snapshot(arena, cid);
	}

	private static void RollHit(SnowballArena arena, SnowballArena.Ball mine, SnowballArena.Ball other, int what, int damage)
	{
		if (what < 2)
		{
			if (damage > 0)
			{
				mine.hits--;
			}
			else if (mine.snowmanHp - damage < 0)
			{
				mine.snowmanHp = 0;
			}
			else
			{
				mine.snowmanHp -= damage;
			}
		}
		if (mine.hits == 0)
		{
			mine.position++;
			switch (mine.position)
			{
			case 45:
				arena.Notice("对方的雪球已经滚到第一段了！");
				break;
			case 290:
				arena.Notice("对方的雪球已经滚到第二段了！");
				break;
			case 560:
				arena.Notice("对方的雪球已经滚到第三段了！");
				break;
			}
			mine.hits = 3;
		}
	}

	private static ArenaResponse HitCoconut(CoconutArena arena, DbStore db, int cid, int team, int id)
	{
		long num = clock();
		if (id < 0 || id >= 506)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < 506; i++)
			{
				if (arena.nuts[i].hittable && num >= arena.nuts[i].hitTime)
				{
					list.Add(i);
				}
			}
			if (list.Count == 0)
			{
				return Snapshot(arena, cid);
			}
			id = list[rng.Next(list.Count)];
		}
		CoconutArena.Nut nut = arena.nuts[id];
		if (!nut.hittable)
		{
			return Snapshot(arena, cid);
		}
		if (num < nut.hitTime)
		{
			return Snapshot(arena, cid);
		}
		if (nut.hits > 2 && random() < 0.4)
		{
			if (random() < 0.01 && arena.stopped > 0)
			{
				nut.hittable = false;
				arena.stopped--;
				return Snapshot(arena, cid);
			}
			nut.hittable = false;
			nut.hits = 0;
			if (random() < 0.05 && arena.bombings > 0)
			{
				arena.bombings--;
				return Snapshot(arena, cid);
			}
			if (arena.falling > 0)
			{
				arena.falling--;
				string text = CharactersDao.FindById(db, cid)?.name ?? "?";
				if (team == 0)
				{
					arena.mapleScore++;
					arena.Notice(text + " of Team Maple knocks down a coconut.");
				}
				else
				{
					arena.storyScore++;
					arena.Notice(text + " of Team Story knocks down a coconut.");
				}
			}
		}
		else
		{
			nut.hits++;
			nut.hitTime = num + 750;
		}
		return Snapshot(arena, cid);
	}

	private static void Tick(Arena arena)
	{
		if (arena.ended)
		{
			return;
		}
		long num = clock();
		if (num < arena.endAt)
		{
			return;
		}
		arena.ended = true;
		arena.clearedAt = num + ((arena is SnowballArena) ? 10000 : 12000);
		if (arena is SnowballArena snowballArena)
		{
			snowballArena.winnerTeam = ((snowballArena.balls[0].position <= snowballArena.balls[1].position) ? ((snowballArena.balls[1].position > snowballArena.balls[0].position) ? 1 : (-1)) : 0);
			snowballArena.Notice((snowballArena.winnerTeam < 0) ? "雪球大战平局！" : ("Team " + ((snowballArena.winnerTeam == 0) ? "Maple" : "Story") + " 获胜！"));
		}
		else
		{
			if (!(arena is CoconutArena coconutArena))
			{
				return;
			}
			if (coconutArena.mapleScore == coconutArena.storyScore && !coconutArena.bonusUsed)
			{
				coconutArena.bonusUsed = true;
				coconutArena.ended = false;
				coconutArena.endAt = num + 120000;
				coconutArena.Notice("平分！加时 2 分钟！");
				return;
			}
			coconutArena.winnerTeam = ((coconutArena.mapleScore <= coconutArena.storyScore) ? ((coconutArena.storyScore > coconutArena.mapleScore) ? 1 : (-1)) : 0);
			for (int i = 0; i < 506; i++)
			{
				coconutArena.nuts[i].hittable = false;
			}
			coconutArena.Notice((coconutArena.winnerTeam < 0) ? "椰子树大战平局！" : ("Team " + ((coconutArena.winnerTeam == 0) ? "Maple" : "Story") + " 获胜！"));
		}
	}

	private static ArenaResponse Snapshot(Arena arena, int cid)
	{
		ArenaResponse arenaResponse = new ArenaResponse
		{
			ok = true,
			kind = arena.Kind,
			myTeam = arena.TeamOf(cid)
		};
		long num = clock();
		if (!arena.ended)
		{
			arenaResponse.timeLeftMs = Math.Max(0L, arena.endAt - num);
		}
		else
		{
			arenaResponse.ended = true;
		}
		if (arena is SnowballArena snowballArena)
		{
			int num2 = Math.Max(0, arenaResponse.myTeam);
			arenaResponse.myPosition = snowballArena.balls[num2].position;
			arenaResponse.otherPosition = snowballArena.balls[(num2 == 0) ? 1u : 0u].position;
			arenaResponse.mySnowmanHp = snowballArena.balls[num2].snowmanHp;
			arenaResponse.otherSnowmanHp = snowballArena.balls[(num2 == 0) ? 1u : 0u].snowmanHp;
			if (snowballArena.winnerTeam >= 0 && arenaResponse.myTeam >= 0)
			{
				arenaResponse.warpTo = ((arenaResponse.myTeam == snowballArena.winnerTeam) ? 109050000 : 109050001);
			}
		}
		else if (arena is CoconutArena coconutArena)
		{
			arenaResponse.mapleScore = coconutArena.mapleScore;
			arenaResponse.storyScore = coconutArena.storyScore;
			arenaResponse.bombings = coconutArena.bombings;
			arenaResponse.falling = coconutArena.falling;
			arenaResponse.stopped = coconutArena.stopped;
			if (coconutArena.winnerTeam >= 0 && arenaResponse.myTeam >= 0)
			{
				arenaResponse.warpTo = ((arenaResponse.myTeam == coconutArena.winnerTeam) ? 109050000 : 109050001);
			}
		}
		if (arena.notices.Count > 0)
		{
			arenaResponse.notice = string.Join("\n", arena.notices);
			arena.notices.Clear();
		}
		return arenaResponse;
	}
}
