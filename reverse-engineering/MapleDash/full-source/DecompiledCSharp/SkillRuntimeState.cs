using System;

public sealed class SkillRuntimeState
{
	public double ReadyAt { get; private set; }

	public double Remaining(double now)
	{
		return Math.Max(0.0, ReadyAt - now);
	}

	public void Reset()
	{
		ReadyAt = 0.0;
	}

	public bool TryStart(double now, double duration)
	{
		if (double.IsNaN(now) || double.IsNaN(duration) || duration < 0.0 || Remaining(now) > 0.0)
		{
			return false;
		}
		ReadyAt = now + duration;
		return true;
	}

	public void Reduce(double now, int percent)
	{
		double num = Remaining(now);
		if (!(num <= 0.0) && percent > 0)
		{
			ReadyAt = now + num * (1.0 - (double)Math.Min(100, percent) / 100.0);
		}
	}
}
