using UnityEngine;

public static class NativeTeleportWindow
{
	private const string Root = "UI/UIWindow.img/Teleport/";

	private const int Empty = 999999999;

	private const int VisibleRows = 5;

	private const int RegularSlots = 5;

	private const int VipSlots = 10;

	private static int itemId;

	private static int itemPosition;

	private static int selected;

	private static int scrollTop;

	private static bool vip;

	private static bool opened;

	private static readonly int[] slots = new int[10];

	private static string nameInput = "";

	private static Vector2 pos;

	private static Vector2 grab;

	private static bool dragging;

	private static bool centered;

	public static bool Visible => opened;

	private static float S => BottomBar.S;

	private static int Capacity
	{
		get
		{
			if (!vip)
			{
				return 5;
			}
			return 10;
		}
	}

	private static int ValidCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < Capacity; i++)
			{
				if (slots[i] != 999999999)
				{
					num++;
				}
			}
			return num;
		}
	}

	private static Vector2 Size
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(175f, 309f);
		}
	}

	private static Rect Window
	{
		get
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			return new Rect(pos.x * S, pos.y * S, Size.x * S, Size.y * S);
		}
	}

	private static int CurrentMapId
	{
		get
		{
			if (!((Object)(object)GameLoop.I != (Object)null) || !((Object)(object)GameLoop.I.field != (Object)null) || GameLoop.I.field.Map == null)
			{
				return Profile.LastMapId;
			}
			return GameLoop.I.field.Map.id;
		}
	}

	private static int FieldLimit
	{
		get
		{
			if (!((Object)(object)GameLoop.I != (Object)null) || !((Object)(object)GameLoop.I.field != (Object)null) || GameLoop.I.field.Map == null)
			{
				return 0;
			}
			return GameLoop.I.field.Map.fieldLimit;
		}
	}

	private static string Key
	{
		get
		{
			if (!vip)
			{
				return "trock";
			}
			return "trockVip";
		}
	}

	private static bool Registrable(int mapId)
	{
		if (mapId / 100000000 != 0)
		{
			return mapId / 1000000 % 100 != 9;
		}
		return false;
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static void Image(Rect r, string node, float x, float y)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/UIWindow.img/Teleport/" + node);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(At(r, x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void LoadSlots()
	{
		for (int i = 0; i < 10; i++)
		{
			slots[i] = 999999999;
		}
		string[] array = Profile.QuestProgress(0, Key, "").Split(',');
		for (int j = 0; j < array.Length && j < Capacity; j++)
		{
			if (int.TryParse(array[j].Trim(), out var result) && result > 0)
			{
				slots[j] = result;
			}
		}
	}

	private static void SaveSlots()
	{
		int[] array = new int[Capacity];
		for (int i = 0; i < Capacity; i++)
		{
			array[i] = slots[i];
		}
		Profile.SetQuestProgress(0, Key, string.Join(",", array));
	}

	public static void Open(int id, int inventoryPosition = 0)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		if (id / 10000 != 504)
		{
			return;
		}
		if ((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.player != (Object)null && GameLoop.I.player.hp <= 0f)
		{
			BottomBar.Toast("You can't do it while you're dead.");
			return;
		}
		int fieldLimit = FieldLimit;
		if ((fieldLimit & 0x40) != 0 || (fieldLimit & 2) != 0)
		{
			BottomBar.Toast("You cannot teleport to this map.");
			return;
		}
		itemId = id;
		itemPosition = inventoryPosition;
		vip = id / 1000 != 5040;
		selected = -1;
		scrollTop = 0;
		nameInput = "";
		dragging = false;
		centered = false;
		LoadSlots();
		opened = true;
		pos = new Vector2(300f, 160f);
		Center();
	}

	public static void Close()
	{
		opened = false;
		itemId = 0;
		selected = -1;
		scrollTop = 0;
		nameInput = "";
		dragging = false;
		centered = false;
	}

	private static void Center()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (!(S < 0.3f))
		{
			pos = NativeUi.ClampWindowPosition(new Vector2((UiKit.W / S - Size.x) / 2f, (UiKit.H / S - Size.y) / 2f), Size, S);
			centered = true;
		}
	}

	public static void Draw()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected Obj, but got Unknown
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Expected Obj, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Expected Obj, but got Unknown
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Expected Obj, but got Unknown
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Invalid comparison between Unknown and I4
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (!centered)
		{
			Center();
		}
		pos = NativeUi.ClampWindowPosition(pos, Size, S);
		Rect window = Window;
		NativeUi.RegisterWindow("NativeTeleportWindow", window, () => Visible);
		Vector2 val = Pointer.Pos / S;
		Rect val2;
		if ((int)Event.current.type == 7 && Pointer.Pressed)
		{
			val2 = At(window, 0f, 0f, 150f, 22f);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				centered = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, Size, S);
			window = Window;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Image(window, "backgrnd", 0f, 0f);
		if (NativeUi.Button(At(window, 156f, 6f, 12f, 12f), "UI/Basic.img/BtClose", val))
		{
			Close();
			return;
		}
		GUIStyle val3 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * S),
			alignment = (TextAnchor)5
		};
		val3.normal.textColor = Color.black;
		GUI.Label(At(window, 113f, 36f, 52f, 16f), "[" + ValidCount + "/" + Capacity + "]", val3);
		GUIStyle val4 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * S)
		};
		val4.normal.textColor = Color.black;
		int num = Mathf.Max(0, Capacity - 5);
		if (vip)
		{
			scrollTop = Mathf.RoundToInt(NativeUi.Scroll4(At(window, 155f, 80f, 15f, 98f), At(window, 9f, 80f, 158f, 95f), scrollTop, num, 1f, val));
		}
		for (int num2 = 0; num2 < 5; num2++)
		{
			int num3 = scrollTop + num2;
			if (num3 >= Capacity)
			{
				break;
			}
			int num4 = slots[num3];
			if (num4 == 999999999)
			{
				continue;
			}
			if (num3 == selected)
			{
				Image(window, "select", 9f, 84 + 18 * num2);
			}
			GUIStyle val5 = new GUIStyle(val4)
			{
				alignment = (TextAnchor)3
			};
			GUI.Label(At(window, 15f, 87 + 18 * num2, 140f, 16f), NativeMapNames.Of(num4), val5);
			if ((int)Event.current.type == 7 && Pointer.Clicked && !NativeUtilPrompt.Visible)
			{
				val2 = At(window, 9f, 84 + 18 * num2, 158f, 17f);
				if (val2.Contains(val))
				{
					selected = num3;
					Pointer.ConsumeClick();
				}
			}
		}
		Rect r = At(window, 34f, 250f, 120f, 13f);
		UiKit.Fill(r, Color.black);
		UiKit.Fill(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), Color.white);
		GUIStyle val6 = new GUIStyle
		{
			font = UiKit.Font,
			fontSize = Mathf.RoundToInt(12f * S)
		};
		val6.normal.textColor = Color.black;
		val6.alignment = (TextAnchor)3;
		nameInput = GUI.TextField(new Rect(r.x + 2f, r.y + 1f, r.width - 4f, r.height - 2f), nameInput, 12, val6);
		if (NativeUi.Button(At(window, 6f, 285f, 47f, 18f), "UI/UIWindow.img/Teleport/BtRegister", val))
		{
			Register();
		}
		if (NativeUi.Button(At(window, 56f, 285f, 47f, 18f), "UI/UIWindow.img/Teleport/BtDelete", val))
		{
			Delete();
		}
		if (NativeUi.Button(At(window, 120f, 285f, 47f, 18f), "UI/UIWindow.img/Teleport/BtMove", val))
		{
			Move();
		}
	}

	private static void Register()
	{
		int current = CurrentMapId;
		if (ValidCount >= Capacity)
		{
			BottomBar.Toast("Your teleport list is full.\nPlease delete an entry before trying again.");
			return;
		}
		for (int i = 0; i < Capacity; i++)
		{
			if (slots[i] == current)
			{
				BottomBar.Toast("You have already entered this map.");
				return;
			}
		}
		if (!Registrable(current))
		{
			BottomBar.Toast("This map is not available to enter for the list.");
			return;
		}
		FieldMap map = FieldCatalog.GetMap(current);
		if (map == null || (map.fieldLimit & 0x40) != 0)
		{
			BottomBar.Toast("You may not save this map.");
			return;
		}
		string text = NativeMapNames.Of(current);
		NativeUtilPrompt.Confirm("Will you enter this map in your teleport list?\n[" + text + "]", (bool ok) =>
		{
			if (ok)
			{
				for (int j = 0; j < Capacity; j++)
				{
					if (slots[j] == 999999999)
					{
						slots[j] = current;
						break;
					}
				}
				SaveSlots();
			}
		});
	}

	private static void Delete()
	{
		if (selected < 0 || selected >= Capacity || slots[selected] == 999999999)
		{
			return;
		}
		string text = NativeMapNames.Of(slots[selected]);
		NativeUtilPrompt.Confirm("Will you delete this map from the\n teleport list?\n[" + text + "]", (bool ok) =>
		{
			if (ok)
			{
				slots[selected] = 999999999;
				selected = -1;
				SaveSlots();
			}
		});
	}

	private static void Move()
	{
		if (selected >= 0 && selected < Capacity && slots[selected] != 999999999)
		{
			int mapId = slots[selected];
			if (mapId == CurrentMapId)
			{
				BottomBar.Toast("It's the map you're currently on.");
				return;
			}
			string text = NativeMapNames.Of(mapId);
			NativeUtilPrompt.Confirm("Will you teleport to this map?\n[" + text + "]", (bool ok) =>
			{
				if (ok)
				{
					Execute(mapId, null);
				}
			});
			return;
		}
		string text2 = ((nameInput != null) ? nameInput.Trim() : "");
		if (text2.Length == 0 || !NameValid(text2))
		{
			BottomBar.Toast("- The character name is invalid.");
			return;
		}
		if (text2 == Profile.playerName)
		{
			BottomBar.Toast("You cannot target yourself\nfor the teleport.");
			return;
		}
		CharRecord charRecord = FindCharacter(text2);
		if (charRecord == null)
		{
			BottomBar.Toast("在这个频道中找不到玩家。");
		}
		else
		{
			Execute(charRecord.map, text2);
		}
	}

	private static bool NameValid(string name)
	{
		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] < ' ')
			{
				return false;
			}
		}
		return true;
	}

	private static CharRecord FindCharacter(string name)
	{
		GameServer server = GameSession.Server;
		if (server == null)
		{
			return null;
		}
		DbRow dbRow = server.Db.Table("characters").Find(DbStore.Eq("name", name));
		if (dbRow != null)
		{
			return CharRecord.FromRow(dbRow);
		}
		return null;
	}

	private static void Execute(int mapId, string targetName)
	{
		int currentMapId = CurrentMapId;
		bool flag = targetName != null;
		CharRecord charRecord = (flag ? FindCharacter(targetName) : null);
		if (flag)
		{
			if (charRecord == null)
			{
				BottomBar.Toast("在这个频道中找不到玩家。");
				return;
			}
			if (charRecord.gm > 0 && !GmAtLeast(charRecord.gm))
			{
				BottomBar.Toast("找不到玩家或者您试图传送到一个非法的位置。");
				return;
			}
		}
		else if (itemId / 1000 < 5041 && mapId / 100000000 != currentMapId / 100000000)
		{
			BottomBar.Toast("您无法通过这个传送石在大陆之间传送。");
			return;
		}
		FieldMap map = FieldCatalog.GetMap(mapId);
		if (map == null || (map.fieldLimit & 0x40) != 0 || (map.forcedReturn != 999999999 && (mapId < 0 || mapId > 2000001)))
		{
			BottomBar.Toast(flag ? "您无法传送到这个地图。" : "找不到玩家或者您试图传送到一个非法的位置。");
		}
		else if (Profile.RemoveItem(itemId, 1, itemPosition))
		{
			Close();
			if ((Object)(object)GameLoop.I != (Object)null && (Object)(object)GameLoop.I.field != (Object)null)
			{
				GameLoop.I.field.WarpTo(mapId);
			}
		}
	}

	private static bool GmAtLeast(int victimGm)
	{
		FullCharacter character = GameSession.Character;
		int num = ((character != null && character.record != null) ? character.record.gm : 0);
		return victimGm <= num;
	}
}
