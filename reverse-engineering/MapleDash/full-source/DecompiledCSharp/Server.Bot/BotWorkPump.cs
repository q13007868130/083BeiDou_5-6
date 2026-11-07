using System.Diagnostics;

namespace Server.Bot;

public static class BotWorkPump
{
	public const float BudgetMs = 4f;

	private static readonly Stopwatch watch = Stopwatch.StartNew();

	private static double frameBeginMs = -1.0;

	public static double FrameSpentMs
	{
		get
		{
			if (frameBeginMs < 0.0)
			{
				return 0.0;
			}
			return watch.Elapsed.TotalMilliseconds - frameBeginMs;
		}
	}

	public static void BeginFrame()
	{
		frameBeginMs = watch.Elapsed.TotalMilliseconds;
	}

	public static bool Has(float reserveMs = 0f)
	{
		return FrameSpentMs + (double)reserveMs < 4.0;
	}
}
