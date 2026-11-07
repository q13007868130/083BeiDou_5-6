using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativePotentialService
{
	private static PotentialRoller roller;

	private static PotentialRoller Shared => roller ?? (roller = new PotentialRoller(PotentialCatalog.Shared, new CubeRateTable(PlatformRes.LoadText("Enhancement/CubeRates"))));

	private static PotentialResponse Reject(string message)
	{
		return new PotentialResponse
		{
			ok = false,
			error = message
		};
	}

	public static bool IsReserved(DbStore db, long id)
	{
		DbRow dbRow = InventoryDao.EquipmentOf(db, id);
		if (dbRow != null)
		{
			return !string.IsNullOrEmpty(DbStore.Str(dbRow, "potentialChoice"));
		}
		return false;
	}

	public static bool BlocksTransfer(DbStore db, InventoryEntry item)
	{
		if (item == null)
		{
			return false;
		}
		DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == item.itemRowId);
		if (dbRow != null && NativeSoulWeaponWz2Service.IsBound(db, DbStore.Int(dbRow, "characterid"), item.itemRowId))
		{
			return true;
		}
		DbRow dbRow2 = InventoryDao.EquipmentOf(db, item.itemRowId);
		if (dbRow2 != null && (DbStore.Int(dbRow2, "starForceVestige") != 0 || !string.IsNullOrEmpty(DbStore.Str(dbRow2, "potentialChoice"))))
		{
			return true;
		}
		Dictionary<string, object> equipmentData = item.equipmentData;
		if (equipmentData != null)
		{
			if (!equipmentData.TryGetValue("starForceVestige", out var value) || Convert.ToInt32(value) == 0)
			{
				if (equipmentData.TryGetValue("potentialChoice", out var value2))
				{
					return !string.IsNullOrEmpty(value2 as string);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static int[] Options(DbRow equipment, bool bonus)
	{
		string text = (bonus ? "bonusOption" : "potentialOption");
		return new int[3]
		{
			DbStore.Int(equipment, text + "1"),
			DbStore.Int(equipment, text + "2"),
			DbStore.Int(equipment, text + "3")
		};
	}

	private static bool Expired(DbRow item)
	{
		long num = DbStore.Long(item, "expiration", -1L);
		if (num > 0)
		{
			return num <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}
		return false;
	}

	public static PotentialResponse Handle(DbStore db, PotentialRequest request, PotentialRoller rolls = null, BotItemCatalog catalog = null, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效潜能请求");
		}
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			if (charRecord == null || dbRow == null || Expired(dbRow))
			{
				return Reject("装备不存在或已到期");
			}
			int num = DbStore.Int(dbRow, "inventorytype");
			int slot = DbStore.Int(dbRow, "position");
			if ((num != 1 && num != 1) || NativeAndroidService.IsApSlot(slot))
			{
				return Reject("请选择背包或已穿戴的装备");
			}
			DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
			if (dbRow2 == null || DbStore.Int(dbRow2, "starForceVestige") != 0)
			{
				return Reject("装备痕迹不能洗潜能");
			}
			if (DbStore.Int(dbRow2, "enhancementVersion") != 282 && (DbStore.Int(dbRow2, "starLevel") != 0 || !string.IsNullOrEmpty(DbStore.Str(dbRow2, "expandAttribute4"))))
			{
				return Reject("旧强化装备尚未完成版本切换");
			}
			int num2 = DbStore.Int(dbRow2, "potentialRevision");
			if (request.commit && request.revision != num2)
			{
				return Reject("装备状态已变化，请重新预览");
			}
			int[] main = Options(dbRow2, bonus: false);
			int[] bonus = Options(dbRow2, bonus: true);
			if (request.choose)
			{
				return Choose(db, request, dbRow2, charRecord.meso, main, bonus);
			}
			if (IsReserved(db, request.inventoryItemId))
			{
				return Reject("请先选择上一次方块结果");
			}
			foreach (DbRow row in db.Table("inventoryitems").rows)
			{
				if (DbStore.Int(row, "characterid") == request.characterId && IsReserved(db, DbStore.Long(row, "inventoryitemid", 0L)))
				{
					return Reject("请先选择上一次方块结果");
				}
			}
			int num3 = DbStore.Int(dbRow, "itemid");
			int num4 = num3 / 10000;
			if (num4 == 156 || num4 == 157)
			{
				return Reject("零职业双武器潜能链尚未接入");
			}
			if (DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
			{
				return Reject("装备已上锁");
			}
			if (catalog == null)
			{
				catalog = BotItemCatalog.Shared;
			}
			Dictionary<string, object> dictionary = catalog.Info(num3);
			if (dictionary == null || BotItemCatalog.Int(dictionary, "cash") != 0 || BotItemCatalog.Int(dictionary, "noPotential") != 0 || BotItemCatalog.Int(dictionary, "fixedPotential") != 0)
			{
				return Reject("这件装备不能变更潜能");
			}
			if (BotItemCatalog.Int(dictionary, "tuc") < 1 && !TucIgnore(num3))
			{
				return Reject("装备模板不能拥有潜能");
			}
			if (!PotentialType(num4))
			{
				return Reject("这类装备不能拥有潜能");
			}
			DbRow dbRow3 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.cubeInventoryItemId);
			if (dbRow3 == null || Expired(dbRow3) || DbStore.Int(dbRow3, "quantity") < 1 || DbStore.Int(dbRow3, "position") <= 0 || (DbStore.Int(dbRow3, "inventorytype") != 2 && DbStore.Int(dbRow3, "inventorytype") != 5))
			{
				return Reject("方块不存在或已到期");
			}
			int num5 = DbStore.Int(dbRow3, "itemid");
			if (num5 == 2049400 || num5 == 2049401)
			{
				return ApplyScroll(db, request, dbRow2, charRecord, dbRow3, num3, dictionary, main, bonus, num2, rolls ?? Shared, random ?? new Random());
			}
			CubeRules.Family family = CubeRules.GetFamily(num5);
			if (family == null)
			{
				return Reject("此方块的特殊规则尚未接入");
			}
			int num6 = DbStore.Int(dbRow2, family.bonus ? "bonusGrade" : "potentialGrade");
			if (num6 < family.minimum || num6 > family.maximum)
			{
				return Reject("潜能等级不适用此方块");
			}
			int num7 = DbStore.Int(dbRow, "flag");
			if (family.karma && BotItemCatalog.Int(dictionary, "tradeBlock") == 0 && (num7 & 8) == 0)
			{
				return Reject("卡勒马方块需要不可交易装备");
			}
			int equipLevel = BotItemCatalog.Int(dictionary, "reqLevel") + DbStore.Int(dbRow2, "iIncReq");
			long num8 = CubeRules.AppraisalCost(equipLevel, num5);
			PotentialResponse potentialResponse = new PotentialResponse
			{
				revision = num2,
				grade = num6,
				main = main,
				bonus = bonus,
				cost = num8,
				meso = charRecord.meso
			};
			if (!request.commit)
			{
				return potentialResponse;
			}
			if (charRecord.meso < num8)
			{
				return Reject("枫币不足");
			}
			PotentialRoller.Result result;
			try
			{
				result = (rolls ?? Shared).Roll(num5, num3, equipLevel, num6, main, bonus, random ?? new Random());
			}
			catch (InvalidOperationException ex)
			{
				return Reject(ex.Message);
			}
			db.BeginTransaction();
			try
			{
				int num9 = DbStore.Int(dbRow3, "quantity");
				if (num9 == 1)
				{
					db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.cubeInventoryItemId);
				}
				else
				{
					dbRow3["quantity"] = (long)num9 - 1L;
				}
				charRecord.meso -= num8;
				CharactersDao.Save(db, charRecord);
				dbRow2["enhancementVersion"] = 282L;
				checked
				{
					dbRow2["potentialRevision"] = unchecked((long)num2) + 1L;
					if (family.choice)
					{
						dbRow2["potentialChoice"] = "{\"bonus\":" + (family.bonus ? "true" : "false") + ",\"grade\":" + result.grade + ",\"options\":[" + string.Join(",", result.options) + "],\"cubeId\":" + num5 + "}";
					}
					else
					{
						Write(dbRow2, family.bonus, result.grade, result.options);
					}
					db.Save();
					db.Commit();
				}
			}
			catch
			{
				db.Rollback();
				throw;
			}
			potentialResponse.revision = num2 + 1;
			potentialResponse.meso = charRecord.meso;
			potentialResponse.pending = family.choice;
			potentialResponse.candidate = result.options;
			potentialResponse.grade = result.grade;
			potentialResponse.main = Options(dbRow2, bonus: false);
			potentialResponse.bonus = Options(dbRow2, bonus: true);
			potentialResponse.inventory = CharService.Load(db, request.characterId).inventory;
			return potentialResponse;
		}
	}

	private static PotentialResponse ApplyScroll(DbStore db, PotentialRequest request, DbRow equipment, CharRecord character, DbRow scroll, int itemId, Dictionary<string, object> info, int[] main, int[] bonus, int revision, PotentialRoller rolls, Random random)
	{
		if (main[0] != 0)
		{
			return Reject("这件装备已经拥有潜能");
		}
		int equipLevel = BotItemCatalog.Int(info, "reqLevel") + DbStore.Int(equipment, "iIncReq");
		PotentialResponse potentialResponse = new PotentialResponse
		{
			revision = revision,
			main = main,
			bonus = bonus,
			meso = character.meso
		};
		if (!request.commit)
		{
			return potentialResponse;
		}
		bool flag = random.Next(100) < ((DbStore.Int(scroll, "itemid") == 2049400) ? 90 : 70);
		PotentialRoller.Result result = null;
		if (flag)
		{
			int[] main2 = new int[3]
			{
				-1,
				-1,
				(random.Next(100) < 10) ? (-1) : 0
			};
			try
			{
				result = rolls.Reveal(itemId, equipLevel, 1, bonus: false, main2, bonus, random);
			}
			catch (InvalidOperationException ex)
			{
				return Reject(ex.Message);
			}
		}
		db.BeginTransaction();
		try
		{
			int num = DbStore.Int(scroll, "quantity");
			if (num == 1)
			{
				db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.cubeInventoryItemId);
			}
			else
			{
				scroll["quantity"] = (long)num - 1L;
			}
			equipment["enhancementVersion"] = 282L;
			checked
			{
				equipment["potentialRevision"] = unchecked((long)revision) + 1L;
				if (result != null)
				{
					Write(equipment, bonus: false, result.grade, result.options);
				}
				db.Save();
				db.Commit();
			}
		}
		catch
		{
			db.Rollback();
			throw;
		}
		potentialResponse.revision = revision + 1;
		potentialResponse.main = Options(equipment, bonus: false);
		potentialResponse.grade = DbStore.Int(equipment, "potentialGrade");
		potentialResponse.scrollResult = (flag ? "success" : "fail");
		potentialResponse.inventory = CharService.Load(db, request.characterId).inventory;
		return potentialResponse;
	}

	private static void Write(DbRow row, bool bonus, int grade, int[] options)
	{
		row[bonus ? "bonusGrade" : "potentialGrade"] = (long)grade;
		string text = (bonus ? "bonusOption" : "potentialOption");
		for (int i = 0; i < 3; i++)
		{
			row[text + (i + 1)] = (long)options[i];
		}
	}

	private static PotentialResponse Choose(DbStore db, PotentialRequest request, DbRow row, long meso, int[] main, int[] bonus)
	{
		if (!(MiniJsonDb.Parse(DbStore.Str(row, "potentialChoice")) is Dictionary<string, object> dictionary))
		{
			return Reject("没有待选择的方块结果");
		}
		int grade = Convert.ToInt32(dictionary["grade"]);
		List<object> list = (List<object>)dictionary["options"];
		int[] array = new int[3]
		{
			Convert.ToInt32(list[0]),
			Convert.ToInt32(list[1]),
			Convert.ToInt32(list[2])
		};
		int num = DbStore.Int(row, "potentialRevision");
		PotentialResponse potentialResponse = new PotentialResponse
		{
			revision = num,
			main = main,
			bonus = bonus,
			candidate = array,
			grade = grade,
			meso = meso,
			pending = true
		};
		if (!request.commit)
		{
			return potentialResponse;
		}
		db.BeginTransaction();
		checked
		{
			try
			{
				if (request.keepNew)
				{
					Write(row, (bool)dictionary["bonus"], grade, array);
				}
				row["potentialChoice"] = "";
				row["potentialRevision"] = unchecked((long)num) + 1L;
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
		}
		potentialResponse.revision = num + 1;
		potentialResponse.pending = false;
		potentialResponse.main = Options(row, bonus: false);
		potentialResponse.bonus = Options(row, bonus: true);
		potentialResponse.inventory = CharService.Load(db, request.characterId).inventory;
		return potentialResponse;
	}

	private static bool PotentialType(int prefix)
	{
		if (prefix < 100 || prefix > 115 || prefix == 114)
		{
			switch (prefix)
			{
			default:
				return prefix == 167;
			case 118:
			case 119:
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
			case 135:
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

	private static bool TucIgnore(int id)
	{
		if (id / 10000 != 135 && id / 10000 != 119 && (id < 1099000 || id > 1099015))
		{
			if (id >= 1098000)
			{
				return id <= 1098008;
			}
			return false;
		}
		return true;
	}
}
