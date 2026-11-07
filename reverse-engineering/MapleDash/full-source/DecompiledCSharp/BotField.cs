using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MapleDash.Wz;
using Server.Bot;
using Server.Bot.Environment;
using Server.Bot.Social;
using Server.Field;
using UnityEngine;

public sealed class BotField : MonoBehaviour
{
	private sealed class View
	{
		public GameObject root;

		public SpriteAnim animation;

		public string appearance;

		public NativeBotChair chair;

		public BotActor actor;

		public float previousY;

		public bool groundedPose;

		public string groundPose;

		public Vector3 target;

		public float speed;

		public float measured;
	}

	private sealed class Balloon
	{
		public string text;

		public float until;

		public string wrapped;

		public float innerW;

		public float innerH;

		public int measuredRing = -1;

		public string[] ringLines;

		public float ringWidth;
	}

	private const int MaxCreatePerFrame = 1;

	private const float MapEnterBudgetMs = 6f;

	private const float CatchUpSeconds = 1f;

	private const float SyncSeconds = 0.1f;

	private const float BalloonSeconds = 5f;

	private const float BalloonFadeSeconds = 0.5f;

	private const float TeleportPx = 200f;

	private readonly Dictionary<int, View> views = new Dictionary<int, View>();

	private readonly Dictionary<int, Balloon> balloons = new Dictionary<int, Balloon>();

	private readonly HashSet<int> present = new HashSet<int>();

	private readonly List<int> removed = new List<int>();

	private readonly Queue<int> pending = new Queue<int>();

	private BotOutput output;

	private int currentMap;

	private int currentWorld;

	private int currentChannel;

	private float lastSync = -10f;

	private float catchUpUntil = -1f;

	private bool synchronized;

	private float nextDiagnostic;

	private int indexedCount;

	private int offlineCount;

	private int worldCount;

	private int channelCount;

	private NativeMarketField market;

	private const float ProjectileSpeed = 16.666668f;

	private int hitVariant;

	private static bool balloonDebugTried;

	private static bool balloonDebug;

	private static GUIStyle balloonStyle;

	private static Texture2D balloonNw;

	private static Texture2D balloonNe;

	private static Texture2D balloonSw;

	private static Texture2D balloonSe;

	private static Texture2D balloonN;

	private static Texture2D balloonS;

	private static Texture2D balloonW;

	private static Texture2D balloonE;

	private static Texture2D balloonC;

	private static Texture2D balloonArrow;

	private static bool balloonArtTried;

	private const float ArtBaseHeight = 768f;

	private const int MaxCharsPerLine = 11;

	private static readonly Dictionary<int, Texture2D> balloonBodies = new Dictionary<int, Texture2D>();

	private void OnEnable()
	{
		output = BotTypeManager.Output;
		output.Published += OnOutput;
		AudioManager.Preload("Game/EnchantSuccess", "Game/EnchantFailure");
	}

	private void OnOutput(BotOutput.Message message)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		BotCharacter botCharacter = BotRegistry.Get(message.actorId);
		if (synchronized && botCharacter != null && message.mapId == currentMap && botCharacter.world == currentWorld && botCharacter.channel == currentChannel)
		{
			if (message.kind == BotOutput.Kind.Chat && !string.IsNullOrEmpty(message.text))
			{
				BottomBar.AddChat(botCharacter.name + " : " + message.text, BottomBar.ChatWhite);
				ShowBalloon(message.actorId, botCharacter.name + ": " + message.text);
			}
			if (message.kind == BotOutput.Kind.Bubble && !string.IsNullOrEmpty(message.text))
			{
				ShowBalloon(message.actorId, botCharacter.name + ": " + message.text);
			}
			if (message.kind == BotOutput.Kind.Emote)
			{
				Express(message.actorId, message.value);
			}
			if (message.kind == BotOutput.Kind.Scroll && views.TryGetValue(message.actorId, out var value))
			{
				string text = ((message.value == 1) ? "Success" : "Failure");
				NativeImgAnimation.Spawn("Effect/BasicEff.img/Enchant/" + text, value.root.transform, value.root.transform.position, 360);
				AudioManager.Play("Game/Enchant" + text, 1f, pitchVary: false);
			}
			if (message.kind == BotOutput.Kind.Attack && views.TryGetValue(message.actorId, out var value2))
			{
				value2.animation.PlayNativeAttack(message.text, message.value);
			}
		}
	}

	public void Synchronize(int mapId, int world, int channel)
	{
		using (BotPerf.Timer("BotField.Sync"))
		{
			if ((Object)(object)market == (Object)null)
			{
				market = ((Component)this).GetComponent<NativeMarketField>() ?? ((Component)this).gameObject.AddComponent<NativeMarketField>();
			}
			market.Synchronize(mapId, world, channel);
			bool flag = synchronized && mapId == currentMap;
			if (flag && world == currentWorld && channel == currentChannel && Time.unscaledTime - lastSync < 0.1f)
			{
				return;
			}
			bool flag2 = !flag;
			lastSync = Time.unscaledTime;
			currentMap = mapId;
			currentWorld = world;
			currentChannel = channel;
			synchronized = true;
			present.Clear();
			indexedCount = (offlineCount = (worldCount = (channelCount = 0)));
			foreach (BotCharacter item in BotRegistry.OnMap(mapId))
			{
				indexedCount++;
				if (!item.online)
				{
					offlineCount++;
					continue;
				}
				if (item.world != world)
				{
					worldCount++;
					continue;
				}
				if (item.channel != channel)
				{
					channelCount++;
					continue;
				}
				present.Add(item.id);
				if (!views.ContainsKey(item.id))
				{
					if (!pending.Contains(item.id))
					{
						pending.Enqueue(item.id);
					}
				}
				else
				{
					Apply(item, views[item.id]);
				}
			}
			removed.Clear();
			foreach (KeyValuePair<int, View> view in views)
			{
				if (!present.Contains(view.Key))
				{
					removed.Add(view.Key);
				}
			}
			foreach (int item2 in removed)
			{
				RemoveView(views[item2]);
				views.Remove(item2);
				balloons.Remove(item2);
			}
			if (flag2)
			{
				catchUpUntil = Time.unscaledTime + 1f;
				if (BotAppearancePrebake.Finished)
				{
					CreateQueued(int.MaxValue, 6f);
				}
				else
				{
					CreateQueued(1);
				}
				if (pending.Count > 0)
				{
					Debug.Log((object)("[Bot] 进图建视图：本图 " + present.Count + " 个，" + 6f + "ms 预算内没建完，剩 " + pending.Count + " 个分帧续建"));
				}
			}
			else
			{
				CreateQueued(1);
			}
			if (!FileLog.Enabled || (!flag2 && !(Time.unscaledTime >= nextDiagnostic)))
			{
				return;
			}
			nextDiagnostic = Time.unscaledTime + 10f;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			string text = "";
			foreach (BotCharacter value in BotRegistry.All().Values)
			{
				if (value.mapId == mapId)
				{
					num++;
					if (value.online && value.world == world && value.channel == channel && !present.Contains(value.id))
					{
						num2++;
					}
					if (text.Length == 0)
					{
						text = value.id + ":" + value.world + "/" + value.channel + "/" + value.online;
					}
				}
			}
			foreach (View value2 in views.Values)
			{
				if ((Object)(object)value2.root != (Object)null && value2.root.activeInHierarchy && (Object)(object)value2.animation.Renderer != (Object)null && ((Renderer)value2.animation.Renderer).enabled && (Object)(object)value2.animation.Renderer.sprite != (Object)null)
				{
					num3++;
				}
			}
			Debug.Log((object)("[Bot/View] map=" + mapId + " context=" + world + "/" + channel + " global=" + BotRegistry.Count() + " actual=" + num + " indexed=" + indexedCount + " offline=" + offlineCount + " wrongWorld=" + worldCount + " wrongChannel=" + channelCount + " indexMissing=" + num2 + " eligible=" + present.Count + " pending=" + pending.Count + " views=" + views.Count + " sprites=" + num3 + " sample(id:world/channel/online)=" + text));
		}
	}

	private void CreateQueued(int limit, float budgetMs = -1f)
	{
		using (BotPerf.Timer("BotField.CreateQueued"))
		{
			int num = 0;
			long num2 = ((budgetMs > 0f) ? Stopwatch.GetTimestamp() : 0);
			while (num < limit && pending.Count > 0)
			{
				if (budgetMs > 0f)
				{
					if (num > 0 && (double)(Stopwatch.GetTimestamp() - num2) * 1000.0 / (double)Stopwatch.Frequency >= (double)budgetMs)
					{
						break;
					}
				}
				else if (num > 0 && !BotWorkPump.Has(1f))
				{
					break;
				}
				int num3 = pending.Dequeue();
				if (present.Contains(num3) && !views.ContainsKey(num3))
				{
					BotCharacter botCharacter = BotRegistry.Get(num3);
					if (botCharacter != null)
					{
						Create(botCharacter);
						num++;
					}
				}
			}
		}
	}

	private void Create(BotActor actor)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected Obj, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		if (!(actor is BotCharacter actor2))
		{
			return;
		}
		using (BotPerf.Timer("BotField.Create"))
		{
			GameObject val = new GameObject("SoloMapling_" + actor.id);
			val.transform.SetParent(((Component)this).transform, false);
			SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
			((Renderer)val2).sortingOrder = 300;
			View view = new View
			{
				root = val,
				animation = val.AddComponent<SpriteAnim>(),
				previousY = actor.y
			};
			view.chair = val.AddComponent<NativeBotChair>();
			string[] array = BotAppearance.CharacterIds(actor2);
			string text = "bot_" + string.Join("_", array);
			WzCharacter.Appearance(text, array);
			view.animation.Init("chars", text, "stand", val2);
			view.animation.throttleWhenBusy = true;
			view.animation.Play("stand", once: false, force: true);
			view.appearance = text;
			view.target = new Vector3(actor.x / 40f, (0f - actor.y) / 40f, 0f);
			val.transform.position = view.target;
			views.Add(actor.id, view);
			Apply(actor, view);
		}
	}

	private void Apply(BotActor actor, View view)
	{
		view.actor = actor;
		if (!(actor is BotCharacter botCharacter))
		{
			return;
		}
		string[] array = BotAppearance.CharacterIds(botCharacter);
		string text = "bot_" + string.Join("_", array);
		if (botCharacter.mountActive && botCharacter.mountItemId > 0 && botCharacter.chair <= 0)
		{
			WzCharacter.Appearance(text, array);
			text = WzCharacter.MountedAppearance(text, botCharacter.mountItemId);
		}
		if (view.appearance != text)
		{
			if (!botCharacter.mountActive || botCharacter.chair > 0)
			{
				WzCharacter.Appearance(text, array);
			}
			view.appearance = text;
			view.animation.key = text;
			view.animation.Play(view.animation.Current, view.animation.IsOneShot, force: true);
		}
		string text2 = ((botCharacter.chair > 0) ? "sit" : Pose(botCharacter.stance));
		view.groundedPose = botCharacter.chair <= 0 && (text2 == "walk" || text2 == "stand");
		view.groundPose = text2;
		view.animation.SetBase(text2);
		if (botCharacter.chair > 0 && view.animation.Current != "sit")
		{
			view.animation.Play("sit", once: false, force: true);
		}
		view.animation.SetFacing(((botCharacter.stance & 1) == 0) ? 1 : (-1));
		view.chair.Set(botCharacter.chair, view.animation);
		view.chair.Refresh();
		if (text2 == "rope" || text2 == "ladder")
		{
			view.animation.speed = ((!botCharacter.moving && botCharacter.y == view.previousY) ? 0f : 1f);
		}
		else if (text2 != "walk")
		{
			view.animation.speed = 1f;
		}
		view.previousY = botCharacter.y;
	}

	private void Update()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		using (BotPerf.Timer("BotField.Update"))
		{
			if (Time.unscaledTime < catchUpUntil)
			{
				CreateQueued(int.MaxValue, 6f);
			}
			else
			{
				CreateQueued(1);
			}
			float deltaTime = Time.deltaTime;
			foreach (View value in views.Values)
			{
				TrackPosition(value, deltaTime);
				Vector3 position = value.root.transform.position;
				Vector3 val = Vector3.MoveTowards(position, value.target, value.speed * deltaTime);
				if (value.groundedPose)
				{
					bool flag = false;
					MapRuntime mapRuntime = MapService.Get(currentMap);
					if (mapRuntime != null)
					{
						BotGroundIndex ground = MapGeometryCache.Get(mapRuntime).ground;
						int x = Mathf.RoundToInt(val.x * 40f);
						int num = Mathf.RoundToInt((0f - val.y) * 40f);
						FootholdSegment footholdSegment = ground.Ground(x, num);
						if (footholdSegment != null)
						{
							int num2 = BotGroundIndex.YAt(footholdSegment, x);
							if (Mathf.Abs(num2 - num) <= 26)
							{
								val.y = (float)(-(num2 - 1)) / 40f;
								flag = true;
							}
						}
					}
					value.animation.SetBase(flag ? value.groundPose : "jump");
				}
				value.root.transform.position = val;
				if (!(deltaTime <= 0f))
				{
					float num3 = Vector3.Distance(position, val) * 40f / deltaTime;
					value.measured = Mathf.Lerp(value.measured, num3, 1f - Mathf.Exp(-10f * deltaTime));
					if (value.animation.Current == "walk")
					{
						value.animation.speed = Mathf.Clamp(value.measured / 125f, 0.25f, 2f);
					}
				}
			}
			if (balloons.Count == 0)
			{
				return;
			}
			float unscaledTime = Time.unscaledTime;
			removed.Clear();
			foreach (KeyValuePair<int, Balloon> balloon in balloons)
			{
				if (unscaledTime >= balloon.Value.until)
				{
					removed.Add(balloon.Key);
				}
			}
			foreach (int item in removed)
			{
				balloons.Remove(item);
			}
		}
	}

	private void TrackPosition(View view, float step)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		BotActor actor = view.actor;
		if (actor == null || (Object)(object)view.root == (Object)null)
		{
			return;
		}
		Vector3 val = new Vector3(actor.x / 40f, (0f - actor.y) / 40f, 0f);
		float num = Vector3.Distance(view.target, val) * 40f;
		if (!(num < 0.01f))
		{
			view.target = val;
			if (num >= 200f)
			{
				view.root.transform.position = val;
				view.speed = 0f;
			}
			else
			{
				float num2 = Mathf.Max(step * 2f, 0.02f);
				view.speed = Mathf.Clamp(num / 40f / num2, 0.05f, 22.5f);
			}
		}
	}

	private static string Pose(int stance)
	{
		return (stance & -2) switch
		{
			2 => "walk", 
			6 => "jump", 
			14 => "ladder", 
			16 => "rope", 
			_ => "stand", 
		};
	}

	public void PlaySkillCast(BotCharacter actor, int skillId, int level, Component target = null)
	{
		if (views.TryGetValue(actor.id, out var value) && !((Object)(object)value.root == (Object)null))
		{
			int level2 = Mathf.Max(1, level);
			string key = NativeSkillVfx.Key(skillId, level2);
			string text = "skill_" + skillId + "@" + level2;
			if (Art.GetClip("chars", value.animation.key, text) == null)
			{
				text = "atk";
			}
			CastBranches(value, key, text, actor.facing, target);
		}
	}

	private void CastBranches(View view, string key, string action, int facing, Component target)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Transform val = (((Object)(object)view.root != (Object)null) ? view.root.transform : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		float num = 0f;
		foreach (NativeVfxClip item in NativeSkillVfx.Parts(key, "prepare"))
		{
			SpawnPart(key, item, val.position, facing, val);
			num = Mathf.Max(num, item.Duration);
		}
		if (num > 0f)
		{
			((MonoBehaviour)this).StartCoroutine(CastAfterPrepare(view, key, action, facing, target, num));
		}
		else
		{
			CastNow(view, key, action, facing, target);
		}
	}

	private IEnumerator CastAfterPrepare(View view, string key, string action, int facing, Component target, float delay)
	{
		yield return (object)new WaitForSeconds(delay);
		if (view != null && !((Object)(object)view.root == (Object)null))
		{
			CastNow(view, key, action, facing, target);
		}
	}

	private void CastNow(View view, string key, string action, int facing, Component target)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		Transform val = (((Object)(object)view.root != (Object)null) ? view.root.transform : null);
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		view.animation.PlayOnce(action);
		string[] array = new string[2] { "effect", "effect0" };
		foreach (string branch in array)
		{
			foreach (NativeVfxClip item in NativeSkillVfx.Parts(key, branch))
			{
				bool flag = NativeSkillVfx.Number(key, item.path, "z") < 0f;
				SpawnPart(key, item, val.position, facing, val, flag ? 299 : 360);
			}
		}
		foreach (NativeVfxClip item2 in NativeSkillVfx.Parts(key, "affected"))
		{
			SpawnPart(key, item2, val.position, facing, val);
		}
		if (NativeSkillVfx.Has(key, "special"))
		{
			SpecialBurst(key, val.position, facing);
		}
		if (!((Object)(object)target == (Object)null))
		{
			if (NativeSkillVfx.Has(key, "ball"))
			{
				((MonoBehaviour)this).StartCoroutine(FlyBall(val, key, target, facing));
			}
			else
			{
				PlaySkillHit(key, target, facing);
			}
		}
	}

	private void SpecialBurst(string key, Vector3 origin, int facing)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "special");
		if (list.Count != 0)
		{
			int count = Mathf.Max(1, (int)NativeSkillVfx.Number(key, "special", "count", list.Count));
			float start = NativeSkillVfx.Number(key, "special", "start") / 1000f;
			float interval = NativeSkillVfx.Number(key, "special", "interval", 60f) / 1000f;
			float width = NativeSkillVfx.Number(key, "special", "x", 200f) / 40f;
			float num = NativeSkillVfx.Number(key, "special", "y") / 40f;
			float fall = ((num > 0f) ? (NativeSkillVfx.Number(key, "special", "duration", 500f) / 1000f) : 0f);
			((MonoBehaviour)this).StartCoroutine(BurstParts(key, list, origin, facing, count, start, interval, width, num, fall));
		}
	}

	private IEnumerator BurstParts(string key, List<NativeVfxClip> parts, Vector3 origin, int facing, int count, float start, float interval, float width, float height, float fall)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (start > 0f)
		{
			yield return (object)new WaitForSeconds(start);
		}
		for (int i = 0; i < count; i++)
		{
			float num = origin.x + (((float)i + 0.5f) / (float)count * 2f - 1f) * width;
			Vector3 val = new Vector3(num, origin.y + height, 0f);
			NativeVfxClip nativeVfxClip = parts[i % parts.Count];
			SkillFx skillFx = SkillFx.PlayPart(key, nativeVfxClip.path, val, facing);
			if ((Object)(object)skillFx != (Object)null && fall > 0f)
			{
				((MonoBehaviour)this).StartCoroutine(FallToGround(skillFx, val, new Vector3(num, PlayerCtrl.GroundAt(num, origin.y), 0f), fall));
			}
			yield return (object)new WaitForSeconds(Mathf.Max(0.001f, interval));
		}
	}

	private IEnumerator FallToGround(SkillFx fx, Vector3 from, Vector3 to, float duration)
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

	private IEnumerator FlyBall(Transform foot, string key, Component target, int facing)
	{
		if ((Object)(object)foot == (Object)null || (Object)(object)target == (Object)null)
		{
			yield break;
		}
		Vector3 from = foot.position + Vector3.up * 0.7f;
		float duration = Vector3.Distance(from, HitAnchor(target, 0f)) / 16.666668f;
		SkillFx ball = SkillFx.PlayPart(key, "ball", from, facing, 1f, 360, loop: true);
		float elapsed = 0f;
		while (elapsed < duration && !((Object)(object)foot == (Object)null) && !((Object)(object)target == (Object)null))
		{
			elapsed += Time.deltaTime;
			if ((Object)(object)ball != (Object)null)
			{
				((Component)ball).transform.position = Vector3.Lerp(from, HitAnchor(target, 0f), Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration)));
			}
			yield return null;
		}
		if ((Object)(object)ball != (Object)null)
		{
			ball.Stop();
		}
		if ((Object)(object)target != (Object)null)
		{
			PlaySkillHit(key, target, facing);
		}
	}

	private static SkillFx SpawnPart(string key, NativeVfxClip clip, Vector3 point, int facing, Transform follow, int order = 360)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		SkillFx skillFx = SkillFx.PlayPart(key, clip.path, point, facing, 1f, order);
		if ((Object)(object)skillFx != (Object)null && (Object)(object)follow != (Object)null)
		{
			skillFx.Follow(follow);
		}
		return skillFx;
	}

	public void PlaySkillHit(int skillId, int level, Component target, int facing)
	{
		PlaySkillHit(NativeSkillVfx.Key(skillId, Mathf.Max(1, level)), target, facing);
	}

	private void PlaySkillHit(string key, Component target, int facing)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			return;
		}
		List<NativeVfxClip> list = NativeSkillVfx.Parts(key, "hit");
		if (list.Count != 0)
		{
			NativeVfxClip nativeVfxClip = list[hitVariant++ % list.Count];
			SkillFx skillFx = SkillFx.PlayPart(key, nativeVfxClip.path, HitAnchor(target, NativeSkillVfx.Number(key, nativeVfxClip.path, "pos")), facing);
			if ((Object)(object)skillFx != (Object)null)
			{
				skillFx.Follow(target.transform);
			}
		}
	}

	private static Vector3 HitAnchor(Component target, float pos)
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

	public void Express(int actorId, int expression)
	{
		if (views.TryGetValue(actorId, out var value))
		{
			if (expression == 0)
			{
				value.animation.ClearExpression();
			}
			else
			{
				value.animation.Express(expression);
			}
		}
	}

	public Nullable PickupTarget(int actorId)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (!views.TryGetValue(actorId, out var value))
		{
			return null;
		}
		SpriteRenderer renderer = value.animation.Renderer;
		float num;
		if (!((Object)(object)renderer != (Object)null) || !((Object)(object)renderer.sprite != (Object)null))
		{
			num = 0f;
		}
		else
		{
			Bounds bounds = ((Renderer)renderer).bounds;
			num = bounds.size.y;
		}
		float num2 = num;
		return Vector2.op_Implicit(value.root.transform.position) + Vector2.up * (num2 * 0.5f);
	}

	public bool TryFeet(int actorId, out Vector3 feet)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		feet = default;
		if (!views.TryGetValue(actorId, out var value) || (Object)(object)value.root == (Object)null)
		{
			return false;
		}
		feet = value.root.transform.position;
		return true;
	}

	public void Clear()
	{
		if ((Object)(object)market != (Object)null)
		{
			market.Clear();
		}
		foreach (View value in views.Values)
		{
			RemoveView(value);
		}
		views.Clear();
		present.Clear();
		removed.Clear();
		pending.Clear();
		balloons.Clear();
		synchronized = false;
		lastSync = -10f;
	}

	private static void RemoveView(View view)
	{
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)view.root);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)view.root);
		}
	}

	private void OnDisable()
	{
		if (output != null)
		{
			output.Published -= OnOutput;
		}
		output = null;
		Clear();
	}

	public void ShowLocalBalloon(string text)
	{
		ShowBalloon(GameSession.CharacterId, text);
	}

	private void ShowBalloon(int actorId, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (balloons.Count > 64)
		{
			balloons.Clear();
		}
		balloons[actorId] = new Balloon
		{
			text = text,
			until = Time.unscaledTime + 5f
		};
		if (!balloonDebugTried)
		{
			balloonDebugTried = true;
			balloonDebug = PlayerPrefs.GetInt("debug_balloon", 0) != 0;
			if (balloonDebug)
			{
				PlayerPrefs.DeleteKey("debug_balloon");
			}
		}
		if (balloonDebug)
		{
			BotCharacter botCharacter = BotRegistry.Get(actorId);
			int num = ((actorId == GameSession.CharacterId) ? NativeNameTag.RingStyle("chatBalloon") : 0);
			bool flag = num == 0 || NativeChatBalloon.Prepare(num, 16, 1) != null;
			Debug.Log((object)("[Bot/Balloon] speaker=" + (botCharacter?.name ?? "?") + "(#" + actorId + ") ring=" + num + ((num == 0) ? "" : (flag ? " 有素材" : " 素材缺失→回退默认")) + " map=" + (botCharacter?.mapId ?? 0) + " x=" + (botCharacter?.x ?? 0f) + " y=" + (botCharacter?.y ?? 0f) + " text=" + text));
		}
	}

	private static void EnsureBalloonArt()
	{
		if (!balloonArtTried)
		{
			balloonArtTried = true;
			balloonNw = TryTexture("UI/ChatBalloon.img/0/nw");
			balloonNe = TryTexture("UI/ChatBalloon.img/0/ne");
			balloonSw = TryTexture("UI/ChatBalloon.img/0/sw");
			balloonSe = TryTexture("UI/ChatBalloon.img/0/se");
			balloonN = TryTexture("UI/ChatBalloon.img/0/n");
			balloonS = TryTexture("UI/ChatBalloon.img/0/s");
			balloonW = TryTexture("UI/ChatBalloon.img/0/w");
			balloonE = TryTexture("UI/ChatBalloon.img/0/e");
			balloonC = TryTexture("UI/ChatBalloon.img/0/c");
			balloonArrow = TryTexture("UI/ChatBalloon.img/0/arrow");
		}
	}

	private static Texture2D TryTexture(string path)
	{
		try
		{
			ImgNode imgNode = WzAssets.Node(path);
			return (imgNode == null) ? null : WzAssets.Texture(imgNode);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void OnGUI()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Invalid comparison between Unknown and I4
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Expected Obj, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_052d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0532: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0504: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		//IL_056c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_076b: Unknown result type (might be due to invalid IL or missing references)
		//IL_077b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0601: Unknown result type (might be due to invalid IL or missing references)
		//IL_0606: Unknown result type (might be due to invalid IL or missing references)
		//IL_0608: Unknown result type (might be due to invalid IL or missing references)
		//IL_0611: Unknown result type (might be due to invalid IL or missing references)
		//IL_0616: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0636: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_065d: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0704: Unknown result type (might be due to invalid IL or missing references)
		//IL_0742: Unknown result type (might be due to invalid IL or missing references)
		//IL_0749: Unknown result type (might be due to invalid IL or missing references)
		//IL_0842: Unknown result type (might be due to invalid IL or missing references)
		//IL_0847: Unknown result type (might be due to invalid IL or missing references)
		//IL_084c: Unknown result type (might be due to invalid IL or missing references)
		//IL_084e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0856: Unknown result type (might be due to invalid IL or missing references)
		//IL_085b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0860: Unknown result type (might be due to invalid IL or missing references)
		//IL_087b: Unknown result type (might be due to invalid IL or missing references)
		//IL_089c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0901: Unknown result type (might be due to invalid IL or missing references)
		//IL_0929: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0946: Unknown result type (might be due to invalid IL or missing references)
		//IL_095f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0969: Unknown result type (might be due to invalid IL or missing references)
		//IL_0976: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		if ((int)Event.current.type != 7)
		{
			return;
		}
		using (BotPerf.Timer("BotField.Repaint"))
		{
			if (ShopWindow.Visible || (!synchronized && balloons.Count == 0))
			{
				return;
			}
			Bounds bounds;
			Bounds bounds2;
			if ((int)Event.current.type == 7 && Pointer.DoubleClicked && !Pointer.Blocked && !GameWindows.AnyVisible)
			{
				Camera main = Camera.main;
				if ((Object)(object)main != (Object)null)
				{
					Vector3 val = main.ScreenToWorldPoint(new Vector3(Pointer.Pos.x * UiKit.Scale, (float)Screen.height - Pointer.Pos.y * UiKit.Scale, 0f));
					if (!((Object)(object)market != (Object)null) || !market.TryVisit(val, (int id) => (!views.TryGetValue(id, out var value3)) ? null : value3.animation.Renderer))
					{
						foreach (KeyValuePair<int, View> view in views)
						{
							SpriteRenderer renderer = view.Value.animation.Renderer;
							if ((Object)(object)renderer != (Object)null && (Object)(object)renderer.sprite != (Object)null)
							{
								bounds = ((Renderer)renderer).bounds;
								float x = val.x;
								float y = val.y;
								bounds2 = ((Renderer)renderer).bounds;
								if (bounds.Contains(new Vector3(x, y, bounds2.center.z)))
								{
									NativeCharacterInfo.Open(BotRegistry.Get(view.Key));
									break;
								}
							}
						}
						if (!NativeCharacterInfo.Visible)
						{
							GameLoop i = GameLoop.I;
							object obj;
							if (i == null)
							{
								obj = null;
							}
							else
							{
								PlayerCtrl player = i.player;
								obj = ((player == null) ? null : ((Component)player).GetComponentInChildren<SpriteAnim>()?.Renderer);
							}
							SpriteRenderer val2 = (SpriteRenderer)obj;
							if ((Object)(object)val2 != (Object)null && (Object)(object)val2.sprite != (Object)null)
							{
								bounds = ((Renderer)val2).bounds;
								float x2 = val.x;
								float y2 = val.y;
								bounds2 = ((Renderer)val2).bounds;
								if (bounds.Contains(new Vector3(x2, y2, bounds2.center.z)))
								{
									NativeCharacterInfo.Open();
								}
							}
						}
					}
				}
			}
			if ((int)Event.current.type == 7 && Pointer.RightPressed && !Pointer.Blocked && !NativeUi.WindowAtPointer && !NativeTradeWindow.Visible && !NativeUtilPrompt.Visible)
			{
				Camera main2 = Camera.main;
				if ((Object)(object)main2 != (Object)null)
				{
					Vector3 val3 = main2.ScreenToWorldPoint(new Vector3(Pointer.Pos.x * UiKit.Scale, (float)Screen.height - Pointer.Pos.y * UiKit.Scale, 0f));
					foreach (KeyValuePair<int, View> view2 in views)
					{
						SpriteRenderer renderer2 = view2.Value.animation.Renderer;
						if ((Object)(object)renderer2 != (Object)null && (Object)(object)renderer2.sprite != (Object)null)
						{
							bounds = ((Renderer)renderer2).bounds;
							float x3 = val3.x;
							float y3 = val3.y;
							bounds2 = ((Renderer)renderer2).bounds;
							if (bounds.Contains(new Vector3(x3, y3, bounds2.center.z)))
							{
								NativeCharacterMenu.Open(BotRegistry.Get(view2.Key));
								break;
							}
						}
					}
				}
			}
			if (balloons.Count == 0)
			{
				return;
			}
			Camera main3 = Camera.main;
			if ((Object)(object)main3 == (Object)null)
			{
				return;
			}
			EnsureBalloonArt();
			if ((Object)(object)balloonC == (Object)null)
			{
				return;
			}
			UiKit.Begin();
			if (balloonStyle == null)
			{
				balloonStyle = new GUIStyle(GUI.skin.label)
				{
					font = UiKit.Font,
					fontSize = 12,
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)0
				};
			}
			float num = Mathf.Max(0.35f, UiKit.H / 768f);
			float unscaledTime = Time.unscaledTime;
			foreach (KeyValuePair<int, Balloon> balloon in balloons)
			{
				Transform transform;
				SpriteRenderer val4;
				if (balloon.Key == GameSession.CharacterId)
				{
					PlayerCtrl playerCtrl = GameLoop.I?.player;
					if (!GameSession.InGame || (Object)(object)playerCtrl == (Object)null)
					{
						continue;
					}
					transform = ((Component)playerCtrl).transform;
					val4 = ((Component)playerCtrl).GetComponentInChildren<SpriteAnim>()?.Renderer;
				}
				else
				{
					if (!views.TryGetValue(balloon.Key, out var value))
					{
						continue;
					}
					transform = value.root.transform;
					val4 = value.animation.Renderer;
				}
				Balloon value2 = balloon.Value;
				float num2 = Mathf.Clamp01((value2.until - unscaledTime) / 0.5f);
				float num3;
				if (!((Object)(object)val4 != (Object)null) || !((Object)(object)val4.sprite != (Object)null))
				{
					num3 = transform.position.y + 0.55f;
				}
				else
				{
					bounds = ((Renderer)val4).bounds;
					num3 = bounds.max.y;
				}
				float num4 = num3;
				Vector3 val5 = main3.WorldToScreenPoint(new Vector3(transform.position.x, num4, 0f));
				if (val5.z < 0f)
				{
					continue;
				}
				Vector2 val6 = new Vector2(val5.x / UiKit.Scale / num, ((float)Screen.height - val5.y) / UiKit.Scale / num);
				if (value2.wrapped == null)
				{
					Measure(value2, balloonStyle);
				}
				float innerW = value2.innerW;
				float innerH = value2.innerH;
				int num5 = ((balloon.Key == GameSession.CharacterId) ? NativeNameTag.RingStyle("chatBalloon") : 0);
				if (num5 != 0 && value2.measuredRing != num5)
				{
					MeasureRing(value2, balloonStyle, num5);
				}
				NativeChatBalloon.Layout layout = ((num5 == 0) ? null : NativeChatBalloon.Prepare(num5, (int)value2.ringWidth, value2.ringLines.Length));
				if (layout != null)
				{
					Matrix4x4 matrix = GUI.matrix;
					Color color = GUI.color;
					GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num, num, 1f));
					GUI.color = new Color(1f, 1f, 1f, num2);
					Texture2D val7 = layout.Frame(unscaledTime);
					Rect val8 = new Rect(val6.x - (float)layout.anchorX, val6.y - (float)((Texture)val7).height, (float)((Texture)val7).width, (float)((Texture)val7).height);
					GUI.DrawTexture(val8, (Texture)(object)val7);
					balloonStyle.normal.textColor = layout.color;
					Rect text = layout.text;
					text.position += val8.position;
					int fontSize = balloonStyle.fontSize;
					balloonStyle.fontSize = NativeChatBalloon.FontSize(num5);
					text.height = layout.lineHeight;
					string[] ringLines = value2.ringLines;
					foreach (string text2 in ringLines)
					{
						GUI.Label(text, text2, balloonStyle);
						text.y += (float)layout.lineHeight;
					}
					balloonStyle.fontSize = fontSize;
					GUI.color = color;
					GUI.matrix = matrix;
					continue;
				}
				float num7 = innerW + 12f;
				float num8 = innerH + 12f;
				Rect val9 = new Rect(val6.x - num7 * 0.5f, val6.y - (((Object)(object)balloonArrow != (Object)null) ? ((float)((Texture)balloonArrow).height - 6f) : 7f) - num8, num7, num8);
				float num9 = val9.x + (num7 - (((Object)(object)balloonArrow != (Object)null) ? ((float)((Texture)balloonArrow).width) : 13f)) * 0.5f;
				float num10 = UiKit.W / num;
				float num11 = UiKit.H / num;
				if (!(val9.xMax < -40f) && !(val9.x > num10 + 40f) && !(val9.yMax < -40f) && !(val9.y > num11 + 40f))
				{
					Matrix4x4 matrix2 = GUI.matrix;
					Color color2 = GUI.color;
					GUI.matrix = matrix2 * Matrix4x4.Scale(new Vector3(num, num, 1f));
					GUI.color = new Color(1f, 1f, 1f, num2);
					Texture2D val10 = BalloonBody((int)innerW, (int)innerH);
					if ((Object)(object)val10 != (Object)null)
					{
						GUI.DrawTexture(val9, (Texture)(object)val10);
					}
					if ((Object)(object)balloonArrow != (Object)null)
					{
						GUI.DrawTexture(new Rect(num9, val9.yMax - 6f, (float)((Texture)balloonArrow).width, (float)((Texture)balloonArrow).height), (Texture)(object)balloonArrow);
					}
					balloonStyle.normal.textColor = new Color(0f, 0f, 0f, num2);
					GUI.Label(new Rect(val9.x + 6f, val9.y + 6f, innerW, innerH), value2.wrapped, balloonStyle);
					if (balloonDebug)
					{
						Color color3 = GUI.color;
						GUI.color = new Color(1f, 0.2f, 0.2f, 1f);
						GUI.DrawTexture(new Rect(val6.x - 3f, val6.y - 3f, 6f, 6f), (Texture)(object)Texture2D.whiteTexture);
						GUI.color = color3;
					}
					GUI.color = color2;
					GUI.matrix = matrix2;
				}
			}
		}
	}

	private static void Measure(Balloon balloon, GUIStyle style)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected Obj, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		string text = balloon.text ?? string.Empty;
		StringBuilder stringBuilder = new StringBuilder(text.Length + 16);
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		while (num3 < text.Length)
		{
			int i = num3;
			int num4 = 0;
			for (; i < text.Length && text[i] != '\n'; i++)
			{
				char c = text[i];
				if (c != ' ' && num4 + 1 > 11)
				{
					break;
				}
				if (c != ' ')
				{
					num4++;
				}
			}
			if (i == num3 && text[num3] != '\n')
			{
				i = num3 + 1;
			}
			string text2 = text.Substring(num3, i - num3).Trim();
			if (text2.Length > 0)
			{
				if (num2 > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(text2);
				num2++;
				num = Mathf.Max(num, style.CalcSize(new GUIContent(text2)).x);
			}
			num3 = ((i < text.Length && text[i] == '\n') ? (i + 1) : i);
		}
		if (num2 == 0)
		{
			num2 = 1;
		}
		balloon.wrapped = stringBuilder.ToString();
		balloon.innerW = Mathf.Max(12f, Mathf.Ceil(num));
		float num5 = Mathf.Max(14f, style.lineHeight);
		balloon.innerH = Mathf.Max(14f, (float)num2 * num5);
	}

	private static void MeasureRing(Balloon balloon, GUIStyle style, int ring)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected Obj, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected Obj, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		int fontSize = style.fontSize;
		style.fontSize = NativeChatBalloon.FontSize(ring);
		float num = NativeChatBalloon.TextWidth(ring);
		float num2 = 0f;
		string text = balloon.text ?? "";
		List<string> list = new List<string>();
		while (text.Length > 0)
		{
			int i;
			for (i = 0; i < text.Length && text[i] != '\n' && style.CalcSize(new GUIContent(text.Substring(0, i + 1))).x <= num; i++)
			{
			}
			if (i == 0 && text[0] != '\n')
			{
				i = 1;
			}
			string text2 = text.Substring(0, i).TrimEnd(' ', '\t', '\r', '\n');
			list.Add(text2);
			num2 = Mathf.Max(num2, style.CalcSize(new GUIContent(text2)).x);
			int j;
			for (j = i; j < text.Length && " \t\r\n".IndexOf(text[j]) >= 0; j++)
			{
			}
			text = text.Substring(j);
		}
		if (list.Count == 0)
		{
			list.Add("");
		}
		balloon.ringLines = list.ToArray();
		balloon.ringWidth = Mathf.Ceil(num2);
		balloon.measuredRing = ring;
		style.fontSize = fontSize;
	}

	private static Texture2D BalloonBody(int innerW, int innerH)
	{
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected Obj, but got Unknown
		int key = (innerW << 16) | innerH;
		if (balloonBodies.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		if (balloonBodies.Count > 48)
		{
			foreach (Texture2D value2 in balloonBodies.Values)
			{
				if ((Object)(object)value2 != (Object)null)
				{
					Object.Destroy((Object)(object)value2);
				}
			}
			balloonBodies.Clear();
		}
		int num = innerW + 12;
		int num2 = innerH + 12;
		Color32[] array = new Color32[num * num2];
		Blit(balloonNw, array, num, num2, 0, 0, 6, 6);
		Blit(balloonNe, array, num, num2, num - 6, 0, 6, 6);
		Blit(balloonSw, array, num, num2, 0, num2 - 6, 6, 6);
		Blit(balloonSe, array, num, num2, num - 6, num2 - 6, 6, 6);
		Blit(balloonN, array, num, num2, 6, 0, innerW, 6);
		Blit(balloonS, array, num, num2, 6, num2 - 6, innerW, 6);
		Blit(balloonW, array, num, num2, 0, 6, 6, innerH);
		Blit(balloonE, array, num, num2, num - 6, 6, 6, innerH);
		Blit(balloonC, array, num, num2, 6, 6, innerW, innerH);
		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false)
		{
			name = "ChatBalloon_" + num + "x" + num2,
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		val.SetPixels32(array);
		val.Apply();
		balloonBodies[key] = val;
		return val;
	}

	private static void Blit(Texture2D piece, Color32[] dst, int dw, int dh, int x, int top, int w, int h)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)piece == (Object)null || w <= 0 || h <= 0)
		{
			return;
		}
		Color32[] pixels = piece.GetPixels32();
		int width = ((Texture)piece).width;
		int height = ((Texture)piece).height;
		for (int i = 0; i < h; i++)
		{
			int num = top + i;
			if (num < 0 || num >= dh)
			{
				continue;
			}
			int num2 = (dh - 1 - num) * dw;
			int num3 = height - 1 - i % height;
			for (int j = 0; j < w; j++)
			{
				int num4 = x + j;
				if (num4 >= 0 && num4 < dw)
				{
					dst[num2 + num4] = pixels[num3 * width + j % width];
				}
			}
		}
	}
}
