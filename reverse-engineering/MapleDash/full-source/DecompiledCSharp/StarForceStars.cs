using MapleDash.Wz;
using UnityEngine;

public static class StarForceStars
{
	private const string Root = "UI/Enchant.img/starForce/summaryStar/";

	public const int PerRow = 15;

	public const int IconWidth = 11;

	public const int IconHeight = 10;

	public const int BlockGap = 10;

	public const int RowGap = 5;

	private static Texture2D gold;

	private static Texture2D superior;

	private static Texture2D empty;

	private static bool loaded;

	public static bool Ready
	{
		get
		{
			Load();
			return (Object)(object)gold != (Object)null;
		}
	}

	private static Texture2D Load(string name)
	{
		ImgNode imgNode = WzAssets.Node("UI/Enchant.img/starForce/summaryStar/" + name);
		if (imgNode != null)
		{
			return WzAssets.Texture(imgNode);
		}
		return null;
	}

	private static void Load()
	{
		if (!loaded)
		{
			loaded = true;
			gold = Load("normal");
			superior = Load("blue");
			empty = Load("empty");
		}
	}

	public static int Rows(int capacity)
	{
		if (capacity > 0)
		{
			return (capacity + 15 - 1) / 15;
		}
		return 0;
	}

	public static float RowWidth(int count)
	{
		return (count > 0) ? (count * 11 + (count - 1) / 5 * 10) : 0;
	}

	public static float BlockHeight(int rows)
	{
		return (rows > 0) ? (rows * 10 + (rows - 1) * 5) : 0;
	}

	public static void Draw(float centerX, float centerY, float scale, int stars, int capacity, bool isSuperior)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		Load();
		if ((Object)(object)gold == (Object)null || capacity <= 0 || scale <= 0f)
		{
			return;
		}
		int num = Rows(capacity);
		Texture2D val = ((isSuperior && (Object)(object)superior != (Object)null) ? superior : gold);
		Texture2D val2 = empty ?? gold;
		float num2 = centerY - BlockHeight(num) * scale * 0.5f;
		for (int i = 0; i < num; i++)
		{
			int num3 = Mathf.Min(15, capacity - i * 15);
			float num4 = centerX - RowWidth(num3) * scale * 0.5f;
			float num5 = num2 + (float)(i * 15) * scale;
			for (int j = 0; j < num3; j++)
			{
				Texture2D val3 = ((i * 15 + j < stars) ? val : val2);
				GUI.DrawTexture(new Rect(num4 + (float)(j * 11 + j / 5 * 10) * scale, num5, 11f * scale, 10f * scale), (Texture)(object)val3);
			}
		}
	}
}
