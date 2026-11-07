using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public static class NativeVMatrixWindow
{
	private enum Dialog
	{
		None,
		Upgrade,
		Result,
		Transform,
		Gemstone,
		Register
	}

	private const string Img = "UI/VMatrixUI.img/";

	private const string SpecialIconFile = "Etc/_Canvas/VCore.img";

	private const float W = 506f;

	private const float H = 719f;

	private const float SpecialLeft = 414f;

	private const float SpecialTop = 172f;

	private const float SpecialW = 247f;

	private const float SpecialH = 402f;

	private const float SlotStepX = 78f;

	private const float SlotStepY = 75f;

	private static bool visible;

	private static bool dragging;

	private static bool positioned;

	private static bool specialOpen;

	private static Vector2 position;

	private static Vector2 grab;

	private static int owner;

	private static int selected = -1;

	private static VMatrixResponse state;

	private static string error;

	private static Dialog dialog;

	private static int dialogCore;

	private static int targetLevel;

	private static int transformCount = 1;

	private static int gemCount = 1;

	private static int gemItem;

	private static int specialScroll;

	private static int specialScrollStart = -1;

	private static bool specialBarDrag;

	private static float specialBarGrab;

	private static bool specialOwnsGesture;

	private static int resultBefore;

	private static int resultAfter;

	private static bool carrying;

	private static int carryCore;

	private static int carrySkill;

	private static int pressSlot = -1;

	private static readonly Dictionary<int, Texture2D> specialIcons = new Dictionary<int, Texture2D>();

	private const float LevelBarCenterX = 28.5f;

	private const float LevelBarTopY = 52f;

	private const float LevelDigitGap = 1f;

	public static bool Visible => visible;

	public static bool Carrying
	{
		get
		{
			if (visible)
			{
				return carrying;
			}
			return false;
		}
	}

	public static bool AtPointer
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (visible)
			{
				if (!dragging)
				{
					return Pointer.HitsWindow(WindowRect());
				}
				return true;
			}
			return false;
		}
	}

	private static float Width
	{
		get
		{
			if (!specialOpen)
			{
				return 506f;
			}
			return 661f;
		}
	}

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / Width, UiKit.H * 0.92f / 719f));

	private static Vector2 Free
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			Vector2 size = new Vector2(Width, 719f);
			if (positioned)
			{
				return NativeUi.ClampWindowPosition(position, size, S);
			}
			return NativeUi.ClampWindowPosition(new Vector2((UiKit.W / S - 506f) / 2f, (UiKit.H / S - 719f) / 2f), size, S);
		}
	}

	private static Rect WindowRect()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(Free.x * S, Free.y * S, Width * S, 719f * S);
	}

	private static Rect SpecialRect()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return GrowthUi.At(414f, 172f, 247f, 402f);
	}

	public static void Escape()
	{
		if (carrying)
		{
			CancelCarry();
		}
		else if (dialog != Dialog.None)
		{
			dialog = Dialog.None;
		}
		else if (specialOpen)
		{
			specialOpen = false;
			specialScroll = 0;
			specialBarDrag = false;
			specialOwnsGesture = false;
		}
		else
		{
			Close();
		}
	}

	public static void Close()
	{
		visible = (dragging = (positioned = (specialOpen = false)));
		state = null;
		error = null;
		selected = -1;
		dialog = Dialog.None;
		specialScroll = 0;
		specialScrollStart = -1;
		specialBarDrag = false;
		specialOwnsGesture = false;
		CancelCarry();
	}

	public static void Open()
	{
		Close();
		if (GameSession.InGame)
		{
			visible = true;
			owner = GameSession.CharacterId;
			Load();
		}
	}

	public static void OpenGemstone(int itemId)
	{
		if (!visible)
		{
			Open();
		}
		if (visible)
		{
			gemItem = itemId;
			gemCount = 1;
			dialog = Dialog.Gemstone;
		}
	}

	private static void Load()
	{
		VMatrixResponse vMatrixResponse = GameSession.Net.VMatrix(new VMatrixRequest
		{
			characterId = owner,
			operation = VMatrixOperation.Snapshot
		});
		if (vMatrixResponse != null)
		{
			state = vMatrixResponse;
			error = (vMatrixResponse.eligible ? null : vMatrixResponse.error);
			if (selected >= 0 && CoreOf(selected) == null)
			{
				selected = -1;
			}
			NativeSpecialCore.Observe(vMatrixResponse);
			VMatrixLevels.Capture(vMatrixResponse, owner);
		}
	}

	private static VMatrixResponse Send(VMatrixOperation operation, int core = 0, int target = 0, int item = 0, int count = 0, bool commit = true)
	{
		VMatrixResponse vMatrixResponse = GameSession.Net.VMatrix(new VMatrixRequest
		{
			characterId = owner,
			operation = operation,
			coreId = core,
			targetLevel = target,
			itemId = item,
			count = count,
			commit = commit
		});
		if (vMatrixResponse == null)
		{
			return null;
		}
		if (!vMatrixResponse.ok)
		{
			error = vMatrixResponse.error;
			BottomBar.Toast(vMatrixResponse.error);
			return vMatrixResponse;
		}
		error = null;
		if (commit)
		{
			state = vMatrixResponse;
			GameSession.ReloadAfterCommand();
			VMatrixLevels.Capture(vMatrixResponse, owner);
		}
		NativeSpecialCore.Observe(vMatrixResponse);
		return vMatrixResponse;
	}

	private static VMatrixCoreView CoreOf(int slot)
	{
		if (state?.slots == null || slot < 0 || slot >= state.slots.Length)
		{
			return null;
		}
		int num = state.slots[slot];
		if (num == 0)
		{
			return null;
		}
		return Find(num);
	}

	private static VMatrixCoreView Find(int coreId)
	{
		if (state?.cores == null)
		{
			return null;
		}
		foreach (VMatrixCoreView core in state.cores)
		{
			if (core.coreId == coreId)
			{
				VMatrixCore vMatrixCore = VMatrixCatalog.Core(coreId);
				if (vMatrixCore != null)
				{
					core.name = vMatrixCore.name;
					core.desc = vMatrixCore.desc;
					core.skills = vMatrixCore.skills;
					core.type = vMatrixCore.type;
					core.maxLevel = vMatrixCore.maxLevel;
				}
				return core;
			}
		}
		return null;
	}

	private static Vector2 SlotAnchor(int slot)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		string text;
		if (slot < 4)
		{
			text = "skill";
		}
		else
		{
			text = ((slot < 10) ? "enforce" : "common");
		}
		int num;
		if (slot < 4)
		{
			num = slot;
		}
		else
		{
			num = ((slot < 10) ? (slot - 4) : (slot - 10));
		}
		ImgNode imgNode = WzAssets.Node("UI/VMatrixUI.img/main/slotPos/" + text + "/" + num);
		if (imgNode != null && imgNode.TryVector("", out var x, out var y))
		{
			return new Vector2((float)x, (float)y);
		}
		int num2 = num % 6;
		float num3 = ((text == "skill") ? 172 : ((text == "enforce") ? 302 : (432 + 75 * (num / 6))));
		return new Vector2(63f + (float)num2 * 78f, num3);
	}

	private static Vector2 Vector(string path, float fallbackX, float fallbackY)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/VMatrixUI.img/" + path);
		if (imgNode == null || !imgNode.TryVector("", out var x, out var y))
		{
			return new Vector2(fallbackX, fallbackY);
		}
		return new Vector2((float)x, (float)y);
	}

	private static Texture2D SpecialIcon(int coreId, bool registered)
	{
		if (coreId <= 0 || !WzAssets.HasFile("Etc/_Canvas/VCore.img"))
		{
			return null;
		}
		int key = (coreId << 1) | (registered ? 1 : 0);
		if (specialIcons.TryGetValue(key, out var value))
		{
			return value;
		}
		ImgNode imgNode = WzAssets.Node("Etc/_Canvas/VCore.img/vSkill/CoreData/" + coreId + (registered ? "/icon" : "/iconDisabled"));
		Texture2D val = ((imgNode == null) ? null : WzAssets.Texture(imgNode));
		specialIcons[key] = val;
		return val;
	}

	public static void Draw()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		Rect val = WindowRect();
		bool atPointer = AtPointer;
		GrowthUi.S = S;
		GrowthUi.R = val;
		Rect val2;
		if (Pointer.Pressed)
		{
			int num;
			if (specialOpen)
			{
				val2 = SpecialRect();
				num = (val2.Contains(Pointer.Pos) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			specialOwnsGesture = (byte)num != 0;
		}
		else if (!Pointer.Down && !Pointer.Released)
		{
			specialOwnsGesture = false;
		}
		int num2;
		if (specialOpen)
		{
			if (!specialOwnsGesture)
			{
				val2 = SpecialRect();
				num2 = (val2.Contains(Pointer.Pos) ? 1 : 0);
			}
			else
			{
				num2 = 1;
			}
		}
		else
		{
			num2 = 0;
		}
		bool flag = (byte)num2 != 0;
		if (Pointer.Pressed)
		{
			pressSlot = ((dialog == Dialog.None && !flag) ? SlotAt(Pointer.Pos) : (-1));
		}
		else if (!Pointer.Down)
		{
			pressSlot = -1;
		}
		if (dialog == Dialog.None && !carrying && pressSlot < 0 && !flag && GrowthUi.Drag(ref dragging, ref positioned, ref position, ref grab, Width, 719f, WindowRect, 46f, 100f, typeof(NativeVMatrixWindow), 6f))
		{
			val = WindowRect();
		}
		GrowthUi.R = val;
		NativeUi.RegisterWindow("NativeVMatrixWindow", val, () => visible);
		if (Pointer.DragStarted && !carrying && pressSlot >= 0 && dialog == Dialog.None)
		{
			VMatrixCoreView vMatrixCoreView = CoreOf(pressSlot);
			if (vMatrixCoreView != null)
			{
				PickCore(pressSlot, vMatrixCoreView.coreId, vMatrixCoreView);
			}
		}
		if (carrying && Pointer.Clicked && pressSlot < 0)
		{
			CancelCarry();
		}
		MatrixPresentation.Begin();
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && dialog == Dialog.None;
		GrowthUi.Image("UI/VMatrixUI.img/main/", "backgrnd", 0f, 0f);
		DrawPoints();
		using (new Pointer.InputMask(flag))
		{
			DrawSlots();
		}
		DrawSpecialCore(flag);
		using (new Pointer.InputMask(flag))
		{
			DrawButtons();
		}
		if (dialog != Dialog.None)
		{
			MatrixPresentation.Begin();
		}
		GUI.enabled = enabled;
		switch (dialog)
		{
		case Dialog.Upgrade:
			DrawUpgrade();
			break;
		case Dialog.Result:
			DrawResult();
			break;
		case Dialog.Transform:
			DrawTransform();
			break;
		case Dialog.Gemstone:
			DrawGemstone();
			break;
		case Dialog.Register:
			DrawRegister();
			break;
		}
		MatrixPresentation.Draw();
		if (atPointer && GrowthUi.Repaint)
		{
			Pointer.ConsumeClick();
			Pointer.Pressed = (Pointer.RightPressed = (Pointer.RightReleased = false));
		}
		if (!string.IsNullOrEmpty(error))
		{
			GrowthUi.Plate(12f, 690f, 482f, 20f, 0.8f);
			GrowthUi.Text("UI/VMatrixUI.img/main/", error, 16f, 692f, 474f, "", (TextAnchor)0, 13f, new Color(1f, 0.58f, 0.52f));
		}
	}

	private static void PickCore(int slot, int coreId, VMatrixCoreView view)
	{
		VMatrixCore vMatrixCore = VMatrixCatalog.Core(coreId);
		int[] array;
		if (view != null && view.skills != null && view.skills.Length != 0)
		{
			array = view.skills;
		}
		else
		{
			array = ((vMatrixCore != null) ? vMatrixCore.skills : new int[0]);
		}
		string why;
		if (array.Length == 0)
		{
			NativeUtilPrompt.Notice("这个核心没有可以绑定的技能。");
		}
		else if (vMatrixCore != null && vMatrixCore.IsBoost)
		{
			NativeUtilPrompt.Notice("這是強化核心，本身不施放技能，只提升既有技能的傷害，不能綁到快捷欄。");
		}
		else if (SkillBook.Get(array[0]) == null)
		{
			NativeUtilPrompt.Notice("这个核心连接的技能（" + array[0] + "）在技能表里找不到，暂时不能绑到快捷栏。");
		}
		else if (!QuickSlots.CanBindToQuickBar(array[0], out why))
		{
			if (!string.IsNullOrEmpty(why))
			{
				NativeUtilPrompt.Notice(why);
			}
		}
		else
		{
			carrying = true;
			carryCore = coreId;
			carrySkill = array[0];
			selected = slot;
			BottomBar.Toast("点一下快捷栏（或键盘设定里的键位），放下「" + (view?.name ?? vMatrixCore?.name ?? "核心") + "」");
		}
	}

	private static void CancelCarry()
	{
		carrying = false;
		carryCore = 0;
		carrySkill = 0;
	}

	private static int SlotAt(Vector2 point)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (state == null)
		{
			return -1;
		}
		for (int i = 0; i < 31; i++)
		{
			Vector2 val = SlotAnchor(i);
			Rect val2 = GrowthUi.At(val.x - 34f, val.y - 39f, 68f, 76f);
			if (val2.Contains(point))
			{
				return i;
			}
		}
		return -1;
	}

	public static void CarryStep()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if (!carrying)
		{
			return;
		}
		if (!visible)
		{
			CancelCarry();
		}
		else
		{
			if (!Pointer.Clicked && !Pointer.DragEnded)
			{
				return;
			}
			if (KeyConfigWindow.Visible && !KeyConfigWindow.Carrying && (int)KeyConfigWindow.HitKey(Pointer.Pos) > 0)
			{
				if (KeyConfigWindow.DropSkill(Pointer.Pos, carrySkill))
				{
					Pointer.ConsumeClick();
					Pointer.Pressed = false;
					CancelCarry();
					return;
				}
			}
			else if (!KeyConfigWindow.Visible && (VirtualControls.HitButtonAt(Pointer.Pos) || VirtualControls.HitButtonAt(Pointer.PressPosition)))
			{
				NativeUtilPrompt.Notice("技能只能绑到键盘的键位上：在键盘设定里拖到键位，或放进快捷栏后指定按键。");
				CancelCarry();
				return;
			}
			if (BottomBar.QuickCollapsed)
			{
				NativeUtilPrompt.Notice("快捷栏处于收起状态，先展开快捷栏再绑定。");
				CancelCarry();
			}
			else if (TryDropOnQuickSlot(Pointer.Pos) || TryDropOnQuickSlot(Pointer.PressPosition))
			{
				Pointer.ConsumeClick();
				Pointer.Pressed = false;
			}
			else if (!Pointer.Clicked || !AtPointer)
			{
				CancelCarry();
			}
		}
	}

	private static bool TryDropOnQuickSlot(Vector2 point)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 26; i++)
		{
			Rect val = BottomBar.SlotRect(i);
			if (!val.Contains(point))
			{
				continue;
			}
			int num = i;
			if ((int)QuickBarKeys.Of(num) == 0)
			{
				num = QuickBarKeys.FreeKeyedSlot();
				if (num < 0)
				{
					NativeUtilPrompt.Notice("快捷栏里配了按键的技能格都占满了。先在键盘设置里给空着的格子指定按键，再回来绑定。");
					CancelCarry();
					return true;
				}
			}
			if (QuickBarKeys.BindSkill(num, carrySkill))
			{
				string text = "（" + KeyMap.Label(QuickBarKeys.Of(num)) + "）";
				BottomBar.Toast((num == i) ? ("已绑定到快捷格 " + (num + 1) + text) : ("这一格没有按键，已改绑到快捷格 " + (num + 1) + text));
			}
			CancelCarry();
			return true;
		}
		return false;
	}

	public static void DrawCarryGhost()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (carrying)
		{
			Texture2D val = ((carryCore != 0) ? MatrixCoreIcons.Texture(carryCore, hexa: false) : null);
			if ((Object)(object)val == (Object)null && carrySkill != 0)
			{
				val = WzAssets.Texture("skillicon/" + carrySkill);
			}
			if (!((Object)(object)val == (Object)null))
			{
				float num = 32f * BottomBar.S;
				GUI.DrawTexture(new Rect(Pointer.Pos.x - num * 0.5f, Pointer.Pos.y - num * 0.5f, num, num), (Texture)(object)val, (ScaleMode)2);
			}
		}
	}

	private static void DrawPoints()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = Vector("main/vector:vPointCount", 302f, 61f);
		GrowthUi.ShadowText("UI/VMatrixUI.img/main/", (state == null) ? "0" : state.points.ToString("N0", CultureInfo.InvariantCulture), val.x, val.y - 6f, 180f, "font:vPoint", (TextAnchor)0, 16f);
	}

	private static void DrawCoreIcon(Texture2D icon, float x, float y, float frameWidth)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		GUI.DrawTexture(GrowthUi.At(x + (frameWidth - (float)((Texture)icon).width) / 2f, y + 28f - (float)((Texture)icon).height / 2f, ((Texture)icon).width, ((Texture)icon).height), (Texture)(object)icon);
	}

	private static Texture2D LevelDigit(char digit)
	{
		ImgNode imgNode = WzAssets.Node("UI/VMatrixUI.img/core/levelNumber/" + digit);
		if (imgNode != null)
		{
			return WzAssets.Texture(imgNode);
		}
		return null;
	}

	private static void DrawLevelNumber(int level, float left, float top)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		string text = level.ToString(CultureInfo.InvariantCulture);
		float num = 0f;
		for (int i = 0; i < text.Length; i++)
		{
			Texture2D val = LevelDigit(text[i]);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			num += (float)((Texture)val).width + ((i > 0) ? 1f : 0f);
		}
		float num2 = left + 28.5f - num / 2f;
		float y = top + 52f;
		for (int j = 0; j < text.Length; j++)
		{
			Texture2D val2 = LevelDigit(text[j]);
			GUI.DrawTexture(GrowthUi.At(num2, y, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
			num2 += (float)((Texture)val2).width + 1f;
		}
	}

	private static void DrawSlots()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		if (state == null)
		{
			return;
		}
		for (int i = 0; i < 31; i++)
		{
			Vector2 val = SlotAnchor(i);
			int num = ((state.slots != null && i < state.slots.Length) ? state.slots[i] : 0);
			string text;
			if (i < 4)
			{
				text = "job";
			}
			else
			{
				text = ((i < 10) ? "enforce" : "common");
			}
			VMatrixCoreView vMatrixCoreView = CoreOf(i);
			Rect val2 = GrowthUi.At(val.x - 34f, val.y - 39f, 68f, 76f);
			bool flag = val2.Contains(Pointer.Pos);
			string text2;
			if (num != 0)
			{
				text2 = ((vMatrixCoreView == null || vMatrixCoreView.level == 0) ? (text + "Locked") : text);
			}
			else
			{
				text2 = "empty";
			}
			ImgNode imgNode = WzAssets.Node("UI/VMatrixUI.img/core/coreBackground/" + text2);
			Vector2 val3 = (Vector2)((imgNode == null) ? new Vector2(34f, 39f) : WzAssets.Origin(imgNode));
			GrowthUi.Image("UI/VMatrixUI.img/core/coreBackground/", text2, val.x - val3.x, val.y - val3.y);
			float num2 = val.x - val3.x;
			float num3 = val.y - val3.y;
			if (num != 0)
			{
				Texture2D val4;
				if (vMatrixCoreView == null)
				{
					val4 = null;
				}
				else
				{
					val4 = ((vMatrixCoreView.type == 2) ? SpecialIcon(vMatrixCoreView.coreId, vMatrixCoreView.level > 0) : MatrixCoreIcons.Core(vMatrixCoreView.coreId, hexa: false, vMatrixCoreView.skills));
				}
				if ((Object)(object)val4 != (Object)null)
				{
					DrawCoreIcon(val4, num2, num3, 58f);
				}
				if (vMatrixCoreView != null && vMatrixCoreView.level > 0)
				{
					DrawLevelNumber(vMatrixCoreView.level, num2, num3);
				}
			}
			if (flag && GUI.enabled && vMatrixCoreView != null)
			{
				VMatrixCore vMatrixCore = VMatrixCatalog.Core(vMatrixCoreView.coreId);
				MatrixPresentation.Skill(vMatrixCoreView.skills, vMatrixCoreView.level, (vMatrixCore != null) ? vMatrixCore.name : vMatrixCoreView.name, (vMatrixCore != null) ? vMatrixCore.desc : vMatrixCoreView.desc);
			}
			if (flag)
			{
				string text3 = ((num == 0) ? "0" : "1");
				Vector2 val5 = WzAssets.Origin(WzAssets.Node("UI/VMatrixUI.img/core/coreState/mouseOver/" + text3));
				GrowthUi.Image("UI/VMatrixUI.img/core/coreState/mouseOver/", text3, val.x - val5.x, val.y - val5.y);
			}
			if (i == selected)
			{
				GrowthUi.Veil(num2, num3, 58f, 68f, new Color(1f, 1f, 1f, 0.18f));
			}
			if (!GUI.enabled || !GrowthUi.Repaint || !flag)
			{
				continue;
			}
			if (Pointer.DoubleClicked)
			{
				Pointer.ConsumeClick();
				CancelCarry();
				selected = ((num == 0) ? (-1) : i);
				if (num != 0)
				{
					dialogCore = num;
					targetLevel = vMatrixCoreView?.level ?? 0;
					dialog = Dialog.Upgrade;
				}
			}
			else if (Pointer.Clicked)
			{
				Pointer.ConsumeClick();
				if (num == 0)
				{
					CancelCarry();
					selected = -1;
				}
				else if (carrying && carryCore == num)
				{
					CancelCarry();
				}
				else
				{
					PickCore(i, num, vMatrixCoreView);
				}
			}
		}
	}

	private static void DrawSpecialCore(bool coveredByPanel)
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_05dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_060f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0848: Unknown result type (might be due to invalid IL or missing references)
		//IL_084d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0851: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_0546: Unknown result type (might be due to invalid IL or missing references)
		//IL_0516: Unknown result type (might be due to invalid IL or missing references)
		//IL_0670: Unknown result type (might be due to invalid IL or missing references)
		//IL_065f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0675: Unknown result type (might be due to invalid IL or missing references)
		//IL_068e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0698: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0702: Unknown result type (might be due to invalid IL or missing references)
		//IL_0706: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_073e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0748: Unknown result type (might be due to invalid IL or missing references)
		//IL_076e: Unknown result type (might be due to invalid IL or missing references)
		VMatrixCoreView vMatrixCoreView = null;
		if (state?.cores != null)
		{
			foreach (VMatrixCoreView core in state.cores)
			{
				if (core.type == 2 && core.selected)
				{
					vMatrixCoreView = core;
				}
			}
		}
		Rect val2;
		if (vMatrixCoreView != null)
		{
			GrowthUi.Image("UI/VMatrixUI.img/core/coreBackground/", "special", 380f, 132f);
			Texture2D val = SpecialIcon(vMatrixCoreView.coreId, registered: true);
			if ((Object)(object)val != (Object)null)
			{
				DrawCoreIcon(val, 380f, 132f, 66f);
			}
		}
		else
		{
			int num;
			if (GUI.enabled && !coveredByPanel)
			{
				val2 = GrowthUi.At(380f, 132f, 66f, 68f);
				num = (val2.Contains(Pointer.Pos) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			GrowthUi.Image("UI/VMatrixUI.img/main/", flag ? "layer:specialCoreUnregisteredMouseOver" : "layer:specialCoreUnregistered", flag ? 378f : 380f, flag ? 130f : 132f);
		}
		if (vMatrixCoreView != null && GUI.enabled && !coveredByPanel)
		{
			val2 = GrowthUi.At(380f, 132f, 66f, 68f);
			if (val2.Contains(Pointer.Pos))
			{
				MatrixPresentation.Details(vMatrixCoreView.name, vMatrixCoreView.desc, vMatrixCoreView.level, showLevel: false);
			}
		}
		if (!specialOpen)
		{
			return;
		}
		GrowthUi.Image("UI/VMatrixUI.img/main/specialCore/", "backgrnd", 414f, 172f);
		List<VMatrixCoreView> list = new List<VMatrixCoreView>();
		foreach (VMatrixCore allCore in VMatrixCatalog.AllCores)
		{
			if (!allCore.IsSpecial)
			{
				continue;
			}
			VMatrixCoreView vMatrixCoreView2 = null;
			if (state?.cores != null)
			{
				foreach (VMatrixCoreView core2 in state.cores)
				{
					if (core2.coreId == allCore.id)
					{
						vMatrixCoreView2 = core2;
						break;
					}
				}
			}
			list.Add(vMatrixCoreView2 ?? new VMatrixCoreView
			{
				coreId = allCore.id,
				type = allCore.type,
				maxLevel = allCore.maxLevel,
				name = allCore.name,
				desc = allCore.desc,
				skills = allCore.skills
			});
		}
		Vector2 val3 = Vector("main/specialCore/vector:slotLT", 10f, 47f);
		Vector2 val4 = Vector("main/specialCore/slot/slotPos", 36f, 39f);
		Vector2 val5 = Vector("main/specialCore/slot/slotOffset", 75f, 75f);
		Vector2 val6 = Vector("main/specialCore/slot/mask/clip_rb", 230f, 297f);
		int num2 = (list.Count + 3 - 1) / 3;
		int maxScroll = Mathf.Max(0, num2 - 4);
		Rect val7 = GrowthUi.At(414f + val3.x, 172f + val3.y, val6.x, val6.y);
		Vector2 val8 = Vector("main/specialCore/slot/scrollBarInfo/lt", 222f, -1f);
		float barX = 414f + val8.x;
		float barY = 172f + val8.y;
		float barTravel = Mathf.Max(1f, 264f);
		Rect val9 = GrowthUi.At(barX - 1f, barY, 13f, 292f);
		if (GUI.enabled)
		{
			if (val7.Contains(Pointer.Pos) && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
			{
				specialScroll = Mathf.Clamp(specialScroll - (int)Mathf.Sign(Input.mouseScrollDelta.y), 0, maxScroll);
			}
			if (maxScroll > 0)
			{
				Rect val10 = thumbAt(specialScroll);
				if (Pointer.Pressed && val9.Contains(Pointer.PressPosition))
				{
					specialScrollStart = -1;
					if (val10.Contains(Pointer.PressPosition))
					{
						specialBarDrag = true;
						specialBarGrab = Pointer.PressPosition.y - val10.y;
					}
					else
					{
						specialScroll = Mathf.Clamp(specialScroll + ((!(Pointer.PressPosition.y < val10.y)) ? 1 : (-1)), 0, maxScroll);
					}
					Pointer.ConsumeClick();
				}
				else if (specialBarDrag && Pointer.Down)
				{
					specialScroll = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01((Pointer.Pos.y - specialBarGrab - barY) / barTravel) * (float)maxScroll), 0, maxScroll);
				}
			}
			if (!Pointer.Down)
			{
				specialBarDrag = false;
			}
			if (maxScroll > 0 && Pointer.Pressed && val7.Contains(Pointer.PressPosition) && !val9.Contains(Pointer.PressPosition))
			{
				specialScrollStart = specialScroll;
			}
			else if (!Pointer.Down)
			{
				specialScrollStart = -1;
			}
			else if (specialScrollStart >= 0)
			{
				int num3 = Mathf.Max(1, Mathf.RoundToInt(val5.y * S));
				specialScroll = Mathf.Clamp(specialScrollStart - Mathf.RoundToInt((Pointer.Pos.y - Pointer.PressPosition.y) / (float)num3), 0, maxScroll);
			}
		}
		specialScroll = Mathf.Clamp(specialScroll, 0, maxScroll);
		for (int i = 0; i < list.Count; i++)
		{
			int num4 = i / 3;
			int num5 = i % 3;
			if (num4 < specialScroll || num4 >= specialScroll + 4)
			{
				continue;
			}
			float num6 = 414f + val3.x + (float)num5 * val5.x + val4.x;
			float num7 = 172f + val3.y + (float)(num4 - specialScroll) * val5.y + val4.y;
			VMatrixCoreView vMatrixCoreView3 = list[i];
			bool flag2 = vMatrixCoreView3.level > 0;
			ImgNode imgNode = WzAssets.Node("UI/VMatrixUI.img/core/coreBackground/" + (flag2 ? "special" : "specialLocked"));
			Vector2 val11 = (Vector2)((imgNode == null) ? new Vector2(34f, 39f) : WzAssets.Origin(imgNode));
			GrowthUi.Image("UI/VMatrixUI.img/core/coreBackground/", flag2 ? "special" : "specialLocked", num6 - val11.x, num7 - val11.y);
			Texture2D val12 = SpecialIcon(vMatrixCoreView3.coreId, flag2);
			if ((Object)(object)val12 != (Object)null)
			{
				DrawCoreIcon(val12, num6 - val11.x, num7 - val11.y, 66f);
			}
			val2 = GrowthUi.At(num6 - val11.x, num7 - val11.y, 66f, 68f);
			bool flag3 = val2.Contains(Pointer.Pos);
			if (flag3 && GUI.enabled)
			{
				MatrixPresentation.Details(vMatrixCoreView3.name, vMatrixCoreView3.desc, vMatrixCoreView3.level, showLevel: false);
			}
			if (flag3)
			{
				GrowthUi.Veil(num6 - val11.x, num7 - val11.y, 66f, 68f, new Color(1f, 1f, 1f, 0.16f));
			}
			if (!GUI.enabled || !GrowthUi.Repaint || !Pointer.Clicked || !flag3)
			{
				continue;
			}
			Pointer.ConsumeClick();
			if (!flag2)
			{
				dialogCore = vMatrixCoreView3.coreId;
				targetLevel = 1;
				dialog = Dialog.Register;
				continue;
			}
			VMatrixResponse vMatrixResponse = Send(vMatrixCoreView3.selected ? VMatrixOperation.UnselectSpecial : VMatrixOperation.SelectSpecial, vMatrixCoreView3.coreId);
			if (vMatrixResponse != null && vMatrixResponse.ok)
			{
				BottomBar.Toast(vMatrixCoreView3.selected ? "已停用特殊核心" : "已启用特殊核心");
			}
		}
		GrowthUi.Image("UI/VMatrixUI.img/main/specialCore/slot/scrollBarInfo/", (maxScroll > 0) ? "enabled/base" : "disabled/base", barX, barY);
		if (maxScroll > 0)
		{
			Rect val13 = thumbAt(specialScroll);
			bool flag4 = val9.Contains(Pointer.Pos);
			string rel;
			if (specialBarDrag)
			{
				rel = "thumb2";
			}
			else
			{
				rel = (flag4 ? "thumb1" : "thumb0");
			}
			GrowthUi.Image("UI/VMatrixUI.img/main/specialCore/slot/scrollBarInfo/enabled/", rel, val13.x, val13.y);
		}
		Rect thumbAt(int scroll)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			return GrowthUi.At(barX, barY + barTravel * ((maxScroll <= 0) ? 0f : Mathf.Clamp01((float)scroll / (float)maxScroll)), 5f, 28f);
		}
	}

	private static void DrawButtons()
	{
		if (GrowthUi.Button("UI/VMatrixUI.img/main/", "button:close"))
		{
			Close();
			return;
		}
		if (GrowthUi.Button("UI/VMatrixUI.img/main/", "button:help"))
		{
			MatrixPresentation.Help(hexa: false);
		}
		if (GrowthUi.Button("UI/VMatrixUI.img/main/", specialOpen ? "button:specialCoreClose" : "button:specialCoreOpen"))
		{
			specialOpen = !specialOpen;
			specialScroll = 0;
			specialBarDrag = false;
			specialOwnsGesture = false;
		}
		if (GrowthUi.Button("UI/VMatrixUI.img/main/", "button:reset"))
		{
			string text = VMatrixCatalog.ResetMeso.ToString("N0", CultureInfo.InvariantCulture);
			NativeUtilPrompt.Confirm("重置后返还已分配的 V 点数，并扣除 " + text + " 枫币。继续？", (bool ok) =>
			{
				if (ok)
				{
					VMatrixResponse vMatrixResponse = Send(VMatrixOperation.Reset);
					if (vMatrixResponse != null && vMatrixResponse.ok)
					{
						BottomBar.Toast("V 点数已重置");
					}
				}
			});
		}
		else
		{
			bool enabled = GUI.enabled;
			GUI.enabled = enabled && state != null && state.points > 0;
			if (GrowthUi.Button("UI/VMatrixUI.img/main/", "button:transform"))
			{
				transformCount = 1;
				dialog = Dialog.Transform;
			}
			GUI.enabled = enabled;
		}
	}

	private static void DrawUpgrade()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		VMatrixCoreView vMatrixCoreView = Find(dialogCore);
		if (vMatrixCoreView == null)
		{
			dialog = Dialog.None;
			return;
		}
		Rect previous = GrowthUi.Shift((Width - 340f) / 2f, 114f, 340f, 491f);
		GrowthUi.Image("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "backgrnd", 0f, 0f);
		Texture2D val = ((vMatrixCoreView.type == 2) ? SpecialIcon(vMatrixCoreView.coreId, registered: true) : MatrixCoreIcons.Core(vMatrixCoreView.coreId, hexa: false, vMatrixCoreView.skills));
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(154f, 70f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
		GrowthUi.Image("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "layer:coreCover", 132f, 43f);
		int level = vMatrixCoreView.level;
		targetLevel = Mathf.Clamp(targetLevel, level, Mathf.Max(level, vMatrixCoreView.maxLevel));
		if (vMatrixCoreView.type == 2)
		{
			targetLevel = 1;
		}
		GrowthUi.ShadowText("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", level.ToString(CultureInfo.InvariantCulture), 130f, 155f, 40f, "font:levelCount", (TextAnchor)5, 14f);
		GrowthUi.Text("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "/", 172f, 155f, 12f, "", (TextAnchor)4, 14f);
		GrowthUi.ShadowText("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", targetLevel.ToString(CultureInfo.InvariantCulture), 186f, 155f, 40f, "font:levelCount", (TextAnchor)3, 14f);
		long num = CostOf(vMatrixCoreView, level, targetLevel);
		long num2 = state?.points ?? 0;
		GrowthUi.ShadowText("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", num2.ToString("N0", CultureInfo.InvariantCulture), 212f, 283f, 92f, "font:levelCount", (TextAnchor)5, 14f);
		GrowthUi.ShadowText(tint: (num > num2) ? new Nullable(new Color(1f, 0.55f, 0.5f)) : ((Nullable)null), root: "UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", text: (num < 0) ? "—" : num.ToString("N0", CultureInfo.InvariantCulture), x: 212f, y: 316f, w: 92f, font: "font:levelCount", align: (TextAnchor)5, height: 14f);
		if (num > num2)
		{
			GrowthUi.Text("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "V 点数不足", 40f, 350f, 260f, "", (TextAnchor)0, 13f, new Color(1f, 0.7f, 0.6f));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && targetLevel < vMatrixCoreView.maxLevel && num >= 0 && num <= num2;
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "button:levelPlus"))
		{
			targetLevel++;
		}
		GUI.enabled = enabled && MaxAffordable(vMatrixCoreView) > targetLevel;
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "button:selectAll"))
		{
			targetLevel = MaxAffordable(vMatrixCoreView);
		}
		GUI.enabled = enabled && targetLevel != level;
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "button:initialize"))
		{
			targetLevel = level;
		}
		GUI.enabled = enabled && targetLevel > level && num >= 0 && num <= num2;
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "button:confirm"))
		{
			VMatrixResponse vMatrixResponse = Send(VMatrixOperation.Upgrade, vMatrixCoreView.coreId, targetLevel);
			if (vMatrixResponse != null && vMatrixResponse.ok)
			{
				resultBefore = level;
				resultAfter = targetLevel;
				dialog = Dialog.Result;
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementAlert/", "button:cancel"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static void DrawResult()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		VMatrixCoreView vMatrixCoreView = Find(dialogCore);
		if (vMatrixCoreView == null)
		{
			dialog = Dialog.None;
			return;
		}
		Rect previous = GrowthUi.Shift((Width - 340f) / 2f, 184f, 340f, 351f);
		GrowthUi.Image("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementResult/", "backgrnd", 0f, 0f);
		Vector2 val = Vector("vSkillUpgradePopUp/enforcementResult/vector:corePos", 175f, 92f);
		Texture2D val2 = ((vMatrixCoreView.type == 2) ? SpecialIcon(vMatrixCoreView.coreId, registered: true) : MatrixCoreIcons.Core(vMatrixCoreView.coreId, hexa: false, vMatrixCoreView.skills));
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(val.x - (float)((Texture)val2).width / 2f, val.y - (float)((Texture)val2).height / 2f, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
		}
		GrowthUi.Image("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementResult/", "layer:coreCover", 132f, 43f);
		Vector2 val3 = Vector("vSkillUpgradePopUp/enforcementResult/vector:levelBefore", 93f, 229f);
		Vector2 val4 = Vector("vSkillUpgradePopUp/enforcementResult/vector:levelAfter", 237f, 229f);
		GrowthUi.Text("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementResult/", resultBefore.ToString(CultureInfo.InvariantCulture), val3.x - 24f, val3.y - 9f, 48f, "font:levelBefore", (TextAnchor)4);
		GrowthUi.Text("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementResult/", resultAfter.ToString(CultureInfo.InvariantCulture), val4.x - 24f, val4.y - 9f, 48f, "font:levelAfter", (TextAnchor)4);
		if (GrowthUi.Button("UI/VMatrixUI.img/vSkillUpgradePopUp/enforcementResult/", "button:cancel"))
		{
			selected = -1;
			dialog = Dialog.None;
			Load();
		}
		GrowthUi.Restore(previous);
	}

	private static long CostOf(VMatrixCoreView core, int from, int to)
	{
		if (to <= from)
		{
			return 0L;
		}
		if (core.type == 2)
		{
			return VMatrixCatalog.RegisterSpecialCost;
		}
		return VMatrixCatalog.CostBetween(core.coreId, from, to);
	}

	private static int MaxAffordable(VMatrixCoreView core)
	{
		if (core.type == 2)
		{
			return 1;
		}
		long num = state?.points ?? 0;
		int i;
		for (i = core.level; i < core.maxLevel; i++)
		{
			int num2 = VMatrixCatalog.CostForLevel(core.coreId, i + 1);
			if (num2 < 0 || num2 > num)
			{
				break;
			}
			num -= num2;
		}
		return i;
	}

	private static void DrawRegister()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		VMatrixCoreView vMatrixCoreView = Find(dialogCore);
		if (vMatrixCoreView == null)
		{
			VMatrixCore vMatrixCore = VMatrixCatalog.Core(dialogCore);
			if (vMatrixCore == null)
			{
				dialog = Dialog.None;
				return;
			}
			vMatrixCoreView = new VMatrixCoreView
			{
				coreId = vMatrixCore.id,
				type = vMatrixCore.type,
				maxLevel = vMatrixCore.maxLevel,
				name = vMatrixCore.name,
				skills = vMatrixCore.skills
			};
		}
		Rect previous = GrowthUi.Shift((Width - 342f) / 2f, 179f, 342f, 361f);
		GrowthUi.Image("UI/VMatrixUI.img/specialCorePopUp/register/", "backgrnd", 0f, 0f);
		Texture2D val = SpecialIcon(vMatrixCoreView.coreId, registered: true);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(155f, 96f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
		GrowthUi.ShadowText("UI/VMatrixUI.img/specialCorePopUp/register/", vMatrixCoreView.name, 40f, 150f, 262f, "", (TextAnchor)4, 14f);
		GrowthUi.ShadowText("UI/VMatrixUI.img/specialCorePopUp/register/", VMatrixCatalog.RegisterSpecialCost.ToString(CultureInfo.InvariantCulture), 200f, 250f, 90f, "", (TextAnchor)5, 14f);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && (state?.points ?? 0) >= VMatrixCatalog.RegisterSpecialCost;
		if (GrowthUi.Button("UI/VMatrixUI.img/specialCorePopUp/register/", "button:apply"))
		{
			VMatrixResponse vMatrixResponse = Send(VMatrixOperation.Upgrade, vMatrixCoreView.coreId, 1);
			if (vMatrixResponse != null && vMatrixResponse.ok)
			{
				dialog = Dialog.None;
				BottomBar.Toast("特殊核心已登记");
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/VMatrixUI.img/specialCorePopUp/register/", "button:cancel"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static void DrawTransform()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		Rect previous = GrowthUi.Shift((Width - 374f) / 2f, 136.5f, 374f, 446f);
		GrowthUi.Image("UI/VMatrixUI.img/vPointTransform/", "backgrnd", 0f, 0f);
		if (state != null && state.transformItem != 0)
		{
			Texture2D val = WzAssets.Texture("itemicon/" + state.transformItem);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(GrowthUi.At(243f, 211f, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
			}
		}
		int num = MaxTransform();
		transformCount = Mathf.Clamp(transformCount, 1, Mathf.Max(1, num));
		GrowthUi.ShadowText("UI/VMatrixUI.img/vPointTransform/", (state?.points ?? 0).ToString("N0", CultureInfo.InvariantCulture), 200f, 150f, 95f, "font:myVPoint", (TextAnchor)5, 14f);
		GrowthUi.ShadowText("UI/VMatrixUI.img/vPointTransform/", (transformCount * (state?.transformCost ?? 0)).ToString(CultureInfo.InvariantCulture), 84f, 250f, 60f, "font:EA", (TextAnchor)5, 14f);
		GrowthUi.ShadowText("UI/VMatrixUI.img/vPointTransform/", transformCount.ToString(CultureInfo.InvariantCulture), 228f, 250f, 60f, "font:EA", (TextAnchor)5, 14f);
		GrowthUi.Text("UI/VMatrixUI.img/vPointTransform/", transformCount.ToString(CultureInfo.InvariantCulture), 84f, 296f, 120f, "", (TextAnchor)4, 14f);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && transformCount > 1;
		if (GrowthUi.Button("UI/VMatrixUI.img/vPointTransform/", "button:transformCountDown"))
		{
			transformCount--;
		}
		GUI.enabled = enabled && transformCount < num;
		if (GrowthUi.Button("UI/VMatrixUI.img/vPointTransform/", "button:transformCountUP"))
		{
			transformCount++;
		}
		if (GrowthUi.Button("UI/VMatrixUI.img/vPointTransform/", "button:transformCountMax"))
		{
			transformCount = Mathf.Max(1, num);
		}
		GUI.enabled = enabled && num > 0;
		if (GrowthUi.Button("UI/VMatrixUI.img/vPointTransform/", "button:confirm"))
		{
			VMatrixResponse vMatrixResponse = Send(VMatrixOperation.Transform, 0, 0, 0, transformCount);
			if (vMatrixResponse != null && vMatrixResponse.ok)
			{
				dialog = Dialog.None;
				BottomBar.Toast("已消耗 " + transformCount * state.transformCost + " V 点，兑换 " + transformCount + " 颗核心宝石");
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/VMatrixUI.img/vPointTransform/", "button:close"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static int MaxTransform()
	{
		if (state == null || state.transformCost <= 0)
		{
			return 0;
		}
		return (int)Math.Min(9999L, state.points / state.transformCost);
	}

	private static void DrawGemstone()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		Rect previous = GrowthUi.Shift((Width - 285f) / 2f, 124.5f, 285f, 470f);
		GrowthUi.Image("UI/VMatrixUI.img/coreGemStoneOpen/open/", "backgrnd", 0f, 0f);
		VMatrixGemstone vMatrixGemstone = VMatrixCatalog.Gemstone(gemItem);
		int num = CountGemstone(gemItem);
		gemCount = Mathf.Clamp(gemCount, 1, Mathf.Max(1, num));
		if (vMatrixGemstone != null)
		{
			GrowthUi.ShadowText("UI/VMatrixUI.img/coreGemStoneOpen/open/", gemCount.ToString(CultureInfo.InvariantCulture), 92f, 130f, 62f, "font:coreCountInfo", (TextAnchor)5, 14f);
			GrowthUi.ShadowText("UI/VMatrixUI.img/coreGemStoneOpen/open/", (state?.points ?? 0).ToString("N0", CultureInfo.InvariantCulture), 150f, 301f, 80f, "font:vPointInfo", (TextAnchor)5, 14f);
			int num2 = gemCount;
			if (vMatrixGemstone.openCore != 0 && Find(vMatrixGemstone.openCore) == null && num2 > 0)
			{
				num2--;
			}
			GrowthUi.ShadowText("UI/VMatrixUI.img/coreGemStoneOpen/open/", (num2 * vMatrixGemstone.points).ToString(CultureInfo.InvariantCulture), 151f, 332f, 80f, "font:corevPointInfo", (TextAnchor)5, 14f);
			GrowthUi.Text("UI/VMatrixUI.img/coreGemStoneOpen/open/", gemCount.ToString(CultureInfo.InvariantCulture), 139f, 198f, 48f, "", (TextAnchor)4, 14f);
		}
		else
		{
			GrowthUi.Text("UI/VMatrixUI.img/coreGemStoneOpen/open/", "这不是核心宝石", 40f, 180f, 200f, "", (TextAnchor)4, 14f, new Color(1f, 0.6f, 0.55f));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && gemCount > 1;
		if (GrowthUi.Button("UI/VMatrixUI.img/coreGemStoneOpen/open/", "button:Down"))
		{
			gemCount--;
		}
		GUI.enabled = enabled && gemCount < num;
		if (GrowthUi.Button("UI/VMatrixUI.img/coreGemStoneOpen/open/", "button:UP"))
		{
			gemCount++;
		}
		if (GrowthUi.Button("UI/VMatrixUI.img/coreGemStoneOpen/open/", "button:All"))
		{
			gemCount = Mathf.Max(1, num);
		}
		GUI.enabled = enabled && vMatrixGemstone != null && num > 0;
		if (GrowthUi.Button("UI/VMatrixUI.img/coreGemStoneOpen/open/", "button:Confirm"))
		{
			VMatrixResponse vMatrixResponse = Send(VMatrixOperation.UseGemstone, 0, 0, gemItem, gemCount);
			if (vMatrixResponse != null && vMatrixResponse.ok)
			{
				dialog = Dialog.None;
				BottomBar.Toast("核心宝石已使用");
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/VMatrixUI.img/coreGemStoneOpen/open/", "button:Cancel"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static int CountGemstone(int itemId)
	{
		if (itemId == 0)
		{
			return 0;
		}
		int num = 0;
		List<InventoryEntry> list = GameSession.Character?.inventory;
		if (list == null)
		{
			return 0;
		}
		foreach (InventoryEntry item in list)
		{
			if (item.itemId == itemId && item.position > 0)
			{
				num += item.quantity;
			}
		}
		return num;
	}
}
