using UnityEngine;

public sealed class NativeDamageLines : MonoBehaviour
{
	private Component target;

	private DamageResult[] hits;

	private bool[] displayed;

	private int next;

	private int interval;

	private int ordinarySkill = -1;

	private double started;

	private FieldDirector field;

	private FieldMap map;

	public static void Show(Component target, DamageResult[] values, bool indexed, int interval, bool total = false, int ordinarySkill = -1)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (values.Length == 0)
		{
			return;
		}
		NativeDamageLines nativeDamageLines = new GameObject("NativeDamageLines").AddComponent<NativeDamageLines>();
		nativeDamageLines.target = target;
		nativeDamageLines.interval = interval;
		nativeDamageLines.ordinarySkill = ordinarySkill;
		nativeDamageLines.field = Object.FindObjectOfType<FieldDirector>();
		nativeDamageLines.map = (((Object)(object)nativeDamageLines.field != (Object)null) ? nativeDamageLines.field.Map : null);
		nativeDamageLines.started = Time.realtimeSinceStartupAsDouble;
		if (!total)
		{
			nativeDamageLines.hits = (DamageResult[])values.Clone();
		}
		else
		{
			long num = 0L;
			for (int i = 0; i < values.Length; i++)
			{
				DamageResult damageResult = values[i];
				num += damageResult.Amount;
			}
			nativeDamageLines.hits = new DamageResult[1]
			{
				new DamageResult(num, critical: false)
			};
		}
		nativeDamageLines.displayed = new bool[nativeDamageLines.hits.Length];
		nativeDamageLines.Tick();
	}

	private static void Delete(GameObject value)
	{
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)value);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)value);
		}
	}

	private void Update()
	{
		Tick();
	}

	public static int OrdinaryLineDelay(int skill, int line)
	{
		switch (skill)
		{
		case 5101004:
		case 5121007:
		case 15101003:
		case 15111004:
			return 0;
		case 22171002:
			return line switch
			{
				3 => 630, 
				2 => 270, 
				1 => 90, 
				0 => 0, 
				_ => 0, 
			};
		case 3111006:
		case 3211006:
		case 13111001:
			return 60 * line;
		default:
			return 120 * line;
		}
	}

	private void Tick()
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null || ((Object)(object)field != (Object)null && field.Map != map))
		{
			Delete(((Component)this).gameObject);
			return;
		}
		for (int i = 0; i < hits.Length; i++)
		{
			if (!displayed[i] && !(Time.realtimeSinceStartupAsDouble < started + (double)((ordinarySkill >= 0) ? OrdinaryLineDelay(ordinarySkill, i) : (i * interval)) / 1000.0))
			{
				displayed[i] = true;
				next++;
				Vector3 val = ((target is EnemyUnit enemyUnit) ? enemyUnit.DamageAnchor : ((BossUnit)(object)target).DamageAnchor);
				val += Vector3.up * ((float)(15 + 30 * i) / 40f);
				DamageResult damageResult = hits[i];
				if (damageResult.Amount == 0L)
				{
					DamageText.Miss(val);
				}
				else
				{
					DamageText.Show(val, damageResult.Amount, damageResult.Critical ? "cri" : "red");
				}
			}
		}
		if (next == hits.Length)
		{
			Delete(((Component)this).gameObject);
		}
	}
}
