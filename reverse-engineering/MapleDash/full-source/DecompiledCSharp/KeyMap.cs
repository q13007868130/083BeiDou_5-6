using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class KeyMap
{
	private static readonly Dictionary<GameAction, KeyCode> defaults = BuildDefaults();

	private static readonly Dictionary<GameAction, KeyCode> bind = new Dictionary<GameAction, KeyCode>();

	private static bool loaded;

	private static readonly List<KeymapEntry> unmapped = new List<KeymapEntry>();

	public static IEnumerable<KeyValuePair<GameAction, KeyCode>> All
	{
		get
		{
			Ensure();
			return bind;
		}
	}

	private static Dictionary<GameAction, KeyCode> BuildDefaults()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<GameAction, KeyCode> dictionary = new Dictionary<GameAction, KeyCode>
		{
			{
				GameAction.Left,
				(KeyCode)276
			},
			{
				GameAction.Right,
				(KeyCode)275
			},
			{
				GameAction.Up,
				(KeyCode)273
			},
			{
				GameAction.Down,
				(KeyCode)274
			},
			{
				GameAction.Menu,
				(KeyCode)27
			},
			{
				GameAction.QuickSlot,
				(KeyCode)0
			},
			{
				GameAction.Help,
				(KeyCode)0
			}
		};
		NativeKeyActions.Entry[] entries = NativeKeyActions.Entries;
		foreach (NativeKeyActions.Entry entry in entries)
		{
			dictionary[entry.action] = (KeyCode)0;
		}
		for (int j = 0; j < KeymapDao.DefaultKeys.Length; j++)
		{
			if (NativeKeyActions.Find(KeymapDao.DefaultTypes[j], KeymapDao.DefaultActions[j], out var action))
			{
				dictionary[action] = NativeKeyLayout.FromScanCode(KeymapDao.DefaultKeys[j]);
			}
		}
		for (int k = 0; k < 26; k++)
		{
			dictionary[QuickSlots.Action(k)] = (KeyCode)0;
		}
		return dictionary;
	}

	public static void Ensure()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected I4, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		if (loaded)
		{
			return;
		}
		loaded = true;
		for (int i = 8; i < 26; i++)
		{
			defaults[QuickSlots.Action(i)] = (KeyCode)0;
		}
		foreach (KeyValuePair<GameAction, KeyCode> @default in defaults)
		{
			KeyCode val = (KeyCode)PlayerPrefs.GetInt("mxd.key." + @default.Key, (int)@default.Value);
			bind[@default.Key] = (Enum.IsDefined(typeof(KeyCode), val) ? NativeKeyLayout.FunctionKey(val) : @default.Value);
		}
		StringBuilder stringBuilder = new StringBuilder("[KeyMap] ");
		foreach (KeyValuePair<GameAction, KeyCode> item in bind)
		{
			if ((int)item.Value != 0)
			{
				stringBuilder.Append(item.Key).Append('=').Append(item.Value)
					.Append(' ');
			}
		}
		Debug.Log((object)stringBuilder.ToString());
	}

	public static int PetPotion(bool hp)
	{
		return unmapped.Find((KeymapEntry e) => e.key == (hp ? 91 : 92) && e.type == 7)?.action ?? 0;
	}

	public static void BindPetPotion(bool hp, int id)
	{
		int key = (hp ? 91 : 92);
		unmapped.RemoveAll((KeymapEntry e) => e.key == key && e.type == 7);
		if (id > 0)
		{
			unmapped.Add(new KeymapEntry
			{
				key = key,
				type = 7,
				action = id
			});
		}
		GameSession.SaveToDb();
	}

	public static void LoadCharacter(List<KeymapEntry> entries)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		bind.Clear();
		foreach (KeyValuePair<GameAction, KeyCode> @default in defaults)
		{
			bind[@default.Key] = @default.Value;
		}
		NativeKeyActions.Entry[] entries2 = NativeKeyActions.Entries;
		foreach (NativeKeyActions.Entry entry in entries2)
		{
			bind[entry.action] = (KeyCode)0;
		}
		for (int j = 0; j < 26; j++)
		{
			bind[QuickSlots.Action(j)] = (KeyCode)0;
		}
		int[] array = new int[26];
		int num = 0;
		unmapped.Clear();
		foreach (KeymapEntry entry2 in entries)
		{
			KeyCode val = NativeKeyLayout.FromScanCode(entry2.key);
			if ((int)val != 0 && NativeKeyActions.Find(entry2.type, entry2.action, out var action))
			{
				Claim(action, val);
			}
			else if ((int)val != 0 && (entry2.type == 1 || entry2.type == 2) && num < 26)
			{
				Claim(QuickSlots.Action(num), val);
				array[num++] = ((entry2.type == 2) ? (-entry2.action) : entry2.action);
			}
			else
			{
				unmapped.Add(entry2);
			}
		}
		QuickSlots.LoadCharacter(array);
	}

	private static void Claim(GameAction action, KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if ((int)key == 0)
		{
			bind[action] = key;
			return;
		}
		foreach (GameAction item in new List<GameAction>(bind.Keys))
		{
			if (item != action && bind[item] == key)
			{
				bind[item] = (KeyCode)0;
			}
		}
		bind[action] = key;
	}

	public static List<KeymapEntry> ExportCharacter()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		List<KeymapEntry> list = new List<KeymapEntry>();
		HashSet<int> hashSet = new HashSet<int>();
		NativeKeyActions.Entry[] entries = NativeKeyActions.Entries;
		foreach (NativeKeyActions.Entry entry in entries)
		{
			AppendKeymap(list, hashSet, entry.action);
		}
		for (int j = 0; j < 26; j++)
		{
			AppendKeymap(list, hashSet, QuickSlots.Action(j));
		}
		foreach (KeymapEntry item in unmapped)
		{
			if (item.type == 7 && (item.key == 91 || item.key == 92))
			{
				list.Add(item);
				continue;
			}
			int num = NativeKeyLayout.ToScanCode(NativeKeyLayout.FromScanCode(item.key));
			if (num >= 0 && !hashSet.Contains(num))
			{
				list.Add(item);
				hashSet.Add(num);
			}
		}
		return list;
	}

	private static void AppendKeymap(List<KeymapEntry> result, HashSet<int> occupied, GameAction action)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!bind.TryGetValue(action, out var value) || (int)value == 0)
		{
			return;
		}
		int num = NativeKeyLayout.ToScanCode(value);
		if (num >= 0 && !occupied.Contains(num))
		{
			NativeKeyActions.Entry entry = NativeKeyActions.Of(action);
			int num2 = QuickSlots.Index(action);
			if (entry != null || (num2 >= 0 && QuickSlots.Id(num2) != 0))
			{
				result.Add(new KeymapEntry
				{
					key = num,
					type = (entry?.Type ?? ((!QuickSlots.IsItem(QuickSlots.Id(num2))) ? 1 : 2)),
					action = (entry?.id ?? Math.Abs(QuickSlots.Id(num2)))
				});
				occupied.Add(num);
			}
		}
	}

	public static KeyCode Of(GameAction a)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		if (!bind.TryGetValue(a, out var value))
		{
			return (KeyCode)0;
		}
		return value;
	}

	public static void Bind(GameAction a, KeyCode k)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected I4, but got Unknown
		Ensure();
		k = NativeKeyLayout.FunctionKey(k);
		foreach (GameAction item in new List<GameAction>(bind.Keys))
		{
			if ((int)k != 0 && bind[item] == k && item != a)
			{
				bind[item] = (KeyCode)0;
				PlayerPrefs.SetInt("mxd.key." + item, 0);
			}
		}
		bind[a] = k;
		PlayerPrefs.SetInt("mxd.key." + a, (int)k);
		PlayerPrefs.Save();
	}

	public static Dictionary<GameAction, KeyCode> Defaults()
	{
		return new Dictionary<GameAction, KeyCode>(defaults);
	}

	public static void Apply(Dictionary<GameAction, KeyCode> values)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected I4, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		foreach (KeyValuePair<GameAction, KeyCode> value in values)
		{
			KeyCode val = NativeKeyLayout.FunctionKey(value.Value);
			if ((int)val != 0)
			{
				foreach (GameAction item in new List<GameAction>(bind.Keys))
				{
					if (item != value.Key && bind[item] == val)
					{
						bind[item] = (KeyCode)0;
						PlayerPrefs.SetInt("mxd.key." + item, 0);
					}
				}
			}
			bind[value.Key] = val;
			PlayerPrefs.SetInt("mxd.key." + value.Key, (int)val);
		}
		PlayerPrefs.Save();
	}

	public static void ResetAll()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected I4, but got Unknown
		Ensure();
		foreach (KeyValuePair<GameAction, KeyCode> @default in defaults)
		{
			bind[@default.Key] = NativeKeyLayout.FunctionKey(@default.Value);
			PlayerPrefs.SetInt("mxd.key." + @default.Key, (int)bind[@default.Key]);
		}
		PlayerPrefs.Save();
	}

	public static string Label(KeyCode k)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected I4, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected I4, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		if ((int)k <= 32)
		{
			if ((int)k == 0)
			{
				return "";
			}
			if ((int)k == 27)
			{
				return "Esc";
			}
			if ((int)k == 32)
			{
				return "Space";
			}
		}
		else if ((int)k <= 127)
		{
			if ((int)k == 96)
			{
				return "`";
			}
			if ((int)k == 127)
			{
				return "Del";
			}
		}
		else
		{
			switch (k - 273)
			{
			case 4:
				return "Ins";
			case 5:
				return "Home";
			case 6:
				return "End";
			case 7:
				return "PgUp";
			case 8:
				return "PgDn";
			case 0:
				return "↑";
			case 1:
				return "↓";
			case 3:
				return "←";
			case 2:
				return "→";
			}
			switch (k - 303)
			{
			case 1:
				return "Shift";
			case 0:
				return "Shift";
			case 3:
				return "Ctrl";
			case 2:
				return "Ctrl";
			case 5:
				return "Alt";
			case 4:
				return "Alt";
			}
		}
		return ((object)k/*cast due to constrained. prefix*/).ToString();
	}
}
