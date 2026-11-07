using System;

public sealed class NativeMobHealth
{
	public long Maximum { get; }

	public long Current { get; private set; }

	public NativeMobHealth(long maximum)
	{
		Maximum = Math.Max(1L, maximum);
		Current = Maximum;
	}

	public long Damage(long amount)
	{
		long num = Math.Min(Current, Math.Max(0L, amount));
		Current -= num;
		return num;
	}

	public void Heal(long amount)
	{
		if (amount >= 0)
		{
			Current += Math.Min(Maximum - Current, amount);
		}
		else
		{
			Damage((amount == long.MinValue) ? long.MaxValue : (-amount));
		}
	}

	public void SetPercent(int percent)
	{
		percent = Math.Max(0, Math.Min(100, percent));
		Current = Math.Max(1L, Maximum / 100 * percent + Maximum % 100 * percent / 100);
	}
}
