using System;
using System.Collections.Generic;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

public static class NativeHexaMatrixWindow
{
	private enum Page
	{
		Skill,
		Stat
	}

	private enum Dialog
	{
		None,
		Upgrade,
		Transform,
		StatActivation,
		StatChange,
		SkillResult,
		StatResult
	}

	private const string Img = "UI/HexaMatrixUI.img/";

	private const float W = 862f;

	private const float H = 624f;

	private const float ChildX = 20f;

	private const float ChildY = 87f;

	private const float ChildW = 830f;

	private const float ChildH = 516f;

	private const float TabX = 20f;

	private const float TabY = 50f;

	private const float TabW = 176f;

	private const float TabH = 35f;

	private const float TabSpace = 4f;

	private const float DetailX = 512f;

	private const float DetailY = 142f;

	private const float DetailW = 310f;

	private const float DetailH = 364f;

	private const float ActivationX = 332f;

	private const float ActivationY = 1f;

	private const float ActivationW = 496f;

	private const float ActivationH = 506f;

	private const float EnforceX = 332f;

	private const float EnforceY = 1f;

	private const float EnforceW = 489f;

	private const float EnforceH = 506f;

	private static HexaStatView resultStatBefore;

	private static HexaStatView resultStatAfter;

	private static bool resultActivation;

	private static int resultBefore;

	private static int resultAfter;

	private static readonly string[] StatNames = new string[7] { "爆擊傷害增加", "Boss傷害增加", "增加無視防禦率", "傷害增加", "攻擊力提升", "魔力增加", "主要能力值增加" };

	private static readonly int[] choices = new int[3];

	private static int dropdown = -1;

	private static int changeCore;

	private static int changePreset;

	private static readonly string[] Sections = new string[4] { "skill", "mastery", "enforce", "common" };

	private static readonly int[] SectionGroup = new int[4] { 2, 0, 1, 3 };

	private static readonly int[] SectionSlots = new int[4] { 6, 4, 4, 4 };

	private static readonly int[] TransformItems = new int[2] { 2638879, 2638603 };

	private static readonly string[] TransformNames = new string[2] { "淡灵魂艾尔达", "HEXA Booster" };

	private static bool visible;

	private static bool dragging;

	private static bool positioned;

	private static bool miniOpen;

	private static Vector2 position;

	private static Vector2 grab;

	private static int owner;

	private static int selectedCore;

	private static int statCore;

	private static int preset;

	private static int transformTab;

	private static int transformCount = 1;

	private static Page page;

	private static Dialog dialog;

	private static int upgradeTarget;

	private static HexaResponse state;

	private static string error;

	public static bool Visible => visible;

	public static bool AtPointer
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			if (visible)
			{
				if (!dragging)
				{
					return Pointer.HitsWindow(ModalRect());
				}
				return true;
			}
			return false;
		}
	}

	private static float S => Mathf.Min(BottomBar.S, Mathf.Min((UiKit.W - 24f) / 862f, UiKit.H * 0.92f / ((dialog == Dialog.StatActivation) ? 699f : ((dialog == Dialog.StatChange) ? 688f : 624f))));

	private static Vector2 Free
	{
		get
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			if (!positioned)
			{
				return new Vector2((UiKit.W / S - 862f) / 2f, (UiKit.H / S - 624f) / 2f);
			}
			float num = ((dialog == Dialog.StatActivation) ? 699f : ((dialog == Dialog.StatChange) ? 688f : 624f)) - 624f;
			Vector2 val = new Vector2(0f, num / 2f);
			return NativeUi.ClampWindowPosition(position - val, new Vector2(862f, 624f + num), S) + val;
		}
	}

	private static bool Eligible
	{
		get
		{
			if (state != null)
			{
				return state.eligible;
			}
			return false;
		}
	}

	private static Rect WindowRect()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(Free.x * S, Free.y * S, 862f * S, 624f * S);
	}

	private static Rect ModalRect()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		Rect result = WindowRect();
		if (dialog == Dialog.StatActivation || dialog == Dialog.StatChange)
		{
			float num = ((dialog == Dialog.StatActivation) ? 699f : 688f) * S;
			return new Rect(result.x, result.y + (result.height - num) / 2f, result.width, num);
		}
		return result;
	}

	public static void Escape()
	{
		if (dialog != Dialog.None)
		{
			dialog = Dialog.None;
		}
		else if (miniOpen)
		{
			miniOpen = false;
		}
		else
		{
			Close();
		}
	}

	public static void Close()
	{
		visible = (dragging = (positioned = (miniOpen = false)));
		state = null;
		error = null;
		selectedCore = 0;
		dialog = Dialog.None;
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

	private static void Load()
	{
		HexaResponse hexaResponse = GameSession.Net.Hexa(new HexaRequest
		{
			characterId = owner,
			operation = HexaOperation.Snapshot
		});
		if (hexaResponse != null)
		{
			state = hexaResponse;
			error = (hexaResponse.eligible ? null : hexaResponse.error);
			if (selectedCore != 0 && CoreOf(selectedCore) == null)
			{
				selectedCore = 0;
			}
			if (state.stats != null && StatOf(statCore, preset) == null && StatOf(statCore, 1 - preset) != null)
			{
				preset = 1 - preset;
			}
			VMatrixLevels.CaptureHexa(hexaResponse, owner);
		}
	}

	private static HexaResponse Send(HexaOperation operation, int core = 0, int target = 0, int presetIndex = 0, int stat1 = 0, int stat2 = 0, int stat3 = 0, int weight = 0, bool commit = true)
	{
		HexaResponse hexaResponse = GameSession.Net.Hexa(new HexaRequest
		{
			characterId = owner,
			operation = operation,
			coreId = core,
			level = 0,
			targetLevel = target,
			stat1 = stat1,
			stat2 = stat2,
			stat3 = stat3,
			weight = weight,
			preset = presetIndex,
			commit = commit
		});
		if (hexaResponse == null)
		{
			return null;
		}
		if (!hexaResponse.ok)
		{
			error = hexaResponse.error;
			BottomBar.Toast(hexaResponse.error);
			return hexaResponse;
		}
		error = null;
		if (commit)
		{
			state = hexaResponse;
			GameSession.ReloadAfterCommand();
			VMatrixLevels.CaptureHexa(hexaResponse, owner);
		}
		return hexaResponse;
	}

	private static HexaCoreView CoreOf(int coreId)
	{
		if (state?.cores == null || coreId == 0)
		{
			return null;
		}
		foreach (HexaCoreView core in state.cores)
		{
			if (core.coreId == coreId)
			{
				return core;
			}
		}
		return null;
	}

	private static List<HexaCoreView> Section(int sectionIndex)
	{
		List<HexaCoreView> list = new List<HexaCoreView>();
		int num = SectionGroup[sectionIndex];
		if (state?.cores != null)
		{
			foreach (HexaCoreView core in state.cores)
			{
				if (core.group == num)
				{
					list.Add(core);
				}
			}
		}
		return list;
	}

	private static HexaStatView StatOf(int coreId, int presetIndex)
	{
		if (state?.stats == null || coreId == 0)
		{
			return null;
		}
		foreach (HexaStatView stat in state.stats)
		{
			if (stat.coreId == coreId && stat.preset == presetIndex)
			{
				return stat;
			}
		}
		return null;
	}

	private static HexaStatView CurrentStat()
	{
		return StatOf(statCore, preset);
	}

	private static Vector2 Vector(string path, float fallbackX, float fallbackY)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node("UI/HexaMatrixUI.img/" + path);
		if (imgNode == null || !imgNode.TryVector("", out var x, out var y))
		{
			return new Vector2(fallbackX, fallbackY);
		}
		return new Vector2((float)x, (float)y);
	}

	private static List<Vector2> SlotPositions(string section)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		List<Vector2> list = new List<Vector2>();
		for (int i = 0; i < 8; i++)
		{
			ImgNode imgNode = WzAssets.Node("UI/HexaMatrixUI.img/hexaSkill/slotPos/" + section + "/" + i);
			if (imgNode == null || !imgNode.TryVector("", out var x, out var y))
			{
				break;
			}
			list.Add(new Vector2((float)x, (float)y));
		}
		return list;
	}

	public static void Draw()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		if (!visible)
		{
			return;
		}
		if (!GameSession.InGame || owner != GameSession.CharacterId)
		{
			Close();
			return;
		}
		Rect r = WindowRect();
		bool atPointer = AtPointer;
		GrowthUi.S = S;
		GrowthUi.R = r;
		if (dialog == Dialog.None && GrowthUi.Drag(ref dragging, ref positioned, ref position, ref grab, 862f, 624f, WindowRect, 46f, 100f, typeof(NativeHexaMatrixWindow), 6f))
		{
			r = WindowRect();
		}
		GrowthUi.R = r;
		NativeUi.RegisterWindow("NativeHexaMatrixWindow", ModalRect(), () => visible);
		MatrixPresentation.Begin();
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && dialog == Dialog.None;
		GrowthUi.Image("UI/HexaMatrixUI.img/main/", "backgrnd", 0f, 0f);
		DrawTabs();
		DrawHeaderButtons();
		DrawResources();
		Rect previous = GrowthUi.Shift(20f, 87f, 830f, 516f);
		if (page == Page.Skill)
		{
			DrawSkillPage();
		}
		else
		{
			DrawStatPage();
		}
		GrowthUi.Restore(previous);
		if (page == Page.Skill)
		{
			DrawSkillDetail();
		}
		if (miniOpen)
		{
			DrawMini();
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
		case Dialog.SkillResult:
			DrawSkillResult();
			break;
		case Dialog.StatResult:
			DrawStatResult();
			break;
		case Dialog.Transform:
			DrawTransform();
			break;
		case Dialog.StatActivation:
			DrawStatSelection(change: false);
			break;
		case Dialog.StatChange:
			DrawStatSelection(change: true);
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
			GrowthUi.Plate(20f, 592f, 830f, 20f, 0.82f);
			GrowthUi.Text("UI/HexaMatrixUI.img/main/", error, 24f, 594f, 820f, "", (TextAnchor)0, 13f, new Color(1f, 0.62f, 0.55f));
		}
	}

	private static void DrawTabs()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 2; i++)
		{
			bool flag = page == (Page)i;
			GrowthUi.Image("UI/HexaMatrixUI.img/main/tab:mainTab/", (flag ? "selected/" : "normal/") + i, 20f + (float)i * 180f, 50f);
			if (Interactive())
			{
				Rect val = GrowthUi.At(20f + (float)i * 180f, 50f, 176f, 35f);
				if (val.Contains(Pointer.Pos) && Pointer.Clicked)
				{
					Pointer.ConsumeClick();
					page = (Page)i;
					dialog = Dialog.None;
				}
			}
		}
	}

	private static void DrawHeaderButtons()
	{
		if (GrowthUi.Button("UI/HexaMatrixUI.img/main/", "button:close"))
		{
			Close();
			return;
		}
		if (GrowthUi.Button("UI/HexaMatrixUI.img/main/", "button:help"))
		{
			MatrixPresentation.Help(hexa: true);
		}
		if (GrowthUi.Button("UI/HexaMatrixUI.img/main/", "button:solErdaTransform"))
		{
			if (!Eligible)
			{
				BottomBar.Toast(error ?? "尚未解锁 HEXA 矩阵。");
				return;
			}
			transformTab = 0;
			transformCount = 1;
			dialog = Dialog.Transform;
		}
	}

	private static void DrawSkillPage()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkill/", "backgrnd", 0f, 0f);
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkill/", "layer:slot", 30f, 22f);
		for (int i = 0; i < Sections.Length; i++)
		{
			List<Vector2> list = SlotPositions(Sections[i]);
			List<HexaCoreView> list2 = Section(i);
			for (int j = 0; j < list.Count && j < SectionSlots[i]; j++)
			{
				HexaCoreView core = ((j < list2.Count) ? list2[j] : null);
				DrawSkillSlot(i, list[j], core);
			}
		}
	}

	private static void DrawSkillSlot(int section, Vector2 anchor, HexaCoreView core)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		if (core == null)
		{
			return;
		}
		string rel = Sections[section];
		GrowthUi.Image("UI/HexaMatrixUI.img/core/coreBackground/", rel, anchor.x - 34f, anchor.y - 39f);
		float num = anchor.x - 34f;
		float num2 = anchor.y - 39f;
		Vector2 val = Vector("core/vector:iconPos", 18f, 27f);
		Texture2D val2 = MatrixCoreIcons.Core(core.coreId, hexa: true, core.skills);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(num + val.x, num2 + val.y, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
		}
		if (core.level > 0)
		{
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/core/", core.level.ToString(CultureInfo.InvariantCulture), num + 8f, num2 + 54f, 52f, "font:level", (TextAnchor)4, 12f);
		}
		if (core.level == 0 && Eligible)
		{
			MatrixPresentation.Animation("UI/HexaMatrixUI.img/core/coreState/activatable/", anchor.x, anchor.y);
		}
		else if (!Eligible)
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/core/coreState/unreleased/", rel, num, num2);
		}
		if (core.coreId == selectedCore)
		{
			MatrixPresentation.Animation("UI/HexaMatrixUI.img/core/coreState/selected/", anchor.x, anchor.y);
		}
		if (Interactive())
		{
			Rect val3 = GrowthUi.At(num, num2, 68f, 78f);
			bool flag = val3.Contains(Pointer.Pos);
			if (flag)
			{
				HexaCore hexaCore = HexaCatalog.Core(core.coreId);
				MatrixPresentation.Skill((hexaCore != null) ? hexaCore.skills : core.skills, core.level, (hexaCore != null) ? hexaCore.name : core.name, (hexaCore != null) ? hexaCore.desc : core.desc);
			}
			if (flag)
			{
				GrowthUi.Veil(num, num2, 68f, 78f, new Color(1f, 1f, 1f, 0.14f));
			}
			if (flag && Pointer.Clicked)
			{
				Pointer.ConsumeClick();
				selectedCore = core.coreId;
				upgradeTarget = 0;
			}
		}
	}

	private static void DrawSkillDetail()
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		HexaCoreView hexaCoreView = CoreOf(selectedCore);
		if (hexaCoreView == null)
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkill/", "layer:default", 512f, 142f);
			return;
		}
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkill/", DetailLayer(hexaCoreView), 512f, 142f);
		bool flag = hexaCoreView.level >= hexaCoreView.maxLevel && hexaCoreView.level > 0;
		if (flag)
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkill/", "layer:enforceCompleted", 525f, 349f);
		}
		Vector2 val = Vector("hexaSkill/vector:descLT", 540f, 196f);
		Vector2 val2 = Vector("hexaSkill/vector:nameSpace", 2f, 7f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", hexaCoreView.name, val.x + val2.x, val.y + val2.y, 195f, "font:skillName", (TextAnchor)0);
		string text = (string.IsNullOrEmpty(hexaCoreView.desc) ? "（該核心沒有說明。）" : hexaCoreView.desc);
		GrowthUi.TextWrapped("UI/HexaMatrixUI.img/hexaSkill/", text, val.x, val.y + 24f, 235f, 110f, "font:skillDesc");
		int solErda;
		int fragments;
		if (hexaCoreView.level == 0)
		{
			NativeHexaMatrixService.ActivateCost(hexaCoreView.group, out solErda, out fragments);
		}
		else
		{
			NativeHexaMatrixService.UpgradeCost(hexaCoreView.group, hexaCoreView.level, out solErda, out fragments);
		}
		int num;
		Vector2 val3;
		if (hexaCoreView.skills != null)
		{
			num = ((hexaCoreView.skills.Length > 1) ? 1 : 0);
			if (num != 0)
			{
				val3 = Vector("hexaSkill/vector:coupleIngredientMainCount", 609f, 411f);
				goto IL_01ab;
			}
		}
		else
		{
			num = 0;
		}
		val3 = Vector("hexaSkill/vector:singleIngredientCount", 667f, 411f);
		goto IL_01ab;
		IL_01ab:
		Vector2 val4 = val3;
		Vector2 val5 = Vector("hexaSkill/vector:coupleIngredientSubCount", 725f, 411f);
		Nullable tint = (((state?.solErda ?? 0) < solErda) ? new Nullable(new Color(1f, 0.55f, 0.5f)) : ((Nullable)null));
		Nullable tint2 = (((state?.fragments ?? 0) < fragments) ? new Nullable(new Color(1f, 0.55f, 0.5f)) : ((Nullable)null));
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", solErda.ToString(CultureInfo.InvariantCulture), val4.x - 40f, val4.y - 8f, 40f, "font:ingredient", (TextAnchor)5, 12f, tint);
		if (num != 0)
		{
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", fragments.ToString(CultureInfo.InvariantCulture), val5.x - 40f, val5.y - 8f, 40f, "font:ingredient", (TextAnchor)5, 12f, tint2);
		}
		else
		{
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", fragments.ToString(CultureInfo.InvariantCulture), val4.x + 28f, val4.y - 8f, 40f, "font:ingredient", (TextAnchor)5, 12f, tint2);
		}
		bool enabled = GUI.enabled;
		if (hexaCoreView.level == 0)
		{
			GUI.enabled = enabled && Eligible;
			if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkill/", "button:activation"))
			{
				HexaResponse hexaResponse = Send(HexaOperation.Activate, hexaCoreView.coreId);
				if (hexaResponse != null && hexaResponse.ok)
				{
					resultActivation = true;
					resultBefore = 0;
					resultAfter = 1;
					dialog = Dialog.SkillResult;
				}
			}
		}
		else
		{
			GUI.enabled = enabled && Eligible && !flag;
			if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkill/", "button:enforcement"))
			{
				upgradeTarget = Mathf.Clamp(hexaCoreView.level + 1, hexaCoreView.level + 1, hexaCoreView.maxLevel);
				dialog = Dialog.Upgrade;
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkill/", "button:solErdaMini"))
		{
			miniOpen = !miniOpen;
		}
	}

	private static string DetailLayer(HexaCoreView core)
	{
		bool flag = core.skills != null && core.skills.Length > 1;
		int[] array = HexaCatalog.Core(core.coreId)?.reqForActivation;
		bool flag2 = array != null && array.Length != 0;
		if (core.level > 0)
		{
			if (!flag)
			{
				return "layer:enforceSingleMain";
			}
			return "layer:enforceCouple";
		}
		if (flag2)
		{
			if (!flag)
			{
				return "layer:activateSingleMainWithReq";
			}
			return "layer:activateCoupleWithReq";
		}
		if (!flag)
		{
			return "layer:activateSingleMain";
		}
		return "layer:activateCouple";
	}

	private static void DrawResources()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		if (state != null)
		{
			Vector2 val = Vector("hexaSkill/vector:mainCount", 608f, 92f);
			Vector2 val2 = Vector("hexaSkill/vector:subCount", 726f, 92f);
			Color value = new Color(0.72f, 0.8f, 0.9f);
			GrowthUi.Plate(val.x - 104f, val.y - 13f, 104f, 18f);
			GrowthUi.Plate(val2.x - 104f, val2.y - 13f, 104f, 18f);
			GrowthUi.Text("UI/HexaMatrixUI.img/hexaSkill/", "艾爾達", val.x - 100f, val.y - 11f, 48f, "", (TextAnchor)0, 12f, value);
			GrowthUi.Text("UI/HexaMatrixUI.img/hexaSkill/", "碎片", val2.x - 100f, val2.y - 11f, 48f, "", (TextAnchor)0, 12f, value);
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", state.solErda.ToString("N0", CultureInfo.InvariantCulture), val.x - 96f, val.y - 11f, 90f, "font:invenCount", (TextAnchor)2, 12f);
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkill/", state.fragments.ToString("N0", CultureInfo.InvariantCulture), val2.x - 96f, val2.y - 11f, 90f, "font:invenCount", (TextAnchor)2, 12f);
		}
	}

	private static void DrawMini()
	{
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "backgrnd", 649f, 196f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "layer:iconMain", 673f, 199f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "layer:iconSub", 746f, 201f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaMini/", (state?.solErda ?? 0).ToString(CultureInfo.InvariantCulture), 673f, 226f, 32f, "", (TextAnchor)4, 12f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaMini/", (state?.fragments ?? 0).ToString(CultureInfo.InvariantCulture), 748f, 226f, 40f, "", (TextAnchor)4, 12f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "layer:gauge", 659f, 236f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "layer:gaugeMax", 659f, 236f);
		float num = ((state == null || state.solErdaGaugeMax <= 0) ? 0f : Mathf.Clamp01((float)state.solErdaGauge / (float)state.solErdaGaugeMax));
		if (num < 1f)
		{
			GrowthUi.Veil(659f + 65f * num, 236f, 65f * (1f - num), 7f, new Color(0f, 0f, 0f, 0.55f));
		}
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaMini/", state?.solErdaGauge.ToString(CultureInfo.InvariantCulture) ?? "0", 657f, 238f, 26f, "font:gauge", (TextAnchor)5, 10f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaMini/", "layer:gaugeSlash", 688f, 236f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaMini/", state?.solErdaGaugeMax.ToString(CultureInfo.InvariantCulture) ?? "0", 689f, 238f, 26f, "font:gauge", (TextAnchor)3, 10f);
		if (GrowthUi.ButtonAt("UI/HexaMatrixUI.img/solErdaMini/", "button:close", GrowthUi.At(793f, 203f, 7f, 7f)))
		{
			miniOpen = false;
		}
	}

	private static void DrawStatPage()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/", "backgrnd", 0f, 0f);
		List<HexaStatCore> allStats = HexaCatalog.AllStats;
		for (int i = 0; i < 6; i++)
		{
			ImgNode imgNode = WzAssets.Node("UI/HexaMatrixUI.img/hexaStat/slotPos/" + i);
			if (imgNode == null || !imgNode.TryVector("", out var x, out var y))
			{
				break;
			}
			Vector2 anchor = new Vector2((float)x, (float)y);
			int num = i / 2;
			int num2 = i % 2;
			HexaStatCore hexaStatCore = ((num < allStats.Count) ? allStats[num] : null);
			HexaStatView view = ((hexaStatCore == null) ? null : StatOf(hexaStatCore.id, num2));
			DrawStatSlot(anchor, hexaStatCore, view, num2);
		}
		DrawStatBoard(allStats);
		if (statCore == 0 && allStats.Count > 0)
		{
			statCore = allStats[0].id;
			preset = 0;
		}
		if (HasStatActive(statCore))
		{
			DrawEnforcementWindow();
		}
		else
		{
			DrawActivationWindow();
		}
	}

	private static bool IsStatActive(int coreId, int presetIndex)
	{
		return StatOf(coreId, presetIndex) != null;
	}

	private static void DrawStatSlot(Vector2 anchor, HexaStatCore definition, HexaStatView view, int slotPreset)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		float num = anchor.x - 34f;
		float num2 = anchor.y - 39f;
		GrowthUi.Image("UI/HexaMatrixUI.img/core/coreBackground/", "stat", num, num2);
		if (view != null && view.level > 0)
		{
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/core/", "Lv" + view.level.ToString(CultureInfo.InvariantCulture), num + 4f, num2 + 56f, 60f, "", (TextAnchor)4, 12f);
		}
		if (definition == null || !Eligible || definition.reqLevel > (state?.level ?? 0))
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/core/coreState/unreleased/", "stat", num, num2);
		}
		if (definition != null && definition.id == statCore && slotPreset == preset)
		{
			MatrixPresentation.Animation("UI/HexaMatrixUI.img/core/coreState/selected/", anchor.x, anchor.y);
		}
		if (Interactive())
		{
			Rect val = GrowthUi.At(num, num2, 68f, 78f);
			bool flag = val.Contains(Pointer.Pos);
			if (flag)
			{
				GrowthUi.Veil(num, num2, 68f, 78f, new Color(1f, 1f, 1f, 0.14f));
			}
			if (flag)
			{
				MatrixPresentation.Hover(StatHint(view));
			}
			if (flag && Pointer.Clicked && definition != null)
			{
				Pointer.ConsumeClick();
				statCore = definition.id;
				preset = slotPreset;
			}
		}
	}

	private static void DrawStatBoard(List<HexaStatCore> stats)
	{
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/statBoard/", "layer:statBoard", 15f, 312f);
		int[] array = new int[7];
		if (state?.stats != null)
		{
			foreach (HexaStatView stat in state.stats)
			{
				if (stat.applied && stat.level == stat.level1 + stat.level2 + stat.level3 && NativeHexaMatrixService.ValidStats(stat.stat1, stat.stat2, stat.stat3))
				{
					array[stat.stat1] += HexaCatalog.StatValue(stat.stat1, stat.level1, main: true);
					array[stat.stat2] += HexaCatalog.StatValue(stat.stat2, stat.level2, main: false);
					array[stat.stat3] += HexaCatalog.StatValue(stat.stat3, stat.level3, main: false);
				}
			}
		}
		for (int i = 0; i < 6; i++)
		{
			int num = i switch
			{
				5 => 6, 
				4 => (HexaCatalog.MainStat(state.job) == "INT") ? 5 : 4, 
				_ => i, 
			};
			int num2 = ((num == 6) ? HexaCatalog.MainStatValue(array[num], state.job) : array[num]);
			string text = ((num <= 3) ? (((float)num2 / 100f).ToString("0.##") + "%") : num2.ToString());
			GrowthUi.Text("UI/HexaMatrixUI.img/hexaStat/statBoard/", StatNames[num], 35f, 369 + i * 18, 200f, "font:stat", (TextAnchor)0);
			GrowthUi.Text("UI/HexaMatrixUI.img/hexaStat/statBoard/", "+" + text, 235f, 369 + i * 18, 65f, "font:stat", (TextAnchor)2);
		}
	}

	private static void DrawActivationWindow()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		Rect previous = GrowthUi.Shift(332f, 1f, 496f, 506f);
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/activation_wnd/", "backgrnd", 0f, 0f);
		HexaStatCore hexaStatCore = HexaCatalog.Stat(statCore);
		if (hexaStatCore != null)
		{
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaStat/activation_wnd/", "Lv." + hexaStatCore.reqLevel + " 解鎖（目前 " + (state?.level ?? 0) + "）", 0f, 429f, 496f, "", (TextAnchor)4, 12f, new Color(0.72f, 0.8f, 0.9f));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && Eligible && hexaStatCore != null && (state?.level ?? 0) >= hexaStatCore.reqLevel && (hexaStatCore.reqCore == 0 || HasStatActive(hexaStatCore.reqCore));
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/activation_wnd/", "button:activation"))
		{
			preset = 0;
			choices[0] = (choices[1] = (choices[2] = -1));
			dropdown = -1;
			dialog = Dialog.StatActivation;
		}
		GUI.enabled = enabled;
		GrowthUi.Restore(previous);
	}

	private static bool HasStatActive(int coreId)
	{
		for (int i = 0; i < 2; i++)
		{
			if (IsStatActive(coreId, i))
			{
				return true;
			}
		}
		return false;
	}

	private static void DrawEnforcementWindow()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_066b: Unknown result type (might be due to invalid IL or missing references)
		Rect previous = GrowthUi.Shift(332f, 1f, 489f, 506f);
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "backgrnd", 0f, 0f);
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", (preset == 0) ? "layer:tab0" : "layer:tab1", 69f, 10f);
		Vector2 val = Vector("hexaStat/enforcement_wnd/vector:enforceCore", 132f, 61f);
		Vector2 val2 = Vector("hexaStat/enforcement_wnd/vector:saveCore", 290f, 61f);
		for (int i = 0; i < 2; i++)
		{
			float num = ((i == 0) ? 90f : 249f);
			HexaStatView hexaStatView = StatOf(statCore, i);
			GrowthUi.Image("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/tab:coreSlot/", ((i == preset) ? "selected/" : "normal/") + i, num, 30f);
			Vector2 val3 = ((i == 0) ? val : val2);
			GrowthUi.Image("UI/HexaMatrixUI.img/core/coreBackground/", "stat", val3.x, val3.y);
			string text = ((i == 0) ? "強化中" : "保存中");
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", text, num + 10f, 38f, 60f, "", (TextAnchor)0, 12f);
			GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", (hexaStatView == null) ? "—" : ("Lv" + hexaStatView.level), num + 10f, 60f, 100f, "", (TextAnchor)0, 13f);
			if (hexaStatView != null)
			{
				GrowthUi.Text("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", hexaStatView.stat1 + " / " + hexaStatView.stat2 + " / " + hexaStatView.stat3, num + 10f, 82f, 132f, "", (TextAnchor)0, 12f, new Color(0.85f, 0.9f, 0.95f));
			}
			if (hexaStatView != null && hexaStatView.applied)
			{
				GrowthUi.Text("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "套用中", num + 10f, 110f, 100f, "", (TextAnchor)0, 12f, new Color(0.55f, 0.95f, 0.7f));
			}
			Rect val4;
			if (GUI.enabled)
			{
				val4 = GrowthUi.At(num, 29f, 152f, 146f);
				if (val4.Contains(Pointer.Pos))
				{
					MatrixPresentation.Hover(StatHint(hexaStatView));
				}
			}
			if (Interactive())
			{
				val4 = GrowthUi.At(num, 29f, 152f, 146f);
				if (val4.Contains(Pointer.Pos) && Pointer.Clicked)
				{
					Pointer.ConsumeClick();
					preset = i;
				}
			}
		}
		HexaStatView hexaStatView2 = CurrentStat();
		int num2 = ((hexaStatView2 != null && hexaStatView2.level < 20) ? NativeHexaMatrixService.StatFragmentCost(hexaStatView2.level1) : 0);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", (num2 <= 0) ? "強化完成" : ("強化需要 " + num2 + " 碎片"), 90f, 196f, 300f, "", (TextAnchor)0, 12f);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && preset == 0 && Eligible && hexaStatView2 != null && hexaStatView2.level < hexaStatView2.maxLevel && (state?.fragments ?? 0) >= num2;
		if (preset == 0 && GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:enforcement"))
		{
			HexaResponse hexaResponse = Send(HexaOperation.EnforceStat, statCore, 0, preset);
			if (hexaResponse != null && hexaResponse.ok)
			{
				resultStatBefore = hexaStatView2;
				resultStatAfter = CurrentStat();
				dialog = Dialog.StatResult;
			}
		}
		GUI.enabled = enabled && preset == 0 && hexaStatView2 != null && (hexaStatView2.level >= 10 || hexaStatView2.level != hexaStatView2.level1 + hexaStatView2.level2 + hexaStatView2.level3);
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:reset"))
		{
			NativeUtilPrompt.Confirm((hexaStatView2.level != hexaStatView2.level1 + hexaStatView2.level2 + hexaStatView2.level3) ? "舊版核心免費初始化，清空等級並保留能力值種類。繼續？" : "初始化清空強化等級，保留能力值種類，消耗1000萬楓幣。繼續？", (bool ok) =>
			{
				if (ok)
				{
					HexaResponse hexaResponse5 = Send(HexaOperation.Reset, statCore, 0, preset);
					if (hexaResponse5 != null && hexaResponse5.ok)
					{
						BottomBar.Toast("已初始化");
					}
				}
			});
		}
		GUI.enabled = enabled && preset == 0 && hexaStatView2 != null && hexaStatView2.level == 20;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:save"))
		{
			HexaResponse hexaResponse2 = Send(HexaOperation.SaveStat, statCore);
			if (hexaResponse2 != null && hexaResponse2.ok)
			{
				BottomBar.Toast("已存到保存槽");
			}
		}
		GUI.enabled = enabled && preset == 0 && StatOf(statCore, 0) != null && !StatOf(statCore, 0).applied;
		if (preset == 0 && GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:apply"))
		{
			HexaResponse hexaResponse3 = Send(HexaOperation.Preset, statCore);
			if (hexaResponse3 != null && hexaResponse3.ok)
			{
				BottomBar.Toast("已套用強化中的能力值");
			}
		}
		GUI.enabled = enabled && preset == 1 && StatOf(statCore, 1) != null && !StatOf(statCore, 1).applied;
		if (preset == 1 && GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:apply2"))
		{
			HexaResponse hexaResponse4 = Send(HexaOperation.Preset, statCore, 0, 1);
			if (hexaResponse4 != null && hexaResponse4.ok)
			{
				BottomBar.Toast("已套用保存中的能力值");
			}
		}
		GUI.enabled = enabled && hexaStatView2 != null;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/", "button:change"))
		{
			choices[0] = hexaStatView2.stat1;
			choices[1] = hexaStatView2.stat2;
			choices[2] = hexaStatView2.stat3;
			dropdown = -1;
			changeCore = statCore;
			changePreset = preset;
			dialog = Dialog.StatChange;
		}
		GUI.enabled = enabled;
		DrawStatLines(hexaStatView2);
		GrowthUi.Restore(previous);
	}

	private static string StatHint(HexaStatView view)
	{
		if (view == null)
		{
			return "尚未激活能力值核心";
		}
		string text = "能力值核心 Lv" + view.level + (view.applied ? "（套用中）" : "");
		int[] array = new int[3] { view.stat1, view.stat2, view.stat3 };
		int[] array2 = new int[3] { view.level1, view.level2, view.level3 };
		for (int i = 0; i < 3; i++)
		{
			if (array[i] >= 0 && array[i] < StatNames.Length)
			{
				int num = HexaCatalog.StatValue(array[i], array2[i], i == 0);
				string text2 = ((array[i] <= 3) ? (((float)num / 100f).ToString("0.##") + "%") : ((array[i] == 6) ? HexaCatalog.MainStatValue(num, state.job) : num).ToString());
				text = text + "\n" + ((i == 0) ? "主要：" : "附加：") + StatNames[array[i]] + " +" + text2 + "  Lv" + array2[i];
			}
		}
		return text;
	}

	private static void DrawStatLines(HexaStatView view)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		if (view == null)
		{
			return;
		}
		string text = "UI/HexaMatrixUI.img/hexaStat/enforcement_wnd/enchantResult/";
		int[] array = new int[3] { view.stat1, view.stat2, view.stat3 };
		int[] array2 = new int[3] { view.level1, view.level2, view.level3 };
		string[] array3 = new string[3] { "main", "add1", "add2" };
		for (int i = 0; i < 3; i++)
		{
			Vector2 val = Vector("hexaStat/enforcement_wnd/enchantResult/pos/vector:" + array3[i], 19f, i switch
			{
				1 => 297, 
				0 => 199, 
				_ => 364, 
			});
			string text2 = ((array[i] >= 0 && array[i] < StatNames.Length) ? StatNames[array[i]] : "");
			GrowthUi.Text(text, text2, val.x + 7f, val.y + 5f, 350f, "font/font:" + ((i == 0) ? "statMain" : "statAdd"), (TextAnchor)0);
			int num = HexaCatalog.StatValue(array[i], array2[i], i == 0);
			string text3 = ((array[i] <= 3) ? (((float)num / 100f).ToString("0.##") + "%") : ((array[i] == 6) ? HexaCatalog.MainStatValue(num, state.job) : num).ToString());
			GrowthUi.Text(text, "+" + text3, val.x + 4f, val.y + 23f, 300f, "font/font:figure", (TextAnchor)0);
			GrowthUi.Image(text + "imgLevel/", Mathf.Clamp(array2[i], 0, 10).ToString(), val.x + 416f, val.y + 23f);
			for (int j = 1; j <= Mathf.Min(array2[i], 10); j++)
			{
				GrowthUi.Image(text + ((i == 0) ? "imgMainGauge/" : "imgAddGauge/"), j.ToString(), val.x + 8f + (float)((j - 1) * 39), val.y + 31f);
			}
		}
	}

	private static void DrawStatSelection(bool change)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0544: Unknown result type (might be due to invalid IL or missing references)
		float num = (change ? 788 : 450);
		float num2 = (change ? 688 : 699);
		Rect previous = GrowthUi.Shift((862f - num) / 2f, (624f - num2) / 2f, num, num2);
		string root = "UI/HexaMatrixUI.img/hexaStatPopUp/" + (change ? "statChange/" : "activationSet/");
		GrowthUi.Image(root, "backgrnd", 0f, 0f);
		float[] array = ((!change) ? new float[3] { 84f, 84f, 84f } : new float[3] { 112f, 338f, 543f });
		float[] array2 = ((!change) ? new float[3] { 228f, 288f, 323f } : new float[3] { 88f, 88f, 88f });
		if (change)
		{
			List<HexaStatCore> allStats = HexaCatalog.AllStats;
			for (int i = 0; i < 6; i++)
			{
				float num3 = 71 + 70 * i;
				GrowthUi.Image(root, "layer:slot" + (i + 1), 29f, num3);
				HexaStatCore hexaStatCore = ((i / 2 < allStats.Count) ? allStats[i / 2] : null);
				HexaStatView hexaStatView = ((hexaStatCore == null) ? null : StatOf(hexaStatCore.id, i % 2));
				if (hexaStatView == null)
				{
					GrowthUi.Image(root, "canvas:disabled", 29f, num3);
					continue;
				}
				bool flag = changeCore == hexaStatView.coreId && changePreset == hexaStatView.preset;
				int[] array3 = new int[3] { hexaStatView.stat1, hexaStatView.stat2, hexaStatView.stat3 };
				for (int j = 0; j < 3; j++)
				{
					if (flag)
					{
						array[j] = j switch
						{
							1 => 338, 
							0 => 112, 
							_ => 543, 
						};
						array2[j] = num3 + 17f;
					}
					else if (DrawStatCombo(root, j switch
					{
						1 => 338, 
						0 => 112, 
						_ => 543, 
					}, num3 + 17f, change: true, array3[j], j, option: false) && dropdown >= 0)
					{
						changeCore = hexaStatView.coreId;
						changePreset = hexaStatView.preset;
						choices[0] = hexaStatView.stat1;
						choices[1] = hexaStatView.stat2;
						choices[2] = hexaStatView.stat3;
					}
				}
			}
			GrowthUi.Text(root, 1000000L.ToString("N0"), 517f, 549f, 100f, "", (TextAnchor)2);
		}
		else
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/core/coreBackground/", "stat", 191f, 61f);
			GrowthUi.Image(root, "layer:coreCover", 187f, 57f);
			GrowthUi.Text(root, "5", 127f, 420f, 40f, "font:quantity", (TextAnchor)2);
			GrowthUi.Text(root, "10", 242f, 420f, 40f, "font:quantity", (TextAnchor)2);
			GrowthUi.Text(root, (state?.solErda ?? 0).ToString(), 135f, 495f, 40f, "possession/font:quantity_P", (TextAnchor)2);
			GrowthUi.Text(root, (state?.fragments ?? 0).ToString(), 245f, 495f, 40f, "possession/font:quantity_P", (TextAnchor)2);
		}
		for (int k = 0; k < 3; k++)
		{
			DrawStatCombo(root, array[k], array2[k], change, choices[k], k, option: false);
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && dropdown < 0 && NativeHexaMatrixService.ValidStats(choices[0], choices[1], choices[2]);
		if (GrowthUi.Button(root, change ? "button:change" : "button:activation"))
		{
			HexaResponse hexaResponse = Send(change ? HexaOperation.ChangeStat : HexaOperation.SetStat, change ? changeCore : statCore, 0, change ? changePreset : 0, choices[0], choices[1], choices[2]);
			if (hexaResponse != null && hexaResponse.ok)
			{
				dialog = Dialog.None;
			}
		}
		GUI.enabled = enabled && dropdown < 0;
		if (GrowthUi.Button(root, "button:cancel"))
		{
			dialog = Dialog.None;
		}
		GUI.enabled = enabled;
		if (dropdown >= 0)
		{
			int num4 = dropdown;
			for (int l = 0; l < 6; l++)
			{
				int num5 = l switch
				{
					5 => 6, 
					4 => (HexaCatalog.MainStat(state.job) == "INT") ? 5 : 4, 
					_ => l, 
				};
				if (DrawStatCombo(root, array[num4], array2[num4] + (float)(32 * (l + 1)), change, num5, num4, option: true))
				{
					choices[num4] = num5;
					dropdown = -1;
				}
			}
		}
		GrowthUi.Restore(previous);
	}

	private static bool DrawStatCombo(string root, float x, float y, bool change, int type, int line, bool option)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		string root2 = root + "comboBoxPad/combo:main/normal/";
		GrowthUi.Image(root2, "0", x, y);
		GrowthUi.Image(root2, "1", x + 2f, y);
		GrowthUi.Image(root2, "2", x + 2f + (float)(change ? 173 : 248), y);
		GrowthUi.Text(root, (type < 0) ? "不選擇" : StatNames[type], x + 8f, y + 7f, change ? 166 : 240, "", (TextAnchor)0, 12f);
		int num;
		if (Hot() && (option || dropdown < 0) && Pointer.Clicked)
		{
			Rect val = GrowthUi.At(x, y, change ? 203 : 283, 32f);
			num = (val.Contains(Pointer.Pos) ? 1 : 0);
			if (num != 0)
			{
				Pointer.ConsumeClick();
				if (!option)
				{
					dropdown = line;
				}
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private static void DrawUpgrade()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		HexaCoreView hexaCoreView = CoreOf(selectedCore);
		if (hexaCoreView == null)
		{
			dialog = Dialog.None;
			return;
		}
		Rect previous = GrowthUi.Shift(257f, 55f, 348f, 514f);
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "backgrnd", 0f, 0f);
		Vector2 val = Vector("hexaSkillPopUp/enforcementAlert/vector:corePos", 140f, 61f);
		Texture2D val2 = MatrixCoreIcons.Core(hexaCoreView.coreId, hexa: true, hexaCoreView.skills);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(val.x - 20f, val.y - 10f, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
		}
		GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "layer:coreCover", 136f, 57f);
		upgradeTarget = Mathf.Clamp(upgradeTarget, hexaCoreView.level + 1, hexaCoreView.maxLevel);
		Vector2 val3 = Vector("hexaSkillPopUp/enforcementAlert/vector:levelBefore", 127f, 206f);
		Vector2 val4 = Vector("hexaSkillPopUp/enforcementAlert/vector:levelAfter", 226f, 206f);
		GrowthUi.Text("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", hexaCoreView.level.ToString(CultureInfo.InvariantCulture), val3.x - 24f, val3.y - 10f, 48f, "font:levelBefore", (TextAnchor)4);
		GrowthUi.Text("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", upgradeTarget.ToString(CultureInfo.InvariantCulture), val4.x - 24f, val4.y - 10f, 48f, "font:levelAfter", (TextAnchor)4);
		long num = 0L;
		long num2 = 0L;
		for (int i = hexaCoreView.level; i < upgradeTarget; i++)
		{
			NativeHexaMatrixService.UpgradeCost(hexaCoreView.group, i, out var solErda, out var fragments);
			num += solErda;
			num2 += fragments;
		}
		bool flag = hexaCoreView.skills != null && hexaCoreView.skills.Length > 1;
		Vector2 val5 = Vector("hexaSkillPopUp/enforcementAlert/vector:singleIngredientCount", 174f, 320f);
		Vector2 val6 = (flag ? Vector("hexaSkillPopUp/enforcementAlert/vector:coupleIngredientMainCount", 116f, 320f) : val5);
		Vector2 val7 = Vector("hexaSkillPopUp/enforcementAlert/vector:coupleIngredientSubCount", 232f, 320f);
		Nullable tint = (((state?.solErda ?? 0) < num) ? new Nullable(new Color(1f, 0.55f, 0.5f)) : ((Nullable)null));
		Nullable tint2 = (((state?.fragments ?? 0) < num2) ? new Nullable(new Color(1f, 0.55f, 0.5f)) : ((Nullable)null));
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", num.ToString(CultureInfo.InvariantCulture), val6.x - 40f, val6.y - 8f, 40f, "font:enforceIngredient", (TextAnchor)5, 12f, tint);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", num2.ToString(CultureInfo.InvariantCulture), flag ? (val7.x - 40f) : (val5.x + 28f), val5.y - 8f, 40f, "font:enforceIngredient", (TextAnchor)5, 12f, tint2);
		bool flag2 = (state?.solErda ?? 0) >= num && (state?.fragments ?? 0) >= num2;
		if (!flag2)
		{
			GrowthUi.Image("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "layer:deficiencyAlert", 49f, 283f);
			GrowthUi.Text("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "氣息或碎片不足", 49f, 291f, 250f, "", (TextAnchor)4, 13f, new Color(1f, 0.7f, 0.62f));
		}
		bool enabled = GUI.enabled;
		GUI.enabled = (enabled && upgradeTarget < hexaCoreView.maxLevel) & flag2;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "button:levelPlus"))
		{
			upgradeTarget++;
		}
		GUI.enabled = enabled && upgradeTarget < hexaCoreView.maxLevel;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "button:selectAll"))
		{
			upgradeTarget = MaxAffordable(hexaCoreView);
		}
		GUI.enabled = enabled && upgradeTarget != hexaCoreView.level + 1;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "button:initialize"))
		{
			upgradeTarget = hexaCoreView.level + 1;
		}
		GUI.enabled = (enabled && upgradeTarget > hexaCoreView.level) & flag2;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "button:confirm"))
		{
			int level = hexaCoreView.level;
			HexaResponse hexaResponse = Send(HexaOperation.Upgrade, hexaCoreView.coreId, upgradeTarget);
			if (hexaResponse != null && hexaResponse.ok)
			{
				resultActivation = false;
				resultBefore = level;
				resultAfter = upgradeTarget;
				dialog = Dialog.SkillResult;
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/hexaSkillPopUp/enforcementAlert/", "button:cancel"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static void DrawStatResult()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		if (resultStatBefore == null || resultStatAfter == null)
		{
			dialog = Dialog.None;
			return;
		}
		string root = "UI/HexaMatrixUI.img/hexaStatPopUp/enforceResult/";
		Rect previous = GrowthUi.Shift(167f, 67.5f, 528f, 489f);
		GrowthUi.Image(root, "backgrnd", 0f, 0f);
		Vector2 val = Vector("hexaStatPopUp/enforceResult/vector:core", 230f, 61f);
		GrowthUi.Image("UI/HexaMatrixUI.img/core/coreBackground/", "stat", val.x, val.y);
		GrowthUi.Image(root, "layer:coreCover", 225f, 56f);
		bool flag = resultStatAfter.level1 > resultStatBefore.level1;
		GrowthUi.Image(root, flag ? "canvas:mainSuccess" : "canvas:success", flag ? 167 : 212, 153f);
		for (int i = 0; i < 2; i++)
		{
			HexaStatView hexaStatView = ((i == 0) ? resultStatBefore : resultStatAfter);
			Vector2 val2 = Vector("hexaStatPopUp/enforceResult/" + ((i == 0) ? "vector:before" : "vector:after"), (i == 0) ? 17 : 274, 221f);
			int[] array = new int[3] { hexaStatView.stat1, hexaStatView.stat2, hexaStatView.stat3 };
			int[] array2 = new int[3] { hexaStatView.level1, hexaStatView.level2, hexaStatView.level3 };
			string[] array3 = new string[3] { "mainStat", "addStat1", "addStat2" };
			for (int j = 0; j < 3; j++)
			{
				Vector2 val3 = Vector("hexaStatPopUp/enforceResult/vector:" + array3[j], 119f, j switch
				{
					1 => 112, 
					0 => 52, 
					_ => 147, 
				});
				int num = HexaCatalog.StatValue(array[j], array2[j], j == 0);
				string text = ((array[j] <= 3) ? (((float)num / 100f).ToString("0.##") + "%") : ((array[j] == 6) ? HexaCatalog.MainStatValue(num, state.job) : num).ToString());
				string text2 = ((array[j] >= 0 && array[j] < StatNames.Length) ? StatNames[array[j]] : "");
				GrowthUi.Text(root, text2 + " +" + text + "  Lv" + array2[j], val2.x + val3.x - 115f, val2.y + val3.y - 9f, 230f, (j == 0) ? "font:mainStat" : "font:addStat", (TextAnchor)4);
			}
		}
		if (GrowthUi.Button(root, "button:confirm"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static void DrawSkillResult()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		HexaCoreView hexaCoreView = CoreOf(selectedCore);
		if (hexaCoreView == null)
		{
			dialog = Dialog.None;
			return;
		}
		string text = "hexaSkillPopUp/" + (resultActivation ? "activationResult/" : "enforcementResult/");
		string root = "UI/HexaMatrixUI.img/" + text;
		Rect previous = GrowthUi.Shift(257f, 105f, 348f, 414f);
		GrowthUi.Image(root, "backgrnd", 0f, 0f);
		Vector2 val = Vector(text + "vector:corePos", 140f, 61f);
		Texture2D val2 = MatrixCoreIcons.Core(hexaCoreView.coreId, hexa: true, hexaCoreView.skills);
		if ((Object)(object)val2 != (Object)null)
		{
			GUI.DrawTexture(GrowthUi.At(val.x - 20f, val.y - 10f, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
		}
		GrowthUi.Image(root, "layer:coreCover", 135f, 56f);
		if (resultActivation)
		{
			Vector2 val3 = Vector(text + "vector:skillName", 174f, 254f);
			GrowthUi.Text(root, hexaCoreView.name, val3.x - 145f, val3.y - 10f, 290f, "font:skillName", (TextAnchor)4, 20f);
		}
		else
		{
			Vector2 val4 = Vector(text + "vector:levelBefore", 125f, 254f);
			Vector2 val5 = Vector(text + "vector:levelAfter", 227f, 254f);
			GrowthUi.Text(root, resultBefore.ToString(CultureInfo.InvariantCulture), val4.x - 24f, val4.y - 10f, 48f, "font:levelBefore", (TextAnchor)4, 20f);
			GrowthUi.Text(root, resultAfter.ToString(CultureInfo.InvariantCulture), val5.x - 24f, val5.y - 10f, 48f, "font:levelAfter", (TextAnchor)4, 20f);
		}
		if (GrowthUi.Button(root, resultActivation ? "button:confirm" : "button:cancel"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static int MaxAffordable(HexaCoreView core)
	{
		long num = state?.solErda ?? 0;
		long num2 = state?.fragments ?? 0;
		int i;
		for (i = core.level; i < core.maxLevel; i++)
		{
			NativeHexaMatrixService.UpgradeCost(core.group, i, out var solErda, out var fragments);
			if (solErda > num || fragments > num2)
			{
				break;
			}
			num -= solErda;
			num2 -= fragments;
		}
		return Mathf.Max(i + 1, core.level + 1);
	}

	private static void DrawTransform()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		Rect previous = GrowthUi.Shift(227f, 66f, 408f, 492f);
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaTransform/", "backgrnd", 0f, 0f);
		for (int i = 0; i < TransformItems.Length; i++)
		{
			float x = 26 + i * 180;
			GrowthUi.Image("UI/HexaMatrixUI.img/solErdaTransform/tab:item/", ((i == transformTab) ? "selected/" : "normal/") + (i + 1), x, 185f);
			if (Hot())
			{
				Rect val = GrowthUi.At(x, 185f, 176f, 35f);
				if (val.Contains(Pointer.Pos) && Pointer.Clicked)
				{
					Pointer.ConsumeClick();
					transformTab = i;
				}
			}
		}
		int num = TransformItems[transformTab];
		int num2 = ((num != 2638879) ? 1 : 30);
		int num3 = (int)Math.Min(9999L, Math.Max(0L, state?.solErda ?? 0));
		transformCount = Mathf.Clamp(transformCount, 1, Mathf.Max(1, num3));
		GrowthUi.Image("UI/HexaMatrixUI.img/solErdaTransform/detailLayer/", "layer:" + transformTab, 25f, 220f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaTransform/", (state?.solErda ?? 0).ToString("N0", CultureInfo.InvariantCulture), 204f, 126f, 110f, "font:EA", (TextAnchor)5, 14f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaTransform/", transformCount.ToString(CultureInfo.InvariantCulture), 136f, 274f, 60f, "font:EA", (TextAnchor)5, 14f);
		GrowthUi.ShadowText("UI/HexaMatrixUI.img/solErdaTransform/", (transformCount * num2).ToString(CultureInfo.InvariantCulture), 268f, 274f, 60f, "font:EA", (TextAnchor)5, 14f);
		GrowthUi.Text("UI/HexaMatrixUI.img/solErdaTransform/", transformCount.ToString(CultureInfo.InvariantCulture), 139f, 321f, 120f, "", (TextAnchor)4, 14f);
		GrowthUi.Text("UI/HexaMatrixUI.img/solErdaTransform/", "1 氣息 → " + num2 + " 個" + TransformNames[transformTab], 25f, 350f, 358f, "", (TextAnchor)4, 12f, new Color(0.78f, 0.84f, 0.9f));
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && transformCount > 1;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/solErdaTransform/", "button:transformCountDown"))
		{
			transformCount--;
		}
		GUI.enabled = enabled && transformCount < num3;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/solErdaTransform/", "button:transformCountUP"))
		{
			transformCount++;
		}
		GUI.enabled = enabled && transformCount != num3;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/solErdaTransform/", "button:transformCountMax"))
		{
			transformCount = Mathf.Max(1, num3);
		}
		GUI.enabled = enabled && num3 > 0;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/solErdaTransform/", "button:confirm"))
		{
			HexaConvertResponse hexaConvertResponse = GameSession.Net.HexaConvert(new HexaConvertRequest
			{
				characterId = owner,
				itemId = num,
				count = transformCount
			});
			if (hexaConvertResponse == null || !hexaConvertResponse.ok)
			{
				error = hexaConvertResponse?.error ?? "兑换失败";
				BottomBar.Toast(error);
			}
			else
			{
				BottomBar.Toast("轉換了 " + transformCount * num2 + " 個" + TransformNames[transformTab]);
				dialog = Dialog.None;
				GameSession.ReloadAfterCommand();
				Load();
			}
		}
		GUI.enabled = enabled;
		if (GrowthUi.Button("UI/HexaMatrixUI.img/solErdaTransform/", "button:close"))
		{
			dialog = Dialog.None;
		}
		GrowthUi.Restore(previous);
	}

	private static bool Hot()
	{
		if (GUI.enabled && GrowthUi.Repaint)
		{
			return !NativeUtilPrompt.Visible;
		}
		return false;
	}

	private static bool Interactive()
	{
		if (Hot())
		{
			return dialog == Dialog.None;
		}
		return false;
	}
}
