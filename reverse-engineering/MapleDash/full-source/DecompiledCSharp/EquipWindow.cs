using MapleDash.Wz;
using UnityEngine;

public static class EquipWindow
{
	public static bool Visible;

	private static Vector2 pos = new Vector2(200f, 100f);

	private static bool expanded;

	private static bool dragging;

	private static bool petVisible;

	private static int petIndex;

	private static double petReadAt;

	private static readonly InventoryEntry[] pets = new InventoryEntry[3];

	private static Vector2 grab;

	private const int SmallW = 175;

	private const int SmallH = 304;

	private const int FullW = 208;

	private const int FullH = 291;

	private const int GridX = 5;

	private const int GridY = 35;

	private const int Step = 33;

	private const int Cell = 32;

	private static readonly int[,] SmallCells = new int[7, 5]
	{
		{ 0, -1, 0, 0, 0 },
		{ -49, -2, 0, -12, -13 },
		{ 0, 0, -3, -4, -20 },
		{ -9, -5, -17, -11, -10 },
		{ -8, -6, -50, -15, -16 },
		{ 0, 0, -7, -51, -53 },
		{ -18, -19, 0, 0, -56 }
	};

	private static readonly int[,] FullCells = new int[7, 6]
	{
		{ 0, -1, 0, 0, 0, 0 },
		{ -49, -2, 0, 0, -12, -13 },
		{ 0, 0, -3, 0, -4, 0 },
		{ -9, -5, -17, 0, -11, -10 },
		{ -8, -6, -50, 0, -15, -16 },
		{ 0, 0, -7, 0, 0, 0 },
		{ -18, -19, 0, 0, 0, 0 }
	};

	private static readonly int[,] PetCells = new int[29, 4]
	{
		{ 14, 112, 77, 0 },
		{ 21, 79, 110, 0 },
		{ 22, 13, 44, 0 },
		{ 23, 46, 44, 0 },
		{ 24, 13, 11, -1 },
		{ 25, 79, 11, -1 },
		{ 26, 79, 77, 0 },
		{ 27, 13, 110, 0 },
		{ 28, 46, 77, 0 },
		{ 29, 112, 110, 0 },
		{ 30, 112, 77, 1 },
		{ 31, 79, 110, 1 },
		{ 32, 112, 110, 1 },
		{ 33, 13, 44, 1 },
		{ 34, 46, 44, 1 },
		{ 35, 79, 77, 1 },
		{ 36, 13, 110, 1 },
		{ 37, 46, 77, 1 },
		{ 38, 112, 77, 2 },
		{ 39, 79, 110, 2 },
		{ 40, 112, 110, 2 },
		{ 41, 13, 44, 2 },
		{ 42, 46, 44, 2 },
		{ 43, 79, 77, 2 },
		{ 44, 13, 110, 2 },
		{ 45, 46, 77, 2 },
		{ 46, 13, 77, 0 },
		{ 47, 13, 77, 1 },
		{ 48, 13, 77, 2 }
	};

	public static int SelectedPet => petIndex;

	private static float S => BottomBar.S;

	public static Vector2 Position
	{
		get
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return pos;
		}
	}

	public static Vector2 Size
	{
		get
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2((float)(expanded ? 208 : 175), (float)(expanded ? 291 : 304));
		}
	}

	public static InventoryEntry EquipmentAt(Vector2 pointer)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || GameSession.Character == null)
		{
			return null;
		}
		int num = (expanded ? 208 : 175);
		int num2 = (expanded ? 291 : 304);
		Vector2 val = NativeUi.ClampWindowPosition(pos, new Vector2((float)num, (float)num2), S) * S;
		Rect r = new Rect(val.x, val.y, (float)num * S, (float)num2 * S);
		Rect val2;
		if (petVisible)
		{
			Rect r2 = At(r, 172f, 123f, 177f, 181f);
			int i;
			for (i = 0; i < PetCells.GetLength(0); i++)
			{
				if (PetCells[i, 3] >= 0 && PetCells[i, 3] != petIndex)
				{
					continue;
				}
				val2 = At(r2, PetCells[i, 1], PetCells[i, 2], 32f, 32f);
				if (val2.Contains(pointer))
				{
					return GameSession.Character.inventory.Find((InventoryEntry item) => item.inventoryType == 1 && item.position == -PetCells[i, 0]);
				}
			}
		}
		int[,] cells = (expanded ? FullCells : SmallCells);
		for (int row = 0; row < cells.GetLength(0); row++)
		{
			int col;
			for (col = 0; col < cells.GetLength(1); col++)
			{
				if (cells[row, col] == 0)
				{
					continue;
				}
				val2 = At(r, 5 + col * 33, 35 + row * 33, 32f, 32f);
				if (val2.Contains(pointer))
				{
					return GameSession.Character.inventory.Find((InventoryEntry inventoryEntry) => inventoryEntry.position == EquipSlot.WornSlotOf(cells[row, col]) && inventoryEntry.inventoryType == 1);
				}
			}
		}
		return null;
	}

	public static bool SlotAt(Vector2 pointer, out int cellSlot)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (NativeAndroidWindow.SlotAt(pointer, out cellSlot))
		{
			return true;
		}
		cellSlot = 0;
		if (!Visible || GameSession.Character == null)
		{
			return false;
		}
		int num = (expanded ? 208 : 175);
		int num2 = (expanded ? 291 : 304);
		Vector2 val = NativeUi.ClampWindowPosition(pos, new Vector2((float)num, (float)num2), S) * S;
		Rect r = new Rect(val.x, val.y, (float)num * S, (float)num2 * S);
		int[,] array = (expanded ? FullCells : SmallCells);
		for (int i = 0; i < array.GetLength(0); i++)
		{
			for (int j = 0; j < array.GetLength(1); j++)
			{
				if (array[i, j] != 0)
				{
					Rect val2 = At(r, 5 + j * 33, 35 + i * 33, 32f, 32f);
					if (val2.Contains(pointer))
					{
						cellSlot = array[i, j];
						return true;
					}
				}
			}
		}
		return false;
	}

	private static Rect At(Rect r, float x, float y, float w, float h)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(r.x + x * S, r.y + y * S, w * S, h * S);
	}

	private static Texture2D Tex(string node)
	{
		return Art.Tex("wz://UI/UIWindow.img/Equip/" + node);
	}

	public static void Toggle()
	{
		Visible = !Visible;
		if (!Visible)
		{
			petVisible = false;
		}
	}

	public static void Draw()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Invalid comparison between Unknown and I4
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Invalid comparison between Unknown and I4
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		int num = (expanded ? 208 : 175);
		int num2 = (expanded ? 291 : 304);
		pos = NativeUi.ClampWindowPosition(pos, new Vector2((float)num, (float)num2), S);
		Rect val = new Rect(pos.x * S, pos.y * S, (float)num * S, (float)num2 * S);
		NativeUi.RegisterWindow("EquipWindow", val, () => Visible);
		bool flag = (int)Event.current.type == 7 && Pointer.Pressed;
		if ((int)Event.current.type == 7)
		{
			_ = Pointer.Released;
		}
		else
			_ = 0;
		if (flag)
		{
			Rect val2 = At(val, 0f, 0f, num - 24, 22f);
			if (val2.Contains(Pointer.Pos))
			{
				dragging = true;
				grab = Pointer.Pos / S - pos;
			}
		}
		if (dragging && Pointer.Down)
		{
			pos = NativeUi.ClampWindowPosition(Pointer.Pos / S - grab, new Vector2((float)num, (float)num2), S);
			val.position = pos * S;
		}
		if (!Pointer.Down)
		{
			dragging = false;
		}
		Texture2D val3 = (expanded ? Tex("FullBackgrnd") : (NativeEquipBackdrop.Texture() ?? Tex("backgrnd")));
		if ((Object)(object)val3 != (Object)null)
		{
			GUI.DrawTexture(val, (Texture)(object)val3);
		}
		if (NativeUi.Button(At(val, num - 19, 5f, 12f, 12f), "UI/Basic.img/BtClose"))
		{
			Visible = false;
			return;
		}
		int[,] array = (expanded ? FullCells : SmallCells);
		int length = array.GetLength(0);
		int length2 = array.GetLength(1);
		for (int num3 = 0; num3 < length; num3++)
		{
			for (int num4 = 0; num4 < length2; num4++)
			{
				int num5 = array[num3, num4];
				if (num5 == 0)
				{
					continue;
				}
				Rect val4 = At(val, 5 + num4 * 33, 35 + num3 * 33, 32f, 32f);
				int num6 = EquipSlot.WornSlotOf(num5);
				int num7 = Profile.EquipOfSlot(num6);
				if (num7 <= 0)
				{
					continue;
				}
				Texture2D val5 = IconOf(num6, num7);
				if ((Object)(object)val5 != (Object)null)
				{
					GUI.DrawTexture(val4, (Texture)(object)val5, (ScaleMode)2);
				}
				if ((int)Event.current.type != 7)
				{
					continue;
				}
				if (val4.Contains(Pointer.Pos))
				{
					ItemTooltip.SetHover(num7, equipped: true);
				}
				if (flag && val4.Contains(Pointer.Pos) && !NativeInventoryActions.Carrying)
				{
					NativeInventoryActions.PickEquipped(num7, num6);
				}
				if (Pointer.DoubleClicked && val4.Contains(Pointer.Pos) && num6 != -11)
				{
					Pointer.ConsumeClick();
					NativeInventoryActions.CancelCarry();
					if (num5 == -49)
					{
						NativeMedalWindow.Wear(num7, unequip: true);
					}
					else
					{
						EquipLoadout.Unequip(num6);
					}
					if (Profile.EquipOfSlot(num6) == 0)
					{
						BottomBar.Toast("已脱下" + EquipSlot.NameOf(num5));
					}
				}
			}
		}
		if ((Profile.JobId / 100 == 22 || Profile.JobId == 2001) && NativeUi.Button(At(val, 5f, 267f, 46f, 19f), "UI/UIWindow.img/Equip/BtDetail"))
		{
			expanded = !expanded;
			return;
		}
		if (!expanded && NativeUi.Button(At(val, 102f, 280f, 67f, 18f), "UI/UIWindow.img/Equip/BtPetEquipShow"))
		{
			petVisible = !petVisible;
			petReadAt = 0.0;
		}
		float x = ((Profile.JobId / 100 == 22 || Profile.JobId == 2001) ? 56f : 5f);
		if (NativeAndroidWindow.ToggleButton(At(val, x, expanded ? 268f : 280f, 46f, 18f), NativeAndroidWindow.Visible, "機器人"))
		{
			NativeAndroidWindow.Toggle();
			Pointer.ConsumeClick();
		}
		if (petVisible)
		{
			DrawPet(val);
		}
	}

	public static bool DropPetPotion(Vector2 pointer, int id)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible || !petVisible || !GameSession.InGame)
		{
			return false;
		}
		Rect r = At(new Rect(pos.x * S, pos.y * S, 0f, 0f), 172f, 123f, 177f, 181f);
		for (int i = 0; i < 2; i++)
		{
			Rect val = At(r, (i == 0) ? 46 : 112, 11f, 32f, 32f);
			if (val.Contains(pointer))
			{
				ImgNode imgNode = NativeItemSource.Node(id)?.Get("spec");
				bool flag = i == 0;
				if (Profile.EquipOfSlot(flag ? (-24) : (-25)) <= 0)
				{
					BottomBar.Toast("请先给宠物装备对应的自动药水包");
					return true;
				}
				if (imgNode == null || id / 1000000 != 2 || (imgNode.Int(flag ? "hp" : "mp") <= 0 && imgNode.Int(flag ? "hpR" : "mpR") <= 0))
				{
					BottomBar.Toast(flag ? "请放入恢复HP的药水" : "请放入恢复MP的药水");
					return true;
				}
				KeyMap.BindPetPotion(flag, id);
				BottomBar.Toast(((flag ? ClientSettings.HpAlarm : ClientSettings.MpAlarm) == 0) ? ("药水已绑定，请在系统设置中调高" + (flag ? "HP" : "MP") + "警报比例以启用自动补药") : ("已绑定" + WzNames.Item(id)));
				return true;
			}
		}
		return false;
	}

	private static void DrawPet(Rect parent)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Invalid comparison between Unknown and I4
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Invalid comparison between Unknown and I4
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		if (!GameSession.InGame)
		{
			return;
		}
		if (Time.realtimeSinceStartupAsDouble >= petReadAt)
		{
			petReadAt = Time.realtimeSinceStartupAsDouble + 0.25;
			for (int i = 0; i < 3; i++)
			{
				pets[i] = GameSession.Net.ChangeInventory(new InventoryChangeRequest
				{
					characterId = GameSession.CharacterId,
					getPet = true,
					petSlot = i
				}).pet;
			}
			if (pets[petIndex] == null)
			{
				petIndex = 0;
			}
		}
		Rect r = At(parent, 172f, 123f, 177f, 181f);
		ImagePet("pet", 0, 0);
		if (NativeUi.Button(At(r, 155f, 159f, 17f, 16f), "UI/UIWindow.img/Equip/BtPetEquipHide"))
		{
			petVisible = false;
			return;
		}
		bool enabled = GUI.enabled;
		for (int j = 0; j < 3; j++)
		{
			GUI.enabled = enabled && pets[j] != null;
			string text = "UI/UIWindow.img/UserInfo/BtPet" + (j + 1);
			Texture2D val = Art.Tex("wz://" + text + "/normal/0");
			if ((Object)(object)val != (Object)null && NativeUi.Button(At(r, 10 + 34 * j, 151f, ((Texture)val).width, ((Texture)val).height), text))
			{
				petIndex = j;
			}
		}
		GUI.enabled = enabled;
		for (int k = 0; k < 2; k++)
		{
			int num = KeyMap.PetPotion(k == 0);
			Rect val2 = At(r, (k == 0) ? 46 : 112, 11f, 32f, 32f);
			Texture2D val3 = ((num > 0) ? NativeItemSource.Icon(num) : null);
			ImgNode imgNode = ((num <= 0) ? null : NativeItemSource.IconNode(num)?.Get("origin"));
			if ((Object)(object)val3 != (Object)null)
			{
				GUI.DrawTexture(At(r, ((k == 0) ? 46 : 112) - (imgNode?.X ?? 0), 43 - (imgNode?.Y ?? 0), ((Texture)val3).width, ((Texture)val3).height), (Texture)(object)val3);
			}
			if (num > 0 && (int)Event.current.type == 7 && val2.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(num);
				if (Pointer.DoubleClicked)
				{
					Pointer.ConsumeClick();
					KeyMap.BindPetPotion(k == 0, 0);
				}
			}
		}
		if (pets[petIndex] == null)
		{
			Vector2[] array = new Vector2[5]
			{
				new Vector2(112f, 77f),
				new Vector2(79f, 110f),
				new Vector2(112f, 110f),
				new Vector2(46f, 11f),
				new Vector2(112f, 11f)
			};
			foreach (Vector2 val4 in array)
			{
				UiKit.Fill(At(r, val4.x, val4.y, 32f, 32f), Color32.op_Implicit(new Color32(byte.MaxValue, (byte)0, (byte)0, (byte)64)));
			}
		}
		for (int m = 0; m < PetCells.GetLength(0); m++)
		{
			if (PetCells[m, 3] >= 0 && PetCells[m, 3] != petIndex)
			{
				continue;
			}
			int slot = -PetCells[m, 0];
			int num2 = Profile.EquipOfSlot(slot);
			if (num2 <= 0)
			{
				continue;
			}
			Texture2D val5 = NativeItemSource.Icon(num2);
			ImgNode imgNode2 = NativeItemSource.IconNode(num2)?.Get("origin");
			int num3 = PetCells[m, 1];
			int num4 = PetCells[m, 2];
			if ((Object)(object)val5 != (Object)null)
			{
				GUI.DrawTexture(At(r, num3 - (imgNode2?.X ?? 0), num4 + 32 - (imgNode2?.Y ?? 0), ((Texture)val5).width, ((Texture)val5).height), (Texture)(object)val5);
			}
			Rect val6 = At(r, num3, num4, 32f, 32f);
			if ((int)Event.current.type == 7 && val6.Contains(Pointer.Pos))
			{
				ItemTooltip.SetHover(num2, equipped: true);
				if (Pointer.DoubleClicked)
				{
					Pointer.ConsumeClick();
					EquipLoadout.Unequip(slot);
				}
			}
		}
		void ImagePet(string path, int x, int y)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val7 = Tex(path);
			if ((Object)(object)val7 != (Object)null)
			{
				GUI.DrawTexture(At(r, x, y, ((Texture)val7).width, ((Texture)val7).height), (Texture)(object)val7);
			}
		}
	}

	private static Texture2D IconOf(int slot, int id)
	{
		if (slot == -11)
		{
			return Art.Tex("wz://Character/Weapon/" + id.ToString("D8") + ".img/info/icon");
		}
		return ArmorIcon(id);
	}

	public static Texture2D ArmorIcon(int itemId)
	{
		string text = EquipCatalog.DirOf(itemId);
		if (text == null)
		{
			return null;
		}
		return Art.Tex("wz://Character/" + text + "/" + itemId.ToString("D8") + ".img/info/icon");
	}

	static EquipWindow()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
	}
}
