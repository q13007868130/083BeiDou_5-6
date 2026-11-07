public static class EquipSlot
{
	public const int Hat = -1;

	public const int FaceAccessory = -2;

	public const int EyeAccessory = -3;

	public const int Earring = -4;

	public const int Top = -5;

	public const int Bottom = -6;

	public const int Shoes = -7;

	public const int Glove = -8;

	public const int Cape = -9;

	public const int Shield = -10;

	public const int Weapon = -11;

	public const int Medal = -49;

	public const int Android = -32;

	public const int MechanicalHeart = -54;

	public static readonly int[] Appearance = new int[8] { -1, -5, -6, -7, -8, -9, -10, -11 };

	public static readonly int[] Worn = new int[53]
	{
		-1, -5, -6, -7, -8, -9, -10, -11, -49, -2,
		-3, -4, -12, -13, -15, -16, -17, -18, -19, -50,
		-20, -55, -56, -54, -32, -53, -51, -101, -102, -103,
		-104, -105, -106, -107, -108, -109, -110, -111, -112, -113,
		-115, -116, -117, -118, -119, -120, -149, -150, -151, -153,
		-154, -155, -156
	};

	public static string NameOf(int slot)
	{
		return slot switch
		{
			-1 => "帽子", 
			-2 => "脸饰", 
			-3 => "眼饰", 
			-4 => "耳环", 
			-5 => "上衣", 
			-6 => "下装", 
			-7 => "鞋子", 
			-8 => "手套", 
			-9 => "披风", 
			-10 => "盾牌", 
			-11 => "武器", 
			-49 => "勋章", 
			-32 => "机器人", 
			-54 => "心臟", 
			_ => "装备", 
		};
	}

	public static int SlotOf(int itemId)
	{
		return NativeInventoryService.EquipmentSlotOf(itemId);
	}

	public static int CashOf(int slot)
	{
		if (slot != 0)
		{
			return slot - 100;
		}
		return 0;
	}

	public static int WornSlotOf(int cellSlot)
	{
		if (cellSlot == 0 || Profile.EquipOfSlot(CashOf(cellSlot)) <= 0)
		{
			return cellSlot;
		}
		return CashOf(cellSlot);
	}
}
