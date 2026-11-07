using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Decorator;
using Server.Bot.Trade;
using Server.Field;

public static class NativeInventoryService
{
	private static readonly Dictionary<(DbStore, int), long> worldQuestUse = new Dictionary<(DbStore, int), long>();

	private static readonly Random worldQuestRandom = new Random();

	public static InventoryChangeResponse Handle(DbStore db, InventoryChangeRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		bool flag = r.readPetExclusions || r.writePetExclusions || r.getPet || r.petTick || r.evolvePetId > 0 || r.petTameness != 0;
		if (charRecord == null || (r.itemId <= 0 && r.dropMeso <= 0 && r.checkItems == null && !flag && !r.sortItems && !r.mergeItems && r.moveFrom <= 0))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "角色或道具不存在"
			};
		}
		InventoryDao.ExpireItems(db, r.characterId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		if (r.sortItems)
		{
			return NativeInventorySort.Sort(db, charRecord, r.sortInventoryType);
		}
		if (r.mergeItems)
		{
			return NativeInventorySort.Merge(db, charRecord, r.sortInventoryType);
		}
		if (r.moveFrom > 0 && r.moveTo > 0)
		{
			return NativeInventorySort.MoveItem(db, charRecord, r.moveInventoryType, r.moveFrom, r.moveTo);
		}
		if (r.extraStorage != ExtraStorageOperation.None)
		{
			return NativeExtraStorage.Handle(db, charRecord, r);
		}
		if (r.readPetExclusions || r.writePetExclusions)
		{
			return NativePetItems.Exclusions(db, r);
		}
		if (r.checkItems != null)
		{
			return new InventoryChangeResponse
			{
				ok = CanReceive(db, charRecord, r)
			};
		}
		if (r.dropToField)
		{
			return DropToField(db, charRecord, r);
		}
		if (r.dropMeso > 0)
		{
			return DropMeso(db, charRecord, r);
		}
		if (r.removeAll)
		{
			foreach (DbRow item in InventoryDao.ItemsByCharacter(db, r.characterId))
			{
				if (DbStore.Int(item, "itemid") == r.itemId && NativePotentialService.IsReserved(db, DbStore.Long(item, "inventoryitemid", 0L)))
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "请先选择方块候选结果"
					};
				}
			}
			InventoryDao.ConsumeItem(db, r.characterId, r.itemId, InventoryDao.CountOf(db, r.characterId, r.itemId));
			if (r.itemId / 1000000 == 1)
			{
				DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "itemid") == r.itemId && DbStore.Int(x, "inventorytype") == 1);
				if (dbRow != null)
				{
					long rowId = DbStore.Long(dbRow, "inventoryitemid", 0L);
					db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == rowId);
					db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == rowId);
				}
			}
			db.Save();
			return new InventoryChangeResponse
			{
				inventory = CharService.Load(db, r.characterId).inventory
			};
		}
		if (r.worldQuestDraw && r.itemId == 2430680)
		{
			long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			(DbStore, int) key = (db, r.characterId);
			if (worldQuestUse.TryGetValue(key, out var value) && num - value < 1000)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "道具使用冷却中"
				};
			}
			DbRow dbRow2 = InventoryDao.ItemsByCharacter(db, r.characterId).Find((DbRow row) => DbStore.Int(row, "itemid") == r.itemId && DbStore.Int(row, "inventorytype") == 2 && DbStore.Int(row, "position") > 0 && (r.sourcePosition == 0 || DbStore.Int(row, "position") == r.sourcePosition) && DbStore.Int(row, "quantity") > 0);
			if (dbRow2 == null)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "没有持有这件物品"
				};
			}
			int num2 = worldQuestRandom.Next(105);
			int num3 = ((num2 < 100) ? (2430681 + num2 / 5) : (2430701 + num2 - 100));
			worldQuestUse[key] = num;
			if (!CanReceive(db, charRecord, new InventoryChangeRequest
			{
				checkItems = new int[1] { num3 },
				checkQuantities = new int[1] { 1 }
			}))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "背包空间不足，无法获得物品！"
				};
			}
			AddItems(db, charRecord, num3, 1, -1L);
			int num4 = DbStore.Int(dbRow2, "quantity") - 1;
			if (num4 > 0)
			{
				dbRow2["quantity"] = (long)num4;
			}
			else
			{
				long rowId2 = DbStore.Long(dbRow2, "inventoryitemid", 0L);
				db.Table("inventoryitems").Delete((DbRow row) => DbStore.Long(row, "inventoryitemid", 0L) == rowId2);
			}
			db.Save();
			return new InventoryChangeResponse
			{
				inventory = CharService.Load(db, r.characterId).inventory,
				message = "获得了任务物品：" + BotItemCatalog.Shared.Name(num3)
			};
		}
		if (r.petTameness != 0)
		{
			return NativePetItems.GainTameness(db, r.characterId, r.petTameness, r.petLeaderOnly);
		}
		if (r.createPet)
		{
			if (r.itemId / 1000 != 5000 || BotItemCatalog.Shared.Info(r.itemId) == null)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "宠物资源不存在"
				};
			}
			int createdPetId = (int)db.Table("pets").Insert(new DbRow
			{
				["name"] = BotItemCatalog.Shared.Name(r.itemId),
				["level"] = 1L,
				["closeness"] = 0L,
				["fullness"] = 100L,
				["summoned"] = 0L,
				["flag"] = 0L
			});
			db.Save();
			return new InventoryChangeResponse
			{
				createdPetId = createdPetId
			};
		}
		if (r.getPet)
		{
			return new InventoryChangeResponse
			{
				pet = NativePetItems.GetPet(db, r.characterId, r.petSlot)
			};
		}
		if (r.evolvePetId > 0)
		{
			return NativePetItems.Evolve(db, r);
		}
		if (r.petTick)
		{
			return NativePetItems.Tick(db, r.characterId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		}
		if (r.togglePet && r.itemId / 1000 == 5000)
		{
			return NativePetItems.Toggle(db, r);
		}
		if (r.quantity == 0 && r.grantItemId == 0 && (r.itemId / 10000 == 212 || r.itemId / 10000 == 524))
		{
			return NativePetItems.Feed(db, r);
		}
		DbTable dbTable = db.Table("inventoryitems");
		if (r.bankExp > 0)
		{
			if (charRecord.level > r.maxBankLevel || (long)charRecord.gachaexp + (long)r.bankExp > int.MaxValue || InventoryDao.CountOf(db, r.characterId, r.itemId) < 1)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "当前等级或暂存经验不符合使用条件"
				};
			}
			charRecord.gachaexp += r.bankExp;
			CharactersDao.Save(db, charRecord);
			InventoryDao.ConsumeItem(db, r.characterId, r.itemId, 1);
			db.Save();
			return new InventoryChangeResponse
			{
				inventory = CharService.Load(db, r.characterId).inventory,
				bankedExp = charRecord.gachaexp
			};
		}
		if (r.breakthrough)
		{
			return NativeDamageCapService.Use(db, charRecord, r.itemId);
		}
		if (r.grantItemId > 0)
		{
			if (r.grantQuantity <= 0 || InventoryDao.CountOf(db, r.characterId, r.itemId) < 1)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "没有持有这件物品"
				};
			}
			_ = r.grantItemId / 1000000;
			InventoryChangeRequest inventoryChangeRequest = new InventoryChangeRequest();
			inventoryChangeRequest.checkItems = new int[1] { r.grantItemId };
			inventoryChangeRequest.checkQuantities = new int[1] { r.grantQuantity };
			InventoryChangeRequest r2 = inventoryChangeRequest;
			if (!CanReceive(db, charRecord, r2))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "背包空间不足，无法获取"
				};
			}
			long expiration;
			if (r.grantDurationMs >= 0)
			{
				expiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + r.grantDurationMs;
			}
			else
			{
				expiration = ((r.grantMinutes < 0) ? (-1) : (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)r.grantMinutes * 60000L));
			}
			AddItems(db, charRecord, r.grantItemId, r.grantQuantity, expiration);
			if (r.consumeGrantSource)
			{
				InventoryDao.ConsumeItem(db, r.characterId, r.itemId, 1);
			}
		}
		else if (r.equip)
		{
			int slot = EquipmentSlotOf(r.itemId);
			if (NativePetItems.IsEquipment(r.itemId))
			{
				slot = NativePetItems.EquipmentSlot(r.itemId, r.petSlot);
				if (r.quantity != 0)
				{
					InventoryEntry pet = NativePetItems.GetPet(db, r.characterId, r.petSlot);
					if (pet == null || !NativePetItems.EquipmentFits(r.itemId, pet.itemId))
					{
						return new InventoryChangeResponse
						{
							ok = false,
							error = "请先召唤可以穿戴此装备的宠物"
						};
					}
				}
				else
				{
					for (int num5 = 0; num5 < 3; num5++)
					{
						if (NativePetItems.EquipmentSlot(r.itemId, num5) == r.equipSlot)
						{
							slot = r.equipSlot;
						}
					}
				}
			}
			if ((slot == -12 && (r.equipSlot == -12 || r.equipSlot == -13 || r.equipSlot == -15 || r.equipSlot == -16)) || (slot == -112 && (r.equipSlot == -112 || r.equipSlot == -113 || r.equipSlot == -115 || r.equipSlot == -116)))
			{
				slot = r.equipSlot;
			}
			if (NativeAndroidService.IsApSlot(r.equipSlot))
			{
				if (!NativeAndroidService.ApAccepts(r.equipSlot, slot))
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "这件装备不能放进机器人的这个换装槽"
					};
				}
				slot = r.equipSlot;
			}
			if (slot == 0 || slot != r.equipSlot)
			{
				if (r.quantity != 0 || r.equipSlot >= 0 || dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "itemid") == r.itemId && DbStore.Int(x, "position") == r.equipSlot && DbStore.Int(x, "inventorytype") == 1) == null)
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "无效装备槽位"
					};
				}
				slot = r.equipSlot;
			}
			DbRow dbRow3 = dbTable.Find((DbRow x) =>
			{
				if (DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "itemid") == r.itemId)
				{
					if (r.quantity != 0)
					{
						if (DbStore.Int(x, "inventorytype") == 1 && InventoryDao.IsBagPosition(DbStore.Int(x, "position")))
						{
							if (r.sourcePosition != 0)
							{
								return DbStore.Int(x, "position") == r.sourcePosition;
							}
							return true;
						}
						return false;
					}
					if (DbStore.Int(x, "inventorytype") == 1)
					{
						return DbStore.Int(x, "position") == slot;
					}
					return false;
				}
				return false;
			});
			if (dbRow3 == null)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "没有持有这件装备"
				};
			}
			if (NativePotentialService.IsReserved(db, DbStore.Long(dbRow3, "inventoryitemid", 0L)))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "请先选择方块候选结果"
				};
			}
			if (r.quantity != 0 && DbStore.Int(InventoryDao.EquipmentOf(db, DbStore.Long(dbRow3, "inventoryitemid", 0L)), "starForceVestige") != 0)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "装备痕迹需要先复原，无法穿戴"
				};
			}
			int num6 = DbStore.Int(dbRow3, "position");
			if (r.quantity == 0)
			{
				int free = ((r.moveTo == 0) ? Free(db, charRecord, 1) : r.moveTo);
				if (r.moveTo != 0 && (free < 1 || free > charRecord.equipslots || dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "inventorytype") == 1 && DbStore.Int(x, "position") == free) != null))
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "请放到装备背包的空格"
					};
				}
				if (free == 0)
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "装备栏已满"
					};
				}
				dbRow3["position"] = (long)free;
				dbRow3["inventorytype"] = 1L;
			}
			else
			{
				int conflictSlot;
				if (slot == -6)
				{
					conflictSlot = -5;
				}
				else if (slot == -5 && r.itemId / 10000 == 105)
				{
					conflictSlot = -6;
				}
				else
				{
					conflictSlot = ((slot == -10) ? (-11) : ((slot == -11 && TwoHanded(r.itemId)) ? (-10) : 0));
				}
				DbRow dbRow4 = ((conflictSlot == 0) ? null : dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "position") == conflictSlot && DbStore.Int(x, "inventorytype") == 1));
				DbRow dbRow5 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "position") == slot && DbStore.Int(x, "inventorytype") == 1);
				if ((dbRow4 != null && NativePotentialService.IsReserved(db, DbStore.Long(dbRow4, "inventoryitemid", 0L))) || (dbRow5 != null && NativePotentialService.IsReserved(db, DbStore.Long(dbRow5, "inventoryitemid", 0L))))
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "请先选择方块候选结果"
					};
				}
				if (dbRow4 != null && (slot != -6 || DbStore.Int(dbRow4, "itemid") / 10000 == 105) && (slot != -10 || TwoHanded(DbStore.Int(dbRow4, "itemid"))))
				{
					int num7 = Free(db, charRecord, 1);
					if (num7 == 0)
					{
						return new InventoryChangeResponse
						{
							ok = false,
							error = "装备栏已满"
						};
					}
					dbRow4["position"] = (long)num7;
					dbRow4["inventorytype"] = 1L;
				}
				DbRow dbRow6 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "position") == slot && DbStore.Int(x, "inventorytype") == 1);
				if (dbRow6 != null)
				{
					dbRow6["position"] = (long)num6;
					dbRow6["inventorytype"] = 1L;
				}
				dbRow3["position"] = (long)slot;
				dbRow3["inventorytype"] = 1L;
			}
		}
		else if (r.projectile)
		{
			bool flag2 = r.itemId / 10000 == 207 || r.itemId / 10000 == 233;
			bool flag3 = r.itemId / 1000 == 2060 || r.itemId / 1000 == 2061;
			DbRow dbRow7 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "itemid") == r.itemId && DbStore.Int(x, "inventorytype") == 2 && DbStore.Int(x, "position") == r.sourcePosition && InventoryDao.IsBagPosition(r.sourcePosition));
			if ((!flag2 && !flag3) || dbRow7 == null || r.quantity < -32768 || r.quantity > 32767)
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "弹药槽位或数量无效"
				};
			}
			long num8 = Math.Max(0, (int)(short)(DbStore.Int(dbRow7, "quantity") + r.quantity));
			if ((num8 > 0) | flag2)
			{
				dbRow7["quantity"] = num8;
			}
			else
			{
				long id = DbStore.Long(dbRow7, "inventoryitemid", 0L);
				dbTable.Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
			}
		}
		else if (r.quantity > 0)
		{
			InventoryChangeRequest inventoryChangeRequest = new InventoryChangeRequest();
			inventoryChangeRequest.checkItems = new int[1] { r.itemId };
			inventoryChangeRequest.checkQuantities = new int[1] { r.quantity };
			InventoryChangeRequest r3 = inventoryChangeRequest;
			if (!CanReceive(db, charRecord, r3))
			{
				return new InventoryChangeResponse
				{
					ok = false,
					error = "背包空间不足，无法获取"
				};
			}
			AddItems(db, charRecord, r.itemId, r.quantity, r.itemExpiration, (r.petId > 0) ? r.petId : (-1));
		}
		else if (r.quantity < 0)
		{
			if (r.sourcePosition != 0)
			{
				DbRow dbRow8 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == r.characterId && DbStore.Int(x, "itemid") == r.itemId && DbStore.Int(x, "inventorytype") == r.itemId / 1000000 && DbStore.Int(x, "position") == r.sourcePosition && InventoryDao.IsBagPosition(r.sourcePosition));
				if (dbRow8 == null || r.quantity == int.MinValue || DbStore.Int(dbRow8, "quantity") < -r.quantity)
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "指定槽位数量不足"
					};
				}
				int num9 = DbStore.Int(dbRow8, "quantity") + r.quantity;
				if (num9 > 0)
				{
					dbRow8["quantity"] = (long)num9;
				}
				else
				{
					long id2 = DbStore.Long(dbRow8, "inventoryitemid", 0L);
					dbTable.Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id2);
					db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id2);
				}
			}
			else
			{
				if (InventoryDao.CountOf(db, r.characterId, r.itemId) < -r.quantity)
				{
					return new InventoryChangeResponse
					{
						ok = false,
						error = "持有数量不足"
					};
				}
				InventoryDao.ConsumeItem(db, r.characterId, r.itemId, -r.quantity);
			}
		}
		db.Save();
		return new InventoryChangeResponse
		{
			inventory = CharService.Load(db, r.characterId).inventory
		};
	}

	internal static void AddItems(DbStore db, CharRecord c, int itemId, int count, long expiration, int petId = -1)
	{
		int num = itemId / 1000000;
		DbTable dbTable = db.Table("inventoryitems");
		BotActor actor = new BotActor
		{
			id = (int)c.id,
			job = c.job,
			character = CharService.Load(db, (int)c.id)
		};
		TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
		bool flag = TradeItemRules.Rechargeable(itemId);
		bool flag2 = itemId / 1000 == 5000;
		int num2 = count;
		int num3 = (((num == 1) | flag2) ? 1 : (flag ? count : tradeItemRules.SlotMax(itemId, actor)));
		if (num != 1 && !flag2 && !flag)
		{
			foreach (DbRow item in InventoryDao.ItemsByCharacter(db, (int)c.id))
			{
				if (num2 <= 0)
				{
					break;
				}
				if (DbStore.Int(item, "itemid") == itemId && DbStore.Int(item, "position") > 0 && DbStore.Int(item, "flag") == 0)
				{
					int num4 = Math.Min(num2, Math.Max(0, num3 - DbStore.Int(item, "quantity")));
					if (num4 != 0)
					{
						item["quantity"] = (long)(DbStore.Int(item, "quantity") + num4);
						item["expiration"] = expiration;
						num2 -= num4;
					}
				}
			}
		}
		while (num2 > 0)
		{
			int num5 = Free(db, c, num);
			int num6 = Math.Min(num2, num3);
			if (num5 == 0 || num6 <= 0)
			{
				throw new InvalidOperationException("Inventory capacity preflight mismatch");
			}
			long id = InventoryDao.InsertItem(db, (int)c.id, itemId, num, num5, num6, flag2 ? petId : (-1));
			if (num == 1)
			{
				InventoryDao.InsertEquipment(db, id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId), "tuc"));
				if (SymbolCatalog.IsSymbol(itemId))
				{
					NativeSymbolService.Normalize(InventoryDao.EquipmentOf(db, id), itemId, c.job);
				}
			}
			dbTable.Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id)["expiration"] = expiration;
			num2 -= num6;
		}
	}

	internal static bool CanReceive(DbStore db, CharRecord c, InventoryChangeRequest r)
	{
		FullCharacter fullCharacter = CharService.Load(db, (int)c.id);
		BotActor actor = new BotActor
		{
			id = (int)c.id,
			job = c.job,
			character = fullCharacter
		};
		List<InventoryEntry> list = new List<InventoryEntry>();
		foreach (InventoryEntry item in fullCharacter.inventory)
		{
			list.Add(item.Copy());
		}
		if (r.removeCheckItem > 0 && r.removeCheckQuantity > 0)
		{
			int num = r.removeCheckQuantity;
			foreach (InventoryEntry item2 in list)
			{
				if (InventoryDao.IsBagPosition(item2.position) && item2.itemId == r.removeCheckItem)
				{
					int num2 = Math.Min(num, item2.quantity);
					item2.quantity -= num2;
					num -= num2;
				}
			}
			list.RemoveAll((InventoryEntry i) => i.quantity <= 0);
		}
		TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
		for (int num3 = 0; num3 < r.checkItems.Length; num3++)
		{
			int num4 = r.checkItems[num3];
			int num5 = ((r.checkQuantities == null || num3 >= r.checkQuantities.Length) ? 1 : r.checkQuantities[num3]);
			if (num4 <= 0 || num5 <= 0 || num5 > 32767 || BotItemCatalog.Shared.Info(num4) == null)
			{
				return false;
			}
			if (num4 / 1000000 == 1 || num4 / 1000 == 5000)
			{
				for (int num6 = 0; num6 < num5; num6++)
				{
					InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanItem(num4);
					if (num4 / 1000 == 5000)
					{
						inventoryEntry.petId = 0;
					}
					if (!tradeItemRules.Receive(actor, list, inventoryEntry))
					{
						return false;
					}
				}
			}
			else if (!tradeItemRules.Receive(actor, list, BotItemCatalog.Shared.CleanItem(num4, num5)))
			{
				return false;
			}
		}
		return true;
	}

	public static int EquipmentSlotOf(int itemId)
	{
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(itemId);
		if (SymbolCatalog.IsSymbol(itemId))
		{
			return SymbolCatalog.SlotOf(itemId);
		}
		string text = ((dictionary != null && dictionary.TryGetValue("islot", out var value)) ? (value as string) : null);
		int num = text switch
		{
			"Cp" => -1, 
			"HrCp" => -1, 
			"Af" => -2, 
			"Ay" => -3, 
			"Ae" => -4, 
			"Ma" => -5, 
			"MaPn" => -5, 
			"Pn" => -6, 
			"So" => -7, 
			"GlGw" => -8, 
			"Gv" => -8, 
			"Sr" => -9, 
			"Si" => -10, 
			"Wp" => -11, 
			"WpSi" => -11, 
			"WpSp" => -11, 
			"Ri" => -12, 
			"Pe" => -17, 
			"Tm" => -18, 
			"Sd" => -19, 
			"Sh" => -20, 
			"An" => -32, 
			"Sw" => -51, 
			"Po" => -53, 
			"Ht" => -54, 
			"Ba" => -55, 
			"Em" => -56, 
			"Me" => -49, 
			"Be" => -50, 
			_ => -BotCustomization.EquipSlotType(itemId), 
		};
		if (text == "An" || text == "Ht")
		{
			return num;
		}
		if (num == 0 || BotItemCatalog.Int(dictionary, "cash") == 0)
		{
			return num;
		}
		return num - 100;
	}

	private static InventoryChangeResponse DropToField(DbStore db, CharRecord character, InventoryChangeRequest r)
	{
		int num = (int)character.id;
		FullCharacter fullCharacter = CharService.Load(db, num);
		if (fullCharacter == null || fullCharacter.inventory == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "角色或道具不存在"
			};
		}
		InventoryEntry source = null;
		foreach (InventoryEntry item in fullCharacter.inventory)
		{
			if (item.itemId == r.itemId && InventoryDao.IsBagPosition(item.position) && (r.sourcePosition == 0 || item.position == r.sourcePosition))
			{
				source = item;
				break;
			}
		}
		if (source == null)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "背包里没有这件物品"
			};
		}
		if (NativeSoulWeaponWz2Service.IsBound(db, num, source.itemRowId))
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "请先停用灵魂武器再丢弃"
			};
		}
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(r.itemId);
		if (dictionary != null && BotItemCatalog.Int(dictionary, "cash") == 1)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "这种道具不能扔掉。"
			};
		}
		bool flag = dictionary != null && BotItemCatalog.Int(dictionary, "quest") == 1;
		int quantity = source.quantity;
		int num2 = r.dropQuantity;
		if (num2 <= 0 || num2 > quantity)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "数量不正确"
			};
		}
		bool flag2 = source.itemId / 10000 == 207;
		if (num2 < quantity && !flag2)
		{
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == source.itemRowId);
			if (dbRow != null)
			{
				dbRow["quantity"] = (long)(quantity - num2);
			}
		}
		else
		{
			num2 = quantity;
			db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == source.itemRowId);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == source.itemRowId);
		}
		db.Save();
		if (!flag)
		{
			InventoryEntry inventoryEntry = source.Copy();
			inventoryEntry.quantity = num2;
			MapItemService.SpawnItemDrop(r.dropMapId, num, num, (MapItemService.PartyOf != null) ? MapItemService.PartyOf(num) : (-1), inventoryEntry, r.dropX, r.dropY, ffaDrop: true, playerDrop: true);
		}
		return new InventoryChangeResponse
		{
			inventory = CharService.Load(db, num).inventory
		};
	}

	private static InventoryChangeResponse DropMeso(DbStore db, CharRecord character, InventoryChangeRequest r)
	{
		long dropMeso = r.dropMeso;
		if (character.hp <= 0)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "死亡状态中不能执行该操作"
			};
		}
		int num = GameConfigDao.GetInt(db, "minimum_gm_level_to_drop", 4);
		if (character.gm > 0 && character.gm < num)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "管理账号不能扔掉金币。"
			};
		}
		if (dropMeso <= 9 || dropMeso > 50000 || dropMeso > character.meso)
		{
			return new InventoryChangeResponse
			{
				ok = false,
				error = "金币数量不正确"
			};
		}
		character.meso -= dropMeso;
		CharactersDao.Save(db, character);
		db.Save();
		MapItemService.SpawnMesoDrop(r.dropMapId, (int)dropMeso, r.dropX, r.dropY, (int)character.id, (int)character.id, (MapItemService.PartyOf != null) ? MapItemService.PartyOf((int)character.id) : (-1), playerDrop: true, 2);
		return new InventoryChangeResponse
		{
			meso = character.meso
		};
	}

	private static bool TwoHanded(int id)
	{
		if (id / 10000 >= 140)
		{
			return id / 10000 <= 149;
		}
		return false;
	}

	internal static int Free(DbStore db, CharRecord c, int type)
	{
		int num = type switch
		{
			4 => c.etcslots, 
			3 => c.setupslots, 
			2 => c.useslots, 
			1 => c.equipslots, 
			_ => 96, 
		};
		HashSet<int> hashSet = new HashSet<int>();
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, (int)c.id))
		{
			if (DbStore.Int(item, "inventorytype") == type)
			{
				hashSet.Add(DbStore.Int(item, "position"));
			}
		}
		for (int i = 1; i <= num; i++)
		{
			if (!hashSet.Contains(i))
			{
				return i;
			}
		}
		return 0;
	}
}
