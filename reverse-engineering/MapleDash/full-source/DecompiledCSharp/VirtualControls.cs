using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class VirtualControls
{
	private delegate float AlphaFn(int x, int y, int n);

	public static readonly Color Teal = new Color(0.06f, 0.66f, 0.6f);

	public static List<VCElem> elems = new List<VCElem>();

	public static string activeName = "默认";

	public static List<string> profileNames = new List<string>();

	private const string PrefOverlay = "mxd.vc.overlay";

	private static bool overlayVisible = true;

	private static bool overlayLoaded;

	private const float EntranceSeconds = 0.2f;

	private static float entranceAt = -1f;

	private static int entranceFrame = -1;

	private static int baseScreenH;

	private const string DirName = "vcontrols";

	private static bool loaded;

	private const string PrefStyleVer = "mxd.vc.stylever";

	private const int StyleVer = 3;

	public static readonly GameAction[] BindableActions = new GameAction[21]
	{
		GameAction.Jump,
		GameAction.Attack,
		GameAction.Pickup,
		GameAction.Slot1,
		GameAction.Slot2,
		GameAction.Slot3,
		GameAction.Slot4,
		GameAction.Slot5,
		GameAction.Slot6,
		GameAction.Slot7,
		GameAction.Slot8,
		GameAction.Equip,
		GameAction.Inven,
		GameAction.Stat,
		GameAction.Skill,
		GameAction.KeySet,
		GameAction.QuickSlot,
		GameAction.Menu,
		GameAction.Shop,
		GameAction.Shortcut,
		GameAction.Help
	};

	private const int MouseFinger = -2;

	private static bool wasOnControl;

	private static bool anyPushed;

	private static string lastPress = "-";

	private static string lastNote = "";

	public static bool ShowDebug;

	private const float StickKeyGate = 0.38f;

	private static readonly Dictionary<int, GUIStyle> _styles = new Dictionary<int, GUIStyle>();

	private static Texture2D _disc;

	private static Texture2D _round;

	private static Texture2D _soft;

	private static Texture2D _ball;

	private const int TexN = 256;

	private static Texture2D _line;

	private static readonly Dictionary<long, Texture2D> _annulus = new Dictionary<long, Texture2D>();

	private static readonly List<long> _annulusKeys = new List<long>();

	private const int AnnulusN = 160;

	private const int AnnulusKeep = 24;

	private const float AnnulusQ = 64f;

	public static bool OverlayVisible
	{
		get
		{
			if (!overlayLoaded)
			{
				overlayLoaded = true;
				overlayVisible = PlayerPrefs.GetInt("mxd.vc.overlay", 1) != 0;
			}
			return overlayVisible;
		}
		set
		{
			overlayLoaded = true;
			if (overlayVisible != value)
			{
				overlayVisible = value;
				if (!value)
				{
					ReleaseAll();
				}
				PlayerPrefs.SetInt("mxd.vc.overlay", value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}
	}

	public static bool Shown
	{
		get
		{
			if (GamePlatform.TouchControls && OverlayVisible && !ShopWindow.Visible)
			{
				return EntranceDone;
			}
			return false;
		}
	}

	private static bool EntranceDone
	{
		get
		{
			if (entranceFrame < 0)
			{
				return true;
			}
			if (Time.frameCount <= entranceFrame + 1)
			{
				return false;
			}
			return Time.unscaledTime - entranceAt >= 0.2f;
		}
	}

	public static float LiftY { get; private set; }

	private static string Dir => Path.Combine(Application.persistentDataPath, "vcontrols");

	private static string ManifestPath => Path.Combine(Dir, "manifest.json");

	public static bool BlockingPointer { get; private set; }

	private static bool CarryingSkill
	{
		get
		{
			if (!SkillWindow.Dragging)
			{
				return NativeVMatrixWindow.Carrying;
			}
			return true;
		}
	}

	public static bool Capturing
	{
		get
		{
			GameLoop i = GameLoop.I;
			if ((Object)(object)i != (Object)null && i.state == GameLoop.GameState.Playing && Shown && !CarryingSkill && !VirtualControlsEditor.Visible)
			{
				return !GameSideMenu.Blocking;
			}
			return false;
		}
	}

	public static void OnEnterGame()
	{
		entranceAt = Time.unscaledTime;
		entranceFrame = Time.frameCount;
		ReleaseAll();
	}

	private static void UpdateLift()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (!TouchScreenKeyboard.visible)
		{
			baseScreenH = Screen.height;
			LiftY = 0f;
			return;
		}
		bool num = baseScreenH > 0 && (float)Screen.height < (float)baseScreenH * 0.9f;
		float num2 = 0f;
		if (!num)
		{
			float num3 = 0f;
			try
			{
				Rect area = TouchScreenKeyboard.area;
				num3 = area.height;
			}
			catch
			{
			}
			if (num3 <= 1f)
			{
				num3 = (float)Screen.height * 0.42f;
			}
			num2 = num3 / Mathf.Max(0.01f, UiKit.Scale);
		}
		LiftY = Mathf.Clamp(num2, 0f, UiKit.H * 0.55f);
	}

	public static Vector2 Center(VCElem e)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		float num = e.cy * UiKit.H - LiftY;
		float r = e.R;
		num = Mathf.Clamp(num, r + 4f, Mathf.Max(r + 4f, UiKit.H - r - 4f));
		return new Vector2(e.cx * UiKit.W, num);
	}

	private static string ProfilePath(string name)
	{
		return Path.Combine(Dir, name + ".json");
	}

	public static void ApplyStickSkin(VCElem e)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		e.borderW = 2f;
		e.halo = 0f;
		e.bg = new Color(1f, 1f, 1f, 0.06f);
		e.line = new Color(1f, 1f, 1f, 0.12f);
		e.bgDown = new Color(1f, 1f, 1f, 0.18f);
		e.lineDown = new Color(1f, 1f, 1f, 0.45f);
		e.shape = 0;
	}

	public static void ApplyFlatSkin(VCElem e)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (e.type == VCElemType.Stick)
		{
			ApplyStickSkin(e);
			return;
		}
		e.borderW = 1.2f;
		e.halo = 0f;
		e.bg = new Color(1f, 1f, 1f, 0.09f);
		e.line = new Color(1f, 1f, 1f, 0.1f);
		e.bgDown = new Color(1f, 1f, 1f, 0.22f);
		e.lineDown = new Color(1f, 1f, 1f, 0.5f);
		if (e.type == VCElemType.Button)
		{
			e.shape = 1;
		}
	}

	public static void Ensure()
	{
		if (!loaded)
		{
			loaded = true;
			ReadManifest();
			if (profileNames.Count == 0)
			{
				profileNames.Add("默认");
			}
			if (!profileNames.Contains(activeName))
			{
				activeName = profileNames[0];
			}
			if (!LoadFromFile(activeName))
			{
				elems.Clear();
				BuildDefaultLayout();
				SaveActive();
			}
			MigrateStyle();
		}
	}

	private static void MigrateStyle()
	{
		if (PlayerPrefs.GetInt("mxd.vc.stylever", 0) >= 3)
		{
			return;
		}
		foreach (VCElem elem in elems)
		{
			ApplyFlatSkin(elem);
		}
		SaveActive();
		PlayerPrefs.SetInt("mxd.vc.stylever", 3);
		PlayerPrefs.Save();
	}

	private static void ReadManifest()
	{
		profileNames.Clear();
		try
		{
			if (!File.Exists(ManifestPath))
			{
				activeName = "默认";
			}
			else
			{
				if (!(MiniJson.Parse(File.ReadAllText(ManifestPath)) is Dictionary<string, object> dictionary))
				{
					return;
				}
				if (dictionary.TryGetValue("active", out var value) && value is string text)
				{
					activeName = text;
				}
				if (!dictionary.TryGetValue("profiles", out var value2) || !(value2 is List<object> list))
				{
					return;
				}
				{
					foreach (object item2 in list)
					{
						if (item2 is string item && !profileNames.Contains(item))
						{
							profileNames.Add(item);
						}
					}
					return;
				}
			}
		}
		catch
		{
		}
	}

	private static void WriteManifest()
	{
		try
		{
			Directory.CreateDirectory(Dir);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("{\"active\":").Append(Q(activeName)).Append(",\"profiles\":[");
			for (int i = 0; i < profileNames.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(Q(profileNames[i]));
			}
			stringBuilder.Append("]}");
			File.WriteAllText(ManifestPath, stringBuilder.ToString());
		}
		catch
		{
		}
	}

	public static bool LoadFromFile(string name)
	{
		try
		{
			if (!File.Exists(ProfilePath(name)))
			{
				return false;
			}
			return ParseElements(File.ReadAllText(ProfilePath(name)));
		}
		catch
		{
			return false;
		}
	}

	public static string ToJson()
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected I4, but got Unknown
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected I4, but got Unknown
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Expected I4, but got Unknown
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Expected I4, but got Unknown
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Expected I4, but got Unknown
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\"elements\":[");
		for (int i = 0; i < elems.Count; i++)
		{
			VCElem vCElem = elems[i];
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append("{\"t\":").Append(Q(vCElem.type.ToString())).Append(",\"a\":")
				.Append(Q(vCElem.action.ToString()))
				.Append(",\"k\":")
				.Append((int)vCElem.key)
				.Append(",\"sm\":")
				.Append(vCElem.shiftMod ? 1 : 0)
				.Append(",\"x\":")
				.Append(N(vCElem.cx))
				.Append(",\"y\":")
				.Append(N(vCElem.cy))
				.Append(",\"s\":")
				.Append(N(vCElem.size))
				.Append(",\"shape\":")
				.Append(vCElem.shape)
				.Append(",\"op\":")
				.Append(N(vCElem.opacity))
				.Append(",\"sc\":")
				.Append(N(vCElem.scale))
				.Append(",\"rot\":")
				.Append(N(vCElem.rotation))
				.Append(",\"asp\":")
				.Append(N(vCElem.aspect))
				.Append(",\"bw\":")
				.Append(N(vCElem.borderW))
				.Append(",\"hl\":")
				.Append(N(vCElem.halo))
				.Append(",\"sbg\":")
				.Append(Q(Hex(vCElem.bg)))
				.Append(",\"sln\":")
				.Append(Q(Hex(vCElem.line)))
				.Append(",\"dbg\":")
				.Append(Q(Hex(vCElem.bgDown)))
				.Append(",\"dln\":")
				.Append(Q(Hex(vCElem.lineDown)))
				.Append(",\"label\":")
				.Append(Q(vCElem.label ?? ""))
				.Append(",\"ssd\":")
				.Append(vCElem.stickStd ? 1 : 0)
				.Append(",\"ku\":")
				.Append((int)vCElem.upKey)
				.Append(",\"kd\":")
				.Append((int)vCElem.downKey)
				.Append(",\"kl\":")
				.Append((int)vCElem.leftKey)
				.Append(",\"kr\":")
				.Append((int)vCElem.rightKey)
				.Append('}');
		}
		stringBuilder.Append("]}");
		return stringBuilder.ToString();
	}

	public static void FromJson(string json)
	{
		if (!string.IsNullOrEmpty(json))
		{
			ParseElements(json);
		}
	}

	private static bool ParseElements(string json)
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!(MiniJson.Parse(json) is Dictionary<string, object> dictionary))
			{
				return false;
			}
			List<object> list = (dictionary.TryGetValue("elements", out var value) ? (value as List<object>) : null);
			if (list == null)
			{
				return false;
			}
			List<VCElem> list2 = new List<VCElem>();
			foreach (object item in list)
			{
				if (item is Dictionary<string, object> dictionary2)
				{
					VCElem vCElem = new VCElem();
					if (dictionary2.TryGetValue("t", out var value2))
					{
						Enum.TryParse<VCElemType>(value2 as string, out vCElem.type);
					}
					if (dictionary2.TryGetValue("a", out var value3))
					{
						Enum.TryParse<GameAction>(value3 as string, out vCElem.action);
					}
					if (dictionary2.TryGetValue("k", out var value4) && value4 is double num && Enum.IsDefined(typeof(KeyCode), (int)num))
					{
						vCElem.key = (KeyCode)(int)num;
					}
					if (dictionary2.TryGetValue("sm", out var value5))
					{
						vCElem.shiftMod = ToF(value5, 0f) > 0.5f;
					}
					if (dictionary2.TryGetValue("x", out var value6))
					{
						vCElem.cx = ToF(value6, vCElem.cx);
					}
					if (dictionary2.TryGetValue("y", out var value7))
					{
						vCElem.cy = ToF(value7, vCElem.cy);
					}
					if (dictionary2.TryGetValue("s", out var value8))
					{
						vCElem.size = ToF(value8, vCElem.size);
					}
					if (dictionary2.TryGetValue("shape", out var value9))
					{
						vCElem.shape = Mathf.RoundToInt(ToF(value9, 0f));
					}
					if (dictionary2.TryGetValue("op", out var value10))
					{
						vCElem.opacity = ToF(value10, vCElem.opacity);
					}
					if (dictionary2.TryGetValue("sc", out var value11))
					{
						vCElem.scale = ToF(value11, vCElem.scale);
					}
					if (dictionary2.TryGetValue("rot", out var value12))
					{
						vCElem.rotation = ToF(value12, vCElem.rotation);
					}
					if (dictionary2.TryGetValue("asp", out var value13))
					{
						vCElem.aspect = ToF(value13, vCElem.aspect);
					}
					if (dictionary2.TryGetValue("bw", out var value14))
					{
						vCElem.borderW = ToF(value14, vCElem.borderW);
					}
					if (dictionary2.TryGetValue("hl", out var value15))
					{
						vCElem.halo = ToF(value15, vCElem.halo);
					}
					if (dictionary2.TryGetValue("sbg", out var value16))
					{
						vCElem.bg = ParseHex(value16 as string, vCElem.bg);
					}
					if (dictionary2.TryGetValue("sln", out var value17))
					{
						vCElem.line = ParseHex(value17 as string, vCElem.line);
					}
					if (dictionary2.TryGetValue("dbg", out var value18))
					{
						vCElem.bgDown = ParseHex(value18 as string, vCElem.bgDown);
					}
					if (dictionary2.TryGetValue("dln", out var value19))
					{
						vCElem.lineDown = ParseHex(value19 as string, vCElem.lineDown);
					}
					if (!dictionary2.ContainsKey("sbg"))
					{
						ApplyFlatSkin(vCElem);
					}
					if (dictionary2.TryGetValue("label", out var value20))
					{
						vCElem.label = (value20 as string) ?? "";
					}
					if (dictionary2.TryGetValue("ssd", out var value21))
					{
						vCElem.stickStd = ToF(value21, 1f) > 0.5f;
					}
					if (dictionary2.TryGetValue("ku", out var value22) && value22 is double num2 && Enum.IsDefined(typeof(KeyCode), (int)num2))
					{
						vCElem.upKey = (KeyCode)(int)num2;
					}
					if (dictionary2.TryGetValue("kd", out var value23) && value23 is double num3 && Enum.IsDefined(typeof(KeyCode), (int)num3))
					{
						vCElem.downKey = (KeyCode)(int)num3;
					}
					if (dictionary2.TryGetValue("kl", out var value24) && value24 is double num4 && Enum.IsDefined(typeof(KeyCode), (int)num4))
					{
						vCElem.leftKey = (KeyCode)(int)num4;
					}
					if (dictionary2.TryGetValue("kr", out var value25) && value25 is double num5 && Enum.IsDefined(typeof(KeyCode), (int)num5))
					{
						vCElem.rightKey = (KeyCode)(int)num5;
					}
					if (vCElem.borderW <= 1.05f)
					{
						vCElem.borderW = 2.2f;
					}
					list2.Add(vCElem);
				}
			}
			elems = list2;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static float ToF(object o, float fallback)
	{
		if (!(o is double num) || double.IsNaN(num))
		{
			return fallback;
		}
		return (float)num;
	}

	public static void SaveActive()
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(ProfilePath(activeName), ToJson());
			if (!profileNames.Contains(activeName))
			{
				profileNames.Add(activeName);
			}
			WriteManifest();
		}
		catch
		{
		}
	}

	public static void NewProfile(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			name = "方案" + (profileNames.Count + 1);
		}
		string item = name;
		int num = 2;
		while (profileNames.Contains(item))
		{
			item = name + "_" + num++;
		}
		SaveActive();
		activeName = item;
		elems = new List<VCElem>();
		BuildDefaultLayout();
		SaveActive();
	}

	public static void DeleteProfile(string name)
	{
		if (profileNames.Count <= 1)
		{
			return;
		}
		profileNames.Remove(name);
		try
		{
			if (File.Exists(ProfilePath(name)))
			{
				File.Delete(ProfilePath(name));
			}
		}
		catch
		{
		}
		if (activeName == name)
		{
			activeName = profileNames[0];
			if (!LoadFromFile(activeName))
			{
				elems.Clear();
				BuildDefaultLayout();
			}
		}
		WriteManifest();
	}

	public static void RestoreDefaultLayout()
	{
		elems.Clear();
		BuildDefaultLayout();
		SaveActive();
	}

	private static void BuildDefaultLayout()
	{
		elems.Add(new VCElem
		{
			type = VCElemType.Stick,
			cx = 0.1f,
			cy = 0.75507903f,
			size = 0.31376976f,
			opacity = 0.6f
		});
		AddDefaultKey((KeyCode)49, 106f, 82f);
		AddDefaultKey((KeyCode)50, 231f, 82f);
		AddDefaultKey((KeyCode)51, 351f, 82f);
		AddDefaultKey((KeyCode)9, 491f, 82f);
		AddDefaultKey((KeyCode)52, 106f, 227f);
		AddDefaultKey((KeyCode)53, 231f, 230f);
		AddDefaultKey((KeyCode)54, 351f, 232f);
		AddDefaultKey((KeyCode)48, 491f, 242f);
		AddDefaultKey((KeyCode)55, 106f, 347f);
		AddDefaultKey((KeyCode)56, 231f, 347f);
		AddDefaultKey((KeyCode)57, 351f, 347f);
		AddDefaultKey((KeyCode)113, 1569f, 128f);
		AddDefaultKey((KeyCode)112, 1713f, 128f);
		AddDefaultKey((KeyCode)121, 1849f, 128f);
		AddDefaultKey((KeyCode)107, 1569f, 242f);
		AddDefaultKey((KeyCode)101, 1713f, 242f);
		AddDefaultKey((KeyCode)105, 1849f, 242f);
		AddDefaultKey((KeyCode)97, 1569f, 372f);
		AddDefaultKey((KeyCode)115, 1713f, 372f);
		AddDefaultKey((KeyCode)100, 1849f, 372f);
		AddDefaultKey((KeyCode)122, 1510f, 587f, 140f);
		AddDefaultKey((KeyCode)99, 1673f, 582f, 140f);
		AddDefaultKey((KeyCode)118, 1840f, 582f, 150f);
		AddDefaultKey((KeyCode)120, 1608f, 744f, 140f);
		AddDefaultKey((KeyCode)32, 1785f, 750f, 162f, "Space");
		AddDefaultKey((KeyCode)102, 530f, 796f);
		AddDefaultKey((KeyCode)119, 662f, 796f);
		AddDefaultKey((KeyCode)283, 812f, 796f);
		AddDefaultKey((KeyCode)284, 944f, 796f);
		AddDefaultKey((KeyCode)127, 1076f, 796f, 104f, "删");
		AddDefaultKey((KeyCode)13, 1219f, 796f, 104f, "Enter");
		AddDefaultKey((KeyCode)27, 1355f, 796f, 104f, "Esc");
		foreach (VCElem elem in elems)
		{
			ApplyFlatSkin(elem);
		}
	}

	private static void AddDefaultKey(KeyCode key, float x, float y, float size = 104f, string label = "")
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		elems.Add(new VCElem
		{
			type = VCElemType.Button,
			key = key,
			cx = x / 1920f,
			cy = y / 886f,
			size = size / 886f,
			opacity = 0.75f,
			label = label
		});
	}

	public static string ActionName(VCElem e)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (e.type != VCElemType.Button)
		{
			return "移动";
		}
		if ((int)e.key != 0)
		{
			return KeyLabel(e.key);
		}
		return ActionDisplayName(e.action);
	}

	public static string KeyLabel(KeyCode k)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected I4, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I4
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected I4, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Invalid comparison between Unknown and I4
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Invalid comparison between Unknown and I4
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Invalid comparison between Unknown and I4
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected I4, but got Unknown
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected I4, but got Unknown
		if ((int)k == 0)
		{
			return "未绑定";
		}
		if ((int)k <= 47)
		{
			if ((int)k <= 9)
			{
				if ((int)k == 8)
				{
					return "Bksp";
				}
				if ((int)k == 9)
				{
					return "Tab";
				}
			}
			else
			{
				if ((int)k == 13)
				{
					return "Enter";
				}
				switch (k - 39)
				{
				case 6:
					return "-";
				case 0:
					return "'";
				case 5:
					return ",";
				case 7:
					return ".";
				case 8:
					return "/";
				}
			}
		}
		else if ((int)k <= 61)
		{
			if ((int)k == 59)
			{
				return ";";
			}
			if ((int)k == 61)
			{
				return "=";
			}
		}
		else
		{
			switch (k - 91)
			{
			default:
				if ((int)k != 301)
				{
					break;
				}
				return "Caps";
			case 5:
				return "`";
			case 0:
				return "[";
			case 2:
				return "]";
			case 1:
				return "\\";
			case 3:
			case 4:
				break;
			}
		}
		if ((int)k >= 48 && (int)k <= 57)
		{
			return (k - 48).ToString();
		}
		if ((int)k >= 256 && (int)k <= 265)
		{
			return "小键盘" + (k - 256);
		}
		string text = KeyMap.Label(k);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return ((object)k/*cast due to constrained. prefix*/).ToString();
	}

	public static GameAction? ActionOfKey(KeyCode k)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if ((int)k == 0)
		{
			return null;
		}
		foreach (KeyValuePair<GameAction, KeyCode> item in KeyMap.All)
		{
			if (item.Value == k)
			{
				return item.Key;
			}
		}
		return null;
	}

	public static int SlotOfKey(KeyCode k)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if ((int)k == 0)
		{
			return -1;
		}
		for (int i = 0; i < 26; i++)
		{
			if (KeyMap.Of(QuickSlots.Action(i)) == k)
			{
				return i;
			}
		}
		return -1;
	}

	public static string ActionDisplayName(GameAction action)
	{
		switch (action)
		{
		case GameAction.Jump:
			return "跳跃";
		case GameAction.Attack:
			return "攻击";
		case GameAction.Pickup:
			return "拾取";
		case GameAction.Equip:
			return "装备";
		case GameAction.Inven:
			return "背包";
		case GameAction.Stat:
			return "能力值";
		case GameAction.Skill:
			return "技能";
		case GameAction.KeySet:
			return "键盘设置";
		case GameAction.QuickSlot:
			return "快捷栏";
		case GameAction.Menu:
			return "菜单";
		case GameAction.Shop:
			return "商店";
		case GameAction.Shortcut:
			return "快捷键";
		case GameAction.Help:
			return "帮助";
		default:
		{
			int num = QuickSlots.Index(action);
			if (num >= 0)
			{
				return "技能格 " + (num + 1);
			}
			NativeKeyActions.Entry entry = NativeKeyActions.Of(action);
			if (entry == null)
			{
				return action.ToString();
			}
			return entry.name;
		}
		}
	}

	public static void Tick(bool feed)
	{
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected I4, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		UpdateLift();
		if (!feed || !Shown || CarryingSkill || !Application.isFocused)
		{
			ReleaseAll();
			return;
		}
		bool flag = Input.touchCount > 0;
		if (flag)
		{
			VCElem vCElem = FindFinger(-2);
			if (vCElem != null)
			{
				Release(vCElem);
			}
			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch touch = Input.GetTouch(i);
				Vector2 p = new Vector2(touch.position.x / UiKit.Scale, ((float)Screen.height - touch.position.y) / UiKit.Scale);
				TouchPhase phase = touch.phase;
				switch ((int)phase)
				{
				case 0:
					if (FindFinger(touch.fingerId) == null)
					{
						VCElem vCElem3 = HitElement(p);
						if (vCElem3 != null)
						{
							vCElem3.finger = touch.fingerId;
							vCElem3.held = true;
							OnPress(vCElem3, p);
						}
					}
					break;
				case 1:
				case 2:
				{
					VCElem vCElem4 = FindFinger(touch.fingerId);
					if (vCElem4 != null)
					{
						OnDrag(vCElem4, p);
					}
					break;
				}
				case 3:
				case 4:
				{
					VCElem vCElem2 = FindFinger(touch.fingerId);
					if (vCElem2 != null)
					{
						Release(vCElem2);
					}
					break;
				}
				}
			}
		}
		else
		{
			Vector2 p2 = ToGui(Vector2.op_Implicit(Input.mousePosition));
			if (Input.GetMouseButtonDown(0))
			{
				VCElem vCElem5 = HitElement(p2);
				if (vCElem5 != null)
				{
					vCElem5.finger = -2;
					vCElem5.held = true;
					OnPress(vCElem5, p2);
				}
			}
			else if (Input.GetMouseButton(0))
			{
				VCElem vCElem6 = FindFinger(-2);
				if (vCElem6 != null)
				{
					OnDrag(vCElem6, p2);
				}
			}
			else
			{
				for (int num = elems.Count - 1; num >= 0; num--)
				{
					if (elems[num].finger != -1)
					{
						Release(elems[num]);
					}
				}
			}
		}
		float num2 = 0f;
		float num3 = 0f;
		foreach (VCElem elem in elems)
		{
			if (elem.finger != -1 && elem.type != VCElemType.Button && (elem.type != VCElemType.Stick || elem.stickStd))
			{
				num2 += elem.axis.x;
				num3 += elem.axis.y;
			}
		}
		InputHub.SetVirtualAxis(Mathf.Clamp(num2, -1f, 1f), Mathf.Clamp(num3, -1f, 1f));
		bool flag2 = HitElement(flag ? Pointer.Pos : ToGui(Vector2.op_Implicit(Input.mousePosition))) != null;
		if (flag)
		{
			for (int j = 0; j < Input.touchCount; j++)
			{
				if (flag2)
				{
					break;
				}
				Touch touch2 = Input.GetTouch(j);
				flag2 = HitElement(ToGui(touch2.position)) != null;
			}
		}
		BlockingPointer = flag2 || wasOnControl;
		wasOnControl = AnyHeld();
	}

	private static Vector2 ToGui(Vector2 screenPos)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(screenPos.x / UiKit.Scale, ((float)Screen.height - screenPos.y) / UiKit.Scale);
	}

	public static bool HitsAtScreen(Vector2 screenPos)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!Capturing)
		{
			return false;
		}
		Ensure();
		return HitElement(new Vector2(screenPos.x / UiKit.Scale, ((float)Screen.height - screenPos.y) / UiKit.Scale)) != null;
	}

	private static bool AnyHeld()
	{
		for (int i = 0; i < elems.Count; i++)
		{
			if (elems[i].finger != -1)
			{
				return true;
			}
		}
		return false;
	}

	private static VCElem FindFinger(int id)
	{
		for (int i = 0; i < elems.Count; i++)
		{
			if (elems[i].finger == id)
			{
				return elems[i];
			}
		}
		return null;
	}

	private static VCElem HitElement(Vector2 p)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (!GamePlatform.TouchControls)
		{
			return null;
		}
		for (int num = elems.Count - 1; num >= 0; num--)
		{
			if (KeyConfigWindow.Visible && !CarryingSkill)
			{
				if (elems[num].type != VCElemType.Button)
				{
					continue;
				}
				NativeKeyActions.Entry entry = NativeKeyActions.Of(EffectiveAction(elems[num]));
				if (entry == null || entry.Type != 4)
				{
					continue;
				}
			}
			if ((!NpcSession.Blocking || (elems[num].type == VCElemType.Button && NativeKeyActions.AvailableDuringNpcDialog(EffectiveAction(elems[num])))) && elems[num].Hit(p))
			{
				return elems[num];
			}
		}
		return null;
	}

	public static VCElem HitAtScreen(Vector2 screenPos)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		return HitElement(new Vector2(screenPos.x / UiKit.Scale, ((float)Screen.height - screenPos.y) / UiKit.Scale));
	}

	public static bool HitButtonAt(Vector2 guiPos)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		VCElem vCElem = HitElement(guiPos);
		if (vCElem != null)
		{
			return vCElem.type == VCElemType.Button;
		}
		return false;
	}

	public static string ElementCaption(VCElem e)
	{
		if (e != null)
		{
			return ActionName(e);
		}
		return "";
	}

	private static void OnPress(VCElem e, Vector2 p)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		switch (e.type)
		{
		case VCElemType.Button:
			anyPushed = true;
			e.held = true;
			SendButton(e, down: true);
			break;
		case VCElemType.Stick:
			UpdateStick(e, p);
			break;
		case VCElemType.Dpad:
			UpdateDpad(e, p);
			break;
		}
	}

	public static GameAction EffectiveAction(VCElem e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ActionOfKey(e.Key) ?? e.action;
	}

	private static void SendButton(VCElem e, bool down)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		if ((((int)e.key != 13 && (int)e.key != 271) || ((!down || !BottomBar.QueueChatEnter()) && (down || !BottomBar.ChatFocused))) && (!down || !BottomBar.ChatFocused || (int)e.key == 13 || (int)e.key == 271))
		{
			GameAction? gameAction = (((int)e.key != 0) ? ActionOfKey(e.key) : new GameAction?(EffectiveAction(e)));
			if ((int)e.key == 0)
			{
				InputHub.SetVirtualAction(gameAction.Value, down);
			}
			if (e.shiftMod)
			{
				InputHub.SetVirtual((KeyCode)304, down);
			}
			if ((int)e.key != 0)
			{
				InputHub.SetVirtual(e.key, down);
			}
			if (down)
			{
				lastPress = (((int)e.key != 0) ? (KeyLabel(e.key) + " → ") : "") + (gameAction.HasValue ? ActionDisplayName(gameAction.Value) : "未绑定功能");
				string text;
				if ((int)e.key != 0)
				{
					text = ((!gameAction.HasValue) ? "请在键盘设定中给此键绑定功能" : "");
				}
				else
				{
					text = "按动作触发";
				}
				lastNote = text;
				string[] array = new string[7]
				{
					"[Virtual] 按下 ",
					((int)e.key == 0) ? "无键" : ((object)e.key/*cast due to constrained. prefix*/).ToString(),
					" → ",
					null,
					null,
					null,
					null
				};
				GameAction? gameAction2 = gameAction;
				array[3] = gameAction2.ToString();
				array[4] = "（攻击键位=";
				array[5] = ((object)KeyMap.Of(GameAction.Attack)/*cast due to constrained. prefix*/).ToString();
				array[6] = "）";
				Debug.Log((object)string.Concat(array));
			}
		}
	}

	private static void OnDrag(VCElem e, Vector2 p)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (e.type == VCElemType.Stick)
		{
			UpdateStick(e, p);
		}
		else if (e.type == VCElemType.Dpad)
		{
			UpdateDpad(e, p);
		}
	}

	private static void UpdateStick(VCElem e, Vector2 p)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = Center(e);
		float r = e.R;
		Vector2 val2 = p - val;
		float magnitude = val2.magnitude;
		if (magnitude > r)
		{
			val2 = val2 / magnitude * r;
		}
		float num = 0.14f;
		float num2 = magnitude / r;
		num2 = ((num2 <= num) ? 0f : ((num2 - num) / (1f - num)));
		Vector2 val3 = new Vector2(val2.x, 0f - val2.y);
		e.axis = ((magnitude < 0.001f) ? Vector2.zero : (val3.normalized * Mathf.Clamp01(num2)));
		SyncStickKeys(e);
	}

	private static void SyncStickKeys(VCElem e)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (e.type == VCElemType.Stick && !e.stickStd)
		{
			PressDir(e.upKey, e.axis.y > 0.38f);
			PressDir(e.downKey, e.axis.y < -0.38f);
			PressDir(e.leftKey, e.axis.x < -0.38f);
			PressDir(e.rightKey, e.axis.x > 0.38f);
		}
	}

	private static void PressDir(KeyCode k, bool down)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		if ((int)k != 0)
		{
			InputHub.SetVirtual(k, down);
		}
	}

	public static void ReleaseStickKeys(VCElem e)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (e != null)
		{
			PressDir(e.upKey, down: false);
			PressDir(e.downKey, down: false);
			PressDir(e.leftKey, down: false);
			PressDir(e.rightKey, down: false);
		}
	}

	private static void UpdateDpad(VCElem e, Vector2 p)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = Center(e);
		Vector2 val2 = p - val;
		float num = e.R * 0.22f;
		float num2 = 0f;
		float num3 = 0f;
		if (val2.x < 0f - num)
		{
			num2 = -1f;
		}
		else if (val2.x > num)
		{
			num2 = 1f;
		}
		if (val2.y > num)
		{
			num3 = -1f;
		}
		else if (val2.y < 0f - num)
		{
			num3 = 1f;
		}
		e.axis = new Vector2(num2, num3);
	}

	private static void Release(VCElem e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		e.finger = -1;
		e.axis = Vector2.zero;
		SyncStickKeys(e);
		if (e.type == VCElemType.Button && e.held)
		{
			SendButton(e, down: false);
		}
		e.held = false;
	}

	public static void ReleaseForEdit(VCElem e)
	{
		Release(e);
	}

	private static void ReleaseAll()
	{
		for (int num = elems.Count - 1; num >= 0; num--)
		{
			if (elems[num].finger != -1)
			{
				Release(elems[num]);
			}
		}
		if (anyPushed)
		{
			foreach (VCElem elem in elems)
			{
				if (elem.type == VCElemType.Button && elem.held)
				{
					SendButton(elem, down: false);
				}
			}
			anyPushed = false;
		}
		InputHub.ClearVirtualAxis();
		BlockingPointer = false;
		wasOnControl = false;
	}

	public static void Draw()
	{
		Ensure();
		GameLoop i = GameLoop.I;
		if (!((Object)(object)i != (Object)null) || i.state != GameLoop.GameState.Playing || VirtualControlsEditor.Visible || !Shown)
		{
			return;
		}
		float dim = (KeyConfigWindow.Visible ? 0.42f : 1f);
		foreach (VCElem elem in elems)
		{
			DrawElement(elem, dim);
		}
		if (!KeyConfigWindow.Visible)
		{
			if (Input.GetKeyDown((KeyCode)290))
			{
				ShowDebug = !ShowDebug;
			}
			if (ShowDebug)
			{
				DrawDebug();
			}
		}
	}

	public static void DrawDropTargets()
	{
		SkillWindow.DrawDragGhost();
	}

	private static void DrawDebug()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected Obj, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		float num = 28f + (float)(elems.Count + 1) * 20f + 24f;
		Rect r = new Rect(10f, UiKit.H * 0.3f, 420f, num);
		UiKit.Fill(r, new Color(0f, 0f, 0f, 0.72f));
		GUIStyle val = new GUIStyle
		{
			fontSize = 13
		};
		val.normal.textColor = Color.white;
		GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 20f, 18f), "虚拟键调试（F9 关闭）", val);
		float num2 = r.y + 26f;
		foreach (VCElem elem in elems)
		{
			if (elem.type != VCElemType.Button)
			{
				GUI.Label(new Rect(r.x + 10f, num2, r.width - 20f, 18f), ((elem.type == VCElemType.Stick) ? "摇杆 " : "十字 ") + elem.axis.ToString("0.00"), val);
				num2 += 20f;
				continue;
			}
			GameAction gameAction = EffectiveAction(elem);
			GameAction? gameAction2 = ActionOfKey(elem.Key);
			string text = KeyLabel(elem.Key) + " | 实际:" + ActionDisplayName(gameAction) + (gameAction2.HasValue ? "(键映射)" : ("(" + ActionDisplayName(elem.action) + ")")) + (elem.held ? " | 按下" : "") + (InputHub.Held(gameAction) ? " | 游戏收到✓" : "");
			GUI.Label(new Rect(r.x + 10f, num2, r.width - 20f, 18f), text, val);
			num2 += 20f;
		}
		GUI.Label(new Rect(r.x + 10f, num2, r.width - 20f, 18f), "攻击键位=" + KeyLabel(KeyMap.Of(GameAction.Attack)) + " | 最近：" + lastPress, val);
	}

	public static void DrawElement(VCElem e)
	{
		DrawElement(e, 1f);
	}

	public static void DrawElement(VCElem e, float dim)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		bool flag = ((e.type == VCElemType.Button) ? e.held : (e.finger != -1));
		float num = Mathf.Clamp(e.opacity, 0.05f, 1f) * Mathf.Clamp01(dim);
		Color val = (flag ? e.bgDown : e.bg);
		Color val2 = (flag ? e.lineDown : e.line);
		val.a *= num;
		val2.a *= num;
		Vector2 val3 = Center(e);
		float baseR = e.BaseR;
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(val3.x, val3.y, 0f), Quaternion.Euler(0f, 0f, e.rotation), new Vector3(Mathf.Max(0.05f, e.aspect) * Mathf.Max(0.05f, e.scale), Mathf.Max(0.05f, e.scale), 1f));
		if (e.type == VCElemType.Stick)
		{
			DrawStick(e, baseR, val, val2, flag, num);
		}
		else if (e.type == VCElemType.Dpad)
		{
			float num2 = Mathf.Max(e.borderW, baseR * 0.032f);
			if (e.halo > 0.004f)
			{
				Color col = val2;
				col.a *= 0.45f;
				DrawAnnulus(Vector2.zero, baseR + num2 * 0.5f, baseR + num2 * 0.5f + e.halo * baseR, col, fade: true);
			}
			FillDisc(Vector2.zero, Mathf.Max(0.1f, baseR - num2 * 0.5f), val);
			DrawAnnulus(Vector2.zero, Mathf.Max(0f, baseR - num2 * 0.5f), baseR + num2 * 0.5f, val2, fade: false);
			float r = baseR * 0.3f;
			DrawDirCap(new Vector2(0f, (0f - baseR) * 0.62f), r, e.axis.y > 0f, val, val2);
			DrawDirCap(new Vector2(0f, baseR * 0.62f), r, e.axis.y < 0f, val, val2);
			DrawDirCap(new Vector2((0f - baseR) * 0.62f, 0f), r, e.axis.x < 0f, val, val2);
			DrawDirCap(new Vector2(baseR * 0.62f, 0f), r, e.axis.x > 0f, val, val2);
		}
		else
		{
			DrawShape(e.shape, baseR, val, val2, e.borderW, e.halo);
			Rect r2 = new Rect(0f - baseR, 0f - baseR, baseR * 2f, baseR * 2f);
			Texture2D val4 = SlotIcon(e);
			if ((Object)(object)val4 != (Object)null)
			{
				Color color = GUI.color;
				GUI.color = new Color(1f, 1f, 1f, num);
				float num3 = Mathf.Min(1f, Mathf.Min(r2.width / (float)((Texture)val4).width, r2.height / (float)((Texture)val4).height));
				Rect val5 = new Rect((float)(-((Texture)val4).width) * num3 * 0.5f, (float)(-((Texture)val4).height) * num3 * 0.5f, (float)((Texture)val4).width * num3, (float)((Texture)val4).height * num3);
				GUI.DrawTexture(val5, (Texture)(object)val4, (ScaleMode)2);
				GUI.color = color;
				DrawCooldown(e, val5, num);
			}
			else
			{
				string text = ElementText(e);
				if (!string.IsNullOrEmpty(text))
				{
					float num4 = baseR * 0.6f;
					int length = text.Length;
					if (length >= 2)
					{
						num4 *= length switch
						{
							3 => 0.68f, 
							2 => 0.86f, 
							_ => 0.52f, 
						};
					}
					ShadowLabel(r2, text, Mathf.Max(9, Mathf.RoundToInt(num4)));
				}
			}
		}
		GUI.matrix = matrix;
	}

	private static void DrawCooldown(VCElem e, Rect rect, float a)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		int num = ButtonSkill(e);
		if (num <= 0)
		{
			return;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		SkillSystem skillSystem = (((Object)(object)playerCtrl != (Object)null) ? ((Component)playerCtrl).GetComponent<SkillSystem>() : null);
		if ((Object)(object)skillSystem == (Object)null)
		{
			return;
		}
		float num2 = skillSystem.RemainingById(num);
		if (!(num2 <= 0.001f))
		{
			float num3 = Mathf.Max(0.01f, skillSystem.CooldownOf(num));
			Texture2D val = Art.Tex("ui/skillwin/cool_" + Mathf.Clamp(Mathf.RoundToInt((1f - Mathf.Clamp01(num2 / num3)) * 15f), 0, 15) + ".png");
			if (!((Object)(object)val == (Object)null))
			{
				Color color = GUI.color;
				GUI.color = new Color(1f, 1f, 1f, a);
				GUI.DrawTexture(rect, (Texture)(object)val);
				GUI.color = color;
			}
		}
	}

	private static void DrawStick(VCElem e, float r, Color bg, Color line, bool active, float op)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(e.borderW, r * 0.022f);
		float num2 = r + num * 0.5f;
		bool flag = line.a > 0.004f;
		if ((e.halo > 0.004f) & flag)
		{
			Color col = line;
			col.a *= 0.38f;
			DrawAnnulus(Vector2.zero, num2, num2 + e.halo * r, col, fade: true);
		}
		FillDisc(Vector2.zero, Mathf.Max(0.1f, r - num * 0.5f), bg);
		float num3 = r * 0.78f;
		FillDisc(Vector2.zero, num3, new Color(1f, 1f, 1f, 0.055f * op));
		if (flag)
		{
			Color col2 = line;
			col2.a *= 0.4f;
			float num4 = Mathf.Max(1.2f, num * 0.5f);
			DrawAnnulus(Vector2.zero, Mathf.Max(0f, num3 - num4 * 0.5f), num3 + num4 * 0.5f, col2, fade: false);
		}
		if (flag)
		{
			DrawAnnulus(Vector2.zero, Mathf.Max(0f, r - num * 0.5f), num2, line, fade: false);
		}
		float num5 = r * 0.4f;
		Vector2 val = ((e.finger != -1) ? (new Vector2(e.axis.x, 0f - e.axis.y) * (r - num5)) : Vector2.zero);
		FillDisc(val + new Vector2(0f, num5 * 0.12f), num5 * 1.02f, new Color(0f, 0f, 0f, 0.22f * op));
		FillBall(val, num5, active ? new Color(0.74f, 0.78f, 0.82f, op) : new Color(0.6f, 0.63f, 0.66f, op));
		if (flag)
		{
			Color col3 = line;
			col3.a *= 0.32f;
			DrawAnnulus(val, Mathf.Max(0f, num5 - 1.2f), num5 + 1.2f, col3, fade: false);
		}
	}

	private static void FillBall(Vector2 c, float r, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), (Texture)(object)BallTex());
		GUI.color = color;
	}

	public static void DrawShape(int shape, float r, Color bg, Color line, float bw, float halo)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		if (shape == 7)
		{
			return;
		}
		float num = Mathf.Max(bw, r * 0.032f);
		bool flag = line.a > 0.004f && num > 0.05f;
		bool flag2 = (halo > 0.004f) & flag;
		if (shape == 1)
		{
			DrawRectShape(r, bg, line, num, halo, flag, flag2);
			return;
		}
		if (shape >= 2)
		{
			DrawPolyShape(shape, r, bg, line, num, halo, flag, flag2);
			return;
		}
		if (flag2)
		{
			Color col = line;
			col.a *= 0.45f;
			float num2 = r + num * 0.5f;
			DrawAnnulus(Vector2.zero, num2, num2 + halo * r, col, fade: true);
		}
		FillDisc(Vector2.zero, Mathf.Max(0.1f, r - num * 0.5f), bg);
		if (flag)
		{
			DrawAnnulus(Vector2.zero, Mathf.Max(0f, r - num * 0.5f), r + num * 0.5f, line, fade: false);
		}
	}

	private static void DrawRectShape(float r, Color bg, Color line, float w, float halo, bool hasLine, bool hasHalo)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		float num = r * 0.16f;
		if (hasHalo)
		{
			Color col = line;
			col.a *= 0.22f;
			float num2 = halo * r;
			SoftRect(new Rect(0f - r - num2, 0f - r - num2, (r + num2) * 2f, (r + num2) * 2f), col);
			col.a *= 1.3f;
			float num3 = num2 * 0.45f;
			SoftRect(new Rect(0f - r - num3, 0f - r - num3, (r + num3) * 2f, (r + num3) * 2f), col);
		}
		SoftRect(new Rect(0f - r, 0f - r, r * 2f, r * 2f), bg);
		if (hasLine)
		{
			float num4 = Mathf.Max(0f, r - num);
			DrawLine(new Vector2(0f - num4, 0f - r), new Vector2(num4, 0f - r), line, w);
			DrawLine(new Vector2(0f - num4, r), new Vector2(num4, r), line, w);
			DrawLine(new Vector2(0f - r, 0f - num4), new Vector2(0f - r, num4), line, w);
			DrawLine(new Vector2(r, 0f - num4), new Vector2(r, num4), line, w);
			int seg = Mathf.Clamp(Mathf.CeilToInt(num * 1.2f), 4, 20);
			Arc(new Vector2(0f - num4, 0f - num4), num, MathF.PI, 4.712389f, seg, line, w);
			Arc(new Vector2(num4, 0f - num4), num, 4.712389f, MathF.PI * 2f, seg, line, w);
			Arc(new Vector2(num4, num4), num, 0f, MathF.PI / 2f, seg, line, w);
			Arc(new Vector2(0f - num4, num4), num, MathF.PI / 2f, MathF.PI, seg, line, w);
		}
	}

	private static void Arc(Vector2 c, float rad, float a0, float a1, int seg, Color col, float w)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 a2 = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * rad;
		for (int i = 1; i <= seg; i++)
		{
			float num = Mathf.Lerp(a0, a1, (float)i / (float)seg);
			Vector2 val = c + new Vector2(Mathf.Cos(num), Mathf.Sin(num)) * rad;
			DrawLine(a2, val, col, w);
			a2 = val;
		}
	}

	private static void DrawPolyShape(int shape, float r, Color bg, Color line, float w, float halo, bool hasLine, bool hasHalo)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = ShapePts(shape, r);
		if (hasHalo)
		{
			Color col = line;
			col.a *= 0.22f;
			FillPoly(OffsetPoly(array, w * 0.5f + halo * r), col);
		}
		FillPoly(array, bg);
		if (hasLine)
		{
			for (int i = 0; i < array.Length; i++)
			{
				DrawLine(array[i], array[(i + 1) % array.Length], line, w);
				FillDisc(array[i], w * 0.5f, line);
			}
		}
	}

	public static Vector2[] OffsetPoly(Vector2[] pts, float d)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		int num = pts.Length;
		Vector2[] array = new Vector2[num];
		Vector2 val = Vector2.zero;
		for (int i = 0; i < num; i++)
		{
			val += pts[i];
		}
		val /= (float)num;
		for (int j = 0; j < num; j++)
		{
			Vector2 val2 = pts[j];
			Vector2 a = pts[(j - 1 + num) % num];
			Vector2 b = pts[(j + 1) % num];
			Vector2 val3 = OutNormal(a, val2, val);
			Vector2 val4 = OutNormal(val2, b, val);
			Vector2 val5 = val3 + val4;
			if (val5.sqrMagnitude < 0.0001f)
			{
				array[j] = val2 + val3 * d;
				continue;
			}
			val5.Normalize();
			float num2 = Mathf.Max(0.3f, Vector2.Dot(val5, val3));
			array[j] = val2 + val5 * (d / num2);
		}
		return array;
	}

	private static Vector2 OutNormal(Vector2 a, Vector2 b, Vector2 cen)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = b - a;
		Vector2 normalized = val.normalized;
		Vector2 val2 = new Vector2(0f - normalized.y, normalized.x);
		if (Vector2.Dot(val2, cen - a) > 0f)
		{
			return -val2;
		}
		return val2;
	}

	public static Vector2[] ShapePts(int shape, float r)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (shape == 2)
		{
			Vector2[] array = new Vector2[6];
			for (int i = 0; i < 6; i++)
			{
				float num = MathF.PI / 2f + (float)i * MathF.PI / 3f;
				array[i] = new Vector2(Mathf.Cos(num), Mathf.Sin(num)) * r;
			}
			return array;
		}
		float num2 = r * 0.62f;
		float num3 = r * 0.55f;
		return shape switch
		{
			3 => new Vector2[5]
			{
				new Vector2(0f - r + num3, 0f - num2),
				new Vector2(r, 0f - num2),
				new Vector2(r, num2),
				new Vector2(0f - r, num2),
				new Vector2(0f - r, 0f - num2 + num3)
			}, 
			4 => new Vector2[5]
			{
				new Vector2(0f - r, 0f - num2),
				new Vector2(r, 0f - num2),
				new Vector2(r, num2),
				new Vector2(0f - r + num3, num2),
				new Vector2(0f - r, num2 - num3)
			}, 
			5 => new Vector2[5]
			{
				new Vector2(0f - r, 0f - num2),
				new Vector2(r - num3, 0f - num2),
				new Vector2(r, 0f - num2 + num3),
				new Vector2(r, num2),
				new Vector2(0f - r, num2)
			}, 
			_ => new Vector2[5]
			{
				new Vector2(0f - r, 0f - num2),
				new Vector2(r, 0f - num2),
				new Vector2(r, num2 - num3),
				new Vector2(r - num3, num2),
				new Vector2(0f - r, num2)
			}, 
		};
	}

	public static void FillPoly(Vector2[] pts, Color col)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		if (pts == null || pts.Length < 3 || col.a <= 0.004f)
		{
			return;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		for (int i = 0; i < pts.Length; i++)
		{
			if (pts[i].y < num)
			{
				num = pts[i].y;
			}
			if (pts[i].y > num2)
			{
				num2 = pts[i].y;
			}
		}
		Color color = GUI.color;
		GUI.color = col;
		int num3 = Mathf.FloorToInt(num);
		int num4 = Mathf.CeilToInt(num2);
		for (int j = num3; j <= num4; j++)
		{
			float num5 = (float)j + 0.5f;
			if (num5 < num || num5 > num2)
			{
				continue;
			}
			float num6 = float.MaxValue;
			float num7 = float.MinValue;
			for (int k = 0; k < pts.Length; k++)
			{
				Vector2 val = pts[k];
				Vector2 val2 = pts[(k + 1) % pts.Length];
				if (!Mathf.Approximately(val.y, val2.y) && !(num5 < Mathf.Min(val.y, val2.y)) && !(num5 > Mathf.Max(val.y, val2.y)))
				{
					float num8 = (num5 - val.y) / (val2.y - val.y);
					float num9 = val.x + (val2.x - val.x) * num8;
					if (num9 < num6)
					{
						num6 = num9;
					}
					if (num9 > num7)
					{
						num7 = num9;
					}
				}
			}
			if (!(num7 < num6))
			{
				if (num7 - num6 > 2f)
				{
					GUI.DrawTexture(new Rect(num6 + 1f, (float)j, num7 - num6 - 2f, 1f), (Texture)(object)Texture2D.whiteTexture);
					Color val3 = new Color(1f, 1f, 1f, 0.5f);
					Color color2 = GUI.color;
					GUI.color = color2 * val3;
					GUI.DrawTexture(new Rect(num6, (float)j, 1.2f, 1f), (Texture)(object)Texture2D.whiteTexture);
					GUI.DrawTexture(new Rect(num7 - 1.2f, (float)j, 1.2f, 1f), (Texture)(object)Texture2D.whiteTexture);
					GUI.color = color2;
				}
				else
				{
					GUI.DrawTexture(new Rect(num6, (float)j, Mathf.Max(1f, num7 - num6), 1f), (Texture)(object)Texture2D.whiteTexture);
				}
			}
		}
		GUI.color = color;
	}

	public static void StrokePoly(Vector2[] pts, Color col, float w)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (pts != null && pts.Length >= 2)
		{
			for (int i = 0; i < pts.Length; i++)
			{
				DrawLine(pts[i], pts[(i + 1) % pts.Length], col, w);
			}
		}
	}

	private static int ButtonSkill(VCElem e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (e.type != VCElemType.Button)
		{
			return 0;
		}
		int num = SlotOfKey(e.Key);
		if (num >= 0)
		{
			return QuickSlots.Id(num);
		}
		return 0;
	}

	private static Texture2D SlotIcon(VCElem e)
	{
		return QuickSlots.BindingIcon(ButtonSkill(e));
	}

	private static string ButtonLabel(VCElem e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if ((int)e.key == 0)
		{
			return ActionDisplayName(e.action);
		}
		string text = KeyLabel(e.key);
		if (string.IsNullOrEmpty(text))
		{
			text = ActionName(e);
		}
		if (!e.shiftMod)
		{
			return text;
		}
		return "⇧" + text;
	}

	private static string ElementText(VCElem e)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(e.label))
		{
			return e.label;
		}
		if ((int)e.key != 0)
		{
			return ButtonLabel(e);
		}
		if (!VirtualControlsEditor.Visible)
		{
			return ActionDisplayName(e.action);
		}
		return "";
	}

	private static void DrawDirCap(Vector2 c, float r, bool active, Color bg, Color line)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Ring(c, r, line, Mathf.Max(1f, r * 0.1f));
		FillDisc(c, Mathf.Max(1f, r - r * 0.1f), active ? line : bg);
	}

	private static void FillDisc(Vector2 c, float r, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), (Texture)(object)DiscTex());
		GUI.color = color;
	}

	private static void SoftRect(Rect r, Color col)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = col;
		GUI.DrawTexture(r, (Texture)(object)SoftTex());
		GUI.color = color;
	}

	private static GUIStyle Style(int size, TextAnchor align)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected I4, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected Obj, but got Unknown
		int key = (size << 4) | (align & 0xF);
		if (_styles.TryGetValue(key, out var value) && value != null)
		{
			return value;
		}
		value = new GUIStyle
		{
			alignment = align,
			fontSize = Mathf.Max(6, size),
			richText = false,
			clipping = (TextClipping)0,
			wordWrap = false
		};
		_styles[key] = value;
		return value;
	}

	public static void Label(Rect r, string s, int size, Color col, TextAnchor align, int weight = 1)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		GUIStyle val = Style(size, align);
		val.normal.textColor = col;
		if (weight <= 0)
		{
			GUI.Label(r, s, val);
			return;
		}
		float num = Mathf.Clamp((float)size * 0.03f, 0.6f, 1.6f);
		GUI.Label(new Rect(r.x - num * 0.5f, r.y, r.width, r.height), s, val);
		GUI.Label(new Rect(r.x + num * 0.5f, r.y, r.width, r.height), s, val);
		if (weight >= 2)
		{
			GUI.Label(new Rect(r.x, r.y - num * 0.5f, r.width, r.height), s, val);
			GUI.Label(new Rect(r.x, r.y + num * 0.5f, r.width, r.height), s, val);
		}
	}

	private static void ShadowLabel(Rect r, string text, int size)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = Style(size, (TextAnchor)4);
		float num = Mathf.Clamp((float)size * 0.055f, 1f, 2.2f);
		val.normal.textColor = new Color(0f, 0f, 0f, 0.72f);
		GUI.Label(new Rect(r.x, r.y + num, r.width, r.height), text, val);
		GUI.Label(new Rect(r.x - num, r.y, r.width, r.height), text, val);
		GUI.Label(new Rect(r.x + num, r.y, r.width, r.height), text, val);
		GUI.Label(new Rect(r.x, r.y - num, r.width, r.height), text, val);
		Label(r, text, size, Color.white, (TextAnchor)4);
	}

	public static Texture2D DiscTex()
	{
		if ((Object)(object)_disc != (Object)null)
		{
			return _disc;
		}
		_disc = MakeAlphaTex((int x, int y, int n) =>
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			float num = (float)n * 0.5f - 1f;
			float num2 = Vector2.Distance(new Vector2((float)x, (float)y), new Vector2((float)n * 0.5f - 0.5f, (float)n * 0.5f - 0.5f));
			return Mathf.Clamp01(num - num2 + 0.5f);
		});
		return _disc;
	}

	public static Texture2D RoundTex()
	{
		if ((Object)(object)_round != (Object)null)
		{
			return _round;
		}
		_round = MakeAlphaTex((int x, int y, int n) =>
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			float num = (float)n * 0.22f;
			float num2 = Mathf.Clamp((float)x, num, (float)n - num);
			float num3 = Mathf.Clamp((float)y, num, (float)n - num);
			float num4 = Vector2.Distance(new Vector2((float)x, (float)y), new Vector2(num2, num3));
			return Mathf.Clamp01(num - num4 + 0.5f);
		});
		return _round;
	}

	public static Texture2D SoftTex()
	{
		if ((Object)(object)_soft != (Object)null)
		{
			return _soft;
		}
		_soft = MakeAlphaTex((int x, int y, int n) =>
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			float num = (float)n * 0.08f;
			float num2 = Mathf.Clamp((float)x, num, (float)n - num);
			float num3 = Mathf.Clamp((float)y, num, (float)n - num);
			float num4 = Vector2.Distance(new Vector2((float)x, (float)y), new Vector2(num2, num3));
			return Mathf.Clamp01(num - num4 + 0.5f);
		});
		return _soft;
	}

	public static Texture2D BallTex()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected Obj, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_ball != (Object)null)
		{
			return _ball;
		}
		Texture2D val = new Texture2D(192, 192, (TextureFormat)5, false);
		Color[] array = new Color[36864];
		float num = 95.5f;
		float num2 = 95f;
		Vector3 val2 = new Vector3(-0.42f, -0.54f, 0.73f);
		Vector3 normalized = val2.normalized;
		for (int i = 0; i < 192; i++)
		{
			for (int j = 0; j < 192; j++)
			{
				float num3 = ((float)j - num) / num2;
				float num4 = ((float)i - num) / num2;
				float num5 = Mathf.Sqrt(num3 * num3 + num4 * num4);
				float num6 = Mathf.Clamp01(num2 * (1f - num5) + 0.5f);
				if (num6 <= 0.002f)
				{
					array[i * 192 + j] = new Color(1f, 1f, 1f, 0f);
					continue;
				}
				float num7 = Mathf.Sqrt(Mathf.Max(0f, 1f - num5 * num5));
				float num8 = Mathf.Clamp01(num3 * normalized.x + num4 * normalized.y + num7 * normalized.z);
				float num9 = 0.56f + 0.44f * num8;
				num9 += Mathf.Pow(num8, 26f) * 0.09f;
				num9 *= 0.88f + 0.12f * num7;
				num9 = Mathf.Clamp01(num9);
				array[i * 192 + j] = new Color(num9, num9, num9, num6);
			}
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		_ball = val;
		return _ball;
	}

	private static Texture2D MakeAlphaTex(AlphaFn fn)
	{
		return MakeAlphaTexN(256, fn);
	}

	private static Texture2D MakeAlphaTexN(int n, AlphaFn fn)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected Obj, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(n, n, (TextureFormat)5, false);
		Color[] array = new Color[n * n];
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				array[i * n + j] = new Color(1f, 1f, 1f, Mathf.Clamp01(fn(j, i, n)));
			}
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		return val;
	}

	public static Texture2D LineTex()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected Obj, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_line != (Object)null)
		{
			return _line;
		}
		Texture2D val = new Texture2D(1, 16, (TextureFormat)5, false);
		Color[] array = new Color[16];
		for (int i = 0; i < 16; i++)
		{
			float num = Mathf.Min((float)i + 0.5f, 15.5f - (float)i);
			array[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(num / 1.5f));
		}
		val.SetPixels(array);
		((Texture)val).filterMode = (FilterMode)1;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		val.Apply();
		((Object)val).hideFlags = (HideFlags)61;
		return _line = val;
	}

	private static void Disc(Vector2 c, float r, Color tint, float alpha, bool outline = false)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (outline)
		{
			Ring(c, r, new Color(1f, 1f, 1f, 0.5f * alpha + 0.15f));
			return;
		}
		Color color = GUI.color;
		GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
		GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), (Texture)(object)DiscTex());
		GUI.color = color;
	}

	private static void Ring(Vector2 c, float r, Color col, float w = 0f)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (w <= 0f)
		{
			w = Mathf.Max(1.5f, r * 0.05f);
		}
		DrawAnnulus(c, Mathf.Max(0f, r - w * 0.5f), r + w * 0.5f, col, fade: false);
	}

	public static void DrawAnnulus(Vector2 c, float ri, float ro, Color col, bool fade)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (!(col.a <= 0.003f) && !(ro <= 0.004f))
		{
			int num = Mathf.Max(1, Mathf.RoundToInt(ro * 64f));
			int riQ = Mathf.Clamp(Mathf.RoundToInt(ri * 64f), 0, num);
			float num2 = (float)num / 64f;
			Texture2D val = AnnulusTex(riQ, num, fade);
			Color color = GUI.color;
			GUI.color = col;
			GUI.DrawTexture(new Rect(c.x - num2, c.y - num2, num2 * 2f, num2 * 2f), (Texture)(object)val);
			GUI.color = color;
		}
	}

	private static Texture2D AnnulusTex(int riQ, int roQ, bool fade)
	{
		long num = ((long)riQ << 32) | ((long)roQ << 1) | (fade ? 1 : 0);
		if (_annulus.TryGetValue(num, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		float ro = (float)roQ / 64f;
		float ri = Mathf.Min((float)riQ / 64f, ro * 0.995f);
		bool inside = ri > 0.002f;
		float band = Mathf.Max(0.0001f, ro - ri);
		Texture2D val = MakeAlphaTexN(160, (int x, int y, int n) =>
		{
			float num2 = (((float)x + 0.5f) / (float)n - 0.5f) * 2f * ro;
			float num3 = (((float)y + 0.5f) / (float)n - 0.5f) * 2f * ro;
			float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
			float num5 = ro * 1.15f / ((float)n * 0.5f);
			float num6 = Mathf.Clamp01((ro - num4) / num5);
			if (inside)
			{
				num6 = Mathf.Min(num6, Mathf.Clamp01((num4 - ri) / num5));
			}
			if (fade)
			{
				float num7 = Mathf.Clamp01((ro - num4) / band);
				num6 *= num7 * num7;
			}
			return num6;
		});
		if (_annulusKeys.Count >= 24)
		{
			long key = _annulusKeys[0];
			_annulusKeys.RemoveAt(0);
			if (_annulus.TryGetValue(key, out var value2) && (Object)(object)value2 != (Object)null)
			{
				Object.Destroy((Object)(object)value2);
			}
			_annulus.Remove(key);
		}
		_annulus[num] = val;
		_annulusKeys.Add(num);
		return val;
	}

	private static void RoundedRect(Rect r, Color tint, float alpha)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
		GUI.DrawTexture(r, (Texture)(object)RoundTex());
		GUI.color = color;
	}

	public static void DrawLine(Vector2 a, Vector2 b, Color col, float width)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = b - a;
		float magnitude = val.magnitude;
		if (!(magnitude < 0.01f) && !(col.a <= 0.002f))
		{
			float num = Mathf.Atan2(val.y, val.x) * 57.29578f;
			Matrix4x4 matrix = GUI.matrix;
			GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(a.x, a.y, 0f), Quaternion.Euler(0f, 0f, num), new Vector3(magnitude, width, 1f));
			Color color = GUI.color;
			GUI.color = col;
			GUI.DrawTexture(new Rect(0f, -0.5f, 1f, 1f), (Texture)(object)LineTex());
			GUI.matrix = matrix;
			GUI.color = color;
		}
	}

	private static string Q(string s)
	{
		StringBuilder stringBuilder = new StringBuilder(s.Length + 8);
		stringBuilder.Append('"');
		foreach (char c in s)
		{
			switch (c)
			{
			case '"':
				stringBuilder.Append("\\\"");
				continue;
			case '\\':
				stringBuilder.Append("\\\\");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\r':
				stringBuilder.Append("\\r");
				continue;
			case '\t':
				stringBuilder.Append("\\t");
				continue;
			}
			if (c < ' ')
			{
				StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
				int num = c;
				stringBuilder2.Append(num.ToString("x4"));
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}

	private static string N(float v)
	{
		return v.ToString("R", CultureInfo.InvariantCulture);
	}

	public static string Hex(Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		uint num = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f);
		uint num2 = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
		uint num3 = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
		uint num4 = (uint)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
		return "#" + ((num << 24) | (num2 << 16) | (num3 << 8) | num4).ToString("X8");
	}

	public static Color ParseHex(string s, Color fallback)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(s))
		{
			return fallback;
		}
		string text = s.Trim().TrimStart('#');
		try
		{
			if (text.Length == 6)
			{
				return new Color((float)Hx(text, 0) / 255f, (float)Hx(text, 2) / 255f, (float)Hx(text, 4) / 255f, 1f);
			}
			if (text.Length == 8)
			{
				return new Color((float)Hx(text, 2) / 255f, (float)Hx(text, 4) / 255f, (float)Hx(text, 6) / 255f, (float)Hx(text, 0) / 255f);
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static int Hx(string s, int at)
	{
		return int.Parse(s.Substring(at, 2), NumberStyles.HexNumber);
	}

	static VirtualControls()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
	}
}
