using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeMonsterBook
{
	public static bool Visible;

	private const float WinW = 475f;

	private const float WinH = 349f;

	private const float TabOutL = 7f;

	private const float TabOutR = 31f;

	private const float GroupW = 513f;

	private const int Pages = 9;

	private const int Slots = 25;

	private const string Root = "UI/UIWindow.img/MonsterBook/";

	private const float LeftX = 41f;

	private const float TopY = 25f;

	private const float SlotX0 = 8f;

	private const float SlotY0 = 31f;

	private const float SlotW = 27f;

	private const float SlotH = 38f;

	private const float SlotDX = 33f;

	private const float SlotDY = 45f;

	private const float InfoX = 43f;

	private const float ArrowY = 285f;

	private const float SearchX = 49f;

	private const float SearchY = 30f;

	private const float SearchW = 120f;

	private const float SearchH = 15f;

	private const float RightX = 241f;

	private const float RightW = 208f;

	private const float RightH = 256f;

	private const float RewardX = 38f;

	private const float RewardY = 60f;

	private const float RewardStep = 36f;

	private const float RewardSize = 32f;

	private const float RightTabRight = 506f;

	private const float RightTabW = 52f;

	private const float RightTabSelW = 66f;

	private const float RightTabY = 25f;

	private const float RightTabDY = 49f;

	private static SocialResponse state;

	private static readonly Dictionary<int, ImgNode> cards = new Dictionary<int, ImgNode>();

	private static readonly List<int> ids = new List<int>();

	private static readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();

	private static int page = 9;

	private static int rightTab;

	private static int selected;

	private static string search = "";

	private static bool searching;

	private static bool focus;

	private static bool windowDrag;

	private static bool contentDrag;

	private static float dragGrab;

	private static float dragBase;

	private static float contentScroll;

	private static Vector2 position = new Vector2(-1f, 32f);

	private static List<int> collected;

	private static SocialResponse collectedOf;

	private static GUIStyle wrap;

	private static void Ensure()
	{
		if (cards.Count > 0)
		{
			return;
		}
		ImgNode imgNode = WzAssets.FileRoot("Item/Consume/0238.img");
		if (imgNode == null)
		{
			return;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (int.TryParse(child.Name, out var result) && child.Int("info/monsterBook") != 0)
			{
				cards[result] = child;
				ids.Add(result);
			}
		}
		ids.Sort();
	}

	public static void Toggle()
	{
		Visible = !Visible;
		if (!Visible)
		{
			return;
		}
		Ensure();
		state = NativeSocialWindow.Request(SocialOperation.Read);
		contentScroll = 0f;
		page = ((state != null && state.ok) ? 9 : 0);
		selected = ((state != null && state.bookCover != 0) ? state.bookCover : 0);
		if (selected == 0 && state != null && state.ok)
		{
			List<int> list = Collected();
			if (list.Count > 0)
			{
				selected = list[0];
			}
		}
	}

	public static bool Collect(int cardId, int quantity)
	{
		if (!GameSession.InGame || GameSession.Net == null)
		{
			return false;
		}
		Ensure();
		if (!cards.ContainsKey(cardId))
		{
			return false;
		}
		for (int i = 0; i < Mathf.Min(quantity, 5); i++)
		{
			state = GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId,
				operation = SocialOperation.AddMonsterCard,
				cardId = cardId
			});
			if (!state.ok)
			{
				return false;
			}
		}
		return true;
	}

	private static int MobOf(int card)
	{
		if (!cards.TryGetValue(card, out var value))
		{
			return 0;
		}
		return value.Int("info/mob");
	}

	private static string NameOf(int card)
	{
		return WzNames.Mob(MobOf(card));
	}

	private static int LevelOf(int card)
	{
		if (state == null || !state.cards.TryGetValue(card, out var value))
		{
			return 0;
		}
		return value;
	}

	private static int IdAt(int slot)
	{
		int num = page * 25 + slot;
		if (num < 0 || num >= ids.Count)
		{
			return 0;
		}
		return ids[num];
	}

	private static Rect IconRect(int slot)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(56f + (float)(slot % 5) * 33f, 56f + (float)(slot / 5) * 45f, 27f, 38f);
	}

	private static List<int> Collected()
	{
		if (collected != null && collectedOf == state)
		{
			return collected;
		}
		collected = new List<int>();
		foreach (int id in ids)
		{
			if (state != null && state.cards.ContainsKey(id))
			{
				collected.Add(id);
			}
		}
		collectedOf = state;
		return collected;
	}

	private static int SelectedAt()
	{
		List<int> list = Collected();
		int num = list.IndexOf(selected);
		if (num < 0)
		{
			if (list.Count <= 0)
			{
				return -1;
			}
			return 0;
		}
		return num;
	}

	private static void Pick(int delta)
	{
		List<int> list = Collected();
		if (list.Count != 0)
		{
			int index = Mathf.Clamp(((SelectedAt() >= 0) ? SelectedAt() : 0) + delta, 0, list.Count - 1);
			selected = list[index];
			int num = ids.IndexOf(selected);
			if (num >= 0)
			{
				page = num / 25;
			}
			contentScroll = 0f;
		}
	}

	public static void Draw()
	{
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Invalid comparison between Unknown and I4
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || state == null)
		{
			return;
		}
		Ensure();
		float s = BottomBar.S;
		if (position.x < 0f)
		{
			position.x = (UiKit.W - 513f) / 2f;
		}
		position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, UiKit.W - 513f));
		position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, UiKit.H - 349f));
		Vector2 val = Pointer.Pos / s;
		NativeUi.RegisterWindow("NativeMonsterBook", new Rect(position.x * s, position.y * s, 513f * s, 349f * s), () => Visible);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = matrix * Matrix4x4.Scale(new Vector3(s, s, 1f));
		try
		{
			Rect val2 = new Rect(position.x, position.y, 513f, 349f);
			if (GUI.enabled && (int)Event.current.type == 7)
			{
				if (Pointer.Pressed)
				{
					Rect val3 = new Rect(val2.x, val2.y, 513f, 18f);
					if (val3.Contains(val))
					{
						windowDrag = true;
						dragGrab = val.y - val2.y;
					}
				}
				if (windowDrag && Pointer.Down)
				{
					position.y = val.y - dragGrab;
				}
				if (!Pointer.Down)
				{
					windowDrag = false;
				}
			}
			position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, UiKit.H - 349f));
			val2.y = position.y;
			GUI.BeginGroup(val2);
			try
			{
				Body(val - new Vector2(val2.x, val2.y));
			}
			finally
			{
				GUI.EndGroup();
			}
		}
		finally
		{
			GUI.matrix = matrix;
		}
	}

	private static void Body(Vector2 p)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		Image("backgrnd", 7f, 0f);
		bool flag = page != 9;
		if (flag)
		{
			CardPage(p);
		}
		else
		{
			InfoPage();
		}
		RightPage(p);
		Tabs(p);
		if (Arrow("arrowLeft", new Rect(55f, 285f, 24f, 16f), page > 0, p))
		{
			Turn(-1);
		}
		if (Arrow("arrowRight", new Rect(192f, 285f, 23f, 17f), page < 8, p))
		{
			Turn(1);
		}
		int num = SelectedAt();
		if (Arrow("arrowLeft", new Rect(277f, 285f, 24f, 16f), num > 0, p))
		{
			Pick(-1);
		}
		if (Arrow("arrowRight", new Rect(414f, 285f, 23f, 17f), num >= 0 && num < Collected().Count - 1, p))
		{
			Pick(1);
		}
		if (NativeUi.Button(new Rect(436f, 8f, 18f, 16f), "UI/UIWindow.img/MonsterBook/BtClose", p))
		{
			Visible = false;
		}
		else if (flag)
		{
			SearchField(p);
		}
	}

	private static void Turn(int delta)
	{
		page = Mathf.Clamp(page + delta, 0, 8);
		contentScroll = 0f;
	}

	private static void CardPage(Vector2 p)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		Image("cardSlot", 48f, 25f);
		for (int i = 0; i < 25; i++)
		{
			int num = IdAt(i);
			if (num == 0 || !state.cards.ContainsKey(num))
			{
				continue;
			}
			Rect val = IconRect(i);
			ImgNode imgNode = cards[num].Get("info/icon");
			Texture2D val2 = ((imgNode != null) ? WzAssets.Texture(imgNode) : null);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(val.x, val.y, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
			}
			if (state.bookCover == num)
			{
				Image("cover", val.x - 2f, val.y - 2f);
			}
			if (num == selected)
			{
				Image("select", val.x - 2f, val.y - 2f);
			}
			int num2 = LevelOf(num);
			if (num2 >= 5)
			{
				Image("fullMark", val.x + 27f - 7f, val.y + 38f - 11f);
			}
			else if (num2 > 0)
			{
				Digit(num2, val.x + 27f - 8f, val.y + 38f - 11f);
			}
			Rect rect = new Rect(val.x - 2f, val.y - 2f, 31f, 42f);
			if (!rect.Contains(p))
			{
				continue;
			}
			bool doubleClicked = Pointer.DoubleClicked;
			if (Hit(rect, p))
			{
				selected = num;
				contentScroll = 0f;
				if (doubleClicked)
				{
					SetCover((state.bookCover != num) ? num : 0);
				}
			}
		}
	}

	private static void Digit(int value, float x, float y)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		float num = x;
		string text = value.ToString();
		for (int i = 0; i < text.Length; i++)
		{
			Texture2D val = WzAssets.Texture("wz://UI/Basic.img/ItemNo/" + text[i]);
			if (!((Object)(object)val == (Object)null))
			{
				GUI.DrawTexture(new Rect(num, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
				num += (float)((Texture)val).width;
			}
		}
	}

	private static void InfoPage()
	{
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		Image("infoPage", 50f, 25f);
		int num = 0;
		int num2 = 0;
		HashSet<int> hashSet = new HashSet<int>();
		foreach (KeyValuePair<int, int> card in state.cards)
		{
			if (!cards.ContainsKey(card.Key))
			{
				continue;
			}
			int num3 = ids.IndexOf(card.Key);
			if (num3 >= 0)
			{
				hashSet.Add(num3 / 25);
			}
			try
			{
				if (WzAssets.MobArtRoot(MobOf(card.Key)).Int("info/boss") != 0)
				{
					num++;
				}
			}
			catch (Exception)
			{
			}
		}
		num2 = hashSet.Count;
		Value(108f, 77f, state.bookLevel.ToString());
		Value(103f, 130f, state.cards.Count.ToString());
		Value(90f, 161f, num.ToString());
		Value(90f, 182f, (state.cards.Count - num).ToString());
		Value(64f, 218f, num2.ToString());
		if (state.bookCover != 0 && cards.ContainsKey(state.bookCover))
		{
			ImgNode imgNode = cards[state.bookCover].Get("info/icon");
			Texture2D val = ((imgNode != null) ? WzAssets.Texture(imgNode) : null);
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(123f, 31f, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
			}
		}
	}

	private static void Value(float x, float y, string value)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(new Rect(50f + x, 25f + y, 70f, 14f), value, Style(11, (TextAnchor)3));
	}

	private static void Tabs(Vector2 p)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i <= 9; i++)
		{
			bool flag = page == i;
			float num = 25 + 28 * i;
			if (i == 9)
			{
				Rect rect = new Rect(0f, num, (float)(flag ? 39 : 27), (float)(flag ? 33 : 35));
				Paint("UI/UIWindow.img/MonsterBook/LeftTabInfo/0", rect, flag, p);
				if (!flag && Hit(rect, p))
				{
					page = 9;
					contentScroll = 0f;
				}
				continue;
			}
			Rect rect2 = new Rect(0f, num, (float)(flag ? 42 : 20), 20f);
			Paint("UI/UIWindow.img/MonsterBook/LeftTab/" + i, rect2, flag, p);
			GUI.Label(new Rect(rect2.x + 1f, num + 1f, (float)(flag ? 26 : 18), 13f), (i + 1).ToString(), Style(10, (TextAnchor)4, i == 7));
			if (!flag && Hit(rect2, p))
			{
				page = i;
				contentScroll = 0f;
			}
		}
		for (int j = 0; j < 4; j++)
		{
			bool flag2 = rightTab == j;
			float num2 = (flag2 ? 66f : 52f);
			Rect rect3 = new Rect(513f - num2, 25f + 49f * (float)j, num2, (float)(flag2 ? 39 : 38));
			Paint("UI/UIWindow.img/MonsterBook/RightTab/" + j, rect3, flag2, p);
			if (!flag2 && Hit(rect3, p))
			{
				rightTab = j;
				contentScroll = 0f;
			}
		}
	}

	private static void Paint(string path, Rect rect, bool selected, Vector2 p)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		string text;
		if (selected)
		{
			text = "selected/0";
		}
		else
		{
			text = ((rect.Contains(p) && Pointer.Down) ? "mouseOver/0" : "normal/0");
		}
		Texture2D val = Art.Tex("wz://" + path + "/" + text);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.x, rect.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Arrow(string node, Rect rect, bool enabled, Vector2 p)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		string text;
		if (enabled)
		{
			text = ((rect.Contains(p) && Pointer.Down) ? "pressed/0" : "normal/0");
		}
		else
		{
			text = "disabled/0";
		}
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/MonsterBook/" + node + "/" + text);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(rect.x, rect.y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
		if (enabled)
		{
			return Hit(rect, p);
		}
		return false;
	}

	private static void RightPage(Vector2 p)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		int num = RightCard();
		if (num == 0)
		{
			return;
		}
		Rect val = new Rect(248f, 25f, 208f, 256f);
		float maximum = Mathf.Max(0f, ContentHeight(num) - 256f);
		GUI.BeginGroup(val);
		try
		{
			Vector2 p2 = p - new Vector2(val.x, val.y);
			contentScroll = Drag(new Rect(0f, 0f, 208f, 256f), p2, contentScroll, maximum);
			switch (rightTab)
			{
			case 1:
				Episode(num, contentScroll);
				break;
			case 2:
				Rewards(num, contentScroll);
				break;
			case 3:
				Areas(num, contentScroll);
				break;
			default:
				BasicInfo(num);
				break;
			}
		}
		finally
		{
			GUI.EndGroup();
		}
	}

	private static int RightCard()
	{
		if (selected != 0 && state.cards.ContainsKey(selected))
		{
			return selected;
		}
		List<int> list = Collected();
		if (list.Count <= 0)
		{
			return 0;
		}
		return list[0];
	}

	private static void BasicInfo(int card)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		int id = MobOf(card);
		GUI.Label(new Rect(0f, 4f, 208f, 16f), WzNames.Mob(id), Style(12, (TextAnchor)4));
		try
		{
			ImgNode imgNode = WzAssets.MobArtRoot(id);
			List<ImgNode> list = WzAssets.Frames(imgNode.Get("stand"));
			if (list.Count > 0)
			{
				Texture2D val = WzAssets.Texture(list[0]);
				if ((Object)(object)val != (Object)null)
				{
					float num = Mathf.Min(new float[3]
					{
						1f,
						160f / (float)((Texture)val).width,
						100f / (float)((Texture)val).height
					});
					float num2 = (float)((Texture)val).width * num;
					float num3 = (float)((Texture)val).height * num;
					GUI.DrawTexture(new Rect((208f - num2) / 2f, 24f + (100f - num3) / 2f, num2, num3), (Texture)(object)val);
				}
			}
			float num4 = 132f;
			Line(38f, num4, "Level : " + imgNode.Int("info/level"));
			Line(38f, num4 + 18f, "HP : " + imgNode.Int("info/maxHP"));
			Line(38f, num4 + 36f, "MP : " + imgNode.Int("info/maxMP"));
			Line(38f, num4 + 54f, "EXP : " + imgNode.Int("info/exp"));
		}
		catch (Exception)
		{
		}
	}

	private static void Line(float x, float y, string text)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(new Rect(x, y, 160f, 16f), text, Style(11, (TextAnchor)3));
	}

	private static void Episode(int card, float scroll)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		GUI.Label(new Rect(38f, 6f - scroll, 142f, ContentHeight(card)), EpisodeText(card), Wrap());
	}

	private static void Rewards(int card, float scroll)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		List<int> list = Children(card, "reward");
		if (list.Count == 0)
		{
			GUI.Label(new Rect(38f, 90f, 142f, 40f), "没有登记战利品", Wrap());
			return;
		}
		Texture2D val = WzAssets.Texture("wz://UI/UIWindow.img/IconBase/0");
		for (int i = 0; i < list.Count && i < 20; i++)
		{
			float num = 38f + (float)(i % 4) * 36f;
			float num2 = 60f + (float)(i / 4) * 36f - scroll;
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(new Rect(num, num2, 32f, 32f), (Texture)(object)val);
			}
			ImgNode imgNode = NativeItemSource.IconNode(list[i]);
			Texture2D val2 = ((imgNode != null) ? WzAssets.Texture(imgNode) : null);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(new Rect(num + (32f - (float)((Texture)val2).width) / 2f, num2 + (32f - (float)((Texture)val2).height) / 2f, (float)((Texture)val2).width, (float)((Texture)val2).height), (Texture)(object)val2);
			}
		}
	}

	private static void Areas(int card, float scroll)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		List<int> list = Children(card, "map");
		if (list.Count == 0)
		{
			GUI.Label(new Rect(38f, 90f, 142f, 40f), "没有登记出现地图", Wrap());
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			float num = (float)(8 + i * 18) - scroll;
			if (!(num > 256f) && !(num + 18f < 0f))
			{
				GUI.Label(new Rect(38f, num, 142f, 16f), WzNames.Map(list[i]), Style(11, (TextAnchor)3));
			}
		}
	}

	private static ImgNode Info(int card)
	{
		return WzAssets.FileRoot("String/MonsterBook.img")?.Get(MobOf(card).ToString());
	}

	private static string EpisodeText(int card)
	{
		object obj = (Info(card)?.Get("episode"))?.Value;
		if (obj != null)
		{
			return Convert.ToString(obj).Replace("\\n", "\n");
		}
		return "";
	}

	private static List<int> Children(int card, string name)
	{
		List<int> list = new List<int>();
		ImgNode imgNode = Info(card)?.Get(name);
		if (imgNode == null)
		{
			return list;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Value != null)
			{
				try
				{
					list.Add(Convert.ToInt32(child.Value));
				}
				catch (Exception)
				{
				}
			}
		}
		return list;
	}

	private static float ContentHeight(int card)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected Obj, but got Unknown
		if (rightTab == 1)
		{
			string text = EpisodeText(card);
			if (string.IsNullOrEmpty(text))
			{
				return 256f;
			}
			return Mathf.Max(256f, Wrap().CalcHeight(new GUIContent(text), 192f) + 12f);
		}
		if (rightTab == 2)
		{
			return 256f;
		}
		if (rightTab == 3)
		{
			return Mathf.Max(256f, (float)(12 + Children(card, "map").Count * 18));
		}
		return 256f;
	}

	private static float Drag(Rect view, Vector2 p, float value, float maximum)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (maximum <= 0f)
		{
			contentDrag = false;
			return 0f;
		}
		if ((int)Event.current.type != 7)
		{
			return value;
		}
		if (Pointer.Pressed && view.Contains(p))
		{
			contentDrag = true;
			dragGrab = p.y;
			dragBase = value;
		}
		if (contentDrag)
		{
			if (Pointer.Down)
			{
				value = Mathf.Clamp(dragBase - (p.y - dragGrab), 0f, maximum);
			}
			else
			{
				contentDrag = false;
			}
		}
		return value;
	}

	private static void SearchField(Vector2 p)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Invalid comparison between Unknown and I4
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Invalid comparison between Unknown and I4
		Rect val = new Rect(56f, 30f, 120f, 15f);
		if (GUI.enabled && (int)Event.current.type == 7 && Pointer.Pressed)
		{
			if (val.Contains(p))
			{
				searching = true;
				focus = true;
			}
			else if (searching)
			{
				searching = false;
				Apply();
			}
		}
		if (focus && (int)Event.current.type == 7)
		{
			GUI.FocusControl("MonsterBookSearch");
			focus = false;
		}
		if (searching)
		{
			GUI.SetNextControlName("MonsterBookSearch");
			string text = GUI.TextField(new Rect(val.x + 2f, val.y + 1f, val.width - 4f, val.height - 2f), search, 20, Style(11, (TextAnchor)3));
			if (text != search)
			{
				search = text;
			}
			if ((int)Event.current.type == 4 && ((int)Event.current.keyCode == 13 || (int)Event.current.keyCode == 271))
			{
				searching = false;
				Apply();
			}
		}
		else if (search.Length > 0)
		{
			GUI.Label(new Rect(val.x + 2f, val.y + 1f, val.width - 4f, val.height - 2f), search, Style(11, (TextAnchor)3));
		}
		if (NativeUi.Button(new Rect(182f, 29f, 34f, 17f), "UI/UIWindow.img/MonsterBook/BtSearch", p))
		{
			if (search.Length == 0)
			{
				searching = true;
				focus = true;
			}
			else
			{
				searching = false;
				Apply();
			}
		}
	}

	private static void Apply()
	{
		if (string.IsNullOrEmpty(search))
		{
			return;
		}
		for (int i = 0; i < ids.Count; i++)
		{
			string text = NameOf(ids[i]);
			if (text != null && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				page = i / 25;
				selected = ids[i];
				contentScroll = 0f;
				return;
			}
		}
		BottomBar.Toast("找不到名字含有「" + search + "」的怪物卡片");
	}

	private static void SetCover(int card)
	{
		if (GameSession.InGame && GameSession.Net != null)
		{
			state = GameSession.Net.Social(new SocialRequest
			{
				characterId = GameSession.CharacterId,
				operation = SocialOperation.SetBookCover,
				cardId = card
			});
			if (!state.ok)
			{
				BottomBar.Toast(state.error);
			}
			else
			{
				GameSession.Character.record.monsterbookcover = state.bookCover;
			}
		}
	}

	private static void Image(string node, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/MonsterBook/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(new Rect(x, y, (float)((Texture)val).width, (float)((Texture)val).height), (Texture)(object)val);
		}
	}

	private static bool Hit(Rect rect, Vector2 p)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!GUI.enabled || (int)Event.current.type != 7 || !Pointer.Released || !rect.Contains(p))
		{
			return false;
		}
		Pointer.ConsumeClick();
		return true;
	}

	private static GUIStyle Style(int size, TextAnchor anchor, bool light = false)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected I4, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected Obj, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected Obj, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		int key = size * 16 + anchor + (light ? 1000 : 0);
		if (styles.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = size,
			padding = new RectOffset(),
			alignment = anchor,
			clipping = (TextClipping)1,
			richText = false
		};
		value.normal.textColor = (light ? new Color(0.97f, 0.95f, 0.88f) : new Color(0.22f, 0.15f, 0.1f));
		styles.Add(key, value);
		return value;
	}

	private static GUIStyle Wrap()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected Obj, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (wrap != null)
		{
			return wrap;
		}
		wrap = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = 11,
			wordWrap = true,
			padding = new RectOffset(),
			clipping = (TextClipping)1,
			richText = false
		};
		wrap.normal.textColor = new Color(0.22f, 0.15f, 0.1f);
		return wrap;
	}

	static NativeMonsterBook()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
	}
}
