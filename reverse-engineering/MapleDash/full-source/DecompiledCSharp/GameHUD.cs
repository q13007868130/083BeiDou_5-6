using Server.Bot;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
	public bool showTuning;

	public static bool HideTuning;

	public static bool InterfaceHidden;

	private Texture2D _white;

	private GUIStyle _cell;

	private GUIStyle _cellTxt;

	private GUIStyle _small;

	private GUIStyle _box;

	private bool _stylesReady;

	private Vector2 _mouseGui;

	private static string castFeedback;

	private static float feedbackUntil;

	private const float SkillCell = 88f;

	private const float SkillGap = 12f;

	private Texture2D White
	{
		get
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected Obj, but got Unknown
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_white == (Object)null)
			{
				_white = new Texture2D(2, 2, (TextureFormat)4, false);
				_white.SetPixels(new Color[4]
				{
					Color.white,
					Color.white,
					Color.white,
					Color.white
				});
				_white.Apply();
			}
			return _white;
		}
	}

	private void Awake()
	{
		castFeedback = null;
		feedbackUntil = 0f;
		if (PlayerPrefs.GetInt("hide_tuning", 0) == 1)
		{
			showTuning = false;
		}
	}

	private void EnsureStyles()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected Obj, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected Obj, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected Obj, but got Unknown
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected Obj, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected Obj, but got Unknown
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		if (!_stylesReady)
		{
			_stylesReady = true;
			_box = new GUIStyle();
			_box.normal.background = White;
			_box.normal.textColor = new Color(0.92f, 0.94f, 0.98f);
			_box.padding = new RectOffset(12, 12, 10, 10);
			_cell = new GUIStyle();
			_cell.normal.background = White;
			_cellTxt = new GUIStyle();
			_cellTxt.alignment = (TextAnchor)4;
			_cellTxt.fontSize = 13;
			_cellTxt.normal.textColor = Color.white;
			_cellTxt.fontStyle = (FontStyle)1;
			_small = new GUIStyle();
			_small.fontSize = 12;
			_small.normal.textColor = new Color(0.85f, 0.88f, 0.94f);
			GUI.skin.horizontalSlider.fixedHeight = 18f;
			GUI.skin.horizontalSliderThumb.fixedWidth = 18f;
		}
	}

	private void Fill(Rect r, Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, (Texture)(object)White);
		GUI.color = color;
	}

	private void OnGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		string name;
		if ((int)Event.current.type == 7)
		{
			name = "HUD.Repaint";
		}
		else
		{
			name = (((int)Event.current.type == 8) ? "HUD.Layout" : "HUD.Input");
		}
		using (BotPerf.Timer(name))
		{
			if (InterfaceHidden && !KeyConfigWindow.Visible)
			{
				return;
			}
			GUI.depth = 0;
			EnsureStyles();
			UiKit.Begin();
			_mouseGui = UiKit.MouseGui();
			if (ShopWindow.Visible)
			{
				GameWindows.TrackPanels();
				NativeCashWindow.Tick();
				NativeCashWindow.Draw();
				NativeUtilPrompt.Draw();
				DrawNotice();
				return;
			}
			if (!NativeUtilPrompt.Visible)
			{
				NativeVMatrixWindow.CarryStep();
			}
			bool flag = NativeUtilPrompt.Visible || NativeSymbolWindow.AtPointer || NativeVMatrixWindow.AtPointer || NativeHexaMatrixWindow.AtPointer;
			bool enabled = GUI.enabled;
			Pointer.InputMask inputMask = new Pointer.InputMask(flag);
			GUI.enabled = enabled && !flag;
			try
			{
				NativeInventoryActions.CarryStep();
				GmTimers.Tick();
				NpcDialogUi.DrawField();
				BottomBar.Draw();
				NativeGainNotice.Draw();
				NativeBossHpBar.Draw();
				NativeEventClock.Draw();
				NativeArenaHud.Draw();
				NativeScreenEffect.Draw();
				QuestAlarm.Draw();
				TemporaryStatView.Draw();
			}
			finally
			{
				inputMask.Dispose();
				GUI.enabled = enabled;
			}
			GameWindows.DrawAll();
			NativeVMatrixWindow.DrawCarryGhost();
			DrawNotice();
			NpcDialogUi.Draw();
			NpcDialogUi.DrawHint();
			QuestNotice.DrawReady();
			if (PlayerPrefs.GetInt("debug_keyconfig", 0) == 1)
			{
				PlayerPrefs.DeleteKey("debug_keyconfig");
				if (!KeyConfigWindow.Visible)
				{
					KeyConfigWindow.Toggle();
				}
			}
			NativeInventoryActions.DrawCarry();
			if (showTuning && !HideTuning)
			{
				DrawTuning();
			}
		}
	}

	public static void SkillBarOrigin(out float x0, out float y0)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		float num = 388f;
		x0 = UiKit.W - num - 12f;
		y0 = UiKit.H - 88f - 12f;
		Rect panel = StatusBar.Panel;
		float width = panel.width;
		float h = UiKit.H;
		panel = StatusBar.Panel;
		float num2 = h - panel.height;
		if (x0 < width + 8f)
		{
			x0 = Mathf.Max(12f, UiKit.W - num - 12f);
			y0 = Mathf.Max(8f, num2 - 88f - 8f);
		}
	}

	public static Rect SkillButtonRect(int slot)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		SkillBarOrigin(out var x, out var y);
		return new Rect(x + (float)slot * 100f, y, 88f, 88f);
	}

	public static bool PressSkill(Vector2 position)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 4; i++)
		{
			Rect val = SkillButtonRect(i);
			if (val.Contains(position))
			{
				PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
				SkillSystem skillSystem = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<SkillSystem>() : null);
				if ((Object)(object)skillSystem != (Object)null)
				{
					castFeedback = (skillSystem.TryCast(i) ? null : skillSystem.LastFailure);
					feedbackUntil = Time.unscaledTime + 1.5f;
				}
				return true;
			}
		}
		return false;
	}

	private void DrawNotice()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected Obj, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected Obj, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		SkillSystem skillSystem = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<SkillSystem>() : null);
		string text = (((Object)(object)skillSystem != (Object)null) ? skillSystem.ActiveNotice : null);
		if (string.IsNullOrEmpty(text))
		{
			text = QuestNotice.Active;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = BottomBar.ActiveToast;
		}
		if (!string.IsNullOrEmpty(text))
		{
			GUIStyle val = new GUIStyle(_small);
			val.alignment = (TextAnchor)4;
			val.fontSize = 15;
			val.normal.textColor = new Color(1f, 0.94f, 0.72f);
			val.wordWrap = true;
			float num = Mathf.Min(600f, UiKit.W - 40f);
			float num2 = val.CalcHeight(new GUIContent(text), num);
			float num3 = UiKit.H - BottomBar.Height - 18f;
			Rect val2 = BottomBar.QuickPanelRect();
			float num4 = Mathf.Min(num3, val2.y - 12f);
			Rect val3 = new Rect((UiKit.W - num) * 0.5f, num4 - num2, num, num2);
			Color textColor = val.normal.textColor;
			val.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
			float num5 = Mathf.Max(0.5f, BottomBar.S * 0.5f);
			GUI.Label(new Rect(val3.x - num5, val3.y, val3.width, val3.height), text, val);
			GUI.Label(new Rect(val3.x + num5, val3.y, val3.width, val3.height), text, val);
			GUI.Label(new Rect(val3.x, val3.y - num5, val3.width, val3.height), text, val);
			GUI.Label(new Rect(val3.x, val3.y + num5, val3.width, val3.height), text, val);
			val.normal.textColor = textColor;
			GUI.Label(val3, text, val);
		}
	}

	private void DrawTopLeft()
	{
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Expected Obj, but got Unknown
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Expected Obj, but got Unknown
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected Obj, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected Obj, but got Unknown
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected Obj, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null)
		{
			return;
		}
		PlayerCtrl player = GameLoop.I.player;
		FieldDirector field = GameLoop.I.field;
		if ((Object)(object)field != (Object)null && field.Map != null)
		{
			GUIStyle val = new GUIStyle(_small);
			val.normal.textColor = new Color(0.85f, 0.93f, 1f);
			GUI.Label(new Rect(18f, 16f, 420f, 20f), field.Map.name + "  (" + field.Map.id + ")   怪物 " + field.AliveCount + "   击杀 " + GameLoop.I.killCount, val);
			if (!string.IsNullOrEmpty(GroundItem.LastPickup))
			{
				GUIStyle val2 = new GUIStyle(_small);
				val2.normal.textColor = new Color(1f, 0.95f, 0.6f);
				GUI.Label(new Rect(18f, 92f, 300f, 20f), "拾取 " + GroundItem.LastPickup, val2);
			}
			if (field.NearPortal != null && field.NearPortal.PressUpTrigger)
			{
				GUIStyle val3 = new GUIStyle(_small);
				val3.normal.textColor = new Color(0.7f, 1f, 0.8f);
				GUI.Label(new Rect(18f, 112f, 320f, 20f), "按 ↑ / W 进入传送门", val3);
			}
		}
		else
		{
			GUI.Label(new Rect(18f, 16f, 260f, 20f), "击杀 " + GameLoop.I.killCount + "   深度层 " + (player.lane + 1) + "/5", _small);
		}
		GUIStyle val4 = new GUIStyle(_small);
		val4.normal.textColor = new Color(0.98f, 0.82f, 0.35f);
		GUI.Label(new Rect(18f, 38f, 320f, 20f), "金币 " + Profile.gold + "   武器：" + WeaponDefs.Get(Profile.equipWeapon).name, val4);
		Rect val5 = new Rect(18f, 62f, 96f, 28f);
		Fill(val5, new Color(0.16f, 0.2f, 0.3f, 0.92f));
		Fill(new Rect(val5.x, val5.y, val5.width, 2f), new Color(0.5f, 0.8f, 1f));
		GUIStyle val6 = new GUIStyle(_small);
		val6.alignment = (TextAnchor)4;
		val6.normal.textColor = Color.white;
		GUI.Label(val5, "更换角色", val6);
		if (!VirtualControlsEditor.Visible && !VirtualControls.BlockingPointer && Pointer.Pressed && val5.Contains(_mouseGui) && (Object)(object)GameLoop.I != (Object)null)
		{
			GameLoop.I.ExitToCharSelect();
		}
	}

	private void DrawStage()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected Obj, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected Obj, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0546: Expected Obj, but got Unknown
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Expected Obj, but got Unknown
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Expected Obj, but got Unknown
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Expected Obj, but got Unknown
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		StageDirector stageDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.stage : null);
		if ((Object)(object)stageDirector == (Object)null)
		{
			return;
		}
		StageLevel curLevel = stageDirector.CurLevel;
		if (curLevel != null)
		{
			GUIStyle val = new GUIStyle(_small);
			val.alignment = (TextAnchor)1;
			val.fontSize = 16;
			val.fontStyle = (FontStyle)1;
			val.normal.textColor = new Color(0.94f, 0.96f, 1f, 0.92f);
			GUI.Label(new Rect(0f, 10f, UiKit.W, 22f), curLevel.id + "   " + curLevel.name, val);
			GUIStyle val2 = new GUIStyle(_small);
			val2.alignment = (TextAnchor)1;
			val2.fontSize = 12;
			val2.normal.textColor = new Color(0.72f, 0.78f, 0.87f, 0.8f);
			GUI.Label(new Rect(0f, 30f, UiKit.W, 20f), "目标：" + curLevel.goal + "    推进 " + (stageDirector.segIndex + 1) + "/" + curLevel.segments.Count, val2);
		}
		BossUnit boss = stageDirector.boss;
		if ((Object)(object)boss != (Object)null && !boss.dead && boss.def != null)
		{
			float num = UiKit.W * 0.56f;
			float num2 = (UiKit.W - num) * 0.5f;
			float num3 = 56f;
			float num4 = 14f;
			Fill(new Rect(num2 - 3f, num3 - 3f, num + 6f, num4 + 6f), new Color(0f, 0f, 0f, 0.75f));
			Fill(new Rect(num2, num3, num, num4), new Color(0.24f, 0.1f, 0.12f));
			Fill(new Rect(num2, num3, num * boss.HpRatio, num4), new Color(0.86f, 0.26f, 0.3f));
			if (boss.def.phases != null)
			{
				foreach (PhaseDef phase in boss.def.phases)
				{
					if (!(phase.at >= 0.999f))
					{
						Fill(new Rect(num2 + num * phase.at, num3, 2f, num4), new Color(0f, 0f, 0f, 0.55f));
					}
				}
			}
			GUIStyle val3 = new GUIStyle(_small);
			val3.alignment = (TextAnchor)4;
			val3.fontSize = 13;
			val3.fontStyle = (FontStyle)1;
			val3.normal.textColor = Color.white;
			GUI.Label(new Rect(num2, num3 - 2f, num, num4), boss.def.name + "   " + Mathf.CeilToInt(boss.hp) + " / " + Mathf.CeilToInt(boss.hpMax), val3);
		}
		if (!string.IsNullOrEmpty(stageDirector.hint) && stageDirector.hintT > 0f)
		{
			GUIStyle val4 = new GUIStyle(_small);
			val4.alignment = (TextAnchor)4;
			val4.fontSize = 15;
			val4.wordWrap = true;
			val4.normal.textColor = new Color(1f, 0.96f, 0.9f, 0.96f);
			float num5 = Mathf.Min(UiKit.W * 0.78f, 620f);
			float num6 = (UiKit.W - num5) * 0.5f;
			float num7 = UiKit.H - 186f;
			Rect val5 = new Rect(num6, num7, num5, 44f);
			Fill(new Rect(val5.x, val5.y, val5.width, val5.height), new Color(0.06f, 0.07f, 0.1f, 0.72f));
			Fill(new Rect(val5.x, val5.y, 3f, val5.height), new Color(0.95f, 0.78f, 0.4f, 0.9f));
			GUI.Label(new Rect(val5.x + 10f, val5.y + 4f, val5.width - 20f, val5.height - 8f), stageDirector.hint, val4);
		}
		if (stageDirector.allClear)
		{
			Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.5f));
			GUIStyle val6 = new GUIStyle();
			val6.alignment = (TextAnchor)4;
			val6.fontSize = 34;
			val6.fontStyle = (FontStyle)1;
			val6.normal.textColor = new Color(0.95f, 0.85f, 0.45f);
			GUI.Label(new Rect(0f, UiKit.H * 0.4f, UiKit.W, 50f), "第一章 · 通关", val6);
			GUIStyle val7 = new GUIStyle(_small);
			val7.alignment = (TextAnchor)4;
			val7.fontSize = 15;
			GUI.Label(new Rect(0f, UiKit.H * 0.4f + 50f, UiKit.W, 24f), "击杀 " + GameLoop.I.killCount + " 只", val7);
		}
	}

	private void DrawTuning()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		float num = 268f;
		Rect r = new Rect(UiKit.W - num - 16f, 16f, num, 232f);
		Fill(r, new Color(0.1f, 0.11f, 0.14f, 0.93f));
		Fill(new Rect(r.x, r.y, r.width, 2f), new Color(0.45f, 0.8f, 0.62f));
		GUI.Label(new Rect(r.x + 12f, r.y + 8f, num - 24f, 22f), "手感调参（实时生效）", _small);
		float num2 = r.y + 34f;
		HitFeel.StopMs = Slider(r.x + 12f, num2, num - 24f, "顿帧", HitFeel.StopMs, 0f, 140f, "ms");
		num2 += 40f;
		HitFeel.ShakeAmp = Slider(r.x + 12f, num2, num - 24f, "震屏", HitFeel.ShakeAmp, 0f, 0.5f, "");
		num2 += 40f;
		HitFeel.Knockback = Slider(r.x + 12f, num2, num - 24f, "击退", HitFeel.Knockback, 0f, 2.5f, "");
		num2 += 40f;
		HitFeel.FlashDur = Slider(r.x + 12f, num2, num - 24f, "闪白", HitFeel.FlashDur, 0f, 0.2f, "s");
		num2 += 40f;
		if (GUI.Button(new Rect(r.x + 12f, num2, 78f, 24f), "轻快"))
		{
			ApplyPreset(35f, 0.1f, 0.55f);
		}
		if (GUI.Button(new Rect(r.x + 98f, num2, 78f, 24f), "标准"))
		{
			ApplyPreset(60f, 0.18f, 0.9f);
		}
		if (GUI.Button(new Rect(r.x + 184f, num2, 72f, 24f), "厚重"))
		{
			ApplyPreset(95f, 0.26f, 1.4f);
		}
	}

	private void ApplyPreset(float stop, float shake, float kb)
	{
		HitFeel.StopMs = stop;
		HitFeel.ShakeAmp = shake;
		HitFeel.Knockback = kb;
	}

	private float Slider(float x, float y, float w, string label, float v, float min, float max, string unit)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected Obj, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(new Rect(x, y, 60f, 18f), label, _small);
		GUIStyle val = new GUIStyle(_small);
		val.alignment = (TextAnchor)5;
		GUI.Label(new Rect(x + w - 80f, y, 80f, 18f), v.ToString("0.00") + unit, val);
		return GUI.HorizontalSlider(new Rect(x, y + 19f, w, 16f), v, min, max);
	}
}
