using System;
using UnityEngine;

public static class NativeArenaHud
{
	private static ArenaResponse state;

	private static long nextSync;

	private static bool warpPending;

	private static int warpTarget;

	private const int SnowballMap = 109060000;

	private const int CoconutMap = 109080000;

	public static bool Visible
	{
		get
		{
			if (state != null)
			{
				return state.kind != 0;
			}
			return false;
		}
	}

	private static float S => BottomBar.S;

	public static void Reset()
	{
		state = null;
		nextSync = 0L;
		warpPending = false;
		warpTarget = 0;
	}

	public static void Draw()
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null)
		{
			return;
		}
		int lastMapId = Profile.LastMapId;
		if (lastMapId != 109060000 && lastMapId != 109080000)
		{
			if (state != null)
			{
				Reset();
			}
			return;
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (num >= nextSync)
		{
			nextSync = num + 1000;
			Sync();
		}
		if ((warpPending && Profile.LastMapId != warpTarget) || state == null || state.kind == 0)
		{
			return;
		}
		ApplyNotices(state);
		Rect r = TopRect();
		UiKit.Fill(r, new Color(0f, 0f, 0f, 0.55f));
		GUIStyle val = Style(13, Color.white, (TextAnchor)0);
		GUIStyle val2 = Style(13, new Color(1f, 0.85f, 0.3f), (TextAnchor)0);
		float num2 = r.x + 8f * S;
		float num3 = r.y + 6f * S;
		if (state.kind == 1)
		{
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), "雪球大战  " + FormatLeft(state.timeLeftMs) + "   我方(队" + state.myTeam + ") " + state.myPosition + " : " + state.otherPosition + " 对方", val);
			num3 += 20f * S;
			DrawBar(num2, num3, r.width - 16f * S, 8f * S, (float)state.myPosition / 620f, new Color(0.35f, 0.65f, 1f));
			DrawBar(num2, num3 + 10f * S, r.width - 16f * S, 8f * S, (float)state.otherPosition / 620f, new Color(1f, 0.5f, 0.4f));
			num3 += 24f * S;
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), "我方雪人 HP " + state.mySnowmanHp, (state.mySnowmanHp > 500) ? val : val2);
			num3 += 20f * S;
			if (!state.ended && ActionButton(num2, num3, "推雪球", 90f))
			{
				Hit((state.myTeam != 0) ? 1 : 0);
			}
			if (!state.ended && ActionButton(num2 + 100f * S, num3, "打雪人", 90f))
			{
				Hit((state.myTeam == 0) ? 2 : 3);
			}
		}
		else
		{
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), "椰子树大战  " + FormatLeft(state.timeLeftMs) + "   Maple " + state.mapleScore + " : " + state.storyScore + " Story", val);
			num3 += 20f * S;
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), "可击落 " + state.falling + "  炸弹 " + state.bombings + "  哑弹 " + state.stopped, val);
			num3 += 20f * S;
			if (!state.ended && ActionButton(num2, num3, "敲椰子", 90f))
			{
				Hit(-1);
			}
		}
		if (state.ended)
		{
			num3 += 24f * S;
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), (state.warpTo != 0) ? "比赛结束，正在传送…" : "比赛结束", val2);
			if (state.warpTo != 0 && !warpPending)
			{
				warpPending = true;
				warpTarget = state.warpTo;
				GameLoop.I?.field?.WarpTo(state.warpTo);
			}
		}
	}

	private static void DrawBar(float x, float y, float w, float h, float t, Color c)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		t = Mathf.Clamp01(t);
		UiKit.Fill(new Rect(x, y, w, h), new Color(1f, 1f, 1f, 0.25f));
		if (t > 0.01f)
		{
			UiKit.Fill(new Rect(x, y, w * t, h), c);
		}
	}

	private static bool ActionButton(float x, float y, string label, float w)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		Rect val = new Rect(x, y, w * S, 24f * S);
		bool flag = val.Contains(Pointer.Pos);
		UiKit.Fill(val, flag ? new Color(0.3f, 0.5f, 0.9f, 0.95f) : new Color(0.2f, 0.35f, 0.7f, 0.9f));
		GUI.Label(val, label, Style(13, Color.white, (TextAnchor)4));
		int num;
		if (flag && (int)Event.current.type == 7)
		{
			num = (Pointer.Clicked ? 1 : 0);
			if (num != 0)
			{
				Pointer.ConsumeClick();
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private static Rect TopRect()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		return new Rect((UiKit.W - 300f * S) / 2f, 44f * S, 300f * S, 108f * S);
	}

	private static GUIStyle Style(int size, Color color, TextAnchor anchor)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.Max(8, Mathf.RoundToInt((float)size * S)),
			alignment = anchor
		};
		val.normal.textColor = color;
		return val;
	}

	private static string FormatLeft(long ms)
	{
		int num = Mathf.Max(0, Mathf.CeilToInt((float)ms / 1000f));
		return num / 60 + ":" + (num % 60).ToString("D2");
	}

	private static void Sync()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (GameSession.Character == null)
		{
			return;
		}
		ArenaResponse arenaResponse = GameSession.Net.Arena(new ArenaRequest
		{
			characterId = GameSession.CharacterId,
			operation = ArenaOperation.Sync
		});
		if (arenaResponse == null || !arenaResponse.ok)
		{
			return;
		}
		if (arenaResponse.kind == 0)
		{
			if (state != null)
			{
				Reset();
			}
			return;
		}
		if (state == null || state.kind != arenaResponse.kind)
		{
			BottomBar.AddChat((arenaResponse.kind == 1) ? "雪球大战开始！" : "椰子树大战开始！", BottomBar.ChatYellow);
		}
		state = arenaResponse;
		ApplyNotices(arenaResponse);
		if (arenaResponse.warpTo != 0 && !warpPending)
		{
			_ = Profile.LastMapId;
			if (arenaResponse.kind == 1)
			{
				_ = 109060000;
			}
			else
				_ = 109080000;
		}
	}

	private static void Hit(int param)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (GameSession.Character == null)
		{
			return;
		}
		ArenaResponse arenaResponse = GameSession.Net.Arena(new ArenaRequest
		{
			characterId = GameSession.CharacterId,
			operation = ArenaOperation.Hit,
			param = param
		});
		if (arenaResponse != null)
		{
			if (!arenaResponse.ok && !string.IsNullOrEmpty(arenaResponse.error))
			{
				BottomBar.AddChat(arenaResponse.error, BottomBar.ChatPink);
				return;
			}
			state = arenaResponse;
			ApplyNotices(arenaResponse);
		}
	}

	private static void ApplyNotices(ArenaResponse res)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(res.notice))
		{
			return;
		}
		string[] array = res.notice.Split('\n');
		foreach (string text in array)
		{
			if (!string.IsNullOrEmpty(text))
			{
				BottomBar.AddChat(text, BottomBar.ChatYellow);
			}
		}
	}
}
