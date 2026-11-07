using System;

public sealed class NativeMobPursuit
{
	private long remaining;

	public bool Active { get; private set; }

	public void Acquire(int randomBelow60000)
	{
		if (!Active)
		{
			if (randomBelow60000 < 0 || randomBelow60000 >= 60000)
			{
				throw new ArgumentOutOfRangeException("randomBelow60000");
			}
			Active = true;
			remaining = 180000L + (long)randomBelow60000;
		}
	}

	public void Clear()
	{
		Active = false;
		remaining = 0L;
	}

	public void Tick(long milliseconds, bool targetAlive)
	{
		if (!targetAlive)
		{
			Clear();
		}
		else if (Active)
		{
			remaining -= milliseconds;
			if (remaining < 0)
			{
				Clear();
			}
		}
	}
}
