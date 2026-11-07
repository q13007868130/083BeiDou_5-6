using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeShields : MonoBehaviour
{
	private sealed class Slot
	{
		public int Id;

		public int Level;

		public string Mode;

		public double Until;

		public double NextDecay;

		public float Amount;

		public float Cap;

		public float Initial;

		public float DecayPrimaryPct;

		public float DecaySecondaryPct;

		public int Elements;

		public int ElementsMax;

		public int Cost;

		public float StepAmount;
	}

	public struct Entry
	{
		public int Id;

		public int Level;

		public string Mode;

		public float Value;

		public float Max;

		public int RemainMs;

		public bool Counted => Mode == "elements";
	}

	private static Dictionary<string, object> contract;

	private readonly List<Slot> slots = new List<Slot>();

	public float Remaining
	{
		get
		{
			float num = 0f;
			foreach (Slot slot in slots)
			{
				num += ((slot.Mode == "elements") ? ((float)slot.Elements * slot.StepAmount) : slot.Amount);
			}
			return num;
		}
	}

	private static Dictionary<string, object> Section(string name)
	{
		if (contract == null)
		{
			TextAsset val = Resources.Load<TextAsset>("NativeShieldContract");
			string text = ((val != null) ? val.text : null);
			contract = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
		}
		if (!contract.TryGetValue(name, out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	private static Dictionary<string, object> Spec(int id)
	{
		Dictionary<string, object> dictionary = Section("shields");
		if (dictionary == null || !dictionary.TryGetValue(id.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	public static bool IsInvincible(int id)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary != null)
		{
			return (string)dictionary["mode"] == "invincible";
		}
		return false;
	}

	private static int At(Dictionary<string, object> spec, string key, int level, int fallback = 0)
	{
		if (spec == null || !spec.TryGetValue(key, out var value))
		{
			return fallback;
		}
		if (!(value is List<object> { Count: not 0 } list))
		{
			return fallback;
		}
		return Convert.ToInt32(list[Mathf.Clamp(level - 1, 0, list.Count - 1)]);
	}

	public static NativeShields For(PlayerCtrl player)
	{
		return ((Component)player).GetComponent<NativeShields>() ?? ((Component)player).gameObject.AddComponent<NativeShields>();
	}

	public void Snapshot(List<Entry> into)
	{
		if (into == null)
		{
			return;
		}
		into.Clear();
		double now = NativeBuffs.Now;
		foreach (Slot slot in slots)
		{
			if (!(now >= slot.Until))
			{
				into.Add(new Entry
				{
					Id = slot.Id,
					Level = slot.Level,
					Mode = slot.Mode,
					Value = ((slot.Mode == "elements") ? ((float)slot.Elements) : slot.Amount),
					Max = ((slot.Mode == "stack") ? slot.Cap : ((slot.Mode == "elements") ? ((float)slot.ElementsMax) : slot.Initial)),
					RemainMs = (int)Math.Min(2147483647.0, Math.Max(0.0, Math.Ceiling((slot.Until - now) * 1000.0)))
				});
			}
		}
	}

	public static void Snapshot(PlayerCtrl player, List<Entry> into)
	{
		NativeShields nativeShields = (((Object)(object)player != (Object)null) ? ((Component)player).GetComponent<NativeShields>() : null);
		if ((Object)(object)nativeShields == (Object)null)
		{
			into?.Clear();
		}
		else
		{
			nativeShields.Snapshot(into);
		}
	}

	private Slot Find(int id)
	{
		foreach (Slot slot in slots)
		{
			if (slot.Id == id)
			{
				return slot;
			}
		}
		return null;
	}

	public bool Activate(int id, int level, float maxHp)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || maxHp <= 0f)
		{
			return false;
		}
		string text = (string)dictionary["mode"];
		if (text == "invincible")
		{
			return false;
		}
		double now = NativeBuffs.Now;
		Slot slot = Find(id);
		if (slot != null && text != "stack")
		{
			slots.Remove(slot);
		}
		slot = Find(id) ?? new Slot
		{
			Id = id
		};
		slot.Level = level;
		slot.Mode = text;
		slot.Until = now + (double)At(dictionary, "time", level, 1);
		slot.NextDecay = now + 1.0;
		switch (text)
		{
		case "pool":
			slot.Amount = maxHp * (float)At(dictionary, "amountPct", level) / 100f;
			break;
		case "stack":
		{
			slot.Cap = maxHp * (float)At(dictionary, "capPct", level) / 100f;
			float num = maxHp * (float)At(dictionary, "gainPct", level) / 100f;
			slot.Amount = Mathf.Min(slot.Cap, slot.Amount + num);
			slot.DecayPrimaryPct = At(dictionary, "decayPrimaryPct", level);
			slot.DecaySecondaryPct = At(dictionary, "decaySecondaryPct", level);
			break;
		}
		case "elements":
			slot.Elements = At(dictionary, "elements", level);
			slot.ElementsMax = slot.Elements;
			slot.Cost = Mathf.Max(1, At(dictionary, "cost", level, 1));
			slot.StepAmount = maxHp * (float)At(dictionary, "stepPct", level, 1) / 100f;
			break;
		default:
			return false;
		}
		if (text == "pool")
		{
			slot.Initial = Mathf.Max(1f, slot.Amount);
		}
		if (slot.Amount <= 0f && slot.Elements <= 0)
		{
			return false;
		}
		if (Find(id) == null)
		{
			slots.Add(slot);
		}
		return true;
	}

	public float Absorb(ref float damage)
	{
		if (damage <= 0f)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = slots.Count - 1;
		while (num2 >= 0 && damage > 0f)
		{
			Slot slot = slots[num2];
			if (slot.Mode == "elements")
			{
				if (slot.StepAmount <= 0f)
				{
					slots.RemoveAt(num2);
				}
				else
				{
					int num3 = Mathf.CeilToInt(damage / slot.StepAmount) * slot.Cost;
					if (slot.Elements >= num3)
					{
						slot.Elements -= num3;
						num += damage;
						damage = 0f;
					}
					else
					{
						float num4 = (float)slot.Elements / (float)slot.Cost * slot.StepAmount;
						num += Mathf.Min(damage, num4);
						damage = Mathf.Max(0f, damage - num4);
						slot.Elements = 0;
					}
					if (slot.Elements <= 0)
					{
						slots.RemoveAt(num2);
					}
				}
			}
			else
			{
				float num5 = Mathf.Min(slot.Amount, damage);
				slot.Amount -= num5;
				num += num5;
				damage -= num5;
				if (slot.Amount <= 0f)
				{
					slots.RemoveAt(num2);
				}
			}
			num2--;
		}
		return num;
	}

	public static void OnSkillHit(PlayerCtrl player, float maxHp)
	{
		if ((Object)(object)player == (Object)null || maxHp <= 0f)
		{
			return;
		}
		Dictionary<string, object> dictionary = Section("shields");
		if (dictionary == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (item.Value is Dictionary<string, object> dictionary2 && dictionary2.TryGetValue("mode", out var value) && !((string)value != "stack") && int.TryParse(item.Key, out var result))
			{
				int num = SkillProgress.LevelOf(result);
				if (num > 0)
				{
					For(player).Activate(result, num, maxHp);
				}
			}
		}
	}

	private void Update()
	{
		if (slots.Count == 0)
		{
			return;
		}
		double now = NativeBuffs.Now;
		for (int num = slots.Count - 1; num >= 0; num--)
		{
			Slot slot = slots[num];
			if (now >= slot.Until)
			{
				slots.RemoveAt(num);
			}
			else if (!(slot.Mode != "stack") && !(now < slot.NextDecay))
			{
				slot.NextDecay = now + 1.0;
				float num2 = (slot.DecayPrimaryPct + slot.DecaySecondaryPct) / 100f;
				slot.Amount -= slot.Amount * num2;
				if (slot.Amount <= 0f)
				{
					slots.RemoveAt(num);
				}
			}
		}
	}

	public void Clear()
	{
		slots.Clear();
	}

	public static void BuffInvincible(Dictionary<string, int> stats, int id)
	{
		if (stats != null && IsInvincible(id))
		{
			stats["INVINCIBLE"] = 100;
		}
	}
}
