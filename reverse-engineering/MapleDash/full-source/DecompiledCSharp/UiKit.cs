using UnityEngine;

public static class UiKit
{
	public const float DesignH = 720f;

	private static Font _font;

	private static bool _fontTried;

	private static bool logged;

	public static float Scale { get; private set; } = 1f;

	public static float W { get; private set; } = 1280f;

	public static float H { get; private set; } = 720f;

	public static Font Font => _font;

	public static void RefreshMetrics()
	{
		Scale = Mathf.Clamp((float)Screen.height / 720f, 0.5f, 4f);
		W = (float)Screen.width / Scale;
		H = (float)Screen.height / Scale;
		LogOnce();
	}

	private static void LogOnce()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!logged)
		{
			logged = true;
			Rect safeArea = Screen.safeArea;
			Debug.Log((object)("[Ui] screen=" + Screen.width + "x" + Screen.height + " dpi=" + Screen.dpi + " scale=" + Scale.ToString("0.00") + " virtual=" + W.ToString("0") + "x" + H.ToString("0") + " safe=" + safeArea.x.ToString("0") + "," + safeArea.y.ToString("0") + " " + safeArea.width.ToString("0") + "x" + safeArea.height.ToString("0") + " insets(l/r/t/b)=" + safeArea.x.ToString("0") + "/" + ((float)Screen.width - safeArea.xMax).ToString("0") + "/" + ((float)Screen.height - safeArea.yMax).ToString("0") + "/" + safeArea.y.ToString("0")));
		}
	}

	public static void Begin()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		RefreshMetrics();
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Scale, Scale, 1f));
		if (!_fontTried)
		{
			_fontTried = true;
			_font = Resources.Load<Font>("Fonts/ui");
		}
		ApplyFont();
	}

	private static void ApplyFont()
	{
		if ((Object)(object)_font == (Object)null)
		{
			return;
		}
		try
		{
			if ((Object)(object)GUI.skin != (Object)null && (Object)(object)GUI.skin.font != (Object)(object)_font)
			{
				GUI.skin.font = _font;
			}
		}
		catch
		{
		}
	}

	public static Vector2 MouseGui()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (Pointer.UsesVirtualCursor)
		{
			return Pointer.Pos;
		}
		return new Vector2(Input.mousePosition.x / Scale, ((float)Screen.height - Input.mousePosition.y) / Scale);
	}

	public static void Fill(Rect r, Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color;
	}
}
