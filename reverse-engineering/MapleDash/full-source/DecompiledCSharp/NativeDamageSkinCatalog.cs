using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class NativeDamageSkinCatalog
{
	private static Dictionary<string, object> catalog;

	private static Dictionary<string, object> Catalog
	{
		get
		{
			if (catalog == null)
			{
				TextAsset val = Resources.Load<TextAsset>("DamageSkinCatalog");
				if ((Object)(object)val == (Object)null)
				{
					throw new InvalidOperationException("Missing DamageSkinCatalog");
				}
				catalog = (Dictionary<string, object>)MiniJson.Parse(val.text);
			}
			return catalog;
		}
	}

	public static int Selected
	{
		get
		{
			int num = GameSession.Character?.record.damageSkinId ?? 0;
			if (!Contains(num))
			{
				return 0;
			}
			return num;
		}
	}

	public static bool Contains(int id)
	{
		return Catalog.ContainsKey(id.ToString(CultureInfo.InvariantCulture));
	}

	public static string Name(int id)
	{
		if (!Catalog.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value))
		{
			return "未知伤害皮肤";
		}
		return (string)value;
	}

	public static int[] Ids()
	{
		List<int> list = new List<int>();
		foreach (string key in Catalog.Keys)
		{
			list.Add(int.Parse(key, CultureInfo.InvariantCulture));
		}
		list.Sort();
		return list.ToArray();
	}

	public static bool Select(int id)
	{
		if (!GameSession.InGame || GameSession.Character == null || !Contains(id))
		{
			return false;
		}
		int damageSkinId = GameSession.Character.record.damageSkinId;
		GameSession.Character.record.damageSkinId = id;
		try
		{
			GameSession.SaveToDb();
		}
		catch (Exception ex)
		{
			GameSession.Character.record.damageSkinId = damageSkinId;
			Debug.LogError((object)("Unable to save damage skin " + id + ": " + ex));
			return false;
		}
		NativeDamageNumber.ResetSkinFailure();
		return true;
	}
}
