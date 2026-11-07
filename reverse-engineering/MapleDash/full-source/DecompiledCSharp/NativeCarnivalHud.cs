using System;
using UnityEngine;

public static class NativeCarnivalHud
{
	private static readonly int[] Fields = new int[6] { 980000101, 980000201, 980000301, 980000401, 980000501, 980000601 };

	private static CarnivalResponse state;

	private static long nextSync;

	private static bool warpPending;

	private static int warpTarget;

	private static bool announced;

	private static bool finishAnnounced;

	public static bool InGame
	{
		get
		{
			if (state != null)
			{
				return state.inCarnival;
			}
			return false;
		}
	}

	private static float S => BottomBar.S;

	public static bool OnCarnivalMap(int mapId)
	{
		int[] fields = Fields;
		for (int i = 0; i < fields.Length; i++)
		{
			if (fields[i] == mapId)
			{
				return true;
			}
		}
		return false;
	}

	public static void Reset()
	{
		state = null;
		nextSync = 0L;
		warpPending = false;
		warpTarget = 0;
		announced = false;
		finishAnnounced = false;
	}

	public static void ReportKill(int cp)
	{
		if (InGame && cp > 0 && GameSession.Character != null)
		{
			CarnivalResponse carnivalResponse = GameSession.Net.Carnival(new CarnivalRequest
			{
				characterId = GameSession.CharacterId,
				operation = 1,
				cp = cp
			});
			if (carnivalResponse != null && carnivalResponse.ok)
			{
				state = carnivalResponse;
			}
		}
	}

	public static void Draw()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)GameLoop.I == (Object)null || (Object)(object)GameLoop.I.player == (Object)null)
		{
			return;
		}
		if (!OnCarnivalMap(Profile.LastMapId))
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
		if ((warpPending && Profile.LastMapId != warpTarget) || state == null || !state.inCarnival)
		{
			return;
		}
		Rect r = TopRect();
		UiKit.Fill(r, new Color(0f, 0f, 0f, 0.55f));
		GUIStyle val = Style(13, Color.white, (TextAnchor)0);
		GUIStyle val2 = Style(13, new Color(1f, 0.5f, 0.4f), (TextAnchor)0);
		GUIStyle val3 = Style(13, new Color(0.45f, 0.7f, 1f), (TextAnchor)0);
		GUIStyle val4 = Style(13, new Color(1f, 0.85f, 0.3f), (TextAnchor)0);
		float num2 = r.x + 8f * S;
		float num3 = r.y + 6f * S;
		GUI.Label(new Rect(num2, num3, 140f * S, 18f * S), "红队 CP " + state.redCp, val2);
		GUI.Label(new Rect(num2 + 90f * S, num3, 140f * S, 18f * S), FormatLeft(state.secondsLeft), val);
		GUI.Label(new Rect(num2 + 160f * S, num3, 140f * S, 18f * S), "蓝队 CP " + state.blueCp, val3);
		num3 += 20f * S;
		GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), "累计  " + state.redTotal + " : " + state.blueTotal + "  （判定用累计分）", val);
		num3 += 20f * S;
		if (state.finished || state.warpTo != 0)
		{
			GUI.Label(new Rect(num2, num3, r.width - 16f, 18f * S), state.win ? ("胜利！名望分 +" + state.festivalPoints + "，正在传送…") : "战败…正在传送…", val4);
			if (state.warpTo != 0 && !warpPending)
			{
				warpPending = true;
				warpTarget = state.warpTo;
				GameLoop.I?.field?.WarpTo(state.warpTo);
			}
		}
		else if (ActionButton(num2, num3, "认输离场", 90f))
		{
			CarnivalResponse carnivalResponse = GameSession.Net.Carnival(new CarnivalRequest
			{
				characterId = GameSession.CharacterId,
				operation = 3
			});
			if (carnivalResponse != null && carnivalResponse.ok)
			{
				state = carnivalResponse;
			}
		}
	}

	private static void Sync()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if (GameSession.Character == null)
		{
			return;
		}
		CarnivalResponse carnivalResponse = GameSession.Net.Carnival(new CarnivalRequest
		{
			characterId = GameSession.CharacterId,
			operation = 2
		});
		if (carnivalResponse != null && carnivalResponse.ok)
		{
			if (carnivalResponse.inCarnival && !announced)
			{
				BottomBar.AddChat("怪物嘉年华开始！击杀怪物为红队赢取 CP！", BottomBar.ChatYellow);
				announced = true;
			}
			if (carnivalResponse.finished && !finishAnnounced)
			{
				finishAnnounced = true;
				BottomBar.AddChat(carnivalResponse.win ? ("怪物嘉年华胜利！名望分 " + carnivalResponse.festivalPoints + "（找斯皮格尔曼兑换奖励）") : ("怪物嘉年华战败…累计 CP " + carnivalResponse.redTotal + " : " + carnivalResponse.blueTotal), carnivalResponse.win ? BottomBar.ChatYellow : BottomBar.ChatPink);
			}
			state = carnivalResponse;
			if (carnivalResponse.finished && !string.IsNullOrEmpty(carnivalResponse.error))
			{
				BottomBar.AddChat(carnivalResponse.error, BottomBar.ChatPink);
			}
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
		UiKit.Fill(val, flag ? new Color(0.7f, 0.3f, 0.3f, 0.95f) : new Color(0.55f, 0.2f, 0.2f, 0.9f));
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
		return new Rect((UiKit.W - 320f * S) / 2f, 44f * S, 320f * S, 96f * S);
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

	private static string FormatLeft(int seconds)
	{
		int num = Mathf.Max(0, seconds);
		return num / 60 + ":" + (num % 60).ToString("D2");
	}
}
