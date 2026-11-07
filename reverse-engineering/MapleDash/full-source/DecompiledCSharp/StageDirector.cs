using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageDirector : MonoBehaviour
{
	public StageChapter chapter;

	public int levelIndex;

	public int segIndex;

	public bool allClear;

	public bool levelClear;

	public BossUnit boss;

	public string hint = "";

	public float hintT;

	private Coroutine run;

	public StageLevel CurLevel
	{
		get
		{
			if (chapter == null || levelIndex < 0 || levelIndex >= chapter.levels.Count)
			{
				return null;
			}
			return chapter.levels[levelIndex];
		}
	}

	public StageSegment CurSegment
	{
		get
		{
			StageLevel curLevel = CurLevel;
			if (curLevel == null || segIndex < 0 || segIndex >= curLevel.segments.Count)
			{
				return null;
			}
			return curLevel.segments[segIndex];
		}
	}

	public int SegmentProgress
	{
		get
		{
			if (CurLevel != null)
			{
				return segIndex;
			}
			return 0;
		}
	}

	public void Begin(StageChapter ch, int startLevel = 0)
	{
		chapter = ch;
		levelIndex = startLevel;
		allClear = false;
		if (run != null)
		{
			((MonoBehaviour)this).StopCoroutine(run);
		}
		run = ((MonoBehaviour)this).StartCoroutine(RunAll());
	}

	private IEnumerator RunAll()
	{
		while (levelIndex < chapter.levels.Count)
		{
			levelClear = false;
			yield return RunLevel(chapter.levels[levelIndex]);
			if ((Object)(object)GameLoop.I != (Object)null && GameLoop.I.gameOver)
			{
				yield break;
			}
			levelClear = true;
			Say("「" + chapter.levels[levelIndex].name + "」通过", 2f);
			yield return (object)new WaitForSecondsRealtime(1.4f);
			levelIndex++;
		}
		allClear = true;
		Say("第一章完成", 999f);
	}

	private IEnumerator RunLevel(StageLevel lv)
	{
		ApplyTheme(lv.theme);
		string key = "Bgm00/RestNPeace";
		if (lv.theme == "harbor")
		{
			key = "Bgm00/GoPicnic";
		}
		else if (lv.theme == "cliff" || lv.theme == "snow")
		{
			key = "Bgm00/SleepyWood";
		}
		AudioManager.PlayBgm(key);
		if (!string.IsNullOrEmpty(lv.teaches))
		{
			Say(lv.teaches, 3.2f);
		}
		for (segIndex = 0; segIndex < lv.segments.Count; segIndex++)
		{
			StageSegment stageSegment = lv.segments[segIndex];
			if (!string.IsNullOrEmpty(stageSegment.hint))
			{
				Say(stageSegment.hint, 2.6f);
			}
			switch (stageSegment.kind)
			{
			case "walk":
				yield return RunWalk(stageSegment);
				break;
			case "combat":
				yield return RunCombat(stageSegment);
				break;
			case "rest":
				yield return RunRest(stageSegment);
				break;
			case "boss":
				yield return RunBoss(stageSegment);
				break;
			default:
				yield return (object)new WaitForSecondsRealtime(1f);
				break;
			}
			if ((Object)(object)GameLoop.I != (Object)null && GameLoop.I.gameOver)
			{
				break;
			}
		}
	}

	private IEnumerator RunWalk(StageSegment seg)
	{
		float t = ((seg.duration > 0f) ? seg.duration : 6f);
		while (t > 0f && (!((Object)(object)GameLoop.I != (Object)null) || !GameLoop.I.gameOver))
		{
			t -= Time.unscaledDeltaTime;
			yield return null;
		}
	}

	private IEnumerator RunRest(StageSegment seg)
	{
		float t = ((seg.duration > 0f) ? seg.duration : 3f);
		while (t > 0f && (!((Object)(object)GameLoop.I != (Object)null) || !GameLoop.I.gameOver))
		{
			t -= Time.unscaledDeltaTime;
			yield return null;
		}
	}

	private IEnumerator RunCombat(StageSegment seg)
	{
		foreach (StageSpawn sp in seg.spawns)
		{
			for (int i = 0; i < sp.count; i++)
			{
				if (!((Object)(object)GameLoop.I == (Object)null) && !GameLoop.I.gameOver)
				{
					Spawn(sp);
					yield return (object)new WaitForSecondsRealtime(Mathf.Max(0.05f, sp.gap));
					continue;
				}
				yield break;
			}
		}
		while (true)
		{
			if (EnemyUnit.All.Count > 0)
			{
				if (!((Object)(object)GameLoop.I == (Object)null) && !GameLoop.I.gameOver)
				{
					yield return null;
					continue;
				}
				break;
			}
			yield return (object)new WaitForSecondsRealtime(0.25f);
			break;
		}
	}

	private IEnumerator RunBoss(StageSegment seg)
	{
		StageBossDef value = null;
		if (!string.IsNullOrEmpty(seg.boss))
		{
			chapter.bosses.TryGetValue(seg.boss, out value);
		}
		if (value == null && chapter.bosses.Count > 0)
		{
			using Dictionary<string, StageBossDef>.Enumerator enumerator = chapter.bosses.GetEnumerator();
			if (enumerator.MoveNext())
			{
				value = enumerator.Current.Value;
			}
		}
		if (value == null)
		{
			Debug.LogWarning((object)("[Stage] 找不到 Boss 定义：" + seg.boss));
			yield break;
		}
		GameObject val = new GameObject("Boss_" + value.id);
		BossUnit b = val.AddComponent<BossUnit>();
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		float num = (((Object)(object)playerCtrl != (Object)null) ? playerCtrl.worldX : 0f) + 7.5f;
		if ((Object)(object)GameLoop.I != (Object)null)
		{
			num = GameLoop.I.ClampSpawnX(num);
		}
		b.Init(value, num);
		boss = b;
		while ((Object)(object)b != (Object)null && !b.dead)
		{
			if ((Object)(object)GameLoop.I != (Object)null && GameLoop.I.gameOver)
			{
				yield break;
			}
			yield return null;
		}
		boss = null;
		yield return (object)new WaitForSecondsRealtime(1.1f);
	}

	private void Spawn(StageSpawn sp)
	{
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		float num = (((Object)(object)playerCtrl != (Object)null) ? playerCtrl.worldX : 0f);
		int lane;
		if (sp.z == "random")
		{
			lane = Random.Range(0, 5);
		}
		else
		{
			lane = ((!int.TryParse(sp.z, out var result)) ? Random.Range(0, 5) : Mathf.Clamp(result, 0, 4));
		}
		if (sp.side == "both")
		{
			GameLoop.I.SpawnMob(sp.type, num + Random.Range(9f, 12f), lane);
			GameLoop.I.SpawnMob(sp.type, num - Random.Range(9f, 12f), Random.Range(0, 5));
		}
		else
		{
			bool flag = sp.side != "left";
			float x = num + (flag ? 1f : (-1f)) * Random.Range(9f, 12f);
			GameLoop.I.SpawnMob(sp.type, x, lane);
		}
	}

	public void OnBossSpawned(BossUnit b)
	{
		boss = b;
	}

	public void OnBossKilled(BossUnit b)
	{
		if ((Object)(object)boss == (Object)(object)b)
		{
			boss = null;
		}
	}

	public void OnBossTelegraph(float seconds)
	{
		HitFeel.Punch(0.5f);
	}

	private void ApplyTheme(string key)
	{
		if (chapter != null && !((Object)(object)GameLoop.I == (Object)null) && !string.IsNullOrEmpty(key) && chapter.themes.TryGetValue(key, out var value))
		{
			GameLoop.I.ApplyTheme(value, key);
		}
	}

	public void Say(string text, float seconds)
	{
		if (!string.IsNullOrEmpty(text))
		{
			hint = text;
			hintT = seconds;
		}
	}

	private void Update()
	{
		if (hintT > 0f)
		{
			hintT -= Time.unscaledDeltaTime;
		}
	}

	public void StopSession()
	{
		((MonoBehaviour)this).StopAllCoroutines();
		run = null;
		boss = null;
	}

	private void OnDisable()
	{
		StopSession();
	}

	public void RestartLevel()
	{
		StopSession();
		levelIndex = Mathf.Clamp(levelIndex, 0, chapter.levels.Count - 1);
		allClear = false;
		levelClear = false;
		boss = null;
		hint = "";
		hintT = 0f;
		run = ((MonoBehaviour)this).StartCoroutine(RunAll());
	}
}
