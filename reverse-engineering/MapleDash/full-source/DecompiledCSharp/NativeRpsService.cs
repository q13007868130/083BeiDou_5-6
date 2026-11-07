using System;
using System.Collections.Generic;

public static class NativeRpsService
{
	private sealed class Session
	{
		public int round;

		public bool ableAnswer = true;

		public bool win;
	}

	public const int RpsAdminNpc = 9000019;

	public const int CertificateBase = 4031332;

	private const int EntryFee = 1000;

	private const int Rounds = 10;

	private static readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();

	private static readonly Random rng = new Random();

	private static Func<int> random = () => rng.Next(3);

	public static void SetRandom(Func<int> r)
	{
		random = r ?? ((Func<int>)(() => rng.Next(3)));
	}

	public static void ResetForTest()
	{
		sessions.Clear();
		random = () => rng.Next(3);
	}

	public static void EndSession(int characterId)
	{
		sessions.Remove(characterId);
	}

	public static void ClearSessions()
	{
		sessions.Clear();
	}

	public static RpsResponse Handle(DbStore db, RpsRequest r)
	{
		if (r == null)
		{
			return new RpsResponse
			{
				ok = false,
				error = "无效请求"
			};
		}
		sessions.TryGetValue(r.characterId, out var value);
		switch (r.operation)
		{
		case RpsOperation.Start:
		case RpsOperation.Retry:
			return Start(db, r.characterId, value, r.operation == RpsOperation.Retry);
		case RpsOperation.Answer:
			return Answer(db, r.characterId, value, r.answer);
		case RpsOperation.TimeOver:
			return TimeOver(db, r.characterId, value);
		case RpsOperation.Continue:
			return NextRound(db, r.characterId, value);
		case RpsOperation.Leave:
			return Dispose(db, r.characterId, value);
		default:
			return new RpsResponse
			{
				ok = false,
				error = "无效操作"
			};
		}
	}

	private static RpsResponse Start(DbStore db, int cid, Session s, bool retry)
	{
		int? num = RewardIfWin(db, cid, s);
		CharRecord charRecord = CharactersDao.FindById(db, cid);
		if (charRecord.meso < 1000)
		{
			return new RpsResponse
			{
				ok = false,
				mode = 6,
				error = "金币不足，无法开始游戏。"
			};
		}
		charRecord.meso -= 1000L;
		CharactersDao.Save(db, charRecord);
		sessions[cid] = new Session();
		return new RpsResponse
		{
			ok = true,
			mode = (retry ? 14 : 9),
			rewardItem = num.GetValueOrDefault()
		};
	}

	private static RpsResponse Answer(DbStore db, int cid, Session s, int answer)
	{
		if (s == null || !s.ableAnswer || s.win || answer < 0 || answer > 2)
		{
			int? num = RewardIfWin(db, cid, s);
			sessions.Remove(cid);
			return new RpsResponse
			{
				ok = false,
				mode = 13,
				closed = true,
				rewardItem = num.GetValueOrDefault(),
				error = "当前不能出手。"
			};
		}
		int num2 = random();
		if (num2 == answer)
		{
			return new RpsResponse
			{
				ok = true,
				mode = 11,
				selection = num2,
				roundLabel = s.round
			};
		}
		if ((answer == 0 && num2 == 2) || (answer == 1 && num2 == 0) || (answer == 2 && num2 == 1))
		{
			s.win = true;
			s.ableAnswer = false;
			return new RpsResponse
			{
				ok = true,
				mode = 11,
				selection = num2,
				roundLabel = s.round + 1
			};
		}
		s.ableAnswer = false;
		return new RpsResponse
		{
			ok = true,
			mode = 11,
			selection = num2,
			roundLabel = -1
		};
	}

	private static RpsResponse TimeOver(DbStore db, int cid, Session s)
	{
		if (s != null && s.ableAnswer && !s.win)
		{
			s.ableAnswer = false;
			return new RpsResponse
			{
				ok = true,
				mode = 10
			};
		}
		return Dispose(db, cid, s);
	}

	private static RpsResponse NextRound(DbStore db, int cid, Session s)
	{
		if (s != null && s.win)
		{
			s.round++;
			if (s.round < 10)
			{
				s.win = false;
				s.ableAnswer = true;
				return new RpsResponse
				{
					ok = true,
					mode = 12
				};
			}
			s.round = 10;
		}
		return Dispose(db, cid, s);
	}

	private static RpsResponse Dispose(DbStore db, int cid, Session s)
	{
		int? num = RewardIfWin(db, cid, s);
		sessions.Remove(cid);
		return new RpsResponse
		{
			ok = true,
			mode = 13,
			closed = true,
			rewardItem = num.GetValueOrDefault()
		};
	}

	private static int? RewardIfWin(DbStore db, int cid, Session s)
	{
		if (s == null || !s.win)
		{
			return null;
		}
		int num = 4031332 + s.round;
		CharRecord c = CharactersDao.FindById(db, cid);
		if (!NativeInventoryService.CanReceive(db, c, new InventoryChangeRequest
		{
			checkItems = new int[1] { num },
			checkQuantities = new int[1] { 1 }
		}))
		{
			return null;
		}
		NativeInventoryService.AddItems(db, c, num, 1, -1L);
		return num;
	}
}
