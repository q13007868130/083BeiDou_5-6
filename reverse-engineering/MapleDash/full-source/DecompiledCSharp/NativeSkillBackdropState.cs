using System;

public sealed class NativeSkillBackdropState
{
	private double expires;

	public string Path { get; private set; }

	public int Revision { get; private set; }

	public void Show(string path, int duration, double now)
	{
		Path = NativeSkillBackdropPacket.Normalize(path);
		duration = Math.Max(0, Math.Min(86400000, duration));
		expires = ((Path != null && duration > 0) ? (now + (double)duration) : 0.0);
		Revision++;
	}

	public int Remaining(double now)
	{
		if (Path != null && expires > 0.0 && now >= expires)
		{
			Show(null, 0, now);
		}
		if (Path != null && expires != 0.0)
		{
			return (int)Math.Min(2147483647.0, Math.Ceiling(expires - now));
		}
		return 0;
	}
}
