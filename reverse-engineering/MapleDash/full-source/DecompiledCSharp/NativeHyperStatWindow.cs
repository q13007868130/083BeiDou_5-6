using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public static class NativeHyperStatWindow
{
	private const string Root = "UI/UIWindow4.img/HyperStat/Window/";

	private const string PlusRoot = "UI/UIAuthenticForce.img/AutEquip/";

	private const float Width = 185f;

	private const float Height = 318f;

	private const int VisibleRows = 12;

	private const int RowStep = 18;

	private const int BarHeight = 16;

	private const float ListTop = 41f;

	private const float ListLeft = 13f;

	private const float ListWidth = 150f;

	private const float ListHeight = 214f;

	private const float LevelLeft = 117f;

	private const float LevelWidth = 44f;

	private static readonly Color LevelInk = new Color(0.22f, 0.22f, 0.26f, 1f);

	private static bool visible;

	private static bool dragging;

	private static bool positioned;

	private static Vector2 position;

	private static Vector2 grab;

	private static int owner;

	private static int preset;

	private static int activePreset;

	private static int selected;

	private static int scroll;

	private static HyperStatResponse state;

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

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / 185f, UiKit.H * 0.85f / 318f));

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
				return new Vector2((UiKit.W / S - 185f) / 2f, (UiKit.H / S - 318f) / 2f);
			}
			return NativeUi.ClampWindowPosition(position, new Vector2(185f, 318f), S);
		}
	}

	private static Rect Window
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(Free.x * S, Free.y * S, 185f * S, 318f * S);
		}
	}

	public static void Close()
	{
		visible = (dragging = (positioned = false));
		state = null;
		error = null;
		scroll = 0;
		selected = 0;
	}

	public static void Open()
	{
		Close();
		NativeSymbolWindow.Close();
		NativeInnerAbilityWindow.Close();
		if (GameSession.InGame)
		{
			visible = true;
			owner = GameSession.CharacterId;
			Load(0);
		}
	}

	private static void Load(int targetPreset)
	{
		if (GameSession.Character?.record.level != Profile.level)
		{
			GameSession.SaveToDb();
		}
		HyperStatResponse hyperStatResponse = GameSession.Net.HyperStat(new HyperStatRequest
		{
			characterId = owner,
			preset = targetPreset,
			operation = HyperStatOperation.Preview
		});
		if (hyperStatResponse.ok)
		{
			state = hyperStatResponse;
			preset = hyperStatResponse.preset;
			error = null;
		}
		else
		{
			error = hyperStatResponse.error;
			BottomBar.Toast(hyperStatResponse.error);
		}
	}

	private static int LevelOf(int skillId)
	{
		if (state?.lines == null)
		{
			return 0;
		}
		foreach (HyperStatLine line in state.lines)
		{
			if (line.skillId == skillId)
			{
				return line.slv;
			}
		}
		return 0;
	}

	private static string Operate(int skillId, HyperStatOperation operation)
	{
		HyperStatResponse hyperStatResponse = GameSession.Net.HyperStat(new HyperStatRequest
		{
			characterId = owner,
			preset = preset,
			skillId = skillId,
			amount = 1,
			operation = operation,
			commit = true
		});
		if (!hyperStatResponse.ok)
		{
			error = hyperStatResponse.error;
			return hyperStatResponse.error;
		}
		state = hyperStatResponse;
		error = null;
		GameSession.ReloadAfterCommand();
		return null;
	}

	public static void Draw()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0551: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_072c: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		if (state == null || state.level != Profile.level)
		{
			Load(preset);
		}
		Rect window = Window;
		GrowthUi.S = S;
		GrowthUi.R = window;
		if (GrowthUi.Drag(ref dragging, ref positioned, ref position, ref grab, 185f, 318f, () =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return Window;
		}, 28f, 30f, typeof(NativeHyperStatWindow)))
		{
			window = Window;
		}
		GrowthUi.R = window;
		NativeUi.RegisterWindow("NativeHyperStatWindow", window, () => visible);
		GrowthUi.Image("UI/UIWindow4.img/HyperStat/Window/", "backgrnd", 0f, 0f);
		GrowthUi.Image("UI/UIWindow4.img/HyperStat/Window/", "backgrnd2", 6f, 7f);
		GrowthUi.Image("UI/UIWindow4.img/HyperStat/Window/", "backgrnd3", 13f, 41f);
		IReadOnlyList<HyperStatInfo> all = HyperStatCatalog.All;
		int num = Mathf.Max(0, all.Count - 12);
		scroll = Mathf.Clamp(scroll, 0, num);
		selected = Mathf.Clamp(selected, 0, Mathf.Max(0, all.Count - 1));
		Rect val2;
		for (int num2 = 0; num2 < 12 && scroll + num2 < all.Count; num2++)
		{
			int num3 = scroll + num2;
			HyperStatInfo hyperStatInfo = all[num3];
			float num4 = 41f + (float)(num2 * 18);
			if (num3 == selected)
			{
				GrowthUi.Veil(13f, num4, 150f, 16f, new Color(1f, 1f, 1f, 0.35f));
			}
			ImgNode imgNode = WzAssets.Node("UI/UIWindow4.img/HyperStat/Window/statList/" + hyperStatInfo.skillId);
			Texture2D val = ((imgNode == null) ? null : WzAssets.Texture(imgNode));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(GrowthUi.At(18f, num4 + (float)(16 - ((Texture)val).height) * 0.5f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
			else
			{
				GrowthUi.Text("UI/UIWindow4.img/HyperStat/Window/", hyperStatInfo.name, 18f, num4, 96f, "", (TextAnchor)3, 16f);
			}
			int num5 = LevelOf(hyperStatInfo.skillId);
			if (num5 > 0)
			{
				GrowthUi.Text("UI/UIWindow4.img/HyperStat/Window/", "Lv" + num5, 117f, num4, 44f, "", (TextAnchor)4, 16f, LevelInk);
			}
			GrowthUi.Image("UI/UIAuthenticForce.img/AutEquip/", "textUp/+", 152f, num4 + 4f);
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val2 = GrowthUi.At(148f, num4, 14f, 16f);
				if (val2.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					selected = num3;
					string text = Operate(hyperStatInfo.skillId, HyperStatOperation.Apply);
					if (text != null)
					{
						BottomBar.Toast(text);
					}
					return;
				}
			}
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val2 = GrowthUi.At(13f, num4, 150f, 16f);
				if (val2.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					selected = num3;
				}
			}
		}
		if (GUI.enabled && GrowthUi.Repaint)
		{
			val2 = GrowthUi.At(13f, 41f, 150f, 214f);
			if (val2.Contains(Pointer.Pos) && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
			{
				scroll = Mathf.Clamp(scroll - (int)Mathf.Sign(Input.mouseScrollDelta.y), 0, num);
			}
		}
		int skillId = ((all.Count > 0) ? all[Mathf.Clamp(selected, 0, all.Count - 1)].skillId : 0);
		if (GrowthUi.Button("UI/UIWindow4.img/HyperStat/Window/", "BtReduce"))
		{
			string text2 = Operate(skillId, HyperStatOperation.Remove);
			if (text2 != null)
			{
				BottomBar.Toast(text2);
			}
		}
		if (GrowthUi.Button("UI/UIWindow4.img/HyperStat/Window/", "BtReset"))
		{
			BottomBar.Toast(Operate(0, HyperStatOperation.Reset) ?? "极限属性已重置");
		}
		activePreset = state?.activePreset ?? preset;
		for (int num6 = 0; num6 < 3; num6++)
		{
			GrowthUi.Image("UI/UIWindow4.img/HyperStat/Window/", "BtpresetPage" + (num6 + 1) + "/" + ((num6 == preset) ? "checked" : "normal") + "/0", 60 + num6 * 20, 267f);
			if (GUI.enabled && GrowthUi.Repaint && Pointer.Clicked)
			{
				val2 = GrowthUi.At(60 + num6 * 20, 267f, 17f, 16f);
				if (val2.Contains(Pointer.Pos))
				{
					Pointer.ConsumeClick();
					Load(num6);
				}
			}
		}
		if (activePreset >= 0 && activePreset < 3)
		{
			GrowthUi.Image("UI/UIWindow4.img/HyperStat/Window/", "BtpresetSelected/0", 60 + activePreset * 20, 267f);
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && activePreset != preset;
		if (GrowthUi.Button("UI/UIWindow4.img/HyperStat/Window/", "BtpresetApplication"))
		{
			HyperStatResponse hyperStatResponse = GameSession.Net.HyperStat(new HyperStatRequest
			{
				characterId = owner,
				preset = preset,
				operation = HyperStatOperation.SwitchPreset,
				commit = true
			});
			if (hyperStatResponse.ok)
			{
				state = hyperStatResponse;
				activePreset = hyperStatResponse.activePreset;
				BottomBar.Toast("已套用预设 " + (preset + 1));
				GameSession.ReloadAfterCommand();
			}
			else
			{
				BottomBar.Toast(hyperStatResponse.error);
			}
		}
		GUI.enabled = enabled;
		int num7 = state?.totalSp ?? 0;
		int num8 = state?.spentSp ?? 0;
		GrowthUi.Text("UI/UIWindow4.img/HyperStat/Window/", Mathf.Max(0, num7 - num8).ToString(CultureInfo.InvariantCulture), 88f, 290f, 26f, "", (TextAnchor)0, 12f, LevelInk);
		if (!string.IsNullOrEmpty(error))
		{
			GrowthUi.Plate(13f, 236f, 150f, 19f, 0.8f);
			GrowthUi.Text("UI/UIWindow4.img/HyperStat/Window/", error, 16f, 238f, 144f, "", (TextAnchor)0, 13f, new Color(1f, 0.58f, 0.52f));
		}
	}

	static NativeHyperStatWindow()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
	}
}
