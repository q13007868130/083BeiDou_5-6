using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeStarForceService
{
	private static readonly string[] Columns = new string[14]
	{
		"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
		"acc", "avoid", "speed", "jump"
	};

	private static readonly string[] WzKeys = new string[14]
	{
		"incSTR", "incDEX", "incINT", "incLUK", "incMHP", "incMMP", "incPAD", "incMAD", "incPDD", "incMDD",
		"incACC", "incEVA", "incSpeed", "incJump"
	};

	private static StarForceResponse Reject(string error)
	{
		return new StarForceResponse
		{
			ok = false,
			error = error
		};
	}

	public static StarForceResponse Handle(DbStore db, StarForceRequest request, StarForceRules rules = null, BotItemCatalog catalog = null, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效星力请求");
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			if (charRecord == null)
			{
				return Reject("角色不存在");
			}
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			if (dbRow == null)
			{
				return Reject("没有持有这件装备");
			}
			int num = DbStore.Int(dbRow, "inventorytype");
			int slot = DbStore.Int(dbRow, "position");
			if ((num != 1 && num != 1) || NativeAndroidService.IsApSlot(slot))
			{
				return Reject("请使用背包或已穿戴的装备");
			}
			long num2 = DbStore.Long(dbRow, "expiration", -1L);
			if (num2 > 0 && num2 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
			{
				return Reject("装备已到期");
			}
			int num3 = DbStore.Int(dbRow, "itemid");
			int num4 = num3 / 10000;
			if (Upgradable(num4))
			{
				switch (num4)
				{
				case 135:
					break;
				case 156:
				case 157:
					return Reject("零职业双武器强化链尚未接入");
				default:
				{
					DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
					if (dbRow2 == null)
					{
						dbRow2 = InventoryDao.EnsureEquipment(db, request.inventoryItemId, catalog);
						if (dbRow2 == null)
						{
							return Reject("装备模板缺失，无法恢复装备实例");
						}
						db.Save();
					}
					if (NativePotentialService.IsReserved(db, request.inventoryItemId))
					{
						return Reject("请先选择方块候选结果");
					}
					if (request.sacrificeInventoryItemId > 0)
					{
						return Restore(db, request, dbRow, dbRow2, rules, catalog);
					}
					if (DbStore.Int(dbRow2, "starForceVestige") != 0)
					{
						return Reject("装备痕迹需要先复原");
					}
					if (DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
					{
						return Reject("装备已上锁");
					}
					if (DbStore.Int(dbRow2, "enhancementVersion") != 282 && (DbStore.Int(dbRow2, "starLevel") != 0 || !string.IsNullOrEmpty(DbStore.Str(dbRow2, "expandAttribute4"))))
					{
						return Reject("旧强化装备尚未完成版本切换");
					}
					int num5 = DbStore.Int(dbRow2, "starForceRevision");
					if (request.commit && request.revision != num5)
					{
						return Reject("装备状态已变化，请重新预览");
					}
					if (catalog == null)
					{
						catalog = BotItemCatalog.Shared;
					}
					Dictionary<string, object> dictionary = catalog.Info(num3);
					if (dictionary == null || BotItemCatalog.Int(dictionary, "cash") != 0 || BotItemCatalog.Int(dictionary, "tuc") <= 0)
					{
						return Reject("装备模板不支持星力强化");
					}
					bool flag = BotItemCatalog.Int(dictionary, "superiorEqp") != 0;
					int num6 = BotItemCatalog.Int(dictionary, "reqLevel") + DbStore.Int(dbRow2, "iIncReq");
					int num7 = DbStore.Int(dbRow2, "starLevel");
					int dropStreak = DbStore.Int(dbRow2, "starCount");
					if (rules == null)
					{
						rules = StarForceRules.Shared;
					}
					StarForceRules.Quote quote;
					try
					{
						quote = rules.Preview(num3, num6, num7, dropStreak, flag, request.safeguard, DbStore.Int(dbRow2, "starForceProtection") != 0);
					}
					catch (ArgumentException ex)
					{
						return Reject(ex.Message);
					}
					Dictionary<string, int> dictionary2 = BaseStats(dbRow2, dictionary);
					StarForceResponse starForceResponse = new StarForceResponse
					{
						quote = quote,
						revision = num5,
						meso = charRecord.meso,
						cap = rules.Cap(num3, num6, flag),
						derivedStats = StarForceStats.Calculate(num3, num6, num7, flag, dictionary2),
						nextStats = StarForceStats.Calculate(num3, num6, num7 + 1, flag, dictionary2)
					};
					if (!request.commit)
					{
						return starForceResponse;
					}
					if (charRecord.meso < quote.cost)
					{
						return Reject("枫币不足");
					}
					int num8 = 0;
					int slot2;
					for (slot2 = 1; slot2 <= charRecord.equipslots; slot2++)
					{
						if (db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Int(x, "inventorytype") == 1 && DbStore.Int(x, "position") == slot2) == null)
						{
							num8 = slot2;
							break;
						}
					}
					if (num8 == 0)
					{
						return Reject("装备栏需要至少一个空位");
					}
					StarForceRules.Result result = rules.Roll(num7, dropStreak, flag, quote, (random ?? new Random()).Next(10000));
					db.BeginTransaction();
					try
					{
						foreach (KeyValuePair<string, int> item in dictionary2)
						{
							dbRow2[item.Key] = (long)item.Value;
						}
						dbRow2["nativeStats"] = 1L;
						dbRow2["enhancementVersion"] = 282L;
						dbRow2["starLevel"] = (long)result.stars;
						dbRow2["starCount"] = (long)result.dropStreak;
						dbRow2["maxStar"] = (long)rules.Cap(num3, num6, flag);
						dbRow2["starForceVestige"] = (long)(result.vestige ? 1 : 0);
						checked
						{
							dbRow2["starForceRevision"] = unchecked((long)num5) + 1L;
							if (!request.safeguard)
							{
								dbRow2["starForceProtection"] = 0L;
							}
						}
						if (result.vestige && num == 1)
						{
							dbRow["inventorytype"] = 1L;
							dbRow["position"] = (long)num8;
						}
						charRecord.meso -= quote.cost;
						CharactersDao.Save(db, charRecord);
						db.Save();
						db.Commit();
					}
					catch
					{
						db.Rollback();
						throw;
					}
					starForceResponse.result = result;
					starForceResponse.revision = num5 + 1;
					starForceResponse.meso = charRecord.meso;
					starForceResponse.derivedStats = (result.vestige ? new Dictionary<string, int>() : StarForceStats.Calculate(num3, num6, result.stars, flag, dictionary2));
					starForceResponse.inventory = CharService.Load(db, request.characterId).inventory;
					return starForceResponse;
				}
				}
			}
			return Reject("这件装备不能进行星力强化");
		}
	}

	private static bool Upgradable(int category)
	{
		if (category < 100 || category > 113)
		{
			switch (category)
			{
			default:
				return category == 167;
			case 115:
			case 118:
			case 121:
			case 122:
			case 123:
			case 124:
			case 125:
			case 126:
			case 127:
			case 128:
			case 129:
			case 130:
			case 131:
			case 132:
			case 133:
			case 134:
			case 136:
			case 137:
			case 138:
			case 139:
			case 140:
			case 141:
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
			case 150:
			case 151:
			case 152:
			case 153:
			case 154:
			case 155:
			case 156:
			case 157:
			case 158:
			case 159:
				break;
			}
		}
		return true;
	}

	private static StarForceResponse Restore(DbStore db, StarForceRequest request, DbRow item, DbRow equipment, StarForceRules rules, BotItemCatalog catalog)
	{
		if (DbStore.Int(equipment, "starForceVestige") == 0)
		{
			return Reject("这件装备不是装备痕迹");
		}
		int num = DbStore.Int(equipment, "starForceRevision");
		if (request.commit && request.revision != num)
		{
			return Reject("装备状态已变化，请重新预览");
		}
		if (request.sacrificeInventoryItemId == request.inventoryItemId)
		{
			return Reject("装备痕迹不能用来复原自身");
		}
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.sacrificeInventoryItemId);
		if (dbRow == null || DbStore.Int(dbRow, "itemid") != DbStore.Int(item, "itemid") || (DbStore.Int(dbRow, "inventorytype") != 1 && DbStore.Int(dbRow, "inventorytype") != 1))
		{
			return Reject("需要一件相同道具ID的装备进行复原");
		}
		DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.sacrificeInventoryItemId);
		if (NativePotentialService.IsReserved(db, request.sacrificeInventoryItemId))
		{
			return Reject("请先选择材料装备的方块候选结果");
		}
		if (dbRow2 == null || DbStore.Int(dbRow2, "starForceVestige") != 0 || DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
		{
			return Reject("复原材料装备不可用");
		}
		if (NativeAndroidService.IsApSlot(DbStore.Int(dbRow, "position")))
		{
			return Reject("请先取下机器人换装部件");
		}
		long num2 = DbStore.Long(dbRow, "expiration", -1L);
		if (num2 > 0 && num2 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
		{
			return Reject("复原材料装备已到期");
		}
		StarForceResponse starForceResponse = new StarForceResponse
		{
			revision = num,
			meso = CharactersDao.FindById(db, request.characterId).meso
		};
		if (!request.commit)
		{
			return starForceResponse;
		}
		db.BeginTransaction();
		checked
		{
			try
			{
				db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.sacrificeInventoryItemId);
				db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.sacrificeInventoryItemId);
				equipment["starForceVestige"] = 0L;
				equipment["starForceRevision"] = unchecked((long)num) + 1L;
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
			starForceResponse.restored = true;
		}
		starForceResponse.revision = num + 1;
		starForceResponse.inventory = CharService.Load(db, request.characterId).inventory;
		return starForceResponse;
	}

	private static Dictionary<string, int> BaseStats(DbRow row, Dictionary<string, object> info)
	{
		bool flag = DbStore.Int(row, "nativeStats") != 0 || DbStore.Int(row, "level") != 0 || DbStore.Int(row, "vicious") != 0;
		string[] columns = Columns;
		foreach (string col in columns)
		{
			if (DbStore.Int(row, col) != 0)
			{
				flag = true;
			}
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int j = 0; j < Columns.Length; j++)
		{
			dictionary[Columns[j]] = (flag ? DbStore.Int(row, Columns[j]) : BotItemCatalog.Int(info, WzKeys[j]));
		}
		return dictionary;
	}
}
