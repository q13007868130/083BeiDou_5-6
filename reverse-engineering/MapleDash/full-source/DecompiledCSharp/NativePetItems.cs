using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativePetItems
{
	private sealed class HungerClock
	{
		public long updated;

		public readonly Dictionary<(int, int), int> counts = new Dictionary<(int, int), int>();

		public readonly Dictionary<int, long> processed = new Dictionary<int, long>();
	}

	private static Dictionary<string, object> rules;

	private static readonly Dictionary<(DbStore, int), long> lastFood = new Dictionary<(DbStore, int), long>();

	private static readonly Dictionary<(DbStore, int), List<int>> petOrder = new Dictionary<(DbStore, int), List<int>>();

	private static readonly Dictionary<DbStore, HungerClock> hungerClocks = new Dictionary<DbStore, HungerClock>();

	private static List<InventoryEntry> ActivePets(DbStore db, int cid)
	{
		List<InventoryEntry> active = new List<InventoryEntry>();
		foreach (InventoryEntry item in CharService.Load(db, cid).inventory)
		{
			if (item.petData != null && Convert.ToInt32(item.petData["summoned"]) == 1)
			{
				active.Add(item);
			}
		}
		(DbStore, int) key = (db, cid);
		if (!petOrder.TryGetValue(key, out var order))
		{
			petOrder[key] = (order = new List<int>());
		}
		order.RemoveAll((int id) => !active.Exists((InventoryEntry item) => item.petId == id));
		foreach (InventoryEntry item2 in active)
		{
			if (!order.Contains(item2.petId))
			{
				order.Add(item2.petId);
			}
		}
		active.Sort((InventoryEntry a, InventoryEntry b) => order.IndexOf(a.petId).CompareTo(order.IndexOf(b.petId)));
		return active;
	}

	public static InventoryEntry GetPet(DbStore db, int cid, int slot)
	{
		List<InventoryEntry> list = ActivePets(db, cid);
		if (slot < 0 || slot >= list.Count)
		{
			return null;
		}
		return list[slot];
	}

	public static InventoryChangeResponse Exclusions(DbStore db, InventoryChangeRequest request)
	{
		if (!ActivePets(db, request.characterId).Exists((InventoryEntry p) => p.petId == request.petId))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "宠物未召唤或不属于当前角色"
			};
		}
		DbTable dbTable = db.Table("petignores");
		if (request.writePetExclusions)
		{
			if (request.petExclusions == null || request.petExclusions.Length > 255 || Array.Exists(request.petExclusions, (int id) => id < 0))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "无效宠物拾取过滤列表"
				};
			}
			HashSet<int> wanted = new HashSet<int>(request.petExclusions);
			dbTable.Delete((DbRow row) => DbStore.Int(row, "petid") == request.petId && !wanted.Contains(DbStore.Int(row, "itemid")));
			foreach (DbRow item in dbTable.Select((DbRow row) => DbStore.Int(row, "petid") == request.petId))
			{
				wanted.Remove(DbStore.Int(item, "itemid"));
			}
			foreach (int item2 in wanted)
			{
				dbTable.Insert(new DbRow
				{
					["petid"] = (long)request.petId,
					["itemid"] = (long)item2
				});
			}
			db.Save();
		}
		InventoryChangeResponse inventoryChangeResponse = new InventoryChangeResponse
		{
			petExclusions = new List<int>()
		};
		foreach (DbRow item3 in dbTable.Select((DbRow row) => DbStore.Int(row, "petid") == request.petId))
		{
			inventoryChangeResponse.petExclusions.Add(DbStore.Int(item3, "itemid"));
		}
		return inventoryChangeResponse;
	}

	public static bool ExcludesPickup(DbStore db, int cid, int itemId)
	{
		if (!CharService.Load(db, cid).inventory.Exists((InventoryEntry item) => item.inventoryType == -1 && item.itemId == 1812007))
		{
			return false;
		}
		List<InventoryEntry> active = ActivePets(db, cid);
		return db.Table("petignores").Find((DbRow row) => DbStore.Int(row, "itemid") == ((itemId == 0) ? int.MaxValue : itemId) && active.Exists((InventoryEntry p) => p.petId == DbStore.Int(row, "petid"))) != null;
	}

	public static bool IsEquipment(int id)
	{
		if (id / 10000 >= 180)
		{
			return id / 10000 <= 183;
		}
		return false;
	}

	public static bool IsPetSlot(int slot)
	{
		switch (slot)
		{
		case -48:
		case -47:
		case -46:
		case -45:
		case -44:
		case -43:
		case -42:
		case -41:
		case -40:
		case -39:
		case -38:
		case -37:
		case -36:
		case -35:
		case -34:
		case -33:
		case -32:
		case -31:
		case -30:
		case -29:
		case -28:
		case -27:
		case -26:
		case -25:
		case -24:
		case -23:
		case -22:
		case -21:
			return slot != -49;
		default:
			return false;
		case -14:
			return true;
		}
	}

	public static int EquipmentSlot(int id, int petSlot)
	{
		if (petSlot < 0 || petSlot > 2)
		{
			return 0;
		}
		if (id / 10000 == 180)
		{
			return -(new int[3] { 14, 30, 38 })[petSlot];
		}
		switch (id)
		{
		case 1822000:
			return -24;
		case 1832000:
			return -25;
		default:
		{
			int num = id - 1812000;
			if (num < 0 || num > 7)
			{
				return 0;
			}
			return -(new int[8, 3]
			{
				{ 23, 34, 42 },
				{ 22, 33, 41 },
				{ 24, 24, 24 },
				{ 25, 25, 25 },
				{ 26, 35, 43 },
				{ 27, 36, 44 },
				{ 28, 37, 45 },
				{ 46, 47, 48 }
			})[num, petSlot];
		}
		}
	}

	public static bool EquipmentFits(int id, int petId)
	{
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		if (!((Dictionary<string, object>)rules["equips"]).TryGetValue(id.ToString(), out var value))
		{
			return false;
		}
		List<object> list = (List<object>)value;
		if (id / 10000 == 180)
		{
			return list.Exists((object x) => Convert.ToInt32(x) == petId);
		}
		return true;
	}

	public static InventoryChangeResponse GainTameness(DbStore db, int cid, int increase, bool leaderOnly = false)
	{
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		List<object> list = (List<object>)rules["tameness"];
		List<InventoryEntry> list2 = ActivePets(db, cid);
		if (leaderOnly && list2.Count > 1)
		{
			list2.RemoveRange(1, list2.Count - 1);
		}
		foreach (InventoryEntry pet in list2)
		{
			DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == pet.petId);
			int num = DbStore.Int(dbRow, "closeness");
			int num2 = DbStore.Int(dbRow, "level");
			if (increase > 0 && num < 30000)
			{
				for (num = (int)Math.Min(30000L, (long)num + (long)increase); num2 < 30 && num >= Convert.ToInt32(list[num2]); num2++)
				{
				}
				dbRow["closeness"] = (long)num;
				dbRow["level"] = (long)num2;
			}
		}
		db.Save();
		return Snapshot(db, cid);
	}

	public static InventoryChangeResponse Evolve(DbStore db, InventoryChangeRequest request)
	{
		int cid = request.characterId;
		int evolvePetId = request.evolvePetId;
		InventoryEntry before = GetPet(db, cid, request.petSlot);
		if (before == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "Pet could not be evolved..."
			};
		}
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		if (!((Dictionary<string, object>)rules["pets"]).ContainsKey(evolvePetId.ToString()))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "无效宠物"
			};
		}
		InventoryChangeResponse inventoryChangeResponse = NativeInventoryService.Handle(db, new InventoryChangeRequest
		{
			characterId = cid,
			checkItems = new int[1] { evolvePetId },
			checkQuantities = new int[1] { 1 }
		});
		if (!inventoryChangeResponse.ok)
		{
			return inventoryChangeResponse;
		}
		DbRow r = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "petid") == before.petId);
		int position = 1;
		for (List<InventoryEntry> inventory = CharService.Load(db, cid).inventory; inventory.Exists((InventoryEntry x) => x.inventoryType == 5 && x.position == position); position++)
		{
		}
		long id = InventoryDao.InsertItem(db, cid, evolvePetId, 5, position, 1);
		DbRow added = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
		added["expiration"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 7776000000L;
		DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == DbStore.Int(added, "petid"));
		string[] array = new string[3] { "level", "closeness", "fullness" };
		foreach (string key in array)
		{
			dbRow[key] = before.petData[key];
		}
		string text = before.petData["name"].ToString();
		dbRow["name"] = ((text == BotItemCatalog.Shared.Name(before.itemId)) ? BotItemCatalog.Shared.Name(evolvePetId) : text);
		dbRow["summoned"] = 1L;
		ActivePets(db, cid);
		long oldId = DbStore.Long(r, "inventoryitemid", 0L);
		db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == oldId);
		db.Save();
		return Snapshot(db, cid);
	}

	public static void EndSession(DbStore db, int cid)
	{
		lastFood.Remove((db, cid));
		petOrder.Remove((db, cid));
		if (!hungerClocks.TryGetValue(db, out var value))
		{
			return;
		}
		value.processed.Remove(cid);
		List<(int, int)> list = new List<(int, int)>();
		foreach (var key in value.counts.Keys)
		{
			if (key.Item1 == cid)
			{
				list.Add(key);
			}
		}
		foreach (var item in list)
		{
			value.counts.Remove(item);
		}
		if (value.counts.Count == 0)
		{
			hungerClocks.Remove(db);
		}
	}

	private static HungerClock Clock(DbStore db, long now)
	{
		if (!hungerClocks.TryGetValue(db, out var value))
		{
			Dictionary<DbStore, HungerClock> dictionary = hungerClocks;
			HungerClock hungerClock = new HungerClock
			{
				updated = now
			};
			value = hungerClock;
			dictionary[db] = hungerClock;
		}
		return value;
	}

	public static InventoryChangeResponse Tick(DbStore db, int cid, long now)
	{
		CharRecord charRecord = CharactersDao.FindById(db, cid);
		if (charRecord == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		HungerClock hungerClock = Clock(db, now);
		if (!hungerClock.processed.ContainsKey(cid))
		{
			hungerClock.processed[cid] = hungerClock.updated;
		}
		int num = GameConfigDao.GetInt(db, "pet_exhaust_count", 3);
		bool flag = GameConfigDao.GetBool(db, "pets_never_hungry") || (charRecord.gm > 0 && GameConfigDao.GetBool(db, "gm_pets_never_hungry", fallback: true));
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item2 in ActivePets(db, cid))
		{
			(int, int) tuple = (cid, item2.petId);
			hashSet.Add(tuple);
			list.Add(item2);
			if (!hungerClock.counts.ContainsKey(tuple) && !flag)
			{
				hungerClock.counts[tuple] = num - ((now - hungerClock.updated <= 55000) ? 1 : 2);
			}
		}
		List<(int, int)> list2 = new List<(int, int)>();
		foreach (var key2 in hungerClock.counts.Keys)
		{
			if (key2.Item1 == cid && (!hashSet.Contains(key2) | flag))
			{
				list2.Add(key2);
			}
		}
		foreach (var item3 in list2)
		{
			hungerClock.counts.Remove(item3);
		}
		if (now - hungerClock.updated >= 60000)
		{
			hungerClock.updated = now;
		}
		if (hungerClock.processed[cid] >= hungerClock.updated)
		{
			return Snapshot(db, cid);
		}
		hungerClock.processed[cid] = hungerClock.updated;
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		Dictionary<string, object> dictionary = (Dictionary<string, object>)rules["pets"];
		bool flag2 = false;
		bool flag3 = false;
		foreach (InventoryEntry item in list)
		{
			(int, int) key = (cid, item.petId);
			if (!hungerClock.counts.TryGetValue(key, out var value))
			{
				continue;
			}
			value++;
			if (value == num)
			{
				value = 0;
				DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == item.petId);
				int num2 = (dictionary.TryGetValue(item.itemId.ToString(), out var value2) ? Convert.ToInt32(((Dictionary<string, object>)value2)["hungry"]) : 0);
				int num3 = DbStore.Int(dbRow, "fullness") - num2;
				if (num3 <= 5)
				{
					num3 = 15;
					dbRow["summoned"] = 0L;
					flag3 = true;
				}
				dbRow["fullness"] = (long)num3;
				flag2 = true;
			}
			hungerClock.counts[key] = value;
		}
		if (flag2)
		{
			db.Save();
		}
		InventoryChangeResponse inventoryChangeResponse = Snapshot(db, cid);
		inventoryChangeResponse.inventoryChanged = flag2;
		if (flag3)
		{
			inventoryChangeResponse.message = "您的宠物饿了！给它一些宠物食物以保持它的健康！";
		}
		return inventoryChangeResponse;
	}

	public static InventoryChangeResponse Toggle(DbStore db, InventoryChangeRequest request)
	{
		int cid = request.characterId;
		int id = request.itemId;
		DbRow held = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "itemid") == id && DbStore.Int(x, "inventorytype") == 5 && DbStore.Int(x, "position") > 0 && (request.sourcePosition == 0 || DbStore.Int(x, "position") == request.sourcePosition));
		DbRow dbRow = ((held == null) ? null : db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == DbStore.Int(held, "petid", -1)));
		if (dbRow == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "宠物不存在"
			};
		}
		if (DbStore.Long(held, "expiration", -1L) < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "请先用生命之水复活宠物"
			};
		}
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		if (id == 5000028 || id == 5000047)
		{
			if (InventoryDao.CountOf(db, cid, id + 1) > 0)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "已经拥有孵化后的宠物"
				};
			}
			int itemId = Convert.ToInt32(((Dictionary<string, object>)((Dictionary<string, object>)rules["pets"])[id.ToString()])["evol1"]);
			long num = DbStore.Long(held, "expiration", -1L);
			long rowId = DbStore.Long(held, "inventoryitemid", 0L);
			int position = DbStore.Int(held, "position");
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == rowId);
			long newId = InventoryDao.InsertItem(db, cid, itemId, 5, position, 1);
			db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == newId)["expiration"] = num;
		}
		else if (DbStore.Int(dbRow, "summoned") == 1)
		{
			dbRow["summoned"] = 0L;
		}
		else
		{
			bool flag = SkillsDao.ByCharacter(db, cid).Exists((DbRow x) => DbStore.Int(x, "skillid") == 8 && DbStore.Int(x, "skilllevel") > 0);
			List<DbRow> list = new List<DbRow>();
			foreach (InventoryEntry item in ActivePets(db, cid))
			{
				list.Add(db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == item.petId));
			}
			if (!flag)
			{
				foreach (DbRow item2 in list)
				{
					item2["summoned"] = 0L;
				}
			}
			else if (list.Count >= 3)
			{
				return Snapshot(db, cid);
			}
			dbRow["summoned"] = 1L;
		}
		db.Save();
		Tick(db, cid, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		return Snapshot(db, cid);
	}

	public static InventoryChangeResponse Feed(DbStore db, InventoryChangeRequest request)
	{
		int id = request.itemId;
		int cid = request.characterId;
		bool cash = id / 10000 == 524;
		if (!cash && id / 10000 != 212)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "无效宠物食品"
			};
		}
		DbRow food = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "itemid") == id && DbStore.Int(x, "inventorytype") == (cash ? 5 : 2) && DbStore.Int(x, "position") > 0 && (request.sourcePosition == 0 || DbStore.Int(x, "position") == request.sourcePosition) && DbStore.Int(x, "quantity") > 0);
		if (food == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "背包里没有这个物品"
			};
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (!cash)
		{
			if (lastFood.TryGetValue((db, cid), out var value) && value + 500 > num)
			{
				return Snapshot(db, cid);
			}
			lastFood[(db, cid)] = num;
		}
		if (rules == null)
		{
			rules = (Dictionary<string, object>)MiniJsonDb.Parse(PlatformRes.LoadText("PetItemRules"));
		}
		List<InventoryEntry> list = ActivePets(db, cid);
		InventoryEntry selected = null;
		int num2 = 0;
		if (cash)
		{
			if (((Dictionary<string, object>)rules["foods"]).TryGetValue(id.ToString(), out var value2))
			{
				Dictionary<string, object> dictionary = (Dictionary<string, object>)value2;
				foreach (InventoryEntry item in list)
				{
					foreach (KeyValuePair<string, object> item2 in dictionary)
					{
						if (int.TryParse(item2.Key, out var _) && Convert.ToInt32(item2.Value) == item.itemId)
						{
							selected = item;
							break;
						}
					}
					if (selected != null)
					{
						break;
					}
				}
				num2 = ((!dictionary.TryGetValue("inc", out var value3)) ? 1 : Convert.ToInt32(value3));
			}
		}
		else if (list.Count > 0)
		{
			selected = list[0];
			foreach (InventoryEntry item3 in list)
			{
				if (Convert.ToInt32(item3.petData["fullness"]) < Convert.ToInt32(selected.petData["fullness"]))
				{
					selected = item3;
				}
			}
			num2 = ((Convert.ToInt32(selected.petData["fullness"]) <= 75) ? 1 : 0);
		}
		if (selected == null)
		{
			if (!cash)
			{
				return Snapshot(db, cid);
			}
			return new InventoryChangeResponse
			{
				ok = false,
				error = "没有可以食用此食品的已召唤宠物"
			};
		}
		DbRow dbRow = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == selected.petId);
		int num3 = DbStore.Int(dbRow, "fullness");
		int num4 = DbStore.Int(dbRow, "closeness");
		int num5 = DbStore.Int(dbRow, "level");
		List<object> list2 = (List<object>)rules["tameness"];
		if ((num3 < 100) | cash)
		{
			num3 = Math.Min(100, num3 + (cash ? 100 : 30));
			if (num2 > 0 && num4 < 30000)
			{
				for (num4 = Math.Min(30000, num4 + num2); num5 < 30 && num4 >= Convert.ToInt32(list2[num5]); num5++)
				{
				}
			}
		}
		else
		{
			num4 = Math.Max(0, num4 - 1);
			if (num5 > 1 && num4 < Convert.ToInt32(list2[num5 - 1]))
			{
				num5--;
			}
		}
		dbRow["fullness"] = (long)num3;
		dbRow["closeness"] = (long)num4;
		dbRow["level"] = (long)num5;
		int num6 = DbStore.Int(food, "quantity");
		if (num6 > 1)
		{
			food["quantity"] = (long)(num6 - 1);
		}
		else
		{
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == DbStore.Long(food, "inventoryitemid", 0L));
		}
		db.Save();
		return Snapshot(db, cid);
	}

	private static InventoryChangeResponse Snapshot(DbStore db, int cid)
	{
		return new InventoryChangeResponse
		{
			inventory = CharService.Load(db, cid).inventory
		};
	}
}
