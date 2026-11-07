using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeAdBoard
{
	private const string Root = "UI/ChatBalloon.img/adboard/0/";

	private const int MaxChars = 40;

	private const int MaxCols = 14;

	private const int Cell = 14;

	private const int RowH = 14;

	private const int LeftW = 18;

	private const int RightW = 18;

	private const int TopH = 19;

	private const int BottomM = 23;

	private const string Prompt = "请输入留言内容。";

	private const string BlockedText = "您无法在这里使用黑板。";

	private static string boardText;

	private static GUIStyle textStyle;

	private static Texture2D artNw;

	private static Texture2D artNe;

	private static Texture2D artSw;

	private static Texture2D artSe;

	private static Texture2D artN;

	private static Texture2D artS;

	private static Texture2D artW;

	private static Texture2D artE;

	private static Texture2D artC;

	private static Texture2D artArrow;

	private static bool artTried;

	private const float ArtBaseHeight = 768f;

	private static readonly Dictionary<int, Texture2D> bodies = new Dictionary<int, Texture2D>();

	public static string Text => boardText ?? "";

	public static void Clear()
	{
		boardText = null;
		if (GameSession.InGame)
		{
			NativeCommunityWindow.Request(CommunityOperation.CloseBoard);
		}
	}

	public static void Open(int itemId)
	{
		if (itemId / 10000 == 537)
		{
			int num = (((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.field != (Object)null && GameLoop.I.field.Map != null) ? GameLoop.I.field.Map.id : 0);
			if (num / 1000000 == 910 && num > 910000000)
			{
				BottomBar.Toast("您无法在这里使用黑板。");
			}
			else
			{
				NativeUtilPrompt.Text("请输入留言内容。", boardText ?? "", 0, 40, Submit);
			}
		}
	}

	private static void Submit(string value)
	{
		string text = (value ?? "").Trim();
		if (text.Length != 0)
		{
			NativeCommunityWindow.InputText = text;
			CommunityResponse communityResponse = NativeCommunityWindow.Request(CommunityOperation.SetBoard);
			if (communityResponse.ok)
			{
				boardText = communityResponse.board;
			}
		}
	}

	private static void EnsureArt()
	{
		if (!artTried)
		{
			artTried = true;
			artNw = Tex("UI/ChatBalloon.img/adboard/0/nw");
			artNe = Tex("UI/ChatBalloon.img/adboard/0/ne");
			artSw = Tex("UI/ChatBalloon.img/adboard/0/sw");
			artSe = Tex("UI/ChatBalloon.img/adboard/0/se");
			artN = Tex("UI/ChatBalloon.img/adboard/0/n");
			artS = Tex("UI/ChatBalloon.img/adboard/0/s");
			artW = Tex("UI/ChatBalloon.img/adboard/0/w");
			artE = Tex("UI/ChatBalloon.img/adboard/0/e");
			artC = Tex("UI/ChatBalloon.img/adboard/0/c");
			artArrow = Tex("UI/ChatBalloon.img/adboard/0/arrow");
		}
	}

	private static Texture2D Tex(string path)
	{
		try
		{
			ImgNode imgNode = WzAssets.Node(path);
			return (imgNode == null) ? null : WzAssets.Texture(imgNode);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static void Draw()
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected Obj, but got Unknown
		//IL_009e: Expected Obj, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected Obj, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		if (!GameSession.InGame || string.IsNullOrEmpty(boardText))
		{
			return;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		Camera main = Camera.main;
		if ((Object)(object)playerCtrl == (Object)null || (Object)(object)main == (Object)null)
		{
			return;
		}
		EnsureArt();
		if ((Object)(object)artNw == (Object)null || (Object)(object)artC == (Object)null)
		{
			return;
		}
		if (textStyle == null)
		{
			textStyle = new GUIStyle
			{
				font = UiKit.Font,
				fontSize = 12,
				wordWrap = false,
				padding = new RectOffset()
			};
		}
		List<string> list = Wrap(boardText, 196);
		int count = list.Count;
		float num = 0f;
		foreach (string item in list)
		{
			num = Mathf.Max(num, textStyle.CalcSize(new GUIContent(item)).x);
		}
		int num2 = ((count == 1) ? (Mathf.CeilToInt(num / 14f) | 1) : 14);
		int num3 = num2 * 14;
		int num4 = count * 14;
		int num5 = num3 + 18 + 18;
		int num6 = 19 + num4 + 23;
		Texture2D val = Body(num5, num6, num3, num4, num2);
		SpriteRenderer componentInChildren = ((Component)playerCtrl).GetComponentInChildren<SpriteRenderer>();
		float x = ((Component)playerCtrl).transform.position.x;
		float y = ((Component)playerCtrl).transform.position.y;
		float num7;
		if (!((Object)(object)componentInChildren != (Object)null) || !((Object)(object)componentInChildren.sprite != (Object)null))
		{
			num7 = 0.55f;
		}
		else
		{
			Bounds bounds = componentInChildren.sprite.bounds;
			num7 = bounds.max.y;
		}
		Vector3 val2 = main.WorldToScreenPoint(new Vector3(x, y + num7, 0f));
		if (val2.z < 0f)
		{
			return;
		}
		float num8 = Mathf.Max(0.35f, UiKit.H / 768f);
		float num9 = val2.x / UiKit.Scale / num8;
		float num10 = ((float)Screen.height - val2.y) / UiKit.Scale / num8;
		Rect val3 = new Rect(num9 + 3f - (float)num5 * 0.5f, num10 - (float)num6, (float)num5, (float)num6);
		float num11 = UiKit.W / num8;
		float num12 = UiKit.H / num8;
		if (!(val3.xMax < -40f) && !(val3.x > num11 + 40f) && !(val3.yMax < -40f) && !(val3.y > num12 + 40f))
		{
			UiKit.Begin();
			Matrix4x4 matrix = GUI.matrix;
			GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(num8, num8, 1f));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(val3, (Texture)(object)val);
			}
			textStyle.normal.textColor = Color.white;
			textStyle.alignment = (TextAnchor)0;
			for (int i = 0; i < count; i++)
			{
				GUI.Label(new Rect(val3.x + 18f, val3.y + 19f + (float)(i * 14), (float)num3, 14f), list[i], textStyle);
			}
			GUI.matrix = matrix;
		}
	}

	private static List<string> Wrap(string text, int maxPixels)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		string text2 = text ?? "";
		while (text2.Length > 0)
		{
			int i = 0;
			int num = 0;
			for (; i < text2.Length; i++)
			{
				int num2 = num + Mathf.CeilToInt(textStyle.CalcSize(new GUIContent(text2[i].ToString())).x);
				if (i > 0 && num2 > maxPixels)
				{
					break;
				}
				num = num2;
			}
			int num3 = i;
			if (i < text2.Length && i > 0)
			{
				int num4 = text2.LastIndexOfAny(new char[2] { ' ', '\n' }, i - 1, i);
				if (num4 >= 0)
				{
					num3 = num4;
				}
			}
			if (num3 <= 0)
			{
				num3 = Mathf.Max(1, i);
			}
			list.Add(text2.Substring(0, num3));
			text2 = ((num3 < text2.Length && text2[num3] == ' ') ? text2.Substring(num3 + 1) : text2.Substring(num3));
		}
		if (list.Count == 0)
		{
			list.Add("");
		}
		return list;
	}

	private static Texture2D Body(int w, int h, int innerW, int innerH, int cols)
	{
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Expected Obj, but got Unknown
		int key = (w << 16) | h;
		if (bodies.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		if (bodies.Count > 24)
		{
			foreach (Texture2D value2 in bodies.Values)
			{
				if ((Object)(object)value2 != (Object)null)
				{
					Object.Destroy((Object)(object)value2);
				}
			}
			bodies.Clear();
		}
		Color32[] array = new Color32[w * h];
		Blit(artNw, array, w, h, 0, 0);
		Blit(artNe, array, w, h, w - 18, 0);
		Blit(artSw, array, w, h, 0, 19 + innerH);
		Blit(artSe, array, w, h, w - 18, 19 + innerH);
		for (int i = 18; i < 18 + innerW; i += 14)
		{
			Blit(artN, array, w, h, i, 0);
			Blit(artS, array, w, h, i, 19 + innerH);
		}
		for (int j = 19; j < 19 + innerH; j += 14)
		{
			Blit(artW, array, w, h, 0, j);
			Blit(artE, array, w, h, w - 18, j);
			for (int k = 18; k < 18 + innerW; k += 14)
			{
				Blit(artC, array, w, h, k, j);
			}
		}
		Blit(artArrow, array, w, h, 18 + cols / 2 * 14, 19 + innerH);
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false)
		{
			name = "AdBoard_" + w + "x" + h,
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		val.SetPixels32(array);
		val.Apply();
		bodies[key] = val;
		return val;
	}

	private static void Blit(Texture2D piece, Color32[] dst, int dw, int dh, int x, int top)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)piece == (Object)null)
		{
			return;
		}
		Color32[] pixels = piece.GetPixels32();
		int width = ((Texture)piece).width;
		int height = ((Texture)piece).height;
		for (int i = 0; i < height; i++)
		{
			int num = top + i;
			if (num < 0 || num >= dh)
			{
				continue;
			}
			int num2 = (dh - 1 - num) * dw;
			int num3 = height - 1 - i;
			for (int j = 0; j < width; j++)
			{
				int num4 = x + j;
				if (num4 >= 0 && num4 < dw)
				{
					dst[num2 + num4] = pixels[num3 * width + j];
				}
			}
		}
	}
}
