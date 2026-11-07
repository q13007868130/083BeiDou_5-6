using System;
using System.Collections.Generic;

namespace Server.Bot.Trade;

public sealed class TradeItemRules
{
	private readonly DbStore db;

	private readonly BotItemCatalog catalog;

	public TradeItemRules(DbStore db, BotItemCatalog catalog)
	{
		this.db = db;
		this.catalog = catalog;
	}

	public static bool Rechargeable(int id)
	{
		if (id / 10000 != 207)
		{
			return id / 10000 == 233;
		}
		return true;
	}

	public static bool MonsterCard(int id)
	{
		return id / 10000 == 238;
	}

	public bool Unique(int id)
	{
		return BotItemCatalog.Int(catalog.Info(id), "only") == 1;
	}

	private bool Config(string key)
	{
		return GameConfigDao.Get(db, key).Equals("true", StringComparison.OrdinalIgnoreCase);
	}

	public bool CanOffer(InventoryEntry item)
	{
		if (NativePotentialService.BlocksTransfer(db, item))
		{
			return false;
		}
		Dictionary<string, object> info = catalog.Info(item.itemId);
		bool flag = BotItemCatalog.Int(info, "cash") == 1;
		bool flag2 = (Config("use_enforce_unmerchable_cash") & flag) || (Config("use_enforce_unmerchable_pet") && item.itemId / 1000 == 5000);
		if (!Config("trade_limit_item_cash") & flag2)
		{
			return false;
		}
		bool flag3 = BotItemCatalog.Int(info, "tradeBlock") == 1 || BotItemCatalog.Int(info, "accountSharable") == 1 || BotItemCatalog.Int(info, "quest") == 1;
		int num = (item.isEquip ? 16 : 2);
		if (!Config("trade_limit_item_nodrop") && flag3)
		{
			return (item.flag & num) == num;
		}
		return true;
	}

	public int SlotMax(int id, BotActor actor)
	{
		Dictionary<string, object> dictionary = catalog.Info(id);
		if (dictionary == null)
		{
			if (id / 1000000 != 1)
			{
				return 100;
			}
			return 1;
		}
		int num = 0;
		if (dictionary != null)
		{
			int num2 = id / 1000000;
			int num3 = (short)GameConfigDao.GetInt(db, "item_slot_max");
			bool flag = (num2 == 2 || num2 == 4) && num3 > 0;
			if (!dictionary.ContainsKey("slotMax"))
			{
				num = ((num2 == 1) ? 1 : (flag ? num3 : 100));
			}
			else
			{
				int num4 = BotItemCatalog.Int(dictionary, "slotMax");
				num = ((num4 == 1) ? 1 : (flag ? num3 : ((short)num4)));
			}
		}
		int num5;
		if (id / 10000 != 207)
		{
			num5 = ((id / 10000 == 233) ? 5200000 : 0);
		}
		else
		{
			num5 = ((actor.job >= 1400 && actor.job <= 1412) ? 14100000 : 4100000);
		}
		if (num5 != 0 && actor.character != null)
		{
			foreach (SkillEntry skill in actor.character.skills)
			{
				if (skill.skillId == num5)
				{
					num += skill.skillLevel * 10;
					break;
				}
			}
		}
		return (short)num;
	}

	public static void ApplyKarma(InventoryEntry item)
	{
		int num = (item.isEquip ? 16 : 2);
		if ((item.flag & num) == num)
		{
			item.flag = (item.flag ^ num) | 8;
		}
	}

	public static int SlotLimit(BotActor actor, int type)
	{
		CharRecord charRecord = actor.character?.record;
		if (charRecord == null)
		{
			if (type != 5)
			{
				return 24;
			}
			return 96;
		}
		return type switch
		{
			1 => charRecord.equipslots, 
			2 => charRecord.useslots, 
			3 => charRecord.setupslots, 
			4 => charRecord.etcslots, 
			5 => 96, 
			_ => 0, 
		};
	}

	public static int FreeSlot(BotActor actor, List<InventoryEntry> inventory, int type)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (InventoryEntry item in inventory)
		{
			if (item.inventoryType == type)
			{
				hashSet.Add(item.position);
			}
		}
		int i = 1;
		for (int num = SlotLimit(actor, type); i <= num; i++)
		{
			if (!hashSet.Contains(i))
			{
				return i;
			}
		}
		return -1;
	}

	public bool Receive(BotActor actor, List<InventoryEntry> inventory, InventoryEntry offered)
	{
		int id = offered.itemId;
		if (Unique(id) && !MonsterCard(id))
		{
			foreach (InventoryEntry item in inventory)
			{
				if (item.itemId == id)
				{
					return false;
				}
			}
		}
		int num = (short)offered.quantity;
		int type = offered.inventoryType;
		if (type == 1)
		{
			if (num != 1)
			{
				return false;
			}
			int num2 = FreeSlot(actor, inventory, type);
			if (num2 < 0)
			{
				return false;
			}
			InventoryEntry inventoryEntry = offered.Copy();
			inventoryEntry.itemRowId = 0L;
			inventoryEntry.position = num2;
			inventory.Add(inventoryEntry);
			return true;
		}
		if (Rechargeable(id) || offered.petId != -1)
		{
			int num3 = FreeSlot(actor, inventory, type);
			if (num3 < 0)
			{
				return false;
			}
			InventoryEntry inventoryEntry2 = offered.Copy();
			inventoryEntry2.itemRowId = 0L;
			inventoryEntry2.position = num3;
			inventoryEntry2.owner = "";
			inventoryEntry2.giftFrom = "";
			inventory.Add(inventoryEntry2);
			return true;
		}
		int num4 = SlotMax(id, actor);
		if (num4 <= 0 && num > 0)
		{
			return false;
		}
		List<InventoryEntry> list = inventory.FindAll((InventoryEntry i) => i.inventoryType == type && i.itemId == id && InventoryDao.IsBagPosition(i.position));
		list.Sort((InventoryEntry a, InventoryEntry b) => a.position.CompareTo(b.position));
		foreach (InventoryEntry item2 in list)
		{
			if (num <= 0)
			{
				break;
			}
			if (item2.quantity < num4 && item2.flag == offered.flag && !(item2.owner != offered.owner))
			{
				int num5 = Math.Min(num, num4 - item2.quantity);
				item2.quantity += num5;
				num -= num5;
			}
		}
		while (num > 0)
		{
			int num6 = FreeSlot(actor, inventory, type);
			if (num6 < 0)
			{
				return false;
			}
			InventoryEntry inventoryEntry3 = offered.Copy();
			inventoryEntry3.itemRowId = 0L;
			inventoryEntry3.position = num6;
			inventoryEntry3.quantity = Math.Min(num, num4);
			inventoryEntry3.giftFrom = "";
			num -= inventoryEntry3.quantity;
			inventory.Add(inventoryEntry3);
		}
		return true;
	}
}
