using System.Collections.Generic;
using UnityEngine;

public sealed class NativeMobDebuffs : MonoBehaviour
{
	private sealed class Disease
	{
		public int x;

		public int y;

		public long until;

		public long nextTick;
	}

	private readonly Dictionary<int, Disease> active = new Dictionary<int, Disease>();

	private PlayerCtrl player;

	public int ForcedWalkSpeed
	{
		get
		{
			if (!Has(126))
			{
				return 0;
			}
			return active[126].x;
		}
	}

	public int ResistancePercent
	{
		get
		{
			if ((Object)(object)player == (Object)null)
			{
				return 0;
			}
			int num = 0;
			Dictionary<string, int> dictionary = player.Attributes?.setItemBonus;
			if (dictionary != null && dictionary.TryGetValue("ASR", out var value))
			{
				num += value;
			}
			NativeBuffs component = ((Component)player).GetComponent<NativeBuffs>();
			if ((Object)(object)component != (Object)null)
			{
				num += component.Value("ASR");
			}
			return Mathf.Clamp(num, 0, 100);
		}
	}

	public bool BlocksAttack
	{
		get
		{
			if (!Has(123))
			{
				return Has(128);
			}
			return true;
		}
	}

	public bool BlocksSkill
	{
		get
		{
			if (!Has(120))
			{
				return BlocksAttack;
			}
			return true;
		}
	}

	public static NativeMobDebuffs For(PlayerCtrl player)
	{
		NativeMobDebuffs nativeMobDebuffs = ((Component)player).GetComponent<NativeMobDebuffs>() ?? ((Component)player).gameObject.AddComponent<NativeMobDebuffs>();
		nativeMobDebuffs.player = player;
		return nativeMobDebuffs;
	}

	public bool Has(int id)
	{
		if (active.TryGetValue(id, out var value))
		{
			return NativeMobController.Now < value.until;
		}
		return false;
	}

	public void Apply(int id, int x, int y, long duration, bool region = false)
	{
		if (Has(id) || player.hp <= 0f)
		{
			return;
		}
		int resistancePercent = ResistancePercent;
		if (resistancePercent > 0 && Random.value * 100f < (float)resistancePercent)
		{
			return;
		}
		if (region)
		{
			NativeBuffs component = ((Component)player).GetComponent<NativeBuffs>();
			if (component != null && component.HasSource(2321005))
			{
				return;
			}
		}
		if (id != 123 && id != 128)
		{
			NativeBuffs component2 = ((Component)player).GetComponent<NativeBuffs>();
			if (component2 != null && component2.HasSource(2321005))
			{
				return;
			}
		}
		int num = 0;
		foreach (Disease value in active.Values)
		{
			if (NativeMobController.Now < value.until)
			{
				num++;
			}
		}
		if (num < 2)
		{
			active[id] = new Disease
			{
				x = x,
				y = y,
				until = NativeMobController.Now + duration,
				nextTick = NativeMobController.Now + 1000
			};
		}
	}

	public void Clear()
	{
		active.Clear();
	}

	public void Dispel()
	{
		int[] array = new int[6] { 124, 121, 125, 120, 122, 126 };
		foreach (int key in array)
		{
			active.Remove(key);
		}
	}

	public void Purge()
	{
		Dispel();
		active.Remove(128);
		active.Remove(133);
		active.Remove(132);
	}

	public void Clear(int id)
	{
		active.Remove(id);
	}

	public void Input(ref float horizontal, ref bool jump, ref float vertical)
	{
		if (Has(132))
		{
			horizontal = 0f - horizontal;
			vertical = 0f - vertical;
		}
		if (Has(122))
		{
			jump = false;
		}
		if (Has(123))
		{
			horizontal = (vertical = 0f);
			jump = false;
		}
		if (Has(128))
		{
			int x = active[128].x;
			horizontal = x switch
			{
				2 => 1, 
				1 => -1, 
				_ => 0, 
			};
			jump = x == 3;
			vertical = 0f;
		}
	}

	public float Healing(float amount, bool skill)
	{
		if (!Has(133))
		{
			return amount;
		}
		if (!skill)
		{
			return amount / 2f;
		}
		return 0f - amount;
	}

	private void Update()
	{
		if (!((Object)(object)player == (Object)null) && !(player.hp <= 0f) && !PlayerCtrl.InputLocked && !(Time.deltaTime <= 0f) && Has(125))
		{
			Disease disease = active[125];
			if (NativeMobController.Now >= disease.nextTick)
			{
				disease.nextTick = NativeMobController.Now + 1000;
				player.TakeBossDamage(Mathf.Min(Mathf.Max(0f, player.hp - 1f), (float)Mathf.Max(1, disease.x)));
			}
		}
	}
}
