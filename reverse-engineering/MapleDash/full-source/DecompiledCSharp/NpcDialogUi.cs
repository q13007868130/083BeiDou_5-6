using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NpcDialogUi
{
	private const string BtnRoot = "UI/UIWindow.img/UtilDlgEx/";

	private static float scrollY;

	private static GUIStyle title;

	private static GUIStyle hint;

	private static GUIStyle inputStyle;

	private static int talkKeyFrame = -1;

	public const int NameFontSize = 12;

	public const float NameExtraWidth = 5f;

	public const float NamePlateHeight = 16f;

	public const float NameTextX = 2f;

	public const float NameGapPx = 2f;

	private static GUIStyle nameStyle;

	private static readonly Color NameBg = new Color(0f, 0f, 0f, 160f / 255f);

	private static readonly Color NameText = Color.yellow;

	private static GUIStyle characterNameStyle;

	private static readonly Dictionary<string, float> characterNameWidths = new Dictionary<string, float>();

	private static readonly Color CharacterNameText = Color.white;

	private static readonly HashSet<int> partyMemberIds = new HashSet<int>();

	private static string measuredLine;

	private static float measuredLineWidth;

	public const int DefaultSpeaker = 9010000;

	private static readonly Dictionary<int, ImgNode> portraitCache = new Dictionary<int, ImgNode>();

	public static float WindowScale => BottomBar.S;

	private static bool CanClickField
	{
		get
		{
			if (!GameWindows.AnyVisible && !ShopWindow.Visible && !GameSideMenu.Blocking && !VirtualControlsEditor.Visible && !KeyConfigWindow.Visible && !VirtualControls.BlockingPointer && !NpcSession.Blocking && (Object)(object)GameLoop.I != (Object)null && !GameLoop.I.gameOver && (Object)(object)GameLoop.I.field != (Object)null)
			{
				return !PlayerCtrl.InputLocked;
			}
			return false;
		}
	}

	public static void ConsumeTalkKeyFrame()
	{
		talkKeyFrame = Time.frameCount;
	}

	public static bool ContinueWithTalkKey()
	{
		if (talkKeyFrame == Time.frameCount)
		{
			return false;
		}
		talkKeyFrame = Time.frameCount;
		NpcSession active = NpcSession.Active;
		if (active == null || !active.Dialog.Visible)
		{
			return false;
		}
		NpcDialog dialog = active.Dialog;
		if (dialog.Mode == NpcDialogMode.Style)
		{
			if (dialog.Styles.Length == 0)
			{
				return false;
			}
			active.Reply(1, dialog.StyleIndex);
			return true;
		}
		if (dialog.Mode == NpcDialogMode.Simple)
		{
			return ConfirmSelection(active);
		}
		if (!dialog.CanConfirmWithTalkKey && dialog.Mode != NpcDialogMode.YesNo && dialog.Mode != NpcDialogMode.AcceptDecline)
		{
			return false;
		}
		active.Reply(1);
		return true;
	}

	private static bool ConfirmSelection(NpcSession session)
	{
		int selectedLink = session.Dialog.SelectedLink;
		if (selectedLink >= 0 && DialogFlow.Links(session.Dialog.Content).Contains(selectedLink))
		{
			session.Reply(1, selectedLink);
			return true;
		}
		foreach (DialogRun run in session.Dialog.Content.Runs)
		{
			if (run.SelectionMarker && run.Link >= 0)
			{
				session.Reply(1, run.Link);
				return true;
			}
		}
		foreach (DialogSelection selection in session.Dialog.Selections)
		{
			if (selection.HeaderArt == null && selection.Index >= 0)
			{
				session.Reply(1, selection.Index);
				return true;
			}
		}
		return false;
	}

	public static bool MoveSelection(int direction)
	{
		NpcSession active = NpcSession.Active;
		if (active == null || !active.Dialog.Visible || active.Dialog.Mode != NpcDialogMode.Simple)
		{
			return false;
		}
		List<int> list = DialogFlow.Links(active.Dialog.Content);
		if (list.Count == 0)
		{
			return false;
		}
		int num = list.IndexOf(active.Dialog.SelectedLink);
		num = ((num >= 0) ? Mathf.Clamp(num + direction, 0, list.Count - 1) : 0);
		active.Dialog.SelectedLink = list[num];
		active.Dialog.RevealSelection = true;
		return true;
	}

	public static void Draw()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		NpcSession.Tick();
		OnlineTime.Tick();
		PqNpcDriver.Tick();
		NpcSession active = NpcSession.Active;
		if (!NpcSession.Silent && active != null && active.Dialog.Visible)
		{
			Matrix4x4 matrix = GUI.matrix;
			Vector2 pos = Pointer.Pos;
			float windowScale = WindowScale;
			try
			{
				GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(windowScale, windowScale, 1f));
				Pointer.Pos = pos / windowScale;
				DrawDialog(active);
			}
			finally
			{
				GUI.matrix = matrix;
				Pointer.Pos = pos;
			}
		}
	}

	public static void DrawField()
	{
		DrawCharacterNames();
		DrawNpcNames();
		DrawMarkers();
		DrawPlayerQuestAlert();
		HandleNpcClick();
	}

	public static void NamePlateRects(Vector2 anchor, Vector2 textSize, out Rect plate, out Rect text)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Ceil(textSize.x) + 5f;
		plate = new Rect(anchor.x - Mathf.Floor(num * 0.5f), anchor.y + 2f, num, 16f);
		text = new Rect(plate.x + 2f, plate.y, num - 5f, 16f);
	}

	public static void DrawCharacterNames()
	{
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected Obj, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected Obj, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected Obj, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if (!GameSession.InGame || (Object)(object)main == (Object)null)
		{
			return;
		}
		GameLoop i = GameLoop.I;
		if ((Object)(object)i == (Object)null)
		{
			return;
		}
		if (characterNameStyle == null)
		{
			characterNameStyle = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				fontStyle = (FontStyle)0,
				alignment = (TextAnchor)0,
				padding = new RectOffset(),
				margin = new RectOffset(),
				wordWrap = false,
				clipping = (TextClipping)1
			};
		}
		partyMemberIds.Clear();
		BotPartyService.Party party = ((GameServer.Local != null && GameServer.Local.Social != null) ? GameServer.Local.Social.Parties.For(GameSession.CharacterId) : null);
		if (party != null)
		{
			foreach (int member in party.members)
			{
				partyMemberIds.Add(member);
			}
		}
		float num = UiKit.H / (2f * main.orthographicSize * 40f);
		Matrix4x4 matrix = GUI.matrix;
		try
		{
			GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
			PlayerCtrl player = i.player;
			float num2 = (partyMemberIds.Contains(GameSession.CharacterId) ? Mathf.Max(0f, ((Object)(object)player != (Object)null) ? player.hp : 0f) : 0f);
			float num3 = ((num2 > 0f && (Object)(object)player != (Object)null) ? player.hpMax : 0f);
			if ((Object)(object)player != (Object)null && player.hp > 0f && !string.IsNullOrEmpty(Profile.playerName))
			{
				DrawCharacterName(main, num, player.FeetPos, Profile.playerName, local: true, num3 > 0f, num2, num3);
			}
			BotField botField = (((Object)(object)i.field != (Object)null) ? ((Component)i.field).GetComponent<BotField>() : null);
			FieldMap fieldMap = (((Object)(object)i.field != (Object)null) ? i.field.Map : null);
			if (!((Object)(object)botField != (Object)null) || fieldMap == null)
			{
				return;
			}
			foreach (BotCharacter item in BotRegistry.OnMap(i.field.BotMapId))
			{
				if (item.online && !string.IsNullOrEmpty(item.name) && botField.TryFeet(item.id, out var feet))
				{
					float partyHp = 0f;
					float num4 = 0f;
					if (partyMemberIds.Contains(item.id) && item != null)
					{
						BotCharacter botCharacter = item;
						partyHp = botCharacter.hp;
						num4 = botCharacter.maxHp;
					}
					DrawCharacterName(main, num, Vector2.op_Implicit(feet), item.name, local: false, num4 > 0f, partyHp, num4);
				}
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private static void DrawPartyHpBar(Vector2 feet, float hp, float maxHp)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		Rect r = new Rect(feet.x - 26f, feet.y - 89f, 52f, 10f);
		UiKit.Fill(r, Color.black);
		UiKit.Fill(new Rect(r.x + 1f, r.y + 1f, 50f, 8f), Color.white);
		UiKit.Fill(new Rect(r.x + 2f, r.y + 2f, 48f, 6f), Color.black);
		int num = ((maxHp > 0f) ? Mathf.FloorToInt(Mathf.Clamp01(hp / maxHp) * 46f) : 0);
		if (num > 0)
		{
			UiKit.Fill(new Rect(r.x + 3f, r.y + 3f, (float)num, 4f), Color.red);
			UiKit.Fill(new Rect(r.x + 3f, r.y + 6f, (float)num, 1f), Color32.op_Implicit(new Color32((byte)160, (byte)3, (byte)0, byte.MaxValue)));
		}
	}

	private static void DrawCharacterName(Camera cam, float scale, Vector2 feet, string name, bool local = false, bool partyMember = false, float partyHp = 0f, float partyHpMax = 0f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected Obj, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected Obj, but got Unknown
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected Obj, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Expected Obj, but got Unknown
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0516: Unknown result type (might be due to invalid IL or missing references)
		//IL_051b: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = cam.WorldToScreenPoint(new Vector3(feet.x, feet.y, 0f));
		if (val.z < 0f)
		{
			return;
		}
		Vector2 val2 = new Vector2(val.x / UiKit.Scale, ((float)Screen.height - val.y) / UiKit.Scale) / scale;
		if (val2.x < -60f || val2.x > UiKit.W / scale + 60f || val2.y < -30f || val2.y > UiKit.H / scale + 60f)
		{
			return;
		}
		if (!characterNameWidths.TryGetValue(name, out var value))
		{
			value = characterNameStyle.CalcSize(new GUIContent(name)).x;
			characterNameWidths[name] = value;
		}
		NamePlateRects(val2, new Vector2(value, 12f), out var plate, out var text);
		int num = (local ? NativeNameTag.RingStyle("nameTag") : 0);
		if (num == 0 || !NativeNameTag.Draw("UI/NameTag.img/" + num, name, new Vector2(val2.x, val2.y + 2f), characterNameStyle, out plate))
		{
			GUIStyle val3 = characterNameStyle;
			if (num == 1)
			{
				val3 = new GUIStyle(characterNameStyle)
				{
					fontStyle = (FontStyle)1
				};
				value = val3.CalcSize(new GUIContent(name)).x;
			}
			NamePlateRects(val2, new Vector2(value, 12f), out plate, out text);
			if (num == 1)
			{
				Color32 val4 = new Color32((byte)44, (byte)128, (byte)173, byte.MaxValue);
				UiKit.Fill(new Rect(plate.x + 1f, plate.y + 1f, plate.width - 2f, plate.height - 2f), Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)178)));
				UiKit.Fill(new Rect(plate.x + 1f, plate.y, plate.width - 2f, 1f), Color32.op_Implicit(val4));
				UiKit.Fill(new Rect(plate.x + 1f, plate.yMax - 1f, plate.width - 2f, 1f), Color32.op_Implicit(val4));
				UiKit.Fill(new Rect(plate.x, plate.y + 1f, 1f, plate.height - 2f), Color32.op_Implicit(val4));
				UiKit.Fill(new Rect(plate.xMax - 1f, plate.y + 1f, 1f, plate.height - 2f), Color32.op_Implicit(val4));
				val3.normal.textColor = Color.black;
				GUI.Label(text, name, val3);
			}
			else
			{
				UiKit.Fill(new Rect(plate.x, plate.y + 1f, plate.width, plate.height - 2f), NameBg);
				UiKit.Fill(new Rect(plate.x + 1f, plate.y, plate.width - 2f, 1f), NameBg);
				UiKit.Fill(new Rect(plate.x + 1f, plate.yMax - 1f, plate.width - 2f, 1f), NameBg);
				characterNameStyle.normal.textColor = CharacterNameText;
				GUI.Label(text, name, characterNameStyle);
			}
		}
		if ((!local & partyMember) && partyHpMax > 0f)
		{
			DrawPartyHpBar(val2, partyHp, partyHpMax);
		}
		if (!local)
		{
			return;
		}
		float num2 = plate.yMax - 1f;
		string text2 = NativeNameplate.LineFor(GameLoop.I?.player);
		if (!string.IsNullOrEmpty(text2) && DrawPlateLine(new Vector2(val2.x, num2), text2, out var plate2))
		{
			num2 = plate2.yMax - 1f;
		}
		int num3 = Profile.EquipOfSlot(-49);
		if (num3 > 0)
		{
			int num4 = EquipCatalog.Info(num3)?.Int("medalTag") ?? 0;
			string text3 = WzNames.Item(num3);
			if (!string.IsNullOrEmpty(text3) && !NativeNameTag.Draw("UI/NameTag.img/medal/" + num4, text3, new Vector2(val2.x, num2 + 2f), characterNameStyle, out var _))
			{
				NamePlateRects(new Vector2(val2.x, num2), new Vector2(characterNameStyle.CalcSize(new GUIContent(text3)).x, 12f), out var plate3, out var text4);
				UiKit.Fill(new Rect(plate3.x, plate3.y + 1f, plate3.width, plate3.height - 2f), NameBg);
				UiKit.Fill(new Rect(plate3.x + 1f, plate3.y, plate3.width - 2f, 1f), NameBg);
				UiKit.Fill(new Rect(plate3.x + 1f, plate3.yMax - 1f, plate3.width - 2f, 1f), NameBg);
				characterNameStyle.normal.textColor = CharacterNameText;
				GUI.Label(text4, text3, characterNameStyle);
			}
		}
	}

	private static bool DrawPlateLine(Vector2 anchor, string text, out Rect plate)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		NamePlateRects(anchor, new Vector2(MeasureLineWidth(text), 12f), out plate, out var text2);
		UiKit.Fill(new Rect(plate.x, plate.y + 1f, plate.width, plate.height - 2f), NameBg);
		UiKit.Fill(new Rect(plate.x + 1f, plate.y, plate.width - 2f, 1f), NameBg);
		UiKit.Fill(new Rect(plate.x + 1f, plate.yMax - 1f, plate.width - 2f, 1f), NameBg);
		characterNameStyle.normal.textColor = CharacterNameText;
		GUI.Label(text2, text, characterNameStyle);
		return true;
	}

	private static float MeasureLineWidth(string text)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (text == measuredLine)
		{
			return measuredLineWidth;
		}
		measuredLine = text;
		measuredLineWidth = characterNameStyle.CalcSize(new GUIContent(text)).x;
		return measuredLineWidth;
	}

	private static void DrawNpcNames()
	{
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected Obj, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected Obj, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Expected Obj, but got Unknown
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		FieldMap map = NpcField.Map;
		Camera main = Camera.main;
		if (map == null || (Object)(object)main == (Object)null)
		{
			return;
		}
		if (nameStyle == null)
		{
			nameStyle = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 12,
				fontStyle = (FontStyle)1,
				alignment = (TextAnchor)0,
				padding = new RectOffset(),
				margin = new RectOffset(),
				wordWrap = false,
				clipping = (TextClipping)1
			};
		}
		float num = UiKit.H / (2f * main.orthographicSize * 40f);
		Matrix4x4 matrix = GUI.matrix;
		try
		{
			GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
			for (int i = 0; i < map.npcs.Count; i++)
			{
				FieldNpc fieldNpc = map.npcs[i];
				if (fieldNpc.hide != 0)
				{
					continue;
				}
				string text = WzNames.Npc(fieldNpc.id);
				if (string.IsNullOrEmpty(text) || fieldNpc.id <= 0)
				{
					continue;
				}
				float num2 = map.WorldX(fieldNpc.x);
				float num3 = map.WorldY(fieldNpc.y);
				Vector3 val = main.WorldToScreenPoint(new Vector3(num2, num3, 0f));
				if (!(val.z < 0f))
				{
					Vector2 val2 = new Vector2(val.x / UiKit.Scale, ((float)Screen.height - val.y) / UiKit.Scale) / num;
					if (!(val2.x < -60f) && !(val2.x > UiKit.W / num + 60f) && !(val2.y < -30f) && !(val2.y > UiKit.H / num + 60f))
					{
						Vector2 textSize = nameStyle.CalcSize(new GUIContent(text));
						NamePlateRects(val2, textSize, out var plate, out var text2);
						UiKit.Fill(new Rect(plate.x, plate.y + 1f, plate.width, plate.height - 2f), NameBg);
						UiKit.Fill(new Rect(plate.x + 1f, plate.y, plate.width - 2f, 1f), NameBg);
						UiKit.Fill(new Rect(plate.x + 1f, plate.yMax - 1f, plate.width - 2f, 1f), NameBg);
						nameStyle.normal.textColor = NameText;
						GUI.Label(text2, text, nameStyle);
					}
				}
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private static void DrawMarkers()
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		FieldMap map = NpcField.Map;
		Camera main = Camera.main;
		if (map == null || (Object)(object)main == (Object)null)
		{
			return;
		}
		EnsureStyles();
		for (int i = 0; i < map.npcs.Count; i++)
		{
			FieldNpc fieldNpc = map.npcs[i];
			if (fieldNpc.hide != 0)
			{
				continue;
			}
			NpcField.Marker marker = NpcField.MarkerOf(fieldNpc.id);
			if (!marker.start && !marker.complete && !marker.progress)
			{
				continue;
			}
			float num = map.WorldX(fieldNpc.x);
			float num2 = map.WorldY(fieldNpc.y) + (fieldNpc.head + 26f) / 40f;
			Vector3 val = main.WorldToScreenPoint(new Vector3(num, num2, 0f));
			if (val.z < 0f)
			{
				continue;
			}
			Vector2 val2 = new Vector2(val.x / UiKit.Scale, ((float)Screen.height - val.y) / UiKit.Scale);
			if (!(val2.x < -40f) && !(val2.x > UiKit.W + 40f) && !(val2.y < -40f) && !(val2.y > UiKit.H + 40f))
			{
				string text;
				if (marker.complete)
				{
					text = "2";
				}
				else
				{
					text = (marker.start ? "0" : "1");
				}
				string path = "UI/UIWindow.img/QuestIcon/" + text;
				DrawQuestIcon(path, val2);
				if (ClickQuestIcon(QuestIconRect(path, val2, 0f)))
				{
					NpcField.Interact(fieldNpc.id);
				}
			}
		}
	}

	private static Rect DrawQuestIcon(string path, Vector2 anchor, float elapsedMs = -1f)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		List<ImgNode> list = WzAssets.Frames(WzAssets.Node(path));
		if (list.Count == 0)
		{
			return default;
		}
		float num = 0f;
		foreach (ImgNode item in list)
		{
			num += (float)Mathf.Max(1, item.Int("delay", 120));
		}
		float time = ((elapsedMs >= 0f) ? Mathf.Repeat(elapsedMs, num) : Mathf.Repeat(Time.unscaledTime * 1000f, num));
		ImgNode frame = FrameAt(list, time);
		Rect val = FrameRect(frame, anchor);
		GUI.DrawTexture(val, (Texture)(object)WzAssets.Texture(frame));
		return val;
	}

	private static ImgNode FrameAt(List<ImgNode> frames, float time)
	{
		ImgNode result = frames[0];
		foreach (ImgNode frame in frames)
		{
			result = frame;
			time -= (float)Mathf.Max(1, frame.Int("delay", 120));
			if (time < 0f)
			{
				break;
			}
		}
		return result;
	}

	private static Rect QuestIconRect(string path, Vector2 anchor, float elapsedMs = -1f)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		List<ImgNode> list = WzAssets.Frames(WzAssets.Node(path));
		if (list.Count == 0)
		{
			return default;
		}
		float num = 0f;
		foreach (ImgNode item in list)
		{
			num += (float)Mathf.Max(1, item.Int("delay", 120));
		}
		float time = ((elapsedMs >= 0f) ? Mathf.Repeat(elapsedMs, num) : Mathf.Repeat(Time.unscaledTime * 1000f, num));
		return FrameRect(FrameAt(list, time), anchor);
	}

	private static Rect FrameRect(ImgNode frame, Vector2 anchor)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = WzAssets.Texture(frame);
		Vector2 val2 = WzAssets.Origin(frame);
		float num = (float)Screen.height / 768f / UiKit.Scale;
		return new Rect(anchor.x - val2.x * num, anchor.y - val2.y * num, (float)((Texture)val).width * num, (float)((Texture)val).height * num);
	}

	private static bool ClickQuestIcon(Rect rect)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7 || !Pointer.Clicked || !CanClickField || !rect.Contains(Pointer.Pos))
		{
			return false;
		}
		Pointer.ConsumeClick();
		return true;
	}

	private static void HandleNpcClick()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type == 7 && Pointer.DoubleClicked && CanClickField && !(Pointer.Pos.y >= UiKit.H - BottomBar.Height))
		{
			FieldNpc fieldNpc = NpcField.Pick(Pointer.Pos);
			if (fieldNpc != null)
			{
				Pointer.ConsumeClick();
				NpcField.Interact(fieldNpc.id);
			}
		}
	}

	private static void DrawPlayerQuestAlert()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		FieldMap map = NpcField.Map;
		Camera main = Camera.main;
		if ((Object)(object)playerCtrl == (Object)null || map == null || (Object)(object)main == (Object)null || !NpcField.AlertAnim(out var path, out var elapsedMs))
		{
			return;
		}
		Vector3 val = main.WorldToScreenPoint(new Vector3(playerCtrl.FeetPos.x, playerCtrl.FeetPos.y + 1.3f, 0f));
		if (!(val.z < 0f))
		{
			Vector2 anchor = new Vector2(val.x / UiKit.Scale, ((float)Screen.height - val.y) / UiKit.Scale);
			DrawQuestIcon(path, anchor, elapsedMs);
			if (ClickQuestIcon(QuestIconRect("Effect/BasicEff.img/QuestAlert/Default", anchor, 0f)))
			{
				NpcField.ShowQuestAlerts();
			}
		}
	}

	private static void DrawDialog(NpcSession session)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		EnsureStyles();
		if (session.Dialog.ScrollReset)
		{
			scrollY = 0f;
			session.Dialog.ScrollReset = false;
		}
		Event current = Event.current;
		if ((int)current.type == 4 && (int)current.keyCode == 27)
		{
			session.Cancel();
			current.Use();
			return;
		}
		if ((int)current.type == 4 && (int)current.keyCode != 0 && current.keyCode == KeyMap.Of(GameAction.NpcTalk))
		{
			ContinueWithTalkKey();
			current.Use();
			return;
		}
		if ((int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271) && ContinueWithTalkKey())
		{
			current.Use();
			return;
		}
		if (session.Dialog.Mode == NpcDialogMode.Style)
		{
			NativeNpcStyleWindow.Draw(session);
			return;
		}
		float num = 529f;
		DialogContent content = session.Dialog.Content;
		float num2;
		if (session.Dialog.Mode == NpcDialogMode.Simple)
		{
			num2 = 24f;
		}
		else
		{
			num2 = ((session.Dialog.Mode == NpcDialogMode.Next || session.Dialog.Mode == NpcDialogMode.Prev || session.Dialog.Mode == NpcDialogMode.NextPrev) ? 18f : 0f);
		}
		bool flag = session.Dialog.Mode == NpcDialogMode.GetText || session.Dialog.Mode == NpcDialogMode.GetNumber;
		float num3 = DialogFlow.ContentHeight(content, num, flag) + num2;
		float num4 = (flag ? Mathf.Max(120f, num3) : DialogFlow.BodyHeight(num3)) + 86f;
		Rect val = new Rect((UiKit.W / WindowScale - num) * 0.5f, (UiKit.H / WindowScale - num4) * 0.5f, num, num4);
		NativeUi.DialogPanel(val);
		DrawPortrait(session.NpcId, val, session.SpeakerName);
		Vector2 val2 = BtnSize("BtClose");
		float num5 = ((session.Dialog.Mode == NpcDialogMode.Simple) ? 10f : 9f);
		if (NativeUi.Button(new Rect(val.x + num5, val.yMax - 26f, val2.x, val2.y), "UI/UIWindow.img/UtilDlgEx/BtClose"))
		{
			session.Cancel();
			return;
		}
		int num6 = DialogFlow.DrawPane(val, content, ref scrollY, num2, flag, ref session.Dialog.SelectedLink, session.Dialog.RevealSelection);
		session.Dialog.RevealSelection = false;
		if (num6 >= 0)
		{
			session.Reply(1, num6);
			return;
		}
		float right = val.xMax - 8f;
		float y = val.yMax - 26f;
		float y2 = val.yMax - 77f;
		float right2 = val.xMax - 32f - (DialogFlow.NeedsScroll(num3) ? 8f : 0f);
		switch (session.Dialog.Mode)
		{
		case NpcDialogMode.Next:
			if (Button(ref right2, y2, "BtNext"))
			{
				session.Reply(1);
			}
			break;
		case NpcDialogMode.Prev:
			if (PageButton(val, num3, "BtPrev", 122f))
			{
				session.Reply(0);
			}
			else if (Button(ref right, y, "BtOK"))
			{
				session.Reply(1);
			}
			break;
		case NpcDialogMode.NextPrev:
			if (Button(ref right2, y2, "BtNext"))
			{
				session.Reply(1);
			}
			if (PageButton(val, num3, "BtPrev", 122f))
			{
				session.Reply(0);
			}
			break;
		case NpcDialogMode.Ok:
			if (Button(ref right, y, "BtOK"))
			{
				session.Reply(1);
			}
			break;
		case NpcDialogMode.YesNo:
			if (FooterButton(val, "BtNo", 54f))
			{
				session.Reply(0);
			}
			if (FooterButton(val, "BtYes", 108f))
			{
				session.Reply(1);
			}
			break;
		case NpcDialogMode.AcceptDecline:
			if (FooterButton(val, "BtQNo", 65f))
			{
				session.Reply(0);
			}
			if (FooterButton(val, "BtQYes", 130f))
			{
				session.Reply(1);
			}
			break;
		case NpcDialogMode.GetText:
		case NpcDialogMode.GetNumber:
			DrawInput(session, val, num3);
			break;
		case NpcDialogMode.Simple:
			if (PageButton(val, num3, "BtNext", 68f))
			{
				ConfirmSelection(session);
			}
			break;
		}
	}

	private static ImgNode PortraitNode(int id)
	{
		if (id <= 0)
		{
			return null;
		}
		if (portraitCache.TryGetValue(id, out var value))
		{
			return value;
		}
		int key = id;
		ImgNode imgNode = null;
		HashSet<int> hashSet = new HashSet<int>();
		while (id > 0 && hashSet.Add(id))
		{
			string relative = "Npc/" + id.ToString("D7") + ".img";
			if (!WzAssets.HasFile(relative))
			{
				break;
			}
			ImgNode imgNode2 = WzAssets.FileRoot(relative);
			if (imgNode2.Get("info/link") != null)
			{
				id = imgNode2.Int("info/link");
				continue;
			}
			imgNode = ((WzAssets.Frames(imgNode2.Get("stand")).Count > 0) ? imgNode2 : null);
			break;
		}
		portraitCache[key] = imgNode;
		return imgNode;
	}

	private static void DrawPortrait(int npcId, Rect rect, string speakerName = null)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = PortraitNode(npcId);
		bool flag = imgNode == null;
		if (flag)
		{
			imgNode = PortraitNode(9010000);
		}
		if (imgNode == null)
		{
			return;
		}
		List<ImgNode> list = WzAssets.Frames(imgNode.Get("stand"));
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		bool flag2 = true;
		foreach (ImgNode item in list)
		{
			Texture2D val = WzAssets.Texture(item);
			Vector2 val2 = WzAssets.Origin(item);
			if (!((Object)(object)val == (Object)null))
			{
				if (flag2)
				{
					num = 0f - val2.x;
					num2 = 0f - val2.y;
					num3 = (float)((Texture)val).width - val2.x;
					num4 = (float)((Texture)val).height - val2.y;
					flag2 = false;
				}
				else
				{
					num = Mathf.Min(num, 0f - val2.x);
					num2 = Mathf.Min(num2, 0f - val2.y);
					num3 = Mathf.Max(num3, (float)((Texture)val).width - val2.x);
					num4 = Mathf.Max(num4, (float)((Texture)val).height - val2.y);
				}
			}
		}
		float num5 = num4 - num2;
		float num6 = num5 + 23f;
		float num7 = rect.height - 86f;
		float num8 = ((num6 <= num7) ? (28f + (float)(int)((num7 - num6) / 2f)) : (rect.height - num6 - 62f));
		if (list.Count > 0)
		{
			float num9 = 0f;
			foreach (ImgNode item2 in list)
			{
				num9 += (float)Mathf.Max(1, Mathf.Abs(item2.Int("delay", 120)));
			}
			float num10 = Mathf.Repeat(Time.unscaledTime * 1000f, num9);
			ImgNode frame = list[0];
			foreach (ImgNode item3 in list)
			{
				frame = item3;
				num10 -= (float)Mathf.Max(1, Mathf.Abs(item3.Int("delay", 120)));
				if (num10 < 0f)
				{
					break;
				}
			}
			Texture2D val3 = WzAssets.Texture(frame);
			Vector2 val4 = WzAssets.Origin(frame);
			GUI.DrawTexture(new Rect(rect.x + 80f - (num + num3) * 0.5f - val4.x, rect.y + num8 + 7f - num2 - val4.y, (float)((Texture)val3).width, (float)((Texture)val3).height), (Texture)(object)val3);
		}
		Texture2D val5 = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx/bar");
		Rect val6 = new Rect(rect.x + 20f, rect.y + num8 + num5 + 3f, 121f, 19f);
		GUI.DrawTexture(val6, (Texture)(object)val5);
		title.fontSize = 12;
		title.alignment = (TextAnchor)4;
		title.normal.textColor = Color.white;
		string text = speakerName;
		if (string.IsNullOrEmpty(text) && !flag)
		{
			text = WzNames.NpcOrNull(npcId);
		}
		if (string.IsNullOrEmpty(text))
		{
			text = WzNames.Npc(9010000);
		}
		GUI.Label(val6, text, title);
	}

	private static void DrawInput(NpcSession session, Rect rect, float contentH)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected Obj, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected Obj, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected Obj, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Invalid comparison between Unknown and I4
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Invalid comparison between Unknown and I4
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Invalid comparison between Unknown and I4
		float num = 28f + (float)(int)((rect.height - 86f - contentH - 20f) / 2f);
		float num2 = rect.y + num + contentH;
		UiKit.Fill(new Rect(rect.x + 156f, num2 + 2f, rect.width - 311f, 18f), Color.black);
		UiKit.Fill(new Rect(rect.x + 157f, num2 + 3f, rect.width - 313f, 16f), Color.white);
		Rect val = new Rect(rect.x + 159f, num2 + 5f, rect.width - 316f, 14f);
		if (inputStyle == null)
		{
			inputStyle = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				alignment = (TextAnchor)0,
				padding = new RectOffset(),
				margin = new RectOffset(),
				clipping = (TextClipping)1
			};
			inputStyle.normal.textColor = Color.black;
		}
		GUI.SetNextControlName("npcInput");
		string s;
		if (session.Dialog.Mode == NpcDialogMode.GetNumber)
		{
			s = GUI.TextField(val, session.Dialog.Input, inputStyle);
			s = FilterDigits(s);
		}
		else
		{
			s = GUI.TextField(val, session.Dialog.Input, 120, inputStyle);
		}
		session.Dialog.Input = s;
		Event current = Event.current;
		bool flag = (int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271);
		Vector2 val2 = BtnSize("BtOK");
		if (!(NativeUi.Button(new Rect(rect.xMax - 54f, rect.yMax - 26f, val2.x, val2.y), "UI/UIWindow.img/UtilDlgEx/BtOK") | flag))
		{
			return;
		}
		int result = -1;
		if (session.Dialog.Mode == NpcDialogMode.GetNumber && (!int.TryParse(s, out result) || result < session.Dialog.NumberMin || result > session.Dialog.NumberMax))
		{
			BottomBar.Toast("请输入 " + session.Dialog.NumberMin + " 到 " + session.Dialog.NumberMax + " 之间的数字");
		}
		else
		{
			if (flag)
			{
				current.Use();
			}
			session.Reply(1, result);
		}
	}

	private static string FilterDigits(string s)
	{
		string text = "";
		for (int i = 0; i < s.Length; i++)
		{
			if (text.Length >= 11)
			{
				break;
			}
			if (char.IsDigit(s[i]) || (i == 0 && s[i] == '-'))
			{
				text += s[i];
			}
		}
		return text;
	}

	private static bool FooterButton(Rect window, string node, float inset)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = BtnSize(node);
		return NativeUi.Button(new Rect(window.xMax - inset, window.yMax - 26f, val.x, val.y), "UI/UIWindow.img/UtilDlgEx/" + node);
	}

	private static bool PageButton(Rect window, float contentH, string node, float inset)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = BtnSize(node);
		return NativeUi.Button(new Rect(window.xMax - 10f - inset - (DialogFlow.NeedsScroll(contentH) ? 8f : 0f), window.yMax - 77f, val.x, val.y), "UI/UIWindow.img/UtilDlgEx/" + node);
	}

	private static bool Button(ref float right, float y, string node)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = BtnSize(node);
		Rect rect = new Rect(right - val.x, y, val.x, val.y);
		right -= val.x + 6f;
		return NativeUi.Button(rect, "UI/UIWindow.img/UtilDlgEx/" + node);
	}

	private static Vector2 BtnSize(string node)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/UtilDlgEx/" + node + "/normal/0");
		if (!((Object)(object)val != (Object)null))
		{
			return new Vector2(58f, 22f);
		}
		return new Vector2((float)((Texture)val).width, (float)((Texture)val).height);
	}

	private static void EnsureStyles()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected Obj, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected Obj, but got Unknown
		if (title == null)
		{
			title = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12
			};
			hint = new GUIStyle
			{
				fontSize = 11,
				alignment = (TextAnchor)4
			};
		}
	}

	public static void DrawHint()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		if (NpcSession.Active != null && NpcSession.Active.Dialog.Visible)
		{
			return;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (!((Object)(object)playerCtrl == (Object)null))
		{
			FieldNpc fieldNpc = NpcField.Nearest(playerCtrl.FeetPos);
			if (fieldNpc != null)
			{
				EnsureStyles();
				hint.normal.textColor = new Color(1f, 0.95f, 0.7f);
				GUI.Label(new Rect(UiKit.W * 0.5f - 120f, UiKit.H - BottomBar.Height - 54f, 240f, 24f), "Space / 双击 与 " + WzNames.Npc(fieldNpc.id) + " 对话", hint);
			}
		}
	}

	static NpcDialogUi()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
	}
}
