using System;
using System.Collections.Generic;
using Server.Bot;
using UnityEngine;

public sealed class NativeAndroid : MonoBehaviour
{
	private struct LandingQuery
	{
		private List<Vector4> terrain;

		private int count;

		private float x;

		private float ceiling;

		private float result;

		private bool valid;

		private bool found;

		public bool Read(float nextX, float nextCeiling, out float landing)
		{
			List<Vector4> list = PlayerCtrl.Terrain;
			int num = list?.Count ?? 0;
			if (!valid || terrain != list || count != num || x != nextX || ceiling != nextCeiling)
			{
				terrain = list;
				count = num;
				x = nextX;
				ceiling = nextCeiling;
				found = PlayerCtrl.FindLanding(x, ceiling, float.NegativeInfinity, -1, out result);
				valid = true;
			}
			landing = result;
			return found;
		}
	}

	private const float TeleportGap = 320f;

	private const float ReadInterval = 0.2f;

	private const float BalloonSeconds = 5f;

	private const float FarewellSeconds = 2.2f;

	private const float ArtBaseHeight = 768f;

	private static readonly Func<int, int> SlotOf = Profile.EquipOfSlot;

	private static readonly string[] ActionFields = new string[4] { "action", "face", "chat", "sound" };

	private PlayerCtrl owner;

	private GameObject visual;

	private SpriteRenderer body;

	private SpriteAnim anim;

	private string key;

	private int templateId;

	private int balloonStyle;

	private int lastLevel;

	private int lastJob = -1;

	private float readAt;

	private float facing = 1f;

	private float lastHp;

	private float byeAt;

	private bool deathPlayed;

	private bool spawned;

	private Vector2 feet;

	private NativeMovementPhysics movementPhysics;

	private double horizontalVelocity;

	private int followDirection;

	private int previousAndroidX;

	private string balloonText;

	private float balloonUntil;

	private float balloonSpan = 5f;

	private float lastActionAt = float.NegativeInfinity;

	private readonly Random random = new Random();

	private readonly int[] equipmentSnapshot = new int[2 + NativeAndroidService.ApSlots.Length];

	private int snapshotCharacter;

	private AndroidState resolved;

	private GUIStyle balloonMeasure;

	private GUIStyle balloonLabel;

	private LandingQuery ownerLanding;

	private LandingQuery localLanding;

	public string Reason { get; private set; } = "";

	public void Tick(PlayerCtrl player, float dt)
	{
		using (BotPerf.Timer("Android.Tick"))
		{
			owner = player;
			if (!GameSession.InGame || (Object)(object)player == (Object)null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing)
			{
				Clear();
				return;
			}
			if (Time.unscaledTime >= readAt)
			{
				readAt = Time.unscaledTime + 0.2f;
				Sync();
			}
			if (!((Object)(object)visual == (Object)null))
			{
				Move(dt);
				Events();
			}
		}
	}

	private void Sync()
	{
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected Obj, but got Unknown
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		using (BotPerf.Timer("Android.Sync"))
		{
			bool flag = resolved == null || snapshotCharacter != GameSession.CharacterId;
			for (int i = 0; i < equipmentSnapshot.Length; i++)
			{
				int arg = i switch
				{
					1 => -54, 
					0 => -32, 
					_ => NativeAndroidService.ApSlots[i - 2], 
				};
				int num = SlotOf(arg);
				if (equipmentSnapshot[i] != num)
				{
					flag = true;
				}
				equipmentSnapshot[i] = num;
			}
			snapshotCharacter = GameSession.CharacterId;
			if (flag)
			{
				resolved = NativeAndroidService.Resolve(snapshotCharacter, SlotOf);
			}
			AndroidState androidState = resolved;
			Reason = androidState.reason;
			if (!androidState.visible)
			{
				if ((Object)(object)visual != (Object)null && byeAt == 0f)
				{
					byeAt = Time.unscaledTime + 2.2f;
					PlayAction("bye", 2f);
				}
				if (byeAt > 0f && Time.unscaledTime >= byeAt)
				{
					byeAt = 0f;
					Clear();
				}
			}
			else
			{
				if (!flag && (Object)(object)visual != (Object)null && byeAt == 0f)
				{
					return;
				}
				byeAt = 0f;
				string[] array = AndroidLook.Ids(androidState);
				if (array == null)
				{
					Clear();
					Reason = "机器人外观资源缺失";
					return;
				}
				string text = AndroidLook.Key(array);
				if (balloonStyle == 0 || templateId != androidState.templateId)
				{
					balloonStyle = BalloonStyleOf(androidState.templateId);
				}
				templateId = androidState.templateId;
				if (!(text == key))
				{
					key = text;
					if ((Object)(object)visual == (Object)null)
					{
						visual = new GameObject("android");
						visual.transform.SetParent(((Component)this).transform, false);
						body = visual.AddComponent<SpriteRenderer>();
						anim = visual.AddComponent<SpriteAnim>();
						feet = owner.FeetPos;
						horizontalVelocity = 0.0;
						movementPhysics = NativeMovementPhysics.Load();
						previousAndroidX = (int)(feet.x * 40f);
						followDirection = 0;
						spawned = false;
					}
					((Renderer)body).sortingOrder = owner.SortingOrder - 1;
					WzCharacter.Register(key, array);
					anim.Init("chars", key, "stand", body);
					anim.SetFacing((int)facing);
					if (!spawned)
					{
						spawned = true;
						PlayAction("hello");
					}
				}
			}
		}
	}

	private static int BalloonStyleOf(int templateId)
	{
		Dictionary<string, object> dictionary = NativeAndroidService.Template(templateId);
		if (dictionary != null && dictionary.TryGetValue("info", out var value) && value is Dictionary<string, object> dictionary2 && dictionary2.TryGetValue("chatBalloon", out var value2))
		{
			int num = Convert.ToInt32(value2);
			if (num > 0)
			{
				return num;
			}
		}
		return 0;
	}

	private void Clear()
	{
		if ((Object)(object)visual != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)visual);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)visual);
			}
		}
		visual = null;
		body = null;
		anim = null;
		key = null;
		spawned = false;
		balloonText = null;
		lastActionAt = float.NegativeInfinity;
		byeAt = 0f;
		lastHp = 0f;
		lastLevel = 0;
		lastJob = -1;
		deathPlayed = false;
		templateId = 0;
		balloonStyle = 0;
		resolved = null;
		ownerLanding = default;
		localLanding = default;
	}

	private void OnDisable()
	{
		Clear();
	}

	private void Move(float dt)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)owner == (Object)null)
		{
			return;
		}
		Vector2 feetPos = owner.FeetPos;
		int ownerX = (int)(feetPos.x * 40f);
		int androidX = (int)(feet.x * 40f);
		bool flag = owner.facing < 0;
		followDirection = NativeAndroidService.FollowDirection(flag, flag, ownerX, androidX, previousAndroidX, followDirection);
		previousAndroidX = androidX;
		bool flag2 = Mathf.Abs(feetPos.x - feet.x) > 8f;
		float num = 0f;
		float num2 = feetPos.y;
		if (ownerLanding.Read(feetPos.x, feetPos.y + 0.3f, out var landing))
		{
			num2 = landing;
		}
		if (flag2)
		{
			feet.x = feetPos.x;
			followDirection = 0;
			horizontalVelocity = 0.0;
			feet.y = num2;
		}
		else
		{
			double num3 = horizontalVelocity;
			horizontalVelocity = movementPhysics.Ground(horizontalVelocity, followDirection, 0.0, 1.0, (double)(Mathf.Max(0.5f, owner.moveSpeed) * 40f) * 1.15 / movementPhysics.WalkSpeed, 0.0, dt);
			num = (float)((num3 + horizontalVelocity) * 0.5 * (double)dt / 40.0);
			feet.x += num;
			float num4 = num2;
			if (localLanding.Read(feet.x, num2 + 0.3f, out var landing2))
			{
				num4 = landing2;
			}
			if (num4 > feet.y)
			{
				feet.y = num4;
			}
			else
			{
				feet.y = Mathf.MoveTowards(feet.y, num4, 10.5f * dt);
			}
		}
		bool flag3 = !flag2 && num != 0f;
		if (flag3)
		{
			facing = ((num > 0f) ? 1 : (-1));
		}
		else
		{
			facing = ((feetPos.x >= feet.x) ? 1 : (-1));
		}
		visual.transform.position = new Vector3(feet.x, feet.y, 0f);
		((Renderer)body).sortingOrder = owner.SortingOrder - 1;
		anim.SetBase(flag3 ? "walk" : "stand");
		anim.SetFacing((int)facing);
	}

	private void Events()
	{
		if (Profile.level > lastLevel && lastLevel > 0)
		{
			PlayAction("levelup");
		}
		lastLevel = Profile.level;
		if (Profile.JobId != lastJob && lastJob >= 0)
		{
			PlayAction("job");
		}
		lastJob = Profile.JobId;
		bool flag = owner.hp <= 0f;
		if (flag && !deathPlayed)
		{
			deathPlayed = true;
			PlayAction("dead");
		}
		else if (!flag)
		{
			deathPlayed = false;
		}
		if (!flag && lastHp > 0f && owner.hp < lastHp)
		{
			PlayAction("alert");
		}
		lastHp = owner.hp;
	}

	public void PlayAction(string action, float seconds = 5f)
	{
		if (templateId == 0 || (Object)(object)anim == (Object)null)
		{
			return;
		}
		Dictionary<string, object> dictionary = NativeAndroidService.RollAction(templateId, action, random);
		if (dictionary == null)
		{
			return;
		}
		bool flag = false;
		string[] actionFields = ActionFields;
		foreach (string text in actionFields)
		{
			if (dictionary.TryGetValue(text, out var value) && value is string { Length: >0 })
			{
				flag = true;
				break;
			}
		}
		if (!flag || Time.unscaledTime - lastActionAt < 1f)
		{
			return;
		}
		lastActionAt = Time.unscaledTime;
		if (dictionary.TryGetValue("action", out var value2) && value2 is string { Length: >0 } text3)
		{
			anim.PlayOnce(text3);
		}
		if (dictionary.TryGetValue("face", out var value3) && value3 is string value4)
		{
			int num = Array.IndexOf(SpriteAnim.EmoteNames, value4);
			if (num > 0)
			{
				anim.Express(num, checkCooldown: false);
			}
		}
		if (dictionary.TryGetValue("chat", out var value5) && value5 is string { Length: >0 } text4)
		{
			balloonText = text4;
			balloonSpan = Mathf.Max(0.2f, seconds);
			balloonUntil = Time.unscaledTime + seconds;
		}
	}

	private void DoubleClickShop()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		if (!Pointer.DoubleClicked || Pointer.Blocked || GameWindows.AnyVisible || (Object)(object)visual == (Object)null || (Object)(object)body == (Object)null || (Object)(object)body.sprite == (Object)null || byeAt != 0f || templateId == 0)
		{
			return;
		}
		Camera main = Camera.main;
		if (!((Object)(object)main == (Object)null))
		{
			Vector3 val = main.ScreenToWorldPoint(new Vector3(Pointer.Pos.x * UiKit.Scale, (float)Screen.height - Pointer.Pos.y * UiKit.Scale, 0f));
			Bounds bounds = ((Renderer)body).bounds;
			float x = val.x;
			float y = val.y;
			Bounds bounds2 = ((Renderer)body).bounds;
			if (bounds.Contains(new Vector3(x, y, bounds2.center.z)))
			{
				Pointer.ConsumeClick();
				NativeNpcShopWindow.OpenAndroid(templateId);
			}
		}
	}

	private void OnGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected Obj, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected Obj, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Expected Obj, but got Unknown
		if ((int)Event.current.type != 7)
		{
			return;
		}
		DoubleClickShop();
		if (string.IsNullOrEmpty(balloonText))
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime > balloonUntil)
		{
			balloonText = null;
			return;
		}
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null || (Object)(object)body == (Object)null || (Object)(object)visual == (Object)null)
		{
			return;
		}
		if (balloonMeasure == null)
		{
			balloonMeasure = new GUIStyle(GUI.skin.label)
			{
				font = UiKit.Font,
				fontSize = 12,
				wordWrap = false
			};
		}
		int innerWidth = (int)Mathf.Max(24f, balloonMeasure.CalcSize(new GUIContent(balloonText)).x);
		NativeChatBalloon.Layout layout = NativeChatBalloon.Prepare(balloonStyle, innerWidth, 1) ?? NativeChatBalloon.Prepare(0, innerWidth, 1) ?? NativeChatBalloon.Prepare("pet/0", innerWidth, 1);
		if (layout == null)
		{
			return;
		}
		float num = Mathf.Max(0.35f, UiKit.H / 768f);
		float num2;
		if (!((Object)(object)body.sprite != (Object)null))
		{
			num2 = feet.y + 0.3f;
		}
		else
		{
			Bounds bounds = ((Renderer)body).bounds;
			num2 = bounds.max.y;
		}
		float num3 = num2;
		Vector3 val = main.WorldToScreenPoint(new Vector3(feet.x, num3, 0f));
		if (!(val.z < 0f))
		{
			Vector2 val2 = new Vector2(val.x / UiKit.Scale / num, ((float)Screen.height - val.y) / UiKit.Scale / num);
			float num4 = Mathf.Clamp01((balloonUntil - unscaledTime) / balloonSpan);
			UiKit.Begin();
			Matrix4x4 matrix = GUI.matrix;
			Color color = GUI.color;
			GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
			GUI.color = new Color(1f, 1f, 1f, num4);
			Texture2D val3 = layout.Frame(unscaledTime);
			Rect val4 = new Rect(val2.x - (float)layout.anchorX, val2.y - (float)((Texture)val3).height, (float)((Texture)val3).width, (float)((Texture)val3).height);
			GUI.DrawTexture(val4, (Texture)(object)val3);
			if (balloonLabel == null)
			{
				balloonLabel = new GUIStyle(GUI.skin.label)
				{
					font = UiKit.Font,
					fontSize = Mathf.Max(12, NativeChatBalloon.FontSize(balloonStyle)),
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)0
				};
			}
			GUIStyle val5 = balloonLabel;
			val5.fontSize = Mathf.Max(12, NativeChatBalloon.FontSize(balloonStyle));
			Color color2 = layout.color;
			val5.normal.textColor = new Color(color2.b, color2.g, color2.r, color2.a * num4);
			Rect text = layout.text;
			text.position += val4.position;
			GUI.Label(text, balloonText, val5);
			GUI.color = color;
			GUI.matrix = matrix;
		}
	}
}
