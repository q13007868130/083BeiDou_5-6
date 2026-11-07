using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class ExpTable
{
	private static long[] exp;

	private static int maxLevel = 300;

	public static bool Ready { get; private set; }

	public static int MaxLevel
	{
		get
		{
			Ensure();
			return maxLevel;
		}
	}

	public static int Count
	{
		get
		{
			Ensure();
			if (exp == null)
			{
				return 0;
			}
			return exp.Length;
		}
	}

	public static void Ensure()
	{
		if (Ready)
		{
			return;
		}
		Ready = true;
		TextAsset val = Resources.Load<TextAsset>("Field/ExpTable");
		if ((Object)(object)val == (Object)null)
		{
			Debug.LogError((object)"[Exp] 缺少 Resources/Field/ExpTable.json");
			exp = ExpCurve.Build(new long[3] { 0L, 15L, 34L });
			return;
		}
		if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary))
		{
			Debug.LogError((object)"[Exp] ExpTable.json 解析失败");
			exp = ExpCurve.Build(new long[3] { 0L, 15L, 34L });
			return;
		}
		maxLevel = Mathf.Clamp((int)Art.Num(dictionary, "maxLevel", 300f), 1, 300);
		List<object> list = (dictionary.ContainsKey("exp") ? (dictionary["exp"] as List<object>) : null);
		if (list == null || list.Count == 0)
		{
			Debug.LogError((object)"[Exp] ExpTable.json 没有 exp 数组");
			exp = ExpCurve.Build(new long[3] { 0L, 15L, 34L });
		}
		else
		{
			long[] array = new long[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				array[i] = Convert.ToInt64(Math.Round(Convert.ToDouble(list[i], CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
			}
			exp = ExpCurve.Build(array);
		}
	}

	public static long Needed(int level)
	{
		Ensure();
		return ExpCurve.Needed(exp, level);
	}
}
