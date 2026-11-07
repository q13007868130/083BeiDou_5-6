using System;
using System.Collections.Generic;
using MapleDash.Wz;

public sealed class NativeCashCatalog : INativeCashCatalog
{
	private readonly Dictionary<int, CashCommodity> items = new Dictionary<int, CashCommodity>();

	private readonly Dictionary<int, IList<int>> packages = new Dictionary<int, IList<int>>();

	private readonly Dictionary<int, bool> complete = new Dictionary<int, bool>();

	public IEnumerable<CashCommodity> All => items.Values;

	public NativeCashCatalog()
	{
		foreach (ImgNode child in WzAssets.FileRoot("Etc/Commodity.img").Children)
		{
			if (child.Get("SN") != null && child.Get("ItemId") != null)
			{
				int num = child.Int("Period", 1);
				int num2 = child.Int("SN");
				items[num2] = new CashCommodity
				{
					sn = num2,
					itemId = child.Int("ItemId"),
					price = child.Int("Price"),
					count = child.Int("Count", 1),
					period = ((num == 0) ? 90 : num),
					onSale = child.Int("OnSale"),
					gender = child.Int("Gender", 2),
					priority = child.Int("Priority"),
					clz = child.Int("Class"),
					limit = IntOf(child, "Limit"),
					pbCash = child.Int("PbCash"),
					pbPoint = child.Int("PbPoint"),
					pbGift = child.Int("PbGift")
				};
			}
		}
		foreach (ImgNode child2 in WzAssets.FileRoot("Etc/CashPackage.img").Children)
		{
			if (!int.TryParse(child2.Name, out var result))
			{
				continue;
			}
			List<int> list = new List<int>();
			ImgNode imgNode = child2.Get("SN");
			if (imgNode != null)
			{
				foreach (ImgNode child3 in imgNode.Children)
				{
					if (child3.Value != null)
					{
						list.Add(Convert.ToInt32(child3.Value));
					}
				}
			}
			packages[result] = list;
		}
	}

	private static int IntOf(ImgNode node, string key, int fallback = 0)
	{
		object obj = node.Get(key)?.Value;
		if (obj == null)
		{
			return fallback;
		}
		if (obj is int)
		{
			return (int)obj;
		}
		if (obj is string s && int.TryParse(s, out var result))
		{
			return result;
		}
		try
		{
			return Convert.ToInt32(obj);
		}
		catch
		{
			return fallback;
		}
	}

	public CashCommodity Find(int sn)
	{
		if (!items.TryGetValue(sn, out var value))
		{
			return null;
		}
		return value;
	}

	public IList<int> Package(int id)
	{
		if (!packages.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public bool Exists(int id)
	{
		if (Package(id) == null)
		{
			return NativeItemSource.Node(id) != null;
		}
		return true;
	}

	public bool Complete(int id)
	{
		if (complete.TryGetValue(id, out var value))
		{
			return value;
		}
		value = WzNames.HasItem(id) && (Package(id) != null || NativeItemSource.IconNode(id) != null);
		complete[id] = value;
		return value;
	}

	public bool IsCash(int id)
	{
		if (id / 1000000 != 5)
		{
			ImgNode imgNode = NativeItemSource.Node(id);
			if (imgNode == null)
			{
				return false;
			}
			return imgNode.Int("info/cash") == 1;
		}
		return true;
	}

	public int EquipSlot(int id)
	{
		return global::EquipSlot.SlotOf(id);
	}

	public string CanEquip(int id, CharRecord c)
	{
		ImgNode imgNode = EquipCatalog.Info(id);
		if (imgNode == null)
		{
			return "装备数据缺失";
		}
		int num = id / 1000 % 10;
		if (num < 2 && num != c.gender)
		{
			return "装备性别不符";
		}
		int num2 = imgNode.Int("reqJob");
		if (num2 != 0 && (num2 & JobDefs.JobBit(c.job)) == 0)
		{
			return "职业不符";
		}
		if (c.level < imgNode.Int("reqLevel") || c.str < imgNode.Int("reqSTR") || c.dex < imgNode.Int("reqDEX") || c.int_ < imgNode.Int("reqINT") || c.luk < imgNode.Int("reqLUK"))
		{
			return "未达到装备要求";
		}
		return null;
	}

	public DbRow Equipment(int id)
	{
		ImgNode imgNode = EquipCatalog.Info(id);
		if (imgNode == null)
		{
			return null;
		}
		string[] array = new string[16]
		{
			"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
			"acc", "avoid", "hands", "speed", "jump", "upgradeslots"
		};
		string[] array2 = new string[16]
		{
			"incSTR", "incDEX", "incINT", "incLUK", "incMHP", "incMMP", "incPAD", "incMAD", "incPDD", "incMDD",
			"incACC", "incEVA", "incCraft", "incSpeed", "incJump", "tuc"
		};
		DbRow dbRow = new DbRow();
		for (int i = 0; i < array2.Length; i++)
		{
			dbRow[array[i]] = (long)imgNode.Int(array2[i]);
		}
		return dbRow;
	}
}
