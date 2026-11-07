using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Bot;
using UnityEngine;

public sealed class BotAppearancePrebake : MonoBehaviour
{
	private static readonly string[] AnimOrder = new string[4] { "stand", "walk", "sit", "jump" };

	private const double FrameBudgetMs = 6.0;

	private const float InGameBudgetMs = 8f;

	private const float WarmupWaitSeconds = 20f;

	private int templateIndex;

	private int animIndex;

	private int frameIndex;

	private int frameCount;

	private bool started;

	private bool finished;

	private float waitSeconds;

	private readonly Stopwatch budget = new Stopwatch();

	public static bool Finished { get; private set; }

	public static bool StandReady { get; private set; }

	private void Awake()
	{
		Finished = false;
		StandReady = false;
	}

	private void Update()
	{
		if (finished)
		{
			return;
		}
		if (!started)
		{
			GameServer local = GameServer.Local;
			if (local == null)
			{
				return;
			}
			waitSeconds += Time.unscaledDeltaTime;
			bool flag = BotWarmup.Ready("Bot/Items") && BotWarmup.Ready("Bot/SoloMapling");
			if (!flag && waitSeconds < 20f)
			{
				return;
			}
			if (!flag)
			{
				Debug.LogWarning((object)"[Bot] 假人资源预热超时，先按现有数据预合成");
			}
			BotAppearanceTemplates.Ensure(local.Db);
			started = true;
			if (!BotAppearanceTemplates.Enabled)
			{
				Finish();
				return;
			}
			Debug.Log((object)("[Bot] 纸娃娃预合成开始：模板 " + BotAppearanceTemplates.All.Count + " 套"));
		}
		budget.Restart();
		bool inGame = GameSession.InGame;
		if (inGame && !BotWorkPump.Has(8f))
		{
			return;
		}
		try
		{
			do
			{
				if (!Step())
				{
					Finish();
					break;
				}
			}
			while (!inGame && budget.Elapsed.TotalMilliseconds < 6.0);
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[Bot] 纸娃娃预合成异常，跳过剩余模板：" + ex));
			Finish();
		}
	}

	private bool Step()
	{
		IReadOnlyList<BotAppearanceTemplate> all = BotAppearanceTemplates.All;
		if (animIndex >= AnimOrder.Length)
		{
			return false;
		}
		if (templateIndex >= all.Count)
		{
			animIndex++;
			templateIndex = 0;
			frameIndex = 0;
			frameCount = 0;
			if (animIndex == 1)
			{
				StandReady = true;
			}
			return animIndex < AnimOrder.Length;
		}
		BotAppearanceTemplate botAppearanceTemplate = all[templateIndex];
		if (frameCount == 0)
		{
			WzCharacter.Appearance(botAppearanceTemplate.key, botAppearanceTemplate.ids);
			frameCount = WzCharacter.PrecomposeFrameCount(botAppearanceTemplate.key, AnimOrder[animIndex]);
			if (frameCount == 0)
			{
				templateIndex++;
				frameIndex = 0;
				return true;
			}
		}
		if (frameIndex < frameCount)
		{
			WzCharacter.PrecomposeFrame(botAppearanceTemplate.key, AnimOrder[animIndex], frameIndex);
			frameIndex++;
			return true;
		}
		templateIndex++;
		frameIndex = 0;
		frameCount = 0;
		return true;
	}

	private void Finish()
	{
		finished = true;
		Finished = true;
		StandReady = true;
		if (BotAppearanceTemplates.Enabled)
		{
			Debug.Log((object)("[Bot] 纸娃娃预合成结束：模板 " + BotAppearanceTemplates.All.Count + " 套"));
		}
		Object.Destroy((Object)(object)this);
	}
}
