namespace Server.Field;

public sealed class FootholdSegment
{
	public int id;

	public int previous;

	public int next;

	public int layer;

	public int group;

	public bool forbidFallDown;

	public float x1;

	public float y1;

	public float x2;

	public float y2;

	public bool Wall => x1 == x2;

	public double Slope
	{
		get
		{
			if (!Wall)
			{
				return (double)(y2 - y1) / (double)(x2 - x1);
			}
			return 0.0;
		}
	}

	public FootholdSegment(float x1, float y1, float x2, float y2)
	{
		this.x1 = x1;
		this.y1 = y1;
		this.x2 = x2;
		this.y2 = y2;
	}
}
