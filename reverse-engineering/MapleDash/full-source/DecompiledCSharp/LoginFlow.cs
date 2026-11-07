using System;
using System.Collections.Generic;
using System.Text;
using MapleDash.Wz;
using UnityEngine;

public class LoginFlow : MonoBehaviour
{
	private sealed class UiAnimation
	{
		public Texture2D[] Frames;

		public float[] Durations;

		public float[] Alpha0;

		public float[] Alpha1;

		public Vector2[] Origins;

		public float Duration;
	}

	private sealed class EulaRun
	{
		public string text = "";

		public bool bold;

		public float width;
	}

	private sealed class EulaLine
	{
		public readonly List<EulaRun> runs = new List<EulaRun>();

		public float width;
	}

	private static LoginFlow inst;

	private const float DW = 1280f;

	private const float DH = 720f;

	private const float LoginCameraY = 1792f;

	private const float WorldCameraY = 1192f;

	private const float TreeCameraY = 592f;

	private const float RaceCameraY = 0f;

	private const string UI = "UI/Login.img/";

	private const string BACK = "Map/Back/login.img/";

	private const string OBJ = "Map/Obj/login.img/";

	private const string VERSION = "Ver. 0.83";

	private LoginStage stage;

	private string account = "";

	private string password = "";

	private string charName = "";

	private string status = "";

	private bool saveId;

	private List<WorldEntry> worlds = new List<WorldEntry>();

	private List<ChannelEntry> channels = new List<ChannelEntry>();

	private CharListResponse charList;

	private int worldIndex = -1;

	private int channelIndex;

	private int selectedSlot;

	private int page;

	private int slotToDelete = -1;

	private bool scrollOpen;

	private bool noticeOpen;

	private bool genderOpen;

	private bool newAccountOpen;

	private LoginResponse pendingNewAccount;

	private bool alertOpen;

	private string alertText = "";

	private string alertImage;

	private string alertExtra;

	private float noticeScroll;

	private List<EulaLine> eulaLines;

	private float eulaLayoutScale = -1f;

	private float eulaBarGrab = -1f;

	private readonly Dictionary<char, float> eulaCharWidths = new Dictionary<char, float>();

	private int newGender;

	private bool focusAccount;

	private string focusedControl = "";

	private TouchScreenKeyboard loginKeyboard;

	private float keyboardOpenedAt;

	private bool keyboardPassword;

	private bool keyboardCharName;

	private GUIStyle loginField;

	private GUIStyle charNameField;

	private int racePick;

	private int createStep;

	private int jobGroup = 1;

	private int gender;

	private int face;

	private int hair;

	private int hairColor;

	private int skinColor;

	private int top;

	private int bottom;

	private int shoes;

	private int weapon;

	private int editRow;

	private static readonly int[] FacesM = new int[3] { 20000, 20001, 20002 };

	private static readonly int[] FacesF = new int[3] { 21000, 21001, 21002 };

	private static readonly int[] AranFacesM = new int[3] { 20100, 20401, 20402 };

	private static readonly int[] AranFacesF = new int[3] { 21700, 21201, 21002 };

	private static readonly int[] HairsM = new int[3] { 30290, 30300, 30310 };

	private static readonly int[] HairsF = new int[3] { 31000, 31040, 31050 };

	private static readonly int[] HairColors = new int[4] { 0, 2, 3, 7 };

	private static readonly int[] Skins = new int[4] { 0, 1, 2, 3 };

	private static readonly int[] TopsM = new int[3] { 1040002, 1040006, 1040010 };

	private static readonly int[] TopsF = new int[4] { 1041002, 1041006, 1041010, 1041011 };

	private static readonly int[] BottomsM = new int[2] { 1060002, 1060006 };

	private static readonly int[] BottomsF = new int[2] { 1061002, 1061008 };

	private static readonly int[] Shoes = new int[4] { 1072001, 1072005, 1072037, 1072038 };

	private static readonly int[] Weapons = new int[3] { 1302000, 1322005, 1312004 };

	private static readonly int[] AranTop = new int[1] { 1042167 };

	private static readonly int[] AranBottom = new int[1] { 1062115 };

	private static readonly int[] AranShoes = new int[1] { 1072383 };

	private static readonly int[] AranWeapon = new int[1] { 1442079 };

	private float s = 1f;

	private float ox;

	private float oy;

	private float styleScale = -1f;

	private bool modal;

	private GUIStyle _label;

	private GUIStyle _small;

	private GUIStyle _tiny;

	private GUIStyle _brown;

	private GUIStyle _brownC;

	private GUIStyle _error;

	private GUIStyle _field;

	private GUIStyle _linkShadow;

	private GUIStyle _version;

	private GUIStyle _hint;

	private GUIStyle _row;

	private GUIStyle _eula;

	private GUIStyle _alert;

	private GUIStyle _alertSmall;

	private GUIStyle _genderValue;

	private readonly Dictionary<string, UiAnimation> uiAnimations = new Dictionary<string, UiAnimation>();

	private readonly Dictionary<string, float> animationStarts = new Dictionary<string, float>();

	private float pageStartedAt;

	private float worldScrollStartedAt;

	private float genderStartedAt;

	private float selectionStartedAt;

	private float channelSelectedAt;

	private bool genderWasOpen;

	private int animatedSlot = -1;

	private int animatedChannel = -1;

	private LoginStage animationStage;

	private readonly Dictionary<string, Sprite> previewSprites = new Dictionary<string, Sprite>();

	private readonly Dictionary<int, string> equipNames = new Dictionary<int, string>();

	private float lastSlotClickAt = -10f;

	private int lastSlotClick = -1;

	private static int releaseLogFrame = -1;

	private static Texture2D clearTex;

	private const float CharEffectAnchorY = 337f;

	private static readonly Vector2[] LoginEffectAt = new Vector2[6]
	{
		new Vector2(766f, 0f),
		new Vector2(906f, 36f),
		new Vector2(906f, 15f),
		new Vector2(990f, 0f),
		new Vector2(905f, 0f),
		new Vector2(912f, 2f)
	};

	private const string NewAccountImage = "6";

	private const string AlertEmptyImage = "4";

	private const float ChannelSelectDx = 20f;

	private const float ChannelSelectDy = 7f;

	private const float ChannelGaugeDx = 7f;

	private const float ChannelGaugeDy = 17f;

	private const float EulaX = 425f;

	private const float EulaY = 147f;

	private const float EulaViewX = 36f;

	private const float EulaViewY = 41f;

	private const float EulaViewW = 353f;

	private const float EulaViewH = 308f;

	private const float EulaBarX = 390f;

	private const float EulaBarY = 40f;

	private const float EulaBarH = 317f;

	private const float EulaLineH = 17f;

	public static LoginFlow I => inst;

	private int Slots
	{
		get
		{
			if (charList == null)
			{
				return 3;
			}
			return Mathf.Max(1, charList.characterSlots);
		}
	}

	private int PageCount => Mathf.Max(1, (Slots + 2) / 3);

	private float EulaMaxScroll => Mathf.Max(0f, (float)((eulaLines != null) ? eulaLines.Count : 0) * 17f - 308f);

	private int[] FaceList
	{
		get
		{
			if (jobGroup != 2)
			{
				if (gender != 0)
				{
					return FacesF;
				}
				return FacesM;
			}
			if (gender != 0)
			{
				return AranFacesF;
			}
			return AranFacesM;
		}
	}

	private int[] HairList
	{
		get
		{
			if (gender != 0)
			{
				return HairsF;
			}
			return HairsM;
		}
	}

	private int[] TopList
	{
		get
		{
			if (jobGroup != 2)
			{
				if (gender != 0)
				{
					return TopsF;
				}
				return TopsM;
			}
			return AranTop;
		}
	}

	private int[] BottomList
	{
		get
		{
			if (jobGroup != 2)
			{
				if (gender != 0)
				{
					return BottomsF;
				}
				return BottomsM;
			}
			return AranBottom;
		}
	}

	private int[] ShoesList
	{
		get
		{
			if (jobGroup != 2)
			{
				return Shoes;
			}
			return AranShoes;
		}
	}

	private int[] WeaponList
	{
		get
		{
			if (jobGroup != 2)
			{
				return Weapons;
			}
			return AranWeapon;
		}
	}

	private void Awake()
	{
		inst = this;
	}

	private void Start()
	{
		GameSession.Boot();
		saveId = PlayerPrefs.GetInt("login_save_id", 0) == 1;
		if (saveId)
		{
			account = PlayerPrefs.GetString("login_account", "");
		}
		if (PlayerPrefs.GetInt("autostart", 0) == 1)
		{
			bool flag = TryAutoLogin();
			Debug.Log((object)("[LoginFlow] autostart=1 autoLogin=" + flag));
			if (flag)
			{
				GameLoop.I.EnterGameFromLogin();
				return;
			}
		}
		newGender = Mathf.Clamp(PlayerPrefs.GetInt("login_new_gender", 0), 0, 1);
		noticeOpen = false;
		stage = LoginStage.Login;
	}

	private void OnDestroy()
	{
		if ((Object)(object)inst == (Object)(object)this)
		{
			inst = null;
		}
	}

	private void OnDisable()
	{
		if (loginKeyboard != null)
		{
			loginKeyboard.active = false;
		}
		loginKeyboard = null;
	}

	private bool TryAutoLogin()
	{
		LoginResponse loginResponse = GameSession.Login("local", "local");
		if (loginResponse.code != LoginResultCode.Ok)
		{
			Debug.LogWarning((object)("[LoginFlow] auto login code=" + loginResponse.code));
			return false;
		}
		if (loginResponse.gender == 10)
		{
			GameSession.SetGender(loginResponse.accountId, 0);
		}
		GameSession.BeginSession(loginResponse.accountId, loginResponse.accountName, 0, 1);
		ServerListResponse serverListResponse = GameSession.ServerList();
		int world = ((serverListResponse.worlds.Count > 0) ? serverListResponse.worlds[0].id : 0);
		GameSession.BeginSession(loginResponse.accountId, loginResponse.accountName, world, 1);
		CharListResponse charListResponse = GameSession.CharList(world, 1);
		if (charListResponse.characters.Count == 0)
		{
			CreateCharResponse createCharResponse = GameSession.CreateChar(new CreateCharRequest
			{
				name = "AutoHero",
				job = 1,
				face = 20000,
				hair = 30300,
				hairColor = 0,
				skinColor = 0,
				top = 1040002,
				bottom = 1060002,
				shoes = 1072001,
				weapon = 1302000,
				gender = 0
			});
			if (createCharResponse.code != 0)
			{
				Debug.LogWarning((object)("[LoginFlow] auto create code=" + createCharResponse.code + " " + createCharResponse.error));
				return false;
			}
			charListResponse = GameSession.CharList(world, 1);
		}
		if (charListResponse.characters.Count == 0)
		{
			Debug.LogWarning((object)"[LoginFlow] auto login: no character");
			return false;
		}
		bool flag = GameSession.SelectChar((int)charListResponse.characters[0].id);
		if (!flag)
		{
			Debug.LogWarning((object)"[LoginFlow] auto login: select char failed");
		}
		return flag;
	}

	private void Update()
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		if ((Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Login)
		{
			return;
		}
		if (loginKeyboard != null)
		{
			if (keyboardCharName)
			{
				charName = loginKeyboard.text;
			}
			else if (keyboardPassword)
			{
				password = loginKeyboard.text;
			}
			else
			{
				account = loginKeyboard.text;
			}
			bool flag = ((!keyboardCharName) ? (stage != LoginStage.Login) : (stage != LoginStage.CharCreate || createStep != 0));
			Status val = loginKeyboard.status;
			bool flag2 = (int)val == 1 || (int)val == 2;
			if (flag | flag2)
			{
				Debug.Log((object)("[Login] 收起键盘 leftField=" + flag + " status=" + ((object)val/*cast due to constrained. prefix*/).ToString() + " held=" + (Time.unscaledTime - keyboardOpenedAt).ToString("0.0") + "s"));
				loginKeyboard.active = false;
				loginKeyboard = null;
			}
		}
		if (GameSideMenu.Blocking || VirtualControlsEditor.Visible)
		{
			return;
		}
		if (noticeOpen)
		{
			HandleEulaScrollInput();
		}
		else if (newAccountOpen)
		{
			if (Input.GetKeyDown((KeyCode)27))
			{
				CancelNewAccount();
			}
		}
		else if (genderOpen)
		{
			if (Input.GetKeyDown((KeyCode)27))
			{
				genderOpen = false;
			}
		}
		else if (alertOpen)
		{
			if (Input.GetKeyDown((KeyCode)27))
			{
				alertOpen = false;
			}
		}
		else
		{
			if (!Input.GetKeyDown((KeyCode)27))
			{
				return;
			}
			if (slotToDelete >= 0)
			{
				slotToDelete = -1;
				return;
			}
			switch (stage)
			{
			case LoginStage.WorldSelect:
				GameSession.Logout();
				stage = LoginStage.Login;
				account = "";
				password = "";
				status = "";
				break;
			case LoginStage.CharSelect:
				stage = LoginStage.WorldSelect;
				break;
			case LoginStage.RaceSelect:
				stage = LoginStage.CharSelect;
				break;
			case LoginStage.CharCreate:
				stage = LoginStage.RaceSelect;
				break;
			case LoginStage.Login:
				Application.Quit();
				break;
			}
		}
	}

	private void OnGUI()
	{
		if (!((Object)(object)GameLoop.I == (Object)null) && GameLoop.I.state == GameLoop.GameState.Login && !VirtualControlsEditor.Visible)
		{
			GUI.depth = -100;
			UiKit.Begin();
			Metrics();
			EnsureStyles();
			modal = noticeOpen || genderOpen || alertOpen || newAccountOpen || slotToDelete >= 0;
			LogReleaseOnce();
			if (animationStage != stage)
			{
				animationStage = stage;
				pageStartedAt = Time.unscaledTime;
				animationStarts.Clear();
				animatedSlot = -1;
				animatedChannel = -1;
			}
			if (genderOpen && !genderWasOpen)
			{
				genderStartedAt = Time.unscaledTime;
			}
			genderWasOpen = genderOpen;
			if (animatedSlot != selectedSlot)
			{
				animatedSlot = selectedSlot;
				selectionStartedAt = Time.unscaledTime;
			}
			if (animatedChannel != channelIndex)
			{
				animatedChannel = channelIndex;
				channelSelectedAt = Time.unscaledTime;
			}
			switch (stage)
			{
			case LoginStage.Login:
				DrawLogin();
				break;
			case LoginStage.WorldSelect:
				DrawWorldSelect();
				break;
			case LoginStage.CharSelect:
				DrawCharSelect();
				break;
			case LoginStage.RaceSelect:
				DrawRaceSelect();
				break;
			case LoginStage.CharCreate:
				DrawCharCreate();
				break;
			}
			if (stage == LoginStage.Login && !modal && !GameSideMenu.Blocking)
			{
				HandleLoginKeys();
			}
			DrawVersion();
			if (slotToDelete >= 0)
			{
				DrawDeleteDialog();
			}
			if (newAccountOpen)
			{
				DrawNewAccountDialog();
			}
			if (genderOpen)
			{
				DrawGenderDialog();
			}
			if (alertOpen)
			{
				DrawAlertDialog();
			}
			if (noticeOpen)
			{
				DrawNoticeDialog();
			}
			DrawLetterbox();
		}
	}

	private void LogReleaseOnce()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 7 && releaseLogFrame != Time.frameCount)
		{
			releaseLogFrame = Time.frameCount;
			if (Pointer.Released || Pointer.Pressed)
			{
				Debug.Log((object)("[Login] " + (Pointer.Released ? "release" : "press") + " pos=" + Pointer.Pos.ToString("0") + " press=" + Pointer.PressPosition.ToString("0") + " modal=" + modal + " virtual=" + Pointer.UsesVirtualCursor + " dragged=" + Pointer.Dragging));
			}
		}
	}

	private void DrawLetterbox()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (ox > 0f)
		{
			UiKit.Fill(new Rect(0f, 0f, ox, UiKit.H), Color.black);
		}
		if (ox + 1280f * s < UiKit.W)
		{
			UiKit.Fill(new Rect(ox + 1280f * s, 0f, UiKit.W - ox - 1280f * s, UiKit.H), Color.black);
		}
		if (oy > 0f)
		{
			UiKit.Fill(new Rect(0f, 0f, UiKit.W, oy), Color.black);
		}
		if (oy + 720f * s < UiKit.H)
		{
			UiKit.Fill(new Rect(0f, oy + 720f * s, UiKit.W, UiKit.H - oy - 720f * s), Color.black);
		}
	}

	private void Metrics()
	{
		s = Mathf.Min(UiKit.W / 1280f, UiKit.H / 720f);
		if (s <= 0f)
		{
			s = 1f;
		}
		ox = (UiKit.W - 1280f * s) / 2f;
		oy = (UiKit.H - 720f * s) / 2f;
	}

	private Rect R(float x, float y, float w, float h)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(ox + x * s, oy + y * s, w * s, h * s);
	}

	private void DrawPageAt(string path, float offsetX, float y)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(path);
		if ((Object)(object)val != (Object)null)
		{
			DrawTex(val, R(offsetX, y, ((Texture)val).width, ((Texture)val).height));
		}
	}

	private void DrawSideMenu(bool showWorldMap)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (showWorldMap && Button("UI/Login.img/WorldSelect/BtViewAll", R(0f, 125f, 99f, 35f)))
		{
			status = "世界地图尚未开放";
		}
		if (Button("UI/Login.img/ViewAllChar/BtVAC", R(0f, 374f, 97f, 34f)))
		{
			status = "查看所有角色尚未开放";
		}
		if (Button("UI/Login.img/Common/BtStart", R(0f, 425f, 98f, 35f)))
		{
			if (stage == LoginStage.WorldSelect)
			{
				GameSession.Logout();
				stage = LoginStage.Login;
				account = "";
				password = "";
				status = "";
			}
			else
			{
				scrollOpen = false;
				stage = LoginStage.WorldSelect;
				status = "";
			}
		}
	}

	private void EnsureStyles()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Expected Obj, but got Unknown
		//IL_02f5: Expected Obj, but got Unknown
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Expected Obj, but got Unknown
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Expected Obj, but got Unknown
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Expected Obj, but got Unknown
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Expected Obj, but got Unknown
		if ((Object)(object)GUI.skin != (Object)null)
		{
			GUI.skin.settings.cursorColor = Color.white;
			GUI.skin.settings.selectionColor = new Color(0.55f, 0.75f, 1f, 0.45f);
			GUI.skin.settings.cursorFlashSpeed = 1f;
		}
		if (!Mathf.Approximately(styleScale, s) || _label == null)
		{
			styleScale = s;
			_label = Style(14, (TextAnchor)3, new Color(0.95f, 0.96f, 1f));
			_small = Style(12, (TextAnchor)3, new Color(0.86f, 0.89f, 0.95f));
			_tiny = Style(11, (TextAnchor)4, new Color(0.96f, 0.97f, 1f));
			_brown = Style(11, (TextAnchor)3, new Color(0.28f, 0.16f, 0.07f));
			_brownC = Style(11, (TextAnchor)4, new Color(0.28f, 0.16f, 0.07f));
			_row = Style(12, (TextAnchor)4, new Color(0.12f, 0.08f, 0.03f));
			_row.clipping = (TextClipping)1;
			_error = Style(12, (TextAnchor)4, new Color(1f, 0.55f, 0.5f));
			_linkShadow = Style(12, (TextAnchor)3, new Color(0f, 0f, 0f, 0.85f));
			_version = Style(9, (TextAnchor)3, new Color(0.16f, 0.17f, 0.19f));
			_hint = Style(12, (TextAnchor)3, new Color(0.29f, 0.31f, 0.35f));
			_eula = Style(12, (TextAnchor)3, new Color(0.31f, 0.18f, 0.11f));
			_eula.clipping = (TextClipping)0;
			_alert = Style(13, (TextAnchor)4, new Color(0.3f, 0.15f, 0.06f));
			_alert.wordWrap = true;
			_alertSmall = Style(12, (TextAnchor)3, new Color(0.3f, 0.15f, 0.06f));
			_genderValue = Style(12, (TextAnchor)3, new Color(0.1f, 0.09f, 0.08f));
			_field = new GUIStyle(GUI.skin.textField)
			{
				font = UiKit.Font,
				fontSize = Mathf.RoundToInt(14f * s),
				alignment = (TextAnchor)3,
				padding = new RectOffset(Mathf.RoundToInt(5f * s), 2, 0, 0)
			};
			Texture2D background = ClearTex();
			_field.normal.background = background;
			_field.focused.background = background;
			_field.hover.background = background;
			_field.active.background = background;
			_field.onNormal.background = background;
			_field.onFocused.background = background;
			_field.onHover.background = background;
			_field.onActive.background = background;
			_field.border = new RectOffset();
			_field.normal.textColor = Color.white;
			_field.focused.textColor = Color.white;
			_field.hover.textColor = Color.white;
			_field.active.textColor = Color.white;
			loginField = new GUIStyle(_field)
			{
				padding = new RectOffset(Mathf.RoundToInt(5f * s), 0, 0, 0),
				contentOffset = new Vector2(0f, -2f * s)
			};
			charNameField = new GUIStyle(loginField)
			{
				alignment = (TextAnchor)3,
				contentOffset = Vector2.zero
			};
		}
	}

	private GUIStyle Style(int size, TextAnchor anchor, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(8, Mathf.RoundToInt((float)size * s)),
			alignment = anchor,
			clipping = (TextClipping)0
		};
		val.normal.textColor = color;
		return val;
	}

	private static Texture2D ClearTex()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected Obj, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)clearTex != (Object)null)
		{
			return clearTex;
		}
		clearTex = new Texture2D(1, 1, (TextureFormat)4, false);
		clearTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
		clearTex.Apply();
		return clearTex;
	}

	private static Texture2D Tex(string path)
	{
		return Art.Tex("wz://" + path);
	}

	private static bool Has(string path)
	{
		return WzAssets.Node("wz://" + path) != null;
	}

	private static void DrawTex(Texture2D tex, Rect rect, float alpha = 1f)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tex == (Object)null))
		{
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, alpha);
			GUI.DrawTexture(rect, (Texture)(object)tex, (ScaleMode)0);
			GUI.color = color;
		}
	}

	private static void DrawTexUv(Texture2D tex, Rect rect, Rect uv, float alpha = 1f)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tex == (Object)null))
		{
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, alpha);
			GUI.DrawTextureWithTexCoords(rect, (Texture)(object)tex, uv);
			GUI.color = color;
		}
	}

	private static Rect UvRect(Texture2D tex, float x, float y, float w, float h)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(x / (float)((Texture)tex).width, 1f - (y + h) / (float)((Texture)tex).height, w / (float)((Texture)tex).width, h / (float)((Texture)tex).height);
	}

	private void DrawSliced(Texture2D tex, Rect dst, Rect src, float left, float right, float bottom)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tex == (Object)null))
		{
			float num = src.height - bottom;
			float num2 = dst.height - bottom;
			float num3 = dst.width - left - right;
			DrawTexUv(tex, new Rect(dst.x, dst.y, left, num2), UvRect(tex, src.x, src.y, left, num));
			DrawTexUv(tex, new Rect(dst.x + left, dst.y, num3, num2), UvRect(tex, src.x + left, src.y, src.width - left - right, num));
			DrawTexUv(tex, new Rect(dst.x + dst.width - right, dst.y, right, num2), UvRect(tex, src.x + src.width - right, src.y, right, num));
			DrawTexUv(tex, new Rect(dst.x, dst.y + num2, left, bottom), UvRect(tex, src.x, src.y + num, left, bottom));
			DrawTexUv(tex, new Rect(dst.x + left, dst.y + num2, num3, bottom), UvRect(tex, src.x + left, src.y + num, src.width - left - right, bottom));
			DrawTexUv(tex, new Rect(dst.x + dst.width - right, dst.y + num2, right, bottom), UvRect(tex, src.x + src.width - right, src.y + num, right, bottom));
		}
	}

	private void DrawTexTiledV(Texture2D tex, Rect rect, float alpha = 1f)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)tex == (Object)null))
		{
			float num = Mathf.Max(1f, (float)((Texture)tex).height * s);
			for (float num2 = 0f; num2 < rect.height; num2 += num)
			{
				float num3 = Mathf.Min(num, rect.height - num2);
				DrawTexUv(tex, new Rect(rect.x, rect.y + num2, rect.width, num3), UvRect(tex, 0f, 0f, ((Texture)tex).width, num3 / s), alpha);
			}
		}
	}

	private void DrawAt(string path, float x, float y, bool centered = false, float alpha = 1f)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(path);
		if (!((Object)(object)val == (Object)null))
		{
			Vector2 val2 = (centered ? Art.Origin("wz://" + path) : Vector2.zero);
			DrawTex(val, R(x - val2.x, y - val2.y, ((Texture)val).width, ((Texture)val).height), alpha);
		}
	}

	private void DrawFull(string path)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(path);
		if ((Object)(object)val != (Object)null)
		{
			DrawTex(val, R(0f, 0f, 1280f, 720f));
		}
	}

	private void DrawClouds(float y, float speed, float alpha)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex("Map/Back/login.img/back/27");
		if (!((Object)(object)val == (Object)null))
		{
			float num = Mathf.Repeat(Time.unscaledTime * speed, (float)((Texture)val).width);
			for (int i = -1; i < 3; i++)
			{
				DrawTex(val, R((float)(i * ((Texture)val).width) - num, y, ((Texture)val).width, ((Texture)val).height), alpha);
			}
		}
	}

	private void DrawBalloon(string prefix, float x, float y, float w, float h)
	{
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex(prefix + "nw");
		Texture2D val2 = Tex(prefix + "n");
		Texture2D val3 = Tex(prefix + "ne");
		Texture2D val4 = Tex(prefix + "w");
		Texture2D val5 = Tex(prefix + "c");
		Texture2D val6 = Tex(prefix + "e");
		Texture2D val7 = Tex(prefix + "sw");
		Texture2D val8 = Tex(prefix + "s");
		Texture2D val9 = Tex(prefix + "se");
		if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null) && !((Object)(object)val3 == (Object)null) && !((Object)(object)val4 == (Object)null) && !((Object)(object)val5 == (Object)null) && !((Object)(object)val6 == (Object)null) && !((Object)(object)val7 == (Object)null) && !((Object)(object)val8 == (Object)null) && !((Object)(object)val9 == (Object)null))
		{
			float num = ((Texture)val4).width;
			float num2 = ((Texture)val6).width;
			float num3 = ((Texture)val).height;
			float num4 = ((Texture)val8).height;
			float num5 = Mathf.Max(0f, w - num - num2);
			float num6 = Mathf.Max(0f, h - num3 - num4);
			DrawTex(val, R(x, y, ((Texture)val).width, ((Texture)val).height));
			DrawTex(val3, R(x + w - (float)((Texture)val3).width, y, ((Texture)val3).width, ((Texture)val3).height));
			DrawTex(val7, R(x, y + h - (float)((Texture)val7).height, ((Texture)val7).width, ((Texture)val7).height));
			DrawTex(val9, R(x + w - (float)((Texture)val9).width, y + h - (float)((Texture)val9).height, ((Texture)val9).width, ((Texture)val9).height));
			if (num5 > 0f)
			{
				DrawTex(val2, R(x + num, y, num5, ((Texture)val2).height));
				DrawTex(val8, R(x + num, y + h - (float)((Texture)val8).height, num5, ((Texture)val8).height));
			}
			if (num6 > 0f)
			{
				DrawTex(val4, R(x, y + num3, ((Texture)val4).width, num6));
				DrawTex(val6, R(x + w - (float)((Texture)val6).width, y + num3, ((Texture)val6).width, num6));
				DrawTex(val5, R(x + num, y + num3, num5, num6));
			}
		}
	}

	private void DrawTabHint()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		Rect r = R(507f, 108f, 232f, 33f);
		UiKit.Fill(new Rect(r.x, r.y + 1.5f * s, r.width, r.height), new Color(0.42f, 0.48f, 0.55f, 0.28f));
		UiKit.Fill(r, new Color(0.86f, 0.89f, 0.94f, 0.95f));
		Label(R(518f, 108f, 212f, 33f), "可以用Tab对菜单进行操作。", _hint);
	}

	private UiAnimation Animation(string path)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (uiAnimations.TryGetValue(path, out var value))
		{
			return value;
		}
		List<Texture2D> list = new List<Texture2D>();
		List<float> list2 = new List<float>();
		List<Vector2> list3 = new List<Vector2>();
		List<float> list4 = new List<float>();
		List<float> list5 = new List<float>();
		float num = 0f;
		ImgNode imgNode = WzAssets.Node("wz://" + path);
		if (imgNode != null)
		{
			for (int i = 0; i < 2048; i++)
			{
				ImgNode imgNode2 = imgNode.Get(i.ToString());
				if (imgNode2 == null)
				{
					break;
				}
				Texture2D val = Tex(path + "/" + i);
				if ((Object)(object)val == (Object)null)
				{
					break;
				}
				list.Add(val);
				float num2 = (float)Mathf.Max(1, imgNode2.Int("delay", 100)) / 1000f;
				list2.Add(num2);
				num += num2;
				list3.Add(Art.Origin("wz://" + path + "/" + i));
				list4.Add((float)imgNode2.Int("a0", 255) / 255f);
				list5.Add((float)imgNode2.Int("a1", 255) / 255f);
			}
		}
		value = new UiAnimation
		{
			Frames = list.ToArray(),
			Durations = list2.ToArray(),
			Origins = list3.ToArray(),
			Alpha0 = list4.ToArray(),
			Alpha1 = list5.ToArray(),
			Duration = num
		};
		uiAnimations[path] = value;
		return value;
	}

	private int AnimationFrame(UiAnimation clip, float elapsed, bool loop, out float alpha)
	{
		alpha = 1f;
		if (clip.Frames.Length == 0)
		{
			return -1;
		}
		float num = (loop ? Mathf.Repeat(Mathf.Max(0f, elapsed), clip.Duration) : Mathf.Max(0f, elapsed));
		for (int i = 0; i < clip.Frames.Length; i++)
		{
			if (num < clip.Durations[i] || i == clip.Frames.Length - 1)
			{
				alpha = Mathf.Lerp(clip.Alpha0[i], clip.Alpha1[i], Mathf.Clamp01(num / clip.Durations[i]));
				return i;
			}
			num -= clip.Durations[i];
		}
		return -1;
	}

	private void DrawAnimationAt(string path, float x, float y, bool centered = false, bool loop = true, float startedAt = -1f)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		UiAnimation uiAnimation = Animation(path);
		int num = AnimationFrame(uiAnimation, Time.unscaledTime - ((startedAt < 0f) ? pageStartedAt : startedAt), loop, out var alpha);
		if (num >= 0)
		{
			Vector2 val = uiAnimation.Origins[num] - (centered ? Vector2.zero : uiAnimation.Origins[0]);
			Texture2D val2 = uiAnimation.Frames[num];
			DrawTex(val2, R(x - val.x, y - val.y, ((Texture)val2).width, ((Texture)val2).height), alpha);
		}
	}

	private void DrawState(string basePath, string state, Rect rect, bool animate = true)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		string text = basePath + "/" + state;
		UiAnimation uiAnimation = Animation(text);
		if (uiAnimation.Frames.Length == 0 && state != "normal")
		{
			text = basePath + "/normal";
			uiAnimation = Animation(text);
		}
		if (uiAnimation.Frames.Length == 0)
		{
			DrawTex(Tex(text), rect);
			return;
		}
		if (!animationStarts.TryGetValue(text, out var value))
		{
			value = (animationStarts[text] = Time.unscaledTime);
		}
		int num = AnimationFrame(uiAnimation, animate ? (Time.unscaledTime - value) : 0f, loop: false, out var alpha);
		Texture2D val = uiAnimation.Frames[num];
		Vector2 val2 = uiAnimation.Origins[num] - uiAnimation.Origins[0];
		float num2 = rect.width / (float)((Texture)uiAnimation.Frames[0]).width;
		float num3 = rect.height / (float)((Texture)uiAnimation.Frames[0]).height;
		DrawTex(val, new Rect(rect.x - val2.x * num2, rect.y - val2.y * num3, (float)((Texture)val).width * num2, (float)((Texture)val).height * num3), alpha);
		string[] array = new string[4] { "normal", "mouseOver", "pressed", "disabled" };
		foreach (string text2 in array)
		{
			if (text2 != state)
			{
				animationStarts.Remove(basePath + "/" + text2);
			}
		}
	}

	private bool Button(string basePath, Rect rect, bool enabled = true, bool popup = false, bool animate = true)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		bool flag = rect.Contains(Pointer.Pos);
		string state;
		if (enabled)
		{
			if (flag && Pointer.Down)
			{
				state = "pressed";
			}
			else
			{
				state = (flag ? "mouseOver" : "normal");
			}
		}
		else
		{
			state = "disabled";
		}
		DrawState(basePath, state, rect, animate);
		return Clicked(rect, enabled, popup);
	}

	private bool Clicked(Rect rect, bool enabled = true, bool popup = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (!enabled)
		{
			return false;
		}
		if (modal && !popup)
		{
			return false;
		}
		if ((int)Event.current.type != 7)
		{
			return false;
		}
		if (!Pointer.Released || !rect.Contains(Pointer.Pos) || !rect.Contains(Pointer.PressPosition))
		{
			return false;
		}
		AudioManager.Play("UI/BtMouseClick", 0.7f, pitchVary: false);
		return true;
	}

	private bool TextLink(Rect rect, string text)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		bool flag = rect.Contains(Pointer.Pos);
		GUI.Label(new Rect(rect.x + s, rect.y + s, rect.width, rect.height), text, _linkShadow);
		GUI.Label(rect, text, flag ? _label : _small);
		return Clicked(rect);
	}

	private void Label(Rect rect, string text, GUIStyle style)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(text))
		{
			GUI.Label(rect, text, style);
		}
	}

	private void DrawNameTag(float centerX, float topY, string name, bool selected = true)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected Obj, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		string text = "UI/Login.img/CharSelect/nameTag/" + (selected ? "1" : "0") + "/";
		Texture2D val = Tex(text + "0");
		Texture2D val2 = Tex(text + "1");
		Texture2D val3 = Tex(text + "2");
		if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null) && !((Object)(object)val3 == (Object)null))
		{
			float num = _tiny.CalcSize(new GUIContent(name)).x / s + 8f;
			float num2 = Mathf.Max((float)(((Texture)val).width + ((Texture)val3).width + ((Texture)val2).width), num);
			float num3 = centerX - num2 * 0.5f;
			DrawTex(val, R(num3, topY, ((Texture)val).width, ((Texture)val).height));
			float num4 = num2 - (float)((Texture)val).width - (float)((Texture)val3).width;
			int num5 = Mathf.Max(1, Mathf.CeilToInt(num4 / (float)((Texture)val2).width));
			for (int i = 0; i < num5; i++)
			{
				DrawTex(val2, R(num3 + (float)((Texture)val).width + (float)(i * ((Texture)val2).width), topY, Mathf.Min((float)((Texture)val2).width, num4 - (float)(i * ((Texture)val2).width)), ((Texture)val2).height));
			}
			DrawTex(val3, R(num3 + num2 - (float)((Texture)val3).width, topY, ((Texture)val3).width, ((Texture)val3).height));
			GUI.Label(R(num3, topY + 1f, num2, ((Texture)val2).height), name, _tiny);
		}
	}

	private void DrawVersion()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(R(1063f, 8f, 90f, 16f), "Ver. 0.83", _version);
	}

	private void DrawWorldBackground(float cameraY)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), Color.black);
		Color c;
		if (Mathf.Abs(cameraY - 0f) < 1f)
		{
			c = new Color(0.22f, 0.44f, 1f);
		}
		else
		{
			c = ((Mathf.Abs(cameraY - 1192f) < 1f) ? new Color(0.58f, 0.74f, 1f) : new Color(0.35f, 0.54f, 1f));
		}
		UiKit.Fill(R(0f, 0f, 1280f, 720f), c);
		if (Mathf.Abs(cameraY - 0f) < 1f)
		{
			DrawClouds(-180f, 5f, 0.95f);
			DrawClouds(515f, 5f, 0.95f);
		}
		else if (Mathf.Abs(cameraY - 1192f) < 1f)
		{
			DrawClouds(250f, 5f, 0.55f);
		}
		else
		{
			DrawClouds(-10f, 5f, 0.95f);
		}
		DrawAt("Map/Back/login.img/back/11", 0f, 0f - cameraY);
	}

	private void DrawCreateBackground()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), Color.black);
		UiKit.Fill(R(0f, 0f, 1280f, 720f), new Color(0.22f, 0.44f, 1f));
		DrawClouds(215f, 6f, 0.95f);
		DrawAt("Map/Back/login.img/back/16", -5f, -26f);
	}

	private void DrawLogin()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		DrawWorldBackground(1792f);
		DrawAnimationAt("Map/Obj/login.img/Title/logo/0", 431f, 81f, centered: false, loop: false);
		DrawLoginEffects();
		DrawFull("UI/Login.img/Common/frame");
		DrawAt("Map/Obj/login.img/Title/signboard/0/0", 563f, 270f);
		if (Button("UI/Login.img/Title/BtLogin", R(835f, 283f, 95f, 45f)))
		{
			DoLogin();
		}
		if (Button("UI/Login.img/Title/BtNew", R(636f, 410f, 92f, 38f)))
		{
			NewAccount();
		}
		if (Button("UI/Login.img/Title/BtHomePage", R(736f, 408f, 93f, 38f)))
		{
			status = "请访问 BeiDouMS 官网";
		}
		if (Button("UI/Login.img/Title/BtQuit", R(836f, 408f, 81f, 40f)))
		{
			Application.Quit();
		}
		if (Button("UI/Login.img/Title/BtLoginIDSave", R(659f, 362f, 51f, 23f)))
		{
			ToggleSaveId();
		}
		if (Button("UI/Login.img/Title/BtLoginIDLost", R(766f, 360f, 59f, 23f)))
		{
			status = "请到论坛找回账号";
		}
		if (Button("UI/Login.img/Title/BtPasswdLost", R(848f, 362f, 56f, 23f)))
		{
			status = "请到论坛找回密码";
		}
		DrawAt("UI/Login.img/Title/check/" + (saveId ? 1 : 0), 640f, 362f);
		DrawLoginField(R(690f, 296f, 135f, 27f), secure: false);
		DrawLoginField(R(690f, 322f, 135f, 27f), secure: true);
		if (focusAccount && !modal && !Application.isMobilePlatform)
		{
			GUI.FocusControl("login_account");
			focusAccount = false;
		}
		if (!modal)
		{
			focusedControl = GUI.GetNameOfFocusedControl();
		}
		if (!string.IsNullOrEmpty(status))
		{
			Label(R(563f, 520f, 368f, 24f), status, _error);
		}
	}

	private void DrawLoginEffects()
	{
		for (int i = 0; i < LoginEffectAt.Length; i++)
		{
			DrawAnimationAt("UI/Login.img/Title/effect/" + i, LoginEffectAt[i].x, LoginEffectAt[i].y);
		}
	}

	private void DrawLoginField(Rect rect, bool secure)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		string text = (secure ? "login_password" : "login_account");
		string text2 = (secure ? password : account);
		if (Application.isMobilePlatform)
		{
			GUI.Label(rect, secure ? new string('*', text2.Length) : text2, loginField);
		}
		else
		{
			GUI.SetNextControlName(text);
			bool enabled = GUI.enabled;
			GUI.enabled = !modal;
			if (secure)
			{
				password = GUI.PasswordField(rect, password, '*', 20, loginField);
			}
			else
			{
				account = GUI.TextField(rect, account, 13, loginField);
			}
			GUI.enabled = enabled;
		}
		if (Clicked(rect))
		{
			if (Application.isMobilePlatform)
			{
				Debug.Log((object)("[Login] 点击输入框 secure=" + secure));
				OpenKeyboard(text2, secure ? 20 : 13, secure, forCharName: false);
			}
			else if (Pointer.DoubleClicked)
			{
				GUI.FocusControl(text);
			}
		}
	}

	private void OpenKeyboard(string value, int maxLength, bool secure, bool forCharName)
	{
		int num = (forCharName ? 2 : (secure ? 1 : 0));
		int num2 = (keyboardCharName ? 2 : (keyboardPassword ? 1 : 0));
		if (loginKeyboard == null || !loginKeyboard.active || num2 != num)
		{
			if (loginKeyboard != null)
			{
				loginKeyboard.active = false;
			}
			keyboardCharName = forCharName;
			keyboardPassword = !forCharName & secure;
			loginKeyboard = TouchScreenKeyboard.Open(value, (TouchScreenKeyboardType)0, false, false, secure, false, "", maxLength);
			keyboardOpenedAt = Time.unscaledTime;
			Debug.Log((object)("[Login] 起键盘 char=" + forCharName + " secure=" + secure + " keyboard=" + ((loginKeyboard != null) ? "ok" : "null")));
		}
	}

	private void HandleLoginKeys()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 4 && ((int)Event.current.keyCode == 13 || (int)Event.current.keyCode == 271))
		{
			string nameOfFocusedControl = GUI.GetNameOfFocusedControl();
			if (!(nameOfFocusedControl != "login_account") || !(nameOfFocusedControl != "login_password"))
			{
				Event.current.Use();
				DoLogin();
			}
		}
	}

	private void ToggleSaveId()
	{
		saveId = !saveId;
		PlayerPrefs.SetInt("login_save_id", saveId ? 1 : 0);
		if (!saveId)
		{
			PlayerPrefs.DeleteKey("login_account");
		}
		PlayerPrefs.Save();
	}

	private void NewAccount()
	{
		status = "输入想要的账号和密码，点“连接”会自动注册";
		focusAccount = true;
	}

	private void ConfirmNewAccount()
	{
		newAccountOpen = false;
		newGender = Mathf.Clamp(PlayerPrefs.GetInt("login_new_gender", 0), 0, 1);
		genderOpen = true;
	}

	private void CancelNewAccount()
	{
		newAccountOpen = false;
		pendingNewAccount = null;
		status = "";
	}

	private void ConfirmNewGender()
	{
		PlayerPrefs.SetInt("login_new_gender", newGender);
		PlayerPrefs.Save();
		genderOpen = false;
		LoginResponse loginResponse = pendingNewAccount;
		pendingNewAccount = null;
		if (loginResponse != null)
		{
			if (!GameSession.SetGender(loginResponse.accountId, newGender))
			{
				Debug.LogWarning((object)"[Login] SetGender 失败；账号仍是未选性别");
			}
			EnterWorldSelect(loginResponse);
		}
		else
		{
			account = "";
			password = "";
			status = "输入新账号和密码，点“连接”将自动注册";
			focusAccount = true;
		}
	}

	private void DoLogin()
	{
		string text = account.Trim();
		bool flag = LoginService.IsNoPasswordAccount(text);
		if (string.IsNullOrEmpty(text) || (string.IsNullOrEmpty(password) && !flag))
		{
			SetAlert("账号不能为空。请输入正确的账号和密码。", "4");
			return;
		}
		if (text.Length > 13)
		{
			SetAlert("账号不能超过 13 个字符，请确认后再输入。", null);
			return;
		}
		LoginResponse loginResponse = GameSession.Login(text, password);
		Debug.Log((object)("[Login] account=" + text + " code=" + loginResponse.code.ToString() + " gender=" + loginResponse.gender));
		if (loginResponse.code != LoginResultCode.Ok)
		{
			ShowAlert(loginResponse);
			return;
		}
		if (saveId)
		{
			PlayerPrefs.SetString("login_account", text);
			PlayerPrefs.SetInt("login_save_id", 1);
			PlayerPrefs.Save();
		}
		if (loginResponse.gender == 10)
		{
			status = "";
			pendingNewAccount = loginResponse;
			noticeScroll = 0f;
			eulaBarGrab = -1f;
			noticeOpen = true;
		}
		else
		{
			EnterWorldSelect(loginResponse);
		}
	}

	private void EnterWorldSelect(LoginResponse res)
	{
		status = "";
		GameSession.BeginSession(res.accountId, res.accountName, 0, 1);
		worlds = GameSession.ServerList().worlds;
		if (worlds.Count == 0)
		{
			status = "没有可用大区";
			return;
		}
		scrollOpen = false;
		worldIndex = -1;
		animatedChannel = -1;
		stage = LoginStage.WorldSelect;
	}

	private void ShowAlert(LoginResponse res)
	{
		switch (res.code)
		{
		case LoginResultCode.WrongPassword:
			SetAlert("密码错误！请确认！", "3");
			break;
		case LoginResultCode.NotRegistered:
			SetAlert("账号不正确。请确认后再输入。", "2");
			break;
		case LoginResultCode.Banned:
			if (string.IsNullOrEmpty(res.banUntil))
			{
				SetAlert("您的账号已被封停。", "16");
			}
			else
			{
				SetAlert("您的账号已被封停。", "16", "解除时间：" + res.banUntil);
			}
			break;
		case LoginResultCode.Blocked:
			SetAlert("该 IP 已被封停，无法连接。\n请联系管理员。", null);
			break;
		case LoginResultCode.AlreadyLoggedIn:
			SetAlert("该账号已登陆！请输入其它账号。", "5");
			break;
		case LoginResultCode.RegisterLimit:
			SetAlert("该 IP 注册的账号数已达上限。\n请稍后再试。", null);
			break;
		case LoginResultCode.Error:
			SetAlert("系统错误，无法连接！请稍后再试。", "15");
			break;
		default:
			SetAlert("无法连接游戏服务器。请稍等后再试。", "22");
			break;
		}
	}

	private void SetAlert(string text, string image, string extra = null)
	{
		alertText = text;
		alertImage = image;
		alertExtra = extra;
		alertOpen = true;
		status = "";
	}

	private void DrawAlertDialog()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.35f));
		float num = 459f;
		float num2 = 251f;
		DrawAt("UI/Login.img/Notice/backgrnd/0", num, num2);
		if (!string.IsNullOrEmpty(alertImage) && Has("UI/Login.img/Notice/text/" + alertImage))
		{
			DrawAt("UI/Login.img/Notice/text/" + alertImage, num + 119f, num2 + 21f);
		}
		else
		{
			Label(R(num + 121f, num2 + 48f, 219f, 78f), alertText, _alert);
		}
		if (!string.IsNullOrEmpty(alertExtra))
		{
			Label(R(num + 121f, num2 + 96f, 219f, 22f), alertExtra, _alertSmall);
		}
		if (Button("UI/Login.img/Notice/BtYes", R(num + 143f, num2 + 168f, 75f, 32f), enabled: true, popup: true))
		{
			alertOpen = false;
		}
	}

	private void SelectWorld(int i)
	{
		if (i >= 0 && i < worlds.Count)
		{
			worldIndex = i;
			channelIndex = 0;
			animatedChannel = -1;
			WorldEntry worldEntry = worlds[i];
			GameSession.BeginSession(GameSession.AccountId, GameSession.AccountName, worldEntry.id, 1);
			channels = WorldService.Channels(GameSession.Server.Db, worldEntry.id);
			if (!scrollOpen)
			{
				worldScrollStartedAt = Time.unscaledTime;
			}
			scrollOpen = true;
		}
	}

	private void DrawWorldSelect()
	{
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		DrawWorldBackground(1192f);
		DrawFull("UI/Login.img/Common/frame");
		DrawPageAt("UI/Login.img/Common/step/1", 0f, 33f);
		if (scrollOpen)
		{
			DrawAnimationAt("UI/Login.img/WorldSelect/scroll/0", 396f, 139f, centered: false, loop: false, worldScrollStartedAt);
		}
		else
		{
			DrawAt("UI/Login.img/WorldSelect/scroll/0/0", 396f, 139f);
		}
		DrawAt("Map/Obj/login.img/WorldSelect/signboard/0/0", 338f, -2f);
		if (scrollOpen && worldIndex >= 0 && Time.unscaledTime - worldScrollStartedAt >= 0.35f)
		{
			DrawWorldContent();
		}
		for (int i = 0; i < 15; i++)
		{
			float x = 472 + 28 * i;
			float y = 164f;
			if (i < worlds.Count)
			{
				int id = worlds[i].id;
				string basePath = (Has("UI/Login.img/WorldSelect/BtWorld/" + id) ? ("UI/Login.img/WorldSelect/BtWorld/" + id) : "UI/Login.img/WorldSelect/BtWorld/t0");
				if (Button(basePath, R(x, y, 28f, 95f)))
				{
					SelectWorld(i);
				}
			}
			else
			{
				DrawEmptyWorldTab(x, y);
			}
		}
		DrawSideMenu(showWorldMap: true);
		if (!string.IsNullOrEmpty(status))
		{
			Label(R(400f, 60f, 400f, 22f), status, _error);
		}
	}

	private void DrawEmptyWorldTab(float x, float y)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex("UI/Login.img/WorldSelect/BtWorld/t20/normal/0");
		if (!((Object)(object)val == (Object)null))
		{
			DrawTexUv(val, R(x, y, 26f, 20f), UvRect(val, 0f, 0f, 26f, 20f));
			DrawTexUv(val, R(x, y + 20f, 26f, 75f), UvRect(val, 0f, 88f, 26f, 1f));
		}
	}

	private void DrawWorldContent()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		WorldEntry worldEntry = worlds[worldIndex];
		DrawAt("UI/Login.img/WorldSelect/BtWorld/chBackgrn", 435f, 297f);
		string path = "UI/Login.img/WorldSelect/world/" + worldEntry.id;
		if (Has(path))
		{
			DrawAt(path, 469f, 280f);
		}
		else
		{
			Label(R(469f, 280f, 240f, 40f), worldEntry.eventMessage, _brown);
		}
		int num = Mathf.Min(3, channels.Count);
		for (int i = 0; i < 20; i++)
		{
			float num2 = 470f + (float)(i % 4 * 94);
			float num3 = 350f + (float)(i / 4 * 32);
			Rect val = R(num2, num3, 92f, 30f);
			bool flag = i < num;
			bool flag2 = false;
			if (flag && Clicked(val))
			{
				channelIndex = i;
				flag2 = Pointer.DoubleClicked;
			}
			else if (!flag && Clicked(val))
			{
				status = "该频道尚未开放";
			}
			Texture2D val2 = Tex("UI/Login.img/WorldSelect/channel/" + i + "/" + (flag ? "normal" : "disabled"));
			if ((Object)(object)val2 != (Object)null)
			{
				DrawTex(val2, val);
			}
			else
			{
				GUI.Label(val, "CH." + (i + 1), _brown);
			}
			if (flag)
			{
				DrawChannelGauge(num2, num3, i);
			}
			if (flag && i == channelIndex)
			{
				DrawAnimationAt("UI/Login.img/WorldSelect/channel/chSelect", num2 + 20f, num3 + 7f, centered: true, loop: false, channelSelectedAt);
			}
			if (flag2)
			{
				EnterCharSelect();
				return;
			}
		}
		if (Button("UI/Login.img/WorldSelect/BtGoworld", R(756f, 497f, 140f, 41f)))
		{
			EnterCharSelect();
		}
		if (!string.IsNullOrEmpty(worldEntry.recommendMessage))
		{
			float num4 = 288f;
			float num5 = 302f;
			DrawBalloon("UI/Login.img/WorldNotice/BalloonForLoginProcess/", num4, num5, 186f, 76f);
			Texture2D val3 = Tex("UI/Login.img/WorldNotice/BalloonForLoginProcess/Title");
			if ((Object)(object)val3 != (Object)null)
			{
				DrawTex(val3, R(num4 + 8f, num5 + 9f, ((Texture)val3).width, ((Texture)val3).height));
			}
			Label(R(num4, num5 + 40f, 186f, 24f), worldEntry.recommendMessage, _row);
		}
	}

	private void DrawChannelGauge(float cellX, float cellY, int index)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Tex("UI/Login.img/WorldSelect/channel/chgauge");
		if (!((Object)(object)val == (Object)null))
		{
			float num = ChannelGaugeWidth((index < channels.Count) ? channels[index].load : 0, ((Texture)val).width);
			float h = Mathf.Min(((Texture)val).height, 8);
			if (!(num <= 0f))
			{
				DrawTexUv(val, R(cellX + 7f, cellY + 17f, num, h), UvRect(val, 0f, 0f, num, h));
			}
		}
	}

	public static float ChannelGaugeWidth(int load, float maxWidth)
	{
		int num = Mathf.Clamp(load / 60 + 1, 1, 14);
		return Mathf.Min(maxWidth, (float)(num * 5 + ((num == 14) ? 1 : 0) + 2));
	}

	private void EnterCharSelect()
	{
		if (worldIndex >= 0 && channels.Count != 0)
		{
			WorldEntry worldEntry = worlds[worldIndex];
			ChannelEntry channelEntry = channels[Mathf.Clamp(channelIndex, 0, Mathf.Min(3, channels.Count) - 1)];
			GameSession.BeginSession(GameSession.AccountId, GameSession.AccountName, worldEntry.id, channelEntry.channel);
			charList = GameSession.CharList(worldEntry.id, channelEntry.channel);
			selectedSlot = ((charList.characters.Count <= 0) ? (-1) : 0);
			page = 0;
			status = "";
			stage = LoginStage.CharSelect;
		}
	}

	private CharRecord CurrentChar()
	{
		if (charList == null || selectedSlot < 0 || selectedSlot >= charList.characters.Count)
		{
			return null;
		}
		return charList.characters[selectedSlot];
	}

	private void DrawCharSelect()
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		DrawWorldBackground(592f);
		DrawFull("UI/Login.img/Common/frame");
		DrawPageAt("UI/Login.img/Common/step/2", 0f, 33f);
		DrawPageAt("UI/Login.img/Common/selectWorld", 0f, 96f);
		DrawWorldPlateText();
		DrawCharSlots();
		DrawAt("Map/Obj/login.img/CharSelect/signboard/0/0", 795f, 189f);
		bool enabled = CurrentChar() != null;
		bool enabled2 = charList != null && charList.characters.Count < Slots;
		if (Button("UI/Login.img/CharSelect/BtSelect", R(826f, 207f, 101f, 30f), enabled))
		{
			EnterGame();
		}
		if (Button("UI/Login.img/CharSelect/BtNew", R(826f, 245f, 101f, 35f), enabled2))
		{
			racePick = 0;
			stage = LoginStage.RaceSelect;
		}
		if (Button("UI/Login.img/CharSelect/BtDelete", R(826f, 293f, 101f, 43f), enabled))
		{
			slotToDelete = selectedSlot;
		}
		DrawTabHint();
		DrawSideMenu(showWorldMap: false);
		if (PageCount > 1)
		{
			Rect rect = R(306f, 300f, 86f, 74f);
			Rect rect2 = R(627f, 301f, 89f, 73f);
			DrawAt("UI/Login.img/CharSelect/pageL/" + ((rect.Contains(Pointer.Pos) && page > 0) ? 1 : 0) + "/0", 306f, 300f);
			DrawAt("UI/Login.img/CharSelect/pageR/" + ((rect2.Contains(Pointer.Pos) && page < PageCount - 1) ? 1 : 0) + "/0", 627f, 301f);
			if (Clicked(rect) && page > 0)
			{
				ChangePage(page - 1);
			}
			if (Clicked(rect2) && page < PageCount - 1)
			{
				ChangePage(page + 1);
			}
			Label(R(535f, 390f, 100f, 20f), page + 1 + " / " + PageCount, _brown);
		}
		if (!string.IsNullOrEmpty(status))
		{
			Label(R(400f, 60f, 400f, 22f), status, _error);
		}
	}

	private void DrawWorldPlateText()
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		string text = ((worlds.Count > 0 && worldIndex >= 0) ? worlds[worldIndex].eventMessage : "");
		int num = ((channels.Count <= 0) ? 1 : channels[Mathf.Clamp(channelIndex, 0, channels.Count - 1)].channel);
		Label(R(90f, 108f, 120f, 20f), text, _label);
		Label(R(90f, 130f, 120f, 18f), "CH." + num, _small);
	}

	private void ChangePage(int next)
	{
		page = Mathf.Clamp(next, 0, PageCount - 1);
		selectedSlot = page * 3;
		if (charList != null && selectedSlot >= charList.characters.Count)
		{
			selectedSlot = charList.characters.Count - 1;
		}
	}

	private void DrawCharSlots()
	{
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		List<CharRecord> list = charList?.characters ?? new List<CharRecord>();
		for (int i = 0; i < 3; i++)
		{
			int num = page * 3 + i;
			float num2 = 492 + 125 * i;
			float num3 = 429f;
			if (num < list.Count)
			{
				CharRecord charRecord = list[num];
				bool flag = num == selectedSlot;
				float x = num2;
				float y = num3 - 337f;
				if (flag)
				{
					DrawAnimationAt("UI/Login.img/CharSelect/effect/1", x, y, centered: true, loop: true, selectionStartedAt);
				}
				DrawAnimationAt("UI/Login.img/CharSelect/character/0", num2, num3, centered: true);
				DrawCharacter(CharPreview(charRecord, flag), num2, num3);
				DrawNameTag(num2, num3 + 2f, charRecord.name, flag);
				if (flag)
				{
					DrawAnimationAt("UI/Login.img/CharSelect/effect/0", x, y, centered: true, loop: false, selectionStartedAt);
				}
				if (flag)
				{
					DrawCharInfo(charRecord, num2);
				}
			}
			else if (num < Slots)
			{
				DrawAt("UI/Login.img/CharSelect/character/1/0", num2, num3, centered: true);
			}
			Rect rect = R(num2 - 25f, num3 - 80f, 50f, 80f);
			if (Clicked(rect))
			{
				if (lastSlotClick == num && Time.unscaledTime - lastSlotClickAt < 0.35f)
				{
					EnterGame();
				}
				lastSlotClick = num;
				lastSlotClickAt = Time.unscaledTime;
				if (num < list.Count)
				{
					selectedSlot = num;
				}
			}
		}
	}

	private void DrawCharInfo(CharRecord rec, float slotX)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		float num = slotX - 120f;
		float num2 = 195f;
		DrawAnimationAt("UI/Login.img/CharSelect/scroll/0", num, num2, centered: false, loop: false, Mathf.Max(pageStartedAt, selectionStartedAt));
		if (!(Time.unscaledTime - Mathf.Max(pageStartedAt, selectionStartedAt) < 0.35f))
		{
			DrawAt("UI/Login.img/CharSelect/charInfo", num + 18f, num2 + 30f);
			float num3 = num + 18f;
			float num4 = num2 + 30f;
			Label(R(num3 + 43f, num4 + 2f, 100f, 16f), JobDefs.NameOf(rec.job), _brown);
			Label(R(num3 + 43f, num4 + 20f, 50f, 16f), rec.level.ToString(), _brown);
			Label(R(num3 + 132f, num4 + 20f, 40f, 16f), rec.fame.ToString(), _brown);
			Label(R(num3 + 43f, num4 + 38f, 50f, 16f), rec.str.ToString(), _brown);
			Label(R(num3 + 132f, num4 + 38f, 40f, 16f), rec.int_.ToString(), _brown);
			Label(R(num3 + 43f, num4 + 56f, 50f, 16f), rec.dex.ToString(), _brown);
			Label(R(num3 + 132f, num4 + 56f, 40f, 16f), rec.luk.ToString(), _brown);
			Label(R(num3 + 14f, num4 + 101f, 183f, 16f), "排名 " + rec.rank + "    人气 " + rec.fame, _brownC);
		}
	}

	private Sprite CharPreview(CharRecord rec, bool selected)
	{
		string[] ids = GameSession.PreviewAppearance(rec);
		return AnimatedPreview(ids, selected);
	}

	private void EnterGame()
	{
		CharRecord charRecord = CurrentChar();
		if (charRecord != null)
		{
			if (GameSession.SelectChar((int)charRecord.id))
			{
				status = "";
				GameLoop.I.EnterGameFromLogin();
			}
			else
			{
				status = "角色读取失败";
			}
		}
	}

	public void ForceLogout(string message)
	{
		BackToLogin(message);
	}

	public void BackToLogin(string message)
	{
		account = (saveId ? PlayerPrefs.GetString("login_account", "") : "");
		password = "";
		charName = "";
		charList = null;
		worlds.Clear();
		channels.Clear();
		worldIndex = -1;
		channelIndex = 0;
		slotToDelete = -1;
		scrollOpen = false;
		noticeOpen = false;
		newAccountOpen = false;
		genderOpen = false;
		alertOpen = false;
		page = 0;
		selectedSlot = 0;
		stage = LoginStage.Login;
		status = (string.IsNullOrEmpty(message) ? "" : message);
	}

	public void BackToCharSelect()
	{
		slotToDelete = -1;
		status = "";
		if (GameSession.AccountId > 0)
		{
			if (worlds.Count == 0)
			{
				worlds = GameSession.ServerList().worlds;
				worldIndex = ((worlds.Count <= 0) ? (-1) : 0);
				if (worldIndex >= 0)
				{
					SelectWorld(worldIndex);
				}
			}
			charList = GameSession.CharList(GameSession.World, GameSession.Channel);
			selectedSlot = ((charList.characters.Count <= 0) ? (-1) : 0);
		}
		page = 0;
		stage = LoginStage.CharSelect;
	}

	private void DrawDeleteDialog()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.55f));
		CharRecord charRecord = CurrentChar();
		DrawAt("UI/Login.img/Notice/backgrnd/0", 459f, 251f);
		string text = ((charRecord != null) ? charRecord.name : "");
		Label(R(490f, 290f, 300f, 24f), "确定删除角色「" + text + "」？", _brown);
		Label(R(490f, 322f, 300f, 20f), "删除后角色数据不可恢复。", _brown);
		if (Button("UI/Login.img/Notice/BtYes", R(540f, 410f, 81f, 41f), enabled: true, popup: true))
		{
			if (charRecord != null)
			{
				GameSession.DeleteChar((int)charRecord.id);
			}
			slotToDelete = -1;
			charList = GameSession.CharList(GameSession.World, GameSession.Channel);
			selectedSlot = ((charList.characters.Count <= 0) ? (-1) : 0);
			status = "";
		}
		if (Button("UI/Login.img/Notice/BtNo", R(660f, 410f, 81f, 41f), enabled: true, popup: true))
		{
			slotToDelete = -1;
		}
	}

	private void DrawGenderDialog()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		DrawAnimationAt("UI/Login.img/Gender/scroll/0", 595f, 159f, centered: false, loop: false, genderStartedAt);
		if (!(Time.unscaledTime - genderStartedAt < 0.35f))
		{
			DrawAt("UI/Login.img/Gender/text/0", 617f, 185f);
			Label(R(751f, 261f, 60f, 22f), (newGender == 0) ? "男" : "女", _genderValue);
			if (Clicked(R(743f, 257f, 98f, 28f), enabled: true, popup: true))
			{
				newGender = ((newGender == 0) ? 1 : 0);
			}
			if (Button("UI/Login.img/Gender/BtYes", R(785f, 310f, 75f, 32f), enabled: true, popup: true))
			{
				ConfirmNewGender();
			}
			if (Button("UI/Login.img/Gender/BtNo", R(857f, 310f, 75f, 32f), enabled: true, popup: true))
			{
				genderOpen = false;
			}
		}
	}

	private void DrawNewAccountDialog()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.45f));
		float num = 459f;
		float num2 = 251f;
		DrawAt("UI/Login.img/Notice/backgrnd/0", num, num2);
		if (Has("UI/Login.img/Notice/text/6"))
		{
			DrawAt("UI/Login.img/Notice/text/6", num + 119f, num2 + 21f);
		}
		else
		{
			Label(R(num + 121f, num2 + 48f, 219f, 78f), "可以使用的账号。\n确定要用这个账号，请按下“确定”按钮。", _alert);
		}
		if (Button("UI/Login.img/Notice/BtYes", R(num + 106f, num2 + 168f, 75f, 32f), enabled: true, popup: true))
		{
			ConfirmNewAccount();
		}
		if (Button("UI/Login.img/Notice/BtNo", R(num + 181f, num2 + 168f, 75f, 32f), enabled: true, popup: true))
		{
			CancelNewAccount();
		}
	}

	private float EulaCharWidth(char c)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (c == ' ')
		{
			return 4f;
		}
		if (eulaCharWidths.TryGetValue(c, out var value))
		{
			return value;
		}
		value = _eula.CalcSize(new GUIContent(c.ToString())).x / Mathf.Max(0.001f, s);
		eulaCharWidths[c] = value;
		return value;
	}

	private void BuildEulaLayout()
	{
		if (eulaLines != null && Mathf.Approximately(eulaLayoutScale, s))
		{
			return;
		}
		eulaLayoutScale = s;
		eulaCharWidths.Clear();
		eulaLines = new List<EulaLine>();
		StringBuilder stringBuilder = new StringBuilder();
		ImgNode imgNode = WzAssets.Node("wz://String/EULA.img/EULA");
		if (imgNode != null)
		{
			for (int i = 0; i < 2048; i++)
			{
				string text = imgNode.Text("Text" + i.ToString("00"), null);
				if (text == null)
				{
					break;
				}
				stringBuilder.Append(text);
			}
		}
		string text2 = stringBuilder.ToString().Replace("\\\\r\\\\n", "\n").Replace("\\r\\n", "\n");
		EulaLine line = new EulaLine();
		EulaRun run = new EulaRun();
		float runW = 0f;
		bool bold = false;
		for (int j = 0; j < text2.Length; j++)
		{
			char c = text2[j];
			if (c == '\\' && j + 3 < text2.Length && text2[j + 1] == 'r' && text2[j + 2] == '\\' && text2[j + 3] == 'n')
			{
				NewLine();
				j += 3;
				continue;
			}
			switch (c)
			{
			case '\n':
			case '\r':
				NewLine();
				continue;
			case '#':
			{
				char c2 = ((j + 1 < text2.Length) ? text2[j + 1] : ' ');
				if (c2 == 'e' || c2 == 'n')
				{
					FlushRun();
					bold = c2 == 'e';
					j++;
					continue;
				}
				break;
			}
			}
			float num = EulaCharWidth(c);
			if (line.width + runW + num > 353f && line.width + runW > 0f)
			{
				NewLine();
			}
			if (run.text.Length == 0)
			{
				run.bold = bold;
			}
			else if (run.bold != bold)
			{
				FlushRun();
			}
			run.text += c;
			runW += num;
		}
		FlushRun();
		if (line.runs.Count > 0)
		{
			eulaLines.Add(line);
		}
		void FlushRun()
		{
			if (run.text.Length != 0)
			{
				run.width = runW;
				line.runs.Add(run);
				line.width += runW;
				run = new EulaRun
				{
					bold = bold
				};
				runW = 0f;
			}
		}
		void NewLine()
		{
			FlushRun();
			eulaLines.Add(line);
			line = new EulaLine();
		}
	}

	private void DrawNoticeDialog()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		BuildEulaLayout();
		float eulaMaxScroll = EulaMaxScroll;
		noticeScroll = ((eulaMaxScroll <= 0f) ? 0f : Mathf.Clamp01(noticeScroll));
		float num = noticeScroll * eulaMaxScroll;
		UiKit.Fill(new Rect(0f, 0f, UiKit.W, UiKit.H), new Color(0f, 0f, 0f, 0.45f));
		DrawAt("UI/Login.img/TOS/0", 640f, 360f, centered: true);
		GUI.BeginGroup(R(461f, 188f, 353f, 308f));
		float num2 = (0f - num) * s;
		foreach (EulaLine eulaLine in eulaLines)
		{
			float num3 = 0f;
			foreach (EulaRun run in eulaLine.runs)
			{
				Rect val = new Rect(num3 * s, num2, run.width * s + 2f * s, 17f * s);
				GUI.Label(val, run.text, _eula);
				if (run.bold)
				{
					GUI.Label(new Rect(val.x + 0.8f * s, val.y, val.width, val.height), run.text, _eula);
				}
				num3 += run.width;
			}
			num2 += 17f * s;
		}
		GUI.EndGroup();
		DrawEulaScrollBar(num, eulaMaxScroll);
		bool enabled = eulaMaxScroll <= 0.5f || noticeScroll >= 0.995f;
		if (Button("UI/Login.img/BtOk", R(691f, 521f, 75f, 32f), enabled, popup: true))
		{
			AcceptNotice();
		}
		if (Button("UI/Login.img/BtCancel", R(765f, 521f, 75f, 32f), enabled: true, popup: true))
		{
			CancelNotice();
		}
	}

	private void AcceptNotice()
	{
		noticeOpen = false;
		eulaBarGrab = -1f;
		ConfirmNewAccount();
	}

	private void CancelNotice()
	{
		noticeOpen = false;
		eulaBarGrab = -1f;
		CancelNewAccount();
	}

	private void HandleEulaScrollInput()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		float eulaMaxScroll = EulaMaxScroll;
		if (eulaMaxScroll <= 0f)
		{
			noticeScroll = 0f;
			return;
		}
		float num = Mathf.Clamp01(noticeScroll) * eulaMaxScroll;
		float num2 = 0f;
		float y = Input.mouseScrollDelta.y;
		if (y != 0f)
		{
			num2 -= y * 60f;
		}
		if (Input.GetKeyDown((KeyCode)274))
		{
			num2 += 17f;
		}
		if (Input.GetKeyDown((KeyCode)273))
		{
			num2 -= 17f;
		}
		if (Input.GetKeyDown((KeyCode)281))
		{
			num2 += 277.19998f;
		}
		if (Input.GetKeyDown((KeyCode)280))
		{
			num2 -= 277.19998f;
		}
		if (num2 != 0f)
		{
			noticeScroll = Mathf.Clamp01((num + num2) / eulaMaxScroll);
		}
	}

	private void DrawEulaScrollBar(float scrollPx, float max)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Invalid comparison between Unknown and I4
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		bool flag = max > 0.5f;
		string text = "UI/Basic.img/VScr6/" + (flag ? "enabled/" : "disabled/");
		float x = 815f;
		float num = 187f;
		float num2 = 317f;
		Rect rect = R(x, num, 12f, 12f);
		Rect rect2 = R(x, num + num2 - 12f, 12f, 12f);
		Rect val = R(x, num + 12f, 12f, num2 - 24f);
		DrawTexTiledV(Tex(text + "base"), val);
		if (!flag)
		{
			DrawTex(Tex(text + "prev"), R(x, num, 12f, 12f));
			DrawTex(Tex(text + "next"), R(x, num + num2 - 12f, 12f, 12f));
			return;
		}
		bool flag2 = rect.Contains(Pointer.Pos);
		bool flag3 = rect2.Contains(Pointer.Pos);
		DrawTex(Tex(text + (flag2 ? "prev1" : "prev0")), R(x, num, 12f, 12f));
		DrawTex(Tex(text + (flag3 ? "next1" : "next0")), R(x, num + num2 - 12f, 12f, 12f));
		float num3 = 12f * s;
		float num4 = val.y + (val.height - num3) * ((max <= 0f) ? 0f : Mathf.Clamp01(scrollPx / max));
		Rect val2 = new Rect(val.x, num4, val.width, num3);
		DrawTex(Tex(text + ((eulaBarGrab >= 0f) ? "thumb1" : "thumb0")), val2);
		if (Clicked(rect, enabled: true, popup: true))
		{
			noticeScroll = Mathf.Clamp01((scrollPx - 5f) / max);
		}
		if (Clicked(rect2, enabled: true, popup: true))
		{
			noticeScroll = Mathf.Clamp01((scrollPx + 5f) / max);
		}
		if ((int)Event.current.type == 7)
		{
			HandleEulaThumbDrag(val2, val);
		}
		if (Clicked(val, enabled: true, popup: true) && !val2.Contains(Pointer.Pos))
		{
			float num5 = (float)Mathf.Max(16, (Mathf.CeilToInt(max / 5f) + 1) / 16) * 5f;
			noticeScroll = Mathf.Clamp01((scrollPx + ((Pointer.Pos.y < val2.y) ? (0f - num5) : num5)) / max);
		}
	}

	private void HandleEulaThumbDrag(Rect thumb, Rect track)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (eulaBarGrab < 0f && Pointer.Pressed && thumb.Contains(Pointer.PressPosition))
		{
			eulaBarGrab = Pointer.PressPosition.y - thumb.y;
		}
		if (!Pointer.Down)
		{
			eulaBarGrab = -1f;
		}
		if (eulaBarGrab >= 0f && track.height > thumb.height)
		{
			noticeScroll = Mathf.Clamp01((Pointer.Pos.y - eulaBarGrab - track.y) / (track.height - thumb.height));
		}
	}

	private void DrawRaceSelect()
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		DrawWorldBackground(0f);
		DrawFull("UI/Login.img/Common/frame");
		DrawPageAt("UI/Login.img/Common/step/3", 0f, 33f);
		DrawPageAt("UI/Login.img/Common/selectWorld", 0f, 96f);
		DrawWorldPlateText();
		DrawAt("UI/Login.img/RaceSelect/textGL/0", 646f, 111f, centered: true);
		int[] array = new int[3] { 223, 209, 181 };
		float[] array2 = new float[3] { 536f, 344f, 752f };
		float[] array3 = new float[3] { 130f, 122f, 124f };
		for (int i = 0; i < 3; i++)
		{
			Rect rect = R(array2[i], array3[i], array[i], 222f);
			if (Clicked(rect))
			{
				racePick = i;
			}
			string text = i switch
			{
				1 => "knight/BtKnight", 
				0 => "normal/BtNormal", 
				_ => "aran/BtAran", 
			};
			string text2 = i switch
			{
				1 => "knight/OnAnimation", 
				0 => "normal/OnAnimation", 
				_ => "aran/OnAnimation", 
			};
			DrawAt("UI/Login.img/RaceSelect/" + ((i == racePick) ? text2 : (text + (rect.Contains(Pointer.Pos) ? "/mouseOver" : "/normal"))) + "/0", array2[i], array3[i]);
		}
		string text3;
		if (racePick != 0)
		{
			text3 = ((racePick == 1) ? "knight/text/0" : "aran/text/0");
		}
		else
		{
			text3 = "normal/text/0";
		}
		Texture2D val = Tex("UI/Login.img/RaceSelect/" + text3);
		if ((Object)(object)val != (Object)null)
		{
			DrawTex(val, R(360f, 352f, ((Texture)val).width, ((Texture)val).height));
		}
		if (Button("UI/Login.img/RaceSelect/BtSelect", R(772f, 463f, 73f, 29f)))
		{
			jobGroup = ((racePick == 0) ? 1 : ((racePick != 1) ? 2 : 0));
			ResetAppearance();
			createStep = 0;
			charName = "";
			status = "";
			stage = LoginStage.CharCreate;
		}
		DrawSideMenu(showWorldMap: false);
	}

	private void ResetAppearance()
	{
		gender = Mathf.Clamp(PlayerPrefs.GetInt("login_new_gender", 0), 0, 1);
		editRow = 0;
		face = FaceList[0];
		hair = HairList[0];
		hairColor = HairColors[0];
		skinColor = Skins[0];
		top = TopList[0];
		bottom = BottomList[0];
		shoes = ShoesList[0];
		weapon = WeaponList[0];
	}

	private void DrawCharCreate()
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		DrawCreateBackground();
		DrawFull("UI/Login.img/Common/frame");
		DrawPageAt("UI/Login.img/Common/step/3", 0f, 33f);
		DrawPageAt("UI/Login.img/Common/selectWorld", 0f, 96f);
		DrawWorldPlateText();
		if (createStep == 0)
		{
			DrawAt("UI/Login.img/NewChar/charName", 720f, 155f);
			Rect val = R(748f, 258f, 148f, 24f);
			if (Application.isMobilePlatform)
			{
				GUI.Label(val, charName, charNameField);
			}
			else
			{
				GUI.SetNextControlName("char_name");
				charName = GUI.TextField(val, charName, 13, charNameField);
			}
			if (Clicked(val))
			{
				if (Application.isMobilePlatform)
				{
					OpenKeyboard(charName, 13, secure: false, forCharName: true);
				}
				else if (Pointer.DoubleClicked)
				{
					GUI.FocusControl("char_name");
				}
			}
			if (Button("UI/Login.img/NewChar/BtYes", R(747f, 333f, 81f, 41f)))
			{
				ConfirmCharName();
			}
			if (Button("UI/Login.img/NewChar/BtNo", R(821f, 333f, 81f, 41f)))
			{
				stage = LoginStage.RaceSelect;
			}
		}
		else
		{
			DrawAt("UI/Login.img/NewChar/charSet", 720f, 155f);
			for (int i = 0; i < 9; i++)
			{
				float num = 261 + 17 * i;
				DrawAt("UI/Login.img/NewChar/avatarSel/" + i + "/normal", 731f, num);
				Label(R(800f, num, 120f, 17f), RowValue(i), _row);
				if (Clicked(R(731f, num, 200f, 17f)))
				{
					editRow = i;
				}
				if (Button("UI/Login.img/NewChar/BtLeft", R(792f, num + 1f, 15f, 16f)))
				{
					CycleRowAt(i, -1);
				}
				if (Button("UI/Login.img/NewChar/BtRight", R(923f, num + 1f, 15f, 16f)))
				{
					CycleRowAt(i, 1);
				}
			}
			if (Button("UI/Login.img/NewChar/BtYes", R(757f, 485f, 81f, 41f)))
			{
				DoCreate();
			}
			if (Button("UI/Login.img/NewChar/BtNo", R(832f, 485f, 81f, 41f)))
			{
				createStep = 0;
			}
		}
		string[] ids = AppearanceIds(skinColor, face, hair + hairColor, top, bottom, shoes, weapon);
		DrawCharacter(AnimatedPreview(ids), 636f, 398f);
		DrawSideMenu(showWorldMap: false);
		if (!string.IsNullOrEmpty(status))
		{
			Label(R(300f, 600f, 450f, 22f), status, _error);
		}
	}

	private void ConfirmCharName()
	{
		if (!GameSession.NameFormatOk(charName))
		{
			SetAlert("角色名需 2-12 位中文、英文或数字。", null);
			return;
		}
		CheckCharNameResponse checkCharNameResponse = GameSession.CheckCharName(charName);
		if (!checkCharNameResponse.available)
		{
			SetAlert((checkCharNameResponse.reason == -2) ? "该名字已被使用，请重新输入。" : "角色名不合法，请重新输入。", null);
			return;
		}
		status = "";
		createStep = 1;
	}

	private string RowValue(int row)
	{
		switch (row)
		{
		case 0:
			return EquipName(face);
		case 1:
			return EquipName(hair);
		case 2:
			return HairColorName(hairColor);
		case 3:
			return SkinColorName(skinColor);
		case 4:
			return EquipName(top);
		case 5:
			return EquipName(bottom);
		case 6:
			return EquipName(shoes);
		case 7:
			return EquipName(weapon);
		default:
			if (gender != 0)
			{
				return "女";
			}
			return "男";
		}
	}

	private void CycleRowAt(int row, int dir)
	{
		editRow = row;
		CycleRow(dir);
	}

	private void CycleRow(int dir)
	{
		if (editRow == 0)
		{
			face = Cycle(FaceList, face, dir);
			return;
		}
		if (editRow == 1)
		{
			hair = Cycle(HairList, hair, dir);
			return;
		}
		if (editRow == 2)
		{
			hairColor = Cycle(HairColors, hairColor, dir);
			return;
		}
		if (editRow == 3)
		{
			skinColor = Cycle(Skins, skinColor, dir);
			return;
		}
		if (editRow == 4)
		{
			top = Cycle(TopList, top, dir);
			return;
		}
		if (editRow == 5)
		{
			bottom = Cycle(BottomList, bottom, dir);
			return;
		}
		if (editRow == 6)
		{
			shoes = Cycle(ShoesList, shoes, dir);
			return;
		}
		if (editRow == 7)
		{
			weapon = Cycle(WeaponList, weapon, dir);
			return;
		}
		gender = ((gender == 0) ? 1 : 0);
		face = FaceList[0];
		hair = HairList[0];
		top = TopList[0];
		bottom = BottomList[0];
	}

	private static int Cycle(int[] list, int value, int dir)
	{
		int num = Array.IndexOf(list, value);
		if (num < 0)
		{
			num = 0;
		}
		num = (num + dir + list.Length) % list.Length;
		return list[num];
	}

	private static string[] AppearanceIds(int skin, int face, int hairWithColor, int top, int bottom, int shoes, int weapon)
	{
		return new string[8]
		{
			Id8(2000 + skin),
			Id8(12000 + skin),
			Id8(face),
			Id8(hairWithColor),
			Id8(top),
			Id8(bottom),
			Id8(shoes),
			Id8(weapon)
		};
	}

	private static int DefaultTop(int gender, int job)
	{
		if (job != 2)
		{
			if (gender != 0)
			{
				return TopsF[0];
			}
			return TopsM[0];
		}
		return AranTop[0];
	}

	private static int DefaultBottom(int gender, int job)
	{
		if (job != 2)
		{
			if (gender != 0)
			{
				return BottomsF[0];
			}
			return BottomsM[0];
		}
		return AranBottom[0];
	}

	private static int DefaultShoes(int job)
	{
		if (job != 2)
		{
			return Shoes[0];
		}
		return AranShoes[0];
	}

	private static int DefaultWeapon(int job)
	{
		if (job != 2)
		{
			return Weapons[0];
		}
		return AranWeapon[0];
	}

	private static string Id8(int id)
	{
		return id.ToString("D8");
	}

	private string EquipName(int id)
	{
		if (equipNames.TryGetValue(id, out var value))
		{
			return value;
		}
		string text = EquipCategory(id);
		string text2 = null;
		if (text != null)
		{
			text2 = WzAssets.Node("String/Eqp.img/Eqp/" + text + "/" + id)?.Text("name");
		}
		if (string.IsNullOrEmpty(text2))
		{
			text2 = id.ToString();
		}
		equipNames[id] = text2;
		return text2;
	}

	private static string EquipCategory(int id)
	{
		switch (id / 10000)
		{
		case 2:
			return "Face";
		case 3:
			return "Hair";
		case 100:
			return "Cap";
		case 104:
			return "Coat";
		case 105:
			return "Longcoat";
		case 106:
			return "Pants";
		case 107:
			return "Shoes";
		case 108:
			return "Glove";
		case 109:
			return "Shield";
		case 110:
			return "Cape";
		default:
			return null;
		case 130:
		case 131:
		case 132:
		case 133:
		case 134:
		case 135:
		case 136:
		case 137:
		case 138:
		case 139:
		case 140:
		case 141:
		case 142:
		case 143:
		case 144:
		case 145:
		case 146:
		case 147:
		case 148:
		case 149:
		case 150:
		case 151:
		case 152:
		case 153:
		case 154:
		case 155:
		case 156:
		case 157:
		case 158:
		case 159:
		case 160:
			return "Weapon";
		}
	}

	private static string HairColorName(int color)
	{
		return color switch
		{
			0 => "黑色", 
			2 => "红色", 
			3 => "橙色", 
			7 => "绿色", 
			_ => color.ToString(), 
		};
	}

	private static string SkinColorName(int color)
	{
		return color switch
		{
			0 => "浅色肌肤", 
			1 => "小麦肌肤", 
			2 => "深色肌肤", 
			3 => "古铜肌肤", 
			_ => color.ToString(), 
		};
	}

	private Sprite AnimatedPreview(string[] ids, bool walk = false)
	{
		string text = string.Join("_", ids);
		int frame = WzCharacter.AppearanceFrame(ids, Time.unscaledTime, walk);
		string key = text + "/" + (walk ? "walk" : "stand") + "/" + frame;
		if (!previewSprites.ContainsKey(key))
		{
			AddPreview(key, ids, frame, walk);
		}
		if (!previewSprites.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	private void AddPreview(string key, string[] ids, int frame, bool walk)
	{
		if (previewSprites.ContainsKey(key))
		{
			return;
		}
		try
		{
			Sprite value = WzCharacter.Appearance(string.Join("_", ids), ids, frame, walk);
			previewSprites[key] = value;
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[LoginFlow] 角色预览失败 " + key + ": " + ex.Message));
			previewSprites[key] = null;
		}
	}

	private void DrawCharacter(Sprite sprite, float feetX, float feetY, float scale = 1f)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)sprite == (Object)null))
		{
			Rect rect = sprite.rect;
			float num = rect.width * scale;
			rect = sprite.rect;
			float num2 = rect.height * scale;
			float x = sprite.pivot.x;
			rect = sprite.rect;
			float num3 = x / rect.width;
			float y = sprite.pivot.y;
			rect = sprite.rect;
			float num4 = y / rect.height;
			float x2 = feetX - num3 * num;
			float y2 = feetY - (1f - num4) * num2;
			DrawTex(sprite.texture, R(x2, y2, num, num2));
		}
	}

	private void DoCreate()
	{
		if (!GameSession.NameFormatOk(charName))
		{
			SetAlert("角色名需 2-12 位中文、英文或数字。", null);
			return;
		}
		CheckCharNameResponse checkCharNameResponse = GameSession.CheckCharName(charName);
		if (!checkCharNameResponse.available)
		{
			SetAlert((checkCharNameResponse.reason == -2) ? "该名字已被使用，请重新输入。" : "角色名不合法，请重新输入。", null);
			return;
		}
		CreateCharResponse createCharResponse = GameSession.CreateChar(new CreateCharRequest
		{
			name = charName,
			job = jobGroup,
			face = face,
			hair = hair,
			hairColor = hairColor,
			skinColor = skinColor,
			top = top,
			bottom = bottom,
			shoes = shoes,
			weapon = weapon,
			gender = gender
		});
		if (createCharResponse.code != 0)
		{
			SetAlert(createCharResponse.error ?? ("创建失败（" + createCharResponse.code + "）"), null);
			return;
		}
		status = "";
		charList = GameSession.CharList(GameSession.World, GameSession.Channel);
		selectedSlot = ((charList.characters.Count > 0) ? (charList.characters.Count - 1) : (-1));
		page = Mathf.Max(0, selectedSlot / 3);
		stage = LoginStage.CharSelect;
	}

	static LoginFlow()
	{
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
	}
}
