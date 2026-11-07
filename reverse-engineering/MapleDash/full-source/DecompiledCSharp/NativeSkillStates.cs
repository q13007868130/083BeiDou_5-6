using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NativeSkillStates : MonoBehaviour
{
	private static Dictionary<string, object> table;

	private readonly Dictionary<int, double> until = new Dictionary<int, double>();

	private readonly Dictionary<int, double> started = new Dictionary<int, double>();

	private static Dictionary<string, object> States
	{
		get
		{
			if (table == null)
			{
				TextAsset val = Resources.Load<TextAsset>("NativeSkillStates");
				string text = ((val != null) ? val.text : null);
				table = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
			}
			if (!table.TryGetValue("states", out var value))
			{
				return null;
			}
			return value as Dictionary<string, object>;
		}
	}

	public static Dictionary<string, object> Spec(int id)
	{
		Dictionary<string, object> states = States;
		if (states == null || !states.TryGetValue(id.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	public static bool IsState(int id)
	{
		return Spec(id) != null;
	}

	public static bool IsTimedEffect(int id)
	{
		return Int(id, "timedEffect") != 0;
	}

	public static bool IsDeathGuard(int id)
	{
		return Int(id, "deathGuard") != 0;
	}

	public static int Int(int id, string key, int fallback = 0)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue(key, out var value))
		{
			return fallback;
		}
		if (value is bool)
		{
			if (!(bool)value)
			{
				return 0;
			}
			return 1;
		}
		return Convert.ToInt32(value);
	}

	public static int[] Ints(int id, string key)
	{
		Dictionary<string, object> dictionary = Spec(id);
		if (dictionary == null || !dictionary.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return new int[0];
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = Convert.ToInt32(list[i]);
		}
		return array;
	}

	public static int[] AllIds()
	{
		Dictionary<string, object> states = States;
		if (states == null)
		{
			return new int[0];
		}
		List<int> list = new List<int>();
		foreach (string key in states.Keys)
		{
			if (int.TryParse(key, out var result))
			{
				list.Add(result);
			}
		}
		return list.ToArray();
	}

	public static NativeSkillStates For(PlayerCtrl player)
	{
		return ((Component)player).GetComponent<NativeSkillStates>() ?? ((Component)player).gameObject.AddComponent<NativeSkillStates>();
	}

	public void Activate(int parentId, int level)
	{
		Dictionary<string, object> states = States;
		if (states == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in states)
		{
			if (int.TryParse(item.Key, out var result) && Int(result, "parent") == parentId)
			{
				ActivateState(result, level);
			}
		}
	}

	public void ActivateState(int stateId, int level, float secondsOverride = 0f)
	{
		if (!IsState(stateId))
		{
			return;
		}
		float num = ((secondsOverride > 0f) ? secondsOverride : ((float)Int(stateId, "time")));
		if (num <= 0f)
		{
			return;
		}
		double now = NativeBuffs.Now;
		if (!started.ContainsKey(stateId) || !Has(stateId))
		{
			started[stateId] = now;
		}
		until[stateId] = now + (double)num;
		if (IsTimedEffect(stateId))
		{
			PlayerCtrl component = ((Component)this).GetComponent<PlayerCtrl>();
			if ((Object)(object)component != (Object)null)
			{
				NativeBuffs.For(component).ApplySkill(stateId, level, NativeBuffs.Now);
			}
		}
	}

	public bool Has(int stateId)
	{
		if (until.TryGetValue(stateId, out var value))
		{
			return NativeBuffs.Now < value;
		}
		return false;
	}

	public void Consume(int stateId)
	{
		until.Remove(stateId);
		started.Remove(stateId);
	}

	public double Since(int stateId)
	{
		if (!started.TryGetValue(stateId, out var value))
		{
			return 0.0;
		}
		return NativeBuffs.Now - value;
	}

	private void Update()
	{
		if (until.Count == 0)
		{
			return;
		}
		double now = NativeBuffs.Now;
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, double> item in until)
		{
			if (now >= item.Value)
			{
				list.Add(item.Key);
			}
		}
		foreach (int item2 in list)
		{
			until.Remove(item2);
			started.Remove(item2);
		}
	}

	public void Clear()
	{
		until.Clear();
		started.Clear();
	}
}
