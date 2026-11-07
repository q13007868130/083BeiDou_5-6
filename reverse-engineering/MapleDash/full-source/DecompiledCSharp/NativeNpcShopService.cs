using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;

public sealed class NativeNpcShopService
{
	private readonly DbStore db;

	private readonly Dictionary<int, int> sessions = new Dictionary<int, int>();

	private readonly Dictionary<int, int> sessionMaps = new Dictionary<int, int>();

	private readonly Dictionary<int, int> sessionSpeakers = new Dictionary<int, int>();

	public const int AndroidShopId = 9201060;

	private bool seeded;

	public Func<int, BotActor> Player;

	private BotItemCatalog Catalog => BotItemCatalog.Shared;

	public NativeNpcShopService(DbStore db)
	{
		this.db = db;
	}

	public void EndSession(int id)
	{
		sessions.Remove(id);
		sessionMaps.Remove(id);
		sessionSpeakers.Remove(id);
	}

	private void Seed()
	{
		if (seeded)
		{
			return;
		}
		if (GameConfigDao.Get(db, "native_npc_shops_seed_v1") != "1")
		{
			if (!(MiniJsonDb.Parse(PlatformRes.LoadText("Shop/NpcShops")) is Dictionary<string, object> dictionary))
			{
				throw new InvalidOperationException("Original NPC shop seed is missing");
			}
			db.BeginTransaction();
			try
			{
				HashSet<int> hashSet = new HashSet<int>();
				foreach (DbRow item in from _ in db.Table("shops")
					select true)
				{
					hashSet.Add(DbStore.Int(item, "shopid"));
				}
				foreach (DbRow item2 in from _ in db.Table("shopitems")
					select true)
				{
					hashSet.Add(DbStore.Int(item2, "shopid"));
				}
				foreach (object item3 in (List<object>)dictionary["shops"])
				{
					Dictionary<string, object> dictionary2 = (Dictionary<string, object>)item3;
					int shop = Convert.ToInt32(dictionary2["shopid"]);
					if (db.Table("shops").Find((DbRow r) => DbStore.Int(r, "shopid") == shop) == null)
					{
						db.Table("shops").Insert(new DbRow
						{
							["shopid"] = dictionary2["shopid"],
							["npcid"] = dictionary2["npcid"]
						});
					}
				}
				foreach (Dictionary<string, object> item4 in (List<object>)dictionary["shopitems"])
				{
					if (hashSet.Contains(Convert.ToInt32(item4["shopid"])))
					{
						continue;
					}
					DbRow dbRow = new DbRow();
					foreach (KeyValuePair<string, object> item5 in item4)
					{
						if (item5.Key != "shopitemid")
						{
							dbRow[item5.Key] = item5.Value;
						}
					}
					db.Table("shopitems").Insert(dbRow);
				}
				db.Table("game_config").Insert(new DbRow
				{
					["config_code"] = "native_npc_shops_seed_v1",
					["config_value"] = "1"
				});
				db.Commit();
				db.Save();
			}
			catch
			{
				db.Rollback();
				throw;
			}
		}
		seeded = true;
	}

	public bool HasNpc(int id)
	{
		lock (db)
		{
			Seed();
			return db.Table("shops").Find((DbRow r) => DbStore.Int(r, "npcid") == id) != null;
		}
	}

	private List<NpcShopItem> Stock(int shop)
	{
		List<DbRow> list = from r in db.Table("shopitems")
			select DbStore.Int(r, "shopid") == shop;
		list.Sort((DbRow a, DbRow b) => DbStore.Int(b, "position").CompareTo(DbStore.Int(a, "position")));
		List<NpcShopItem> list2 = new List<NpcShopItem>();
		List<int> list3 = new List<int>();
		for (int num = 2070000; num <= 2070016; num++)
		{
			if (num != 2070014)
			{
				list3.Add(num);
			}
		}
		list3.Add(2331000);
		list3.Add(2332000);
		list3.Add(2070018);
		for (int num2 = 2330000; num2 <= 2330005; num2++)
		{
			list3.Add(num2);
		}
		foreach (DbRow item in list)
		{
			int num3 = DbStore.Int(item, "itemid");
			list2.Add(new NpcShopItem
			{
				itemId = num3,
				price = DbStore.Int(item, "price"),
				pitch = DbStore.Int(item, "pitch")
			});
			list3.Remove(num3);
		}
		foreach (int item2 in list3)
		{
			list2.Add(new NpcShopItem
			{
				itemId = item2
			});
		}
		return list2;
	}

	public NpcShopResponse Handle(NpcShopRequest r)
	{
		lock (db)
		{
			Seed();
			FullCharacter c = CharService.Load(db, r.characterId);
			if (c == null)
			{
				return new NpcShopResponse
				{
					ok = false,
					error = "角色不存在"
				};
			}
			string text = null;
			BotActor botActor = Player?.Invoke(r.characterId);
			if (botActor?.trade != null && r.operation != NpcShopOperation.Close)
			{
				return new NpcShopResponse
				{
					ok = false,
					error = "交易中不能打开商店"
				};
			}
			int value;
			if (r.operation == NpcShopOperation.Close)
			{
				EndSession(r.characterId);
			}
			else if (r.operation == NpcShopOperation.OpenNpc || r.operation == NpcShopOperation.OpenShop)
			{
				DbRow dbRow = db.Table("shops").Find((DbRow x) => DbStore.Int(x, (r.operation == NpcShopOperation.OpenNpc) ? "npcid" : "shopid") == r.id);
				if (dbRow == null && r.fallback)
				{
					dbRow = db.Table("shops").Find((DbRow x) => DbStore.Int(x, "shopid") == 11000);
				}
				EndSession(r.characterId);
				if (dbRow == null)
				{
					text = "这位 NPC 没有商店";
				}
				else
				{
					sessions[r.characterId] = DbStore.Int(dbRow, "shopid");
					sessionMaps[r.characterId] = c.record.map;
					if (r.npcTemplateId != 0)
					{
						AndroidState androidState = NativeAndroidService.Resolve(r.characterId, (int slot) => c.inventory.Find((InventoryEntry x) => x.inventoryType == 1 && x.position == slot)?.itemId ?? 0);
						if (!androidState.visible || androidState.templateId != r.npcTemplateId)
						{
							text = "机器人未召出";
							EndSession(r.characterId);
						}
						else
						{
							sessionSpeakers[r.characterId] = r.npcTemplateId;
						}
					}
					else
					{
						sessionSpeakers.Remove(r.characterId);
					}
				}
			}
			else if (!sessions.TryGetValue(r.characterId, out value) || sessionMaps[r.characterId] != c.record.map)
			{
				EndSession(r.characterId);
				text = "请重新打开商店";
			}
			else
			{
				BotActor actor = new BotActor
				{
					id = r.characterId,
					job = c.record.job,
					character = c
				};
				BotTradeService botTradeService = new BotTradeService(db, Catalog);
				db.BeginTransaction();
				try
				{
					text = Apply(actor, botTradeService.Rules, Stock(value), r);
					if (text == null)
					{
						botTradeService.Persist(actor);
						db.Commit();
						db.Save();
						if (botActor?.character != null)
						{
							botActor.character.inventory = c.inventory;
							botActor.character.record.meso = c.record.meso;
						}
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
			}
			NpcShopResponse npcShopResponse = new NpcShopResponse
			{
				ok = (text == null),
				error = (text ?? ""),
				character = CharService.Load(db, r.characterId)
			};
			if (sessions.TryGetValue(r.characterId, out var current))
			{
				DbRow r2 = db.Table("shops").Find((DbRow x) => DbStore.Int(x, "shopid") == current);
				npcShopResponse.opened = true;
				npcShopResponse.shopId = current;
				npcShopResponse.npcId = DbStore.Int(r2, "npcid");
				npcShopResponse.androidTemplateId = (sessionSpeakers.TryGetValue(r.characterId, out var value2) ? value2 : 0);
				npcShopResponse.mapId = sessionMaps[r.characterId];
				npcShopResponse.items = Stock(current);
				foreach (InventoryEntry item in npcShopResponse.character.inventory)
				{
					if (InventoryDao.IsBagPosition(item.position))
					{
						npcShopResponse.sellPrices[item.inventoryType * 1000 + item.position] = SellPrice(item.itemId, (!TradeItemRules.Rechargeable(item.itemId)) ? 1 : item.quantity);
					}
				}
			}
			return npcShopResponse;
		}
	}

	private string Apply(BotActor actor, TradeItemRules rules, List<NpcShopItem> stock, NpcShopRequest r)
	{
		FullCharacter character = actor.character;
		List<InventoryEntry> inventory = character.inventory;
		if (character.record.hp <= 0)
		{
			return "当前状态不能交易";
		}
		int num = r.quantity;
		if (r.operation == NpcShopOperation.Buy)
		{
			if (r.index < 0 || r.index >= stock.Count || stock[r.index].itemId != r.itemId || num < 1 || num > 32767)
			{
				return "商品或数量无效";
			}
			NpcShopItem npcShopItem = stock[r.index];
			if (Catalog.Info(npcShopItem.itemId) == null)
			{
				return "道具资源缺失";
			}
			if (npcShopItem.itemId / 1000000 == 1 && num != 1)
			{
				return "装备每次购买一件";
			}
			bool flag = TradeItemRules.Rechargeable(npcShopItem.itemId);
			long num2 = ((npcShopItem.price > 0) ? Math.Min((long)((float)npcShopItem.price * (float)num), 2147483647L) : Math.Min((long)((float)npcShopItem.pitch * (float)num), 2147483647L));
			if (npcShopItem.price <= 0 && npcShopItem.pitch <= 0)
			{
				return "此道具仅提供补充服务";
			}
			if (npcShopItem.price > 0 && character.record.meso < num2)
			{
				return "金币不足";
			}
			if (npcShopItem.price <= 0)
			{
				long num3 = 0L;
				foreach (InventoryEntry item in inventory)
				{
					if (item.inventoryType == 4 && item.position > 0 && item.itemId == 4310000)
					{
						num3 += item.quantity;
					}
				}
				if (num3 < num2)
				{
					return "音符数量不足";
				}
			}
			if (!rules.Receive(actor, inventory, Catalog.CleanItem(npcShopItem.itemId, flag ? rules.SlotMax(npcShopItem.itemId, actor) : num)))
			{
				return "背包空间不足或道具已达持有上限";
			}
			if (npcShopItem.price > 0)
			{
				character.record.meso -= (int)(flag ? npcShopItem.price : num2);
			}
			else
			{
				foreach (InventoryEntry item2 in new List<InventoryEntry>(inventory))
				{
					if (num2 == 0L)
					{
						break;
					}
					if (item2.itemId == 4310000 && item2.inventoryType == 4 && item2.position > 0)
					{
						int num4 = (int)Math.Min(num2, item2.quantity);
						item2.quantity -= num4;
						num2 -= num4;
						if (item2.quantity == 0)
						{
							inventory.Remove(item2);
						}
					}
				}
			}
			return null;
		}
		InventoryEntry inventoryEntry = inventory.Find((InventoryEntry i) => i.inventoryType == r.inventoryType && i.position == r.position && InventoryDao.IsBagPosition(i.position));
		if (inventoryEntry == null)
		{
			return "该背包槽位没有道具";
		}
		if (r.operation == NpcShopOperation.Recharge)
		{
			if (inventoryEntry.inventoryType != 2 || !TradeItemRules.Rechargeable(inventoryEntry.itemId) || inventoryEntry.quantity < 0)
			{
				return "不能补充此道具";
			}
			int num5 = rules.SlotMax(inventoryEntry.itemId, actor);
			int num6 = (int)Math.Ceiling(UnitPrice(inventoryEntry.itemId) * (double)Math.Max(0, num5 - inventoryEntry.quantity));
			if (num6 < 0 || character.record.meso < num6)
			{
				return "金币不足";
			}
			if (inventoryEntry.quantity < num5)
			{
				inventoryEntry.quantity = num5;
				character.record.meso -= num6;
			}
			return null;
		}
		if (r.operation != NpcShopOperation.Sell || r.inventoryType > 5)
		{
			return "无效商店操作";
		}
		if (NativePotentialService.BlocksTransfer(db, inventoryEntry))
		{
			return "潜能结果待选装备或装备痕迹不能出售";
		}
		if (num == 0 || num == 65535)
		{
			num = 1;
		}
		if (num < 1 || num > 32767 || inventoryEntry.quantity < 0)
		{
			return "出售数量无效";
		}
		if (TradeItemRules.Rechargeable(inventoryEntry.itemId))
		{
			num = inventoryEntry.quantity;
		}
		else if (num > inventoryEntry.quantity)
		{
			return "道具数量不足";
		}
		int num7 = SellPrice(inventoryEntry.itemId, num);
		if (num7 > 0 && character.record.meso + num7 > int.MaxValue)
		{
			return "金币已达上限";
		}
		inventoryEntry.quantity -= num;
		if (inventoryEntry.quantity == 0)
		{
			inventory.Remove(inventoryEntry);
		}
		if (num7 > 0)
		{
			character.record.meso += num7;
		}
		return null;
	}

	public int SellPrice(int id, int quantity)
	{
		int num = BotItemCatalog.Int(Catalog.Info(id), "price", -1);
		if (num != -1)
		{
			if (!TradeItemRules.Rechargeable(id))
			{
				return num * quantity;
			}
			return num + (int)Math.Ceiling((double)quantity * UnitPrice(id));
		}
		return -1;
	}

	public double UnitPrice(int id)
	{
		Dictionary<string, object> dictionary = Catalog.Info(id);
		if (dictionary == null || !dictionary.TryGetValue("unitPrice", out var value))
		{
			return 0.0;
		}
		double num = Convert.ToDouble(value);
		double num2 = Math.Floor(num);
		double num3 = num - num2;
		double num4 = 0.0;
		double num5 = 1.0;
		int num6 = 1;
		double num7;
		do
		{
			num7 = num5;
			num5 /= 2.0;
			if (num3 == num5)
			{
				break;
			}
			if (num3 > num5)
			{
				num4 += num5;
				num3 -= num5;
			}
			num6++;
		}
		while (num6 <= 5);
		if (num6 > 5)
		{
			num7 = num5;
			num5 = 0.0;
		}
		return num2 + num4 + ((Math.Abs(num3 - num5) < Math.Abs(num3 - num7)) ? num5 : num7);
	}
}
