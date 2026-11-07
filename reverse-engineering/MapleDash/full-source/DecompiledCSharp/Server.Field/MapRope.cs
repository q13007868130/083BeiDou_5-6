using System;

namespace Server.Field;

public sealed class MapRope
{
	public int id;

	public int x;

	public int y1;

	public int y2;

	public bool ladder;

	public bool upperExit;

	public int Top => Math.Min(y1, y2);

	public int Bottom => Math.Max(y1, y2);
}
