using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Bot;

public static class NativeFlameService
{
	private const int MaxTier = 7;

	private static FlameResponse Reject(string error)
	{
		return new FlameResponse
		{
			ok = false,
			error = error
		};
	}

	public static bool IsReserved(DbStore db, long inventoryItemId)
	{
		DbRow dbRow = InventoryDao.EquipmentOf(db, inventoryItemId);
		if (dbRow != null)
		{
			return !string.IsNullOrEmpty(DbStore.Str(dbRow, "flameChoice"));
		}
		return false;
	}

	public static FlameResponse Handle(DbStore db, FlameRequest request, Random random = null)
	{
		if (db == null || request == null)
		{
			return Reject("无效星火请求");
		}
		if (random == null)
		{
			random = new Random();
		}
		lock (db)
		{
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
			if (dbRow == null)
			{
				return Reject("没有持有这件装备");
			}
			int num = DbStore.Int(dbRow, "itemid");
			int num2 = DbStore.Int(dbRow, "inventorytype");
			int slot = DbStore.Int(dbRow, "position");
			if (num2 != 1 && num2 != 1)
			{
				return Reject("请使用背包或已穿戴的装备");
			}
			if (NativeAndroidService.IsApSlot(slot))
			{
				return Reject("请先取下机器人换装部件");
			}
			long num3 = DbStore.Long(dbRow, "expiration", -1L);
			if (num3 > 0 && num3 <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
			{
				return Reject("装备已到期");
			}
			if (!FlameCatalog.CanFlame(num))
			{
				return Reject("这件装备不能使用星火");
			}
			DbRow dbRow2 = InventoryDao.EquipmentOf(db, request.inventoryItemId);
			if (dbRow2 == null)
			{
				return Reject("装备模板缺失");
			}
			if (DbStore.Int(dbRow2, "locked") != 0 || (DbStore.Int(dbRow, "flag") & 1) != 0)
			{
				return Reject("装备已上锁");
			}
			int num4 = DbStore.Int(dbRow2, "flameRevision");
			if (request.choose)
			{
				if (request.commit && request.revision != num4)
				{
					return Reject("装备状态已变化，请重新预览");
				}
				return Choose(db, request, dbRow2, num4);
			}
			if (request.commit && request.revision != num4)
			{
				return Reject("装备状态已变化，请重新预览");
			}
			if (IsReserved(db, request.inventoryItemId))
			{
				return Reject("请先选择上一次星火的结果");
			}
			DbRow dbRow3 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == request.characterId && DbStore.Long(x, "inventoryitemid", 0L) == request.flameInventoryItemId);
			if (dbRow3 == null || DbStore.Int(dbRow3, "quantity") < 1 || DbStore.Int(dbRow3, "position") <= 0)
			{
				return Reject("星火道具不存在");
			}
			int itemId = DbStore.Int(dbRow3, "itemid");
			int num5 = FlameCatalog.CreateTypeOf(itemId);
			if (num5 < 0)
			{
				return Reject("这个道具不是星火");
			}
			FlameRule flameRule = FlameCatalog.Rule(num5);
			if (flameRule == null)
			{
				return Reject("这种星火还没有规则数据");
			}
			Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(num);
			int reqLevel = ((dictionary != null) ? BotItemCatalog.Int(dictionary, "reqLevel") : 0);
			int incReq = DbStore.Int(dbRow2, "iIncReq");
			bool weapon = num / 10000 >= 121 && num / 10000 <= 160;
			bool bossWeapon = dictionary != null && BotItemCatalog.Int(dictionary, "bossReward") != 0;
			int baseAttack = ((dictionary != null) ? Math.Max(BotItemCatalog.Int(dictionary, "incPAD"), BotItemCatalog.Int(dictionary, "incMAD")) : 0);
			FlameResponse flameResponse = new FlameResponse
			{
				revision = num4,
				createType = num5,
				exGradeOption = DbStore.Long(dbRow2, "exGradeOption", 0L)
			};
			flameResponse.current = Describe(dbRow, dbRow2, flameResponse.exGradeOption);
			if (!request.commit)
			{
				flameResponse.candidate = Roll(flameRule, reqLevel, incReq, weapon, bossWeapon, baseAttack, random);
				flameResponse.lineCount = flameResponse.candidate.Count;
				return flameResponse;
			}
			if (!FlameCatalog.IsChoice(itemId) && IsReserved(db, request.inventoryItemId))
			{
				return Reject("请先选择上一次星火的结果");
			}
			List<FlameLine> list = Roll(flameRule, reqLevel, incReq, weapon, bossWeapon, baseAttack, random);
			bool flag = FlameCatalog.IsChoice(itemId);
			db.BeginTransaction();
			try
			{
				int num6 = DbStore.Int(dbRow3, "quantity");
				if (num6 <= 1)
				{
					db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.flameInventoryItemId);
				}
				else
				{
					dbRow3["quantity"] = (long)num6 - 1L;
				}
				long num7 = Pack(list);
				if (flag)
				{
					dbRow2["flameChoice"] = Serialize(list);
				}
				else
				{
					dbRow2["exGradeOption"] = num7;
				}
				checked
				{
					dbRow2["flameRevision"] = unchecked((long)num4) + 1L;
					db.Save();
					db.Commit();
					flameResponse.pending = flag;
					if (!flag)
					{
						flameResponse.exGradeOption = num7;
					}
				}
			}
			catch
			{
				db.Rollback();
				throw;
			}
			flameResponse.revision = num4 + 1;
			flameResponse.candidate = list;
			flameResponse.lineCount = list.Count;
			if (!flameResponse.pending)
			{
				flameResponse.current = list;
			}
			flameResponse.inventory = CharService.Load(db, request.characterId).inventory;
			return flameResponse;
		}
	}

	private static FlameResponse Choose(DbStore db, FlameRequest request, DbRow equipment, int revision)
	{
		string text = DbStore.Str(equipment, "flameChoice");
		if (string.IsNullOrEmpty(text))
		{
			return Reject("没有待确认的星火结果");
		}
		DbRow target = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == request.inventoryItemId);
		List<FlameLine> list = Describe(target, equipment, long.TryParse(text, out var result) ? result : 0);
		FlameResponse flameResponse = new FlameResponse
		{
			revision = revision,
			pending = true,
			candidate = list,
			lineCount = list.Count,
			exGradeOption = DbStore.Long(equipment, "exGradeOption", 0L)
		};
		flameResponse.current = Describe(target, equipment, flameResponse.exGradeOption);
		if (!request.commit)
		{
			return flameResponse;
		}
		db.BeginTransaction();
		checked
		{
			try
			{
				if (request.keepNew)
				{
					equipment["exGradeOption"] = Pack(list);
				}
				equipment["flameChoice"] = "";
				equipment["flameRevision"] = unchecked((long)revision) + 1L;
				db.Save();
				db.Commit();
			}
			catch
			{
				db.Rollback();
				throw;
			}
		}
		flameResponse.revision = revision + 1;
		flameResponse.pending = false;
		flameResponse.exGradeOption = DbStore.Long(equipment, "exGradeOption", 0L);
		flameResponse.current = Describe(target, equipment, flameResponse.exGradeOption);
		flameResponse.inventory = CharService.Load(db, request.characterId).inventory;
		return flameResponse;
	}

	private static List<FlameLine> Describe(DbRow target, DbRow equipment, long packed)
	{
		return NativeGrowthStats.DescribeFlame(new InventoryEntry
		{
			itemId = DbStore.Int(target, "itemid"),
			exGradeOption = packed,
			equipmentData = new Dictionary<string, object> { ["iIncReq"] = DbStore.Int(equipment, "iIncReq") }
		});
	}

	public static List<FlameLine> Roll(FlameRule rule, int reqLevel, int incReq, bool weapon, bool bossWeapon, int baseAttack, Random random)
	{
		FlameCatalog.Weights weights = new FlameCatalog.Weights(rule.lineWeights);
		FlameCatalog.Weights weights2 = new FlameCatalog.Weights(rule.tierWeights);
		int num = (bossWeapon ? 4 : weights.Pick(random.Next(weights.total)));
		List<FlameLine> list = new List<FlameLine>();
		HashSet<int> hashSet = new HashSet<int>();
		int num2 = 0;
		while (list.Count < num && num2++ < 200)
		{
			int num3 = random.Next(21);
			if (!hashSet.Add(num3))
			{
				continue;
			}
			if (!FlameCatalog.Allowed(num3, weapon, reqLevel, incReq))
			{
				hashSet.Remove(num3);
				continue;
			}
			int num4 = weights2.Pick(random.Next(weights2.total));
			if (bossWeapon)
			{
				num4 = Math.Min(7, num4 + 2);
			}
			FlameStatDef flameStatDef = FlameStats.ByVal(num3);
			if (flameStatDef != null)
			{
				list.Add(new FlameLine
				{
					statVal = num3,
					exGrade = flameStatDef.exGrade,
					tier = num4,
					label = flameStatDef.label,
					value = FlameCatalog.Value(num3, num4, reqLevel, incReq, weapon, bossWeapon, baseAttack)
				});
			}
		}
		return list;
	}

	public static long Pack(List<FlameLine> lines)
	{
		long num = 0L;
		long num2 = 1L;
		foreach (FlameLine line in lines)
		{
			num += (line.exGrade * 10 + line.tier) * num2;
			num2 *= 1000;
		}
		return num;
	}

	public static List<FlameLine> Unpack(long packed)
	{
		List<FlameLine> list = new List<FlameLine>();
		long num = packed;
		while (num > 0)
		{
			int num2 = (int)(num % 1000);
			num /= 1000;
			if (num2 != 0)
			{
				int exGrade = num2 / 10;
				int tier = num2 % 10;
				FlameStatDef flameStatDef = FlameStats.ByExGrade(exGrade);
				list.Add(new FlameLine
				{
					exGrade = exGrade,
					tier = tier,
					statVal = (flameStatDef?.val ?? (-1)),
					label = ((flameStatDef == null) ? "" : flameStatDef.label)
				});
			}
		}
		return list;
	}

	private static string Serialize(List<FlameLine> lines)
	{
		return Pack(lines).ToString(CultureInfo.InvariantCulture);
	}

	private static List<FlameLine> Deserialize(string packed)
	{
		if (!long.TryParse(packed, out var result))
		{
			return new List<FlameLine>();
		}
		return Unpack(result);
	}
}
