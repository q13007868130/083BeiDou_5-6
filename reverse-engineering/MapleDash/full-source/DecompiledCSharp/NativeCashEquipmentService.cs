using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeCashEquipmentService
{
	public static Func<int, int> IncubatorPick;

	private static EquipPatchResponse Reject(string message)
	{
		return new EquipPatchResponse
		{
			ok = false,
			error = message
		};
	}

	public static EquipPatchResponse Apply(DbStore db, EquipPatchRequest request)
	{
		if (request == null)
		{
			return Reject("无效现金道具请求");
		}
		if (NativePotentialService.IsReserved(db, request.inventoryItemId))
		{
			return Reject("请先选择方块候选结果");
		}
		DbRow dbRow = InventoryDao.EquipmentOf(db, request.inventoryItemId);
		if (dbRow != null && DbStore.Int(dbRow, "starForceVestige") != 0)
		{
			return Reject("装备痕迹需要先复原");
		}
		CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
		DbRow dbRow2 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
		int cashItemId = request.cashItemId;
		if (charRecord == null || dbRow2 == null || InventoryDao.CountOf(db, request.characterId, cashItemId) < 1)
		{
			return Reject("装备或现金道具不存在");
		}
		int num = DbStore.Int(dbRow2, "position");
		int num2 = DbStore.Int(dbRow2, "itemid");
		int num3 = DbStore.Int(dbRow2, "flag");
		long num4 = DbStore.Long(dbRow2, "expiration", 0L);
		long num5 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		Dictionary<string, object> info = BotItemCatalog.Shared.Info(num2);
		switch (cashItemId)
		{
		case 5060000:
			if (num >= 0)
			{
				return Reject("请将取名道具放到已穿戴的装备上");
			}
			dbRow2["owner"] = charRecord.name;
			break;
		case 5060001:
		case 5061000:
		case 5061001:
		case 5061002:
		case 5061003:
		{
			if (num4 > -1 && (num3 & 1) == 0)
			{
				return Reject("这件时限道具不能封印");
			}
			dbRow2["flag"] = (long)(num3 | 1);
			int num14 = cashItemId switch
			{
				5061003 => 365, 
				5061002 => 90, 
				5061001 => 30, 
				5061000 => 7, 
				_ => 0, 
			};
			if (num14 > 0)
			{
				dbRow2["expiration"] = ((num4 > -1) ? num4 : num5) + (long)num14 * 86400000L;
			}
			break;
		}
		default:
			if (cashItemId / 10000 == 550)
			{
				Dictionary<string, object> info2 = BotItemCatalog.Shared.Info(cashItemId);
				int num6 = BotItemCatalog.Int(info2, "addTime");
				int num7 = BotItemCatalog.Int(info2, "maxDays");
				if (num >= 0 || num4 <= 0)
				{
					return Reject("请选择身上已穿戴的时限装备");
				}
				if (BotItemCatalog.Int(info, "cash") == 1)
				{
					return Reject("现金装备无法延长使用时间");
				}
				if (num6 <= 0)
				{
					return Reject("沙漏没有提供有效的延长时间");
				}
				long num8 = num4 + (long)num6 * 1000L;
				if (num7 > 0 && num8 > num5 + (long)num7 * 86400000L)
				{
					return Reject("延长后超过此沙漏允许的天数上限");
				}
				dbRow2["expiration"] = num8;
				break;
			}
			if (cashItemId / 10000 == 552)
			{
				int num9 = ((num2 / 1000000 == 1) ? 16 : 2);
				if (DbStore.Int(dbRow2, "quantity") <= 0 || (num3 & num9) != 0 || BotItemCatalog.Int(info, "tradeAvailable") <= 0)
				{
					return Reject("这件物品无法使用宿命剪刀");
				}
				dbRow2["flag"] = (long)(num3 | num9);
				break;
			}
			if (cashItemId / 10000 == 557)
			{
				DbRow dbRow3 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
				if (num2 / 1000000 != 1 || !InventoryDao.IsBagPosition(num) || dbRow3 == null || DbStore.Int(dbRow3, "vicious") >= 2)
				{
					return Reject("请选择背包中金锤次数不足两次的装备");
				}
				if (!NativeScrollService.HasStats(CharService.ToInventoryEntry(db, dbRow2)))
				{
					InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanEquip(num2);
					dbRow3["upgradeslots"] = (long)inventoryEntry.upgradeslots;
					string[] array = new string[14]
					{
						"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
						"acc", "avoid", "speed", "jump"
					};
					int[] array2 = new int[14]
					{
						inventoryEntry.str, inventoryEntry.dex, inventoryEntry.int_, inventoryEntry.luk, inventoryEntry.hp, inventoryEntry.mp, inventoryEntry.watk, inventoryEntry.matk, inventoryEntry.wdef, inventoryEntry.mdef,
						inventoryEntry.acc, inventoryEntry.avoid, inventoryEntry.speed, inventoryEntry.jump
					};
					for (int num10 = 0; num10 < array.Length; num10++)
					{
						dbRow3[array[num10]] = (long)array2[num10];
					}
					dbRow3["nativeStats"] = 1L;
				}
				dbRow3["vicious"] = (long)(DbStore.Int(dbRow3, "vicious") + 1);
				dbRow3["upgradeslots"] = (long)(DbStore.Int(dbRow3, "upgradeslots") + 1);
				break;
			}
			if (cashItemId == 5060002)
			{
				if (num2 == 5060002)
				{
					return Reject("不能孵化孵化器自己");
				}
				int[] array3 = new int[20]
				{
					1012070, 1302049, 1302063, 1322027, 2000004, 2000005, 2020013, 2020015, 2040307, 2040509,
					2040519, 2040521, 2040533, 2040715, 2040717, 2040810, 2040811, 2070005, 2070006, 4020009
				};
				int[] array4 = new int[20]
				{
					1, 1, 1, 1, 240, 200, 200, 200, 1, 1,
					1, 1, 1, 1, 1, 1, 1, 1, 1, 3
				};
				int num11 = ((IncubatorPick != null) ? IncubatorPick(array3.Length) : new Random().Next(array3.Length));
				int num12 = array3[num11];
				int num13 = array4[num11];
				if (!NativeInventoryService.CanReceive(db, charRecord, new InventoryChangeRequest
				{
					checkItems = new int[1] { num12 },
					checkQuantities = new int[1] { num13 }
				}))
				{
					return Reject("背包空间不足");
				}
				if (InventoryDao.ConsumeItem(db, request.characterId, num2, 1) <= 0)
				{
					return Reject("祭品道具不存在");
				}
				NativeInventoryService.AddItems(db, charRecord, num12, num13, -1L);
				InventoryDao.ConsumeItem(db, request.characterId, cashItemId, 1);
				db.Save();
				return new EquipPatchResponse
				{
					inventory = CharService.Load(db, request.characterId).inventory,
					rewardItemId = num12,
					rewardQuantity = num13
				};
			}
			return Reject("无效的装备现金道具");
		}
		InventoryDao.ConsumeItem(db, request.characterId, cashItemId, 1);
		db.Save();
		return new EquipPatchResponse
		{
			inventory = CharService.Load(db, request.characterId).inventory
		};
	}
}
