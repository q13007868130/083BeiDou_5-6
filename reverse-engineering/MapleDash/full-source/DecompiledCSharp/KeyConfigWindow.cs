using System.Collections.Generic;
using UnityEngine;

public static class KeyConfigWindow
{
	private static Dictionary<GameAction, KeyCode> draft;

	private static readonly GameAction[] palette = BuildPalette();

	private static KeyCode[] quickDraft;

	private static int quickPage;

	private static readonly Dictionary<int, int> skillDraft = new Dictionary<int, int>();

	private static Vector2 pos = new Vector2(-1f, -1f);

	private static Vector2 offset;

	private static bool dragging;

	private static bool quickEdit;

	private static int quickIndex;

	private static int confirmMode;

	private static GameAction? carried;

	private static bool swallow;

	private static KeyCode selected;

	public static bool Visible { get; private set; }

	private static float S => Mathf.Min(new float[3]
	{
		BottomBar.S,
		(UiKit.W - 16f) / 629f,
		(UiKit.H - 16f) / 373f
	});

	public static bool Carrying => carried.HasValue;

	public static GameAction CarriedAction
	{
		get
		{
			if (!carried.HasValue)
			{
				return GameAction.Left;
			}
			return carried.Value;
		}
	}

	private static Rect QuickRect
	{
		get
		{
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(Mathf.Clamp(pos.x + 360f * S, 0f, Mathf.Max(0f, UiKit.W - 266f * S)), Mathf.Clamp(pos.y + 220f * S, 0f, Mathf.Max(0f, UiKit.H - 238f * S)), 266f * S, 238f * S);
		}
	}

	private static GameAction[] BuildPalette()
	{
		List<GameAction> list = new List<GameAction>();
		NativeKeyActions.Entry[] entries = NativeKeyActions.Entries;
		foreach (NativeKeyActions.Entry entry in entries)
		{
			list.Add(entry.action);
		}
		return list.ToArray();
	}

	private static Rect R(float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(pos.x + x * S, pos.y + y * S, w * S, h * S);
	}

	private static Texture2D T(string name)
	{
		return Art.Tex("ui/keyconfig/" + name + ".bytes");
	}

	private static void EnsurePos()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (pos.x < 0f)
		{
			pos = new Vector2((UiKit.W - 629f * S) / 2f, Mathf.Max(8f, (UiKit.H - 373f * S) / 2f));
		}
	}

	public static void Toggle()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (Visible)
		{
			Close();
			return;
		}
		draft = new Dictionary<GameAction, KeyCode>();
		foreach (KeyValuePair<GameAction, KeyCode> item in KeyMap.All)
		{
			draft[item.Key] = item.Value;
		}
		quickDraft = QuickBarKeys.Copy();
		quickPage = 0;
		confirmMode = 0;
		Visible = true;
		carried = null;
		swallow = false;
		skillDraft.Clear();
		quickEdit = false;
		selected = (KeyCode)0;
		EnsurePos();
		InputHub.ReleaseAllVirtual();
		TouchInput.Reset();
	}

	public static void Close()
	{
		Visible = false;
		confirmMode = 0;
		carried = null;
		swallow = false;
		dragging = false;
		skillDraft.Clear();
	}

	public static bool DropSkill(Vector2 point, int skillId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		KeyCode val = HitKey(point);
		if ((int)val == 0)
		{
			return false;
		}
		if (!QuickSlots.CanBindToQuickBar(skillId, out var why))
		{
			if (!string.IsNullOrEmpty(why))
			{
				NativeUtilPrompt.Notice(why);
			}
			return true;
		}
		int num = -1;
		for (int i = 0; i < 26; i++)
		{
			GameAction key = QuickSlots.Action(i);
			if (draft[key] == val)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			for (int j = 0; j < 26; j++)
			{
				if (!skillDraft.ContainsKey(j) && (QuickSlots.Id(j) == 0 || (int)draft[QuickSlots.Action(j)] == 0))
				{
					num = j;
					break;
				}
			}
		}
		if (num < 0)
		{
			BottomBar.Toast("快捷技能位已满，请先清空一个位置");
			return true;
		}
		foreach (GameAction item in new List<GameAction>(draft.Keys))
		{
			if (draft[item] == val)
			{
				draft[item] = (KeyCode)0;
			}
		}
		draft[QuickSlots.Action(num)] = val;
		skillDraft[num] = skillId;
		return true;
	}

	public static Texture2D Icon(GameAction action)
	{
		if (QuickSlots.Index(action) >= 0)
		{
			return QuickSlots.BindingIcon(QuickSlots.Id(QuickSlots.Index(action)));
		}
		NativeKeyActions.Entry entry = NativeKeyActions.Of(action);
		if (entry == null)
		{
			return null;
		}
		return T("icon_" + entry.id);
	}

	public static bool InTray(GameAction action)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		if (draft != null)
		{
			if (draft.TryGetValue(action, out var value))
			{
				return (int)value == 0;
			}
			return true;
		}
		return false;
	}

	private static Texture2D DraftIcon(GameAction action)
	{
		int num = QuickSlots.Index(action);
		if (num >= 0 && skillDraft.TryGetValue(num, out var value))
		{
			return QuickSlots.BindingIcon(value);
		}
		return Icon(action);
	}

	private static bool Button(string name, float x, float y, int w, int h)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		Rect val = R(x, y, w, h);
		bool flag = val.Contains(Pointer.Pos);
		string text = name;
		string text2;
		if (flag)
		{
			text2 = (Pointer.Down ? "pressed" : "mouseOver");
		}
		else
		{
			text2 = "normal";
		}
		Texture2D val2 = T(text + "_" + text2);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		if ((confirmMode == 0 && !swallow && (int)Event.current.type == 7 && Pointer.Released) & flag)
		{
			if (quickEdit)
			{
				Rect quickRect = QuickRect;
				return !quickRect.Contains(Pointer.Pos);
			}
			return true;
		}
		return false;
	}

	public static KeyCode HitKey(Vector2 point)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (Visible && confirmMode == 0)
		{
			Rect val;
			if (quickEdit)
			{
				val = QuickRect;
				if (val.Contains(point))
				{
					goto IL_0025;
				}
			}
			foreach (KeyValuePair<KeyCode, Vector2> position in NativeKeyLayout.Positions)
			{
				val = R(position.Value.x, position.Value.y, 32f, 32f);
				if (val.Contains(point))
				{
					return NativeKeyLayout.FunctionKey(position.Key);
				}
			}
			return (KeyCode)0;
		}
		goto IL_0025;
		IL_0025:
		return (KeyCode)0;
	}

	private static GameAction? PickTargetAt(Vector2 point)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (draft == null || quickEdit)
		{
			return null;
		}
		KeyCode val = HitKey(point);
		if ((int)val != 0)
		{
			foreach (KeyValuePair<GameAction, KeyCode> item in draft)
			{
				if (item.Value == val && (Object)(object)DraftIcon(item.Key) != (Object)null)
				{
					return item.Key;
				}
			}
			return null;
		}
		for (int i = 0; i < palette.Length; i++)
		{
			if (InTray(palette[i]) && !((Object)(object)DraftIcon(palette[i]) == (Object)null))
			{
				Vector2 val2 = NativeKeyLayout.TrayPosition(palette[i]);
				Rect val3 = R(val2.x, val2.y, 32f, 32f);
				if (val3.Contains(point))
				{
					return palette[i];
				}
			}
		}
		return null;
	}

	public static bool PickableAt(Vector2 point)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return PickTargetAt(point).HasValue;
	}

	public static bool PickAction(GameAction action)
	{
		if (draft == null || carried.HasValue)
		{
			return false;
		}
		if (!draft.ContainsKey(action) && !InTray(action))
		{
			return false;
		}
		carried = action;
		return true;
	}

	public static bool PickAt(Vector2 point)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		GameAction? gameAction = PickTargetAt(point);
		if (!gameAction.HasValue || !PickAction(gameAction.Value))
		{
			return false;
		}
		BottomBar.Toast("拿起「" + ActionLabel(gameAction.Value) + "」：点目标键位放下，Esc 取消");
		return true;
	}

	public static bool DropOnKey(KeyCode key)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (!carried.HasValue || draft == null || (int)key == 0)
		{
			return false;
		}
		GameAction value = carried.Value;
		foreach (GameAction item in new List<GameAction>(draft.Keys))
		{
			if (draft[item] == key)
			{
				draft[item] = (KeyCode)0;
			}
		}
		draft[value] = key;
		carried = null;
		return true;
	}

	public static bool ReturnToTray()
	{
		if (!carried.HasValue || draft == null)
		{
			return false;
		}
		GameAction value = carried.Value;
		draft[value] = (KeyCode)0;
		carried = null;
		return true;
	}

	public static void CancelCarry()
	{
		carried = null;
	}

	public static bool DropAt(Vector2 point)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (!carried.HasValue)
		{
			return false;
		}
		KeyCode val = HitKey(point);
		if ((int)val != 0)
		{
			return DropOnKey(val);
		}
		Rect val2 = R(0f, 262f, 629f, 111f);
		if (val2.Contains(point))
		{
			GameAction value = carried.Value;
			if (!ReturnToTray())
			{
				return false;
			}
			BottomBar.Toast("「" + ActionLabel(value) + "」已放回托盘");
			return true;
		}
		CancelCarry();
		return false;
	}

	public static Vector2 KeyCenter(KeyCode key)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		EnsurePos();
		KeyCode val = NativeKeyLayout.FunctionKey(key);
		foreach (KeyValuePair<KeyCode, Vector2> position in NativeKeyLayout.Positions)
		{
			if (NativeKeyLayout.FunctionKey(position.Key) == val)
			{
				return new Vector2(pos.x + (position.Value.x + 16f) * S, pos.y + (position.Value.y + 16f) * S);
			}
		}
		return new Vector2(-1f, -1f);
	}

	public static KeyCode DraftKey(GameAction action)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (draft == null || !draft.TryGetValue(action, out var value))
		{
			return (KeyCode)0;
		}
		return value;
	}

	public static int DraftUsers(KeyCode key)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (draft == null || (int)key == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (KeyValuePair<GameAction, KeyCode> item in draft)
		{
			if (item.Value == key)
			{
				num++;
			}
		}
		return num;
	}

	private static string ActionLabel(GameAction action)
	{
		int num = QuickSlots.Index(action);
		if (num >= 0)
		{
			int num2 = (skillDraft.TryGetValue(num, out var value) ? value : QuickSlots.Id(num));
			if (QuickSlots.IsItem(num2))
			{
				return WzNames.Item(-num2);
			}
			SkillData skillData = SkillBook.Get(num2);
			if (skillData != null)
			{
				return skillData.name;
			}
		}
		NativeKeyActions.Entry entry = NativeKeyActions.Of(action);
		if (entry == null)
		{
			return action.ToString();
		}
		return entry.name;
	}

	private static void SetQuickKey(KeyCode code)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected I4, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between I4 and Unknown
		if ((int)code == 0)
		{
			return;
		}
		code = NativeKeyLayout.FunctionKey(code);
		for (int i = 0; i < quickDraft.Length; i++)
		{
			if (i != quickIndex && (int)quickDraft[i] == (int)code)
			{
				quickDraft[i] = quickDraft[quickIndex];
			}
		}
		quickDraft[quickIndex] = (KeyCode)(int)code;
	}

	private static void DrawQuickEdit()
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Invalid comparison between Unknown and I4
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected Obj, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Invalid comparison between Unknown and I4
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected Obj, but got Unknown
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Invalid comparison between Unknown and I4
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Invalid comparison between Unknown and I4
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(pos.x + 360f * S, 0f, Mathf.Max(0f, UiKit.W - 266f * S));
		float num2 = Mathf.Clamp(pos.y + 220f * S, 0f, Mathf.Max(0f, UiKit.H - 238f * S));
		Texture2D val = T("quickslot_backgrnd");
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(num, num2, 266f * S, 238f * S), (Texture)(object)val);
		}
		for (int i = 0; i < 8; i++)
		{
			int num3 = quickPage * 8 + i;
			if (num3 >= 26)
			{
				break;
			}
			Rect val2 = new Rect(num + (float)(64 + i % 4 * 35) * S, num2 + (float)(113 + i / 4 * 34) * S, 32f * S, 30f * S);
			GUIStyle val3 = new GUIStyle
			{
				fontSize = Mathf.RoundToInt(9f * S),
				alignment = (TextAnchor)4
			};
			val3.normal.textColor = Color.black;
			GUI.Label(val2, KeyMap.Label(quickDraft[num3]), val3);
			if ((int)Event.current.type == 7 && Pointer.Pressed && val2.Contains(Pointer.Pos))
			{
				quickIndex = num3;
			}
		}
		if ((int)Event.current.type == 4 && (int)Event.current.keyCode != 0 && (int)Event.current.keyCode != 27)
		{
			SetQuickKey(Event.current.keyCode);
			Event.current.Use();
		}
		GUIStyle val4 = new GUIStyle
		{
			fontSize = Mathf.RoundToInt(10f * S),
			alignment = (TextAnchor)4
		};
		val4.normal.textColor = Color.black;
		GUI.Label(new Rect(num + 20f * S, num2 + 185f * S, 226f * S, 16f * S), "快捷格 " + (quickPage * 8 + 1) + "–" + Mathf.Min(26, quickPage * 8 + 8) + " / 26", val4);
		if (NativeUi.Button(new Rect(num + 12f * S, num2 + 207f * S, 18f * S, 18f * S), "UI/Basic.img/BtUP"))
		{
			quickPage = (quickPage + 3) % 4;
			quickIndex = quickPage * 8;
		}
		if (NativeUi.Button(new Rect(num + 236f * S, num2 + 207f * S, 18f * S, 18f * S), "UI/Basic.img/BtDown"))
		{
			quickPage = (quickPage + 1) % 4;
			quickIndex = quickPage * 8;
		}
		Rect val5 = new Rect(num + 109f * S, num2 + 207f * S, 47f * S, 19f * S);
		Texture2D val6 = T("BtOK_normal");
		if ((Object)(object)val6 != (Object)null)
		{
			GUI.DrawTexture(val5, (Texture)(object)val6);
		}
		if ((int)Event.current.type == 7 && Pointer.Released && val5.Contains(Pointer.Pos))
		{
			quickEdit = false;
		}
	}

	private static void ResetDraft()
	{
		draft = KeyMap.Defaults();
		quickDraft = QuickBarKeys.Defaults();
		skillDraft.Clear();
	}

	private static void DrawConfirmation()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected Obj, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Invalid comparison between Unknown and I4
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Invalid comparison between Unknown and I4
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Invalid comparison between Unknown and I4
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		Rect val = R(182.5f, 120.5f, 264f, 132f);
		Texture2D val2 = T("confirm_backgrnd");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val2);
		}
		GUIStyle val3 = new GUIStyle
		{
			fontSize = Mathf.RoundToInt(12f * S),
			alignment = (TextAnchor)4,
			wordWrap = true
		};
		val3.normal.textColor = Color.black;
		GUI.Label(new Rect(val.x + 16f * S, val.y + 32f * S, 232f * S, 48f * S), (confirmMode == 1) ? "要恢复默认键位和快捷键设置吗？" : "要清除全部可配置键位吗？", val3);
		for (int i = 0; i < 2; i++)
		{
			Rect val4 = new Rect(val.x + (float)(55 + i * 82) * S, val.y + 90f * S, 65f * S, 24f * S);
			bool flag = val4.Contains(Pointer.Pos);
			string text = ((i == 0) ? "yes_" : "no_");
			string text2;
			if (flag)
			{
				text2 = (Pointer.Down ? "pressed" : "mouseOver");
			}
			else
			{
				text2 = "normal";
			}
			Texture2D val5 = T("confirm_" + text + text2);
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(val4, (Texture)(object)val5);
			}
			if ((int)Event.current.type != 7 || !Pointer.Released || !flag)
			{
				continue;
			}
			if (i == 0)
			{
				if (confirmMode == 1)
				{
					ResetDraft();
				}
				else
				{
					foreach (GameAction item in new List<GameAction>(draft.Keys))
					{
						if (item > GameAction.Down && item != GameAction.Menu)
						{
							draft[item] = (KeyCode)0;
						}
					}
				}
			}
			confirmMode = 0;
		}
		if ((int)Event.current.type == 4 && (int)Event.current.keyCode == 27)
		{
			confirmMode = 0;
			Event.current.Use();
		}
	}

	private static void PointerStep()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!Pointer.Pressed)
		{
			return;
		}
		if (carried.HasValue)
		{
			if (!DropAt(Pointer.Pos))
			{
				swallow = true;
			}
		}
		else if (!quickEdit)
		{
			PickAt(Pointer.Pos);
		}
	}

	private static void UpdateCursor()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (confirmMode == 0)
		{
			if (carried.HasValue)
			{
				NativeCursor.Request(12);
			}
			else if (!Pointer.Down && !Pointer.RightDown && PickableAt(Pointer.Pos))
			{
				NativeCursor.Request(11);
			}
		}
	}

	public static void Draw()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Invalid comparison between Unknown and I4
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_060c: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		EnsurePos();
		pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, UiKit.W - 629f * S));
		pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, UiKit.H - 373f * S));
		NativeUi.RegisterWindow("KeyConfigWindow", R(0f, 0f, 629f, 373f), () => Visible);
		Rect val;
		int num;
		if (confirmMode == 0 && (int)Event.current.type == 7)
		{
			if (quickEdit)
			{
				val = QuickRect;
				num = ((!val.Contains(Pointer.Pos)) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (flag && Input.GetKeyDown((KeyCode)27))
		{
			if (carried.HasValue)
			{
				carried = null;
			}
			else
			{
				Close();
			}
			return;
		}
		if (flag && Pointer.Pressed && !carried.HasValue)
		{
			val = R(0f, 0f, 590f, 22f);
			if (val.Contains(Pointer.Pos))
			{
				dragging = true;
				offset = Pointer.Pos - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = Pointer.Pos - offset;
			pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, UiKit.W - 629f * S));
			pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, UiKit.H - 373f * S - BottomBar.Height));
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		if (flag)
		{
			PointerStep();
		}
		Texture2D val2 = T("backgrnd");
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(R(0f, 0f, 629f, 373f), (Texture)(object)val2);
		}
		foreach (KeyValuePair<KeyCode, Vector2> position in NativeKeyLayout.Positions)
		{
			KeyCode val3 = NativeKeyLayout.FunctionKey(position.Key);
			Rect val4 = R(position.Value.x, position.Value.y, 32f, 32f);
			foreach (KeyValuePair<GameAction, KeyCode> item in draft)
			{
				if (item.Value == val3 && (int)val3 != 0 && (!carried.HasValue || item.Key != carried.Value))
				{
					int key = QuickSlots.Index(item.Key);
					Texture2D val5 = ((!skillDraft.TryGetValue(key, out var value)) ? Icon(item.Key) : QuickSlots.BindingIcon(value));
					if ((Object)(object)val5 != (Object)null)
					{
						GUI.DrawTexture(val4, (Texture)(object)val5, (ScaleMode)2);
					}
				}
			}
			if (flag && quickEdit && Pointer.Pressed && val4.Contains(Pointer.Pos))
			{
				selected = val3;
				SetQuickKey(val3);
			}
		}
		for (int num2 = 0; num2 < palette.Length; num2++)
		{
			if (InTray(palette[num2]) && (!carried.HasValue || carried.Value != palette[num2]))
			{
				Vector2 val6 = NativeKeyLayout.TrayPosition(palette[num2]);
				Rect val7 = R(val6.x, val6.y, 32f, 32f);
				Texture2D val8 = DraftIcon(palette[num2]);
				if (!((Object)(object)val8 == (Object)null))
				{
					GUI.DrawTexture(val7, (Texture)(object)val8, (ScaleMode)2);
				}
			}
		}
		if (Button("BtClose", 609f, 6f, 12, 12) || Button("BtCancel", 58f, 236f, 47, 19))
		{
			Close();
			return;
		}
		if (Button("BtOK", 8f, 236f, 47, 19))
		{
			QuickBarKeys.Save(quickDraft);
			KeyMap.Apply(draft);
			foreach (KeyValuePair<int, int> item2 in skillDraft)
			{
				QuickSlots.Bind(item2.Key, item2.Value);
			}
			if (GameSession.InGame)
			{
				GameSession.SaveToDb();
			}
			Close();
			return;
		}
		if (Button("BtDefault", 112f, 236f, 65, 19))
		{
			confirmMode = 1;
			carried = null;
		}
		if (Button("BtDelete", 177f, 236f, 71, 19))
		{
			confirmMode = 2;
			carried = null;
		}
		if (Button("BtQuickSlot", 260f, 236f, 96, 18))
		{
			quickEdit = !quickEdit;
			carried = null;
		}
		if (quickEdit)
		{
			DrawQuickEdit();
		}
		if (confirmMode != 0)
		{
			DrawConfirmation();
		}
		if (carried.HasValue)
		{
			Texture2D val9 = DraftIcon(carried.Value);
			if ((Object)(object)val9 != (Object)null)
			{
				GUI.DrawTexture(new Rect(Pointer.Pos.x - 16f * S, Pointer.Pos.y - 16f * S, 32f * S, 32f * S), (Texture)(object)val9);
			}
		}
		UpdateCursor();
		if (Pointer.Released)
		{
			swallow = false;
		}
	}

	static KeyConfigWindow()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
	}
}
