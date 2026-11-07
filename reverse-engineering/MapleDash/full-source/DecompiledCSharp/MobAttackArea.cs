using MapleDash.Wz;

public struct MobAttackArea
{
	public const float HitSlack = 10f;

	public float centerX;

	public float centerY;

	public float halfWidth;

	public float halfHeight;

	public float radius;

	public bool circle;

	public bool Contains(float dx, float dy)
	{
		float num = dx - centerX;
		float num2 = dy - centerY;
		if (circle)
		{
			float num3 = radius + 10f;
			return num * num + num2 * num2 <= num3 * num3;
		}
		if (num >= 0f - halfWidth && num <= halfWidth && num2 >= 0f - halfHeight)
		{
			return num2 <= halfHeight;
		}
		return false;
	}

	public bool Overlaps(float minX, float minY, float maxX, float maxY)
	{
		if (circle)
		{
			float num;
			if (centerX < minX)
			{
				num = minX - centerX;
			}
			else
			{
				num = ((centerX > maxX) ? (centerX - maxX) : 0f);
			}
			float num2;
			if (centerY < minY)
			{
				num2 = minY - centerY;
			}
			else
			{
				num2 = ((centerY > maxY) ? (centerY - maxY) : 0f);
			}
			float num3 = radius + 10f;
			return num * num + num2 * num2 <= num3 * num3;
		}
		if (minX <= centerX + halfWidth && maxX >= centerX - halfWidth && minY <= centerY + halfHeight)
		{
			return maxY >= centerY - halfHeight;
		}
		return false;
	}

	public static float WaveOffsetX(int start, int waveIndex, int spacing, int dir)
	{
		return ((start + waveIndex) * spacing + spacing / 2) * ((dir >= 0) ? 1 : (-1));
	}

	public static MobAttackArea Rect(int ltX, int ltY, int rbX, int rbY)
	{
		return new MobAttackArea
		{
			centerX = (float)(ltX + rbX) * 0.5f,
			centerY = (float)(ltY + rbY) * 0.5f,
			halfWidth = (float)(rbX - ltX) * 0.5f,
			halfHeight = (float)(rbY - ltY) * 0.5f
		};
	}

	public static MobAttackArea Circle(int x, int y, int r)
	{
		return new MobAttackArea
		{
			centerX = x,
			centerY = y,
			radius = r,
			circle = true
		};
	}

	public static bool TryParse(ImgNode info, out MobAttackArea area)
	{
		area = default;
		ImgNode imgNode = info?.Get("range") ?? info;
		if (imgNode == null)
		{
			return false;
		}
		ImgNode imgNode2 = imgNode.Get("lt");
		ImgNode imgNode3 = imgNode.Get("rb");
		if (imgNode2 != null && imgNode3 != null)
		{
			area = Rect(imgNode2.X, imgNode2.Y, imgNode3.X, imgNode3.Y);
			return true;
		}
		ImgNode imgNode4 = imgNode.Get("sp");
		int num = imgNode.Int("r");
		if (imgNode4 == null || num <= 0)
		{
			return false;
		}
		area = Circle(imgNode4.X, imgNode4.Y, num);
		return true;
	}
}
