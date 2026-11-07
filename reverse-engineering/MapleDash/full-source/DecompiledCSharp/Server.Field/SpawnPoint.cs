using System;

namespace Server.Field;

public sealed class SpawnPoint
{
	private readonly int monster;

	private readonly int mobTime;

	private readonly int team;

	private readonly int fh;

	private readonly int f;

	private readonly float posX;

	private readonly float posY;

	private readonly bool immobile;

	private long nextPossibleSpawn;

	private int mobInterval = 5000;

	private double respawnTimeMultiplier = 1.0;

	private int spawnedMonsters;

	private bool denySpawn;

	public SpawnPointDef Def { get; }

	public int MobId => monster;

	public int Team => team;

	public float PosX => posX;

	public float PosY => posY;

	public int MobTime => mobTime;

	public int Spawned => spawnedMonsters;

	public SpawnPoint(SpawnPointDef def, int mobIntervalMs, long nowMs)
	{
		Def = def;
		monster = def.mob;
		mobTime = def.mobTime;
		team = def.team;
		fh = def.fh;
		f = def.f;
		posX = def.x;
		posY = def.y;
		immobile = def.immobile;
		mobInterval = mobIntervalMs;
		nextPossibleSpawn = nowMs;
	}

	public int GetFh()
	{
		return fh;
	}

	public int GetF()
	{
		return f;
	}

	public void SetDenySpawn(bool v)
	{
		denySpawn = v;
	}

	public bool GetDenySpawn()
	{
		return denySpawn;
	}

	public void SetRespawnTimeMultiplier(double m)
	{
		if (m <= 0.0)
		{
			throw new ArgumentException("respawn multiplier must be positive");
		}
		respawnTimeMultiplier = m;
	}

	private long ScaleRespawnDelay(long delay)
	{
		return Math.Max(0L, (long)Math.Round((double)delay * respawnTimeMultiplier));
	}

	public bool ShouldSpawn(long nowMs)
	{
		if (denySpawn || mobTime < 0 || spawnedMonsters > 0)
		{
			return false;
		}
		return nextPossibleSpawn <= nowMs;
	}

	public bool ShouldForceSpawn()
	{
		if (mobTime >= 0)
		{
			return spawnedMonsters <= 0;
		}
		return false;
	}

	public void MarkSpawned(long nowMs)
	{
		spawnedMonsters++;
		if (mobTime == 0)
		{
			nextPossibleSpawn = nowMs + ScaleRespawnDelay(mobInterval);
		}
	}

	public void OnKilled(long aniTimeMs, long nowMs)
	{
		nextPossibleSpawn = nowMs;
		if (mobTime > 0)
		{
			nextPossibleSpawn += ScaleRespawnDelay((long)mobTime * 1000L);
		}
		else
		{
			nextPossibleSpawn += ScaleRespawnDelay(aniTimeMs);
		}
		if (spawnedMonsters > 0)
		{
			spawnedMonsters--;
		}
	}

	public SpawnPoint Duplicate(long nowMs)
	{
		return new SpawnPoint(Def, mobInterval, nowMs)
		{
			denySpawn = denySpawn,
			respawnTimeMultiplier = respawnTimeMultiplier
		};
	}
}
