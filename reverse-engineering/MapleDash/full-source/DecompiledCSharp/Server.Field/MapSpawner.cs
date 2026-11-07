using System;
using System.Collections.Generic;

namespace Server.Field;

public sealed class MapSpawner
{
	public Func<long> Clock = () => (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

	private readonly List<SpawnPoint> monsterSpawn = new List<SpawnPoint>();

	private readonly List<SpawnPoint> frenzySpawn = new List<SpawnPoint>();

	private int mobInterval = 5000;

	private bool mIsRandomRefresh = true;

	private bool allowSummons = true;

	public double BossRespawnMobTimeRate = 1.0;

	public bool FullRespawn { get; set; }

	public bool FrenzyTotemActive { get; set; }

	public int SpawnPointCount => monsterSpawn.Count;

	public List<SpawnPoint> AllSpawnPoints => new List<SpawnPoint>(monsterSpawn);

	public void SetMobInterval(int ms)
	{
		mobInterval = ms;
	}

	public void SetRandomRefresh(bool v)
	{
		mIsRandomRefresh = v;
	}

	public void SetAllowSummons(bool v)
	{
		allowSummons = v;
	}

	public SpawnPoint AddMonsterSpawn(SpawnPointDef def)
	{
		int num = def.mobTime;
		if (def.isBoss)
		{
			double bossRespawnMobTimeRate = BossRespawnMobTimeRate;
			if (bossRespawnMobTimeRate > 0.0 && bossRespawnMobTimeRate <= 1.0)
			{
				num = (int)((double)num * bossRespawnMobTimeRate);
			}
		}
		SpawnPoint spawnPoint = new SpawnPoint(new SpawnPointDef
		{
			mob = def.mob,
			mobTime = num,
			fh = def.fh,
			f = def.f,
			team = def.team,
			cy = def.cy,
			x = def.x,
			y = def.y,
			rx0 = def.rx0,
			rx1 = def.rx1,
			immobile = def.immobile,
			isBoss = def.isBoss
		}, mobInterval, Clock());
		monsterSpawn.Add(spawnPoint);
		return spawnPoint;
	}

	public void AddAllMonsterSpawn(SpawnPointDef def)
	{
		monsterSpawn.Add(new SpawnPoint(def, mobInterval, Clock()));
	}

	public List<SpawnPoint> SpawnPointsOf(int mobId)
	{
		List<SpawnPoint> list = new List<SpawnPoint>();
		foreach (SpawnPoint item in monsterSpawn)
		{
			if (item.MobId == mobId)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public List<SpawnPoint> StartFrenzy()
	{
		if (FrenzyTotemActive)
		{
			return new List<SpawnPoint>();
		}
		foreach (SpawnPoint item in monsterSpawn)
		{
			if (item.MobTime >= 0)
			{
				item.SetRespawnTimeMultiplier(0.5);
				frenzySpawn.Add(item.Duplicate(Clock()));
			}
		}
		monsterSpawn.AddRange(frenzySpawn);
		FrenzyTotemActive = true;
		return new List<SpawnPoint>(frenzySpawn);
	}

	public void StopFrenzy()
	{
		foreach (SpawnPoint item in frenzySpawn)
		{
			monsterSpawn.Remove(item);
		}
		frenzySpawn.Clear();
		foreach (SpawnPoint item2 in monsterSpawn)
		{
			item2.SetRespawnTimeMultiplier(1.0);
		}
		FrenzyTotemActive = false;
	}

	private int GetSpawnedOnMap()
	{
		int num = 0;
		foreach (SpawnPoint item in monsterSpawn)
		{
			num += item.Spawned;
		}
		return num;
	}

	public List<SpawnPoint> Respawn(int numPlayers)
	{
		if (!allowSummons)
		{
			return new List<SpawnPoint>();
		}
		int num = CalculateNumShouldSpawn(monsterSpawn.Count, GetSpawnedOnMap(), numPlayers, FullRespawn, FrenzyTotemActive);
		if (num <= 0)
		{
			return new List<SpawnPoint>();
		}
		List<SpawnPoint> list = new List<SpawnPoint>(monsterSpawn);
		if (mIsRandomRefresh)
		{
			Shuffle(list);
		}
		List<SpawnPoint> list2 = new List<SpawnPoint>();
		int num2 = 0;
		long nowMs = Clock();
		foreach (SpawnPoint item in list)
		{
			if (item.ShouldSpawn(nowMs))
			{
				list2.Add(item);
				if (++num2 >= num)
				{
					break;
				}
			}
		}
		return list2;
	}

	public void CloseSpawnPoints()
	{
		foreach (SpawnPoint item in monsterSpawn)
		{
			item.SetDenySpawn(v: true);
		}
	}

	public void RestoreSpawnPoints()
	{
		foreach (SpawnPoint item in monsterSpawn)
		{
			item.SetDenySpawn(v: false);
		}
	}

	private static double GetCurrentSpawnRate(int numPlayers)
	{
		return 0.7 + 0.05 * (double)Math.Min(6, numPlayers);
	}

	private static int CalculateNumShouldSpawn(int spawnPointCount, int spawnedMonsterCount, int numPlayers, bool fullRespawn, bool frenzyTotemActive)
	{
		return ((fullRespawn | frenzyTotemActive) ? spawnPointCount : ((int)Math.Ceiling(GetCurrentSpawnRate(numPlayers) * (double)spawnPointCount))) - spawnedMonsterCount;
	}

	private static void Shuffle(List<SpawnPoint> list)
	{
		Random random = new Random();
		int count = list.Count;
		while (count > 1)
		{
			int num = random.Next(count--);
			int index = count;
			int index2 = num;
			SpawnPoint spawnPoint = list[num];
			SpawnPoint spawnPoint2 = list[count];
			SpawnPoint spawnPoint3 = (list[index] = spawnPoint);
			spawnPoint3 = (list[index2] = spawnPoint2);
		}
	}
}
