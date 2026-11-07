using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillSystem : MonoBehaviour
{
	private sealed class FieldZone
	{
		public int State;

		public int Parent;

		public int Body;

		public int DurationSec;

		public int IntervalMs;

		public int MobCount;

		public int Projectiles;

		public int MaxActivations;

		public string Trigger;

		public string TargetMode;
	}

	private enum ProcMode
	{
		Roll,
		Charge,
		SummonCharge,
		BuffRoll,
		MarkPerTarget
	}

	private sealed class ProcSpec
	{
		public readonly int Trigger;

		public readonly int Body;

		public readonly ProcMode Mode;

		public readonly string ChargeField;

		public readonly string CapField;

		public readonly string PropField;

		public readonly int BrandBody;

		public readonly bool Basic;

		public readonly int AltState;

		public readonly int AltBody;

		public readonly string AltPropField;

		public readonly string ScaleField;

		public readonly string CooldownField;

		public readonly int CooldownUnitMs;

		public ProcSpec(int trigger, ProcMode mode, int body = 0, string chargeField = "x", string capField = "y", string propField = "prop", int brandBody = 0, bool basic = false, string scaleField = "", int altState = 0, int altBody = 0, string altPropField = "", string cooldownField = "cooltimeMS", int cooldownUnitMs = 1)
		{
			Trigger = trigger;
			Body = ((body == 0) ? trigger : body);
			Mode = mode;
			ChargeField = chargeField;
			CapField = capField;
			PropField = propField;
			BrandBody = brandBody;
			Basic = basic;
			ScaleField = scaleField;
			AltState = altState;
			AltBody = altBody;
			AltPropField = altPropField;
			CooldownField = cooldownField;
			CooldownUnitMs = cooldownUnitMs;
		}
	}

	private sealed class AddAttackLink
	{
		public int Next;

		public float Delay;
	}

	private sealed class CurseMark
	{
		public double startedAt;

		public double until;

		public int stacks;
	}

	private sealed class CurseSpec
	{
		public int Parent;

		public int State;

		public int Body;

		public Dictionary<string, object> Row;
	}

	private sealed class DotSpec
	{
		public int Max;

		public List<float[]> Levels = new List<float[]>();
	}

	private sealed class DotMark
	{
		public int Skill;

		public double Next;

		public double Until;

		public float DotPercent;

		public float Interval;

		public float Seconds;

		public double FromPercent;

		public Vector2 Origin;

		public int Facing;
	}

	private sealed class CapturedReplayHit
	{
		public Component Target;

		public DamageResult[] Hits;
	}

	private enum SeqTrigger
	{
		Recast,
		Basic,
		Chain
	}

	private sealed class SkillSequence
	{
		public int Parent;

		public int Child;

		public int ChildMax;

		public int DurationSec;

		public int Charges;

		public int Repeat;

		public int ChildMp;

		public SeqTrigger Trigger;

		public string ChildName;

		public int[] AttackTimes;

		public bool Magic;

		public bool ParentAttack;

		public int SkillType;

		public int AreaAttack;

		public Dictionary<string, object> Levels;
	}

	private sealed class RiftZone
	{
		public int Parent;

		public int Child;

		public Dictionary<string, object> Levels;
	}

	private sealed class WoundSpec
	{
		public int Parent;

		public int MaxStacks;

		public int DurationSec;

		public int[] Sources;
	}

	private sealed class Rift
	{
		public Vector2 Position;

		public int Stages;

		public double ExpireAt;

		public double SpawnedAt;
	}

	private sealed class TrembleWave
	{
		public float Force;

		public float Seconds;

		public float Delay;
	}

	private sealed class TrembleSpec
	{
		public readonly List<TrembleWave> Waves = new List<TrembleWave>();
	}

	private static Dictionary<string, object> fieldZoneData;

	private static List<FieldZone> fieldZones;

	private readonly Dictionary<int, long> fieldZoneGeneration = new Dictionary<int, long>();

	private readonly Dictionary<int, double> fieldZoneUntil = new Dictionary<int, double>();

	private static readonly ProcSpec[] procSpecs = new ProcSpec[24]
	{
		new ProcSpec(1140009, ProcMode.Roll),
		new ProcSpec(1240008, ProcMode.Roll),
		new ProcSpec(1340007, ProcMode.Roll),
		new ProcSpec(3240014, ProcMode.Roll),
		new ProcSpec(3100001, ProcMode.Roll, 0, "x", "y", "prop", 0, basic: true),
		new ProcSpec(3200001, ProcMode.Roll, 0, "x", "y", "prop", 0, basic: true),
		new ProcSpec(1340004, ProcMode.Roll, 1341005, "x", "y", "prop", 1341006),
		new ProcSpec(2341009, ProcMode.Roll, 2341010),
		new ProcSpec(2141012, ProcMode.Roll, 2140013),
		new ProcSpec(2241003, ProcMode.Roll, 2241004),
		new ProcSpec(5141009, ProcMode.Roll, 5140010, "x", "y", "x"),
		new ProcSpec(500001016, ProcMode.Roll),
		new ProcSpec(500001020, ProcMode.Roll),
		new ProcSpec(5241009, ProcMode.Roll, 0, "x", "y", "prop", 0, basic: false, "", 0, 0, "", "dummyStr4", 1000),
		new ProcSpec(12140015, ProcMode.Charge, 12141016),
		new ProcSpec(14141019, ProcMode.Charge),
		new ProcSpec(14141004, ProcMode.SummonCharge, 0, "z"),
		new ProcSpec(14141012, ProcMode.SummonCharge, 0, "z"),
		new ProcSpec(13141003, ProcMode.BuffRoll),
		new ProcSpec(400041008, ProcMode.BuffRoll),
		new ProcSpec(400041028, ProcMode.BuffRoll, 0, "x", "y", "prop", 0, basic: false, "x"),
		new ProcSpec(2141007, ProcMode.Roll),
		new ProcSpec(4240016, ProcMode.Roll, 4241018, "x", "y", "y", 0, basic: false, "", 4240017, 4241019, "s"),
		new ProcSpec(4141003, ProcMode.MarkPerTarget, 4141004)
	};

	private readonly Dictionary<int, int> procCounter = new Dictionary<int, int>();

	private readonly Dictionary<int, int> procBrands = new Dictionary<int, int>();

	private readonly Dictionary<int, int> procBrandTicks = new Dictionary<int, int>();

	private readonly Dictionary<int, double> procNext = new Dictionary<int, double>();

	private readonly Dictionary<int, double> procUntil = new Dictionary<int, double>();

	private readonly Dictionary<int, double> chainNext = new Dictionary<int, double>();

	private readonly Dictionary<int, int> chainUses = new Dictionary<int, int>();

	private readonly Dictionary<int, bool> chainWasActive = new Dictionary<int, bool>();

	private readonly Dictionary<Component, double> assassinMarks = new Dictionary<Component, double>();

	private static Dictionary<int, AddAttackLink> addAttacks;

	private static bool addAttacksLoaded;

	private const float DefaultFollowupDelay = 0.3f;

	private static Dictionary<string, object> curseData;

	private static Dictionary<int, bool> curseParents;

	private readonly Dictionary<Component, CurseMark> curseMarks = new Dictionary<Component, CurseMark>();

	private static readonly Dictionary<int, CurseSpec> curseSpecs = new Dictionary<int, CurseSpec>();

	private double curseActiveUntil;

	private double iceNext;

	private int curseLevel;

	private long curseGeneration;

	private CurseSpec curseActive;

	private static Dictionary<int, DotSpec> dotSpecs;

	private static bool dotsLoaded;

	private readonly Dictionary<Component, Dictionary<int, int>> dotTokens = new Dictionary<Component, Dictionary<int, int>>();

	private int dotSerial;

	private static Dictionary<int, Dictionary<string, object>> finisherTable;

	private static Dictionary<string, object> followupData;

	private bool fourSeasonsFrenzy;

	private int dualBladeHits;

	private double arrowRainExpires;

	private double arrowRainNext;

	private readonly Dictionary<Component, long> windDots = new Dictionary<Component, long>();

	private long windGeneration;

	private readonly List<NativeImgAnimation> nativeReplayEffects = new List<NativeImgAnimation>();

	private static readonly HashSet<int> RepeatAttackSkills = new HashSet<int> { 400011052 };

	private static readonly Dictionary<int, int> RepeatAttackBodies = new Dictionary<int, int> { { 400011052, 400011053 } };

	private readonly Dictionary<int, int> mechResources = new Dictionary<int, int>();

	private readonly Dictionary<int, double> mechUntil = new Dictionary<int, double>();

	private readonly Dictionary<int, double> mechNextPassive = new Dictionary<int, double>();

	private readonly Dictionary<int, int> mechBursts = new Dictionary<int, int>();

	private double darkSightBlockedUntil;

	private int darkSightEvades;

	private bool darkSightWasActive;

	private bool mechReplaceShock;

	private int mechReplaceLevel;

	private readonly NativeLightningSpearState spearCombo = new NativeLightningSpearState();

	private static Dictionary<string, object> sequenceData;

	private static List<SkillSequence> sequences;

	private static List<WoundSpec> wounds;

	private static readonly Dictionary<int, RiftZone> riftZones = new Dictionary<int, RiftZone>();

	private readonly List<Rift> rifts = new List<Rift>();

	private Coroutine riftTender;

	private double lastRiftSpawn;

	private readonly Dictionary<int, double> formUntil = new Dictionary<int, double>();

	private readonly Dictionary<int, int> formCharges = new Dictionary<int, int>();

	private Component woundTarget;

	private int woundStacks;

	private double woundUntil;

	private static Dictionary<string, object> stackData;

	private static Dictionary<int, Dictionary<string, object>> stackSpecs;

	private int stackCount;

	private int stackLevel;

	private int stackCooldownReduce;

	private int stackExhaustCooldown;

	private double stackUntil;

	private long stackGeneration;

	private bool stackExhaust;

	private static Dictionary<int, TrembleSpec> trembles;

	private static bool tremblesLoaded;

	private GameAction? boundHeldAction;

	private GameAction heldAction;

	private SkillDef heldSkill;

	private long lastHeldShot;

	private Coroutine heldPreparation;

	private readonly List<SkillFx> heldVisuals = new List<SkillFx>();

	private bool heldCharging;

	private bool heldReady;

	private bool heldFired;

	private readonly List<SkillDef> definitions = new List<SkillDef>();

	private Dictionary<int, SkillRuntimeState> states = new Dictionary<int, SkillRuntimeState>();

	private PlayerCtrl p;

	private float actionUntil;

	private bool actionBlocking;

	private int actionSkillId;

	private readonly Dictionary<int, NativeSummon> summons = new Dictionary<int, NativeSummon>();

	private string notice;

	private float noticeUntil;

	private readonly List<SkillFx> ownedVisuals = new List<SkillFx>();

	private static bool fxDebugChecked;

	private static bool fxDebugValue;

	private int impactVariant;

	private int finishVariant;

	private int comboCount;

	private float comboUntil;

	private SkillFx comboFx;

	private SkillFx comboFx2;

	private const float ProjectileSpeed = 16.666668f;

	private int ballVariant;

	private int hammerVariant;

	private static List<FieldZone> Zones
	{
		get
		{
			if (fieldZones != null)
			{
				return fieldZones;
			}
			fieldZones = new List<FieldZone>();
			if (fieldZoneData == null)
			{
				TextAsset val = Resources.Load<TextAsset>("NativeFieldZones");
				string text = ((val != null) ? val.text : null);
				fieldZoneData = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
			}
			if (!fieldZoneData.TryGetValue("zones", out var value))
			{
				return fieldZones;
			}
			if (!(value is Dictionary<string, object> dictionary))
			{
				return fieldZones;
			}
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (item.Value is Dictionary<string, object> dictionary2)
				{
					List<object> list = (dictionary2.TryGetValue("bodies", out var value2) ? (value2 as List<object>) : null);
					if (list != null && list.Count != 0)
					{
						fieldZones.Add(new FieldZone
						{
							State = ZoneInt(dictionary2, "state"),
							Parent = ZoneInt(dictionary2, "parent"),
							Body = Convert.ToInt32(list[0]),
							DurationSec = ZoneInt(dictionary2, "durationSec"),
							IntervalMs = ZoneInt(dictionary2, "intervalMs"),
							MobCount = ZoneInt(dictionary2, "mobCount", 1),
							Projectiles = ZoneInt(dictionary2, "projectiles", 1),
							MaxActivations = ZoneInt(dictionary2, "maxActivations"),
							Trigger = ZoneStr(dictionary2, "trigger"),
							TargetMode = ZoneStr(dictionary2, "targetMode")
						});
					}
				}
			}
			return fieldZones;
		}
	}

	private static Dictionary<int, AddAttackLink> AddAttacks
	{
		get
		{
			if (addAttacksLoaded)
			{
				return addAttacks;
			}
			addAttacksLoaded = true;
			addAttacks = new Dictionary<int, AddAttackLink>();
			TextAsset val = Resources.Load<TextAsset>("NativeSkillAddAttacks");
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogWarning((object)"[AddAttack] 缺 Assets/Resources/NativeSkillAddAttacks.json，追加攻击不会补");
				return addAttacks;
			}
			if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("skills", out var value) || !(value is Dictionary<string, object> dictionary2))
			{
				return addAttacks;
			}
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (long.TryParse(item.Key, out var result) && result > 0 && result <= int.MaxValue && item.Value is Dictionary<string, object> dictionary3 && dictionary3.TryGetValue("next", out var value2))
				{
					int num = Convert.ToInt32(value2);
					if (num > 0 && num != result)
					{
						addAttacks[(int)result] = new AddAttackLink
						{
							Next = num,
							Delay = Art.Num(dictionary3, "delay") / 1000f
						};
					}
				}
			}
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[AddAttack] 载入 " + addAttacks.Count + " 条自动追加链"));
			}
			return addAttacks;
		}
	}

	public static int AddAttackCount => AddAttacks.Count;

	private static Dictionary<int, DotSpec> Dots
	{
		get
		{
			if (dotsLoaded)
			{
				return dotSpecs;
			}
			dotsLoaded = true;
			dotSpecs = new Dictionary<int, DotSpec>();
			TextAsset val = Resources.Load<TextAsset>("NativeSkillDots");
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogWarning((object)"[Dot] 缺 Assets/Resources/NativeSkillDots.json，持续伤害不会结算");
				return dotSpecs;
			}
			if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("skills", out var value) || !(value is Dictionary<string, object> dictionary2))
			{
				return dotSpecs;
			}
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (!long.TryParse(item.Key, out var result) || result <= 0 || result > int.MaxValue || !(item.Value is Dictionary<string, object> dictionary3) || !dictionary3.TryGetValue("lv", out var value2) || !(value2 is List<object> list))
				{
					continue;
				}
				DotSpec dotSpec = new DotSpec
				{
					Max = Mathf.Max(1, Art.Int(dictionary3, "max", list.Count))
				};
				foreach (object item2 in list)
				{
					if (item2 is List<object> { Count: >=3 } list2)
					{
						dotSpec.Levels.Add(new float[3]
						{
							Convert.ToSingle(list2[0]),
							Convert.ToSingle(list2[1]),
							Convert.ToSingle(list2[2])
						});
					}
					else
					{
						dotSpec.Levels.Add(null);
					}
				}
				if (dotSpec.Levels.Count != 0)
				{
					dotSpecs[(int)result] = dotSpec;
				}
			}
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Dot] 载入 " + dotSpecs.Count + " 条持续伤害技能"));
			}
			return dotSpecs;
		}
	}

	public static int DotSkillCount => Dots.Count;

	private static Dictionary<string, object> FollowupData => followupData ?? (followupData = (Dictionary<string, object>)MiniJson.Parse(Resources.Load<TextAsset>("NativeSkillFollowups").text));

	public int CooldownReducePercent
	{
		get
		{
			if (stackCount <= 0 || !(Time.realtimeSinceStartupAsDouble < stackUntil))
			{
				return 0;
			}
			return stackCooldownReduce;
		}
	}

	private static Dictionary<int, TrembleSpec> Trembles
	{
		get
		{
			if (tremblesLoaded)
			{
				return trembles;
			}
			tremblesLoaded = true;
			TextAsset val = Resources.Load<TextAsset>("NativeSkillTremble");
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogWarning((object)"[Tremble] 缺 Assets/Resources/NativeSkillTremble.json，技能震屏不会播");
				return trembles = new Dictionary<int, TrembleSpec>();
			}
			if (!(MiniJson.Parse(val.text) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("skills", out var value) || !(value is Dictionary<string, object> dictionary2))
			{
				return trembles = new Dictionary<int, TrembleSpec>();
			}
			trembles = new Dictionary<int, TrembleSpec>();
			foreach (KeyValuePair<string, object> item in dictionary2)
			{
				if (!long.TryParse(item.Key, out var result) || result <= 0 || result > int.MaxValue || !(item.Value is Dictionary<string, object> dictionary3) || !dictionary3.TryGetValue("waves", out var value2) || !(value2 is List<object> list))
				{
					continue;
				}
				TrembleSpec trembleSpec = new TrembleSpec();
				foreach (object item2 in list)
				{
					if (item2 is Dictionary<string, object> d)
					{
						float num = Art.Num(d, "force");
						float num2 = Mathf.Max(1f, Art.Num(d, "time"));
						if (!(num <= 0f))
						{
							trembleSpec.Waves.Add(new TrembleWave
							{
								Force = num,
								Seconds = num2 / 1000f,
								Delay = Mathf.Max(0f, Art.Num(d, "delay") / 1000f)
							});
						}
					}
				}
				if (trembleSpec.Waves.Count > 0)
				{
					trembles[(int)result] = trembleSpec;
				}
			}
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Tremble] 载入 " + trembles.Count + " 条技能震屏"));
			}
			return trembles;
		}
	}

	public static int TrembleCount => Trembles.Count;

	public int ContinuousSkillId => heldSkill?.native.Id ?? 0;

	public IReadOnlyList<SkillDef> skills => definitions.AsReadOnly();

	public string LastFailure { get; private set; }

	public string ActiveNotice
	{
		get
		{
			if (!(Time.unscaledTime < noticeUntil))
			{
				return null;
			}
			return notice;
		}
	}

	private static bool FxDebug
	{
		get
		{
			if (!fxDebugChecked)
			{
				fxDebugChecked = true;
				fxDebugValue = PlayerPrefs.GetInt("debug_hitlog", 0) != 0;
			}
			return fxDebugValue;
		}
	}

	public static event Action<SkillDef, Component> SpecialCoreTriggered;

	private static int ZoneInt(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static string ZoneStr(Dictionary<string, object> row, string key)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return "";
		}
		return Convert.ToString(value);
	}

	private void StartFieldZones(SkillDef cast, Vector2 origin, int facing)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (cast == null || cast.native == null)
		{
			return;
		}
		foreach (FieldZone zone in Zones)
		{
			if (zone.Parent == cast.native.Id && zone.Trigger == "onCast")
			{
				BeginFieldZone(zone, cast.native.Level, origin, facing);
			}
		}
	}

	private void TriggerFieldZonesOnHit(SkillDef cast, Vector2 origin, int facing)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)p == (Object)null || cast == null || cast.native == null || cast.native.Kind != "Attack")
		{
			return;
		}
		NativeSkillStates nativeSkillStates = NativeSkillStates.For(p);
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		foreach (FieldZone zone in Zones)
		{
			if ((!(zone.Trigger != "onHit") || !(zone.Trigger != "onSkillHitWhileState")) && nativeSkillStates.Has(zone.State) && (!(zone.Trigger == "onSkillHitWhileState") || cast.native.Id != zone.Parent) && (!fieldZoneUntil.TryGetValue(zone.State, out var value) || !(realtimeSinceStartupAsDouble < value)))
			{
				BeginFieldZone(zone, Mathf.Max(1, SkillProgress.LevelOf(zone.Parent)), origin, facing);
			}
		}
	}

	private void BeginFieldZone(FieldZone zone, int level, Vector2 origin, int facing)
	{
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		if (zone.IntervalMs > 0 && zone.DurationSec > 0 && zone.Body != 0)
		{
			int level2 = Mathf.Clamp(level, 1, Mathf.Max(1, SkillBook.MaxLevelOf(zone.Parent)));
			int level3 = Mathf.Clamp(level, 1, Mathf.Max(1, SkillBook.MaxLevelOf(zone.Body)));
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(zone.Parent, level2);
			NativeSkillDefinition nativeSkillDefinition2 = SkillBook.Native(zone.Body, level3);
			if (nativeSkillDefinition != null && nativeSkillDefinition2 != null && !(nativeSkillDefinition2.DamagePercent <= 0f))
			{
				SkillDef host = new SkillDef(new ClassSkill
				{
					name = "",
					art = "",
					icon = ""
				}, nativeSkillDefinition);
				fieldZoneGeneration.TryGetValue(zone.State, out var value);
				value++;
				fieldZoneGeneration[zone.State] = value;
				fieldZoneUntil[zone.State] = Time.realtimeSinceStartupAsDouble + (double)zone.DurationSec;
				FieldDirector fieldDirector = Object.FindObjectOfType<FieldDirector>();
				FieldMap map = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
				((MonoBehaviour)this).StartCoroutine(RunFieldZone(zone, value, nativeSkillDefinition, nativeSkillDefinition2, host, origin, facing, fieldDirector, map));
			}
		}
	}

	private IEnumerator RunFieldZone(FieldZone zone, long generation, NativeSkillDefinition parent, NativeSkillDefinition body, SkillDef host, Vector2 origin, int facing, FieldDirector field, FieldMap map)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		double start = Time.realtimeSinceStartupAsDouble;
		double deadline = start + (double)zone.DurationSec;
		double period = (double)zone.IntervalMs / 1000.0;
		int limit = ((zone.MaxActivations > 0) ? zone.MaxActivations : int.MaxValue);
		for (int tick = 1; tick <= limit; tick++)
		{
			double due = start + (double)tick * period;
			while (Time.realtimeSinceStartupAsDouble < due)
			{
				yield return null;
			}
			if (due > deadline || !ReplayAlive(field, map) || !fieldZoneGeneration.TryGetValue(zone.State, out var value) || value != generation)
			{
				break;
			}
			List<Component> list = ((zone.TargetMode == "boss") ? SeekingTargets(host, origin, facing, mistral: false) : ReplayTargets(parent, origin, facing, Mathf.Clamp(zone.MobCount, 1, 15), fistFinish: false));
			if (list.Count == 0)
			{
				continue;
			}
			PlayNativeReplayEffect(zone.Body, origin, facing);
			DamageResult[] array = (((Object)(object)p != (Object)null) ? p.ReplayFallback(parent, 1, close: true) : Array.Empty<DamageResult>());
			if (array.Length == 0)
			{
				continue;
			}
			int num = ((zone.TargetMode == "boss") ? Mathf.Min(Mathf.Max(1, zone.Projectiles), list.Count) : Mathf.Min(Mathf.Max(1, zone.MobCount), list.Count));
			for (int i = 0; i < num; i++)
			{
				Component val = list[i % list.Count];
				if (TargetAlive(val))
				{
					FireProc(host, body, array, parent.DamagePercent, val, origin, facing, body.DamagePercent);
				}
			}
		}
	}

	private void ClearFieldZones()
	{
		foreach (int item in new List<int>(fieldZoneGeneration.Keys))
		{
			fieldZoneGeneration[item]++;
		}
		fieldZoneUntil.Clear();
	}

	private void ResetProcState()
	{
		assassinMarks.Clear();
		procCounter.Clear();
		procBrands.Clear();
		procBrandTicks.Clear();
		procNext.Clear();
		procUntil.Clear();
		chainNext.Clear();
		chainUses.Clear();
		chainWasActive.Clear();
	}

	private void StartProcBuff(SkillDef s)
	{
		if (s.native == null)
		{
			return;
		}
		ProcSpec procSpec = ProcOf(s.native.Id);
		if (procSpec != null && procSpec.Mode == ProcMode.BuffRoll)
		{
			float num = Art.Num(SkillBook.Get(s.native.Id)?.Level(s.native.Level), "time");
			if (!(num <= 0f))
			{
				procUntil[s.native.Id] = Time.realtimeSinceStartupAsDouble + (double)num;
			}
		}
	}

	private static ProcSpec ProcOf(int id)
	{
		ProcSpec[] array = procSpecs;
		foreach (ProcSpec procSpec in array)
		{
			if (procSpec.Trigger == id)
			{
				return procSpec;
			}
		}
		return null;
	}

	private bool ProcBuffActive(int id)
	{
		if (procUntil.TryGetValue(id, out var value))
		{
			return Time.realtimeSinceStartupAsDouble < value;
		}
		return false;
	}

	private void TriggerProcSkills(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		if (cast.native == null || cast.native.Kind != "Attack" || captured == null || captured.Count == 0 || cast.native.DamagePercent <= 0f)
		{
			return;
		}
		int num = 0;
		foreach (CapturedReplayHit item in captured)
		{
			if (item.Hits == null)
			{
				continue;
			}
			DamageResult[] hits = item.Hits;
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].Amount > 0)
				{
					num++;
				}
			}
		}
		if (num == 0)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		UpdateShadowBladeState();
		TriggerStateChains(cast, captured, origin, facing);
		ProcSpec[] array = procSpecs;
		foreach (ProcSpec procSpec in array)
		{
			int num2 = SkillProgress.LevelOf(procSpec.Trigger);
			if (num2 <= 0 || !ProcWeaponOk(procSpec.Trigger) || (procNext.TryGetValue(procSpec.Trigger, out var value) && realtimeSinceStartupAsDouble < value))
			{
				continue;
			}
			Dictionary<string, object> dictionary = SkillBook.Get(procSpec.Trigger)?.Level(num2);
			NativeSkillDefinition nativeSkillDefinition = BodyDefinition(procSpec, num2);
			if (dictionary == null || nativeSkillDefinition == null)
			{
				continue;
			}
			if (procSpec.Mode == ProcMode.MarkPerTarget)
			{
				TryAssassinMark(procSpec, dictionary, nativeSkillDefinition, cast, captured, origin, facing);
				continue;
			}
			NativeSkillStates nativeSkillStates = (((Object)(object)p != (Object)null) ? NativeSkillStates.For(p) : null);
			bool flag = procSpec.AltState > 0 && (Object)(object)nativeSkillStates != (Object)null && nativeSkillStates.Has(procSpec.AltState);
			if (flag && procSpec.AltBody > 0)
			{
				int num3 = SkillBook.MaxLevelOf(procSpec.AltBody);
				if (num3 > 0)
				{
					NativeSkillDefinition nativeSkillDefinition2 = SkillBook.Native(procSpec.AltBody, Mathf.Clamp(num2, 1, num3));
					if (nativeSkillDefinition2 != null && nativeSkillDefinition2.DamagePercent > 0f)
					{
						nativeSkillDefinition = nativeSkillDefinition2;
					}
					else
					{
						flag = false;
					}
				}
			}
			double num4 = ((procSpec.ScaleField.Length > 0) ? ((float)NativeSkillOverrides.Scale(procSpec.Trigger, Art.Int(dictionary, procSpec.ScaleField))) : nativeSkillDefinition.DamagePercent);
			if (num4 <= 0.0)
			{
				continue;
			}
			double fromPercent = ((procSpec.ScaleField.Length > 0) ? 100.0 : ((double)cast.native.DamagePercent));
			string key = ((flag && procSpec.AltPropField.Length > 0) ? procSpec.AltPropField : procSpec.PropField);
			int num5 = Mathf.Clamp(Art.Int(dictionary, key, 100), 0, 100);
			bool flag2;
			switch (procSpec.Mode)
			{
			case ProcMode.BuffRoll:
				if (!ProcBuffActive(procSpec.Trigger))
				{
					continue;
				}
				flag2 = Random.Range(0, 100) < num5;
				break;
			case ProcMode.Charge:
			case ProcMode.SummonCharge:
			{
				int num6 = ((procSpec.Mode == ProcMode.SummonCharge) ? Mathf.Max(1, Art.Int(dictionary, procSpec.CapField, 1)) : int.MaxValue);
				procCounter.TryGetValue(procSpec.Trigger, out var value2);
				value2 = Mathf.Min(num6, value2 + num);
				if (procSpec.Mode == ProcMode.Charge)
				{
					int num7 = Mathf.Max(1, Art.Int(dictionary, procSpec.ChargeField, 1));
					flag2 = value2 >= num7;
					if (flag2)
					{
						value2 -= num7;
					}
				}
				else
				{
					flag2 = value2 > 0 && Random.Range(0, 100) < num5;
					if (flag2)
					{
						value2--;
					}
				}
				procCounter[procSpec.Trigger] = Mathf.Clamp(value2, 0, num6);
				break;
			}
			default:
				flag2 = Random.Range(0, 100) < num5;
				break;
			}
			if (!flag2)
			{
				continue;
			}
			List<Component> list = ProcTargets(captured, Mathf.Max(1, nativeSkillDefinition.TargetCount), origin);
			if (list.Count == 0)
			{
				continue;
			}
			PlayNativeReplayEffect(flag ? procSpec.AltBody : procSpec.Trigger, origin, facing);
			foreach (Component item2 in list)
			{
				FireProc(cast, nativeSkillDefinition, ProcSource(captured, item2, cast.native), fromPercent, item2, origin, facing, num4);
			}
			int num8 = Art.Int(dictionary, procSpec.CooldownField) * procSpec.CooldownUnitMs;
			if (num8 > 0)
			{
				procNext[procSpec.Trigger] = realtimeSinceStartupAsDouble + (double)num8 / 1000.0;
			}
			if (procSpec.BrandBody > 0)
			{
				TryBrand(procSpec, dictionary, cast, captured, origin, facing);
			}
		}
	}

	private void UpdateShadowBladeState()
	{
		if ((Object)(object)p == (Object)null)
		{
			return;
		}
		int num = SkillProgress.LevelOf(4240016);
		if (num <= 0)
		{
			return;
		}
		Dictionary<string, object> dictionary = SkillBook.Get(4240016)?.Level(num);
		if (dictionary == null)
		{
			return;
		}
		NativeSkillStates nativeSkillStates = NativeSkillStates.For(p);
		NativeBuffs nativeBuffs = NativeBuffs.For(p);
		bool flag = false;
		double num2 = 0.0;
		foreach (NativeBuffs.Effect item in nativeBuffs.Visible)
		{
			if (item.Stats.ContainsKey("DARKSIGHT"))
			{
				flag = true;
				num2 = NativeBuffs.Now - item.Started;
				break;
			}
		}
		if (nativeSkillStates.Has(4241005))
		{
			flag = true;
			num2 = Math.Max(num2, nativeSkillStates.Since(4241005));
		}
		if (flag)
		{
			bool flag2 = nativeSkillStates.Has(4240014);
			bool flag3 = num2 >= (double)Art.Int(dictionary, "v", 9);
			if (flag2 || flag3)
			{
				nativeSkillStates.ActivateState(4240017, num, Art.Int(dictionary, "w", 3));
			}
		}
	}

	private void TriggerStateChains(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)p == (Object)null || cast.native == null || captured == null)
		{
			return;
		}
		NativeSkillStates nativeSkillStates = NativeSkillStates.For(p);
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		int[] array = NativeSkillStates.AllIds();
		foreach (int num in array)
		{
			if (NativeSkillStates.Ints(num, "bodies").Length == 0)
			{
				continue;
			}
			if (!nativeSkillStates.Has(num))
			{
				chainWasActive[num] = false;
				chainUses[num] = 0;
				continue;
			}
			if (!chainWasActive.TryGetValue(num, out var value) || !value)
			{
				chainUses[num] = 0;
				chainWasActive[num] = true;
			}
			bool flag = false;
			int[] array2 = NativeSkillStates.Ints(num, "triggers");
			for (int j = 0; j < array2.Length; j++)
			{
				if (array2[j] == cast.native.Id)
				{
					flag = true;
					break;
				}
			}
			if (flag && (!chainNext.TryGetValue(num, out var value2) || !(realtimeSinceStartupAsDouble < value2)))
			{
				chainUses.TryGetValue(num, out var value3);
				int num2 = NativeSkillStates.Int(num, "maxActivations", 8);
				if (num2 <= 0 || value3 < num2)
				{
					int num3 = NativeSkillStates.Int(num, "cooldownMs");
					double val = ((num3 > 0) ? ((double)num3 / 1000.0) : ((double)NativeSkillStates.Int(num, "cooldownSec", 2)));
					chainNext[num] = realtimeSinceStartupAsDouble + Math.Max(0.01, val);
					chainUses[num] = value3 + 1;
					((MonoBehaviour)this).StartCoroutine(FireStateChain(num, cast, captured, NativeSkillStates.Ints(num, "bodies"), origin, facing));
				}
			}
		}
	}

	private IEnumerator FireStateChain(int stateId, SkillDef cast, List<CapturedReplayHit> captured, int[] bodies, Vector2 origin, int facing)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		int id = NativeSkillStates.Int(stateId, "parent");
		int level = Mathf.Max(1, SkillProgress.LevelOf(id));
		foreach (int bodyId in bodies)
		{
			yield return (object)new WaitForSeconds(0.12f);
			if ((Object)(object)p == (Object)null || p.hp <= 0f)
			{
				break;
			}
			int num = SkillBook.MaxLevelOf(bodyId);
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(bodyId, (num <= 0) ? 1 : Mathf.Clamp(level, 1, num));
			if (nativeSkillDefinition == null || nativeSkillDefinition.DamagePercent <= 0f)
			{
				continue;
			}
			List<Component> list = ProcTargets(captured, Mathf.Max(1, nativeSkillDefinition.TargetCount), origin);
			if (list.Count == 0)
			{
				continue;
			}
			PlayNativeReplayEffect(bodyId, origin, facing);
			foreach (Component item in list)
			{
				FireProc(cast, nativeSkillDefinition, ProcSource(captured, item, cast.native), cast.native.DamagePercent, item, origin, facing, nativeSkillDefinition.DamagePercent);
			}
		}
	}

	private void TryAssassinMark(ProcSpec spec, Dictionary<string, object> row, NativeSkillDefinition body, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		if (body == null || body.DamagePercent <= 0f || cast.native == null)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		int num = Mathf.Clamp(Art.Int(row, spec.PropField, 100), 0, 100);
		int num2 = Mathf.Max(1, Art.Int(row, "bulletCount", 1));
		int num3 = Mathf.Max(1, Art.Int(row, "mobCount", 1));
		int range = Mathf.Max(1, Art.Int(row, "range", 500));
		float num4 = Mathf.Max(1f, (float)Art.Int(row, "dotTime", 20));
		foreach (CapturedReplayHit item in captured)
		{
			Component target = item.Target;
			if ((Object)(object)target == (Object)null || !TargetAlive(target))
			{
				continue;
			}
			if (!assassinMarks.TryGetValue(target, out var value) || !(realtimeSinceStartupAsDouble < value))
			{
				if (Random.Range(0, 100) < num)
				{
					assassinMarks[target] = realtimeSinceStartupAsDouble + (double)num4;
				}
				continue;
			}
			assassinMarks.Remove(target);
			List<Component> list = MarkTargets(target, range);
			if (list.Count != 0)
			{
				PlayNativeReplayEffect(spec.Body, origin, facing);
				int num5 = Mathf.Min(num2, Mathf.Min(num3, list.Count));
				for (int i = 0; i < num5; i++)
				{
					Component target2 = list[i];
					FireProc(cast, body, ProcSource(captured, target2, cast.native), cast.native.DamagePercent, target2, origin, facing, body.DamagePercent);
				}
			}
		}
	}

	private List<Component> MarkTargets(Component marked, int range)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = new List<Component>();
		Vector2 val = Vector2.op_Implicit(marked.transform.position);
		float num = (float)range / 40f;
		float num2 = num * num;
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		Vector2 val2;
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead)
			{
				val2 = Vector2.op_Implicit(((Component)bossUnit).transform.position) - val;
				if (val2.sqrMagnitude <= num2)
				{
					list.Add((Component)(object)bossUnit);
				}
			}
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead)
			{
				val2 = Vector2.op_Implicit(((Component)item).transform.position) - val;
				if (val2.sqrMagnitude <= num2)
				{
					list.Add((Component)(object)item);
				}
			}
		}
		list.Sort((Component a, Component b) =>
		{
			bool flag = IsBossTarget(a);
			bool flag2 = IsBossTarget(b);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			int num3 = TargetMaxHp(b).CompareTo(TargetMaxHp(a));
			return (num3 == 0) ? ((Object)a).GetInstanceID().CompareTo(((Object)b).GetInstanceID()) : num3;
		});
		return list;
	}

	private void TryBrand(ProcSpec spec, Dictionary<string, object> row, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(1, Art.Int(row, "u2", 1));
		int num2 = Mathf.Max(1, Art.Int(row, "u", 1));
		procBrandTicks.TryGetValue(spec.Trigger, out var value);
		value++;
		if (value < num)
		{
			procBrandTicks[spec.Trigger] = value;
			return;
		}
		procBrandTicks[spec.Trigger] = 0;
		procBrands.TryGetValue(spec.Trigger, out var value2);
		value2++;
		if (value2 < num2)
		{
			procBrands[spec.Trigger] = value2;
			return;
		}
		procBrands[spec.Trigger] = 0;
		int level = SkillProgress.LevelOf(spec.Trigger);
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(spec.BrandBody, level);
		if (nativeSkillDefinition == null || nativeSkillDefinition.DamagePercent <= 0f)
		{
			return;
		}
		List<Component> list = ProcTargets(captured, Mathf.Max(1, nativeSkillDefinition.TargetCount), origin);
		if (list.Count == 0)
		{
			return;
		}
		PlayNativeReplayEffect(spec.BrandBody, origin, facing);
		foreach (Component item in list)
		{
			FireProc(cast, nativeSkillDefinition, ProcSource(captured, item, cast.native), cast.native.DamagePercent, item, origin, facing, nativeSkillDefinition.DamagePercent);
		}
	}

	public void NativeBasicProcHit(Component target, DamageResult hit, int facing)
	{
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null || hit.Amount <= 0 || (Object)(object)p == (Object)null || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		ProcSpec[] array = procSpecs;
		foreach (ProcSpec procSpec in array)
		{
			if (!procSpec.Basic || !ProcWeaponOk(procSpec.Trigger) || (procNext.TryGetValue(procSpec.Trigger, out var value) && realtimeSinceStartupAsDouble < value))
			{
				continue;
			}
			int num = SkillProgress.LevelOf(procSpec.Trigger);
			if (num <= 0)
			{
				continue;
			}
			Dictionary<string, object> dictionary = SkillBook.Get(procSpec.Trigger)?.Level(num);
			NativeSkillDefinition nativeSkillDefinition = BodyDefinition(procSpec, num);
			if (dictionary != null && nativeSkillDefinition != null && !(nativeSkillDefinition.DamagePercent <= 0f) && Random.Range(0, 100) < Mathf.Clamp(Art.Int(dictionary, procSpec.PropField, 100), 0, 100))
			{
				PlayNativeReplayEffect(procSpec.Trigger, p.FeetPos, facing);
				FireProc(null, nativeSkillDefinition, new DamageResult[1] { hit }, 100.0, target, p.FeetPos, facing, nativeSkillDefinition.DamagePercent);
				int num2 = Art.Int(dictionary, procSpec.CooldownField) * procSpec.CooldownUnitMs;
				if (num2 > 0)
				{
					procNext[procSpec.Trigger] = realtimeSinceStartupAsDouble + (double)num2 / 1000.0;
				}
			}
		}
	}

	private NativeSkillDefinition BodyDefinition(ProcSpec spec, int level)
	{
		if (spec.Body != spec.Trigger)
		{
			int num = SkillBook.MaxLevelOf(spec.Body);
			if (num > 0)
			{
				NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(spec.Body, Mathf.Clamp(level, 1, num));
				if (nativeSkillDefinition != null && nativeSkillDefinition.DamagePercent > 0f)
				{
					return nativeSkillDefinition;
				}
			}
		}
		return SkillBook.Native(spec.Trigger, level);
	}

	private void FireProc(SkillDef cast, NativeSkillDefinition proc, DamageResult[] source, double fromPercent, Component target, Vector2 origin, int facing, double percent)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null) && source.Length != 0)
		{
			DamageResult[] hits = NativeSkillReplay.Adapt(source, proc.HitCount, fromPercent, percent, GameSession.Character?.record.damageCap ?? 19999999);
			if (cast == null)
			{
				cast = new SkillDef(new ClassSkill
				{
					name = proc.Id.ToString(),
					art = proc.Id.ToString(),
					icon = ""
				}, proc);
			}
			ApplyReplayHit(cast, proc, target, hits, origin, facing, new NativeReplayStage
			{
				Total = true,
				Interval = 120
			});
		}
	}

	private bool ProcWeaponOk(int id)
	{
		switch (id)
		{
		case 3100001:
			if ((Object)(object)p != (Object)null)
			{
				return p.WeaponType == WeaponKind.bow;
			}
			return false;
		case 3200001:
			if ((Object)(object)p != (Object)null)
			{
				return p.WeaponType == WeaponKind.crossbow;
			}
			return false;
		case 400041028:
			if ((Object)(object)p != (Object)null)
			{
				return p.WeaponType == WeaponKind.claw;
			}
			return false;
		default:
			return true;
		}
	}

	private List<Component> ProcTargets(List<CapturedReplayHit> captured, int limit, Vector2 origin)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = new List<Component>();
		foreach (CapturedReplayHit item in captured)
		{
			Component target = item.Target;
			if (!((Object)(object)target == (Object)null) && !(target is EnemyUnit { dead: not false }) && !(target is BossUnit { dead: not false }) && !list.Contains(target))
			{
				list.Add(target);
			}
		}
		list.Sort((Component a, Component b) =>
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			bool flag = IsBossTarget(a);
			bool flag2 = IsBossTarget(b);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			int num = TargetMaxHp(b).CompareTo(TargetMaxHp(a));
			if (num != 0)
			{
				return num;
			}
			Vector2 val = Vector2.op_Implicit(a.transform.position) - origin;
			float sqrMagnitude = val.sqrMagnitude;
			val = Vector2.op_Implicit(b.transform.position) - origin;
			float sqrMagnitude2 = val.sqrMagnitude;
			return sqrMagnitude.CompareTo(sqrMagnitude2);
		});
		if (list.Count > limit)
		{
			list.RemoveRange(limit, list.Count - limit);
		}
		return list;
	}

	private static bool IsBossTarget(Component target)
	{
		if (!(target is BossUnit))
		{
			if (target is EnemyUnit enemyUnit)
			{
				return NativeCombatData.Int(enemyUnit.CombatStats, "boss") != 0;
			}
			return false;
		}
		return true;
	}

	private static long TargetMaxHp(Component target)
	{
		if (target is EnemyUnit enemyUnit)
		{
			return NativeCombatData.Long(enemyUnit.CombatStats, "maxHP", 0L);
		}
		if (target is BossUnit bossUnit)
		{
			return NativeCombatData.Long(bossUnit.CombatStats, "maxHP", 0L);
		}
		return 0L;
	}

	private DamageResult[] ProcSource(List<CapturedReplayHit> captured, Component target, NativeSkillDefinition main)
	{
		foreach (CapturedReplayHit item in captured)
		{
			if (item.Target == target && item.Hits != null && item.Hits.Length != 0)
			{
				bool flag = false;
				DamageResult[] hits = item.Hits;
				for (int i = 0; i < hits.Length; i++)
				{
					DamageResult damageResult = hits[i];
					flag |= damageResult.Amount > 0;
				}
				if (flag)
				{
					return item.Hits;
				}
				break;
			}
		}
		if (!((Object)(object)p != (Object)null))
		{
			return Array.Empty<DamageResult>();
		}
		return p.ReplayFallback(main, Mathf.Clamp(main.HitCount, 1, 15), close: true);
	}

	private void PlayAddAttack(SkillDef cast, float extraDelay = 0f)
	{
		if (cast?.native != null && AddAttacks.TryGetValue(cast.native.Id, out var value))
		{
			((MonoBehaviour)this).StartCoroutine(AddAttackAfter(cast, value, extraDelay));
		}
	}

	private IEnumerator AddAttackAfter(SkillDef parent, AddAttackLink link, float extraDelay)
	{
		float num = Mathf.Max(0.3f, link.Delay) + Mathf.Max(0f, extraDelay);
		if (num > 0f)
		{
			yield return (object)new WaitForSeconds(num);
		}
		if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing || heldSkill != null)
		{
			yield break;
		}
		int num2 = SkillProgress.LevelOf(link.Next);
		if (num2 <= 0)
		{
			num2 = Mathf.Max(1, parent.native.Level);
		}
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(link.Next, num2);
		if (nativeSkillDefinition == null)
		{
			yield break;
		}
		SkillData skillData = SkillBook.Get(link.Next);
		string art = link.Next.ToString();
		if (WzAssets.Index != null && WzAssets.Index["skills"] is Dictionary<string, object> dictionary)
		{
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (Convert.ToInt32(item.Value) == link.Next)
				{
					art = item.Key;
					break;
				}
			}
		}
		SkillDef s = new SkillDef(new ClassSkill
		{
			name = (skillData?.name ?? link.Next.ToString()),
			art = art,
			icon = skillData?.icon
		}, nativeSkillDefinition);
		if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[AddAttack] " + parent.native.Id + " → " + link.Next + " lv=" + num2));
		}
		BeginCast(s, continuousShot: true);
	}

	public static bool AddAttackOf(int skillId, out int next, out float delay)
	{
		next = 0;
		delay = 0f;
		if (!AddAttacks.TryGetValue(skillId, out var value))
		{
			return false;
		}
		next = value.Next;
		delay = value.Delay;
		return true;
	}

	private static void EnsureCurses()
	{
		if (curseData != null)
		{
			return;
		}
		TextAsset val = Resources.Load<TextAsset>("NativeSkillCurses");
		string text = ((val != null) ? val.text : null);
		curseData = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
		curseParents = new Dictionary<int, bool>();
		if (!curseData.TryGetValue("curses", out var value) || !(value is Dictionary<string, object> dictionary))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (int.TryParse(item.Key, out var result))
			{
				curseParents[result] = true;
			}
		}
	}

	public static bool IsCurseParent(int id)
	{
		EnsureCurses();
		return curseParents.ContainsKey(id);
	}

	private static int CurseInt(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static CurseSpec CurseFor(int parentId, int level)
	{
		EnsureCurses();
		int key = parentId * 100 + Mathf.Clamp(level, 1, 30);
		if (curseSpecs.TryGetValue(key, out var value))
		{
			return value;
		}
		if (!curseData.TryGetValue("curses", out var value2) || !(value2 is Dictionary<string, object> dictionary))
		{
			return null;
		}
		if (!dictionary.TryGetValue(parentId.ToString(), out var value3) || !(value3 is Dictionary<string, object> dictionary2))
		{
			return null;
		}
		Dictionary<string, object> dictionary3 = (dictionary2.TryGetValue("levels", out var value4) ? (value4 as Dictionary<string, object>) : null);
		if (dictionary3 == null || !dictionary3.TryGetValue(Mathf.Clamp(level, 1, 30).ToString(), out var value5) || !(value5 is Dictionary<string, object> row))
		{
			return null;
		}
		CurseSpec curseSpec = new CurseSpec
		{
			Parent = parentId,
			State = CurseInt(dictionary2, "state"),
			Body = CurseInt(dictionary2, "body"),
			Row = row
		};
		curseSpecs[key] = curseSpec;
		return curseSpec;
	}

	public void StartCurse(SkillDef cast)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		if (cast == null || cast.native == null || (Object)(object)p == (Object)null)
		{
			return;
		}
		CurseSpec curseSpec = CurseFor(cast.native.Id, cast.native.Level);
		if (curseSpec != null)
		{
			double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			curseActive = curseSpec;
			curseLevel = cast.native.Level;
			curseActiveUntil = realtimeSinceStartupAsDouble + (double)CurseInt(curseSpec.Row, "lightDurationSec", 20);
			iceNext = 0.0;
			long generation = ++curseGeneration;
			Vector2 feetPos = p.FeetPos;
			int facing = p.facing;
			ApplyCurse(curseSpec, feetPos, facing, realtimeSinceStartupAsDouble);
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Curse] cast parent=" + curseSpec.Parent + " lv=" + curseLevel + " marks=" + curseMarks.Count + " until=+" + CurseInt(curseSpec.Row, "lightDurationSec", 20) + "s"));
			}
			FieldDirector fieldDirector = Object.FindObjectOfType<FieldDirector>();
			FieldMap map = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
			((MonoBehaviour)this).StartCoroutine(CurseLights(curseSpec, generation, feetPos, facing, fieldDirector, map));
		}
	}

	private void ApplyCurse(CurseSpec spec, Vector2 origin, int facing, double now)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = ReplayTargets(CurseBody(spec), origin, facing, Mathf.Clamp(CurseInt(spec.Row, "curseTargets", 10), 1, 15), fistFinish: false);
		int num = CurseInt(spec.Row, "curseDurationSec", 15);
		int num2 = CurseInt(spec.Row, "refreshCapSec", 20);
		int num3 = Mathf.Max(1, CurseInt(spec.Row, "maxStacks", 6));
		int num4 = 0;
		foreach (Component item in list)
		{
			if (TargetAlive(item))
			{
				if (!curseMarks.TryGetValue(item, out var value))
				{
					value = new CurseMark
					{
						startedAt = now,
						stacks = 0
					};
					curseMarks[item] = value;
				}
				value.stacks = Mathf.Min(num3, value.stacks + 1);
				value.until = Mathf.Min((float)(value.startedAt + (double)num2), (float)(now + (double)num));
				num4++;
			}
		}
		if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[Curse] apply targets=" + list.Count + " applied=" + num4 + " marks=" + curseMarks.Count + " duration=" + num + "s"));
		}
	}

	private NativeSkillDefinition CurseBody(CurseSpec spec)
	{
		return SkillBook.Native(spec.Parent, curseLevel);
	}

	private NativeSkillDefinition CurseState(CurseSpec spec)
	{
		return SkillBook.Native(spec.State, curseLevel) ?? SkillBook.Native(spec.Parent, curseLevel);
	}

	private IEnumerator CurseLights(CurseSpec spec, long generation, Vector2 origin, int facing, FieldDirector field, FieldMap map)
	{
		double period = (double)CurseInt(spec.Row, "lightIntervalMs", 5000) / 1000.0;
		int targets = CurseInt(spec.Row, "lightTargets", 5);
		int maxStacks = Mathf.Max(1, CurseInt(spec.Row, "maxStacks", 6));
		int tick = 1;
		while (true)
		{
			double due = curseActiveUntil - (double)CurseInt(spec.Row, "lightDurationSec", 20) + (double)tick * period;
			while (Time.realtimeSinceStartupAsDouble < due)
			{
				yield return null;
			}
			if (due > curseActiveUntil || curseGeneration != generation || !ReplayAlive(field, map))
			{
				break;
			}
			List<Component> list = ReplayTargets(CurseState(spec), p.FeetPos, p.facing, Mathf.Clamp(targets, 1, 15), fistFinish: false);
			double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			int num = CurseInt(spec.Row, "curseDurationSec", 15);
			int num2 = CurseInt(spec.Row, "refreshCapSec", 20);
			foreach (Component item in list)
			{
				if (TargetAlive(item))
				{
					if (!curseMarks.TryGetValue(item, out var value))
					{
						value = new CurseMark
						{
							startedAt = realtimeSinceStartupAsDouble,
							stacks = 0
						};
						curseMarks[item] = value;
					}
					value.stacks = Mathf.Min(maxStacks, value.stacks + 1);
					value.until = Mathf.Min((float)(value.startedAt + (double)num2), (float)(realtimeSinceStartupAsDouble + (double)num));
				}
			}
			PlayNativeReplayEffect(spec.State, p.FeetPos, p.facing);
			tick++;
		}
	}

	private void TriggerCurseOnHit(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		bool flag = PlayerPrefs.GetInt("debug_hitlog", 0) != 0;
		if (curseActive == null || cast == null || cast.native == null)
		{
			if (flag && cast != null && cast.native != null && cast.native.Id == 400011121)
			{
				Debug.Log((object)"[Curse] skip: no active curse");
			}
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (realtimeSinceStartupAsDouble > curseActiveUntil || cast.native.Kind != "Attack" || cast.native.Id == curseActive.Body)
		{
			return;
		}
		if (captured == null || captured.Count == 0)
		{
			if (flag)
			{
				Debug.Log((object)("[Curse] skip " + cast.native.Id + ": 无命中记录"));
			}
			return;
		}
		bool flag2 = false;
		int num = 1;
		foreach (CapturedReplayHit item in captured)
		{
			if ((Object)(object)item.Target == (Object)null)
			{
				continue;
			}
			if (!curseMarks.TryGetValue(item.Target, out var value))
			{
				if (flag)
				{
					Debug.Log((object)("[Curse] skip " + cast.native.Id + ": 目标未被诅咒 marks=" + curseMarks.Count));
				}
			}
			else if (!(realtimeSinceStartupAsDouble > value.until))
			{
				flag2 = true;
				num = Mathf.Max(num, value.stacks);
				value.until = Mathf.Min((float)(value.startedAt + (double)CurseInt(curseActive.Row, "refreshCapSec", 20)), (float)(realtimeSinceStartupAsDouble + (double)CurseInt(curseActive.Row, "curseDurationSec", 15)));
			}
		}
		if (!flag2 || realtimeSinceStartupAsDouble < iceNext)
		{
			return;
		}
		iceNext = realtimeSinceStartupAsDouble + (double)CurseInt(curseActive.Row, "iceCooldownMs", 400) / 1000.0;
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(curseActive.Body, curseLevel);
		if (nativeSkillDefinition == null || nativeSkillDefinition.DamagePercent <= 0f)
		{
			return;
		}
		PlayNativeReplayEffect(curseActive.Body, origin, facing);
		DamageResult[] array = ((captured.Count > 0) ? captured[0].Hits : Array.Empty<DamageResult>());
		if (array.Length == 0 && (Object)(object)p != (Object)null)
		{
			array = p.ReplayFallback(cast.native, 1, close: true);
		}
		if (array.Length == 0)
		{
			return;
		}
		List<Component> list = ReplayTargets(nativeSkillDefinition, origin, facing, Mathf.Clamp(CurseInt(curseActive.Row, "iceTargets", 15), 1, 15), fistFinish: false);
		int num2 = CurseInt(curseActive.Row, "bonusHitsPerStack", 1);
		int hits = Mathf.Max(1, nativeSkillDefinition.HitCount + Mathf.Max(0, num - 1) * num2);
		foreach (Component item2 in list)
		{
			if (TargetAlive(item2))
			{
				FireIce(nativeSkillDefinition, array, cast.native.DamagePercent, item2, origin, facing, nativeSkillDefinition.DamagePercent, hits);
			}
		}
		if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[Curse] ice body=" + curseActive.Body + " stacks=" + num + " hits=" + hits + " targets=" + list.Count));
		}
	}

	private void FireIce(NativeSkillDefinition body, DamageResult[] source, double fromPercent, Component target, Vector2 origin, int facing, double percent, int hits)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		DamageResult[] hits2 = NativeSkillReplay.Adapt(source, hits, fromPercent, percent, GameSession.Character?.record.damageCap ?? 19999999);
		SkillDef cast = new SkillDef(new ClassSkill
		{
			name = body.Id.ToString(),
			art = "",
			icon = ""
		}, body);
		ApplyReplayHit(cast, body, target, hits2, origin, facing, new NativeReplayStage
		{
			Total = true,
			Interval = 120
		});
	}

	private void ClearCurses()
	{
		curseGeneration++;
		curseActiveUntil = 0.0;
		curseActive = null;
		curseMarks.Clear();
	}

	private void ApplyDots(SkillDef cast, List<CapturedReplayHit> captured)
	{
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		if (cast?.native == null || captured == null || captured.Count == 0)
		{
			return;
		}
		bool flag = PlayerPrefs.GetInt("debug_hitlog", 0) != 0;
		if (!Dots.TryGetValue(cast.native.Id, out var value))
		{
			if (flag)
			{
				Debug.Log((object)("[DotCall] id=" + cast.native.Id + " 不在 DoT 表里（captured=" + captured.Count + "）"));
			}
			return;
		}
		int num = Mathf.Clamp(cast.native.Level, 1, Mathf.Min(value.Max, value.Levels.Count));
		float[] array = ((num >= 1 && num <= value.Levels.Count) ? value.Levels[num - 1] : null);
		if (array == null || array[0] <= 0f || array[1] <= 0f || array[2] <= 0f)
		{
			return;
		}
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(cast.native.Id, cast.native.Level);
		if (nativeSkillDefinition == null)
		{
			return;
		}
		HashSet<Component> hashSet = new HashSet<Component>();
		int num2 = 0;
		foreach (CapturedReplayHit item in captured)
		{
			Component target = item.Target;
			if ((Object)(object)target == (Object)null || !TargetAlive(target))
			{
				continue;
			}
			num2++;
			if (!hashSet.Add(target))
			{
				continue;
			}
			DotMark dotMark = new DotMark
			{
				Skill = cast.native.Id,
				DotPercent = array[0],
				Seconds = array[1],
				Interval = array[2],
				FromPercent = cast.native.DamagePercent,
				Origin = (((Object)(object)p != (Object)null) ? p.FeetPos : Vector2.op_Implicit(target.transform.position)),
				Facing = ((!((Object)(object)p != (Object)null)) ? 1 : p.facing)
			};
			DamageResult[] array2 = ProcSource(captured, target, cast.native);
			if (array2.Length != 0)
			{
				if (!dotTokens.TryGetValue(target, out var value2))
				{
					value2 = (dotTokens[target] = new Dictionary<int, int>());
				}
				value2[cast.native.Id] = ++dotSerial;
				if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
				{
					Debug.Log((object)("[DotApply] id=" + cast.native.Id + " 每跳=" + dotMark.DotPercent + "% 间隔=" + dotMark.Interval + "s 时长=" + dotMark.Seconds + "s"));
				}
				((MonoBehaviour)this).StartCoroutine(DotTicks(cast, nativeSkillDefinition, target, dotMark, array2, value2[cast.native.Id]));
			}
		}
		if (flag && num2 == 0)
		{
			Debug.Log((object)("[DotCall] id=" + cast.native.Id + " 命中目标已全部死亡，不挂 DoT（captured=" + captured.Count + "）"));
		}
	}

	private IEnumerator DotTicks(SkillDef cast, NativeSkillDefinition body, Component target, DotMark mark, DamageResult[] source, int token)
	{
		int planned = Mathf.Max(1, Mathf.RoundToInt(mark.Seconds / Mathf.Max(0.05f, mark.Interval)));
		NativeReplayStage stage = new NativeReplayStage
		{
			Total = true,
			Interval = 0
		};
		long total = 0L;
		int ticks = 0;
		for (int i = 0; i < planned; i++)
		{
			yield return (object)new WaitForSeconds(mark.Interval);
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing || (Object)(object)target == (Object)null || !TargetAlive(target) || !dotTokens.TryGetValue(target, out var value) || !value.TryGetValue(mark.Skill, out var value2) || value2 != token)
			{
				break;
			}
			DamageResult[] array = NativeSkillReplay.Adapt(source, 1, mark.FromPercent, mark.DotPercent, GameSession.Character?.record.damageCap ?? 19999999);
			if (array.Length == 0)
			{
				break;
			}
			ApplyReplayHit(cast, body, target, array, mark.Origin, mark.Facing, stage);
			total += array[0].Amount;
			ticks++;
		}
		if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[Dot] id=" + mark.Skill + " 跳=" + ticks + " 每跳=" + mark.DotPercent + "% 间隔=" + mark.Interval + "s 合计=" + total));
		}
	}

	private void ClearDots()
	{
		foreach (Dictionary<int, int> value in dotTokens.Values)
		{
			foreach (int item in new List<int>(value.Keys))
			{
				value[item] = -1;
			}
		}
		dotTokens.Clear();
	}

	public static bool DotSpecOf(int skillId, int level, out float dmg, out float seconds, out float interval)
	{
		dmg = (seconds = (interval = 0f));
		if (!Dots.TryGetValue(skillId, out var value))
		{
			return false;
		}
		int num = Mathf.Clamp(level, 1, Mathf.Min(value.Max, value.Levels.Count)) - 1;
		if (num < 0 || num >= value.Levels.Count || value.Levels[num] == null)
		{
			return false;
		}
		float[] array = value.Levels[num];
		dmg = array[0];
		seconds = array[1];
		interval = array[2];
		if (dmg > 0f && seconds > 0f)
		{
			return interval > 0f;
		}
		return false;
	}

	private static Dictionary<string, object> FinisherSpec(int parentId)
	{
		if (finisherTable == null)
		{
			finisherTable = new Dictionary<int, Dictionary<string, object>>();
			TextAsset val = Resources.Load<TextAsset>("NativeSkillFinishers");
			string text = ((val != null) ? val.text : null);
			if (!string.IsNullOrEmpty(text) && ((Dictionary<string, object>)MiniJson.Parse(text)).TryGetValue("finishers", out var value) && value is List<object> list)
			{
				foreach (object item in list)
				{
					if (item is Dictionary<string, object> dictionary)
					{
						int num = Art.Id(dictionary, "parent");
						if (num > 0)
						{
							finisherTable[num] = dictionary;
						}
					}
				}
			}
		}
		if (!finisherTable.TryGetValue(parentId, out var value2))
		{
			return null;
		}
		return value2;
	}

	public void StartFinisher(SkillDef parent)
	{
		if (parent == null || parent.native == null || (Object)(object)p == (Object)null)
		{
			return;
		}
		Dictionary<string, object> dictionary = FinisherSpec(parent.native.Id);
		if (dictionary == null)
		{
			return;
		}
		int num = Art.Id(dictionary, "child");
		if (num > 0)
		{
			int level = parent.native.Level;
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(num, level);
			if (nativeSkillDefinition != null)
			{
				int waves = Mathf.Max(1, Art.Int(dictionary, "waves", 1));
				float interval = Mathf.Max(0.04f, (float)Art.Int(dictionary, "intervalMs", 120) / 1000f);
				string visualKeyOverride = NativeSkillVfx.Key(num, level);
				SkillDef body = new SkillDef(new ClassSkill
				{
					name = parent.name,
					art = "",
					icon = ""
				}, nativeSkillDefinition, visualKeyOverride);
				((MonoBehaviour)this).StartCoroutine(FinisherWaves(body, waves, interval));
			}
		}
	}

	private IEnumerator FinisherWaves(SkillDef body, int waves, float interval)
	{
		FieldDirector field = Object.FindObjectOfType<FieldDirector>();
		FieldMap map = (((Object)(object)field != (Object)null) ? field.Map : null);
		for (int i = 0; i < waves; i++)
		{
			if (!ReplayAlive(field, map))
			{
				break;
			}
			Vector2 feetPos = p.FeetPos;
			int facing = p.facing;
			BeginVisuals(body, bodyOnly: true);
			HitAt(body, body.native, feetPos, facing);
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Finisher] wave=" + (i + 1) + "/" + waves + " child=" + body.native.Id + " vfx=" + body.vfx));
			}
			if (i + 1 < waves)
			{
				yield return (object)new WaitForSeconds(interval);
			}
		}
	}

	private static int FollowupInt(string key)
	{
		return Convert.ToInt32(FollowupData[key]);
	}

	private static int[] FollowupTimes(string key)
	{
		List<object> list = (List<object>)FollowupData[key];
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = Convert.ToInt32(list[i]);
		}
		return array;
	}

	private bool ReplayAlive(FieldDirector field, FieldMap map)
	{
		if ((Object)(object)p != (Object)null && p.hp > 0f && ((Behaviour)p).isActiveAndEnabled && (Object)(object)GameLoop.I != (Object)null && !GameLoop.I.gameOver && GameLoop.I.state == GameLoop.GameState.Playing)
		{
			if (!((Object)(object)field == (Object)null))
			{
				return field.Map == map;
			}
			return true;
		}
		return false;
	}

	private static bool TargetAlive(Component t)
	{
		if ((Object)(object)t != (Object)null)
		{
			if (!(t is EnemyUnit { dead: false }))
			{
				if (t is BossUnit bossUnit)
				{
					return !bossUnit.dead;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private NativeSkillDefinition ReplayDefinition(int id, int level)
	{
		return SkillBook.Native(id, Mathf.Clamp(level, 1, SkillBook.MaxLevelOf(id)));
	}

	private void ExtraStage(SkillDef cast, int id, int[] times, DamageResult[] source, List<CapturedReplayHit> captured, Vector2 origin, int facing, FieldDirector field, FieldMap map, bool fixedOrigin = false, int level = 0)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		NativeSkillDefinition nativeSkillDefinition = ReplayDefinition(id, (level > 0) ? level : cast.native.Level);
		if (nativeSkillDefinition != null)
		{
			bool flag = false;
			DamageResult[] array = source;
			for (int i = 0; i < array.Length; i++)
			{
				DamageResult damageResult = array[i];
				flag |= damageResult.Amount > 0;
			}
			if (IsRangedReplay(cast.native.Id) && !flag)
			{
				source = p.ReplayFallback(cast.native, 1, close: false);
			}
			DamageResult[] template = NativeSkillReplay.Adapt(source, nativeSkillDefinition.HitCount, cast.native.DamagePercent, nativeSkillDefinition.DamagePercent, 19999999L);
			NativeReplayStage stage = new NativeReplayStage
			{
				Skill = id,
				Times = times,
				Interval = 120,
				Fixed = fixedOrigin
			};
			((MonoBehaviour)this).StartCoroutine(ReplayStage(cast, nativeSkillDefinition, stage, captured, template, origin, facing, field, map));
		}
	}

	private void TriggerReactiveSkills(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		ApplyDots(cast, captured);
		TriggerProcSkills(cast, captured, origin, facing);
		if (captured != null && captured.Count > 0)
		{
			TriggerFieldZonesOnHit(cast, origin, facing);
		}
		OnMechanicHit(cast, captured, origin, facing);
		TriggerCurseOnHit(cast, captured, origin, facing);
		NotifySequenceHit(cast, captured);
		FieldDirector fieldDirector = Object.FindObjectOfType<FieldDirector>();
		FieldMap map = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
		DamageResult[] source = ((captured.Count > 0) ? captured[0].Hits : Array.Empty<DamageResult>());
		int id = cast.native.Id;
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (Profile.JobId == 422 && captured.Count > 0)
		{
			if ((id == 4221020 || id == 4221018) && State(4221013).TryStart(realtimeSinceStartupAsDouble, 12.0))
			{
				if (id == 4221020)
				{
					ExtraStage(cast, 4221014, FollowupTimes("HAUNTED_EDGE_ASURA_TIMES_MS"), source, captured, origin, facing, fieldDirector, map);
					ExtraStage(cast, 4221015, FollowupTimes("HAUNTED_EDGE_PROJECTILE_TIMES_MS"), source, captured, origin, facing, fieldDirector, map);
				}
				else
				{
					ExtraStage(cast, 4221016, FollowupTimes("HAUNTED_EDGE_RAKSHASA_TIMES_MS"), source, captured, origin, facing, fieldDirector, map);
				}
			}
			dualBladeHits = Math.Min(FollowupInt("SUDDEN_RAID_MAX_STORED_HITS"), dualBladeHits + 1);
			int num = FollowupInt("SUDDEN_RAID_TRIGGER_HITS");
			if (dualBladeHits >= num && State(4221017).TryStart(realtimeSinceStartupAsDouble, 1.0))
			{
				dualBladeHits -= num;
				ExtraStage(cast, 4221017, FollowupTimes("SUDDEN_RAID_FINISHER_TIMES_MS"), source, captured, origin, facing, fieldDirector, map);
			}
		}
		if (id == 4121023)
		{
			fourSeasonsFrenzy = captured.Count > 0;
		}
		if (IsRangedReplay(id))
		{
			switch (id)
			{
			case 3121033:
				return;
			case 3121010:
				arrowRainExpires = realtimeSinceStartupAsDouble + (double)FollowupInt("ARROW_RAIN_DURATION_MS") / 1000.0;
				arrowRainNext = realtimeSinceStartupAsDouble;
				break;
			}
			if (!(realtimeSinceStartupAsDouble >= arrowRainExpires) && (id == 3121010 || captured.Count != 0) && !(realtimeSinceStartupAsDouble < arrowRainNext))
			{
				arrowRainNext = realtimeSinceStartupAsDouble + (double)FollowupInt("ARROW_RAIN_FIELD_COOLDOWN_MS") / 1000.0;
				ExtraStage(cast, 3121033, FollowupTimes("ARROW_RAIN_FIELD_TIMES_MS"), source, captured, origin, facing, fieldDirector, map, fixedOrigin: true, Math.Max(1, SkillProgress.LevelOf(3121010)));
			}
		}
	}

	public void NativeBasicRangedHit(Component target, DamageResult hit, int facing)
	{
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (!((Object)(object)target == (Object)null) && !(realtimeSinceStartupAsDouble >= arrowRainExpires) && !(realtimeSinceStartupAsDouble < arrowRainNext))
		{
			NativeSkillDefinition nativeSkillDefinition = ReplayDefinition(3121033, Math.Max(1, SkillProgress.LevelOf(3121010)));
			if (nativeSkillDefinition != null)
			{
				arrowRainNext = realtimeSinceStartupAsDouble + (double)FollowupInt("ARROW_RAIN_FIELD_COOLDOWN_MS") / 1000.0;
				FieldDirector fieldDirector = Object.FindObjectOfType<FieldDirector>();
				FieldMap map = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
				SkillDef cast = new SkillDef(new ClassSkill
				{
					name = "",
					art = "",
					icon = ""
				}, nativeSkillDefinition);
				DamageResult[] template = NativeSkillReplay.Adapt(new DamageResult[1] { hit }, nativeSkillDefinition.HitCount, 100.0, nativeSkillDefinition.DamagePercent, 19999999L);
				NativeReplayStage stage = new NativeReplayStage
				{
					Skill = 3121033,
					Times = FollowupTimes("ARROW_RAIN_FIELD_TIMES_MS"),
					Fixed = true,
					Interval = 120
				};
				((MonoBehaviour)this).StartCoroutine(ReplayStage(cast, nativeSkillDefinition, stage, new List<CapturedReplayHit>(), template, p.FeetPos, facing, fieldDirector, map));
			}
		}
	}

	private void StartSeekingSkills(SkillDef cast, DamageResult[] template, Vector2 origin, int facing, FieldDirector field, FieldMap map)
	{
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		if (cast.native.Id != 13121003 && cast.native.Id != 13121013)
		{
			return;
		}
		if (template.Length == 0)
		{
			template = p.ReplayFallback(cast.native, 1, close: false);
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		if (cast.native.Id == 13121003)
		{
			NativeSkillDefinition nativeSkillDefinition = ReplayDefinition(13121018, cast.native.Level);
			DamageResult[] hits = NativeSkillReplay.Adapt(template, nativeSkillDefinition.HitCount, cast.native.DamagePercent, nativeSkillDefinition.DamagePercent, 19999999L);
			DamageResult[] dot = NativeSkillReplay.Adapt(template, 1, cast.native.DamagePercent, FollowupInt("MERCILESS_WINDS_DOT_PERCENT"), 19999999L);
			Dictionary<Component, int> counts = new Dictionary<Component, int>();
			int num = 0;
			double deadline = realtimeSinceStartupAsDouble + (double)FollowupInt("MERCILESS_WINDS_TRACKING_DURATION_MS") / 1000.0;
			int[] array = FollowupTimes("MERCILESS_WINDS_TIMES_MS");
			foreach (int num2 in array)
			{
				((MonoBehaviour)this).StartCoroutine(SeekingProjectile(cast, nativeSkillDefinition, hits, dot, origin, facing, field, map, num++, realtimeSinceStartupAsDouble + (double)num2 / 1000.0, deadline, counts, mistral: false));
			}
			return;
		}
		int[] array2 = FollowupTimes("MISTRAL_SPIRIT_COUNTS");
		int[] array3 = FollowupTimes("MISTRAL_SPIRIT_LIFETIMES_MS");
		int[] array4 = FollowupTimes("MISTRAL_SPIRIT_ENABLE_DELAYS_MS");
		int num3 = 0;
		for (int j = 0; j < FollowupInt("MISTRAL_SPIRIT_DURATION_MS"); j += FollowupInt("MISTRAL_SPIRIT_INTERVAL_MS"))
		{
			int num4 = num3++ % 3;
			NativeSkillDefinition nativeSkillDefinition2 = ReplayDefinition(13121015 + num4, cast.native.Level);
			DamageResult[] hits2 = NativeSkillReplay.Adapt(template, nativeSkillDefinition2.HitCount, cast.native.DamagePercent, nativeSkillDefinition2.DamagePercent, 19999999L);
			double num5 = realtimeSinceStartupAsDouble + (double)(FollowupInt("MISTRAL_SPIRIT_START_TIME_MS") + j) / 1000.0;
			double deadline2 = Math.Min(num5 + (double)array3[num4] / 1000.0, realtimeSinceStartupAsDouble + (double)FollowupInt("MISTRAL_SPIRIT_TOTAL_END_TIME_MS") / 1000.0);
			for (int k = 0; k < array2[num4]; k++)
			{
				((MonoBehaviour)this).StartCoroutine(SeekingProjectile(cast, nativeSkillDefinition2, hits2, null, origin, facing, field, map, k, num5 + (double)array4[num4] / 1000.0, deadline2, null, mistral: true));
			}
		}
	}

	private List<Component> SeekingTargets(SkillDef cast, Vector2 origin, int facing, bool mistral)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = ReplayTargets(cast.native, origin, facing, int.MaxValue, fistFinish: false);
		if (mistral && !cast.native.HasBounds)
		{
			list.RemoveAll((Component t) =>
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				return Mathf.Abs(t.transform.position.x - origin.x) > 30f || Mathf.Abs(t.transform.position.y - origin.y) > 20f;
			});
		}
		list.Sort((Component a, Component b) =>
		{
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			bool flag = a is BossUnit || (a is EnemyUnit enemyUnit && NativeCombatData.Int(enemyUnit.CombatStats, "boss") != 0);
			bool flag2 = b is BossUnit || (b is EnemyUnit enemyUnit2 && NativeCombatData.Int(enemyUnit2.CombatStats, "boss") != 0);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			if (flag)
			{
				Dictionary<string, object> d = ((a is EnemyUnit enemyUnit3) ? enemyUnit3.CombatStats : ((BossUnit)(object)a).CombatStats);
				int num = Art.Num((b is EnemyUnit enemyUnit4) ? enemyUnit4.CombatStats : ((BossUnit)(object)b).CombatStats, "maxHP").CompareTo(Art.Num(d, "maxHP"));
				if (num != 0)
				{
					return num;
				}
			}
			Vector3 val = a.transform.position - Vector2.op_Implicit(origin);
			float sqrMagnitude = val.sqrMagnitude;
			val = b.transform.position - Vector2.op_Implicit(origin);
			int num2 = sqrMagnitude.CompareTo(val.sqrMagnitude);
			return (num2 == 0) ? ((Object)a).GetInstanceID().CompareTo(((Object)b).GetInstanceID()) : num2;
		});
		return list;
	}

	private IEnumerator SeekingProjectile(SkillDef cast, NativeSkillDefinition replay, DamageResult[] hits, DamageResult[] dot, Vector2 origin, int facing, FieldDirector field, FieldMap map, int index, double start, double deadline, Dictionary<Component, int> counts, bool mistral)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		while (Time.realtimeSinceStartupAsDouble < start)
		{
			yield return null;
		}
		while (ReplayAlive(field, map) && Time.realtimeSinceStartupAsDouble < deadline)
		{
			List<Component> list = SeekingTargets(cast, origin, facing, mistral);
			if (list.Count > 0)
			{
				Component val = list[index % list.Count];
				DamageResult[] hits2 = hits;
				if (counts != null)
				{
					counts.TryGetValue(val, out var value);
					if (value > 0)
					{
						hits2 = NativeSkillReplay.Adapt(hits, hits.Length, 100.0, FollowupInt("MERCILESS_WINDS_REPEATED_TARGET_PERCENT"), 19999999L);
					}
					counts[val] = value + 1;
				}
				ApplyReplayHit(cast, replay, val, hits2, origin, facing, new NativeReplayStage
				{
					Indexed = mistral,
					Total = !mistral,
					Interval = 120
				});
				if (dot != null && TargetAlive(val))
				{
					long num = ++windGeneration;
					windDots[val] = num;
					((MonoBehaviour)this).StartCoroutine(WindDot(val, dot[0].Amount, num, field, map));
				}
				break;
			}
			double next = Time.realtimeSinceStartupAsDouble + (double)FollowupInt(mistral ? "MISTRAL_SPIRIT_SEARCH_INTERVAL_MS" : "MERCILESS_WINDS_SEARCH_INTERVAL_MS") / 1000.0;
			while (Time.realtimeSinceStartupAsDouble < next)
			{
				yield return null;
			}
		}
	}

	private IEnumerator WindDot(Component target, long amount, long generation, FieldDirector field, FieldMap map)
	{
		double start = Time.realtimeSinceStartupAsDouble;
		for (int tick = 1; tick <= FollowupInt("MERCILESS_WINDS_DOT_TICKS"); tick++)
		{
			double due = start + (double)(tick * FollowupInt("MERCILESS_WINDS_DOT_INTERVAL_MS")) / 1000.0;
			while (Time.realtimeSinceStartupAsDouble < due)
			{
				yield return null;
			}
			if (!ReplayAlive(field, map) || !TargetAlive(target) || !windDots.TryGetValue(target, out var value) || value != generation)
			{
				yield break;
			}
			if (target is EnemyUnit enemyUnit)
			{
				enemyUnit.TakeReplayDot(amount);
			}
			else
			{
				((BossUnit)(object)target).TakeReplayDot(amount);
			}
		}
		if (windDots.TryGetValue(target, out var value2) && value2 == generation)
		{
			windDots.Remove(target);
		}
	}

	private void ClearNativeReplayEffects()
	{
		foreach (NativeImgAnimation nativeReplayEffect in nativeReplayEffects)
		{
			if ((Object)(object)nativeReplayEffect != (Object)null)
			{
				nativeReplayEffect.Remove();
			}
		}
		nativeReplayEffects.Clear();
		windDots.Clear();
		ClearCurses();
		ClearDots();
		ClearStacks();
	}

	public static bool IsRepeatingSkill(int id)
	{
		return RepeatAttackSkills.Contains(id);
	}

	private void StartRepeatingAttack(SkillDef s)
	{
		if (s.native == null || !RepeatAttackSkills.Contains(s.native.Id))
		{
			return;
		}
		Dictionary<string, object> d = SkillBook.Get(s.native.Id)?.Level(s.native.Level);
		int num = Art.Int(d, "attackDelay");
		int num2 = Art.Int(d, "time");
		if (num > 0 && num2 > 0)
		{
			SkillDef body = s;
			if (RepeatAttackBodies.TryGetValue(s.native.Id, out var value))
			{
				string visualKeyOverride = NativeSkillVfx.Key(value, s.native.Level);
				body = new SkillDef(new ClassSkill
				{
					name = s.name,
					art = s.art,
					icon = s.icon
				}, s.native, visualKeyOverride);
			}
			((MonoBehaviour)this).StartCoroutine(RepeatAttack(body, (float)num / 1000f, num2));
		}
	}

	private IEnumerator RepeatAttack(SkillDef body, float period, float seconds)
	{
		FieldDirector field = Object.FindObjectOfType<FieldDirector>();
		FieldMap map = (((Object)(object)field != (Object)null) ? field.Map : null);
		double start = Time.realtimeSinceStartupAsDouble;
		double end = start + (double)seconds;
		int tick = 0;
		while (start + (double)((float)(tick + 1) * period) <= end)
		{
			int num = tick + 1;
			tick = num;
			double due = start + (double)((float)num * period);
			while (Time.realtimeSinceStartupAsDouble < due)
			{
				yield return null;
			}
			if (!ReplayAlive(field, map))
			{
				break;
			}
			Vector2 feetPos = p.FeetPos;
			int facing = p.facing;
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Repeat] tick=" + tick + " body=" + body.native.Id + " vfx=" + body.vfx));
			}
			BeginVisuals(body, bodyOnly: true);
			((MonoBehaviour)this).StartCoroutine(RepeatingHammer(body, feetPos, facing, 0.45f));
			HitAt(body, body.native, feetPos, facing);
		}
	}

	private void StartThunderSeries(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (s.native != null && s.native.Id == 400021030)
		{
			int num = Art.Int(SkillBook.Get(s.native.Id)?.Level(s.native.Level), "w");
			if (num > 1)
			{
				((MonoBehaviour)this).StartCoroutine(ThunderSeries(s, origin, facing, num));
			}
		}
	}

	private IEnumerator ThunderSeries(SkillDef s, Vector2 origin, int facing, int bolts)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		for (int index = 1; index < bolts; index++)
		{
			yield return (object)new WaitForSeconds(0.12f);
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing)
			{
				break;
			}
			HitAt(s, s.native, origin, facing);
		}
	}

	private static int MechanicId(string kind)
	{
		int[] array = NativeSkillMechanics.AllIds();
		foreach (int num in array)
		{
			if (NativeSkillMechanics.Str(num, "kind") == kind)
			{
				return num;
			}
		}
		return 0;
	}

	private static Dictionary<string, object> MechRow(int id)
	{
		return SkillBook.Get(id)?.Level(Mathf.Max(1, SkillProgress.LevelOf(id)));
	}

	private static int MechField(int id, string fieldKey)
	{
		string text = NativeSkillMechanics.Str(id, fieldKey);
		Dictionary<string, object> dictionary = MechRow(id);
		if (!string.IsNullOrEmpty(text) && dictionary != null)
		{
			return Art.Int(dictionary, text);
		}
		return 0;
	}

	private int Resource(int id)
	{
		if (!mechResources.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value;
	}

	private bool MechanicActive(int id, double now)
	{
		if (mechUntil.TryGetValue(id, out var value))
		{
			return now < value;
		}
		return false;
	}

	private string MechanicCastGate(int id, int level)
	{
		SettleDarkSight();
		int num = MechanicId("darkSight");
		if (num > 0 && id == NativeSkillMechanics.Int(num, "parent") && NativeBuffs.Now < darkSightBlockedUntil)
		{
			return "隐身术冷却中（黑暗面回避）";
		}
		int num2 = MechanicId("resourceBurst");
		if (num2 > 0 && id == num2)
		{
			int num3 = MechField(num2, "costField");
			if (num3 > 0 && Resource(num2) < num3)
			{
				return "海龙石不足（需要 " + num3 + " 个）";
			}
		}
		return null;
	}

	private SkillDef ResolveMechanicReplacement(SkillDef s)
	{
		if ((Object)(object)p == (Object)null || s == null || s.native == null)
		{
			return s;
		}
		int num = MechanicId("replace");
		if (num <= 0 || s.native.Id != NativeSkillMechanics.Int(num, "source"))
		{
			return s;
		}
		if (!NativeSkillStates.For(p).Has(NativeSkillMechanics.Int(num, "state")))
		{
			return s;
		}
		int[] array = NativeSkillMechanics.Ints(num, "bodies");
		if (array.Length == 0)
		{
			return s;
		}
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(array[0], Mathf.Clamp(s.native.Level, 1, Mathf.Max(1, SkillBook.MaxLevelOf(array[0]))));
		if (nativeSkillDefinition == null || nativeSkillDefinition.DamagePercent <= 0f)
		{
			return s;
		}
		mechReplaceShock = true;
		mechReplaceLevel = s.native.Level;
		return new SkillDef(new ClassSkill
		{
			name = s.name,
			art = s.art,
			icon = s.icon
		}, nativeSkillDefinition);
	}

	private void OnMechanicCast(SkillDef s, Vector2 origin, int facing)
	{
		if ((Object)(object)p == (Object)null || s == null || s.native == null)
		{
			return;
		}
		double now = NativeBuffs.Now;
		int id = s.native.Id;
		int[] array = NativeSkillMechanics.AllIds();
		foreach (int num in array)
		{
			string text = NativeSkillMechanics.Str(num, "kind");
			if (text == "darkSight")
			{
				if (id == NativeSkillMechanics.Int(num, "parent"))
				{
					if (darkSightEvades > 0)
					{
						ApplyDarkSightCooldown();
					}
					darkSightWasActive = true;
					darkSightEvades = 0;
				}
			}
			else if (id == num)
			{
				switch (text)
				{
				case "resourceBurst":
					mechResources[num] = Mathf.Max(0, Resource(num) - MechField(num, "costField"));
					mechUntil[num] = now + (double)MechField(num, "durationField");
					mechBursts[num] = 0;
					break;
				case "spread":
					mechUntil[num] = now + (double)MechField(num, "durationField");
					break;
				case "charm":
					mechResources[num] = MechField(num, "capField");
					mechUntil[num] = now + (double)MechField(num, "durationField");
					mechNextPassive[num] = now + (double)MechField(num, "durationField") + (double)MechField(num, "passiveIntervalField") / 1000.0;
					break;
				}
			}
		}
	}

	private void OnMechanicHit(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)p == (Object)null || cast == null || cast.native == null || captured == null || captured.Count == 0)
		{
			return;
		}
		double now = NativeBuffs.Now;
		int id = cast.native.Id;
		if (mechReplaceShock)
		{
			mechReplaceShock = false;
			FireReplacementShock(cast, captured, origin, facing);
		}
		int[] array = NativeSkillMechanics.AllIds();
		foreach (int num in array)
		{
			switch (NativeSkillMechanics.Str(num, "kind"))
			{
			case "resourceBurst":
				if (NativeSkillMechanics.InList(num, "gainSkills", id))
				{
					int num2 = MechField(num, "capField");
					mechResources[num] = Mathf.Min(Mathf.Max(1, num2), Resource(num) + 1);
				}
				if (NativeSkillMechanics.InList(num, "bonusTargets", id))
				{
					FireDragonBonus(num, cast, captured, origin, facing);
				}
				if (MechanicActive(num, now) && NativeSkillMechanics.InList(num, "gainSkills", id))
				{
					FireDragonBurst(num, cast, captured, origin, facing);
				}
				break;
			case "spread":
				if (MechanicActive(num, now) && NativeSkillMechanics.InList(num, "throwSkills", id))
				{
					FireSpread(num, cast, captured, origin, facing);
				}
				break;
			case "charm":
				AccumulateCharms(num, now);
				if (NativeSkillMechanics.InList(num, "throwSkills", id))
				{
					FireCharms(num, cast, captured, origin, facing);
				}
				break;
			case "replace":
				if (NativeSkillMechanics.InList(num, "triggerSkills", id))
				{
					NativeSkillStates.For(p).ActivateState(NativeSkillMechanics.Int(num, "state"), Mathf.Max(1, SkillProgress.LevelOf(NativeSkillMechanics.Int(num, "source"))), MechField(num, "durationField"));
				}
				break;
			}
		}
		SettleDarkSight();
	}

	private void FireMechanicHit(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing, NativeSkillDefinition proc, double fromPercent, double percent, int hits, int targetLimit, int visualId)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if (percent <= 0.0 || cast == null || cast.native == null || captured == null)
		{
			return;
		}
		List<Component> list = ProcTargets(captured, Mathf.Clamp(targetLimit, 1, 15), origin);
		if (list.Count == 0)
		{
			return;
		}
		if (visualId > 0)
		{
			PlayNativeReplayEffect(visualId, origin, facing);
		}
		NativeSkillDefinition replay = proc ?? cast.native;
		foreach (Component item in list)
		{
			DamageResult[] hits2 = NativeSkillReplay.Adapt(ProcSource(captured, item, cast.native), Mathf.Clamp(hits, 1, 15), fromPercent, percent, GameSession.Character?.record.damageCap ?? 19999999);
			ApplyReplayHit(cast, replay, item, hits2, origin, facing, new NativeReplayStage
			{
				Total = true,
				Interval = 120
			});
		}
	}

	private void FireDragonBurst(int mid, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		int num = MechField(mid, "maxBurstsField");
		mechBursts.TryGetValue(mid, out var value);
		if (num > 0 && value >= num)
		{
			return;
		}
		mechBursts[mid] = value + 1;
		int num2 = Mathf.Max(1, SkillProgress.LevelOf(mid));
		double num3 = 1.0 + (double)Mathf.Max(0, MechField(mid, "bonusField")) / 100.0;
		int[] array = NativeSkillMechanics.Ints(mid, "bodies");
		foreach (int num4 in array)
		{
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(num4, Mathf.Clamp(num2, 1, Mathf.Max(1, SkillBook.MaxLevelOf(num4))));
			if (nativeSkillDefinition != null && !(nativeSkillDefinition.DamagePercent <= 0f))
			{
				FireMechanicHit(cast, captured, origin, facing, nativeSkillDefinition, cast.native.DamagePercent, (double)nativeSkillDefinition.DamagePercent * num3, nativeSkillDefinition.HitCount, nativeSkillDefinition.TargetCount, num4);
			}
		}
	}

	private void FireDragonBonus(int mid, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		double num = MechField(mid, "bonusField");
		if (!(num <= 0.0))
		{
			FireMechanicHit(cast, captured, origin, facing, cast.native, cast.native.DamagePercent, num, 1, Mathf.Clamp(cast.native.TargetCount, 1, 15), 0);
		}
	}

	private void FireSpread(int mid, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		List<Dictionary<string, object>> list = NativeSkillMechanics.Variants(mid);
		if (list.Count == 0)
		{
			return;
		}
		Dictionary<string, object> dictionary = null;
		foreach (Dictionary<string, object> item in list)
		{
			if (NativeSkillMechanics.InList(item, "triggers", cast.native.Id))
			{
				dictionary = item;
				break;
			}
		}
		if (dictionary == null)
		{
			dictionary = list[list.Count - 1];
		}
		int num = Convert.ToInt32(dictionary["directions"]);
		Dictionary<string, object> dictionary2 = MechRow(mid);
		if (dictionary2 != null)
		{
			double percent = Art.Int(dictionary2, Convert.ToString(dictionary["field"]));
			int targetLimit = Art.Int(dictionary2, Convert.ToString(dictionary["mobField"]), 1);
			for (int i = 0; i < num; i++)
			{
				FireMechanicHit(cast, captured, origin, facing, cast.native, cast.native.DamagePercent, percent, 1, targetLimit, mid);
			}
		}
	}

	private void AccumulateCharms(int mid, double now)
	{
		if (MechanicActive(mid, now))
		{
			return;
		}
		int num = MechField(mid, "passiveIntervalField");
		if (num > 0)
		{
			if (!mechNextPassive.TryGetValue(mid, out var value))
			{
				mechNextPassive[mid] = now + (double)num / 1000.0;
			}
			else if (!(now < value))
			{
				int num2 = Mathf.Clamp((int)((now - value) * 1000.0 / (double)num) + 1, 1, 64);
				int num3 = Mathf.Max(1, MechField(mid, "capField"));
				mechResources[mid] = Mathf.Min(num3, Resource(mid) + num2);
				mechNextPassive[mid] = value + (double)(num2 * num) / 1000.0;
			}
		}
	}

	private void FireCharms(int mid, SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		int num = Resource(mid);
		int num2 = Mathf.Max(1, MechField(mid, "minPerCastField"));
		int num3 = Mathf.Max(num2, MechField(mid, "maxPerCastField"));
		if (num < num2)
		{
			return;
		}
		int num4 = Mathf.Min(num, num3);
		mechResources[mid] = num - num4;
		Dictionary<string, object> dictionary = MechRow(mid);
		if (dictionary != null)
		{
			double percent = Art.Int(dictionary, NativeSkillMechanics.Str(mid, "damageField"));
			int hits = Art.Int(dictionary, NativeSkillMechanics.Str(mid, "hitsField"), 1);
			int targetLimit = Art.Int(dictionary, NativeSkillMechanics.Str(mid, "mobsField"), 1);
			int visualId = NativeSkillMechanics.Int(mid, "body");
			for (int i = 0; i < num4; i++)
			{
				FireMechanicHit(cast, captured, origin, facing, cast.native, cast.native.DamagePercent, percent, hits, targetLimit, visualId);
			}
		}
	}

	private void FireReplacementShock(SkillDef cast, List<CapturedReplayHit> captured, Vector2 origin, int facing)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		int num = MechanicId("replace");
		if (num <= 0)
		{
			return;
		}
		int[] array = NativeSkillMechanics.Ints(num, "bodies");
		if (array.Length >= 2)
		{
			int num2 = array[1];
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(num2, Mathf.Clamp(mechReplaceLevel, 1, Mathf.Max(1, SkillBook.MaxLevelOf(num2))));
			if (nativeSkillDefinition != null && !(nativeSkillDefinition.DamagePercent <= 0f))
			{
				FireMechanicHit(cast, captured, origin, facing, nativeSkillDefinition, cast.native.DamagePercent, nativeSkillDefinition.DamagePercent, nativeSkillDefinition.HitCount, nativeSkillDefinition.TargetCount, num2);
			}
		}
	}

	public bool TryEvadeHpRatioDamage()
	{
		if ((Object)(object)p == (Object)null || p.hp <= 0f)
		{
			return false;
		}
		int num = MechanicId("darkSight");
		if (num <= 0)
		{
			return false;
		}
		if (!NativeSkillStates.For(p).Has(NativeSkillMechanics.Int(num, "state")))
		{
			return false;
		}
		int num2 = MechField(num, "maxEvadesField");
		if (num2 > 0 && darkSightEvades >= num2)
		{
			return false;
		}
		darkSightEvades++;
		darkSightWasActive = true;
		return true;
	}

	private void SettleDarkSight()
	{
		int num = MechanicId("darkSight");
		if (num <= 0 || (Object)(object)p == (Object)null)
		{
			return;
		}
		if (NativeSkillStates.For(p).Has(NativeSkillMechanics.Int(num, "state")))
		{
			darkSightWasActive = true;
		}
		else if (darkSightWasActive)
		{
			darkSightWasActive = false;
			if (darkSightEvades > 0)
			{
				ApplyDarkSightCooldown();
			}
		}
	}

	private void ApplyDarkSightCooldown()
	{
		int num = MechanicId("darkSight");
		if (num > 0 && darkSightEvades > 0)
		{
			int num2 = Mathf.Max(1, NativeSkillMechanics.Int(num, "cooldownPerEvadeSec", 3));
			int num3 = Mathf.Max(1, NativeSkillMechanics.Int(num, "maxStacks", 5));
			darkSightBlockedUntil = NativeBuffs.Now + (double)(Mathf.Min(darkSightEvades, num3) * num2);
			darkSightEvades = 0;
		}
	}

	private void ClearMechanics()
	{
		mechResources.Clear();
		mechUntil.Clear();
		mechNextPassive.Clear();
		mechBursts.Clear();
		darkSightBlockedUntil = 0.0;
		darkSightEvades = 0;
		darkSightWasActive = false;
		mechReplaceShock = false;
	}

	private bool BeginReplay(SkillDef cast, Vector2 origin, int facing)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<NativeReplayStage> readOnlyList = NativeSkillReplay.Get(cast.native.Id);
		bool flag = cast.native.Id == 4121023 && fourSeasonsFrenzy;
		if (flag)
		{
			readOnlyList = NativeSkillReplay.Get(4121024);
			fourSeasonsFrenzy = false;
		}
		if (readOnlyList == null)
		{
			return false;
		}
		List<CapturedReplayHit> list = new List<CapturedReplayHit>();
		foreach (Component item in ReplayTargets(cast.native, origin, facing, Mathf.Clamp(cast.native.TargetCount, 1, 15), fistFinish: false, unbounded: false))
		{
			DamageResult[] array = new DamageResult[Mathf.Clamp(cast.native.HitCount, 1, 15)];
			Dictionary<string, object> target = ((item is EnemyUnit enemyUnit) ? enemyUnit.CombatStats : ((BossUnit)(object)item).CombatStats);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = p.RollAttack(target, cast.native);
			}
			list.Add(new CapturedReplayHit
			{
				Target = item,
				Hits = array
			});
		}
		if (!cast.native.Magic)
		{
			NativeBuffs component = ((Component)p).GetComponent<NativeBuffs>();
			if (IsRangedReplay(cast.native.Id))
			{
				component?.OnRangedAttack(list.Count);
			}
			else
			{
				component?.OnCloseAttack();
			}
		}
		DamageResult[] array2 = ((list.Count > 0) ? list[0].Hits : Array.Empty<DamageResult>());
		FieldDirector fieldDirector = Object.FindObjectOfType<FieldDirector>();
		FieldMap map = (((Object)(object)fieldDirector != (Object)null) ? fieldDirector.Map : null);
		if (!flag)
		{
			TriggerReactiveSkills(cast, list, origin, facing);
		}
		StartSeekingSkills(cast, array2, origin, facing, fieldDirector, map);
		if (cast.native.Id == 15121002)
		{
			if (!spearCombo.TryPress((long)(Time.realtimeSinceStartupAsDouble * 1000.0)))
			{
				return true;
			}
			int id = 15121022 + spearCombo.PressCount - 1;
			p.PlayNativeSkill(id);
			PlayNativeReplayEffect(id, origin, facing);
			LockAction(0.18f, cast.native);
			NativeReplayStage stage = new NativeReplayStage
			{
				Total = true,
				Interval = 120
			};
			foreach (CapturedReplayHit item2 in list)
			{
				ApplyReplayHit(cast, cast.native, item2.Target, item2.Hits, origin, facing, stage);
			}
			NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(15121009, Mathf.Clamp(cast.native.Level, 1, SkillBook.MaxLevelOf(15121009)));
			DamageResult[] hits = NativeSkillReplay.Adapt((array2.Length != 0) ? array2 : p.ReplayFallback(cast.native, Mathf.Clamp(nativeSkillDefinition.HitCount, 1, 15), close: true), nativeSkillDefinition.HitCount, cast.native.DamagePercent, nativeSkillDefinition.DamagePercent, GameSession.Character?.record.damageCap ?? 19999999);
			int num = 0;
			foreach (CapturedReplayHit item3 in list)
			{
				if (!((Object)(object)item3.Target == (Object)null) && !(item3.Target is EnemyUnit { dead: not false }) && !(item3.Target is BossUnit { dead: not false }))
				{
					PlayNativeReplayEffect(15121009, p.FeetPos, facing);
					ApplyReplayHit(cast, nativeSkillDefinition, item3.Target, hits, origin, facing, stage);
					if (++num == 3)
					{
						break;
					}
				}
			}
			if (!spearCombo.Finishing)
			{
				return true;
			}
		}
		foreach (NativeReplayStage item4 in readOnlyList)
		{
			NativeSkillDefinition nativeSkillDefinition2 = SkillBook.Native(item4.Skill, Mathf.Clamp(cast.native.Level, 1, SkillBook.MaxLevelOf(item4.Skill)));
			if (nativeSkillDefinition2 == null)
			{
				throw new InvalidOperationException("Missing replay WZ level: " + item4.Skill);
			}
			DamageResult[] array3 = array2;
			int num2 = ((item4.Warrior && item4.OriginalFirst) ? cast.native.HitCount : nativeSkillDefinition2.HitCount);
			num2 = Mathf.Clamp(num2, 1, 15);
			bool flag2 = false;
			DamageResult[] array4 = array3;
			for (int j = 0; j < array4.Length; j++)
			{
				DamageResult damageResult = array4[j];
				flag2 |= damageResult.Amount > 0;
			}
			bool flag3 = array3.Length == 0 || (IsRangedReplay(cast.native.Id) && !flag2);
			DamageResult[] template;
			if (flag3 && item4.Warrior)
			{
				template = p.ReplayFallback(cast.native, num2, close: true);
			}
			else
			{
				if (flag3 && (cast.native.Magic || IsRangedReplay(cast.native.Id)))
				{
					array3 = p.ReplayFallback(cast.native, 1, close: false);
				}
				float num3 = ((item4.Warrior && item4.OriginalFirst) ? cast.native.DamagePercent : nativeSkillDefinition2.DamagePercent);
				template = NativeSkillReplay.Adapt(array3, num2, cast.native.DamagePercent, num3, GameSession.Character?.record.damageCap ?? 19999999);
			}
			((MonoBehaviour)this).StartCoroutine(ReplayStage(cast, nativeSkillDefinition2, item4, list, template, origin, facing, fieldDirector, map));
		}
		return true;
	}

	private static bool IsRangedReplay(int id)
	{
		int num = id / 10000;
		if (num != 312 && num != 322 && num != 412 && num != 522 && num != 1312)
		{
			return num == 1412;
		}
		return true;
	}

	private List<Component> ReplayTargets(NativeSkillDefinition bounds, Vector2 origin, int facing, int count, bool fistFinish, bool unbounded = true, bool fuma = false)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = new List<Component>();
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead && Contains((Component)(object)bossUnit))
			{
				list.Add((Component)(object)bossUnit);
			}
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && Contains((Component)(object)item))
			{
				list.Add((Component)(object)item);
			}
		}
		list.Sort((Component a, Component b) =>
		{
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			bool flag = a is BossUnit || (a is EnemyUnit enemyUnit && NativeCombatData.Int(enemyUnit.CombatStats, "boss") != 0);
			bool flag2 = b is BossUnit || (b is EnemyUnit enemyUnit2 && NativeCombatData.Int(enemyUnit2.CombatStats, "boss") != 0);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			Vector2 val = Vector2.op_Implicit(a.transform.position) - origin;
			float sqrMagnitude = val.sqrMagnitude;
			val = Vector2.op_Implicit(b.transform.position) - origin;
			int num = sqrMagnitude.CompareTo(val.sqrMagnitude);
			return (num == 0) ? ((Object)a).GetInstanceID().CompareTo(((Object)b).GetInstanceID()) : num;
		});
		if (list.Count > count)
		{
			list.RemoveRange(count, list.Count - count);
		}
		return list;
		bool Contains(Component t)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			if (fuma)
			{
				Vector2 val = Vector2.op_Implicit(t.transform.position) - origin;
				if (val.x >= -3.5f && val.x < 3.5f && val.y > -3.5f)
				{
					return val.y <= 3.5f;
				}
				return false;
			}
			if (unbounded && !fistFinish && !bounds.HasBounds)
			{
				return true;
			}
			if (!fistFinish)
			{
				return InRange(t, bounds, origin, facing);
			}
			Vector2 val2 = Vector2.op_Implicit(t.transform.position) - origin;
			if (val2.x >= -25f && val2.x < 25f && val2.y > -5f)
			{
				return val2.y <= 11.25f;
			}
			return false;
		}
	}

	private IEnumerator ReplayStage(SkillDef cast, NativeSkillDefinition replay, NativeReplayStage stage, List<CapturedReplayHit> captured, DamageResult[] template, Vector2 origin, int facing, FieldDirector field, FieldMap map)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		double started = Time.realtimeSinceStartupAsDouble;
		for (int index = 0; index < stage.Times.Length; index++)
		{
			double due = started + (double)stage.Times[index] / 1000.0;
			while (Time.realtimeSinceStartupAsDouble < due)
			{
				yield return null;
			}
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing || ((Object)(object)field != (Object)null && field.Map != map))
			{
				yield break;
			}
			if (index == 0 && stage.OriginalFirst && !stage.Warrior && !stage.LiveFacing && captured.Count > 0)
			{
				foreach (CapturedReplayHit item in captured)
				{
					ApplyReplayHit(cast, cast.native, item.Target, item.Hits, origin, facing, stage);
				}
				continue;
			}
			Vector2 origin2 = (stage.Fixed ? origin : p.FeetPos);
			if (cast.native.Id == 15121002)
			{
				PlayNativeReplayEffect(replay.Id, origin2, facing);
			}
			int facing2 = (stage.LiveFacing ? p.facing : facing);
			if (replay.Id == 4121011)
			{
				origin2.x += (float)(facing * (600 * Mathf.Clamp(stage.Times[index] - 420, 0, 360) / 360)) / 40f;
			}
			NativeSkillDefinition nativeSkillDefinition = (stage.OriginalBounds ? cast.native : replay);
			int num = (stage.OriginalBounds ? cast.native.TargetCount : replay.TargetCount);
			if (cast.native.Id == 1121020)
			{
				num = Mathf.Clamp(num, 1, 8);
			}
			if (cast.native.Magic && !nativeSkillDefinition.HasBounds)
			{
				foreach (CapturedReplayHit item2 in captured)
				{
					ApplyReplayHit(cast, replay, item2.Target, item2.Hits, origin2, facing2, stage);
				}
				continue;
			}
			foreach (Component item3 in ReplayTargets(nativeSkillDefinition, origin2, facing2, Mathf.Clamp(num, 1, 15), replay.Id == 5121016, unbounded: true, replay.Id == 4121011))
			{
				ApplyReplayHit(cast, replay, item3, template, origin2, facing2, stage);
			}
		}
		if (cast.native.Id == 15121002 && replay.Id == 15121011)
		{
			spearCombo.Reset();
		}
	}

	private void ApplyReplayHit(SkillDef cast, NativeSkillDefinition replay, Component target, DamageResult[] hits, Vector2 origin, int facing, NativeReplayStage stage)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			return;
		}
		EnemyUnit enemyUnit = target as EnemyUnit;
		BossUnit bossUnit = target as BossUnit;
		if ((!((Object)(object)enemyUnit != (Object)null) || !enemyUnit.dead) && (!((Object)(object)bossUnit != (Object)null) || !bossUnit.dead))
		{
			int direction = ((target.transform.position.x >= origin.x) ? 1 : (-1));
			if ((Object)(object)enemyUnit != (Object)null)
			{
				enemyUnit.TakeReplayHit(hits, direction, replay.Magic, stage.Indexed, stage.Interval, stage.Total);
			}
			else if ((Object)(object)bossUnit != (Object)null)
			{
				bossUnit.TakeReplayHit(hits, direction, stage.Indexed, stage.Interval, stage.Total);
			}
			bool flag = false;
			for (int i = 0; i < hits.Length; i++)
			{
				DamageResult damageResult = hits[i];
				flag |= damageResult.Amount > 0;
			}
			Impact(cast, replay, target, direction, flag);
		}
	}

	private void PlayNativeReplayEffect(int id, Vector2 origin, int facing)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		string key = NativeSkillVfx.Key(id, Mathf.Max(1, SkillProgress.LevelOf(id)));
		foreach (NativeVfxClip item in NativeSkillVfx.Parts(key, "effect"))
		{
			Visual(key, item.path, Vector2.op_Implicit(origin), facing, (NativeSkillVfx.Number(key, item.path, "fix") != 0f) ? null : ((Component)p).transform, loop: false, p.SortingOrder + ((!(NativeSkillVfx.Number(key, item.path, "z") < 0f)) ? 1 : (-1)));
		}
	}

	private static int SeqInt(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	private static int[] SeqInts(Dictionary<string, object> row, string key)
	{
		if (row == null || !row.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return new int[0];
		}
		int[] array = new int[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = Convert.ToInt32(list[i]);
		}
		return array;
	}

	private static void LoadSequences()
	{
		if (sequences != null)
		{
			return;
		}
		sequences = new List<SkillSequence>();
		wounds = new List<WoundSpec>();
		TextAsset val = Resources.Load<TextAsset>("NativeSkillSequences");
		string text = ((val != null) ? val.text : null);
		sequenceData = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
		if (sequenceData.TryGetValue("sequences", out var value) && value is Dictionary<string, object> dictionary)
		{
			bool flag = default;
			bool flag2 = default;
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (!(item.Value is Dictionary<string, object> dictionary2))
				{
					continue;
				}
				Dictionary<string, object> dictionary3 = (dictionary2.TryGetValue("childSkill", out var value2) ? (value2 as Dictionary<string, object>) : null);
				string text2 = (dictionary2.TryGetValue("trigger", out var value3) ? Convert.ToString(value3) : "");
				List<SkillSequence> list = sequences;
				SkillSequence skillSequence = new SkillSequence
				{
					Parent = SeqInt(dictionary2, "parent"),
					Child = SeqInt(dictionary2, "child"),
					ChildMax = Mathf.Max(1, SeqInt(dictionary3, "maxLevel", 1)),
					DurationSec = SeqInt(dictionary2, "durationSec"),
					Charges = SeqInt(dictionary2, "charges"),
					Repeat = Mathf.Max(1, SeqInt(dictionary2, "repeat", 1)),
					ChildName = (dictionary2.TryGetValue("childName", out var value4) ? Convert.ToString(value4) : ""),
					ChildMp = SeqInt(dictionary2, "childMp"),
					Trigger = ((!(text2 == "recast")) ? ((text2 == "basicAttack") ? SeqTrigger.Basic : SeqTrigger.Chain) : SeqTrigger.Recast),
					AttackTimes = SeqInts(dictionary2, "attackTimes")
				};
				int num;
				if (dictionary3 != null && dictionary3.TryGetValue("magic", out var value5))
				{
					if (value5 is bool)
					{
						flag = (bool)value5;
						num = 1;
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 0;
				}
				skillSequence.Magic = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
				int num2;
				if (dictionary2.TryGetValue("parentAttack", out var value6))
				{
					if (value6 is bool)
					{
						flag2 = (bool)value6;
						num2 = 1;
					}
					else
					{
						num2 = 0;
					}
				}
				else
				{
					num2 = 0;
				}
				skillSequence.ParentAttack = (byte)((uint)num2 & (flag2 ? 1u : 0u)) != 0;
				skillSequence.SkillType = SeqInt(dictionary3, "skillType");
				skillSequence.AreaAttack = SeqInt(dictionary3, "areaAttack");
				skillSequence.Levels = (dictionary2.TryGetValue("childLevels", out var value7) ? (value7 as Dictionary<string, object>) : null);
				list.Add(skillSequence);
			}
		}
		if (sequenceData.TryGetValue("wounds", out var value8) && value8 is Dictionary<string, object> dictionary4)
		{
			foreach (KeyValuePair<string, object> item2 in dictionary4)
			{
				if (item2.Value is Dictionary<string, object> row)
				{
					wounds.Add(new WoundSpec
					{
						Parent = SeqInt(row, "parent"),
						MaxStacks = Mathf.Max(1, SeqInt(row, "maxStacks", 1)),
						DurationSec = SeqInt(row, "durationSec"),
						Sources = SeqInts(row, "sources")
					});
				}
			}
		}
		if (!sequenceData.TryGetValue("zones", out var value9) || !(value9 is Dictionary<string, object> dictionary5))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item3 in dictionary5)
		{
			if (item3.Value is Dictionary<string, object> dictionary6)
			{
				riftZones[SeqInt(dictionary6, "parent")] = new RiftZone
				{
					Parent = SeqInt(dictionary6, "parent"),
					Child = SeqInt(dictionary6, "child"),
					Levels = (dictionary6.TryGetValue("levels", out var value10) ? (value10 as Dictionary<string, object>) : null)
				};
			}
		}
	}

	private static SkillSequence SequenceOf(int parent, SeqTrigger trigger)
	{
		LoadSequences();
		foreach (SkillSequence sequence in sequences)
		{
			if (sequence.Parent == parent && sequence.Trigger == trigger)
			{
				return sequence;
			}
		}
		return null;
	}

	private static SkillSequence SequenceByTrigger(SeqTrigger trigger)
	{
		LoadSequences();
		foreach (SkillSequence sequence in sequences)
		{
			if (sequence.Trigger == trigger)
			{
				return sequence;
			}
		}
		return null;
	}

	private static SkillSequence FormSequenceOf(int parent)
	{
		LoadSequences();
		foreach (SkillSequence sequence in sequences)
		{
			if (sequence.Parent == parent && (sequence.Trigger == SeqTrigger.Recast || sequence.Trigger == SeqTrigger.Basic))
			{
				return sequence;
			}
		}
		return null;
	}

	private static WoundSpec WoundOf(int parent)
	{
		LoadSequences();
		foreach (WoundSpec wound in wounds)
		{
			if (wound.Parent == parent)
			{
				return wound;
			}
		}
		return null;
	}

	private static bool SequenceParentAttacks(int id)
	{
		LoadSequences();
		foreach (SkillSequence sequence in sequences)
		{
			if (sequence.Parent == id)
			{
				return sequence.ParentAttack;
			}
		}
		return false;
	}

	private static RiftZone RiftOf(int parent)
	{
		LoadSequences();
		if (!riftZones.TryGetValue(parent, out var value))
		{
			return null;
		}
		return value;
	}

	private NativeSkillDefinition SequenceChildDefinition(SkillSequence seq, int level)
	{
		if (seq == null || seq.Levels == null)
		{
			return null;
		}
		int num = Mathf.Clamp(level, 1, seq.ChildMax);
		if (!seq.Levels.TryGetValue(num.ToString(), out var value) || !(value is Dictionary<string, object> dictionary))
		{
			return null;
		}
		Dictionary<string, object> skill = new Dictionary<string, object>
		{
			{ "id", seq.Child },
			{ "kind", "Attack" },
			{ "magic", seq.Magic },
			{ "element", "" },
			{ "summonInterval", 1f },
			{ "skillType", seq.SkillType },
			{ "areaAttack", seq.AreaAttack },
			{ "vMatrix", true }
		};
		Dictionary<string, object> level2 = new Dictionary<string, object>(dictionary) { ["level"] = num };
		return new NativeSkillDefinition(skill, level2);
	}

	private void StartSequenceForm(SkillSequence seq, int level)
	{
		if (seq != null && seq.DurationSec > 0)
		{
			formUntil[seq.Parent] = Time.realtimeSinceStartupAsDouble + (double)seq.DurationSec;
			formCharges[seq.Parent] = seq.Charges;
			if (seq.Charges > 0)
			{
				Report(seq.Charges + " 次「" + seq.ChildName + "」");
			}
		}
	}

	private bool FormActive(SkillSequence seq)
	{
		if (seq != null && formUntil.TryGetValue(seq.Parent, out var value))
		{
			return Time.realtimeSinceStartupAsDouble < value;
		}
		return false;
	}

	private int FormCharges(SkillSequence seq)
	{
		if (!FormActive(seq))
		{
			return 0;
		}
		if (!formCharges.TryGetValue(seq.Parent, out var value))
		{
			return 0;
		}
		return value;
	}

	private void EndSequenceForm(int parent)
	{
		formUntil.Remove(parent);
		formCharges.Remove(parent);
	}

	private bool SpendFormCharge(SkillSequence seq)
	{
		if (seq == null)
		{
			return false;
		}
		int num = FormCharges(seq);
		if (num <= 0)
		{
			EndSequenceForm(seq.Parent);
			return false;
		}
		if (num - 1 <= 0)
		{
			EndSequenceForm(seq.Parent);
		}
		else
		{
			formCharges[seq.Parent] = num - 1;
		}
		return true;
	}

	private bool TrySequenceRecast(SkillDef s, out bool cast)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		cast = false;
		if ((Object)(object)p == (Object)null || p.hp <= 0f || s?.native == null)
		{
			return false;
		}
		SkillSequence seq = SequenceOf(s.native.Id, SeqTrigger.Recast);
		if (!FormActive(seq))
		{
			return false;
		}
		cast = CastSequenceChild(seq, s.native.Level, p.FeetPos, p.facing, null, consumeCharge: true);
		return true;
	}

	public bool TrySequenceBasicAttack()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)p == (Object)null || p.hp <= 0f)
		{
			return false;
		}
		GameLoop i = GameLoop.I;
		if ((Object)(object)i == (Object)null || i.state != GameLoop.GameState.Playing || i.gameOver)
		{
			return false;
		}
		if (p.IsClimbing)
		{
			return false;
		}
		SkillSequence skillSequence = SequenceByTrigger(SeqTrigger.Basic);
		if (!FormActive(skillSequence))
		{
			return false;
		}
		if (Time.unscaledTime < actionUntil)
		{
			return false;
		}
		return CastSequenceChild(skillSequence, SkillProgress.LevelOf(skillSequence.Parent), p.FeetPos, p.facing, null, consumeCharge: true);
	}

	private bool CastSequenceChild(SkillSequence seq, int parentLevel, Vector2 origin, int facing, Component onlyTarget, bool consumeCharge)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		NativeSkillDefinition nativeSkillDefinition = SequenceChildDefinition(seq, parentLevel);
		if (nativeSkillDefinition == null || nativeSkillDefinition.DamagePercent <= 0f)
		{
			return false;
		}
		int num = ((seq.ChildMp > 0) ? nativeSkillDefinition.MpCost : 0);
		if (num > 0 && p.mp < (float)num)
		{
			return Reject("魔力不足（需要 " + num + "）");
		}
		if (consumeCharge && !SpendFormCharge(seq))
		{
			return Reject("「" + seq.ChildName + "」次数已用尽");
		}
		if (num > 0)
		{
			p.TrySpendMp(num);
		}
		SkillDef def = new SkillDef(new ClassSkill
		{
			name = seq.ChildName,
			art = seq.Child.ToString(),
			icon = ""
		}, nativeSkillDefinition);
		p.PlayNativeSkill(seq.Child);
		PlaySequenceEffect(seq, parentLevel, origin, facing);
		LockAction(Mathf.Max(p.SkillActionDuration, 0.22f), nativeSkillDefinition);
		AudioManager.Play("Skill/" + seq.Child + "/Use", 0.9f);
		((MonoBehaviour)this).StartCoroutine(RunSequenceChild(seq, def, nativeSkillDefinition, origin, facing, onlyTarget));
		return true;
	}

	private IEnumerator RunSequenceChild(SkillSequence seq, SkillDef def, NativeSkillDefinition child, Vector2 origin, int facing, Component onlyTarget)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		bool spread = child.TargetCount <= 1 && seq.Repeat > 1 && (Object)(object)onlyTarget == (Object)null;
		List<Component> spreadDone = (spread ? new List<Component>() : null);
		for (int round = 0; round < seq.Repeat; round++)
		{
			int num = ((seq.AttackTimes != null && seq.AttackTimes.Length != 0) ? seq.AttackTimes[Mathf.Min(round, seq.AttackTimes.Length - 1)] : 0);
			if (round > 0 && num > 0)
			{
				yield return (object)new WaitForSeconds((float)num / 1000f);
			}
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing)
			{
				break;
			}
			Vector2 origin2 = (((Object)(object)onlyTarget != (Object)null) ? p.FeetPos : origin);
			Component val = onlyTarget;
			if (spread)
			{
				val = null;
				foreach (Component item in SelectTargets(def, child, origin2, p.facing, null, null))
				{
					if (!((Object)(object)item == (Object)null) && !spreadDone.Contains(item))
					{
						val = item;
						break;
					}
				}
				if ((Object)(object)val == (Object)null)
				{
					break;
				}
				spreadDone.Add(val);
			}
			HitAt(def, child, origin2, p.facing, null, val);
			RiftZone riftZone = RiftOf(seq.Parent);
			if (riftZone != null)
			{
				TouchRifts(riftZone, SelectTargets(def, child, origin2, p.facing, null, val));
			}
		}
	}

	private void TouchRifts(RiftZone zone, List<Component> targets)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (zone == null || targets == null || targets.Count == 0)
		{
			return;
		}
		int level = Mathf.Max(1, SkillProgress.LevelOf(zone.Parent));
		Dictionary<string, object> dictionary = RiftRow(zone, level);
		if (dictionary == null)
		{
			return;
		}
		int num = Mathf.Max(1, Art.Int(dictionary, "maxZones", 1));
		int num2 = Mathf.Max(1, Art.Int(dictionary, "stages", 1));
		float num3 = Mathf.Max(1f, Art.Num(dictionary, "lifeSec", 1f));
		float num4 = Art.Num(dictionary, "regenSec");
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		foreach (Component target in targets)
		{
			if ((Object)(object)target == (Object)null)
			{
				continue;
			}
			Vector2 val = Vector2.op_Implicit(target.transform.position);
			Rift rift = null;
			foreach (Rift rift2 in rifts)
			{
				Vector2 val2 = rift2.Position - val;
				if (val2.sqrMagnitude <= 2.25f)
				{
					rift = rift2;
					break;
				}
			}
			if (rift != null)
			{
				rift.Stages++;
				if (rift.Stages >= num2)
				{
					ExplodeRift(zone, rift, 100);
					rifts.Remove(rift);
				}
			}
			else if (rifts.Count < num && (!(num4 > 0f) || !(realtimeSinceStartupAsDouble - lastRiftSpawn < (double)num4)))
			{
				rifts.Add(new Rift
				{
					Position = val,
					Stages = 1,
					ExpireAt = realtimeSinceStartupAsDouble + (double)num3,
					SpawnedAt = realtimeSinceStartupAsDouble
				});
				lastRiftSpawn = realtimeSinceStartupAsDouble;
			}
		}
		if (rifts.Count > 0 && riftTender == null)
		{
			riftTender = ((MonoBehaviour)this).StartCoroutine(TendRifts(zone));
		}
	}

	private IEnumerator TendRifts(RiftZone zone)
	{
		do
		{
			yield return (object)new WaitForSeconds(0.15f);
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver)
			{
				rifts.Clear();
				riftTender = null;
				yield break;
			}
			int level = Mathf.Max(1, SkillProgress.LevelOf(zone.Parent));
			Dictionary<string, object> dictionary = RiftRow(zone, level);
			int percent = ((dictionary != null) ? Mathf.Max(0, Art.Int(dictionary, "expirePercent", 100)) : 100);
			double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			for (int num = rifts.Count - 1; num >= 0; num--)
			{
				if (!(realtimeSinceStartupAsDouble < rifts[num].ExpireAt))
				{
					ExplodeRift(zone, rifts[num], percent);
					rifts.RemoveAt(num);
				}
			}
		}
		while (rifts.Count != 0);
		riftTender = null;
	}

	private Dictionary<string, object> RiftRow(RiftZone zone, int level)
	{
		if (zone?.Levels == null)
		{
			return null;
		}
		int count = zone.Levels.Count;
		int num = Mathf.Clamp(level, 1, Mathf.Max(1, count));
		if (!zone.Levels.TryGetValue(num.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	private void ExplodeRift(RiftZone zone, Rift rift, int percent)
	{
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, object> dictionary = RiftRow(zone, Mathf.Max(1, SkillProgress.LevelOf(zone.Parent)));
		if (dictionary != null)
		{
			Dictionary<string, object> level = new Dictionary<string, object>(dictionary) { ["damage"] = (double)(Art.Num(dictionary, "damage") * (float)Mathf.Max(0, percent)) / 100.0 };
			NativeSkillDefinition nativeSkillDefinition = new NativeSkillDefinition(new Dictionary<string, object>
			{
				{ "id", zone.Parent },
				{ "kind", "Attack" },
				{ "magic", false },
				{ "element", "" },
				{ "summonInterval", 1f },
				{ "skillType", 0 },
				{ "areaAttack", 1 },
				{ "vMatrix", true }
			}, level);
			if (!(nativeSkillDefinition.DamagePercent <= 0f))
			{
				SkillDef s = new SkillDef(new ClassSkill
				{
					name = "",
					art = zone.Parent.ToString(),
					icon = ""
				}, nativeSkillDefinition);
				PlayNativeReplayEffect(zone.Parent, rift.Position, (!((Object)(object)p != (Object)null)) ? 1 : p.facing);
				HitAt(s, nativeSkillDefinition, rift.Position, (!((Object)(object)p != (Object)null)) ? 1 : p.facing);
			}
		}
	}

	private void PlaySequenceEffect(SkillSequence seq, int level, Vector2 origin, int facing)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		string key = NativeSkillVfx.Key(seq.Child, Mathf.Max(1, level));
		foreach (NativeVfxClip item in NativeSkillVfx.Parts(key, "effect"))
		{
			Visual(key, item.path, Vector2.op_Implicit(origin), facing, (NativeSkillVfx.Number(key, item.path, "fix") != 0f) ? null : ((Component)p).transform, loop: false, p.SortingOrder + ((!(NativeSkillVfx.Number(key, item.path, "z") < 0f)) ? 1 : (-1)));
		}
	}

	private void NotifySequenceHit(SkillDef cast, List<CapturedReplayHit> captured)
	{
		LoadSequences();
		if (cast?.native == null || captured == null || captured.Count == 0)
		{
			return;
		}
		WoundSpec woundSpec = null;
		foreach (WoundSpec wound in wounds)
		{
			int[] sources = wound.Sources;
			for (int i = 0; i < sources.Length; i++)
			{
				if (sources[i] == cast.native.Id)
				{
					woundSpec = wound;
					break;
				}
			}
		}
		if (woundSpec == null)
		{
			return;
		}
		Component val = null;
		long num = -1L;
		foreach (CapturedReplayHit item in captured)
		{
			Component target = item.Target;
			if (!((Object)(object)target == (Object)null) && !(target is EnemyUnit { dead: not false }) && !(target is BossUnit { dead: not false }))
			{
				long num2 = TargetMaxHp(target);
				if (num2 > num)
				{
					num = num2;
					val = target;
				}
			}
		}
		if (!((Object)(object)val == (Object)null))
		{
			double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
			if (val != woundTarget)
			{
				woundStacks = 0;
				woundTarget = val;
			}
			woundStacks = Mathf.Min(woundSpec.MaxStacks, woundStacks + 1);
			woundUntil = realtimeSinceStartupAsDouble + (double)woundSpec.DurationSec;
		}
	}

	private bool WoundReady(WoundSpec spec)
	{
		if (spec == null || (Object)(object)woundTarget == (Object)null)
		{
			return false;
		}
		if (Time.realtimeSinceStartupAsDouble >= woundUntil)
		{
			ClearWound();
			return false;
		}
		if (woundTarget is EnemyUnit { dead: not false })
		{
			ClearWound();
			return false;
		}
		if (woundTarget is BossUnit { dead: not false })
		{
			ClearWound();
			return false;
		}
		return woundStacks >= spec.MaxStacks;
	}

	private void ClearWound()
	{
		woundTarget = null;
		woundStacks = 0;
		woundUntil = 0.0;
	}

	private IEnumerator RunSequenceChain(SkillSequence seq, int parentLevel)
	{
		WoundSpec spec = WoundOf(seq.Parent);
		Component victim = woundTarget;
		if (!WoundReady(spec) || (Object)(object)victim == (Object)null)
		{
			yield break;
		}
		ClearWound();
		yield return (object)new WaitForSeconds(0.12f);
		if (!((Object)(object)p == (Object)null) && !(p.hp <= 0f) && !((Object)(object)victim == (Object)null) && !(victim is EnemyUnit { dead: not false }) && !(victim is BossUnit { dead: not false }))
		{
			Vector3 position = victim.transform.position;
			int num = ((position.x >= p.FeetPos.x) ? 1 : (-1));
			float num2 = position.x - (float)num * 1.5f;
			float groundY = PlayerCtrl.GroundAt(num2, p.FeetPos.y);
			p.PlaceAt(num2, groundY);
			p.facing = ((position.x >= num2) ? 1 : (-1));
			float num3 = Art.Num(SkillBook.Get(seq.Parent)?.Level(parentLevel), "t");
			if (num3 > 0f)
			{
				p.invulnUntil = Mathf.Max(p.invulnUntil, Time.unscaledTime + num3);
			}
			PlaySequenceEffect(seq, parentLevel, p.FeetPos, p.facing);
			CastSequenceChild(seq, parentLevel, p.FeetPos, p.facing, victim, consumeCharge: false);
		}
	}

	private void ClearSequences()
	{
		formUntil.Clear();
		formCharges.Clear();
		ClearWound();
		rifts.Clear();
		riftTender = null;
		lastRiftSpawn = 0.0;
	}

	private static void EnsureStacks()
	{
		if (stackData != null)
		{
			return;
		}
		TextAsset val = Resources.Load<TextAsset>("NativeSkillStacks");
		string text = ((val != null) ? val.text : null);
		stackData = (string.IsNullOrEmpty(text) ? new Dictionary<string, object>() : ((Dictionary<string, object>)MiniJson.Parse(text)));
		stackSpecs = new Dictionary<int, Dictionary<string, object>>();
		if (!stackData.TryGetValue("stacks", out var value) || !(value is Dictionary<string, object> dictionary))
		{
			return;
		}
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (int.TryParse(item.Key, out var result) && item.Value is Dictionary<string, object> value2)
			{
				stackSpecs[result] = value2;
			}
		}
	}

	public static bool IsStackSkill(int id)
	{
		EnsureStacks();
		return stackSpecs.ContainsKey(id);
	}

	private static Dictionary<string, object> StackRow(int id, int level)
	{
		EnsureStacks();
		if (!stackSpecs.TryGetValue(id, out var value))
		{
			return null;
		}
		Dictionary<string, object> dictionary = (value.TryGetValue("levels", out var value2) ? (value2 as Dictionary<string, object>) : null);
		if (dictionary == null || !dictionary.TryGetValue(Mathf.Clamp(level, 1, 30).ToString(), out var value3))
		{
			return null;
		}
		return value3 as Dictionary<string, object>;
	}

	private static int StackInt(Dictionary<string, object> row, string key, int fallback = 0)
	{
		if (row == null || !row.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	public int StackCooldown(int id, float defaultCd)
	{
		if (!IsStackSkill(id) || !stackExhaust || stackExhaustCooldown <= 0)
		{
			return (int)defaultCd;
		}
		return stackExhaustCooldown;
	}

	private float EffectiveCooldown(SkillDef s)
	{
		float num = StackCooldown(s.native.Id, s.cd);
		int cooldownReducePercent = CooldownReducePercent;
		if (cooldownReducePercent <= 0)
		{
			return num;
		}
		return num * (float)(100 - cooldownReducePercent) / 100f;
	}

	public void ApplyStack(SkillDef cast)
	{
		if (cast == null || cast.native == null || (Object)(object)p == (Object)null || !IsStackSkill(cast.native.Id))
		{
			return;
		}
		Dictionary<string, object> dictionary = StackRow(cast.native.Id, cast.native.Level);
		if (dictionary == null)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		int num = Mathf.Max(1, StackInt(dictionary, "maxStacks", 6));
		int num2 = StackInt(dictionary, "durationSec", 30);
		stackExhaustCooldown = StackInt(dictionary, "exhaustCooldownSec", 240);
		if (realtimeSinceStartupAsDouble > stackUntil || stackLevel == 0)
		{
			stackCount = 1;
		}
		else
		{
			stackCount = Mathf.Min(num, stackCount + 1);
		}
		stackLevel = cast.native.Level;
		stackUntil = realtimeSinceStartupAsDouble + (double)num2;
		stackExhaust = stackCount >= num;
		stackGeneration++;
		List<object> list = (dictionary.TryGetValue("effects", out var value) ? (value as List<object>) : null);
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		stackCooldownReduce = 0;
		if (list != null)
		{
			foreach (object item in list)
			{
				if (!(item is Dictionary<string, object> dictionary3) || StackInt(dictionary3, "layer") > stackCount)
				{
					continue;
				}
				int value2 = StackInt(dictionary3, "value");
				if (!dictionary3.TryGetValue("stats", out var value3) || !(value3 is List<object> list2))
				{
					continue;
				}
				foreach (object item2 in list2)
				{
					string text = Convert.ToString(item2);
					if (text == "CooldownReduce")
					{
						stackCooldownReduce = value2;
					}
					else
					{
						dictionary2[text] = value2;
					}
				}
			}
		}
		dictionary2["STACK_BLESSING"] = stackCount;
		NativeBuffs.For(p).ApplyStack(cast.native.Id, cast.native.Level, dictionary2, num2 * 1000, realtimeSinceStartupAsDouble);
		long generation = stackGeneration;
		((MonoBehaviour)this).StartCoroutine(StackExpiry(cast.native.Id, generation));
		if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
		{
			Debug.Log((object)("[Stack] " + cast.native.Id + " layer=" + stackCount + "/" + num + " cdReduce=" + stackCooldownReduce + " stats=" + string.Join(",", dictionary2.Keys)));
		}
	}

	private IEnumerator StackExpiry(int id, long generation)
	{
		while (Time.realtimeSinceStartupAsDouble < stackUntil)
		{
			if (stackGeneration != generation)
			{
				yield break;
			}
			yield return null;
		}
		if (stackGeneration == generation && !((Object)(object)p == (Object)null))
		{
			stackCount = 0;
			stackLevel = 0;
			stackCooldownReduce = 0;
			stackExhaust = false;
			NativeBuffs.For(p).Cancel(id);
			State(id).TryStart(Time.realtimeSinceStartupAsDouble, stackExhaustCooldown);
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Stack] expired → cooldown " + stackExhaustCooldown + "s"));
			}
		}
	}

	private void ClearStacks()
	{
		stackGeneration++;
		stackCount = 0;
		stackLevel = 0;
		stackCooldownReduce = 0;
		stackUntil = 0.0;
		stackExhaust = false;
	}

	private void PlayTremble(SkillDef s)
	{
		if (s?.native == null || !Trembles.TryGetValue(s.native.Id, out var value))
		{
			return;
		}
		foreach (TrembleWave wave in value.Waves)
		{
			((MonoBehaviour)this).StartCoroutine(TrembleAfter(wave));
		}
	}

	private IEnumerator TrembleAfter(TrembleWave wave)
	{
		if (wave.Delay > 0f)
		{
			yield return (object)new WaitForSeconds(wave.Delay);
		}
		if (!((Object)(object)p == (Object)null) && !(p.hp <= 0f) && !((Object)(object)GameLoop.I == (Object)null) && GameLoop.I.state == GameLoop.GameState.Playing)
		{
			HitFeel.Shake(wave.Force / 40f, wave.Seconds);
			if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
			{
				Debug.Log((object)("[Tremble] force=" + wave.Force + " time=" + wave.Seconds * 1000f + "ms delay=" + wave.Delay * 1000f + "ms amp=" + (wave.Force / 40f).ToString("0.000")));
			}
		}
	}

	public static bool TrembleSpecOf(int skillId, out float force, out float seconds, out float delay)
	{
		force = (seconds = (delay = 0f));
		if (!Trembles.TryGetValue(skillId, out var value) || value.Waves.Count == 0)
		{
			return false;
		}
		TrembleWave trembleWave = value.Waves[0];
		force = trembleWave.Force;
		seconds = trembleWave.Seconds;
		delay = trembleWave.Delay;
		return true;
	}

	public static int TrembleWaveCount(int skillId)
	{
		if (!Trembles.TryGetValue(skillId, out var value))
		{
			return 0;
		}
		return value.Waves.Count;
	}

	public bool TryCastBoundSkill(int id, GameAction action)
	{
		boundHeldAction = action;
		try
		{
			return TryCastSkill(id);
		}
		finally
		{
			boundHeldAction = null;
		}
	}

	private void StartHeld(SkillDef definition, GameAction action, bool charging = false)
	{
		heldSkill = definition;
		heldAction = action;
		heldCharging = charging;
		heldReady = false;
		heldFired = false;
		p.PlaySkill(definition.action);
		heldPreparation = ((MonoBehaviour)this).StartCoroutine(HeldVisuals(definition));
		AudioManager.Play("Skill/" + definition.native.Id + "/Use", 0.9f);
	}

	private IEnumerator HeldVisuals(SkillDef definition)
	{
		foreach (string item in NativeSkillVfx.Layers(definition.vfx, "prepare"))
		{
			foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(definition.vfx, item))
			{
				heldVisuals.Add(Visual(definition.vfx, item2.path, Vector2.op_Implicit(p.FeetPos), p.facing, ((Component)p).transform));
			}
		}
		float num = Preparation(definition);
		if (num > 0f)
		{
			yield return (object)new WaitForSeconds(num);
		}
		if (heldSkill != definition)
		{
			yield break;
		}
		foreach (string item3 in NativeSkillVfx.Layers(definition.vfx, "keydown"))
		{
			foreach (NativeVfxClip item4 in NativeSkillVfx.Parts(definition.vfx, item3))
			{
				heldVisuals.Add(Visual(definition.vfx, item4.path, Vector2.op_Implicit(p.FeetPos), p.facing, ((Component)p).transform, loop: true, (NativeSkillVfx.Number(definition.vfx, item4.path, "z") < 0f) ? (p.SortingOrder - 1) : 360));
			}
		}
		heldPreparation = null;
		heldReady = true;
		BeginVisuals(definition, bodyOnly: true);
	}

	private void StopHeld(bool release)
	{
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		SkillDef skillDef = heldSkill;
		heldSkill = null;
		bool flag = heldCharging;
		heldCharging = false;
		heldReady = false;
		if (heldPreparation != null)
		{
			((MonoBehaviour)this).StopCoroutine(heldPreparation);
			heldPreparation = null;
		}
		foreach (SkillFx heldVisual in heldVisuals)
		{
			if ((Object)(object)heldVisual != (Object)null)
			{
				heldVisual.Stop();
			}
		}
		heldVisuals.Clear();
		if (skillDef == null)
		{
			return;
		}
		p?.FinishHeldSkill();
		ClearActionLock();
		if (!release || !((Object)(object)p != (Object)null))
		{
			return;
		}
		foreach (string item in NativeSkillVfx.Layers(skillDef.vfx, "keydownend"))
		{
			foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(skillDef.vfx, item))
			{
				Visual(skillDef.vfx, item2.path, Vector2.op_Implicit(p.FeetPos), p.facing, ((Component)p).transform);
			}
		}
		if (flag && skillDef.native != null)
		{
			TryCastSkill(skillDef.native.Id);
		}
		else if (!flag && !heldFired && skillDef.native != null)
		{
			BeginCast(skillDef, continuousShot: true);
		}
		if (!flag)
		{
			StartFinisher(skillDef);
		}
	}

	private void Update()
	{
		if (heldSkill == null)
		{
			return;
		}
		GameLoop i = GameLoop.I;
		if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)i == (Object)null || i.gameOver || i.state != GameLoop.GameState.Playing)
		{
			StopHeld(release: false);
			return;
		}
		int num = QuickSlots.Index(heldAction);
		if (!InputHub.Held(heldAction) || num < 0 || QuickSlots.Id(num) != heldSkill.native.Id || PlayerCtrl.InputLocked || PlayerCtrl.ScriptUiLock > 0 || NpcSession.Blocking || KeyConfigWindow.Visible || VirtualControlsEditor.Visible)
		{
			StopHeld(release: true);
		}
		else
		{
			if (Time.deltaTime <= 0f || heldCharging || !heldReady)
			{
				return;
			}
			long now = (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
			if (NativeHeldSkill.ShotDue(now, lastHeldShot, (heldSkill.native != null) ? heldSkill.native.AttackDelay : 0))
			{
				lastHeldShot = now;
				if (!BeginCast(heldSkill, continuousShot: true))
				{
					Report(LastFailure);
					StopHeld(release: true);
				}
				else
				{
					heldFired = true;
				}
			}
		}
	}

	private void LockAction(float seconds, NativeSkillDefinition definition)
	{
		actionUntil = Time.unscaledTime + seconds;
		actionSkillId = definition?.Id ?? 0;
		actionBlocking = definition != null && (definition.Kind == "Attack" || definition.Kind == "Pull" || definition.DamagePercent > 0f);
	}

	private void ClearActionLock()
	{
		actionUntil = 0f;
		actionBlocking = false;
		actionSkillId = 0;
	}

	private void Awake()
	{
		p = ((Component)this).GetComponent<PlayerCtrl>();
		NativeBuffs.For(p).Removed += RemoveBuffSummon;
		ClassSkill[] array = ClassDefs.Get(Profile.cls).skills;
		for (int i = 0; i < array.Length; i++)
		{
			ClassSkill presentation = array[i];
			NativeSkillDefinition nativeSkillDefinition = NativeSkillCatalog.Get(presentation.art);
			if (nativeSkillDefinition == null)
			{
				throw new InvalidOperationException("缺少技能等级数据：" + presentation.art);
			}
			definitions.Add(new SkillDef(presentation, nativeSkillDefinition));
			if (!states.ContainsKey(nativeSkillDefinition.Id))
			{
				states.Add(nativeSkillDefinition.Id, new SkillRuntimeState());
			}
		}
	}

	public float Remaining(int slot)
	{
		return (float)states[definitions[slot].native.Id].Remaining(Time.realtimeSinceStartupAsDouble);
	}

	public float RemainingById(int id)
	{
		if (!states.TryGetValue(id, out var value))
		{
			return 0f;
		}
		return (float)value.Remaining(Time.realtimeSinceStartupAsDouble);
	}

	public void ReduceCooldowns(int percent)
	{
		if (percent <= 0)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		foreach (SkillRuntimeState value in states.Values)
		{
			value.Reduce(realtimeSinceStartupAsDouble, percent);
		}
	}

	public float CooldownOf(int id)
	{
		if (SkillBook.Get(id) == null)
		{
			return 0f;
		}
		int level = Mathf.Max(1, SkillProgress.LevelOf(id));
		return SkillBook.Native(id, level)?.Cooldown ?? 0f;
	}

	public void BindCooldowns(Dictionary<int, SkillRuntimeState> characterStates)
	{
		states = characterStates;
		foreach (SkillDef definition in definitions)
		{
			if (!states.ContainsKey(definition.native.Id))
			{
				states.Add(definition.native.Id, new SkillRuntimeState());
			}
		}
	}

	private bool Reject(string reason)
	{
		LastFailure = reason;
		return false;
	}

	public void Report(string text)
	{
		notice = text;
		noticeUntil = Time.unscaledTime + 1.8f;
	}

	private SkillRuntimeState State(int id)
	{
		if (!states.TryGetValue(id, out var value))
		{
			value = new SkillRuntimeState();
			states[id] = value;
		}
		return value;
	}

	public bool TryCast(int slot)
	{
		LastFailure = null;
		if (slot < 0 || slot >= definitions.Count || (Object)(object)p == (Object)null)
		{
			return Reject("技能不可用");
		}
		return BeginCast(definitions[slot]);
	}

	public bool TryCastSkill(int skillId)
	{
		LastFailure = null;
		if (NativeSoulWeapon.IsSkill(skillId))
		{
			string text = NativeSoulWeapon.Cast(skillId);
			if (text != null)
			{
				return Reject(text);
			}
			return true;
		}
		SkillData skillData = SkillBook.Get(skillId);
		if (skillData == null)
		{
			return Reject("缺少技能数据");
		}
		int num = SkillProgress.LevelOf(skillId);
		if (num <= 0)
		{
			return Reject("技能尚未学习（技能窗里加点）");
		}
		if (NativeMount.IsSkill(skillId))
		{
			if (!((Object)(object)p == (Object)null) && !(p.hp <= 0f))
			{
				GameLoop i = GameLoop.I;
				if (i != null && i.state == GameLoop.GameState.Playing && !GameLoop.I.gameOver)
				{
					string text2 = NativeMount.Toggle(p, skillId, num);
					if (text2 != null)
					{
						return Reject(text2);
					}
					return true;
				}
			}
			return Reject("当前无法骑乘");
		}
		if (skillId == 1016)
		{
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I?.field == (Object)null)
			{
				return Reject("当前无法放置轮回碑石。");
			}
			if (Profile.EquipOfSlot(-53) != 1189999)
			{
				return Reject("请先装备轮回碑石。");
			}
			if (State(1016).Remaining(Time.realtimeSinceStartupAsDouble) > 0.0)
			{
				return Reject("技能冷却中");
			}
			string text3 = GameLoop.I.field.ActivateFrenzy(p);
			if (text3 != null)
			{
				return Reject(text3);
			}
			State(1016).TryStart(Time.realtimeSinceStartupAsDouble, 1200.0);
			p.PlayNativeSkill(1016);
			return true;
		}
		if (NativeArrowMode.IsToggle(skillId))
		{
			if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I?.field == (Object)null || GameLoop.I.state != GameLoop.GameState.Playing || GameLoop.I.gameOver)
			{
				return Reject("当前无法切换箭镞");
			}
			if (!NativeArrowMode.CooledDown)
			{
				return Reject("箭镞切换中");
			}
			string text4 = NativeArrowMode.Switch(p);
			Report(text4);
			return true;
		}
		if (skillData.IsPassive && !InstantSelfSkill(skillId))
		{
			return Reject("被动技能无需施放");
		}
		NativeSkillDefinition nativeSkillDefinition = SkillBook.Native(skillId, num);
		if (nativeSkillDefinition == null)
		{
			return Reject("技能等级数据缺失");
		}
		string art = skillId.ToString();
		foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)WzAssets.Index["skills"])
		{
			if (Convert.ToInt32(item.Value) == skillId)
			{
				art = item.Key;
				break;
			}
		}
		SkillDef s = new SkillDef(new ClassSkill
		{
			name = skillData.name,
			art = art,
			icon = skillData.icon
		}, nativeSkillDefinition);
		return BeginCast(s);
	}

	private bool BeginCast(SkillDef s, bool continuousShot = false)
	{
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0942: Unknown result type (might be due to invalid IL or missing references)
		//IL_0974: Unknown result type (might be due to invalid IL or missing references)
		//IL_097a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_088b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dfc: Unknown result type (might be due to invalid IL or missing references)
		LastFailure = null;
		s = ResolveMechanicReplacement(s);
		GameLoop i = GameLoop.I;
		if ((Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)i == (Object)null || i.state != GameLoop.GameState.Playing || i.gameOver)
		{
			return Reject("当前无法释放技能");
		}
		if (p.IsClimbing)
		{
			return Reject("攀爬时无法释放技能");
		}
		NativeMobDebuffs component = ((Component)p).GetComponent<NativeMobDebuffs>();
		if (component != null && component.BlocksSkill && !NativeBuffs.IsHerosWill(s.native?.Id ?? 0))
		{
			return Reject("异常状态下无法释放技能");
		}
		if (heldSkill != null && !continuousShot)
		{
			if (s.native == null || s.native.Id == heldSkill.native.Id)
			{
				return Reject("正在持续释放技能");
			}
			StopHeld(release: false);
		}
		if (!continuousShot && Time.unscaledTime < actionUntil && (actionBlocking || (s.native != null && s.native.Id == actionSkillId)))
		{
			return Reject("正在释放技能");
		}
		if (s.native == null)
		{
			return Reject("技能等级数据缺失");
		}
		bool flag = !continuousShot;
		if (!continuousShot && TrySequenceRecast(s, out var cast))
		{
			return cast;
		}
		if (NativeMount.Item(p) > 0)
		{
			return Reject("请先解除骑乘状态");
		}
		if (s.native.Kind == "Passive" && !InstantSelfSkill(s.native.Id))
		{
			return Reject("被动技能无需施放");
		}
		if (s.native.Kind == "FlashJump" && !p.CanUseFlashJump(s.native.Id))
		{
			return Reject("当前无法使用二段跳");
		}
		Vector2 destination = default;
		if (s.native.Kind == "Teleport" && !p.TeleportDestination(s.native, out destination))
		{
			LastFailure = null;
			return false;
		}
		bool flag2 = s.native.Id == 4121023 && fourSeasonsFrenzy;
		if (flag && !flag2 && State(s.native.Id).Remaining(Time.realtimeSinceStartupAsDouble) > 0.0)
		{
			return Reject("技能冷却中");
		}
		NativeBuffs nativeBuffs = NativeBuffs.For(p);
		if (nativeBuffs.Value("MORPH") > 0 && nativeBuffs.Value("MORPH") < 100 && (s.native.Kind == "Attack" || s.native.Kind == "Pull"))
		{
			return Reject("变身状态下无法攻击");
		}
		int num = s.native.MpCost;
		int id;
		if (Profile.JobId == 211 || Profile.JobId == 212)
		{
			id = 2110001;
		}
		else if (Profile.JobId == 221 || Profile.JobId == 222)
		{
			id = 2210001;
		}
		else
		{
			id = ((Profile.JobId == 1211 || Profile.JobId == 1212) ? 12110001 : ((Profile.JobId >= 2215 && Profile.JobId <= 2218) ? 22150000 : 0));
		}
		int num2 = SkillProgress.LevelOf(id);
		if (num2 > 0)
		{
			num = (int)((double)num * ((double)Art.Int(SkillBook.Get(id).Level(num2), "x") / 100.0));
		}
		num = ((!nativeBuffs.HasStat("INFINITY")) ? (num - (int)((double)num * ((double)nativeBuffs.Value("CONCENTRATE") / 100.0))) : 0);
		if (flag && p.mp < (float)num)
		{
			return Reject("魔力不足（需要 " + s.native.MpCost + "）");
		}
		if (flag && s.native.HpCost > 0 && p.hp <= (float)s.native.HpCost)
		{
			return Reject("生命不足（需要大于 " + s.native.HpCost + "）");
		}
		Dictionary<string, object> d = SkillBook.Get(s.native.Id)?.Level(s.native.Level);
		bool flag3 = s.native.Kind == "Attack" && !s.native.Magic && NativeProjectiles.ConsumesAmmo(s.native.Id);
		NativeProjectiles.Cost cost = (flag3 ? NativeProjectiles.For(p, s.native.Id, s.native.Level) : default(NativeProjectiles.Cost));
		if ((flag & flag3) && !cost.Allowed)
		{
			return Reject("缺少可用弹药");
		}
		int id2 = Art.Int(d, "itemCon");
		int num3 = Art.Int(d, "itemConNo");
		if (flag && num3 > 0 && Profile.ItemCount(id2) < num3)
		{
			return Reject("技能所需物品不足");
		}
		int num4 = 0;
		int num5 = 0;
		int num6 = ((s.native.Id == 4121006) ? Art.Int(d, "bulletConsume") : 0);
		if (num6 > 0)
		{
			if (GameSession.InGame)
			{
				foreach (InventoryEntry item in GameSession.Character.inventory)
				{
					if (item.inventoryType == 2 && item.position > 0 && item.itemId / 10000 == 207 && item.quantity >= num6 && (num5 == 0 || item.position < num5))
					{
						num4 = item.itemId;
						num5 = item.position;
					}
				}
			}
			else
			{
				foreach (int item2 in Profile.InventoryItems(1))
				{
					if (item2 / 10000 == 207 && Profile.ItemCount(item2) >= num6)
					{
						num4 = item2;
						break;
					}
				}
			}
			if (flag && num4 == 0)
			{
				return Reject("技能所需飞镖不足");
			}
		}
		bool flag4 = !continuousShot && boundHeldAction.HasValue && (NativeHeldSkill.Continuous(s.native.Id) || NativeSkillVfx.RapidAttack(s.native.Id));
		if (!continuousShot && boundHeldAction.HasValue && !flag4 && NativeSkillVfx.HasLayer(s.vfx, "keydown") && NativeSkillVfx.HasLayer(s.vfx, "keydownend"))
		{
			StartHeld(s, boundHeldAction.Value, charging: true);
			return true;
		}
		string text = MechanicCastGate(s.native.Id, s.native.Level);
		if (text != null)
		{
			return Reject(text);
		}
		if (flag && num3 > 0 && !Profile.RemoveItem(id2, num3))
		{
			return Reject("技能所需物品扣除失败");
		}
		if (flag && num6 > 0 && !Profile.ConsumeProjectile(num4, num6, num5))
		{
			return Reject("技能所需飞镖扣除失败");
		}
		if ((flag & flag3) && !NativeProjectiles.Spend(cost))
		{
			return Reject("弹药扣除失败");
		}
		if (flag && !flag2 && !State(s.native.Id).TryStart(Time.realtimeSinceStartupAsDouble, EffectiveCooldown(s)))
		{
			return Reject("技能冷却中");
		}
		if (flag)
		{
			p.TrySpendMp(num);
			float hp = p.hp;
			p.hp -= s.native.HpCost;
			NativePetAutopot.OnLoss(p, hp, p.mp);
		}
		if (flag && s.native.Id == 4111004)
		{
			int num7 = Art.Int(d, "moneyCon");
			if (num7 > 0)
			{
				Profile.Spend(Math.Min(Profile.gold, (long)num7 + (long)Random.Range(0, num7 / 2)));
			}
		}
		if (flag4)
		{
			StartHeld(s, boundHeldAction.Value);
			return true;
		}
		if (continuousShot)
		{
			if (UsesProjectile(s))
			{
				((MonoBehaviour)this).StartCoroutine(ProjectileCast(s, p.FeetPos, p.facing, immediate: true));
			}
			else
			{
				HitAt(s, s.native, p.FeetPos, p.facing);
			}
			return true;
		}
		ApplyInstantSelf(s.native);
		NativeSkillVideo.PlaySkill(s.native.Id, p);
		StartRepeatingAttack(s);
		StartCurse(s);
		PlayTremble(s);
		ApplyStack(s);
		StartProcBuff(s);
		if (s.native.Kind == "Teleport")
		{
			NativeImgAnimation.Spawn("Effect/BasicEff.img/Teleport", null, Vector2.op_Implicit(p.FeetPos), p.SortingOrder + 1, p.facing > 0);
			p.PlaceAt(destination.x, destination.y);
			LockAction(0.12f, s.native);
			AudioManager.Play("Skill/" + s.native.Id + "/Use", 0.9f);
			return true;
		}
		if (s.native.Kind == "FlashJump")
		{
			p.ApplyFlashJump(s.native.Level, s.native.Id);
			BeginVisuals(s);
			foreach (NativeVfxClip item3 in NativeSkillVfx.Parts(s.vfx, "movement"))
			{
				Visual(s.vfx, item3.path, Vector2.op_Implicit(p.FeetPos), p.facing);
			}
			AudioManager.Play("Skill/" + s.native.Id + "/Use", 0.9f);
			return true;
		}
		bool flag5 = !string.IsNullOrEmpty(s.art);
		float num8 = (flag5 ? Preparation(s) : 0f);
		if (flag5)
		{
			p.PlaySkill((num8 > 0f) ? (s.action + "_prepare") : s.action, num8);
			LockAction(NativeWarriorMelee(s.native.Id) ? p.SkillActionDuration : Mathf.Max(p.SkillActionDuration, num8 + p.GetSkillActionDuration(s.action)), s.native);
			BeginVisuals(s);
		}
		else
		{
			p.PlayNativeSkill(s.native.Id);
			PlayNativeReplayEffect(s.native.Id, p.FeetPos, p.facing);
			LockAction(Mathf.Max(p.SkillActionDuration, 0.32f), s.native);
		}
		bool flag6 = BeginReplay(s, p.FeetPos, p.facing);
		PlayAddAttack(s);
		NativeBuffs.For(p).ApplySkill(s.native.Id, s.native.Level, NativeBuffs.Now);
		NativeSkillStates.For(p).Activate(s.native.Id, s.native.Level);
		SkillSequence skillSequence = FormSequenceOf(s.native.Id);
		if (skillSequence != null)
		{
			StartSequenceForm(skillSequence, s.native.Level);
		}
		SkillSequence skillSequence2 = SequenceOf(s.native.Id, SeqTrigger.Chain);
		if (skillSequence2 != null)
		{
			((MonoBehaviour)this).StartCoroutine(RunSequenceChain(skillSequence2, s.native.Level));
		}
		OnMechanicCast(s, p.FeetPos, p.facing);
		StartFieldZones(s, p.FeetPos, p.facing);
		NativeShields nativeShields = NativeShields.For(p);
		if (nativeShields.Activate(s.native.Id, s.native.Level, p.hpMax))
		{
			BottomBar.Toast("护盾 " + Mathf.RoundToInt(nativeShields.Remaining));
		}
		if (NativeQuiverBurst.IsSkill(s.native.Id))
		{
			NativeQuiverBurst.Activate(s.native.Level);
		}
		NativeSpecialCore.OnSkillUse(p);
		if (s.native.Kind == "Summon")
		{
			RemoveBuffSummon(s.native.Id);
			summons[s.native.Id] = NativeSummon.Spawn(this, p, s);
		}
		AudioManager.Play("Skill/" + s.native.Id + "/Use", 0.9f);
		if (flag6)
		{
			return true;
		}
		if (InstantSelfSkill(s.native.Id))
		{
			return true;
		}
		if (s.art == "dash")
		{
			((MonoBehaviour)this).StartCoroutine(Rush(s));
		}
		else
		{
			((MonoBehaviour)this).StartCoroutine(Cast(s, p.FeetPos, p.facing));
		}
		return true;
	}

	public static bool InstantSelfSkill(int id)
	{
		if (id != 2301002 && id != 5101005 && id != 4211001 && id != 2311001 && id != 5121010 && id != 9101000 && !NativeBuffs.IsHerosWill(id))
		{
			return NativeArrowMode.IsToggle(id);
		}
		return true;
	}

	private void ApplyInstantSelf(NativeSkillDefinition d)
	{
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, object> dictionary = SkillBook.Get(d.Id)?.Level(d.Level);
		if (dictionary == null)
		{
			return;
		}
		int num = Art.Int(dictionary, "hp");
		if (num != 0)
		{
			p.Heal((int)(p.hpMax * (float)num / 100f), skill: true);
		}
		int num2 = Art.Int(dictionary, "mp");
		if (num2 != 0)
		{
			p.mp = Mathf.Min(p.mpMax, p.mp + (float)num2);
		}
		if (d.Id == 4211001)
		{
			double num3 = (double)Art.Int(dictionary, "y") / 100.0;
			int num4 = (int)((double)p.Attributes.luk * 2.3 * num3);
			int num5 = (int)((double)p.Attributes.luk * 3.5 * num3);
			p.hp = Mathf.Min(p.hpMax, p.hp + (float)num4 + (float)Mathf.FloorToInt(Random.value * (float)(num5 - num4 + 1)));
		}
		if (d.Id == 5101005)
		{
			int num6 = Art.Int(dictionary, "x");
			int num7 = ((num6 > 0) ? Mathf.Min((int)p.hp - 1, (int)p.hpMax / num6) : 0);
			p.hp -= num7;
			p.mp = Mathf.Min(p.mpMax, p.mp + (float)(int)((double)num7 * ((double)Art.Int(dictionary, "y") / 100.0)));
		}
		if ((d.Id == 2311001 || d.Id == 9101000) && Random.value < d.Probability)
		{
			((Component)p).GetComponent<NativeMobDebuffs>()?.Dispel();
		}
		else if (NativeBuffs.IsHerosWill(d.Id))
		{
			((Component)p).GetComponent<NativeMobDebuffs>()?.Purge();
		}
		if (d.Id == 9101000)
		{
			p.hp = p.hpMax;
			p.mp = p.mpMax;
		}
		if (d.Id == 5121010)
		{
			foreach (KeyValuePair<int, SkillRuntimeState> state in states)
			{
				if (state.Key != 5121010)
				{
					state.Value.Reset();
				}
			}
		}
		if (d.Id != 2311001)
		{
			return;
		}
		int num8 = 0;
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && InRange(((Component)item).transform.position, d, p.FeetPos, p.facing))
			{
				item.NativeController?.Dispel(d.Id);
				if (++num8 >= d.TargetCount)
				{
					break;
				}
			}
		}
	}

	private IEnumerator Cast(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (NativeWarriorMelee(s.native.Id))
		{
			List<Component> list = SelectTargets(s, s.native, origin, facing, null, null);
			int num = Mathf.Min(s.native.TargetCount, list.Count);
			for (int i = 0; i < num; i++)
			{
				int num2 = Mathf.RoundToInt(Mathf.Abs(list[i].transform.position.x - origin.x) * 40f);
				int num3 = p.MeleeSkillImpactMs + (int)Math.Round((double)num2 * 1.5);
				((MonoBehaviour)this).StartCoroutine(MeleeTarget(s, list[i], origin, facing, (float)num3 / 1000f));
			}
			yield break;
		}
		float num4 = Preparation(s);
		if (num4 > 0f)
		{
			yield return (object)new WaitForSeconds(num4);
		}
		if (s.native.Kind == "Summon" || (s.native.Kind == "Buff" && !SequenceParentAttacks(s.native.Id)))
		{
			yield break;
		}
		if (UsesProjectile(s) && !IsRepeatingSkill(s.native.Id))
		{
			yield return ProjectileCast(s, origin, facing);
			yield break;
		}
		float num5;
		if (s.art == "doom")
		{
			num5 = NativeSkillVfx.Get(s.vfx, "effect").Duration * 0.5f;
		}
		else if (s.art == "firebolt")
		{
			num5 = 0.45f;
		}
		else if (s.art == "arrowrain")
		{
			num5 = 0.5f;
		}
		else
		{
			num5 = ((s.art == "starfall") ? (NativeSkillVfx.Number(s.vfx, "hit/0", "hitAfter", 1800f) / 1000f) : (SkillFxPlan.Get(s.art)?.impact ?? 0f));
		}
		if (num5 > 0f)
		{
			yield return (object)new WaitForSeconds(num5);
		}
		HitAt(s, s.native, origin, facing);
		StartThunderSeries(s, origin, facing);
	}

	private static bool NativeWarriorMelee(int id)
	{
		if (id != 1001004)
		{
			return id == 1001005;
		}
		return true;
	}

	private IEnumerator MeleeTarget(SkillDef skill, Component target, Vector2 origin, int facing, float delay)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (delay > 0f)
		{
			yield return (object)new WaitForSeconds(delay);
		}
		if ((Object)(object)target != (Object)null)
		{
			HitAt(skill, skill.native, origin, facing, null, target);
		}
	}

	private IEnumerator Rush(SkillDef s)
	{
		p.BeginRush(s.native, p.SkillActionDuration);
		HashSet<int> struck = new HashSet<int>();
		HitAt(s, s.native, p.FeetPos, p.facing, struck);
		while (p.IsRushing)
		{
			yield return null;
			HitAt(s, s.native, p.FeetPos, p.facing, struck);
		}
	}

	private bool InRange(Vector3 position, NativeSkillDefinition d, Vector2 origin, int facing)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		if (d.FullScreen)
		{
			Camera main = Camera.main;
			if ((Object)(object)main == (Object)null)
			{
				return true;
			}
			float num = main.orthographicSize * main.aspect + 1f;
			return Mathf.Abs(position.x - ((Component)main).transform.position.x) <= num;
		}
		Vector2 val = Vector2.op_Implicit(position) - origin;
		float num2 = val.x * (float)facing;
		Vector2 val2 = d.Min;
		Vector2 val3 = d.Max;
		if (d.Kind == "Summon")
		{
			Nullable val4 = NativeSummon.AttackRange(d);
			if (!val4.HasValue)
			{
				return false;
			}
			Rect value = val4.Value;
			val2 = value.min / 40f;
			value = val4.Value;
			val3 = value.max / 40f;
		}
		if (num2 >= 0f - val3.x && num2 <= 0f - val2.x && val.y >= 0f - val3.y)
		{
			return val.y <= 0f - val2.y;
		}
		return false;
	}

	private bool InRange(Component target, NativeSkillDefinition d, Vector2 origin, int facing)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if (InRange(target.transform.position, d, origin, facing))
		{
			return true;
		}
		if (d.Kind == "Summon" || !d.HasBounds || !(target is EnemyUnit { SelectionBounds: not null } enemyUnit))
		{
			return false;
		}
		float num = ((facing < 0) ? d.Min.x : (0f - d.Max.x));
		float num2 = ((facing < 0) ? d.Max.x : (0f - d.Min.x));
		Rect val = Rect.MinMaxRect(origin.x + num, origin.y - d.Max.y, origin.x + num2, origin.y - d.Min.y);
		return val.Overlaps(enemyUnit.SelectionBounds.Value);
	}

	private List<Component> SelectTargets(SkillDef s, NativeSkillDefinition d, Vector2 origin, int facing, HashSet<int> struck, Component onlyTarget)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = new List<Component>();
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead && ((Object)(object)onlyTarget == (Object)(object)bossUnit || ((Object)(object)onlyTarget == (Object)null && InRange((Component)(object)bossUnit, d, origin, facing))))
			{
				list.Add((Component)(object)bossUnit);
			}
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && ((Object)(object)onlyTarget == (Object)(object)item || ((Object)(object)onlyTarget == (Object)null && InRange((Component)(object)item, d, origin, facing))))
			{
				list.Add((Component)(object)item);
			}
		}
		list.Sort((Component a, Component b) =>
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			if (s.art == "illusion" && a is BossUnit != b is BossUnit)
			{
				if (!(a is BossUnit))
				{
					return 1;
				}
				return -1;
			}
			Vector3 val = a.transform.position - Vector2.op_Implicit(origin);
			float sqrMagnitude = val.sqrMagnitude;
			val = b.transform.position - Vector2.op_Implicit(origin);
			return sqrMagnitude.CompareTo(val.sqrMagnitude);
		});
		list.RemoveAll((Component t) => struck != null && struck.Contains(((Object)t).GetInstanceID()));
		return list;
	}

	internal int HitAt(SkillDef s, NativeSkillDefinition d, Vector2 origin, int facing, HashSet<int> struck = null, Component onlyTarget = null)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
		if (d == null || (Object)(object)p == (Object)null || p.hp <= 0f || (Object)(object)GameLoop.I == (Object)null || GameLoop.I.gameOver || GameLoop.I.state != GameLoop.GameState.Playing)
		{
			return 0;
		}
		NativeMobDebuffs component = ((Component)p).GetComponent<NativeMobDebuffs>();
		if (component != null && component.BlocksSkill)
		{
			return 0;
		}
		using (Profile.DeferSaves())
		{
			using (GameServer.Local?.Db.DeferSaves())
			{
				List<Component> list = SelectTargets(s, d, origin, facing, struck, onlyTarget);
				if (SeedTowerCompat.HandleAttack(d.Id, (list.Count > 0) ? (list[0] as EnemyUnit) : null))
				{
					return 0;
				}
				int num = ((!(d.Kind == "Summon")) ? (d.TargetCount + Mathf.Max(0, NativeSkillEnhance.TargetPlus(d.Id))) : (NativeSummon.AttackInfo(d)?.Int("mobCount", 1) ?? 1));
				if (struck != null)
				{
					num = Mathf.Max(0, num - struck.Count);
				}
				int num2 = Mathf.Min(num, list.Count);
				long num3 = 0L;
				List<CapturedReplayHit> list2 = new List<CapturedReplayHit>();
				for (int i = 0; i < num2; i++)
				{
					EnemyUnit enemyUnit = list[i] as EnemyUnit;
					BossUnit bossUnit = list[i] as BossUnit;
					int num4 = (((Object)(object)onlyTarget != (Object)null) ? facing : ((list[i].transform.position.x >= origin.x) ? 1 : (-1)));
					struck?.Add(((Object)list[i]).GetInstanceID());
					if (d.Id == 4111004)
					{
						enemyUnit?.NativeController?.Dispel(d.Id);
					}
					if (d.Kind == "Dispel")
					{
						if (!((Object)(object)enemyUnit != (Object)null) || !(Random.value < d.Probability))
						{
							continue;
						}
						enemyUnit.SealSkills(d.Duration);
						enemyUnit.NativeController?.Dispel(d.Id);
						foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(s.vfx, "mob"))
						{
							Visual(s.vfx, item2.path, TargetAnchor((Component)(object)enemyUnit, NativeSkillVfx.Number(s.vfx, item2.path, "pos")), num4, ((Component)enemyUnit).transform);
						}
						continue;
					}
					if (d.Kind == "Pull")
					{
						if ((Object)(object)enemyUnit != (Object)null && Random.value < d.Probability)
						{
							enemyUnit.PullTowards(p.worldX);
						}
						continue;
					}
					bool flag = false;
					List<DamageResult> list3 = new List<DamageResult>();
					for (int j = 0; j < d.HitCount; j++)
					{
						DamageResult item = p.RollAttack(((Object)(object)enemyUnit != (Object)null) ? enemyUnit.CombatStats : bossUnit.CombatStats, d);
						list3.Add(item);
						flag |= item.Amount > 0;
					}
					if ((((Object)(object)enemyUnit != (Object)null) & flag) && (d.Id == 4121003 || d.Id == 4221003) && Random.value < d.Probability)
					{
						enemyUnit.NativeController?.ApplyShowdown(d.Id, Art.Int(SkillBook.Get(d.Id)?.Level(d.Level), "x"), (long)(d.Duration * 1000f), enemyUnit.IsBoss);
					}
					if ((Object)(object)enemyUnit != (Object)null)
					{
						num3 += enemyUnit.TakeReplayHit(list3.ToArray(), num4, d.Magic, indexed: false, 120, total: false, d.Id);
						int num5 = DrainRecovery(d.Id, num3, enemyUnit.NativeController?.MaximumHp ?? enemyUnit.hpMax, (int)p.hpMax, Art.Int(SkillBook.Get(d.Id)?.Level(d.Level), "x"));
						if (num5 > 0)
						{
							p.hp = Mathf.Min(p.hpMax, p.hp + (float)num5);
						}
						if (d.Magic && d.Kind != "Summon" && !enemyUnit.dead && (Object)(object)enemyUnit.NativeController != (Object)null)
						{
							int num6 = (Profile.JobId - Profile.JobId % 10) * 10000;
							int num7 = SkillProgress.LevelOf(num6);
							if (num7 > 0 && (num6 == 2100000 || num6 == 2200000 || num6 == 2300000))
							{
								Dictionary<string, object> d2 = SkillBook.Get(num6)?.Level(num7);
								if (Random.value < (float)Art.Int(d2, "prop", 100) / 100f)
								{
									p.mp = Mathf.Min(p.mpMax, p.mp + (float)enemyUnit.NativeController.AbsorbMp(Art.Int(d2, "x")));
								}
							}
						}
					}
					else if ((Object)(object)bossUnit != (Object)null)
					{
						bossUnit.TakeReplayHit(list3.ToArray(), num4, indexed: false, 120, total: false, d.Id);
					}
					Impact(s, d, list[i], num4, flag);
					list2.Add(new CapturedReplayHit
					{
						Target = list[i],
						Hits = list3.ToArray()
					});
					if (!((Object)(object)enemyUnit != (Object)null))
					{
						continue;
					}
					if ((s.art == "roar" || s.art == "chain") && Random.value < d.Probability)
					{
						float num8 = d.Duration * (float)((!(s.art == "chain")) ? 1 : 2);
						enemyUnit.Stun(num8);
						foreach (NativeVfxClip item3 in NativeSkillVfx.Parts(s.vfx, "mob"))
						{
							SkillFx skillFx = Visual(s.vfx, item3.path, TargetAnchor((Component)(object)enemyUnit, NativeSkillVfx.Number(s.vfx, item3.path, "pos")), num4, ((Component)enemyUnit).transform, NativeSkillVfx.Number(s.vfx, item3.path, "repeat") > 0f);
							if ((Object)(object)skillFx != (Object)null && NativeSkillVfx.Number(s.vfx, item3.path, "repeat") > 0f)
							{
								Object.Destroy((Object)(object)((Component)skillFx).gameObject, num8);
							}
						}
					}
					if (s.art == "dragon" || s.art == "dash")
					{
						enemyUnit.Push(num4, d);
					}
				}
				if (d.Id == s.native.Id && d.Kind == "Attack" && num2 > 0 && p.UsesRangedWeapon)
				{
					NativeArrowMode.OnHit(p, list[0], (list[0].transform.position.x >= origin.x) ? 1 : (-1));
				}
				if (d.Id == s.native.Id && d.Kind == "Attack" && num2 > 0)
				{
					int num9 = Mathf.Max(1, d.HitCount) * num2;
					int monsterId = (((Object)(object)list[0] != (Object)null) ? ((Object)list[0]).GetInstanceID() : 0);
					for (int k = 0; k < num9; k++)
					{
						NativeSpecialCore.OnAttack(p, monsterId);
					}
				}
				if (d.Kind == "Attack" && !d.Magic)
				{
					if (p.UsesRangedWeapon)
					{
						((Component)p).GetComponent<NativeBuffs>()?.OnRangedAttack(num2);
					}
					else
					{
						((Component)p).GetComponent<NativeBuffs>()?.OnCloseAttack();
					}
				}
				if (d.Id == s.native.Id && d.Kind != "Summon")
				{
					TriggerReactiveSkills(s, list2, origin, facing);
				}
				if (d.Id == s.native.Id && num2 > 0 && d.Kind != "Summon")
				{
					NativeShields.OnSkillHit(p, p.hpMax);
				}
				if (PlayerPrefs.GetInt("debug_hitlog", 0) != 0)
				{
					Debug.Log((object)("[Hit] id=" + d.Id + " lv=" + d.Level + " kind=" + d.Kind + " dmg%=" + d.DamagePercent + " 段=" + d.HitCount + " 目标上限=" + d.TargetCount + " 框=" + ((object)d.Min/*cast due to constrained. prefix*/).ToString() + "~" + ((object)d.Max/*cast due to constrained. prefix*/).ToString() + " hasBounds=" + d.HasBounds + " ⇒ 选中=" + list.Count + " 命中=" + num2 + " 总伤害=" + num3));
				}
				return num2;
			}
		}
	}

	public static int DrainRecovery(int skillId, long cumulativeDamage, long monsterMaxHp, int playerMaxHp, int percent)
	{
		if (skillId != 4101005 && skillId != 5111004 && skillId != 15111001 && skillId != 14101006)
		{
			return 0;
		}
		return (int)Math.Max(0L, Math.Min(monsterMaxHp, Math.Min((int)Math.Min(2147483647.0, (double)cumulativeDamage * ((double)percent / 100.0)), playerMaxHp / 2)));
	}

	public void CancelActions()
	{
		StopHeld(release: false);
		((MonoBehaviour)this).StopAllCoroutines();
		spearCombo.Reset();
		ResetProcState();
		ClearFieldZones();
		ClearSequences();
		ClearMechanics();
		ClearNativeReplayEffects();
		NativeSkillVideo.Stop(NativeSkillVideo.Channel.PlayerSkill);
		ClearVisuals();
		foreach (NativeSummon value in summons.Values)
		{
			if ((Object)(object)value != (Object)null)
			{
				((Component)value).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)value).gameObject);
			}
		}
		summons.Clear();
		ClearActionLock();
		if ((Object)(object)p != (Object)null)
		{
			((Component)p).GetComponent<NativeShields>()?.Clear();
			((Component)p).GetComponent<NativeSkillStates>()?.Clear();
			p.CancelRush();
		}
	}

	private void RemoveBuffSummon(int id)
	{
		if (summons.TryGetValue(id, out var value))
		{
			summons.Remove(id);
			if ((Object)(object)value != (Object)null)
			{
				((Component)value).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)value).gameObject);
			}
		}
	}

	public void RestoreBuffSummons()
	{
		NativeBuffs component = ((Component)this).GetComponent<NativeBuffs>();
		if ((Object)(object)component == (Object)null)
		{
			return;
		}
		foreach (NativeBuffs.Effect item in new List<NativeBuffs.Effect>(component.Visible))
		{
			if (NativeSummon.Root(item.Id, item.Level) != null)
			{
				if (NativeSummon.Stationary(item.Id))
				{
					component.Cancel(item.Id);
					continue;
				}
				SkillData skillData = SkillBook.Get(item.Id);
				SkillDef skill = new SkillDef(new ClassSkill
				{
					name = skillData.name,
					art = item.Id.ToString(),
					icon = skillData.icon
				}, SkillBook.Native(item.Id, item.Level));
				RemoveBuffSummon(item.Id);
				summons[item.Id] = NativeSummon.Spawn(this, p, skill);
			}
		}
	}

	private void OnDestroy()
	{
		NativeBuffs nativeBuffs = (((Object)(object)p != (Object)null) ? ((Component)p).GetComponent<NativeBuffs>() : null);
		if ((Object)(object)nativeBuffs != (Object)null)
		{
			nativeBuffs.Removed -= RemoveBuffSummon;
		}
	}

	private void OnDisable()
	{
		CancelActions();
	}

	private SkillFx Visual(string key, string part, Vector3 point, int facing, Transform follow = null, bool loop = false, int order = 360)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SkillFx skillFx = SkillFx.PlayPart(key, part, point, facing, 1f, order, loop);
		if ((Object)(object)skillFx != (Object)null)
		{
			ownedVisuals.RemoveAll((SkillFx item) => (Object)(object)item == (Object)null);
			ownedVisuals.Add(skillFx);
			if ((Object)(object)follow != (Object)null)
			{
				skillFx.Follow(follow);
			}
		}
		if (FxDebug && (Object)(object)skillFx != (Object)null)
		{
			Debug.Log((object)("[FxPlay] key=" + key + " part=" + skillFx.Part));
		}
		return skillFx;
	}

	private void BeginVisuals(SkillDef s, bool bodyOnly = false)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(CastVisuals(s, p.FeetPos, p.facing, bodyOnly));
	}

	private IEnumerator CastVisuals(SkillDef s, Vector2 origin, int facing, bool bodyOnly = false)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (!bodyOnly)
		{
			foreach (string item in NativeSkillVfx.Layers(s.vfx, "prepare"))
			{
				foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(s.vfx, item))
				{
					PlayLayer(s.vfx, item2, Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, 360);
				}
			}
			float delay = Preparation(s);
			if (delay > 0f)
			{
				yield return (object)new WaitForSeconds(delay);
			}
			if (delay > 0f)
			{
				p.PlaySkill(s.action);
			}
		}
		foreach (string item3 in NativeSkillVfx.Layers(s.vfx, "effect"))
		{
			foreach (NativeVfxClip item4 in NativeSkillVfx.Parts(s.vfx, item3))
			{
				int num = (int)NativeSkillVfx.Number(s.vfx, item4.path, "z");
				if (num == 0)
				{
					num = (int)NativeSkillVfx.Number(s.vfx, item3, "z");
				}
				bool flag = s.art == "doom" || s.art == "rupture";
				PlayLayer(s.vfx, item4, flag ? Vector2.op_Implicit(origin) : Vector2.op_Implicit(p.FeetPos), facing, flag ? null : ((Component)p).transform, (num < 0) ? (p.SortingOrder - 1) : (360 + Mathf.Max(0, num)));
			}
		}
		foreach (string item5 in NativeSkillVfx.Layers(s.vfx, "affected"))
		{
			foreach (NativeVfxClip item6 in NativeSkillVfx.Parts(s.vfx, item5))
			{
				PlayLayer(s.vfx, item6, Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, 360);
			}
		}
		string[] array = new string[1] { "rushEffect" };
		foreach (string stem in array)
		{
			foreach (string item7 in NativeSkillVfx.Layers(s.vfx, stem))
			{
				foreach (NativeVfxClip item8 in NativeSkillVfx.Parts(s.vfx, item7))
				{
					int num2 = (int)NativeSkillVfx.Number(s.vfx, item8.path, "z");
					if (num2 == 0)
					{
						num2 = (int)NativeSkillVfx.Number(s.vfx, item7, "z");
					}
					PlayLayer(s.vfx, item8, Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, (num2 < 0) ? (p.SortingOrder - 1) : (360 + Mathf.Max(0, num2)));
				}
			}
		}
		foreach (string item9 in NativeSkillVfx.Layers(s.vfx, "specialAffected"))
		{
			List<NativeVfxClip> list = NativeSkillVfx.Parts(s.vfx, item9);
			int num3 = ((list.Count > 1 && SameOrigin(list)) ? 1 : list.Count);
			for (int j = 0; j < num3; j++)
			{
				PlayLayer(s.vfx, list[j], Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, 360);
			}
		}
		foreach (string item10 in NativeSkillVfx.Layers(s.vfx, "effect_ple"))
		{
			PlaySegmentedLayer(s, item10, Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, 360, p.SkillActionDuration);
		}
		if (NativeSkillVfx.Has(s.vfx, "frameEffect"))
		{
			foreach (string item11 in NativeSkillVfx.SubBranches(s.vfx, "frameEffect"))
			{
				PlaySegmentedLayer(s, item11, Vector2.op_Implicit(origin), facing, null, 340, p.SkillActionDuration);
			}
		}
		if (NativeSkillVfx.Has(s.vfx, "customEffect"))
		{
			PlayCustomEffect(s, Vector2.op_Implicit(origin), facing, null, 340);
		}
		PlayFieldLoop(s, origin, facing);
		if (NativeSkillVfx.Has(s.vfx, "afterimage"))
		{
			((MonoBehaviour)this).StartCoroutine(Afterimage(s, facing));
		}
		foreach (string item12 in NativeSkillVfx.Roots(s.vfx))
		{
			if (!IsScreenLayer(item12))
			{
				continue;
			}
			foreach (NativeVfxClip item13 in NativeSkillVfx.Parts(s.vfx, item12))
			{
				PlayScreenClip(s.vfx, item13, origin);
			}
			foreach (NativeVfxVideo item14 in NativeSkillVfx.VideoParts(s.vfx, item12))
			{
				PlayVideo(item14, screen: true);
			}
		}
		array = new string[3] { "effect", "effect0", "special" };
		foreach (string branch in array)
		{
			foreach (NativeVfxVideo item15 in NativeSkillVfx.VideoParts(s.vfx, branch))
			{
				PlayVideo(item15, screen: false);
			}
		}
		if (NativeSkillVfx.Has(s.vfx, "tile"))
		{
			if (s.art == "firebolt")
			{
				((MonoBehaviour)this).StartCoroutine(GroundFire(s, origin, facing));
			}
			else
			{
				TileField(s, origin, facing);
			}
		}
		if (NativeSkillVfx.HasLayer(s.vfx, "special") && s.art != "rupture")
		{
			yield return SpecialField(s, origin, facing);
		}
		PlayFinish(s, Vector2.op_Implicit(origin), facing, ((Component)p).transform);
	}

	private static bool SameOrigin(List<NativeVfxClip> clips)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (clips == null || clips.Count < 2)
		{
			return false;
		}
		if (clips[0].frames == null || clips[0].frames.Length == 0)
		{
			return false;
		}
		Vector2 origin = clips[0].frames[0].origin;
		foreach (NativeVfxClip clip in clips)
		{
			if (clip.frames == null || clip.frames.Length == 0 || clip.frames[0].origin != origin)
			{
				return false;
			}
		}
		return true;
	}

	private void PlaySegmentedLayer(SkillDef s, string branch, Vector3 point, int facing, Transform follow, int order, float minimumHold)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		List<NativeVfxClip> list = NativeSkillVfx.Segment(s.vfx, branch, "loop");
		if (list.Count == 0)
		{
			list = NativeSkillVfx.Segment(s.vfx, branch, "repeat");
		}
		float num = Mathf.Max(minimumHold, (list.Count > 0) ? (list[0].Duration * 3f) : 0.8f);
		foreach (NativeVfxClip item in NativeSkillVfx.Segment(s.vfx, branch, "pre"))
		{
			PlayLayer(s.vfx, item, point, facing, follow, order);
			flag = true;
		}
		foreach (NativeVfxClip item2 in NativeSkillVfx.Segment(s.vfx, branch, "start"))
		{
			PlayLayer(s.vfx, item2, point, facing, follow, order);
			flag = true;
		}
		foreach (NativeVfxClip item3 in list)
		{
			SkillFx skillFx = Visual(s.vfx, item3.path, point, facing, follow, loop: true, order);
			if ((Object)(object)skillFx != (Object)null && num > 0f)
			{
				Object.Destroy((Object)(object)((Component)skillFx).gameObject, num);
			}
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		foreach (NativeVfxClip item4 in NativeSkillVfx.Parts(s.vfx, branch))
		{
			string text = ((item4.path.Length > branch.Length) ? item4.path.Substring(branch.Length + 1) : "");
			int num2 = text.IndexOf('/');
			switch ((num2 < 0) ? text : text.Substring(0, num2))
			{
			case "pre":
			case "start":
			case "loop":
			case "repeat":
			case "end":
				continue;
			}
			PlayLayer(s.vfx, item4, point, facing, follow, order);
		}
		List<NativeVfxClip> list2 = NativeSkillVfx.Segment(s.vfx, branch, "end");
		if (list2.Count > 0)
		{
			((MonoBehaviour)this).StartCoroutine(PlayStageLater(s.vfx, list2, num, point, facing, follow, order));
		}
	}

	private void PlayCustomEffect(SkillDef s, Vector3 point, int facing, Transform follow, int order)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		foreach (NativeVfxClip item in NativeSkillVfx.Segment(s.vfx, "customEffect", "start"))
		{
			PlayLayer(s.vfx, item, point, facing, follow, order);
		}
		List<NativeVfxClip> list = NativeSkillVfx.Segment(s.vfx, "customEffect", "end");
		if (list.Count > 0)
		{
			((MonoBehaviour)this).StartCoroutine(PlayStageLater(s.vfx, list, p.SkillActionDuration, point, facing, follow, order));
		}
	}

	private void PlayFinish(SkillDef s, Vector3 point, int facing, Transform follow)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> list = new List<NativeVfxClip>();
		foreach (string item2 in NativeSkillVfx.Layers(s.vfx, "finish"))
		{
			list.AddRange(NativeSkillVfx.Parts(s.vfx, item2));
		}
		if (list.Count != 0)
		{
			NativeVfxClip item = list[finishVariant++ % list.Count];
			((MonoBehaviour)this).StartCoroutine(PlayStageLater(s.vfx, new List<NativeVfxClip> { item }, p.SkillActionDuration, point, facing, follow, 360));
		}
	}

	private void PlayFieldLoop(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> list = new List<NativeVfxClip>();
		foreach (string item in NativeSkillVfx.Layers(s.vfx, "start"))
		{
			list.AddRange(NativeSkillVfx.Parts(s.vfx, item));
		}
		List<NativeVfxClip> list2 = new List<NativeVfxClip>();
		foreach (string item2 in NativeSkillVfx.Layers(s.vfx, "repeat"))
		{
			list2.AddRange(NativeSkillVfx.Parts(s.vfx, item2));
		}
		List<NativeVfxClip> list3 = new List<NativeVfxClip>();
		foreach (string item3 in NativeSkillVfx.Layers(s.vfx, "end"))
		{
			list3.AddRange(NativeSkillVfx.Parts(s.vfx, item3));
		}
		if ((list.Count == 0 && list2.Count == 0 && list3.Count == 0) || (list2.Count == 0 && list3.Count == 0))
		{
			return;
		}
		bool flag = s.native.Kind == "Buff";
		Vector3 point = (Vector3)(flag ? Vector2.op_Implicit(p.FeetPos) : new Vector3(origin.x + (float)(facing * 48) / 40f, PlayerCtrl.GroundAt(origin.x, origin.y), 0f));
		Transform follow = (flag ? ((Component)p).transform : null);
		int order = (flag ? (p.SortingOrder - 1) : 340);
		float num = Mathf.Max(p.SkillActionDuration, (list2.Count > 0) ? (list2[0].Duration * 3f) : 0.8f);
		foreach (NativeVfxClip item4 in list)
		{
			PlayLayer(s.vfx, item4, point, facing, follow, order);
		}
		foreach (NativeVfxClip item5 in list2)
		{
			SkillFx skillFx = Visual(s.vfx, item5.path, point, facing, follow, loop: true, order);
			if ((Object)(object)skillFx != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)skillFx).gameObject, num);
			}
		}
		if (list3.Count > 0)
		{
			((MonoBehaviour)this).StartCoroutine(PlayStageLater(s.vfx, list3, num, point, facing, follow, order));
		}
	}

	private IEnumerator Afterimage(SkillDef s, int facing)
	{
		foreach (NativeVfxClip item in NativeSkillVfx.Parts(s.vfx, "afterimage"))
		{
			SkillFx skillFx = Visual(s.vfx, item.path, Vector2.op_Implicit(p.FeetPos), facing, ((Component)p).transform, loop: false, p.SortingOrder - 2);
			if ((Object)(object)skillFx != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)skillFx).gameObject, Mathf.Max(0.12f, item.Duration));
			}
			yield return (object)new WaitForSeconds(0.05f);
		}
	}

	private IEnumerator PlayStageLater(string key, List<NativeVfxClip> clips, float wait, Vector3 point, int facing, Transform follow, int order)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (wait > 0f)
		{
			yield return (object)new WaitForSeconds(wait);
		}
		foreach (NativeVfxClip clip in clips)
		{
			PlayLayer(key, clip, point, facing, follow, order);
		}
	}

	private void PlayLayer(string key, NativeVfxClip clip, Vector3 point, int facing, Transform follow, int order, bool loop = false)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (clip.StartDelay <= 0f)
		{
			Visual(key, clip.path, point, facing, follow, loop, order);
		}
		else
		{
			((MonoBehaviour)this).StartCoroutine(DelayedLayer(key, clip, point, facing, follow, order, loop));
		}
	}

	private IEnumerator DelayedLayer(string key, NativeVfxClip clip, Vector3 point, int facing, Transform follow, int order, bool loop)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForSeconds(clip.StartDelay);
		Visual(key, clip.path, point, facing, follow, loop, order);
	}

	private static bool IsScreenLayer(string root)
	{
		if (!root.StartsWith("screen", StringComparison.Ordinal))
		{
			return root == "background";
		}
		return true;
	}

	private void PlayScreenClip(string key, NativeVfxClip clip, Vector2 origin)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		Vector3 point = (Vector3)(((Object)(object)main == (Object)null) ? Vector2.op_Implicit(origin) : new Vector3(((Component)main).transform.position.x, ((Component)main).transform.position.y, 0f));
		SkillFx skillFx = Visual(key, clip.path, point, -1, ((Object)(object)main == (Object)null) ? null : ((Component)main).transform, loop: false, 700 + (int)NativeSkillVfx.Number(key, clip.path, "z"));
		if ((Object)(object)skillFx != (Object)null && (Object)(object)main != (Object)null)
		{
			Bounds bounds = skillFx.CurrentSprite.bounds;
			Vector3 size = bounds.size;
			float num = Mathf.Max(main.orthographicSize * 2f / size.y, main.orthographicSize * 2f * main.aspect / size.x);
			((Component)skillFx).transform.localScale = Vector3.one * num;
		}
	}

	private void PlayVideo(NativeVfxVideo video, bool screen)
	{
		int sortingOrder;
		if (screen)
		{
			sortingOrder = 700 + video.z;
		}
		else
		{
			sortingOrder = ((video.z < 0) ? (p.SortingOrder - 1) : (360 + video.z));
		}
		NativeSkillVideo.PlayNode(video.node?.VideoData, video.path.Replace('/', '_'), p, sortingOrder, video.loop, video.delay);
	}

	private IEnumerator GroundFire(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForSeconds(0.45f);
		TileField(s, origin, facing);
	}

	private static bool UsesProjectile(SkillDef s)
	{
		if (s.art != "firebolt")
		{
			if (!NativeSkillVfx.Has(s.vfx, "ball"))
			{
				return NativeShootObj.Has(s.native.Id);
			}
			return true;
		}
		return false;
	}

	private static float ProjectileRange(SkillDef s)
	{
		int num = Art.Int(SkillBook.Get(s.native.Id)?.Level(s.native.Level), "range");
		if (num > 0)
		{
			return (float)num / 40f;
		}
		if (s.native.HasBounds)
		{
			return Mathf.Max(0f - s.native.Min.x, s.native.Max.x);
		}
		float num2 = ShootReach(s);
		if (num2 > 0f)
		{
			return num2;
		}
		if (!NativeProjectiles.IsRangedSkill(s.native.Id) || s.native.Magic)
		{
			return 7.5f;
		}
		if (s.native.Id == 4111003 || s.native.Id == 4221003)
		{
			return 7.5f;
		}
		WeaponKind kind = WeaponDefs.Get(Profile.equipWeapon).kind;
		int id;
		if (kind != WeaponKind.claw)
		{
			id = ((Profile.JobId / 1000 == 1) ? 13000001 : 3000002);
		}
		else
		{
			id = ((Profile.JobId / 1000 == 1) ? 14000001 : 4000001);
		}
		int num3 = SkillProgress.LevelOf(id);
		int num4 = ((num3 > 0) ? Art.Int(SkillBook.Get(id)?.Level(num3), "range") : 0);
		int num5;
		switch (kind)
		{
		default:
			num5 = 0;
			break;
		case WeaponKind.gun:
			num5 = 200;
			break;
		case WeaponKind.claw:
			num5 = 200 + num4;
			break;
		case WeaponKind.bow:
		case WeaponKind.crossbow:
			num5 = 300 + num4;
			break;
		}
		return (float)num5 / 40f;
	}

	private bool InProjectileRange(Component target, SkillDef s, Vector2 origin, int facing)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (s.native.HasBounds)
		{
			return InRange(target, s.native, origin, facing);
		}
		return ProjectileIntersects((Rect)((target is EnemyUnit { ProjectileBounds: not null, ProjectileBounds: var projectileBounds }) ? projectileBounds.Value : new Rect(target.transform.position.x, target.transform.position.y, 0f, 0f)), origin, facing, ProjectileRange(s), (!s.native.Magic && NativeProjectiles.IsRangedSkill(s.native.Id)) ? 0f : 1.25f);
	}

	internal static bool ProjectileIntersects(Rect bounds, Vector2 origin, int facing, float range, float start)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		float num = origin.y + 0.7f;
		int num2 = Mathf.RoundToInt(range * 40f);
		for (int i = Mathf.RoundToInt(start * 40f); i < num2; i += 20)
		{
			float num3 = (float)i / 40f;
			float num4 = (float)Mathf.Min(i + 20, num2) / 40f;
			float num5 = (float)(i / 4) / 40f;
			float num6 = origin.x + num3 * (float)facing;
			float num7 = origin.x + num4 * (float)facing;
			if (bounds.xMax >= Mathf.Min(num6, num7) && bounds.xMin <= Mathf.Max(num6, num7) && bounds.yMax >= num - num5 && bounds.yMin <= num + num5)
			{
				return true;
			}
		}
		return false;
	}

	private float Preparation(SkillDef s)
	{
		float num = 0f;
		foreach (string item in NativeSkillVfx.Layers(s.vfx, "prepare"))
		{
			foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(s.vfx, item))
			{
				num = Mathf.Max(num, item2.Duration);
			}
		}
		return num;
	}

	private void TileField(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> list = NativeSkillVfx.Parts(s.vfx, "tile");
		float num = NativeSkillVfx.Number(s.vfx, "tile", "effectDistance", 180f) / 40f;
		if (list.Count != 0 && !(num <= 0f))
		{
			Camera main = Camera.main;
			float num2 = (((Object)(object)main == (Object)null) ? 10f : (main.orthographicSize * main.aspect));
			float num3 = Mathf.Max(origin.x + s.native.Min.x, ((Object)(object)main == (Object)null) ? (origin.x - num2) : (((Component)main).transform.position.x - num2));
			float num4 = Mathf.Min(origin.x - s.native.Min.x, ((Object)(object)main == (Object)null) ? (origin.x + num2) : (((Component)main).transform.position.x + num2));
			int num5 = 0;
			for (float num6 = num3; num6 <= num4; num6 += num)
			{
				float num7 = PlayerCtrl.GroundAt(num6, origin.y);
				Visual(s.vfx, list[num5++ % list.Count].path, new Vector3(num6, num7, 0f), facing);
			}
		}
	}

	private IEnumerator SpecialField(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		foreach (string item in NativeSkillVfx.Layers(s.vfx, "special"))
		{
			((MonoBehaviour)this).StartCoroutine(SpecialLayer(s, origin, facing, item));
		}
		yield break;
	}

	private IEnumerator SpecialLayer(SkillDef s, Vector2 origin, int facing, string branch)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> parts = NativeSkillVfx.Parts(s.vfx, branch);
		if (parts.Count == 0)
		{
			yield break;
		}
		float num = NativeSkillVfx.Number(s.vfx, branch, "start") / 1000f;
		if (num > 0f)
		{
			yield return (object)new WaitForSeconds(num);
		}
		int count = (int)NativeSkillVfx.Number(s.vfx, branch, "count", parts.Count);
		float interval = NativeSkillVfx.Number(s.vfx, branch, "interval", 60f) / 1000f;
		float width = NativeSkillVfx.Number(s.vfx, branch, "x", 200f) / 40f;
		float height = NativeSkillVfx.Number(s.vfx, branch, "y") / 40f;
		float fall = ((height > 0f) ? (NativeSkillVfx.Number(s.vfx, branch, "duration", 500f) / 1000f) : 0f);
		bool spread = NativeSkillVfx.Number(s.vfx, branch, "count") > 0f;
		for (int i = 0; i < count; i++)
		{
			NativeVfxClip nativeVfxClip = parts[i % parts.Count];
			float num2;
			if (spread)
			{
				num2 = origin.x + (((float)i + 0.5f) / (float)count * 2f - 1f) * width;
			}
			else
			{
				int num3 = (ContainsSegment(nativeVfxClip.path, "front") ? facing : (ContainsSegment(nativeVfxClip.path, "back") ? (-facing) : 0));
				num2 = origin.x + (float)num3 * width;
			}
			Vector3 val = new Vector3(num2, origin.y + height, 0f);
			if (s.art == "doom")
			{
				val.x = origin.x + Mathf.Lerp(s.native.Min.x, s.native.Max.x, ((float)i + 0.5f) / (float)count);
				val.y = origin.y + (float)(i % 3 * 45) / 40f;
			}
			float num4 = NativeSkillVfx.Number(s.vfx, nativeVfxClip.path, "z");
			if (num4 == 0f)
			{
				num4 = NativeSkillVfx.Number(s.vfx, branch, "z");
			}
			SkillFx skillFx = Visual(s.vfx, nativeVfxClip.path, val, facing, null, fall > 0f, (num4 < 0f) ? (p.SortingOrder - 1) : (360 + (int)num4));
			if (fall > 0f && (Object)(object)skillFx != (Object)null)
			{
				((MonoBehaviour)this).StartCoroutine(Fall(skillFx, val, new Vector3(num2, PlayerCtrl.GroundAt(num2, origin.y), 0f), fall));
			}
			yield return (object)new WaitForSeconds(Mathf.Max(0.001f, interval));
		}
	}

	private static bool ContainsSegment(string path, string segment)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		for (int num = path.IndexOf("/" + segment, StringComparison.Ordinal); num >= 0; num = path.IndexOf("/" + segment, num + 1, StringComparison.Ordinal))
		{
			int num2 = num + 1 + segment.Length;
			if (num2 >= path.Length || path[num2] == '/')
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerator Fall(SkillFx fx, Vector3 from, Vector3 to, float duration)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		float elapsed = 0f;
		while ((Object)(object)fx != (Object)null && elapsed < duration)
		{
			elapsed += Time.deltaTime;
			((Component)fx).transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
			yield return null;
		}
		if ((Object)(object)fx != (Object)null)
		{
			fx.Stop();
		}
	}

	private void Impact(SkillDef s, NativeSkillDefinition definition, Component target, int direction, bool successful)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		if (successful)
		{
			string key = ((definition.Id == s.native.Id) ? s.vfx : NativeSkillVfx.Key(definition.Id, definition.Level));
			List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "hit");
			List<NativeVfxClip> list2 = ((definition.HitCount >= 2) ? NativeSkillVfx.Parts(key, "hit2") : new List<NativeVfxClip>());
			int num = impactVariant++;
			List<NativeVfxClip> list3 = ((list2.Count > 0 && num % 2 == 1) ? list2 : list);
			if (list3.Count == 0)
			{
				list3 = ((list2.Count > 0) ? list2 : list);
			}
			if (list3.Count > 0)
			{
				NativeVfxClip nativeVfxClip = list3[num % list3.Count];
				Vector3 point = TargetAnchor(target, NativeSkillVfx.Number(key, nativeVfxClip.path, "pos"));
				Visual(key, nativeVfxClip.path, point, direction, target.transform);
			}
			ShowComboNumber(s);
		}
	}

	private void ShowComboNumber(SkillDef s)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime > comboUntil)
		{
			comboCount = 0;
		}
		comboCount++;
		comboUntil = unscaledTime + 1.5f;
		List<NativeVfxClip> list = NativeSkillVfx.Icons(s.vfx, "number");
		if (list.Count > 0)
		{
			if ((Object)(object)comboFx != (Object)null)
			{
				comboFx.Stop();
			}
			NativeVfxClip nativeVfxClip = list[Mathf.Clamp(comboCount, 1, list.Count) - 1];
			comboFx = Visual(s.vfx, nativeVfxClip.path, Vector2.op_Implicit(p.FeetPos), p.facing, ((Component)p).transform, loop: false, 500);
			if ((Object)(object)comboFx != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)comboFx).gameObject, 1.2f);
			}
		}
		List<NativeVfxClip> list2 = NativeSkillVfx.Icons(s.vfx, "number2");
		if (list2.Count > 0)
		{
			if ((Object)(object)comboFx2 != (Object)null)
			{
				comboFx2.Stop();
			}
			NativeVfxClip nativeVfxClip2 = list2[Mathf.Clamp(comboCount, 1, list2.Count) - 1];
			comboFx2 = Visual(s.vfx, nativeVfxClip2.path, Vector2.op_Implicit(p.FeetPos), p.facing, ((Component)p).transform, loop: false, 501);
			if ((Object)(object)comboFx2 != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)comboFx2).gameObject, 1.2f);
			}
		}
	}

	private static Vector3 TargetAnchor(Component target, float pos = 0f)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		SpriteRenderer[] componentsInChildren = target.GetComponentsInChildren<SpriteRenderer>();
		SpriteRenderer val = null;
		SpriteRenderer[] array = componentsInChildren;
		Bounds bounds;
		foreach (SpriteRenderer val2 in array)
		{
			if (!((Renderer)val2).enabled || !((Object)(object)val2.sprite != (Object)null))
			{
				continue;
			}
			if (!((Object)(object)val == (Object)null))
			{
				bounds = ((Renderer)val2).bounds;
				float y = bounds.size.y;
				bounds = ((Renderer)val).bounds;
				if (!(y > bounds.size.y))
				{
					continue;
				}
			}
			val = val2;
		}
		if ((Object)(object)val == (Object)null)
		{
			return target.transform.position + Vector3.up * 0.6f;
		}
		bounds = ((Renderer)val).bounds;
		float x = bounds.center.x;
		float y2;
		if (pos != 2f)
		{
			bounds = ((Renderer)val).bounds;
			y2 = bounds.center.y;
		}
		else
		{
			bounds = ((Renderer)val).bounds;
			y2 = bounds.max.y;
		}
		return new Vector3(x, y2, 0f);
	}

	private IEnumerator ShootObjCast(SkillDef s, Vector2 origin, int facing, bool immediate)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		ShootObjData data = NativeShootObj.Get(s.native.Id);
		if (data == null)
		{
			yield break;
		}
		float num = (immediate ? 0f : data.startDelayMs);
		if (num > 0f)
		{
			yield return (object)new WaitForSeconds(num / 1000f);
		}
		int groups = Mathf.Max(1, data.groupCount);
		for (int group = 0; group < groups; group++)
		{
			if (group > 0)
			{
				float num2;
				if (data.groupIntervalMs > 0f)
				{
					num2 = data.groupIntervalMs;
				}
				else
				{
					num2 = ((data.randomDelayMaxMs > 0f) ? Random.Range(data.randomDelayMinMs, Mathf.Max(data.randomDelayMinMs, data.randomDelayMaxMs)) : data.randomDelayMinMs);
				}
				if (num2 > 0f)
				{
					yield return (object)new WaitForSeconds(num2 / 1000f);
				}
			}
			if (data.paths.Count == 0)
			{
				((MonoBehaviour)this).StartCoroutine(ShootObjActor(s, data, null, origin, facing, group));
				continue;
			}
			foreach (ShootObjPath path in data.paths)
			{
				((MonoBehaviour)this).StartCoroutine(ShootObjActor(s, data, path, origin, facing, group));
			}
		}
	}

	private static Vector3 ShootPoint(Vector2 origin, int facing, Vector2 wz)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(origin.x - wz.x * (float)facing, origin.y - wz.y, 0f);
	}

	private IEnumerator ShootObjActor(SkillDef s, ShootObjData data, ShootObjPath path, Vector2 origin, int facing, int group)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (path == null)
		{
			path = new ShootObjPath
			{
				name = "info"
			};
			path.points.Add(new ShootObjPoint
			{
				pos = new Vector2((0f - ProjectileRange(s)) * 40f, 0f),
				v = data.infoSpeed,
				hasPos = true
			});
		}
		if (data.startAlphaDelayMs > 0f)
		{
			yield return (object)new WaitForSeconds(data.startAlphaDelayMs / 1000f);
		}
		Vector3 val = ShootPoint(origin, facing, data.start);
		if (data.multiStartPos && group > 0)
		{
			val += Vector3.up * ((float)group * 20f / 40f);
		}
		if (data.startOffset && group > 0)
		{
			val += Vector3.right * (float)facing * ((float)group * 40f / 40f);
		}
		int num = (data.pickDir5Way ? 5 : (data.pickDir3Way ? 3 : 0));
		float lane = ((num > 0) ? ((float)Random.Range(-(num / 2), num / 2 + 1) * ((num == 5) ? 30f : 15f)) : 0f);
		List<SkillFx> visuals = new List<SkillFx>();
		foreach (string layer in data.layers)
		{
			bool flag = layer.Length > 0 && layer[0] == 's';
			SkillFx skillFx = Visual(s.vfx, "shootobj/layerList/" + layer, val, facing, null, !flag);
			if ((Object)(object)skillFx != (Object)null)
			{
				if (data.flipRotate != 0f && facing < 0)
				{
					Vector3 localScale = ((Component)skillFx).transform.localScale;
					localScale.x = 0f - localScale.x;
					((Component)skillFx).transform.localScale = localScale;
				}
				visuals.Add(skillFx);
			}
		}
		Dictionary<int, int> struck = new Dictionary<int, int>();
		List<Component> targets = ShootCandidates(s, origin, facing);
		int budget = data.HitBudget(s.native.TargetCount);
		int perSweep = data.PerSweep;
		int perMob = data.PerMobLimit;
		Vector3 position = val;
		int landed = 0;
		float traveledMs = 0f;
		float totalMs = data.Duration(path);
		bool fading = false;
		List<ShootObjPoint> points = path.points;
		for (int i = 0; i < points.Count; i++)
		{
			ShootObjPoint point = points[i];
			if (point.delayMs > 0f)
			{
				yield return (object)new WaitForSeconds(point.delayMs / 1000f);
				traveledMs += point.delayMs;
			}
			Vector3 target = ShootPoint(origin, facing, ShootObjTarget(s, data, point, origin, facing, lane));
			float num2 = ShootObjData.Speed((point.v > 0f) ? point.v : data.groupVMin);
			if (num2 <= 0f)
			{
				position = target;
				SetShootPosition(visuals, position);
				continue;
			}
			float duration = Vector3.Distance(position, target) / num2;
			float elapsed = 0f;
			Vector3 from = position;
			if (data.rotateAttack)
			{
				RotateShoot(visuals, target - from);
			}
			Component chased = (data.chaseTarget ? NearestShootTarget(targets, from) : null);
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				traveledMs += Time.deltaTime * 1000f;
				if ((Object)(object)chased != (Object)null)
				{
					target = TargetAnchor(chased);
				}
				position = Vector3.Lerp(from, target, Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
				SetShootPosition(visuals, position);
				landed += ShootStrike(s, data, position, facing, targets, struck, ref budget, perSweep, perMob);
				if (!fading)
				{
					fading = StartShootFade(data, visuals, traveledMs, totalMs);
				}
				if (data.useUpWhenMaxMobCount && struck.Count >= perSweep)
				{
					break;
				}
				yield return null;
			}
			SetShootPosition(visuals, position);
			if ((data.useUpWhenMaxMobCount && struck.Count >= perSweep) || budget <= 0)
			{
				break;
			}
		}
		if (!fading)
		{
			float num3 = ((data.deleteAlpha > 0) ? 0.12f : 0f);
			foreach (SkillFx item in visuals)
			{
				if ((Object)(object)item != (Object)null)
				{
					if (num3 > 0f)
					{
						item.FadeOut(num3);
					}
					else
					{
						item.Stop();
					}
				}
			}
		}
		bool flag2 = data.paths.Count == 0 || path == data.paths[0] || path.name == "info";
		if ((landed == 0 && struck.Count == 0) & flag2)
		{
			HitAt(s, s.native, Vector2.op_Implicit(position), facing);
		}
	}

	private static bool StartShootFade(ShootObjData data, List<SkillFx> visuals, float traveledMs, float totalMs)
	{
		if (data.deleteAlpha <= 0 || data.deleteAlphaDelayMs >= 0f || totalMs <= 0f)
		{
			return false;
		}
		if (traveledMs < totalMs + data.deleteAlphaDelayMs)
		{
			return false;
		}
		foreach (SkillFx visual in visuals)
		{
			if ((Object)(object)visual != (Object)null)
			{
				visual.FadeOut(0.12f);
			}
		}
		return true;
	}

	private static void RotateShoot(List<SkillFx> visuals, Vector3 direction)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (direction.sqrMagnitude < 0.0001f)
		{
			return;
		}
		float num = Mathf.Atan2(direction.y, direction.x) * 57.29578f;
		foreach (SkillFx visual in visuals)
		{
			if ((Object)(object)visual != (Object)null)
			{
				((Component)visual).transform.rotation = Quaternion.Euler(0f, 0f, num);
			}
		}
	}

	private static Component NearestShootTarget(List<Component> targets, Vector3 from)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Component result = null;
		float num = float.MaxValue;
		foreach (Component target in targets)
		{
			if ((Object)(object)target == (Object)null)
			{
				continue;
			}
			EnemyUnit enemyUnit = target as EnemyUnit;
			if ((Object)(object)enemyUnit != (Object)null && enemyUnit.dead)
			{
				continue;
			}
			BossUnit bossUnit = target as BossUnit;
			if (!((Object)(object)bossUnit != (Object)null) || !bossUnit.dead)
			{
				Vector3 val = target.transform.position - from;
				float sqrMagnitude = val.sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = target;
				}
			}
		}
		return result;
	}

	private void SpecialCoreHit(SkillDef s, Component target)
	{
		SpecialCoreTriggered?.Invoke(s, target);
		NativeSpecialCore.OnAttack(p, ((Object)(object)target != (Object)null) ? ((Object)target).GetInstanceID() : 0);
	}

	private static Vector2 ShootObjTarget(SkillDef s, ShootObjData data, ShootObjPoint point, Vector2 origin, int facing, float lane)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val;
		if (point.hasPos)
		{
			val = point.pos;
		}
		else if (data.angles.Count > 0)
		{
			Vector2 val2 = data.angles[Random.Range(0, data.angles.Count)];
			float num = Random.Range(val2.x, val2.y) * (MathF.PI / 180f);
			float num2 = ProjectileRange(s) * 40f;
			val = new Vector2((0f - Mathf.Cos(num)) * num2, (0f - Mathf.Sin(num)) * num2);
		}
		else
		{
			val = Vector2.zero;
		}
		if (lane == 0f)
		{
			return val;
		}
		float num3 = lane * (MathF.PI / 180f);
		float num4 = Mathf.Cos(num3);
		float num5 = Mathf.Sin(num3);
		return new Vector2(val.x * num4 - val.y * num5, val.x * num5 + val.y * num4);
	}

	private List<Component> ShootCandidates(SkillDef s, Vector2 origin, int facing)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = SelectTargets(s, s.native, origin, facing, null, null);
		if (list.Count > 0)
		{
			return list;
		}
		float num = ProjectileRange(s) + ShootReach(s);
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead && Mathf.Abs(((Component)bossUnit).transform.position.x - origin.x) <= num)
			{
				list.Add((Component)(object)bossUnit);
			}
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && Mathf.Abs(((Component)item).transform.position.x - origin.x) <= num)
			{
				list.Add((Component)(object)item);
			}
		}
		return list;
	}

	private static float ShootReach(SkillDef s)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		ShootObjData shootObjData = NativeShootObj.Get(s.native.Id);
		if (shootObjData == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (ShootObjPath path in shootObjData.paths)
		{
			foreach (ShootObjPoint point in path.points)
			{
				float num2 = num;
				Vector2 val = new Vector2(point.pos.x, point.pos.y);
				num = Mathf.Max(num2, val.magnitude / 40f);
			}
		}
		return num;
	}

	private int ShootStrike(SkillDef s, ShootObjData data, Vector3 at, int facing, List<Component> targets, Dictionary<int, int> struck, ref int budget, int perSweep, int perMob)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (Component target in targets)
		{
			if (num >= perSweep || budget <= 0)
			{
				break;
			}
			if ((Object)(object)target == (Object)null)
			{
				continue;
			}
			int instanceID = ((Object)target).GetInstanceID();
			struck.TryGetValue(instanceID, out var value);
			if (value < perMob && ShootOverlaps(target, at, data.bodyWH))
			{
				struck[instanceID] = value + 1;
				budget--;
				HitAt(s, s.native, Vector2.op_Implicit(target.transform.position), facing, null, target);
				if (data.triggerSpecialCore)
				{
					SpecialCoreHit(s, target);
				}
				num++;
				if (data.multiAttackCount > 1)
				{
					((MonoBehaviour)this).StartCoroutine(MultiStrike(s, target, facing, data.multiAttackCount, data.multiAttackHitDelayMs, struck, perMob));
				}
			}
		}
		return num;
	}

	private IEnumerator MultiStrike(SkillDef s, Component target, int facing, int count, float delayMs, Dictionary<int, int> struck, int perMob)
	{
		for (int i = 1; i < count; i++)
		{
			if (delayMs > 0f)
			{
				yield return (object)new WaitForSeconds(delayMs / 1000f);
			}
			if ((Object)(object)target == (Object)null)
			{
				break;
			}
			int instanceID = ((Object)target).GetInstanceID();
			struck.TryGetValue(instanceID, out var value);
			if (value >= perMob)
			{
				break;
			}
			struck[instanceID] = value + 1;
			HitAt(s, s.native, Vector2.op_Implicit(target.transform.position), facing, null, target);
		}
	}

	private static bool ShootOverlaps(Component target, Vector3 at, Vector2 bodyWH)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		Rect val = new Rect(at.x - bodyWH.x * 0.5f / 40f, at.y - bodyWH.y * 0.5f / 40f, bodyWH.x / 40f, bodyWH.y / 40f);
		EnemyUnit enemyUnit = target as EnemyUnit;
		Rect val2 = (Rect)(((Object)(object)enemyUnit != (Object)null && enemyUnit.SelectionBounds.HasValue) ? enemyUnit.SelectionBounds.Value : new Rect(target.transform.position.x, target.transform.position.y, 0f, 0f));
		return val.Overlaps(val2);
	}

	private static void SetShootPosition(List<SkillFx> visuals, Vector3 position)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		foreach (SkillFx visual in visuals)
		{
			if ((Object)(object)visual != (Object)null)
			{
				((Component)visual).transform.position = position;
			}
		}
	}

	private IEnumerator ProjectileCast(SkillDef s, Vector2 origin, int facing, bool immediate = false)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (NativeShootObj.Has(s.native.Id))
		{
			yield return ShootObjCast(s, origin, facing, immediate);
			yield break;
		}
		List<Component> targets = SelectTargets(s, s.native, origin, facing);
		float num = (immediate ? 0f : (NativeSkillVfx.Number(s.vfx, "ball", "delay", p.SkillActionDuration * 500f) / 1000f));
		if (num > 0f)
		{
			yield return (object)new WaitForSeconds(num);
		}
		bool flag = s.art == "dragon" || s.art == "ray";
		if (targets.Count == 0)
		{
			Vector3 val = Vector2.op_Implicit(origin + Vector2.up * 0.7f);
			((MonoBehaviour)this).StartCoroutine(Fly(s, val, val + Vector3.right * (float)facing * ProjectileRange(s), null, facing));
			yield break;
		}
		if (flag)
		{
			yield return Pierce(s, origin, facing, targets);
			yield break;
		}
		foreach (Component item in targets)
		{
			((MonoBehaviour)this).StartCoroutine(Fly(s, Vector2.op_Implicit(origin + Vector2.up * 0.7f), TargetAnchor(item), item, facing));
		}
	}

	private string BallPart(SkillDef s)
	{
		if (!NativeSkillVfx.Has(s.vfx, "flipBall"))
		{
			return "ball";
		}
		if ((ballVariant++ & 1) != 0)
		{
			return "flipBall";
		}
		return "ball";
	}

	private IEnumerator RepeatingHammer(SkillDef body, Vector2 origin, int facing, float minSeconds)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> list = NativeSkillVfx.Parts(body.vfx, "ball");
		if (list.Count == 0)
		{
			yield break;
		}
		NativeVfxClip nativeVfxClip = list[hammerVariant++ % list.Count];
		Vector3 from = new Vector3(origin.x, origin.y + 0.7f, 0f);
		Vector3 to = from + Vector3.right * (float)facing * Mathf.Max(2f, ProjectileRange(body));
		SkillFx fx = Visual(body.vfx, nativeVfxClip.path, from, facing, null, loop: true);
		float duration = Mathf.Max(minSeconds, Vector3.Distance(from, to) / 16.666668f);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			if ((Object)(object)fx != (Object)null)
			{
				((Component)fx).transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
			}
			yield return null;
		}
		if ((Object)(object)fx != (Object)null)
		{
			fx.Stop();
		}
	}

	private IEnumerator Fly(SkillDef s, Vector3 from, Vector3 to, Component target, int facing)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		SkillFx fx = Visual(s.vfx, BallPart(s), from, facing, null, loop: true);
		float duration = Vector3.Distance(from, to) / 16.666668f;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			if ((Object)(object)fx != (Object)null)
			{
				((Component)fx).transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
			}
			yield return null;
		}
		if ((Object)(object)fx != (Object)null)
		{
			fx.Stop();
		}
		if ((Object)(object)target != (Object)null)
		{
			HitAt(s, s.native, Vector2.op_Implicit(target.transform.position), facing, null, target);
		}
	}

	private IEnumerator Pierce(SkillDef s, Vector2 origin, int facing, List<Component> targets)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		targets.Sort((Component a, Component b) =>
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			return Mathf.Abs(a.transform.position.x - origin.x).CompareTo(Mathf.Abs(b.transform.position.x - origin.x));
		});
		Vector3 from = Vector2.op_Implicit(origin + Vector2.up * 0.7f);
		float distance = ProjectileRange(s);
		SkillFx fx = Visual(s.vfx, BallPart(s), from, facing, null, loop: true);
		int next = 0;
		float traveled = 0f;
		while (traveled < distance)
		{
			traveled += 16.666668f * Time.deltaTime;
			if ((Object)(object)fx != (Object)null)
			{
				((Component)fx).transform.position = from + Vector3.right * (float)facing * traveled;
			}
			while (next < targets.Count && ((Object)(object)targets[next] == (Object)null || Mathf.Abs(targets[next].transform.position.x - origin.x) <= traveled))
			{
				Component val = targets[next++];
				if ((Object)(object)val != (Object)null)
				{
					HitAt(s, s.native, Vector2.op_Implicit(val.transform.position), facing, null, val);
				}
			}
			yield return null;
		}
		if ((Object)(object)fx != (Object)null)
		{
			fx.Stop();
		}
	}

	private List<Component> SelectTargets(SkillDef s, NativeSkillDefinition d, Vector2 origin, int facing)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		List<Component> list = new List<Component>();
		BossUnit[] array = Object.FindObjectsOfType<BossUnit>();
		foreach (BossUnit bossUnit in array)
		{
			if (!bossUnit.dead && InProjectileRange((Component)(object)bossUnit, s, origin, facing))
			{
				list.Add((Component)(object)bossUnit);
			}
		}
		foreach (EnemyUnit item in EnemyUnit.All)
		{
			if (!item.dead && InProjectileRange((Component)(object)item, s, origin, facing))
			{
				list.Add((Component)(object)item);
			}
		}
		list.Sort((Component a, Component b) =>
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			return (s.art == "illusion" && a is BossUnit != b is BossUnit) ? ((!(a is BossUnit)) ? 1 : (-1)) : Mathf.Abs(a.transform.position.x - origin.x).CompareTo(Mathf.Abs(b.transform.position.x - origin.x));
		});
		if (list.Count > d.TargetCount)
		{
			list.RemoveRange(d.TargetCount, list.Count - d.TargetCount);
		}
		return list;
	}

	private void ClearVisuals()
	{
		foreach (SkillFx ownedVisual in ownedVisuals)
		{
			if ((Object)(object)ownedVisual != (Object)null)
			{
				ownedVisual.Stop();
			}
		}
		ownedVisuals.Clear();
		NativeSkillVideo.ClearLayers();
	}
}
