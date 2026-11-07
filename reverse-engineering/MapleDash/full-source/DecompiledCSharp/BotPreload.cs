using System;
using System.Diagnostics;
using Server.Bot;
using Server.Bot.Environment;
using Server.Bot.Types.Pq;
using UnityEngine;

public sealed class BotPreload : MonoBehaviour
{
	private const float MaxWarmupSeconds = 45f;

	private bool built;

	private bool finished;

	private float warmupSeconds;

	private void Update()
	{
		if (finished)
		{
			return;
		}
		GameServer local = GameServer.Local;
		if (local == null || !BotWarmup.Ready("Bot/Items") || !BotWarmup.Ready("Bot/SoloMapling") || !BotWarmup.Ready("Bot/Maps") || !BotWarmup.Ready("Bot/ReactorTemplates") || !BotWarmup.Ready("Bot/ReactorDrops"))
		{
			return;
		}
		if (!built)
		{
			built = true;
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				_ = local.Bots;
				FileLog.Note("[Bot/Preload] runtime_ctor_ms=" + stopwatch.ElapsedMilliseconds);
				Debug.Log((object)("[Bot] 运行时已预建：假人 " + BotRegistry.Count() + " 个"));
			}
			catch (Exception ex)
			{
				Debug.LogWarning((object)("[Bot] 运行时预建失败：" + ex.Message));
				finished = true;
				Object.Destroy((Object)(object)this);
				return;
			}
		}
		if (GameSession.InGame)
		{
			PqDirector.PrefetchAll();
			finished = true;
			Object.Destroy((Object)(object)this);
			return;
		}
		warmupSeconds += Time.unscaledDeltaTime;
		bool flag = warmupSeconds >= 45f;
		PqDirector.PrefetchStep();
		if (!BotAppearanceTemplates.Enabled || BotAppearancePrebake.StandReady || flag)
		{
			try
			{
				BotWorkPump.BeginFrame();
				PqDirector.PrefetchStep();
				local.Bots.Drive((long)(Time.realtimeSinceStartup * 1000f));
			}
			catch (Exception ex2)
			{
				Debug.LogError((object)("[Bot] 假人预热异常，直接放行：" + ex2));
				finished = true;
				Object.Destroy((Object)(object)this);
				return;
			}
			if (flag || BotEnvironment.IsMarketStartupComplete)
			{
				PqDirector.PrefetchAll();
				finished = true;
				Debug.Log((object)("[Bot] 假人预热完成：在场 " + BotRegistry.Count() + " 个，进图不再生成" + (flag ? "（超时放行）" : "")));
				Object.Destroy((Object)(object)this);
			}
		}
	}
}
