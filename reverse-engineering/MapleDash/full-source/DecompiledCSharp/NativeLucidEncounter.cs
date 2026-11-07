using System;
using UnityEngine;

public sealed class NativeLucidEncounter
{
	private readonly NativeBossEncounter owner;

	private readonly FieldDirector field;

	private readonly EnemyUnit boss;

	private readonly int phase;

	private long butterfly;

	private long dust;

	private long summon;

	private long dragon;

	private long laser;

	private long shoot;

	private long rush;

	private long glass;

	private long hurdle;

	private long bomb;

	private long fury;

	private int gauge;

	private int positionIndex;

	private int glassIndex;

	private bool failed;

	private bool stopped;

	private static readonly int[] Support = new int[5] { 8880164, 8880161, 8880165, 8880171, 8880175 };

	private long Now => NativeMobController.Now;

	public NativeLucidEncounter(NativeBossEncounter owner, FieldDirector field, EnemyUnit boss)
	{
		this.owner = owner;
		this.field = field;
		this.boss = boss;
		phase = ((boss.fieldMobId == 8880140) ? 1 : ((boss.fieldMobId == 8880141) ? 2 : 3));
		butterfly = Now + 5000;
		dust = Now + 9000;
		summon = Now + 7000;
		dragon = Now + ((phase == 1) ? 20000 : 28000);
		laser = Now + 12000;
		shoot = Now + 25000;
		rush = Now + 18000;
		glass = Now + 10000;
		hurdle = Now + 1000;
		bomb = Now + ((phase == 1) ? 12000 : 15000);
		fury = Now + 45000;
		if (phase == 3)
		{
			int[] support = Support;
			foreach (int id in support)
			{
				owner.Remove(id);
			}
			owner.Effect("customSkill/lucid/furyVideoLayer");
			BottomBar.Toast("露希德已暴怒：45秒后噩梦迷雾将降临");
		}
	}

	private void Schedule(int delay, Action action)
	{
		owner.Schedule(boss, delay, () =>
		{
			if (!stopped)
			{
				action();
			}
		});
	}

	private void Hit(int delay, int percent, Nullable range = null)
	{
		Schedule(delay, () =>
		{
			owner.Damage(boss, percent, range);
		});
	}

	public void OnKilled(EnemyUnit e)
	{
		if (!((Object)(object)e != (Object)(object)boss) && !failed)
		{
			stopped = true;
			if (phase == 3)
			{
				owner.Effect("customSkill/lucid/furyStopVideoLayer");
			}
		}
	}

	public void Tick()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		if (stopped || failed || (Object)(object)boss == (Object)null || boss.dead)
		{
			return;
		}
		if (phase == 3)
		{
			if (Now >= fury)
			{
				FailFury();
			}
			return;
		}
		if (Now >= butterfly)
		{
			Butterflies();
		}
		if (Now >= dust)
		{
			Vector2 val = NativeMobController.Pixels(((Component)boss).transform.position);
			owner.Damage(boss, 10, new Rect(val.x - 600f, val.y - 500f, 1200f, 650f));
			dust = Now + ((phase == 1) ? 12000 : 10000);
		}
		if (Now >= summon)
		{
			Nightmares();
			summon = Now + ((phase == 1) ? 15000 : 18000);
		}
		if (Now >= dragon)
		{
			owner.Effect("customSkill/lucid/dragonP" + phase + "VideoLayer");
			Hit(6300, 100);
			dragon = Now + ((phase == 1) ? 35000 : 38000);
		}
		if (Now >= bomb)
		{
			owner.Effect("customSkill/lucid/bombVideoLayer");
			Schedule(3000, () =>
			{
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0034: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_0048: Unknown result type (might be due to invalid IL or missing references)
				//IL_0054: Unknown result type (might be due to invalid IL or missing references)
				//IL_006a: Unknown result type (might be due to invalid IL or missing references)
				PlayerCtrl playerCtrl2 = GameLoop.I?.player;
				if (!((Object)(object)playerCtrl2 == (Object)null) && !(playerCtrl2.hp <= 0f))
				{
					Vector2 val3 = NativeMobController.Pixels(Vector2.op_Implicit(playerCtrl2.FeetPos));
					owner.Damage(boss, 35, new Rect(val3.x - 250f, val3.y - 250f, 500f, 500f));
				}
			});
			bomb = Now + 25000;
		}
		if (Now >= hurdle)
		{
			foreach (EnemyUnit item in EnemyUnit.All)
			{
				if (item.fieldMode && !item.dead && item.fieldMobId == ((phase == 1) ? 8880161 : 8880171))
				{
					Vector2 val2 = NativeMobController.Pixels(((Component)item).transform.position);
					PlayerCtrl playerCtrl = GameLoop.I?.player;
					Rect value = new Rect(val2.x - 20f, val2.y - 500f, 40f, 510f);
					if ((Object)(object)playerCtrl != (Object)null && value.Contains(NativeMobController.Pixels(Vector2.op_Implicit(playerCtrl.FeetPos))))
					{
						owner.Damage(boss, 20, value);
						break;
					}
				}
			}
			hurdle = Now + 1000;
		}
		if (phase != 2)
		{
			return;
		}
		if (Now >= laser)
		{
			owner.Effect("customSkill/lucid/laserRainVideoLayer");
			Hit(1260, 18);
			Hit(3000, 18);
			Schedule(3000, () =>
			{
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				if (owner.Count(8880171) < 15)
				{
					owner.Spawn(8880171, GroundPoint());
				}
			});
			laser = Now + 30000;
		}
		if (Now >= shoot)
		{
			owner.Effect("customSkill/lucid/phantomBarrageVideoLayer");
			for (int num = 0; num < 12; num++)
			{
				Hit(2400 + num * 1000, 10);
			}
			shoot = Now + 45000;
		}
		if (Now >= rush)
		{
			Rush();
			rush = Now + 20000;
		}
		if (Now < glass)
		{
			return;
		}
		int num2 = glassIndex++ % LucidTables.GlassAreas.Length;
		Rect area = LucidTables.GlassAreas[num2];
		owner.Effect("customSkill/lucid/stainedGlass" + ((num2 == 0) ? "" : num2.ToString()) + "VideoLayer");
		Hit(1260, 20, area);
		Schedule(3000, () =>
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			if (owner.Count(8880171) < 15)
			{
				owner.Spawn(8880171, new Vector2(area.center.x, area.y));
			}
		});
		glass = Now + 10000;
	}

	private void Butterflies()
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		int hp = Mathf.Clamp(Mathf.CeilToInt((float)(boss.hp / boss.hpMax * 100)), 1, 100);
		int num = Math.Min(NativeBossRules.ButterflyCount(hp), 40 - gauge);
		gauge += num;
		int id = ((phase == 1) ? 8880165 : 8880175);
		Vector2[] array = ((phase == 1) ? LucidTables.ButterflyP1 : LucidTables.ButterflyP2);
		int num2 = Math.Min(num, 40 - owner.Count(id));
		for (int i = 0; i < num2; i++)
		{
			owner.Spawn(id, array[positionIndex++ % array.Length], ground: false);
		}
		if (gauge >= 40)
		{
			gauge = 0;
			owner.Remove(8880165);
			owner.Remove(8880175);
			owner.Effect("customSkill/lucid/butterflyBurstVideoLayer");
			Hit(1800, 30);
		}
		butterfly = Now + NativeBossRules.ButterflyInterval(hp);
	}

	private Vector2 GroundPoint()
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		int[] array = ((phase != 1) ? new int[6] { 0, 250, 500, 750, 1000, 1250 } : new int[6] { 220, 520, 820, 1120, 1420, 1720 });
		return new Vector2((float)array[Random.Range(0, array.Length)], (float)((phase != 1) ? 380 : 0));
	}

	private void Nightmares()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		if (phase == 1 && owner.Count(8880164) < 4)
		{
			owner.Spawn(8880164, GroundPoint());
		}
		int id = ((phase == 1) ? 8880161 : 8880171);
		int num = Math.Min((phase == 1) ? 1 : 2, 15 - owner.Count(id));
		for (int i = 0; i < num; i++)
		{
			owner.Spawn(id, GroundPoint());
		}
	}

	private void Rush()
	{
		owner.Effect("customSkill/lucid/rushVideoLayer");
		bool hit = false;
		for (int i = 0; i <= 3000; i += 100)
		{
			int time = i;
			Schedule(i, () =>
			{
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				//IL_0057: Unknown result type (might be due to invalid IL or missing references)
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0060: Unknown result type (might be due to invalid IL or missing references)
				//IL_0065: Unknown result type (might be due to invalid IL or missing references)
				//IL_006a: Unknown result type (might be due to invalid IL or missing references)
				if (!hit)
				{
					Vector2 val = RushPosition(time);
					PlayerCtrl playerCtrl = GameLoop.I?.player;
					if ((Object)(object)playerCtrl != (Object)null)
					{
						Rect val2 = new Rect(val.x - 47f, val.y - 135f, 123f, 149f);
						if (val2.Contains(NativeMobController.Pixels(Vector2.op_Implicit(playerCtrl.FeetPos))))
						{
							hit = true;
							owner.Damage(boss, 20);
						}
					}
				}
			});
		}
	}

	private static Vector2 RushPosition(int elapsed)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		for (int i = 1; i < LucidTables.RushPath.Length; i++)
		{
			num += Vector2.Distance(LucidTables.RushPath[i - 1], LucidTables.RushPath[i]) / (float)LucidTables.RushSpeeds[i];
		}
		float num2 = (float)Mathf.Clamp(elapsed, 0, 3000) * num / 3000f;
		float num3 = 0f;
		for (int j = 1; j < LucidTables.RushPath.Length; j++)
		{
			float num4 = Vector2.Distance(LucidTables.RushPath[j - 1], LucidTables.RushPath[j]) / (float)LucidTables.RushSpeeds[j];
			if (num2 <= num3 + num4 || j == LucidTables.RushPath.Length - 1)
			{
				return Vector2.Lerp(LucidTables.RushPath[j - 1], LucidTables.RushPath[j], (num4 == 0f) ? 1f : ((num2 - num3) / num4));
			}
			num3 += num4;
		}
		return LucidTables.RushPath[LucidTables.RushPath.Length - 1];
	}

	private void FailFury()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		failed = true;
		Vector3 position = ((Component)boss).transform.position;
		owner.Remove(8880152);
		EnemyUnit fail = field.SpawnNativeMob(8880154, position, ground: false, floating: true);
		fail?.NativeController?.BlockFor(10000);
		owner.Schedule(fail, 3000L, () =>
		{
			owner.Damage(fail, 100);
		});
		owner.Schedule(fail, 10000L, () =>
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			owner.Remove(8880154);
			EnemyUnit enemyUnit = owner.Spawn(8880141, new Vector2(711f, -786f), ground: false);
			if ((Object)(object)enemyUnit != (Object)null)
			{
				enemyUnit.NativeController?.SetHpPercent(16);
			}
		});
	}
}
