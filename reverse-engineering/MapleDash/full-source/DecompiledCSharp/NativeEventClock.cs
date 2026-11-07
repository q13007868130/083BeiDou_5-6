using MapleDash.Wz;
using UnityEngine;

public static class NativeEventClock
{
	private const string PanelRoot = "Map/Obj/etc.img/timer/";

	private const string FontRoot = "Map/Obj/etc.img/clock/fontTime/";

	private const int PanelX = 271;

	private const int PanelY = 30;

	private const int PanelW = 258;

	private const int PanelH = 58;

	private const int DigitY = 12;

	private static readonly int[] SlotX = new int[4] { 86, 112, 179, 205 };

	private static int totalSeconds;

	private static float shownAt;

	private static bool visible;

	private static readonly Texture2D[] digits = new Texture2D[10];

	private static Texture2D backgrnd;

	private static Texture2D backgrndHour;

	private static bool texturesLoaded;

	public static bool Visible => visible;

	public static void Show(int seconds)
	{
		if (seconds <= 0)
		{
			Hide();
			return;
		}
		totalSeconds = seconds;
		shownAt = Time.unscaledTime;
		visible = true;
	}

	public static void Hide()
	{
		visible = false;
		totalSeconds = 0;
	}

	public static void Draw()
	{
		if (visible)
		{
			int num = totalSeconds - Mathf.FloorToInt(Time.unscaledTime - shownAt);
			if (num < 0)
			{
				num = 0;
			}
			DrawTimer(num);
			if (num == 0)
			{
				Hide();
			}
		}
	}

	private static void DrawTimer(int left)
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		bool flag = left >= 3600;
		int num = (flag ? (left / 3600) : (left / 60));
		int num2 = (flag ? (left % 3600 / 60) : (left % 60));
		if (num > 99)
		{
			num = 99;
		}
		LoadTextures();
		float s = BottomBar.S;
		float num3 = (UiKit.W - 800f * s) * 0.5f + 271f * s;
		float num4 = 30f * s;
		Texture2D val = (flag ? backgrndHour : backgrnd);
		if ((Object)(object)val == (Object)null)
		{
			DrawFallback(num3, num4, num, num2, s);
			return;
		}
		GUI.DrawTexture(new Rect(num3, num4, 258f * s, 58f * s), (Texture)(object)val);
		if (num / 10 != 0)
		{
			Digit(digits[num / 10], SlotX[0], num3, num4, s);
		}
		Digit(digits[num % 10], SlotX[1], num3, num4, s);
		Digit(digits[num2 / 10], SlotX[2], num3, num4, s);
		Digit(digits[num2 % 10], SlotX[3], num3, num4, s);
	}

	private static void Digit(Texture2D texture, int slotX, float x, float y, float s)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)texture == (Object)null))
		{
			GUI.DrawTexture(new Rect(x + (float)slotX * s, y + 12f * s, (float)((Texture)texture).width * s, (float)((Texture)texture).height * s), (Texture)(object)texture);
		}
	}

	private static void LoadTextures()
	{
		if (texturesLoaded)
		{
			return;
		}
		texturesLoaded = true;
		if (WzAssets.HasFile("Map/Obj/etc.img"))
		{
			backgrnd = Texture("Map/Obj/etc.img/timer/backgrnd");
			backgrndHour = Texture("Map/Obj/etc.img/timer/backgrndhour");
			for (int i = 0; i < 10; i++)
			{
				digits[i] = Texture("Map/Obj/etc.img/clock/fontTime/" + i);
			}
		}
	}

	private static Texture2D Texture(string path)
	{
		ImgNode imgNode = WzAssets.Node(path);
		if (imgNode != null)
		{
			return WzAssets.Texture(imgNode);
		}
		return null;
	}

	private static void DrawFallback(float x, float y, int high, int low, float s)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected Obj, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		string text = high.ToString("00") + ":" + low.ToString("00");
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)4,
			fontSize = Mathf.RoundToInt(26f * s)
		};
		val.normal.textColor = new Color(1f, 0.94f, 0.55f);
		GUI.Label(new Rect(x, y, 258f * s, 58f * s), text, val);
	}
}
