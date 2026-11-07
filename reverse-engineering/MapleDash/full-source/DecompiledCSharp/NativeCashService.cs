using System;
using System.Collections.Generic;
using Server.Bot;

public sealed class NativeCashService
{
	private readonly DbStore db;

	private readonly HashSet<int> opened = new HashSet<int>();

	public Func<int, bool> CanEnter;

	private bool imported;

	public static Func<int, int> SurprisePick;

	private int surpriseRewardItem;

	private int surpriseRewardSn;

	public INativeCashCatalog Catalog { get; set; }

	private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public NativeCashService(DbStore db)
	{
		this.db = db;
	}

	public void EndSession(int id)
	{
		opened.Remove(id);
	}

	private int Group(CharRecord c)
	{
		if (!GameConfigDao.GetBool(db, "use_joint_cash_shop_inventory"))
		{
			if (c.job / 1000 != 1)
			{
				if (c.job / 1000 != 2)
				{
					return 3;
				}
				return 5;
			}
			return 4;
		}
		return 7;
	}

	private bool Own(DbRow row, CharRecord c)
	{
		if (DbStore.Int(row, "accountid") == c.accountid && DbStore.Int(row, "world") == c.world)
		{
			return DbStore.Int(row, "jobgroup") == Group(c);
		}
		return false;
	}

	private List<DbRow> Stock(CharRecord c)
	{
		return from r in db.Table("cash_items")
			select Own(r, c) && DbStore.Int(r, "characterid") == 0;
	}

	private static string Currency(int type)
	{
		return type switch
		{
			4 => "nxPrepaid", 
			2 => "maplePoint", 
			1 => "nxCredit", 
			_ => null, 
		};
	}

	public CashCommodity Commodity(int sn)
	{
		return Commodity(db.Table("modified_cash_item").Find((DbRow r) => DbStore.Int(r, "sn") == sn), sn);
	}

	public CashCommodity Commodity(DbRow row, int sn)
	{
		CashCommodity cashCommodity = Catalog?.Find(sn)?.Copy();
		if (row == null)
		{
			return cashCommodity;
		}
		if (cashCommodity == null)
		{
			cashCommodity = new CashCommodity
			{
				sn = sn
			};
		}
		cashCommodity.itemId = Override(row, "item_id", cashCommodity.itemId);
		cashCommodity.price = Override(row, "price", cashCommodity.price);
		cashCommodity.count = Override(row, "count", cashCommodity.count);
		cashCommodity.period = Override(row, "period", cashCommodity.period);
		cashCommodity.onSale = Override(row, "on_sale", cashCommodity.onSale);
		cashCommodity.gender = Override(row, "commodity_gender", cashCommodity.gender);
		cashCommodity.priority = Override(row, "priority", cashCommodity.priority);
		cashCommodity.bonus = Override(row, "bonus", cashCommodity.bonus);
		cashCommodity.maplePoint = Override(row, "maple_point", cashCommodity.maplePoint);
		cashCommodity.meso = Override(row, "meso", cashCommodity.meso);
		cashCommodity.forPremiumUser = Override(row, "for_premium_user", cashCommodity.forPremiumUser);
		cashCommodity.clz = Override(row, "class", cashCommodity.clz);
		cashCommodity.limit = Override(row, "limit", cashCommodity.limit);
		cashCommodity.pbCash = Override(row, "pb_cash", cashCommodity.pbCash);
		cashCommodity.pbPoint = Override(row, "pb_point", cashCommodity.pbPoint);
		cashCommodity.pbGift = Override(row, "pb_gift", cashCommodity.pbGift);
		cashCommodity.packageSn = Override(row, "package_sn", cashCommodity.packageSn);
		return cashCommodity;
	}

	private static int Override(DbRow row, string key, int fallback)
	{
		if (!row.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		return Convert.ToInt32(value);
	}

	public void ImportCommodities()
	{
		if (imported || Catalog == null)
		{
			return;
		}
		imported = true;
		DbTable dbTable = db.Table("modified_cash_item");
		HashSet<int> hashSet = new HashSet<int>();
		foreach (DbRow item in dbTable.Select((DbRow x) => true))
		{
			hashSet.Add(DbStore.Int(item, "sn"));
		}
		int num = 0;
		foreach (CashCommodity item2 in Catalog.All)
		{
			if (item2.onSale == 1 && Catalog.Complete(item2.itemId) && !hashSet.Contains(item2.sn))
			{
				dbTable.Insert(new DbRow
				{
					["sn"] = (long)item2.sn,
					["item_id"] = (long)item2.itemId,
					["count"] = (long)item2.count,
					["price"] = (long)item2.price,
					["period"] = (long)item2.period,
					["on_sale"] = (long)item2.onSale,
					["commodity_gender"] = (long)item2.gender,
					["priority"] = (long)item2.priority,
					["class"] = (long)item2.clz,
					["limit"] = (long)item2.limit,
					["pb_cash"] = (long)item2.pbCash,
					["pb_point"] = (long)item2.pbPoint,
					["pb_gift"] = (long)item2.pbGift
				});
				num++;
			}
		}
		if (num > 0)
		{
			db.Save();
		}
	}

	public CashResponse Handle(CashRequest r)
	{
		lock (db)
		{
			CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
			if (charRecord == null)
			{
				return new CashResponse
				{
					ok = false,
					error = "角色不存在"
				};
			}
			Storage(charRecord);
			ImportCommodities();
			Sweep();
			db.BeginTransaction();
			string text;
			try
			{
				text = Apply(charRecord, r);
				if (text == null)
				{
					db.Commit();
				}
				else
				{
					db.Rollback();
				}
			}
			catch
			{
				db.Rollback();
				throw;
			}
			db.Save();
			CashResponse cashResponse = Read(CharactersDao.FindById(db, r.characterId));
			cashResponse.ok = text == null;
			cashResponse.error = text ?? "";
			if (cashResponse.ok && r.operation == CashOperation.OpenSurprise)
			{
				cashResponse.rewardItemId = surpriseRewardItem;
				cashResponse.rewardSn = surpriseRewardSn;
			}
			return cashResponse;
		}
	}

	public void Sweep()
	{
		foreach (DbRow owned in from x in db.Table("cash_items")
			select DbStore.Int(x, "characterid") > 0)
		{
			DbRow dbRow = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == DbStore.Long(owned, "inventoryitemid", 0L));
			if (dbRow == null)
			{
				db.Table("cash_items").Delete((DbRow x) => DbStore.Long(x, "id", 0L) == DbStore.Long(owned, "id", 0L));
			}
			else
			{
				owned["quantity"] = (long)DbStore.Int(dbRow, "quantity");
				owned["expiration"] = DbStore.Long(dbRow, "expiration", -1L);
				owned["petid"] = DbStore.Long(dbRow, "petid", -1L);
			}
		}
		foreach (DbRow row in from x in db.Table("cash_items")
			select DbStore.Long(x, "expiration", 0L) > 0 && DbStore.Long(x, "expiration", 0L) <= Now)
		{
			int num = DbStore.Int(row, "itemid");
			int petId = DbStore.Int(row, "petid", -1);
			if (num / 1000 == 5000)
			{
				DbRow dbRow2 = db.Table("pets").Find((DbRow x) => DbStore.Int(x, "petid") == petId);
				if (dbRow2 != null)
				{
					dbRow2["summoned"] = 0L;
				}
				if (num != 5000054 && !GameConfigDao.GetBool(db, "use_erase_pet_on_expiration"))
				{
					row["expiration"] = -1L;
					long heldId = DbStore.Long(row, "inventoryitemid", 0L);
					DbRow dbRow3 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == heldId);
					if (dbRow3 != null)
					{
						dbRow3["expiration"] = -1L;
					}
					continue;
				}
				db.Table("petignores").Delete((DbRow x) => DbStore.Int(x, "petid") == petId);
				db.Table("pets").Delete((DbRow x) => DbStore.Int(x, "petid") == petId);
			}
			long num2 = DbStore.Long(row, "inventoryitemid", 0L);
			if (num2 > 0)
			{
				CharService.RemoveItem(db, num2);
			}
			db.Table("cash_items").Delete((DbRow x) => DbStore.Long(x, "id", 0L) == DbStore.Long(row, "id", 0L));
		}
	}

	private string Apply(CharRecord c, CashRequest r)
	{
		int cid = (int)c.id;
		if (r.operation == CashOperation.Open)
		{
			if (Catalog == null)
			{
				return "商城目录资源未加载";
			}
			if (c.hp <= 0 || (CanEnter != null && !CanEnter(c.map)))
			{
				return "当前状态或地图不能进入商城";
			}
			opened.Add(cid);
			string text = LoadGifts(c);
			if (text != null)
			{
				return text;
			}
			return null;
		}
		if (r.operation == CashOperation.Close)
		{
			opened.Remove(cid);
			return null;
		}
		if (r.operation == CashOperation.Read)
		{
			return null;
		}
		if (r.operation == CashOperation.AdjustBalance)
		{
			DbRow dbRow = AccountsDao.FindById(db, c.accountid);
			string text2 = Currency(r.currency);
			if (dbRow == null || text2 == null)
			{
				return "无效账号或余额类型";
			}
			long num = (long)DbStore.Int(dbRow, text2) + (long)r.amount;
			if (num < 0 || num > int.MaxValue)
			{
				return "余额不足或超出上限";
			}
			dbRow[text2] = num;
			return null;
		}
		if (!opened.Contains(cid))
		{
			return "请先进入商城";
		}
		if (r.operation == CashOperation.RedeemCode)
		{
			return new BotNXCodes(db).Redeem(c, r.message, Now);
		}
		if (Catalog == null)
		{
			return "商城目录资源未加载";
		}
		DbRow dbRow2 = AccountsDao.FindById(db, c.accountid);
		if (dbRow2 == null)
		{
			return "账号不存在";
		}
		CashCommodity cashCommodity = Commodity(r.sn);
		DbRow row;
		switch (r.operation)
		{
		case CashOperation.Buy:
		{
			string text4 = CanBuy(c, cashCommodity, dbRow2, r.currency);
			if (text4 != null)
			{
				return text4;
			}
			List<CashCommodity> list5 = Expand(cashCommodity);
			if (list5 == null || list5.Count == 0)
			{
				return "礼包目录不完整";
			}
			if (Stock(c).Count + list5.Count > 1000)
			{
				return "现金仓库已满（最多1000件）";
			}
			foreach (CashCommodity item in list5)
			{
				string text8 = ValidItem(c, item);
				if (text8 != null)
				{
					return text8;
				}
			}
			Charge(dbRow2, r.currency, cashCommodity.price);
			foreach (CashCommodity item2 in list5)
			{
				AddStock(c, item2, "");
			}
			return null;
		}
		case CashOperation.Gift:
		{
			string text4 = CanBuy(c, cashCommodity, dbRow2, 4);
			if (text4 != null)
			{
				return text4;
			}
			if (string.IsNullOrEmpty(r.message) || r.message.Length > 73)
			{
				return "赠言须为1～73个字符";
			}
			if (!int.TryParse(DbStore.Str(dbRow2, "birthday").Replace("-", ""), out var result) || result != r.birthday)
			{
				return "生日验证失败";
			}
			DbRow dbRow6 = db.Table("characters").Find((DbRow x) => string.Equals(DbStore.Str(x, "name"), r.target, StringComparison.OrdinalIgnoreCase));
			if (dbRow6 == null)
			{
				return "收件角色不存在";
			}
			if (DbStore.Int(dbRow6, "accountid") == c.accountid)
			{
				return "不能向同账号角色赠送";
			}
			if (Expand(cashCommodity) == null)
			{
				return "礼包目录不完整";
			}
			Charge(dbRow2, 4, cashCommodity.price);
			db.Table("cash_gifts").Insert(new DbRow
			{
				["characterid"] = (long)DbStore.Int(dbRow6, "id"),
				["sn"] = (long)cashCommodity.sn,
				["sender"] = c.name,
				["message"] = r.message
			});
			NativeNoteService.SendNormal(db, c.name + " 给你送了一份礼物！快去现金商城查看吧。", c.name, DbStore.Str(dbRow6, "name"));
			return null;
		}
		case CashOperation.ClaimGifts:
		{
			List<DbRow> list2 = from x in db.Table("cash_gifts")
				select DbStore.Int(x, "characterid") == c.id;
			List<KeyValuePair<CashCommodity, string>> list3 = new List<KeyValuePair<CashCommodity, string>>();
			foreach (DbRow item3 in list2)
			{
				List<CashCommodity> list4 = Expand(Commodity(DbStore.Int(item3, "sn")));
				if (list4 == null)
				{
					return "礼物目录缺失";
				}
				foreach (CashCommodity item4 in list4)
				{
					if (!Catalog.Exists(item4.itemId))
					{
						return "礼物道具数据缺失";
					}
					list3.Add(new KeyValuePair<CashCommodity, string>(item4, DbStore.Str(item3, "sender")));
				}
			}
			if (Stock(c).Count + list3.Count > 1000)
			{
				return "现金仓库没有足够空间";
			}
			foreach (KeyValuePair<CashCommodity, string> item5 in list3)
			{
				AddStock(c, item5.Key, item5.Value);
			}
			db.Table("cash_gifts").Delete((DbRow x) => DbStore.Int(x, "characterid") == cid);
			return null;
		}
		case CashOperation.Withdraw:
		{
			row = db.Table("cash_items").Find((DbRow x) => DbStore.Long(x, "id", 0L) == r.itemId && Own(x, c) && DbStore.Int(x, "characterid") == 0);
			if (row == null)
			{
				return "现金道具不存在或不属于此账号职业仓库";
			}
			int num8 = DbStore.Int(row, "itemid");
			int num9 = num8 / 1000000;
			if (num9 == 1)
			{
				num9 = 1;
			}
			int max = Limit(c, num9);
			int num10 = FreeSlot(cid, num9, max);
			if (num10 < 1)
			{
				return "对应道具栏已满";
			}
			long inventoryId = InventoryDao.InsertItem(db, cid, num8, num9, num10, DbStore.Int(row, "quantity"), DbStore.Int(row, "petid", -1));
			DbRow dbRow8 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == inventoryId);
			dbRow8["expiration"] = DbStore.Long(row, "expiration", 0L);
			dbRow8["giftFrom"] = DbStore.Str(row, "giftFrom");
			dbRow8["accountid"] = (long)c.accountid;
			row["petid"] = DbStore.Long(dbRow8, "petid", -1L);
			if (num9 == 1)
			{
				DbRow dbRow9 = Catalog.Equipment(num8) ?? new DbRow();
				dbRow9["inventoryitemid"] = inventoryId;
				db.Table("inventoryequipment").Insert(dbRow9);
			}
			row["characterid"] = c.id;
			row["inventoryitemid"] = inventoryId;
			return null;
		}
		case CashOperation.Deposit:
		{
			row = db.Table("cash_items").Find((DbRow x) => DbStore.Long(x, "id", 0L) == r.itemId && Own(x, c) && DbStore.Int(x, "characterid") == cid);
			if (row == null)
			{
				return "只能存入当前角色持有的现金道具";
			}
			DbRow dbRow4 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == DbStore.Long(row, "inventoryitemid", 0L) && DbStore.Int(x, "characterid") == cid);
			if (dbRow4 == null || !InventoryDao.IsBagPosition(DbStore.Int(dbRow4, "position")))
			{
				return "请先卸下该现金装备或宠物";
			}
			if (Stock(c).Count >= 1000)
			{
				return "现金仓库已满";
			}
			row["quantity"] = (long)DbStore.Int(dbRow4, "quantity");
			row["petid"] = DbStore.Long(dbRow4, "petid", -1L);
			row["expiration"] = DbStore.Long(dbRow4, "expiration", -1L);
			CharService.RemoveItem(db, DbStore.Long(row, "inventoryitemid", 0L));
			row["inventoryitemid"] = 0L;
			row["characterid"] = 0L;
			return null;
		}
		case CashOperation.Equip:
		case CashOperation.Unequip:
		{
			row = db.Table("cash_items").Find((DbRow x) => DbStore.Long(x, "id", 0L) == r.itemId && Own(x, c) && DbStore.Int(x, "characterid") == cid);
			if (row == null)
			{
				return "装备不属于当前角色";
			}
			DbRow dbRow4 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Long(x, "inventoryitemid", 0L) == DbStore.Long(row, "inventoryitemid", 0L));
			if (dbRow4 == null || DbStore.Int(dbRow4, "inventorytype") != 1)
			{
				return "这不是装备";
			}
			int itemId = DbStore.Int(dbRow4, "itemid");
			if (r.operation == CashOperation.Equip)
			{
				string text3 = Catalog.CanEquip(itemId, c);
				if (text3 != null)
				{
					return text3;
				}
				int destination = Catalog.EquipSlot(itemId);
				if (destination == 0)
				{
					return "此类装备没有可用槽位";
				}
				int num3 = DbStore.Int(dbRow4, "position");
				if (!InventoryDao.IsBagPosition(num3))
				{
					return "已经装备";
				}
				DbRow dbRow5 = db.Table("inventoryitems").Find((DbRow x) => DbStore.Int(x, "characterid") == cid && DbStore.Int(x, "inventorytype") == 1 && DbStore.Int(x, "position") == destination);
				if (dbRow5 != null)
				{
					dbRow5["position"] = (long)num3;
				}
				dbRow4["position"] = (long)destination;
			}
			else
			{
				if (InventoryDao.IsBagPosition(DbStore.Int(dbRow4, "position")))
				{
					return "该装备没有穿戴";
				}
				int num4 = FreeSlot(cid, 1, c.equipslots);
				if (num4 == 0)
				{
					return "装备栏已满";
				}
				dbRow4["position"] = (long)num4;
			}
			return null;
		}
		case CashOperation.WishList:
		{
			if (r.wishes == null || r.wishes.Length > 10)
			{
				return "愿望清单最多10项";
			}
			int[] wishes = r.wishes;
			foreach (int sn in wishes)
			{
				CashCommodity cashCommodity3 = Commodity(sn);
				if (cashCommodity3 == null || cashCommodity3.onSale != 1)
				{
					return "愿望清单包含未出售商品";
				}
			}
			db.Table("cash_wishlist").Delete((DbRow x) => DbStore.Int(x, "characterid") == cid);
			wishes = r.wishes;
			foreach (int num7 in wishes)
			{
				db.Table("cash_wishlist").Insert(new DbRow
				{
					["characterid"] = c.id,
					["sn"] = (long)num7
				});
			}
			return null;
		}
		case CashOperation.ExpandInventory:
		{
			string text6 = Currency(r.currency);
			if (text6 == null)
			{
				return "无效余额类型";
			}
			if (r.inventoryType < 1 || r.inventoryType > 4)
			{
				return "不能扩充此道具栏";
			}
			int num11 = Limit(c, r.inventoryType);
			if (num11 + 4 > 96)
			{
				return "道具栏已达上限";
			}
			if (DbStore.Int(dbRow2, text6) < 2000)
			{
				return "余额不足，扩充4格需要2000点";
			}
			Charge(dbRow2, r.currency, 2000);
			string text7;
			if (r.inventoryType == 1)
			{
				text7 = "equipslots";
			}
			else if (r.inventoryType == 2)
			{
				text7 = "useslots";
			}
			else
			{
				text7 = ((r.inventoryType == 3) ? "setupslots" : "etcslots");
			}
			db.Table("characters").Update((DbRow x) => DbStore.Int(x, "id") == cid, new DbRow { [text7] = (long)(num11 + 4) });
			return null;
		}
		case CashOperation.ExpandStorage:
		{
			string text5 = Currency(r.currency);
			if (text5 == null)
			{
				return "无效余额类型";
			}
			DbRow dbRow7 = Storage(c);
			int num5 = DbStore.Int(dbRow7, "slots");
			if (num5 + 4 > 48)
			{
				return "仓库已达48格上限";
			}
			if (DbStore.Int(dbRow2, text5) < 2000)
			{
				return "余额不足，扩充4格需要2000点";
			}
			Charge(dbRow2, r.currency, 2000);
			dbRow7["slots"] = (long)(num5 + 4);
			return null;
		}
		case CashOperation.ExpandCharacters:
		{
			string text4 = CanBuy(c, cashCommodity, dbRow2, r.currency);
			if (text4 != null)
			{
				return text4;
			}
			if (cashCommodity.itemId != 5430000)
			{
				return "该商品不是角色栏扩充券";
			}
			if (DbStore.Int(dbRow2, "characterslots") >= 15)
			{
				return "角色栏已达上限";
			}
			Charge(dbRow2, r.currency, cashCommodity.price);
			dbRow2["characterslots"] = (long)(DbStore.Int(dbRow2, "characterslots") + 1);
			return null;
		}
		case CashOperation.OpenSurprise:
		{
			DbRow dbRow3 = db.Table("cash_items").Find((DbRow x) => DbStore.Long(x, "id", 0L) == r.itemId && Own(x, c) && DbStore.Int(x, "characterid") == 0);
			if (dbRow3 == null || DbStore.Int(dbRow3, "itemid") != 5222000 || DbStore.Int(dbRow3, "quantity") <= 0)
			{
				return "没有可开启的惊喜箱";
			}
			if (Stock(c).Count >= 100)
			{
				return "现金仓库已满";
			}
			List<CashCommodity> list = new List<CashCommodity>();
			foreach (CashCommodity item6 in Catalog.All)
			{
				if (item6.onSale == 1 && item6.itemId / 10000 != 910 && Catalog.Exists(item6.itemId))
				{
					list.Add(item6);
				}
			}
			if (list.Count == 0)
			{
				return "商城目录为空";
			}
			int index = ((SurprisePick != null) ? SurprisePick(list.Count) : new Random().Next(list.Count));
			CashCommodity cashCommodity2 = list[index];
			int num2 = DbStore.Int(dbRow3, "quantity");
			if (num2 <= 1)
			{
				db.Table("cash_items").Delete((DbRow x) => DbStore.Long(x, "id", 0L) == r.itemId);
			}
			else
			{
				dbRow3["quantity"] = (long)(num2 - 1);
			}
			AddStock(c, cashCommodity2, "");
			surpriseRewardItem = cashCommodity2.itemId;
			surpriseRewardSn = cashCommodity2.sn;
			return null;
		}
		default:
			return "无效商城操作";
		}
	}

	private string LoadGifts(CharRecord c)
	{
		int cid = (int)c.id;
		int num = 0;
		foreach (DbRow gift in from x in db.Table("cash_gifts")
			select DbStore.Int(x, "characterid") == cid)
		{
			List<CashCommodity> list = Expand(Commodity(DbStore.Int(gift, "sn")));
			if (list == null || Stock(c).Count + num + list.Count > 1000)
			{
				continue;
			}
			foreach (CashCommodity item in list)
			{
				AddStock(c, item, DbStore.Str(gift, "sender"));
			}
			NativeNoteService.AvailableNotes++;
			num += list.Count;
			db.Table("cash_gifts").Delete((DbRow x) => DbStore.Int(x, "id") == DbStore.Int(gift, "id"));
		}
		if (num > 0)
		{
			db.Save();
		}
		return null;
	}

	private string CanBuy(CharRecord c, CashCommodity item, DbRow account, int currency)
	{
		string text = Currency(currency);
		if (text == null)
		{
			return "无效余额类型";
		}
		if (item == null || item.onSale != 1 || item.price < 0 || item.count < 1 || item.period < -1)
		{
			return "商品未出售或数据无效";
		}
		if (DbStore.Int(account, text) < item.price)
		{
			return "余额不足";
		}
		return ValidItem(c, item);
	}

	private string ValidItem(CharRecord c, CashCommodity item)
	{
		if (item == null || !Catalog.Exists(item.itemId))
		{
			return "道具资源缺失";
		}
		int num = item.itemId / 10000;
		if ((num == 503 || num == 514) && c.level < 16)
		{
			return "达到16级才能购买商店道具";
		}
		if ((item.itemId / 1000 == 5211 || item.itemId / 1000 == 5360) && !GameConfigDao.GetBool(db, "use_supply_rate_coupons"))
		{
			return "当前服务器不出售倍率券";
		}
		if (num == 543 && item.itemId != 5430000 && c.level < 30)
		{
			return "达到30级才能购买此道具";
		}
		return null;
	}

	private List<CashCommodity> Expand(CashCommodity item)
	{
		if (item == null)
		{
			return null;
		}
		IList<int> list = Catalog.Package(item.itemId);
		if (list == null)
		{
			if (item.itemId / 10000 != 910)
			{
				return new List<CashCommodity> { item };
			}
			return null;
		}
		List<CashCommodity> list2 = new List<CashCommodity>();
		foreach (int item2 in list)
		{
			CashCommodity cashCommodity = Commodity(item2);
			if (cashCommodity == null || Catalog.Package(cashCommodity.itemId) != null)
			{
				return null;
			}
			list2.Add(cashCommodity);
		}
		return list2;
	}

	private static void Charge(DbRow account, int currency, int price)
	{
		string text = Currency(currency);
		account[text] = (long)(DbStore.Int(account, text) - price);
	}

	private void AddStock(CharRecord c, CashCommodity item, string sender)
	{
		db.Table("cash_items").Insert(new DbRow
		{
			["accountid"] = (long)c.accountid,
			["world"] = (long)c.world,
			["jobgroup"] = (long)Group(c),
			["sn"] = (long)item.sn,
			["itemid"] = (long)item.itemId,
			["quantity"] = (long)item.count,
			["expiration"] = Expiration(item, Now),
			["giftFrom"] = sender
		});
	}

	public static long Expiration(CashCommodity item, long now)
	{
		if (item.period == -1)
		{
			return -1L;
		}
		if (item.period == 1)
		{
			if (item.itemId == 5211048 || item.itemId == 5360042)
			{
				return now + 14400000;
			}
			if (item.itemId == 5211901 || item.itemId == 5360901 || item.itemId == 5211902 || item.itemId == 5360902)
			{
				return now + 1800000;
			}
			if (item.itemId == 5211060)
			{
				return now + 7200000;
			}
		}
		return now + (long)((item.period == 0) ? 90 : item.period) * 86400000L;
	}

	private int Limit(CharRecord c, int type)
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

	private int FreeSlot(int cid, int type, int max)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, cid))
		{
			if (DbStore.Int(item, "inventorytype") == type)
			{
				hashSet.Add(DbStore.Int(item, "position"));
			}
		}
		for (int i = 1; i <= max; i++)
		{
			if (!hashSet.Contains(i))
			{
				return i;
			}
		}
		return 0;
	}

	private DbRow Storage(CharRecord c)
	{
		DbTable dbTable = db.Table("storages");
		DbRow dbRow = dbTable.Find((DbRow row) => DbStore.Int(row, "accountid") == c.accountid && DbStore.Int(row, "world") == c.world);
		if (dbRow != null)
		{
			return dbRow;
		}
		long id = dbTable.Insert(new DbRow
		{
			["accountid"] = (long)c.accountid,
			["world"] = (long)c.world,
			["slots"] = 4L,
			["meso"] = 0L
		});
		return dbTable.Find((DbRow row) => DbStore.Long(row, "storageid", 0L) == id);
	}

	private CashResponse Read(CharRecord c)
	{
		DbRow r = AccountsDao.FindById(db, c.accountid);
		CashResponse cashResponse = new CashResponse
		{
			storageSlots = DbStore.Int(Storage(c), "slots"),
			opened = opened.Contains((int)c.id),
			credit = DbStore.Int(r, "nxCredit"),
			points = DbStore.Int(r, "maplePoint"),
			prepaid = DbStore.Int(r, "nxPrepaid"),
			character = CharService.Load(db, (int)c.id)
		};
		if (Catalog != null)
		{
			foreach (DbRow item in from x in db.Table("modified_cash_item")
				select true)
			{
				CashCommodity cashCommodity = Commodity(item, DbStore.Int(item, "sn"));
				if (cashCommodity != null && cashCommodity.itemId != 0 && cashCommodity.onSale == 1 && Catalog.Complete(cashCommodity.itemId))
				{
					cashResponse.catalog.Add(cashCommodity);
				}
			}
		}
		cashResponse.catalog.Sort((CashCommodity a, CashCommodity b) => a.sn.CompareTo(b.sn));
		foreach (DbRow item2 in from x in db.Table("cash_items")
			select Own(x, c) && (DbStore.Int(x, "characterid") == 0 || DbStore.Int(x, "characterid") == c.id))
		{
			cashResponse.stock.Add(new CashStock
			{
				id = DbStore.Long(item2, "id", 0L),
				sn = DbStore.Int(item2, "sn"),
				itemId = DbStore.Int(item2, "itemid"),
				quantity = DbStore.Int(item2, "quantity"),
				expiration = DbStore.Long(item2, "expiration", 0L),
				characterId = DbStore.Int(item2, "characterid"),
				giftFrom = DbStore.Str(item2, "giftFrom")
			});
		}
		foreach (DbRow item3 in from x in db.Table("cash_wishlist")
			select DbStore.Int(x, "characterid") == c.id)
		{
			cashResponse.wishes.Add(DbStore.Int(item3, "sn"));
		}
		cashResponse.gifts = (from x in db.Table("cash_gifts")
			select DbStore.Int(x, "characterid") == c.id).Count;
		return cashResponse;
	}
}
