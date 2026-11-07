using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;

public static class NativeInventorySort
{
	private sealed class Slot
	{
		public int Index;

		public DbRow Row;

		public InventoryEntry Item;
	}

	private const int Equip = 1;

	private const int Cash = 5;

	private static int SlotLimit(DbStore db, CharRecord c, int type)
	{
		return type switch
		{
			4 => c.etcslots, 
			3 => c.setupslots, 
			2 => c.useslots, 
			1 => c.equipslots, 
			_ => 96, 
		};
	}

	private static bool Rechargeable(int itemId)
	{
		return itemId / 10000 == 207;
	}

	private static int ProjectileWatk(int itemId)
	{
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(itemId);
		if (dictionary != null)
		{
			return BotItemCatalog.Int(dictionary, "incPAD");
		}
		return 0;
	}

	private static InventoryChangeResponse ApplyOrder(DbStore db, List<Slot> ordered, int characterId)
	{
		for (int i = 0; i < ordered.Count; i++)
		{
			ordered[i].Row["position"] = (long)(i + 1);
			ordered[i].Item.position = i + 1;
		}
		return null;
	}

	private static List<Slot> LoadSlots(DbStore db, int characterId, int type)
	{
		List<Slot> list = new List<Slot>();
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, characterId))
		{
			if (DbStore.Int(item, "inventorytype") == type)
			{
				int num = DbStore.Int(item, "position");
				if (InventoryDao.IsBagPosition(num))
				{
					list.Add(new Slot
					{
						Index = num,
						Row = item,
						Item = CharService.ToInventoryEntry(db, item)
					});
				}
			}
		}
		list.Sort((Slot a, Slot b) => a.Index.CompareTo(b.Index));
		return list;
	}

	public static InventoryChangeResponse Sort(DbStore db, CharRecord c, int invType)
	{
		int characterId = (int)c.id;
		if (!GameConfigDao.GetBool(db, "use_item_sort", fallback: true))
		{
			return new InventoryChangeResponse
			{
				ok = true,
				inventory = CharService.Load(db, characterId).inventory
			};
		}
		if (invType < 1 || invType > 5)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "背包页无效"
			};
		}
		List<Slot> list = LoadSlots(db, characterId, invType);
		if (list.Exists((Slot s) => NativePotentialService.IsReserved(db, s.Item.itemRowId)))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "请先选择方块候选结果"
			};
		}
		if (list.Count == 0)
		{
			return new InventoryChangeResponse
			{
				ok = true,
				inventory = CharService.Load(db, characterId).inventory
			};
		}
		List<Slot> list2 = new List<Slot>(list);
		int primary = (GameConfigDao.GetBool(db, "use_item_sort_by_name") ? 2 : 0);
		int secondary = ((invType != 1) ? 1 : 3);
		PairedQuicksort(list2, primary, secondary, invType);
		ApplyOrder(db, list2, characterId);
		db.Save();
		return new InventoryChangeResponse
		{
			ok = true,
			inventory = CharService.Load(db, characterId).inventory
		};
	}

	private static void PairedQuicksort(List<Slot> a, int primary, int secondary, int invType)
	{
		if (a.Count == 0)
		{
			return;
		}
		MapleQuicksort(a, 0, a.Count - 1, primary);
		if (invType == 2)
		{
			ReverseSortSublist(a, BinarySearchElement(a, 206));
			ReverseSortSublist(a, BinarySearchElement(a, 207));
			ReverseSortSublist(a, BinarySearchElement(a, 233));
		}
		List<int> list = new List<int> { 0 };
		for (int i = 1; i < a.Count; i++)
		{
			if (a[i - 1].Item.itemId != a[i].Item.itemId)
			{
				list.Add(i);
			}
		}
		list.Add(a.Count);
		for (int j = 0; j < list.Count - 1; j++)
		{
			if (list[j + 1] > list[j])
			{
				MapleQuicksort(a, list[j], list[j + 1] - 1, secondary);
			}
		}
	}

	private static void MapleQuicksort(List<Slot> a, int esq, int dir, int sort)
	{
		int num;
		int num2;
		switch (sort)
		{
		case 3:
			(num, num2) = PartitionByLevel(a, esq, dir);
			break;
		case 2:
			(num, num2) = PartitionByName(a, esq, dir);
			break;
		case 1:
			(num, num2) = PartitionByQuantity(a, esq, dir);
			break;
		default:
			(num, num2) = PartitionByItemId(a, esq, dir);
			break;
		}
		if (esq < num2)
		{
			MapleQuicksort(a, esq, num2, sort);
		}
		if (num < dir)
		{
			MapleQuicksort(a, num, dir, sort);
		}
	}

	private static (int, int) PartitionByItemId(List<Slot> a, int esq, int dir)
	{
		int num = esq;
		int num2 = dir;
		int itemId = a[(num + num2) / 2].Item.itemId;
		while (true)
		{
			if (itemId > a[num].Item.itemId)
			{
				num++;
				continue;
			}
			while (itemId < a[num2].Item.itemId)
			{
				num2--;
			}
			if (num <= num2)
			{
				Slot value = a[num];
				a[num] = a[num2];
				a[num2] = value;
				num++;
				num2--;
			}
			if (num > num2)
			{
				break;
			}
		}
		return (num, num2);
	}

	private static (int, int) PartitionByQuantity(List<Slot> a, int esq, int dir)
	{
		int num = esq;
		int num2 = dir;
		int quantity = a[(num + num2) / 2].Item.quantity;
		while (true)
		{
			if (quantity > a[num].Item.quantity)
			{
				num++;
				continue;
			}
			while (quantity < a[num2].Item.quantity)
			{
				num2--;
			}
			if (num <= num2)
			{
				Slot value = a[num];
				a[num] = a[num2];
				a[num2] = value;
				num++;
				num2--;
			}
			if (num > num2)
			{
				break;
			}
		}
		return (num, num2);
	}

	private static int EquipLevel(InventoryEntry e)
	{
		return e.equipLevel;
	}

	private static (int, int) PartitionByLevel(List<Slot> a, int esq, int dir)
	{
		int num = esq;
		int num2 = dir;
		int num3 = EquipLevel(a[(num + num2) / 2].Item);
		while (true)
		{
			if (num3 > EquipLevel(a[num].Item))
			{
				num++;
				continue;
			}
			while (num3 < EquipLevel(a[num2].Item))
			{
				num2--;
			}
			if (num <= num2)
			{
				Slot value = a[num];
				a[num] = a[num2];
				a[num2] = value;
				num++;
				num2--;
			}
			if (num > num2)
			{
				break;
			}
		}
		return (num, num2);
	}

	private static (int, int) PartitionByName(List<Slot> a, int esq, int dir)
	{
		int num = esq;
		int num2 = dir;
		string strA = BotItemCatalog.Shared.Name(a[(num + num2) / 2].Item.itemId) ?? "";
		while (true)
		{
			if (string.CompareOrdinal(strA, BotItemCatalog.Shared.Name(a[num].Item.itemId) ?? "") > 0)
			{
				num++;
				continue;
			}
			while (string.CompareOrdinal(strA, BotItemCatalog.Shared.Name(a[num2].Item.itemId) ?? "") < 0)
			{
				num2--;
			}
			if (num <= num2)
			{
				Slot value = a[num];
				a[num] = a[num2];
				a[num2] = value;
				num++;
				num2--;
			}
			if (num > num2)
			{
				break;
			}
		}
		return (num, num2);
	}

	private static (int, int) PartitionByProjectileAtk(List<Slot> a, int esq, int dir)
	{
		int num = esq;
		int num2 = dir;
		int num3 = ProjectileWatk(a[(num + num2) / 2].Item.itemId);
		while (true)
		{
			if (num3 < ProjectileWatk(a[num].Item.itemId))
			{
				num++;
				continue;
			}
			while (num3 > ProjectileWatk(a[num2].Item.itemId))
			{
				num2--;
			}
			if (num <= num2)
			{
				Slot value = a[num];
				a[num] = a[num2];
				a[num2] = value;
				num++;
				num2--;
			}
			if (num > num2)
			{
				break;
			}
		}
		return (num, num2);
	}

	private static int Subtype(InventoryEntry e)
	{
		return e.itemId / 10000;
	}

	private static int[] BinarySearchElement(List<Slot> a, int rangeId)
	{
		int num = 0;
		int num2 = a.Count - 1;
		int num3 = -1;
		while (num2 >= num)
		{
			num3 = (num + num2) / 2;
			int num4 = Subtype(a[num3].Item);
			if (num4 == rangeId)
			{
				break;
			}
			if (num4 < rangeId)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		if (num2 < num)
		{
			return null;
		}
		num = num3 - 1;
		num2 = num3 + 1;
		while (num >= 0 && Subtype(a[num].Item) == rangeId)
		{
			num--;
		}
		num++;
		for (; num2 < a.Count && Subtype(a[num2].Item) == rangeId; num2++)
		{
		}
		num2--;
		return new int[2] { num, num2 };
	}

	private static void ReverseSortSublist(List<Slot> a, int[] range)
	{
		if (range != null)
		{
			PartitionByProjectileAtk(a, range[0], range[1]);
		}
	}

	public static InventoryChangeResponse Merge(DbStore db, CharRecord c, int invType)
	{
		int num = (int)c.id;
		if (!GameConfigDao.GetBool(db, "use_item_sort", fallback: true))
		{
			return new InventoryChangeResponse
			{
				ok = true,
				inventory = CharService.Load(db, num).inventory
			};
		}
		if (invType < 1 || invType > 5)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "背包页无效"
			};
		}
		List<Slot> list = LoadSlots(db, num, invType);
		if (list.Exists((Slot s) => NativePotentialService.IsReserved(db, s.Item.itemRowId)))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "请先选择方块候选结果"
			};
		}
		if (list.Count == 0)
		{
			return new InventoryChangeResponse
			{
				ok = true,
				inventory = CharService.Load(db, num).inventory
			};
		}
		int num2 = SlotLimit(db, c, invType);
		BotActor actor = new BotActor
		{
			id = num,
			job = c.job,
			character = CharService.Load(db, num)
		};
		TradeItemRules rules = new TradeItemRules(db, BotItemCatalog.Shared);
		Dictionary<int, Slot> map = new Dictionary<int, Slot>();
		foreach (Slot item in list)
		{
			map[item.Index] = item;
		}
		for (int num3 = 1; num3 <= num2; num3++)
		{
			Slot slot = At(num3);
			if (slot == null)
			{
				continue;
			}
			for (int num4 = num3 + 1; num4 <= num2; num4++)
			{
				Slot slot2 = At(num4);
				if (slot2 != null && slot.Item.itemId == slot2.Item.itemId)
				{
					if (slot.Item.quantity == SlotMaxOf(slot.Item.itemId))
					{
						break;
					}
					Move(map, invType, num4, num3, SlotMaxOf(slot2.Item.itemId));
				}
			}
		}
		int num5 = 1;
		while (true)
		{
			if (num5 <= num2 && At(num5) != null)
			{
				num5++;
				continue;
			}
			if (num5 > num2)
			{
				break;
			}
			int num6 = -1;
			for (int num7 = num5 + 1; num7 <= num2; num7++)
			{
				if (At(num7) != null)
				{
					num6 = num7;
					break;
				}
			}
			if (num6 <= 0)
			{
				break;
			}
			Move(map, invType, num6, num5, SlotMaxOf(At(num6).Item.itemId));
		}
		Persist(db, list, map);
		db.Save();
		return new InventoryChangeResponse
		{
			ok = true,
			inventory = CharService.Load(db, num).inventory
		};
		Slot At(int position)
		{
			if (!map.TryGetValue(position, out var value))
			{
				return null;
			}
			return value;
		}
		int SlotMaxOf(int itemId)
		{
			return rules.SlotMax(itemId, actor);
		}
	}

	private static void Persist(DbStore db, List<Slot> slots, Dictionary<int, Slot> map)
	{
		foreach (KeyValuePair<int, Slot> item in map)
		{
			item.Value.Row["position"] = (long)item.Key;
			item.Value.Item.position = item.Key;
		}
		HashSet<long> hashSet = new HashSet<long>();
		foreach (KeyValuePair<int, Slot> item2 in map)
		{
			hashSet.Add(item2.Value.Item.itemRowId);
		}
		foreach (Slot slot in slots)
		{
			if (!hashSet.Contains(slot.Item.itemRowId))
			{
				long rowId = slot.Item.itemRowId;
				db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
				db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == rowId);
			}
		}
	}

	public static InventoryChangeResponse MoveItem(DbStore db, CharRecord c, int invType, int from, int to)
	{
		int num = (int)c.id;
		if (invType < 1 || invType > 5)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "背包页无效"
			};
		}
		int num2 = SlotLimit(db, c, invType);
		if (from <= 0 || to <= 0 || from == to || from > num2 || to > num2)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "槽位无效"
			};
		}
		List<Slot> list = LoadSlots(db, num, invType);
		Dictionary<int, Slot> dictionary = new Dictionary<int, Slot>();
		foreach (Slot item in list)
		{
			dictionary[item.Index] = item;
		}
		if (!dictionary.TryGetValue(from, out var value))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "该格子没有物品"
			};
		}
		if (NativePotentialService.IsReserved(db, value.Item.itemRowId) || (dictionary.TryGetValue(to, out var value2) && NativePotentialService.IsReserved(db, value2.Item.itemRowId)))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "请先选择方块候选结果"
			};
		}
		BotActor actor = new BotActor
		{
			id = num,
			job = c.job,
			character = CharService.Load(db, num)
		};
		TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
		Move(dictionary, invType, from, to, tradeItemRules.SlotMax(value.Item.itemId, actor));
		Persist(db, list, dictionary);
		db.Save();
		return new InventoryChangeResponse
		{
			ok = true,
			inventory = CharService.Load(db, num).inventory
		};
	}

	private static void Move(Dictionary<int, Slot> map, int invType, int src, int dst, int slotMax)
	{
		if (src < 0 || dst < 0 || !map.TryGetValue(src, out var value))
		{
			return;
		}
		if (!map.TryGetValue(dst, out var value2))
		{
			map.Remove(src);
			map[dst] = value;
			value.Row["position"] = (long)dst;
			value.Item.position = dst;
		}
		else if (value2.Item.itemId == value.Item.itemId && !Rechargeable(value.Item.itemId) && string.Equals(value2.Item.owner ?? "", value.Item.owner ?? "", StringComparison.Ordinal) && invType != 1 && invType != 5)
		{
			int num = value.Item.quantity + value2.Item.quantity;
			if (num > slotMax)
			{
				value.Item.quantity = num - slotMax;
				value2.Item.quantity = slotMax;
				value.Row["quantity"] = (long)value.Item.quantity;
				value2.Row["quantity"] = (long)value2.Item.quantity;
			}
			else
			{
				map.Remove(src);
				value2.Item.quantity = num;
				value2.Row["quantity"] = (long)num;
				value.Item.position = 0;
			}
		}
		else
		{
			map[src] = value2;
			map[dst] = value;
			value.Row["position"] = (long)dst;
			value.Item.position = dst;
			value2.Row["position"] = (long)src;
			value2.Item.position = src;
		}
	}
}
