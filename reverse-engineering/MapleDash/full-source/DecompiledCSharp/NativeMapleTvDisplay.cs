using UnityEngine;

public static class NativeMapleTvDisplay
{
	private const float FrameDelay = 0.1f;

	private static string speaker;

	private static string message;

	private static int sendType;

	private static float until;

	private static float startedAt;

	private static bool playing;

	public static bool Playing
	{
		get
		{
			if (playing)
			{
				return Time.unscaledTime < until;
			}
			return false;
		}
	}

	public static void Start(TvResponse r)
	{
		speaker = r.speaker;
		message = r.message ?? "";
		sendType = r.sendType;
		startedAt = Time.unscaledTime;
		until = Time.unscaledTime + (float)r.durationMs / 1000f;
		playing = true;
	}

	public static void Reset()
	{
		playing = false;
		speaker = (message = null);
	}

	public static void Draw()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected Obj, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		if (!Playing)
		{
			if (playing)
			{
				playing = false;
			}
			return;
		}
		Texture2D val = ((Time.unscaledTime - startedAt < 1.6f) ? Art.Tex("wz://UI/MapleTV.img/TVon/" + (int)((Time.unscaledTime - startedAt) / 0.1f) % 16) : Art.Tex("wz://UI/MapleTV.img/TVbasic/0"));
		if (!((Object)(object)val == (Object)null))
		{
			float s = BottomBar.S;
			Rect val2 = new Rect(UiKit.W - (float)(((Texture)val).width + 6) * s, 6f * s, (float)((Texture)val).width * s, (float)((Texture)val).height * s);
			GUI.DrawTexture(val2, (Texture)(object)val);
			if (!string.IsNullOrEmpty(message))
			{
				GUIStyle val3 = new GUIStyle
				{
					font = UiKit.Font,
					fontSize = Mathf.Max(8, Mathf.RoundToInt(11f * s)),
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)1
				};
				val3.normal.textColor = Color.white;
				GUI.Label(new Rect(val2.x + 14f * s, val2.y + 24f * s, val2.width - 28f * s, 20f * s), speaker + " : " + message, val3);
			}
		}
	}
}
