using System;
using System.Collections.Generic;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class NativeMapWindows
{
	public static bool WorldVisible;

	private static int miniMode;

	private static string worldFile;

	private static readonly List<string> worldFiles = new List<string>();

	public static void ToggleMini()
	{
		miniMode = (miniMode + 2) % 3;
	}

	public static void CloseWorld()
	{
		WorldVisible = false;
	}

	public static void ToggleWorld()
	{
		WorldVisible = !WorldVisible;
		if (!WorldVisible)
		{
			return;
		}
		if (worldFiles.Count == 0)
		{
			foreach (Dictionary<string, object> item in (List<object>)WzAssets.Index["files"])
			{
				string text = (string)item["path"];
				if (text.StartsWith("Map/WorldMap/WorldMap", StringComparison.Ordinal) && text.EndsWith(".img", StringComparison.Ordinal))
				{
					worldFiles.Add(text);
				}
			}
		}
		worldFiles.Sort(StringComparer.Ordinal);
		worldFile = "Map/WorldMap/WorldMap.img";
		int num = int.MaxValue;
		foreach (string worldFile in worldFiles)
		{
			ImgNode imgNode = WzAssets.FileRoot(worldFile).Get("MapList");
			if (imgNode == null)
			{
				continue;
			}
			int num2 = 0;
			bool flag = false;
			foreach (ImgNode child in imgNode.Children)
			{
				foreach (ImgNode item2 in child.Get("mapNo")?.Children ?? new List<ImgNode>())
				{
					num2++;
					if (Convert.ToInt32(item2.Value) == Profile.LastMapId)
					{
						flag = true;
					}
				}
			}
			if (flag && num2 < num)
			{
				num = num2;
				NativeMapWindows.worldFile = worldFile;
			}
		}
	}

	private static GUIStyle Label(int size = 12)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected Obj, but got Unknown
		GUIStyle val = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = size,
			wordWrap = true
		};
		val.normal.textColor = Color.white;
		return val;
	}

	private static void Marker(ImgNode icon, Vector2 position, float scale)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (icon != null)
		{
			Texture2D val = WzAssets.Texture(icon);
			Vector2 val2 = WzAssets.Origin(icon);
			GUI.DrawTexture(new Rect(position.x - val2.x * scale, position.y - val2.y * scale, (float)((Texture)val).width * scale, (float)((Texture)val).height * scale), (Texture)(object)val);
		}
	}

	private static void MiniMarker(ImgNode icon, Vector2 position, float scale)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (icon != null)
		{
			Texture2D val = WzAssets.Texture(icon);
			GUI.DrawTexture(new Rect(position.x - (float)((Texture)val).width * 0.5f * scale, position.y - (float)((Texture)val).height * 0.5f * scale, (float)((Texture)val).width * scale, (float)((Texture)val).height * scale), (Texture)(object)val);
		}
	}

	public static void DrawMini()
	{
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected Obj, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected Obj, but got Unknown
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_0597: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0806: Unknown result type (might be due to invalid IL or missing references)
		//IL_0813: Unknown result type (might be due to invalid IL or missing references)
		//IL_0818: Unknown result type (might be due to invalid IL or missing references)
		//IL_083a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0888: Unknown result type (might be due to invalid IL or missing references)
		//IL_0900: Unknown result type (might be due to invalid IL or missing references)
		//IL_0966: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4d: Unknown result type (might be due to invalid IL or missing references)
		FieldMap map = FieldCatalog.GetMap(Profile.LastMapId);
		if (map == null)
		{
			return;
		}
		ImgNode imgNode = WzAssets.FileRoot(WzAssets.MapPath(map.id));
		ImgNode mini = imgNode.Get("miniMap");
		ImgNode imgNode2 = mini?.Get("canvas");
		float s = BottomBar.S;
		Texture2D val = ((imgNode2 != null) ? WzAssets.Texture(imgNode2) : null);
		float num = (((Object)(object)val != (Object)null) ? ((((Texture)val).width >= 280) ? 210 : ((Texture)val).width) : 0);
		float num2 = (((Object)(object)val != (Object)null) ? ((((Texture)val).height >= 150) ? 112 : ((Texture)val).height) : 0);
		string text = imgNode.Get("info/mapMark")?.Value as string;
		Texture2D val2 = (string.IsNullOrEmpty(text) ? null : Art.Tex("wz://Map/MapHelper.img/mark/" + text));
		GUIStyle val3 = Label(Mathf.RoundToInt(12f * s));
		val3.wordWrap = false;
		float num3 = ((miniMode == 0 && (Object)(object)val2 != (Object)null) ? 40 : 0);
		float num4 = val3.CalcSize(new GUIContent(map.street)).x / s + num3 + 5f;
		float num5 = val3.CalcSize(new GUIContent(map.name)).x / s + num3 + 5f;
		float num6 = ((miniMode == 0) ? Mathf.Max(num, Mathf.Max(num4, num5)) : num);
		float num7 = ((miniMode == 2) ? (num4 + num5 + 80f) : Mathf.Max(120f, num6 + 12f));
		float num8 = ((miniMode == 0) ? 72 : 29);
		float num9 = ((miniMode == 2) ? 20f : (num2 + (float)((miniMode == 0) ? 87 : 43)));
		Rect r = new Rect(8f, 8f, num7 * s, num9 * s);
		if (miniMode == 2)
		{
			DrawPart("UI/UIWindow.img/MiniMap/Min/w", r.x, r.y, 8f * s, r.height);
			DrawPart("UI/UIWindow.img/MiniMap/Min/c", r.x + 8f * s, r.y, r.width - 12f * s, r.height);
			DrawPart("UI/UIWindow.img/MiniMap/Min/e", r.xMax - 4f * s, r.y, 4f * s, r.height);
		}
		else
		{
			DrawMiniFrame(r, "UI/UIWindow.img/MiniMap/" + ((miniMode == 0) ? "MaxMap/" : "MinMap/"), num8 * s, (float)((miniMode == 0) ? 15 : 14) * s, s, num * s);
		}
		Texture2D val4 = Art.Tex("wz://UI/UIWindow.img/MiniMap/title");
		if ((Object)(object)val4 != (Object)null)
		{
			GUI.DrawTexture(new Rect(r.x + 8f * s, r.y + 10f * s, (float)((Texture)val4).width * s, (float)((Texture)val4).height * s), (Texture)(object)val4);
		}
		float num10 = r.y + (float)((miniMode == 2) ? 4 : 6) * s;
		float num11 = r.xMax - (float)((miniMode == 2) ? 44 : 42) * s;
		if (NativeUi.Button(new Rect(num11, num10, 36f * s, 12f * s), "UI/UIWindow.img/MiniMap/BtMap"))
		{
			ToggleWorld();
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && miniMode != 0 && (Object)(object)val != (Object)null;
		bool flag = NativeUi.Button(new Rect(num11 - 14f * s, num10, 12f * s, 12f * s), "UI/Basic.img/BtMax");
		GUI.enabled = enabled && miniMode != 2 && (Object)(object)val != (Object)null;
		bool flag2 = NativeUi.Button(new Rect(num11 - 27f * s, num10, 12f * s, 12f * s), "UI/Basic.img/BtMin");
		GUI.enabled = enabled;
		if (flag | flag2)
		{
			miniMode += ((!flag) ? 1 : (-1));
		}
		else
		{
			if (miniMode == 2)
			{
				return;
			}
			if (miniMode == 0)
			{
				float num12 = r.x + (float)(((Object)(object)val2 != (Object)null) ? 48 : 8) * s;
				if ((Object)(object)val2 != (Object)null)
				{
					GUI.DrawTexture(new Rect(r.x + 6f * s, r.y + 23f * s, (float)((Texture)val2).width * s, (float)((Texture)val2).height * s), (Texture)(object)val2);
				}
				DrawMiniLabel(new Rect(num12 + s, r.y + 28f * s, r.xMax - num12 - 6f * s, 18f * s), map.street, val3, s);
				DrawMiniLabel(new Rect(num12 + s, r.y + 44f * s, r.xMax - num12 - 6f * s, 18f * s), map.name, val3, s);
			}
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			Rect val5 = new Rect(r.x + (num7 - num) * 0.5f * s, r.y + num8 * s, num * s, num2 * s);
			float divisor = Mathf.Pow(2f, (float)mini.Int("mag", 4));
			PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
			float num13 = (((Object)(object)playerCtrl != (Object)null) ? (playerCtrl.FeetPos.x * 40f) : 0f);
			float num14 = (((Object)(object)playerCtrl != (Object)null) ? ((0f - playerCtrl.FeetPos.y) * 40f) : 0f);
			float scrollX = Mathf.Floor((Mathf.Min(Mathf.Max(num13 - num * divisor / 2f, (float)(-mini.Int("centerX"))), (float)(mini.Int("width", ((Texture)val).width * (int)divisor) - mini.Int("centerX")) - num * divisor) + (float)mini.Int("centerX")) / divisor);
			float scrollY = Mathf.Floor((Mathf.Min(Mathf.Max(num14 - num2 * divisor / 2f, (float)(-mini.Int("centerY"))), (float)(mini.Int("height", ((Texture)val).height * (int)divisor) - mini.Int("centerY")) - num2 * divisor) + (float)mini.Int("centerY")) / divisor);
			GUI.BeginGroup(val5);
			Color color = GUI.color;
			GUI.color = Color32.op_Implicit(new Color32((byte)0, (byte)0, (byte)0, (byte)160));
			GUI.DrawTexture(new Rect(0f, 0f, val5.width, val5.height), (Texture)(object)Texture2D.whiteTexture);
			GUI.color = color;
			GUI.DrawTexture(new Rect((0f - scrollX) * s, (0f - scrollY) * s, (float)((Texture)val).width * s, (float)((Texture)val).height * s), (Texture)(object)val);
			Func<float, float, Vector2> func = (float x, float y) =>
			{
				//IL_004e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0059: Unknown result type (might be due to invalid IL or missing references)
				return new Vector2(Mathf.Floor((x + (float)mini.Int("centerX")) / divisor) - scrollX, Mathf.Floor((y + (float)mini.Int("centerY")) / divisor) - scrollY) * s;
			};
			ImgNode imgNode3 = WzAssets.FileRoot("Map/MapHelper.img").Get("minimap");
			foreach (FieldPortal portal in map.portals)
			{
				if (portal.type == 2 || portal.type == 7)
				{
					MiniMarker(imgNode3.Get("portal"), func(portal.x, portal.y), s);
				}
			}
			foreach (FieldNpc npc in map.npcs)
			{
				if (npc.hide == 0)
				{
					MiniMarker(imgNode3.Get("npc"), func(npc.x, npc.y), s);
				}
			}
			ImgNode imgNode4 = imgNode3.Get("another");
			ImgNode imgNode5 = imgNode3.Get("party");
			ImgNode imgNode6 = imgNode3.Get("partymaster");
			BotPartyService parties = BotTypeManager.Parties;
			foreach (BotCharacter item in BotRegistry.OnMap(map.id))
			{
				if (item != null && item.online && item.mapId == map.id)
				{
					BotPartyService.Party party = parties?.For(item.id);
					ImgNode imgNode7;
					if (party == null)
					{
						imgNode7 = imgNode4;
					}
					else
					{
						imgNode7 = ((party.leader == item.id) ? imgNode6 : imgNode5);
					}
					MiniMarker((imgNode7 != null) ? imgNode7 : imgNode4, func(item.x, item.y), s);
				}
			}
			if ((Object)(object)playerCtrl != (Object)null)
			{
				MiniMarker(imgNode3.Get("user"), func(num13, num14), s);
			}
			GUI.EndGroup();
		}
	}

	private static void DrawMiniLabel(Rect r, string text, GUIStyle style, float scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle(style);
		val.normal.textColor = Color32.op_Implicit(new Color32((byte)82, (byte)117, (byte)156, byte.MaxValue));
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i != 0 || j != 0)
				{
					GUI.Label(new Rect(r.x + (float)i * scale, r.y + (float)j * scale, r.width, r.height), text, val);
				}
			}
		}
		GUI.Label(r, text, style);
	}

	private static void DrawPart(string path, float x, float y, float width, float height)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (!(width <= 0f) && !(height <= 0f))
		{
			GUI.DrawTexture(new Rect(x, y, width, height), (Texture)(object)Art.Tex("wz://" + path));
		}
	}

	private static void DrawMiniFrame(Rect r, string root, float top, float bottom, float s, float viewportWidth)
	{
		float num = (r.width - viewportWidth) * 0.5f - 6f * s;
		DrawPart(root + "c", r.x + 6f * s, r.y + top, num, r.height - top - bottom);
		DrawPart(root + "c", r.xMax - 6f * s - num, r.y + top, num, r.height - top - bottom);
		DrawPart(root + "nw", r.x, r.y, 60f * s, top);
		DrawPart(root + "n", r.x + 60f * s, r.y, r.width - 66f * s, top);
		DrawPart(root + "ne", r.xMax - 6f * s, r.y, 6f * s, top);
		DrawPart(root + "w", r.x, r.y + top, 6f * s, r.height - top - bottom);
		DrawPart(root + "e", r.xMax - 6f * s, r.y + top, 6f * s, r.height - top - bottom);
		DrawPart(root + "sw", r.x, r.yMax - bottom, 6f * s, bottom);
		DrawPart(root + "s", r.x + 6f * s, r.yMax - bottom, r.width - 12f * s, bottom);
		DrawPart(root + "se", r.xMax - 6f * s, r.yMax - bottom, 6f * s, bottom);
	}

	public static void DrawWorld()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0431: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Invalid comparison between Unknown and I4
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		if (!WorldVisible)
		{
			return;
		}
		ImgNode imgNode = WzAssets.FileRoot(worldFile);
		ImgNode imgNode2 = imgNode.Get("BaseImg");
		if (imgNode2 == null)
		{
			return;
		}
		float num = Mathf.Min(new float[3]
		{
			BottomBar.S,
			(UiKit.W - 32f) / 666f,
			(UiKit.H - 64f) / 524f
		});
		Rect val = new Rect((UiKit.W - 666f * num) / 2f, 20f, 666f * num, 524f * num);
		NativeUi.RegisterWindow("NativeMapWindows", val, () => WorldVisible);
		DrawWorldFrame(val, num);
		ImgNode imgNode3 = imgNode2.Get("0");
		Vector2 val2 = ((imgNode3 != null) ? WzAssets.Origin(imgNode3) : Vector2.zero);
		Vector2 val3 = val.position + (new Vector2(13f, 35f) + val2) * num;
		foreach (ImgNode child in imgNode2.Children)
		{
			Marker(child, val3, num);
		}
		ImgNode imgNode4 = imgNode.Get("MapList");
		string text = "";
		if (imgNode4 != null)
		{
			foreach (ImgNode child2 in imgNode4.Children)
			{
				ImgNode imgNode5 = child2.Get("spot");
				if (imgNode5 == null)
				{
					continue;
				}
				Vector2 val4 = val3 + new Vector2((float)imgNode5.X, (float)imgNode5.Y) * num;
				List<string> list = new List<string>();
				bool flag = false;
				foreach (ImgNode item in child2.Get("mapNo")?.Children ?? new List<ImgNode>())
				{
					int num2 = Convert.ToInt32(item.Value);
					if (num2 == Profile.LastMapId)
					{
						flag = true;
					}
					if (FieldCatalog.Maps.TryGetValue(num2, out var value))
					{
						list.Add(value.name);
					}
				}
				Marker(WzAssets.Node("Map/MapHelper.img/worldMap/mapImage/" + child2.Int("type")), val4, num);
				if (flag)
				{
					Marker(WzAssets.Node("Map/MapHelper.img/worldMap/curPos/0"), val4, num);
				}
				Rect val5 = new Rect(val4.x - 12f * num, val4.y - 12f * num, 24f * num, 24f * num);
				if (val5.Contains(Pointer.Pos))
				{
					text = string.Join(" / ", list);
				}
			}
		}
		ImgNode imgNode6 = imgNode.Get("MapLink");
		if (imgNode6 != null)
		{
			foreach (ImgNode child3 in imgNode6.Children)
			{
				ImgNode imgNode7 = child3.Get("link/linkImg");
				if (imgNode7 == null)
				{
					continue;
				}
				Vector2 val6 = WzAssets.Origin(imgNode7);
				Texture2D val7 = WzAssets.Texture(imgNode7);
				Rect val8 = new Rect(val3.x - val6.x * num, val3.y - val6.y * num, (float)((Texture)val7).width * num, (float)((Texture)val7).height * num);
				if (val8.Contains(Pointer.Pos))
				{
					Marker(imgNode7, val3, num);
					text = child3.Text("toolTip");
					if ((int)Event.current.type == 7 && Pointer.Clicked)
					{
						Pointer.ConsumeClick();
						worldFile = "Map/WorldMap/" + child3.Text("link/linkMap") + ".img";
						return;
					}
				}
			}
		}
		if (NativeUi.Button(new Rect(val.xMax - 20f * num, val.y + 6f * num, 12f * num, 12f * num), "UI/Basic.img/BtClose"))
		{
			WorldVisible = false;
		}
		Texture2D val9 = Art.Tex("wz://UI/UIWindow.img/WorldMap/title");
		if ((Object)(object)val9 != (Object)null)
		{
			GUI.DrawTexture(new Rect(val.x + 8f * num, val.y + 10f * num, (float)((Texture)val9).width * num, (float)((Texture)val9).height * num), (Texture)(object)val9);
		}
		if (text.Length > 0)
		{
			GUI.Label(new Rect(val.x + 13f * num, val.yMax - 17f * num, val.width - 26f * num, 17f * num), text, Label(Mathf.RoundToInt(12f * num)));
		}
	}

	private static void DrawWorldFrame(Rect r, float s)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(r.x + 2f * s, r.y + 2f * s, r.width - 4f * s, r.height - 4f * s), (Texture)(object)Texture2D.whiteTexture);
		GUI.color = color;
		DrawPart("UI/UIWindow.img/WorldMap/Border/1", r.x + 7f * s, r.y, r.width - 14f * s, 33f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/6", r.x + 7f * s, r.yMax - 18f * s, r.width - 14f * s, 18f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/3", r.x, r.y + 32f * s, 7f * s, r.height - 50f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/4", r.xMax - 7f * s, r.y + 32f * s, 7f * s, r.height - 50f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/0", r.x, r.y, 60f * s, 33f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/2", r.xMax - 7f * s, r.y, 7f * s, 32f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/5", r.x, r.yMax - 18f * s, 7f * s, 18f * s);
		DrawPart("UI/UIWindow.img/WorldMap/Border/7", r.xMax - 7f * s, r.yMax - 18f * s, 7f * s, 18f * s);
	}
}
