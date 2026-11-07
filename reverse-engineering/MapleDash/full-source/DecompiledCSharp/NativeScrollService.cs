using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeScrollService
{
	private static readonly string[] Columns = new string[14]
	{
		"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
		"acc", "avoid", "speed", "jump"
	};

	private static readonly string[] WzStats = new string[14]
	{
		"STR", "DEX", "INT", "LUK", "MHP", "MMP", "PAD", "MAD", "PDD", "MDD",
		"ACC", "EVA", "Speed", "Jump"
	};

	private static EquipPatchResponse Reject(string error)
	{
		return new EquipPatchResponse
		{
			ok = false,
			error = error
		};
	}

	public static bool HasStats(InventoryEntry e)
	{
		if (e != null)
		{
			if ((e.equipmentData == null || !e.equipmentData.TryGetValue("nativeStats", out var value) || Convert.ToInt32(value) == 0) && e.str == 0 && e.dex == 0 && e.int_ == 0 && e.luk == 0 && e.hp == 0 && e.mp == 0 && e.watk == 0 && e.matk == 0 && e.wdef == 0 && e.mdef == 0 && e.acc == 0 && e.avoid == 0 && e.speed == 0 && e.jump == 0 && e.upgradeslots == 0)
			{
				return e.starLevel != 0;
			}
			return true;
		}
		return false;
	}

	private static bool Pristine(DbRow row)
	{
		if (DbStore.Int(row, "level") != 0 || DbStore.Int(row, "vicious") != 0 || DbStore.Int(row, "upgradeslots") != 0)
		{
			return false;
		}
		for (int i = 0; i < Columns.Length; i++)
		{
			if (DbStore.Int(row, Columns[i]) != 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool UnsownSlots(DbRow row)
	{
		if (DbStore.Int(row, "level") == 0 && DbStore.Int(row, "vicious") == 0)
		{
			return DbStore.Int(row, "upgradeslots") == 0;
		}
		return false;
	}

	private static bool Clean(int id)
	{
		if (id >= 2049000)
		{
			return id <= 2049003;
		}
		return false;
	}

	private static bool Chaos(int id)
	{
		if ((id < 2049100 || id > 2049103) && id != 2049115)
		{
			return id == 2049116;
		}
		return true;
	}

	private static bool Modifier(int id)
	{
		if (id != 2040727)
		{
			return id == 2041058;
		}
		return true;
	}

	private static bool HeartScroll(int id)
	{
		return IsHeartScroll(id);
	}

	public static bool IsHeartScroll(int id)
	{
		if (id < 2616004 || id > 2616010)
		{
			if (id >= 2643008)
			{
				return id <= 2643010;
			}
			return false;
		}
		return true;
	}

	private static bool HeartEquip(int id)
	{
		return id / 10000 == 167;
	}

	private static string HeartHint(int scroll)
	{
		int[] array = BotItemCatalog.Shared.RequiredEquips(scroll);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		List<string> list = new List<string>();
		int[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			int id = array2[i];
			int num = BotItemCatalog.Int(BotItemCatalog.Shared.Info(id), "tuc");
			list.Add((BotItemCatalog.Shared.Name(id) ?? ("道具 " + id)) + ((num > 0) ? ("（" + num + " 槽）") : "（0 槽）"));
		}
		return "这张卷轴适用于：" + string.Join("、", list);
	}

	private static bool Category(int scroll, int equip)
	{
		if (!HeartScroll(scroll))
		{
			if (scroll / 100 != 20492)
			{
				return scroll / 100 % 100 == equip / 10000 % 100;
			}
			if (equip / 10000 != 111 && equip / 10000 != 112)
			{
				return equip / 10000 == 113;
			}
			return true;
		}
		return HeartEquip(equip);
	}

	public static EquipPatchResponse Apply(DbStore db, EquipPatchRequest request, Random random = null)
	{
		if (request == null)
		{
			return Reject("无效卷轴请求");
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
		if (random == null)
		{
			random = new Random();
		}
		CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
		if (charRecord == null)
		{
			return Reject("无效卷轴");
		}
		if (request.scrollId / 10000 != 204 && !HeartScroll(request.scrollId))
		{
			return Reject("无效卷轴");
		}
		DbTable dbTable = db.Table("inventoryitems");
		DbRow dbRow2 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId && DbStore.Int(x, "itemid") / 1000000 == 1);
		DbRow dbRow3 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Int(x, "itemid") == request.scrollId && DbStore.Int(x, "position") > 0 && (request.scrollPosition == 0 || DbStore.Int(x, "position") == request.scrollPosition) && DbStore.Int(x, "quantity") > 0);
		if (dbRow2 == null || dbRow3 == null)
		{
			return Reject("装备或卷轴不存在");
		}
		if (DbStore.Int(dbRow2, "position") > 0 && request.vegaItemId == 0 && SkillsDao.ByCharacter(db, request.characterId).Find((DbRow x) => DbStore.Int(x, "skillid") == 1003 && DbStore.Int(x, "skilllevel") > 0) == null)
		{
			return Reject("请将卷轴放到已穿戴的装备上，或先学习匠人之魂");
		}
		int num = DbStore.Int(dbRow2, "itemid");
		int scrollId = request.scrollId;
		if (request.scrollRequirements != null && request.scrollRequirements.Length != 0 && Array.IndexOf(request.scrollRequirements, num) < 0)
		{
			return Reject("这件装备不符合卷轴要求");
		}
		if (!Clean(scrollId) && !Chaos(scrollId) && scrollId != 2049920 && !Category(scrollId, num))
		{
			return Reject("卷轴与装备种类不符");
		}
		if (HeartScroll(scrollId))
		{
			int[] array = BotItemCatalog.Shared.RequiredEquips(scrollId);
			bool flag = array != null && array.Length != 0;
			if (flag && Array.IndexOf(array, num) < 0)
			{
				string text = HeartHint(scrollId);
				return Reject("这件心脏不符合卷轴要求" + ((text == null) ? "" : ("，" + text)));
			}
			if (!flag && !BotItemCatalog.Shared.ScopedTo(scrollId, num))
			{
				return Reject("这张卷轴没有适用这件心脏的数据");
			}
		}
		Dictionary<string, int> dictionary = BotItemCatalog.Shared.EquipStats(scrollId);
		if (dictionary == null)
		{
			return Reject("源端没有这张卷轴的数据");
		}
		DbRow dbRow4 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
		DbRow dbRow5 = new DbRow();
		if (dbRow4 != null)
		{
			foreach (KeyValuePair<string, object> item in dbRow4)
			{
				dbRow5[item.Key] = item.Value;
			}
		}
		InventoryEntry e = CharService.ToInventoryEntry(db, dbRow2);
		int num2 = BotItemCatalog.Int(BotItemCatalog.Shared.Info(num), "tuc");
		if (!HasStats(e) || Pristine(dbRow5))
		{
			Dictionary<string, int> dictionary2 = BotItemCatalog.Shared.EquipStats(num);
			for (int num3 = 0; num3 < Columns.Length; num3++)
			{
				dbRow5[Columns[num3]] = (long)(short)((dictionary2 != null && dictionary2.TryGetValue(WzStats[num3], out var value)) ? value : 0);
			}
			dbRow5["upgradeslots"] = (long)num2;
		}
		else if (UnsownSlots(dbRow5))
		{
			dbRow5["upgradeslots"] = (long)num2;
		}
		int num4 = DbStore.Int(dbRow5, "upgradeslots");
		int num5 = DbStore.Int(dbRow5, "level");
		int num6 = DbStore.Int(dbRow5, "vicious");
		int num7 = BotItemCatalog.Int(BotItemCatalog.Shared.Info(num), "tuc");
		if (Clean(scrollId) && (num7 == 0 || num4 + num5 >= num7 + num6))
		{
			return Reject("没有可恢复的升级次数");
		}
		if (!Clean(scrollId) && !Modifier(scrollId) && scrollId != 2049920 && num4 < 1)
		{
			string text2 = BotItemCatalog.Shared.Name(num) ?? ("道具 " + num);
			if (num7 <= 0)
			{
				if (!HeartScroll(scrollId))
				{
					return Reject(text2 + " 没有可升级次数（数据里 tuc=0），砸不了卷");
				}
				string text3 = HeartHint(scrollId);
				return Reject(text2 + " 是 0 槽心脏，砸不了卷" + ((text3 == null) ? "（数据里 tuc=0）" : ("，" + text3)));
			}
			return Reject("装备可升级次数不足（" + text2 + "：剩 " + num4 + " 次 / 共 " + num7 + " 次，已升级 " + num5 + " 次）");
		}
		DbRow dbRow6 = null;
		if (request.whiteScroll)
		{
			dbRow6 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Int(x, "position") > 0 && DbStore.Int(x, "quantity") > 0 && DbStore.Int(x, "itemid") == 2340001) ?? dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Int(x, "position") > 0 && DbStore.Int(x, "quantity") > 0 && DbStore.Int(x, "itemid") == 2340000);
		}
		DbRow dbRow7 = null;
		if (request.vegaItemId != 0)
		{
			if (request.vegaItemId != 5610000 && request.vegaItemId != 5610001)
			{
				return Reject("无效的织女星咒语");
			}
			dbRow7 = dbTable.Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Int(x, "position") > 0 && DbStore.Int(x, "quantity") > 0 && DbStore.Int(x, "itemid") == request.vegaItemId);
			if (dbRow7 == null)
			{
				return Reject("没有持有织女星咒语");
			}
		}
		double num8 = dictionary["success"];
		if (request.vegaItemId == 5610000 && num8 == 10.0)
		{
			num8 = 30.0;
		}
		if (request.vegaItemId == 5610001 && num8 == 60.0)
		{
			num8 = 90.0;
		}
		bool flag2 = charRecord.gm > 0 && GameConfigDao.GetBool(db, "use_perfect_gm_scroll", fallback: true);
		bool flag3 = flag2 || random.NextDouble() >= Math.Pow(1.0 - num8 / 100.0, GameConfigDao.GetInt(db, "scroll_chance_rolls", 1));
		bool flag4 = false;
		if (flag3)
		{
			if (Clean(scrollId) || scrollId == 2049920)
			{
				dbRow5["upgradeslots"] = (long)(sbyte)(num4 + 1);
			}
			else
			{
				switch (scrollId)
				{
				case 2040727:
					dbRow2["flag"] = (long)(DbStore.Int(dbRow2, "flag") | 2);
					break;
				case 2041058:
					dbRow2["flag"] = (long)(DbStore.Int(dbRow2, "flag") | 4);
					break;
				case 2049100:
				case 2049101:
				case 2049102:
				case 2049115:
				case 2049116:
				{
					bool flag5 = scrollId == 2049115 || scrollId == 2049116;
					int num10 = GameConfigDao.GetInt(db, flag5 ? "forward_chaos_scroll_stat_range" : "chaos_scroll_stat_range", 5);
					int num11 = GameConfigDao.GetInt(db, "chaos_scroll_stat_rate", 1);
					bool flag6 = GameConfigDao.GetBool(db, "use_enhanced_chaos_scroll");
					string[] columns = Columns;
					foreach (string text4 in columns)
					{
						int num13 = DbStore.Int(dbRow5, text4);
						if (num13 > 0)
						{
							int num14 = (flag6 ? num13 : ((num11 > 0) ? (-32768) : 0));
							for (int num15 = 0; num15 < Math.Max(1, num11); num15++)
							{
								num14 = Math.Min(32767, Math.Max(num14, ((flag6 && num11 > 0) ? num14 : num13) + random.Next((!flag5) ? (-num10) : 0, num10 + 1)));
							}
							dbRow5[text4] = (long)Math.Max(0, num14);
						}
					}
					break;
				}
				default:
				{
					for (int num9 = 0; num9 < Columns.Length; num9++)
					{
						if (dictionary.TryGetValue(WzStats[num9], out var value2))
						{
							dbRow5[Columns[num9]] = (long)(short)Math.Min(32767, DbStore.Int(dbRow5, Columns[num9]) + value2);
						}
					}
					break;
				}
				}
				if (!flag2 && !Modifier(scrollId))
				{
					dbRow5["upgradeslots"] = (long)(sbyte)(num4 - 1);
				}
				dbRow5["level"] = (long)(sbyte)(num5 + 1);
			}
		}
		else
		{
			if (!flag2 && !GameConfigDao.GetBool(db, "use_perfect_scrolling") && dbRow6 == null && !Clean(scrollId) && scrollId != 2049920 && !Modifier(scrollId))
			{
				dbRow5["upgradeslots"] = (long)(sbyte)(num4 - 1);
			}
			flag4 = random.Next(100) < dictionary["cursed"];
		}
		if (dbRow6 != null && !Clean(scrollId))
		{
			Consume(db, dbRow6);
		}
		Consume(db, dbRow3);
		if (dbRow7 != null)
		{
			Consume(db, dbRow7);
		}
		if (flag4 && num != 1112803 && num != 1112806 && num != 1112807 && num != 1112809)
		{
			dbTable.Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
		}
		else
		{
			flag4 = false;
			dbRow5["nativeStats"] = 1L;
			if (dbRow4 == null)
			{
				InventoryDao.InsertEquipment(db, request.inventoryItemId);
				dbRow4 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
			}
			foreach (KeyValuePair<string, object> item2 in dbRow5)
			{
				dbRow4[item2.Key] = item2.Value;
			}
		}
		db.Save();
		return new EquipPatchResponse
		{
			scrollResult = (flag4 ? "curse" : (flag3 ? "success" : "fail")),
			inventory = CharService.Load(db, request.characterId).inventory
		};
	}

	private static void Consume(DbStore db, DbRow row)
	{
		int num = DbStore.Int(row, "quantity");
		long id = DbStore.Long(row, "inventoryitemid", 0L);
		if (num > 1)
		{
			row["quantity"] = (long)(num - 1);
			return;
		}
		db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == id);
	}
}
