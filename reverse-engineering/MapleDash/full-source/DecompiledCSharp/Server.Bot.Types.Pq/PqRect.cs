namespace Server.Bot.Types.Pq;

public struct PqRect(int x, int y, int w, int h)
{
	public int x = x;

	public int y = y;

	public int w = w;

	public int h = h;

	public int CenterX => x + w / 2;

	public int CenterY => y + h / 2;
}
