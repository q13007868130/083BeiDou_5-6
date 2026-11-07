using System.Collections.Generic;
using Server.Bot;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
	public enum GameState
	{
		Login,
		Playing
	}

	public static GameLoop I;

	public PlayerCtrl player;

	public CameraRig camRig;

	public StageDirector stage;

	public FieldDirector field;

	private GameObject hudGo;

	public int killCount;

	public float elapsed;

	public bool gameOver;

	public string lastKiller = "";

	public GameState state;

	private float spawnTimer;

	private float eliteTimer;

	private float respawnTimer;

	public readonly DeathSettlement deaths = new DeathSettlement();

	private readonly Dictionary<int, SkillRuntimeState> playerCooldowns = new Dictionary<int, SkillRuntimeState>();

	private const int MaxEnemies = 11;

	private SpriteRenderer skySr;

	private SpriteRenderer groundSr;

	private SpriteRenderer edgeSr;

	private readonly List<SpriteRenderer> farSr = new List<SpriteRenderer>();

	private readonly List<SpriteRenderer> pillarSr = new List<SpriteRenderer>();

	private readonly List<SpriteRenderer> tickSr = new List<SpriteRenderer>();

	private Backdrop backdrop;

	private string themeKey = "";

	private bool exitToLoginPending;

	public bool StageMode => (Object)(object)stage != (Object)null;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void Boot()
	{
		WzWindowsLoader.Begin(CreateGame);
	}

	private static void CreateGame()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("[GameLoop]");
		Object.DontDestroyOnLoad((Object)val);
		val.AddComponent<HitFeelRunner>();
		val.AddComponent<GameLoop>();
		val.AddComponent<InputBridge>();
		val.AddComponent<BotPreload>();
		val.AddComponent<BotAppearancePrebake>();
	}

	private void Awake()
	{
		if ((Object)(object)I != (Object)null && (Object)(object)I != (Object)(object)this)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
		else
		{
			I = this;
		}
	}

	private void Start()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected Obj, but got Unknown
		Profile.Load();
		BuildBackground();
		GameObject val = GameObject.Find("Main Camera");
		if ((Object)(object)val != (Object)null && (Object)(object)val.GetComponent<CameraRig>() == (Object)null)
		{
			val.SetActive(false);
		}
		GameObject val2 = new GameObject("MainCamera");
		val2.tag = "MainCamera";
		val2.AddComponent<Camera>().orthographic = true;
		camRig = val2.AddComponent<CameraRig>();
		if (!GameSession.InGame)
		{
			state = GameState.Login;
			((Component)this).gameObject.AddComponent<LoginFlow>();
		}
		else
		{
			StartGame();
		}
	}

	public void EnterGameFromLogin()
	{
		if ((Object)(object)player != (Object)null)
		{
			state = GameState.Playing;
		}
		else
		{
			StartGame();
		}
	}

	public void StartGame()
	{
		if ((Object)(object)player != (Object)null)
		{
			state = GameState.Playing;
			return;
		}
		state = GameState.Playing;
		gameOver = false;
		killCount = 0;
		elapsed = 0f;
		deaths.Clear();
		NativeSpecialCore.Sync();
		BuildScene();
		FieldCatalog.Ensure();
		if (FieldCatalog.Ready)
		{
			field = ((Component)this).gameObject.AddComponent<FieldDirector>();
			int num = Profile.LastMapId;
			if (PlayerPrefs.HasKey("debug_field_map"))
			{
				num = PlayerPrefs.GetInt("debug_field_map");
				PlayerPrefs.DeleteKey("debug_field_map");
			}
			if (!FieldCatalog.Maps.ContainsKey(num))
			{
				num = 1000000;
			}
			field.Begin(num);
			Debug.Log((object)("[GameLoop] 原版地图模式：" + FieldCatalog.Maps.Count + " 张原始地图"));
			if (PlayerPrefs.GetInt("debug_god", 0) == 1)
			{
				PlayerPrefs.DeleteKey("debug_god");
				if ((Object)(object)player != (Object)null)
				{
					player.hpMax = 99999f;
					player.hp = 99999f;
				}
				Debug.Log((object)"[GameLoop] 调试无敌已开启");
			}
			VirtualControls.OnEnterGame();
			return;
		}
		StageChapter stageChapter = StageChapter.Load("Stage/chapter1");
		if (stageChapter != null && stageChapter.levels.Count > 0)
		{
			int num2 = 0;
			if (PlayerPrefs.HasKey("debug_start_level"))
			{
				num2 = Mathf.Clamp(PlayerPrefs.GetInt("debug_start_level"), 0, stageChapter.levels.Count - 1);
				PlayerPrefs.DeleteKey("debug_start_level");
			}
			stage = ((Component)this).gameObject.AddComponent<StageDirector>();
			stage.Begin(stageChapter, num2);
			Debug.Log((object)("[GameLoop] 关卡模式：" + stageChapter.levels.Count + " 关，Boss " + stageChapter.bosses.Count + " 个，从第 " + (num2 + 1) + " 关开始"));
			if (PlayerPrefs.GetInt("debug_god", 0) == 1)
			{
				PlayerPrefs.DeleteKey("debug_god");
				if ((Object)(object)player != (Object)null)
				{
					player.hpMax = 99999f;
					player.hp = 99999f;
				}
				Debug.Log((object)"[GameLoop] 调试无敌已开启");
			}
		}
		else
		{
			Debug.Log((object)"[GameLoop] 无关卡数据，进入无限刷怪模式");
		}
		VirtualControls.OnEnterGame();
	}

	public void TeardownBattle()
	{
		gameOver = false;
		if ((Object)(object)field != (Object)null)
		{
			field.Shutdown();
			Object.Destroy((Object)(object)field);
			field = null;
		}
		if ((Object)(object)stage != (Object)null)
		{
			stage.StopSession();
			Object.Destroy((Object)(object)stage);
			stage = null;
		}
		ClearBattleEntities();
		if ((Object)(object)player != (Object)null)
		{
			((Component)player).GetComponent<SkillSystem>()?.CancelActions();
			((Component)player).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)player).gameObject);
			player = null;
		}
		if ((Object)(object)hudGo != (Object)null)
		{
			Object.Destroy((Object)(object)hudGo);
			hudGo = null;
		}
		deaths.Clear();
		camRig.target = null;
		camRig?.SnapTo(0f);
		HitFeel.Reset();
		Profile.Save();
	}

	public void ExitToCharSelect()
	{
		if ((Object)(object)DeathFlow.I != (Object)null)
		{
			DeathFlow.I.ForceClear();
		}
		GameSession.SaveToDb();
		GameSession.DeselectChar();
		TeardownBattle();
		state = GameState.Login;
		(((Object)(object)LoginFlow.I != (Object)null) ? LoginFlow.I : ((Component)this).gameObject.AddComponent<LoginFlow>()).BackToCharSelect();
	}

	public void RequestExitToLogin()
	{
		exitToLoginPending = true;
	}

	public void ExitToLogin()
	{
		if ((Object)(object)DeathFlow.I != (Object)null)
		{
			DeathFlow.I.ForceClear();
		}
		BottomMenu.Close();
		BottomBar.ResetChat();
		GameSession.Logout();
		TeardownBattle();
		state = GameState.Login;
		(((Object)(object)LoginFlow.I != (Object)null) ? LoginFlow.I : ((Component)this).gameObject.AddComponent<LoginFlow>()).BackToLogin("");
	}

	private void ClearBattleEntities()
	{
		NativeSummon[] array = Object.FindObjectsOfType<NativeSummon>();
		foreach (NativeSummon nativeSummon in array)
		{
			if ((Object)(object)nativeSummon != (Object)null)
			{
				((Component)nativeSummon).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)nativeSummon).gameObject);
			}
		}
		EnemyUnit[] array2 = Object.FindObjectsOfType<EnemyUnit>();
		foreach (EnemyUnit enemyUnit in array2)
		{
			if ((Object)(object)enemyUnit != (Object)null)
			{
				((Component)enemyUnit).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)enemyUnit).gameObject);
			}
		}
		EnemyUnit.All.Clear();
		BossUnit[] array3 = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array3)
		{
			if ((Object)(object)bossUnit != (Object)null)
			{
				((Component)bossUnit).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)bossUnit).gameObject);
			}
		}
		GroundItem.Clear();
		SkillFx[] array4 = Object.FindObjectsOfType<SkillFx>();
		foreach (SkillFx skillFx in array4)
		{
			if ((Object)(object)skillFx != (Object)null)
			{
				((Component)skillFx).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)skillFx).gameObject);
			}
		}
		HitFeel.Reset();
	}

	private void BuildScene()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected Obj, but got Unknown
		GameObject val = new GameObject("Player");
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		val2.sprite = SpriteFactory.Bottom();
		val2.color = new Color(0.98f, 0.76f, 0.38f);
		((Renderer)val2).sortingOrder = 300;
		val.transform.localScale = Vector3.one;
		player = val.AddComponent<PlayerCtrl>();
		val.AddComponent<SkillSystem>().BindCooldowns(playerCooldowns);
		camRig.target = val.transform;
		camRig.SnapTo(0f);
		hudGo = new GameObject("HUD");
		hudGo.AddComponent<GameHUD>();
		spawnTimer = 1.2f;
		eliteTimer = 14f;
	}

	private void BuildBackground()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		Art.Ensure();
		GameObject val = new GameObject("Backdrop");
		backdrop = val.AddComponent<Backdrop>();
		backdrop.Build();
		if (Art.Ready)
		{
			return;
		}
		SpriteRenderer val2 = SpriteFactory.Make("GroundEdge", null, new Vector2(60f, 0.06f), new Color(0.42f, 0.62f, 0.5f, 0.75f), 1, bottomPivot: false);
		((Component)val2).transform.position = new Vector3(0f, -1.38f, 0f);
		edgeSr = val2;
		SpriteRenderer val3 = SpriteFactory.Make("Sky", null, new Vector2(46f, 18f), new Color(0.11f, 0.13f, 0.18f), -300, bottomPivot: false);
		((Component)val3).transform.position = new Vector3(0f, 2.5f, 0f);
		skySr = val3;
		float[] array = new float[3] { 4.2f, 3f, 2.2f };
		Color[] array2 = new Color[3]
		{
			new Color(0.16f, 0.19f, 0.25f),
			new Color(0.19f, 0.23f, 0.29f),
			new Color(0.22f, 0.27f, 0.33f)
		};
		for (int i = 0; i < 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				SpriteRenderer val4 = SpriteFactory.Make("Far" + i + "_" + j, null, new Vector2(7.5f + (float)i * 1.4f, array[i]), array2[i], -250 + i * 10 + ((j > 0) ? 1 : 0));
				((Component)val4).transform.position = new Vector3((float)j * 7.2f + (float)i * 1.7f, -1.4f, 0f);
				farSr.Add(val4);
			}
		}
		SpriteRenderer val5 = SpriteFactory.Make("Ground", null, new Vector2(46f, 1.5f), new Color(0.17f, 0.19f, 0.23f), -5, bottomPivot: false);
		((Component)val5).transform.position = new Vector3(0f, -2.15f, 0f);
		groundSr = val5;
		for (int k = -14; k <= 14; k++)
		{
			SpriteRenderer val6 = SpriteFactory.Make("Tick" + k, null, new Vector2(0.1f, 0.3f), new Color(0.28f, 0.32f, 0.38f), 2, bottomPivot: false);
			((Component)val6).transform.position = new Vector3((float)k * 1.6f, -1.95f, 0f);
			tickSr.Add(val6);
		}
	}

	public void ApplyTheme(StageTheme t)
	{
		ApplyTheme(t, themeKey);
	}

	public void ApplyTheme(StageTheme t, string key)
	{
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		if (t == null)
		{
			return;
		}
		PlayerCtrl playerCtrl = player;
		if (playerCtrl != null)
		{
			((Component)playerCtrl).GetComponent<SkillSystem>()?.CancelActions();
		}
		GroundItem.Clear();
		if (!string.IsNullOrEmpty(key))
		{
			themeKey = key;
		}
		if ((Object)(object)backdrop != (Object)null)
		{
			backdrop.Apply(themeKey, t);
		}
		if ((Object)(object)backdrop != (Object)null && backdrop.HasStage)
		{
			Art.StageInfo stageInfo = Art.Stage(themeKey);
			PlayerCtrl.SetBounds(backdrop.WalkMin, backdrop.WalkMax);
			PlayerCtrl.SetTerrain(stageInfo?.Footholds, stageInfo?.ForbidFallDown, stageInfo?.FootholdForces, stageInfo?.FootholdDrag);
			PlayerCtrl.Climbs = stageInfo?.Climbs;
			if ((Object)(object)camRig != (Object)null && stageInfo != null)
			{
				camRig.SetStage(stageInfo);
			}
		}
		else
		{
			PlayerCtrl.SetBounds(-13f, 13f);
			PlayerCtrl.SetTerrain(null);
			PlayerCtrl.Climbs = null;
			if ((Object)(object)camRig != (Object)null)
			{
				camRig.ClearStage();
			}
		}
		if ((Object)(object)player != (Object)null)
		{
			player.worldX = PlayerCtrl.ClampX(player.worldX);
			player.ResetTerrain();
		}
		if ((Object)(object)edgeSr != (Object)null)
		{
			edgeSr.color = t.accent;
		}
		if ((Object)(object)skySr != (Object)null)
		{
			skySr.color = t.bgTop;
		}
		if ((Object)(object)groundSr != (Object)null)
		{
			groundSr.color = t.ground;
		}
		for (int i = 0; i < farSr.Count; i++)
		{
			float num = 0.78f + 0.11f * (float)(i % 7);
			farSr[i].color = Color.Lerp(t.bgBottom, t.mid, num);
		}
		foreach (SpriteRenderer item in pillarSr)
		{
			if ((Object)(object)item != (Object)null)
			{
				item.color = t.groundDeep;
			}
		}
		foreach (SpriteRenderer item2 in tickSr)
		{
			if ((Object)(object)item2 != (Object)null)
			{
				item2.color = Color.Lerp(t.groundDeep, t.accent, 0.25f);
			}
		}
	}

	private void Update()
	{
		using (BotPerf.Timer("GameLoop.Update"))
		{
			NativeHuntRuntime.Tick();
			NativeBotBossCombat.Tick(NativeMobController.Now);
			NativeDojoService.Tick();
			if (exitToLoginPending)
			{
				exitToLoginPending = false;
				ExitToLogin();
			}
			else
			{
				if (state != GameState.Playing)
				{
					return;
				}
				if (gameOver)
				{
					if (!((Object)(object)DeathFlow.I != (Object)null) || !DeathFlow.I.Active)
					{
						respawnTimer -= Time.unscaledDeltaTime;
						if (respawnTimer <= 0f)
						{
							Restart();
						}
					}
					return;
				}
				float deltaTime = Time.deltaTime;
				if (deltaTime <= 0f)
				{
					return;
				}
				elapsed += deltaTime;
				if (!((Object)(object)field != (Object)null) && !((Object)(object)stage != (Object)null))
				{
					spawnTimer -= deltaTime;
					if (spawnTimer <= 0f && EnemyUnit.All.Count < 11)
					{
						spawnTimer = Mathf.Max(0.75f, 1.7f - elapsed * 0.004f);
						Spawn(elite: false);
					}
					eliteTimer -= deltaTime;
					if (eliteTimer <= 0f)
					{
						eliteTimer = Mathf.Max(9f, 22f - elapsed * 0.05f);
						Spawn(elite: true);
					}
				}
			}
		}
	}

	private void Spawn(bool elite)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(elite ? "Elite" : "Mob");
		val.AddComponent<SpriteRenderer>().sprite = SpriteFactory.Bottom();
		float num = (elite ? 1.15f : 1f);
		val.transform.localScale = new Vector3(num, num, 1f);
		EnemyUnit enemyUnit = val.AddComponent<EnemyUnit>();
		enemyUnit.hpMax = (long)(elite ? (150f + elapsed * 2.5f) : (42f + elapsed * 1.6f));
		enemyUnit.damage = (elite ? 16f : 8f);
		enemyUnit.speed = (elite ? 1.35f : Random.Range(1.6f, 2.4f));
		enemyUnit.elite = elite;
		enemyUnit.label = (elite ? "精英" : "杂兵");
		enemyUnit.Init(Random.Range(0, 5), player.worldX + 11f);
	}

	public EnemyUnit SpawnMob(string type, float x, int lane = -1)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("Mob_" + type);
		val.AddComponent<SpriteRenderer>().sprite = SpriteFactory.Bottom();
		EnemyUnit enemyUnit = val.AddComponent<EnemyUnit>();
		Color tint = new Color(0.62f, 0.36f, 0.72f);
		float num = 1f;
		string artKey = "";
		switch (type)
		{
		case "slime":
			enemyUnit.hpMax = 30L;
			enemyUnit.damage = 6f;
			enemyUnit.speed = 2.3f;
			tint = new Color(0.42f, 0.8f, 0.52f);
			artKey = "slime";
			break;
		case "mushroom":
			enemyUnit.hpMax = 44L;
			enemyUnit.damage = 9f;
			enemyUnit.speed = 1.85f;
			tint = new Color(0.85f, 0.42f, 0.45f);
			artKey = "mushroom";
			break;
		case "greenmush":
			enemyUnit.hpMax = 52L;
			enemyUnit.damage = 10f;
			enemyUnit.speed = 1.75f;
			artKey = "greenmush";
			break;
		case "bat":
			enemyUnit.hpMax = 34L;
			enemyUnit.damage = 7f;
			enemyUnit.speed = 2.7f;
			tint = new Color(0.55f, 0.5f, 0.78f);
			break;
		case "crab":
			enemyUnit.hpMax = 60L;
			enemyUnit.damage = 11f;
			enemyUnit.speed = 1.5f;
			tint = new Color(0.92f, 0.55f, 0.35f);
			break;
		case "pig":
			enemyUnit.hpMax = 52L;
			enemyUnit.damage = 10f;
			enemyUnit.speed = 1.75f;
			tint = new Color(0.95f, 0.72f, 0.68f);
			artKey = "pig";
			break;
		case "snail":
			enemyUnit.hpMax = 26L;
			enemyUnit.damage = 5f;
			enemyUnit.speed = 1.3f;
			artKey = "snail";
			break;
		default:
			enemyUnit.hpMax = 42L;
			enemyUnit.damage = 8f;
			enemyUnit.speed = 2f;
			break;
		}
		enemyUnit.tint = tint;
		enemyUnit.artKey = artKey;
		enemyUnit.label = type;
		val.transform.localScale = new Vector3(num, num, 1f);
		enemyUnit.Init((lane < 0) ? Random.Range(0, 5) : lane, ClampSpawnX(x));
		return enemyUnit;
	}

	public float ClampSpawnX(float x)
	{
		float num = 1.2f;
		return Mathf.Clamp(x, PlayerCtrl.XMin + num, PlayerCtrl.XMax - num);
	}

	public void OnEnemyKilled(EnemyUnit e)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (state == GameState.Playing && !gameOver && !((Object)(object)e == (Object)null) && e.dead && deaths.TrySettle(((Object)e).GetInstanceID()))
		{
			killCount++;
			NativeSpecialCore.OnKill(player);
			if ((Object)(object)field != (Object)null)
			{
				field.OnEnemyKilled(e);
			}
			else
			{
				BattleRewards.Drop(e.artKey, ((Component)e).transform.position);
			}
		}
	}

	public void OnBossKilled(BossUnit boss)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (state == GameState.Playing && !gameOver && !((Object)(object)boss == (Object)null) && boss.dead && deaths.TrySettle(((Object)boss).GetInstanceID()))
		{
			NativeSpecialCore.OnKill(player);
			BattleRewards.Drop((boss.def.type == "slime") ? "slimeking" : "mushmom", ((Component)boss).transform.position);
		}
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			GameSession.SaveToDb();
		}
	}

	private void OnApplicationQuit()
	{
		GameSession.SaveToDb();
		GameServer.Stop();
	}

	public void OnPlayerDead()
	{
		if (!gameOver)
		{
			gameOver = true;
			NativeSpecialCore.OnPlayerDeath(player);
			PlayerCtrl playerCtrl = player;
			if (playerCtrl != null)
			{
				((Component)playerCtrl).GetComponent<NativeBuffs>()?.Clear();
			}
			stage?.StopSession();
			PlayerCtrl playerCtrl2 = player;
			if (playerCtrl2 != null)
			{
				((Component)playerCtrl2).GetComponent<SkillSystem>()?.CancelActions();
			}
			HitFeel.Reset();
			DeathFlow deathFlow = (((Object)(object)DeathFlow.I != (Object)null) ? DeathFlow.I : ((Component)this).gameObject.AddComponent<DeathFlow>());
			if ((Object)(object)field != (Object)null)
			{
				deathFlow.Begin(player);
			}
			else
			{
				respawnTimer = 1.6f;
			}
		}
	}

	private void Restart()
	{
		gameOver = false;
		killCount = 0;
		elapsed = 0f;
		spawnTimer = 1.2f;
		eliteTimer = 14f;
		ClearBattleEntities();
		deaths.Clear();
		if ((Object)(object)field != (Object)null)
		{
			if ((Object)(object)player != (Object)null)
			{
				player.ResetCombatState();
				player.hp = player.hpMax;
				player.invulnUntil = Time.unscaledTime + 1f;
			}
			field.RespawnPlayer();
			return;
		}
		if ((Object)(object)player != (Object)null)
		{
			player.ResetCombatState();
			player.worldX = 0f;
			player.ResetTerrain();
			player.lane = 2;
			player.invulnUntil = Time.unscaledTime + 1f;
		}
		if ((Object)(object)camRig != (Object)null)
		{
			camRig.SnapTo(0f);
		}
		if ((Object)(object)stage != (Object)null)
		{
			stage.RestartLevel();
		}
	}
}
