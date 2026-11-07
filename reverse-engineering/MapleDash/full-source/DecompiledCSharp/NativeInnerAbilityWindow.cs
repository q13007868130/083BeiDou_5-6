using System;
using System.Collections.Generic;
using UnityEngine;

public static class NativeInnerAbilityWindow
{
	private const string Root = "UI/UIWindow4.img/AbilitySetting/";

	private const float Width = 248f;

	private const float Height = 388f;

	private static readonly float[] RowY = new float[3] { 197f, 218f, 239f };

	private static readonly float[] BackY = new float[3] { 194f, 215f, 236f };

	private static readonly string[] GradeNodes = new string[4] { "rare", "epic", "unique", "legendary" };

	private static readonly string[] GradeNames = new string[4] { "稀有", "史诗", "唯一", "传说" };

	private static bool visible;

	private static bool dragging;

	private static bool positioned;

	private static Vector2 position;

	private static Vector2 grab;

	private static int owner;

	private static int preset;

	private static int activePreset;

	private static bool useCirculator;

	private static readonly bool[] locked = new bool[3];

	private static AbilityResponse state;

	private static string error;

	public static bool Visible => visible;

	public static bool AtPointer
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			if (visible)
			{
				if (!dragging)
				{
					Rect window = Window;
					return window.Contains(Pointer.Pos);
				}
				return true;
			}
			return false;
		}
	}

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / 248f, UiKit.H * 0.85f / 388f));

	private static Vector2 Free
	{
		get
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			if (!positioned)
			{
				return new Vector2((UiKit.W / S - 248f) / 2f, (UiKit.H / S - 388f) / 2f);
			}
			return NativeUi.ClampWindowPosition(position, new Vector2(248f, 388f), S);
		}
	}

	private static Rect Window
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(Free.x * S, Free.y * S, 248f * S, 388f * S);
		}
	}

	public static void Close()
	{
		visible = (dragging = (positioned = false));
		state = null;
		error = null;
		useCirculator = false;
		for (int i = 0; i < locked.Length; i++)
		{
			locked[i] = false;
		}
	}

	public static void Open()
	{
		Close();
		NativeSymbolWindow.Close();
		NativeHyperStatWindow.Close();
		if (GameSession.InGame)
		{
			visible = true;
			owner = GameSession.CharacterId;
			useCirculator = FindCirculator() != 0;
			Load(0);
		}
	}

	private static void Load(int targetPreset)
	{
		AbilityRequest abilityRequest = new AbilityRequest
		{
			characterId = owner,
			preset = targetPreset,
			operation = AbilityOperation.Preview
		};
		abilityRequest.lockedRows = LockedRows();
		AbilityResponse abilityResponse = GameSession.Net.InnerAbility(abilityRequest);
		if (abilityResponse.ok)
		{
			state = abilityResponse;
			preset = abilityResponse.preset;
			error = null;
		}
		else
		{
			error = abilityResponse.error;
			BottomBar.Toast(abilityResponse.error);
		}
	}

	private static List<int> LockedRows()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < locked.Length; i++)
		{
			if (locked[i])
			{
				list.Add(i);
			}
		}
		return list;
	}

	private static string Send(AbilityOperation operation)
	{
		AbilityRequest abilityRequest = new AbilityRequest
		{
			characterId = owner,
			preset = preset,
			operation = operation,
			lockedRows = LockedRows(),
			commit = true
		};
		if (operation == AbilityOperation.Circulator)
		{
			abilityRequest.circulatorInventoryItemId = FindCirculator();
		}
		AbilityResponse abilityResponse = GameSession.Net.InnerAbility(abilityRequest);
		if (!abilityResponse.ok)
		{
			error = abilityResponse.error;
			return abilityResponse.error;
		}
		error = null;
		state = abilityResponse;
		GameSession.ReloadAfterCommand();
		return null;
	}

	private static long FindCirculator()
	{
		List<InventoryEntry> list = GameSession.Character?.inventory;
		if (list == null)
		{
			return 0L;
		}
		foreach (InventoryEntry item in list)
		{
			if (item.position > 0 && (item.itemId == 2702 || item.itemId == 2703 || item.itemId == 5062))
			{
				return item.itemRowId;
			}
		}
		return 0L;
	}

	public static void Draw()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0634: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_041e: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		Rect window = Window;
		GrowthUi.S = S;
		GrowthUi.R = window;
		if (GrowthUi.Drag(ref dragging, ref positioned, ref position, ref grab, 248f, 388f, () =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return Window;
		}, 28f, 30f, typeof(NativeInnerAbilityWindow)))
		{
			window = Window;
		}
		GrowthUi.R = window;
		NativeUi.RegisterWindow("NativeInnerAbilityWindow", window, () => visible);
		GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "backgrnd", 0f, 0f);
		GrowthUi.Plate(8f, 5f, 152f, 20f, 0.62f);
		GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", "内在能力", 12f, 7f, 146f, "", (TextAnchor)0);
		AbilityResponse abilityResponse = state;
		if (abilityResponse != null && abilityResponse.pending)
		{
			DrawConfirm();
			return;
		}
		int num = state?.grade ?? 0;
		string text = GradeNodes[Mathf.Clamp(num, 0, 3)];
		GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "AbilityGrade/" + text + "/0", 9f, 162f);
		Rect val;
		if (FindCirculator() != 0L)
		{
			GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", useCirculator ? "消耗：循環器（点击改用名聲值）" : "消耗：名聲值（点击改用循環器）", 14f, 146f, 226f, "", (TextAnchor)0, 13f, new Color(0.8f, 0.85f, 1f));
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val = GrowthUi.At(14f, 144f, 220f, 16f);
				if (val.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					useCirculator = !useCirculator;
					Load(preset);
				}
			}
		}
		List<AbilityLine> list = state?.lines ?? new List<AbilityLine>();
		for (int num2 = 0; num2 < 3; num2++)
		{
			float y = BackY[num2];
			float y2 = RowY[num2];
			int num3 = ((num2 < list.Count) ? list[num2].grade : num);
			GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "BtAbility/" + GradeNodes[Mathf.Clamp(num3, 0, 3)] + "/0", 9f, y);
			GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "BtAbilityLockEff/" + (locked[num2] ? "lock" : "unlock") + "/normal/0", 9f, y);
			int num4 = state?.lineCount ?? 3;
			int num5;
			string text2;
			if (num2 < num4)
			{
				num5 = ((num2 < list.Count) ? 1 : 0);
				if (num5 != 0)
				{
					text2 = (string.IsNullOrEmpty(list[num2].name) ? "未洗出" : list[num2].name);
					goto IL_0356;
				}
			}
			else
			{
				num5 = 0;
			}
			text2 = "未解锁";
			goto IL_0356;
			IL_0356:
			string text3 = text2;
			Color value = (Color)(locked[num2] ? new Color(0.65f, 0.72f, 0.9f) : GrowthUi.Ink);
			GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", text3, 14f, y2, 150f, "", (TextAnchor)0, 16f, value);
			if (num5 != 0)
			{
				GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", "Lv" + list[num2].slv, 168f, y2, 42f, "", (TextAnchor)2, 16f, value);
			}
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val = GrowthUi.At(9f, y, 230f, 18f);
				if (val.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					locked[num2] = !locked[num2];
					Load(preset);
				}
			}
		}
		GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", (state?.honorExp ?? 0).ToString("N0"), 127f, 288f, 100f, "", (TextAnchor)2, 16f);
		GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", (state?.cost ?? 0).ToString("N0"), 127f, 313f, 100f, "", (TextAnchor)2, 16f);
		activePreset = state?.activePreset ?? preset;
		for (int num6 = 0; num6 < 3; num6++)
		{
			GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "button:preset" + (num6 + 1) + "/" + ((num6 == preset) ? "checked" : "normal") + "/0", 177 + num6 * 20, 263f);
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val = GrowthUi.At(175 + num6 * 20, 261f, 14f, 14f);
				if (val.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					Load(num6);
				}
			}
		}
		if (activePreset >= 0 && activePreset < 3)
		{
			GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", "使用中 " + (activePreset + 1), 14f, 263f, 160f, "", (TextAnchor)0, 14f, new Color(0.62f, 0.88f, 0.6f));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && !(state?.pending ?? false);
		if (GrowthUi.Button("UI/UIWindow4.img/AbilitySetting/", "button:Use"))
		{
			string text4 = Send((!useCirculator) ? AbilityOperation.Honor : AbilityOperation.Circulator);
			if (text4 != null)
			{
				BottomBar.Toast(text4);
			}
			else if (!useCirculator)
			{
				BottomBar.Toast("内在能力已重置");
			}
			else
			{
				Load(preset);
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/UIWindow4.img/AbilitySetting/", "button:close"))
		{
			Close();
		}
	}

	private static void DrawConfirm()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		List<AbilityLine> list = state?.candidate;
		if (list == null)
		{
			return;
		}
		float num = Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / 342f, UiKit.H * 0.95f / 389f));
		Rect val = new Rect((UiKit.W - 342f * num) / 2f, (UiKit.H - 389f * num) / 2f, 342f * num, 389f * num);
		Rect r = GrowthUi.R;
		float s = GrowthUi.S;
		GrowthUi.R = val;
		GrowthUi.S = num;
		NativeUi.RegisterWindow("NativeAbilityConfirm", val, () => true);
		GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "confirm/backgrnd", 0f, 0f);
		GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", "预设 " + (preset + 1) + " 的新结果", 36f, 92f, 270f, "", (TextAnchor)1, 15f);
		int num2 = state.grade;
		foreach (AbilityLine item in list)
		{
			num2 = Math.Max(num2, item.grade);
		}
		GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "confirm/AbilityGrade/" + GradeNodes[Mathf.Clamp(num2, 0, 3)] + "/0", 35f, 176f);
		GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", "新 " + GradeNames[Mathf.Clamp(num2, 0, 3)], 44f, 182f, 200f, "", (TextAnchor)0, 16f);
		float[] array = new float[3] { 208f, 228f, 248f };
		float[] array2 = new float[3] { 210f, 230f, 250f };
		for (int num3 = 0; num3 < 3; num3++)
		{
			int num4 = ((num3 < list.Count) ? list[num3].grade : num2);
			GrowthUi.Image("UI/UIWindow4.img/AbilitySetting/", "confirm/Ability/" + GradeNodes[Mathf.Clamp(num4, 0, 3)] + "/0", 36f, array[num3]);
			string text = ((num3 < list.Count) ? (list[num3].name + "   Lv" + list[num3].slv) : "—");
			GrowthUi.Text("UI/UIWindow4.img/AbilitySetting/", text, 44f, array2[num3], 256f, "", (TextAnchor)2, 16f);
		}
		if (GrowthUi.Button("UI/UIWindow4.img/AbilitySetting/", "confirm/button:Ok"))
		{
			Choose(accept: true);
		}
		else if (GrowthUi.Button("UI/UIWindow4.img/AbilitySetting/", "confirm/button:Cancel"))
		{
			Choose(accept: false);
		}
		GrowthUi.R = r;
		GrowthUi.S = s;
	}

	private static void Choose(bool accept)
	{
		AbilityResponse abilityResponse = GameSession.Net.InnerAbility(new AbilityRequest
		{
			characterId = owner,
			preset = preset,
			operation = AbilityOperation.Choose,
			accept = accept,
			commit = true
		});
		if (!abilityResponse.ok)
		{
			error = abilityResponse.error;
			BottomBar.Toast(abilityResponse.error);
			return;
		}
		state = abilityResponse;
		error = null;
		GameSession.ReloadAfterCommand();
		BottomBar.Toast(accept ? "已采用新的内在能力" : "已保留原来的内在能力");
	}
}
