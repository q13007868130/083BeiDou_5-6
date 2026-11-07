using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Trade;

public static class NativeMakerService
{
	private sealed class Entry
	{
		public double rawCost;

		public int reqCost;

		public int reqLevel;

		public int reqMakerLevel;

		public readonly List<MakerReqItem> reqItems = new List<MakerReqItem>();

		public readonly List<MakerReqItem> gainItems = new List<MakerReqItem>();

		public bool Invalid => reqLevel < 0;

		public void TrimCost()
		{
			reqCost = (int)(rawCost / 1000.0);
			reqCost *= 1000;
		}
	}

	public const short InvalidRecipe = -1;

	public const short MissingItems = 1;

	public const short MissingMeso = 2;

	public const short LowLevel = 3;

	public const short LowMakerLevel = 4;

	public const short InventoryFull = 5;

	private const int ChaosScroll60 = 2049100;

	private static readonly string[] StatKeys = new string[14]
	{
		"STR", "DEX", "INT", "LUK", "MHP", "MMP", "PAD", "MAD", "PDD", "MDD",
		"ACC", "EVA", "Speed", "Jump"
	};

	private static readonly string[] StatColumns = new string[14]
	{
		"str", "dex", "int", "luk", "hp", "mp", "watk", "matk", "wdef", "mdef",
		"acc", "avoid", "speed", "jump"
	};

	public static Random Rng = new Random();

	public static MakerResponse Handle(DbStore db, MakerRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return Fail("角色不存在");
		}
		return r.operation switch
		{
			MakerOperation.Open => Open(db, charRecord), 
			MakerOperation.Craft => Craft(db, charRecord, r, 0), 
			MakerOperation.Crystal => Craft(db, charRecord, r, 3), 
			MakerOperation.Disassemble => Craft(db, charRecord, r, 4), 
			_ => Fail("未知的制作操作"), 
		};
	}

	private static MakerResponse Open(DbStore db, CharRecord c)
	{
		List<MakerRecipeView> list = new List<MakerRecipeView>();
		foreach (MakerRecipe item in MakerTable.All())
		{
			MakerRecipeView makerRecipeView = new MakerRecipeView
			{
				itemId = item.itemId,
				category = item.category,
				reqLevel = item.reqLevel,
				reqSkillLevel = item.reqSkillLevel,
				itemNum = item.itemNum,
				tuc = item.tuc,
				meso = item.meso,
				catalyst = item.catalyst
			};
			foreach (MakerReqItem reqItem in item.reqItems)
			{
				makerRecipeView.reqItems.Add(new MakerIngredient
				{
					itemId = reqItem.itemId,
					count = reqItem.count,
					held = InventoryDao.CountOf(db, (int)c.id, reqItem.itemId)
				});
			}
			list.Add(makerRecipeView);
		}
		return new MakerResponse
		{
			recipes = list,
			level = c.level,
			meso = c.meso,
			makerLevel = MakerSkillLevel(db, (int)c.id, c.job)
		};
	}

	private static MakerResponse Craft(DbStore db, CharRecord c, MakerRequest r, int type)
	{
		int characterId = (int)c.id;
		int num = r.toCreate;
		int num2 = -1;
		int pos = -1;
		List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
		int num3 = -1;
		Entry entry;
		switch (type)
		{
		case 3:
		{
			int itemId = num;
			num = MakerTable.CrystalFromLeftover(num);
			if (num == -1)
			{
				return Fail(Name(itemId) + " 无法转换为怪物结晶");
			}
			entry = new Entry
			{
				reqLevel = 0,
				reqMakerLevel = 1
			};
			entry.reqItems.Add(new MakerReqItem
			{
				itemId = itemId,
				count = 100
			});
			entry.gainItems.Add(new MakerReqItem
			{
				itemId = num,
				count = 1
			});
			break;
		}
		case 4:
		{
			pos = r.position;
			int num8 = EquipIdAt(db, characterId, pos);
			if (num8 == 0 || num8 != num)
			{
				return Fail("无法对该物品进行分解");
			}
			DbRow dbRow = InventoryDao.ItemsByCharacter(db, characterId).Find((DbRow x) => DbStore.Int(x, "inventorytype") == 1 && DbStore.Int(x, "position") == pos);
			if (dbRow != null && NativePotentialService.BlocksTransfer(db, CharService.ToInventoryEntry(db, dbRow)))
			{
				return Fail("待选方块结果的装备或装备痕迹不能分解");
			}
			num2 = num;
			int num9 = DisassembledFee(num);
			List<MakerReqItem> list3 = DisassembledItems(num);
			if (num9 <= -1 || list3.Count == 0)
			{
				return Fail(Name(num) + " 无法分解为怪物结晶");
			}
			entry = new Entry
			{
				reqLevel = 0,
				reqMakerLevel = 1
			};
			entry.rawCost = num9;
			entry.reqItems.Add(new MakerReqItem
			{
				itemId = num2,
				count = 1
			});
			entry.gainItems.AddRange(list3);
			break;
		}
		default:
			if (IsEquipment(num))
			{
				if (r.stimulant)
				{
					num3 = StimulantOf(num);
					if (!HasItem(db, characterId, num3))
					{
						num3 = -1;
					}
				}
				int num4 = Math.Min((r.reagentIds != null) ? r.reagentIds.Length : 0, ReagentSlots(num));
				for (int i = 0; i < num4; i++)
				{
					int reagentId = r.reagentIds[i];
					if (IsMakerReagent(reagentId))
					{
						int num5 = list.FindIndex((KeyValuePair<int, int> x) => x.Key == reagentId);
						if (num5 < 0)
						{
							list.Add(new KeyValuePair<int, int>(reagentId, 1));
						}
						else
						{
							list[num5] = new KeyValuePair<int, int>(reagentId, list[num5].Value + 1);
						}
					}
				}
				List<KeyValuePair<int, int>> list2 = new List<KeyValuePair<int, int>>();
				foreach (KeyValuePair<int, int> item in list)
				{
					int num6 = InventoryDao.CountOf(db, characterId, item.Key);
					if (num6 < item.Value)
					{
						list2.Add(new KeyValuePair<int, int>(item.Key, num6));
					}
				}
				foreach (KeyValuePair<int, int> pair in list2)
				{
					int num7 = list.FindIndex((KeyValuePair<int, int> x) => x.Key == pair.Key);
					if (num7 >= 0)
					{
						if (pair.Value > 0)
						{
							list[num7] = new KeyValuePair<int, int>(pair.Key, pair.Value);
						}
						else
						{
							list.RemoveAt(num7);
						}
					}
				}
				if (list.Count > 0 && !RemoveOddMakerReagents(db, num, list))
				{
					return Fail("攻击力/魔力强化宝石只能用在武器装备上");
				}
			}
			entry = GetItemCreateEntry(db, num, num3, list);
			break;
		}
		short createStatus = GetCreateStatus(db, c, entry);
		MakerResponse makerResponse = new MakerResponse
		{
			status = createStatus,
			level = c.level,
			makerLevel = MakerSkillLevel(db, characterId, c.job),
			gained = new List<MakerIngredient>()
		};
		if (createStatus != 0)
		{
			makerResponse.ok = false;
			makerResponse.error = StatusText(num, entry, createStatus);
			makerResponse.meso = c.meso;
			return makerResponse;
		}
		if (num2 != -1)
		{
			RemoveEquipAt(db, characterId, pos);
		}
		else
		{
			foreach (MakerReqItem reqItem in entry.reqItems)
			{
				InventoryDao.ConsumeItem(db, characterId, reqItem.itemId, reqItem.count);
			}
		}
		int reqCost = entry.reqCost;
		bool num10 = num3 != -1 || list.Count > 0;
		bool flag = true;
		if (!num10)
		{
			if (reqCost > 0)
			{
				c.meso -= reqCost;
			}
			foreach (MakerReqItem gainItem in entry.gainItems)
			{
				if (gainItem.count > 0)
				{
					GiveItem(db, c, gainItem.itemId, gainItem.count);
					makerResponse.gained.Add(new MakerIngredient
					{
						itemId = gainItem.itemId,
						count = gainItem.count
					});
				}
			}
		}
		else
		{
			int itemId2 = entry.gainItems[0].itemId;
			if (num3 != -1)
			{
				InventoryDao.ConsumeItem(db, characterId, num3, 1);
			}
			foreach (KeyValuePair<int, int> item2 in list)
			{
				InventoryDao.ConsumeItem(db, characterId, item2.Key, item2.Value);
			}
			if (reqCost > 0)
			{
				c.meso -= reqCost;
			}
			flag = AddBoostedMakerItem(db, c, itemId2, num3, list);
			if (flag)
			{
				makerResponse.gained.Add(new MakerIngredient
				{
					itemId = itemId2,
					count = entry.gainItems[0].count
				});
			}
		}
		CharactersDao.Save(db, c);
		makerResponse.craftSucceeded = flag;
		makerResponse.meso = c.meso;
		makerResponse.inventory = CharService.Load(db, characterId).inventory;
		return makerResponse;
	}

	public static int QuoteCost(DbStore db, int itemId, bool stimulant, int[] reagentIds)
	{
		List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>();
		if (reagentIds != null)
		{
			foreach (int reagentId in reagentIds)
			{
				if (IsMakerReagent(reagentId))
				{
					int num = list.FindIndex((KeyValuePair<int, int> x) => x.Key == reagentId);
					if (num < 0)
					{
						list.Add(new KeyValuePair<int, int>(reagentId, 1));
					}
					else
					{
						list[num] = new KeyValuePair<int, int>(reagentId, list[num].Value + 1);
					}
				}
			}
		}
		int stimulantId = (stimulant ? StimulantOf(itemId) : (-1));
		Entry itemCreateEntry = GetItemCreateEntry(db, itemId, stimulantId, list);
		if (!itemCreateEntry.Invalid)
		{
			return itemCreateEntry.reqCost;
		}
		return -1;
	}

	private static Entry GetItemCreateEntry(DbStore db, int toCreate, int stimulantId, List<KeyValuePair<int, int>> reagents)
	{
		Entry makerItemEntry = GetMakerItemEntry(toCreate);
		if (makerItemEntry.Invalid)
		{
			return makerItemEntry;
		}
		if (stimulantId != -1)
		{
			makerItemEntry.rawCost += StimulantFee(db, toCreate);
		}
		foreach (KeyValuePair<int, int> reagent in reagents)
		{
			makerItemEntry.rawCost += ReagentFee(db, toCreate, reagent.Key % 10 + 1) * (double)reagent.Value;
		}
		makerItemEntry.TrimCost();
		return makerItemEntry;
	}

	private static Entry GetMakerItemEntry(int toCreate)
	{
		MakerRecipe makerRecipe = MakerTable.Get(toCreate);
		Entry entry = new Entry();
		if (makerRecipe == null)
		{
			entry.reqLevel = -1;
			entry.reqMakerLevel = -1;
			entry.rawCost = -1.0;
			entry.gainItems.Add(new MakerReqItem
			{
				itemId = toCreate,
				count = -1
			});
			return entry;
		}
		entry.reqLevel = makerRecipe.reqLevel;
		entry.reqMakerLevel = makerRecipe.reqSkillLevel;
		entry.rawCost = makerRecipe.meso;
		entry.gainItems.Add(new MakerReqItem
		{
			itemId = toCreate,
			count = makerRecipe.itemNum
		});
		foreach (MakerReqItem reqItem in makerRecipe.reqItems)
		{
			entry.reqItems.Add(reqItem);
		}
		return entry;
	}

	private static double StimulantFee(DbStore db, int itemId)
	{
		if (!GameConfigDao.GetBool(db, "use_maker_fee_heuristics", fallback: true))
		{
			return 14000.0;
		}
		int num = EquipLevelReq(itemId);
		return EquipFamily(itemId) switch
		{
			100 => 1145.736246 * Math.Exp(0.03336832546 * (double)num), 
			105 => 2117.469118 * Math.Exp(0.03355349137 * (double)num), 
			107 => 1218.624674 * Math.Exp(0.0342266043 * (double)num), 
			108 => 2129.531152 * Math.Exp(0.03421778102 * (double)num), 
			104 => 1770.630579 * Math.Exp(0.03359768677 * (double)num), 
			106 => 1442.98837 * Math.Exp(0.03444783295 * (double)num), 
			109 => 6312.40136 * Math.Exp(0.0237929527 * (double)num), 
			_ => 4313.581428 * Math.Exp(0.03147837094 * (double)num), 
		};
	}

	private static double ReagentFee(DbStore db, int itemId, int reagentLevel)
	{
		if (!GameConfigDao.GetBool(db, "use_maker_fee_heuristics", fallback: true))
		{
			return 8000 * reagentLevel;
		}
		int num = EquipLevelReq(itemId);
		return EquipFamily(itemId) switch
		{
			100 => 5592.01613 * Math.Exp(0.02914576018 * (double)num) * (double)reagentLevel, 
			105 => 3405.23441 * Math.Exp(0.03413001038 * (double)num) * (double)reagentLevel, 
			107 => 2115.697484 * Math.Exp(0.0354881705 * (double)num) * (double)reagentLevel, 
			108 => 4684.040894 * Math.Exp(0.03166500585 * (double)num) * (double)reagentLevel, 
			104 => 2955.89017 * Math.Exp(0.0339948456 * (double)num) * (double)reagentLevel, 
			106 => 1774.722181 * Math.Exp(0.03854321409 * (double)num) * (double)reagentLevel, 
			109 => 12014.11296 * Math.Exp(0.02185471162 * (double)num) * (double)reagentLevel, 
			_ => 4538.650247 * Math.Exp(0.0371980303 * (double)num) * (double)reagentLevel, 
		};
	}

	private static int EquipFamily(int itemId)
	{
		int num = itemId / 100000;
		if (num != 13 && num != 14)
		{
			return itemId / 10000;
		}
		return itemId / 1000;
	}

	private static int EquipLevelReq(int itemId)
	{
		return BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId), "reqLevel");
	}

	private static int ReagentSlots(int itemId)
	{
		if (BotItemCatalog.Shared.Info(itemId) == null)
		{
			return 0;
		}
		int num = EquipLevelReq(itemId);
		if (num < 78)
		{
			return 1;
		}
		if (num >= 108)
		{
			return 3;
		}
		return 2;
	}

	private static int StimulantOf(int itemId)
	{
		return MakerTable.Get(itemId)?.catalyst ?? (-1);
	}

	private static int DisassembledFee(int itemId)
	{
		MakerRecipe makerRecipe = MakerTable.Get(itemId);
		if (makerRecipe == null)
		{
			return -1;
		}
		return (int)((float)((double)makerRecipe.meso * 0.13636363636364) / 1000f) * 1000;
	}

	private static List<MakerReqItem> DisassembledItems(int itemId)
	{
		List<MakerReqItem> list = new List<MakerReqItem>();
		MakerRecipe makerRecipe = MakerTable.Get(itemId);
		if (makerRecipe == null)
		{
			return list;
		}
		foreach (MakerReqItem reqItem in makerRecipe.reqItems)
		{
			if (reqItem.itemId >= 4260000 && reqItem.itemId < 4270000)
			{
				list.Add(new MakerReqItem
				{
					itemId = reqItem.itemId,
					count = reqItem.count / 2
				});
			}
		}
		return list;
	}

	private static bool RemoveOddMakerReagents(DbStore db, int toCreate, List<KeyValuePair<int, int>> reagents)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		List<int> list = new List<int>();
		bool flag = IsWeapon(toCreate) || GameConfigDao.GetBool(db, "use_maker_permissive_atk_up");
		foreach (KeyValuePair<int, int> reagent in reagents)
		{
			int key = reagent.Key;
			int num = reagent.Key / 100;
			if (num < 42502 && !flag)
			{
				return false;
			}
			if (dictionary.TryGetValue(num, out var value))
			{
				if (value < key)
				{
					list.Add(value);
					dictionary[num] = key;
				}
				else
				{
					list.Add(key);
				}
			}
			else
			{
				dictionary[num] = key;
			}
		}
		foreach (int id in list)
		{
			reagents.RemoveAll((KeyValuePair<int, int> x) => x.Key == id);
		}
		for (int num2 = 0; num2 < reagents.Count; num2++)
		{
			reagents[num2] = new KeyValuePair<int, int>(reagents[num2].Key, 1);
		}
		return true;
	}

	private static short GetCreateStatus(DbStore db, CharRecord c, Entry recipe)
	{
		if (recipe.Invalid)
		{
			return -1;
		}
		if (!HasItems(db, c, recipe))
		{
			return 1;
		}
		if (c.meso < recipe.reqCost)
		{
			return 2;
		}
		if (c.level < recipe.reqLevel)
		{
			return 3;
		}
		if (MakerSkillLevel(db, (int)c.id, c.job) < recipe.reqMakerLevel)
		{
			return 4;
		}
		if (!CanHoldAllAfterRemoving(db, c, recipe))
		{
			return 5;
		}
		return 0;
	}

	private static bool HasItems(DbStore db, CharRecord c, Entry recipe)
	{
		foreach (MakerReqItem reqItem in recipe.reqItems)
		{
			if (InventoryDao.CountOf(db, (int)c.id, reqItem.itemId) < reqItem.count)
			{
				return false;
			}
		}
		return true;
	}

	private static bool CanHoldAllAfterRemoving(DbStore db, CharRecord c, Entry recipe)
	{
		FullCharacter fullCharacter = CharService.Load(db, (int)c.id);
		if (fullCharacter == null || fullCharacter.inventory == null)
		{
			return false;
		}
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
		foreach (MakerReqItem reqItem in recipe.reqItems)
		{
			int num = reqItem.count;
			foreach (InventoryEntry item2 in list)
			{
				if (num <= 0)
				{
					break;
				}
				if (InventoryDao.IsBagPosition(item2.position) && item2.itemId == reqItem.itemId)
				{
					int num2 = Math.Min(num, item2.quantity);
					item2.quantity -= num2;
					num -= num2;
				}
			}
			list.RemoveAll((InventoryEntry i) => i.quantity <= 0);
		}
		TradeItemRules tradeItemRules = new TradeItemRules(db, BotItemCatalog.Shared);
		foreach (MakerReqItem gainItem in recipe.gainItems)
		{
			if (gainItem.itemId <= 0 || gainItem.count <= 0)
			{
				continue;
			}
			if (gainItem.itemId / 1000000 == 1 || gainItem.itemId / 1000 == 5000)
			{
				for (int num3 = 0; num3 < gainItem.count; num3++)
				{
					InventoryEntry inventoryEntry = BotItemCatalog.Shared.CleanItem(gainItem.itemId);
					if (gainItem.itemId / 1000 == 5000)
					{
						inventoryEntry.petId = 0;
					}
					if (!tradeItemRules.Receive(actor, list, inventoryEntry))
					{
						return false;
					}
				}
			}
			else if (!tradeItemRules.Receive(actor, list, BotItemCatalog.Shared.CleanItem(gainItem.itemId, gainItem.count)))
			{
				return false;
			}
		}
		return true;
	}

	private static string StatusText(int toCreate, Entry recipe, short status)
	{
		return status switch
		{
			-1 => "该物品无法用制作技能制作", 
			1 => "背包里缺少制作 " + Name(toCreate) + " 所需的材料", 
			2 => "金币不足（需要 " + recipe.reqCost.ToString("N0") + "）", 
			3 => "等级不足，无法制作", 
			4 => "制作技能等级不足", 
			5 => "背包已满", 
			_ => "", 
		};
	}

	public static int MakerSkillLevel(DbStore db, int characterId, int job)
	{
		int num = job / 1000 * 10000000 + 1007;
		foreach (DbRow item in SkillsDao.ByCharacter(db, characterId))
		{
			if (DbStore.Int(item, "skillid") == num)
			{
				return DbStore.Int(item, "skilllevel");
			}
		}
		return 0;
	}

	private static bool AddBoostedMakerItem(DbStore db, CharRecord c, int itemId, int stimulantId, List<KeyValuePair<int, int>> reagents)
	{
		if (stimulantId != -1 && !RollSuccessChance(db, 90.0))
		{
			return false;
		}
		Dictionary<string, object> dictionary = BotItemCatalog.Shared.Info(itemId);
		if (dictionary == null)
		{
			return false;
		}
		Dictionary<string, int> stats = BotItemCatalog.Shared.EquipStats(itemId) ?? new Dictionary<string, int>();
		int num = BotItemCatalog.Int(dictionary, "tuc");
		if (IsAccessory(itemId) && num <= 0)
		{
			num = 3;
		}
		if (GameConfigDao.GetBool(db, "use_enhanced_crafting"))
		{
			if (c.gm <= 0 || !GameConfigDao.GetBool(db, "use_perfect_gm_scroll", fallback: true))
			{
				num++;
			}
			Dictionary<string, int> dictionary2 = BotItemCatalog.Shared.EquipStats(2049100);
			double percent = ((dictionary2 != null && dictionary2.TryGetValue("success", out var value)) ? value : 60);
			if (RollSuccessChance(db, percent))
			{
				int range = GameConfigDao.GetInt(db, "chaos_scroll_stat_range", 5);
				string[] statKeys = StatKeys;
				foreach (string key in statKeys)
				{
					ChaosRoll(db, stats, key, range);
				}
			}
			num--;
		}
		if (reagents.Count > 0)
		{
			Dictionary<string, int> dictionary3 = new Dictionary<string, int>();
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			foreach (KeyValuePair<int, int> reagent in reagents)
			{
				MakerTable.MakerStatUp makerStatUp = MakerTable.Reagent(reagent.Key);
				if (makerStatUp == null || string.IsNullOrEmpty(makerStatUp.stat) || makerStatUp.stat.Length < 4)
				{
					continue;
				}
				if (makerStatUp.stat.Substring(0, 4).Contains("rand"))
				{
					if (makerStatUp.stat.Substring(4) == "Stat")
					{
						list.Add(makerStatUp.value * reagent.Value);
					}
					else
					{
						list2.Add(makerStatUp.value * reagent.Value);
					}
					continue;
				}
				string text = makerStatUp.stat.Substring(3);
				switch (text)
				{
				case "MaxHP":
					text = "MHP";
					break;
				case "MaxMP":
					text = "MMP";
					break;
				case "ReqLevel":
					continue;
				}
				dictionary3[text] = (dictionary3.TryGetValue(text, out var value2) ? value2 : 0) + makerStatUp.value * reagent.Value;
			}
			ImproveEquipStats(stats, dictionary3);
			foreach (int item in list)
			{
				ChaosOption(db, stats, item, option: false);
			}
			foreach (int item2 in list2)
			{
				ChaosOption(db, stats, item2, option: true);
			}
		}
		if (stimulantId != -1)
		{
			RandomizeUpgradeStats(stats);
		}
		InsertEquip(db, c, itemId, stats, num);
		return true;
	}

	private static void ImproveEquipStats(Dictionary<string, int> stats, Dictionary<string, int> deltas)
	{
		foreach (KeyValuePair<string, int> delta in deltas)
		{
			if (Array.IndexOf(StatKeys, delta.Key) >= 0)
			{
				stats.TryGetValue(delta.Key, out var value);
				stats[delta.Key] = Math.Min(32767, value + delta.Value);
			}
		}
	}

	private static void ChaosOption(DbStore db, Dictionary<string, int> stats, int range, bool option)
	{
		string[] array = ((!option) ? new string[6] { "STR", "DEX", "INT", "LUK", "ACC", "EVA" } : new string[8] { "PAD", "PDD", "MAD", "MDD", "Speed", "Jump", "MHP", "MMP" });
		foreach (string key in array)
		{
			ChaosRoll(db, stats, key, range);
		}
	}

	private static void ChaosRoll(DbStore db, Dictionary<string, int> stats, string key, int range)
	{
		if (stats.TryGetValue(key, out var value) && value > 0)
		{
			int num = value + Rng.Next(-range, range + 1);
			if (!GameConfigDao.GetBool(db, "use_enhanced_chaos_scroll") && num < 0)
			{
				num = 0;
			}
			stats[key] = Math.Min(32767, num);
		}
	}

	private static void RandomizeUpgradeStats(Dictionary<string, int> stats)
	{
		string[] array = new string[9] { "STR", "DEX", "INT", "LUK", "MAD", "PAD", "ACC", "EVA", "Jump" };
		string[] array2 = new string[4] { "PDD", "MDD", "MHP", "MMP" };
		string[] array3 = array;
		foreach (string key in array3)
		{
			RandUpgradedStat(stats, key, 2);
		}
		array3 = array2;
		foreach (string key2 in array3)
		{
			RandUpgradedStat(stats, key2, 5);
		}
	}

	private static void RandUpgradedStat(Dictionary<string, int> stats, string key, int maxRange)
	{
		if (stats.TryGetValue(key, out var value) && value != 0)
		{
			stats[key] = (int)((double)value + Math.Floor(Rng.NextDouble() * (double)(maxRange + 1)));
		}
	}

	private static bool RollSuccessChance(DbStore db, double percent)
	{
		return Rng.NextDouble() >= Math.Pow(1.0 - percent / 100.0, GameConfigDao.GetInt(db, "scroll_chance_rolls", 1));
	}

	private static void InsertEquip(DbStore db, CharRecord c, int itemId, Dictionary<string, int> stats, int slots)
	{
		int num = NativeInventoryService.Free(db, c, 1);
		if (num == 0)
		{
			throw new InvalidOperationException("Inventory capacity preflight mismatch");
		}
		long inventoryItemId = InventoryDao.InsertItem(db, (int)c.id, itemId, 1, num, 1);
		InventoryDao.InsertEquipment(db, inventoryItemId);
		DbRow dbRow = InventoryDao.EquipmentOf(db, inventoryItemId);
		for (int i = 0; i < StatColumns.Length; i++)
		{
			dbRow[StatColumns[i]] = (long)(short)(stats.TryGetValue(StatKeys[i], out var value) ? value : 0);
		}
		dbRow["upgradeslots"] = (long)slots;
		dbRow["level"] = 0L;
		dbRow["nativeStats"] = 1L;
	}

	private static void GiveItem(DbStore db, CharRecord c, int itemId, int count)
	{
		if (itemId / 1000000 == 1)
		{
			Dictionary<string, int> stats = BotItemCatalog.Shared.EquipStats(itemId) ?? new Dictionary<string, int>();
			InsertEquip(db, c, itemId, stats, BotItemCatalog.Int(BotItemCatalog.Shared.Info(itemId), "tuc"));
		}
		else
		{
			NativeInventoryService.AddItems(db, c, itemId, count, -1L);
		}
	}

	private static bool HasItem(DbStore db, int characterId, int itemId)
	{
		if (itemId > 0)
		{
			return InventoryDao.CountOf(db, characterId, itemId) > 0;
		}
		return false;
	}

	private static int EquipIdAt(DbStore db, int characterId, int position)
	{
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, characterId))
		{
			if (DbStore.Int(item, "inventorytype") == 1 && DbStore.Int(item, "position") == position && InventoryDao.IsBagPosition(position))
			{
				return DbStore.Int(item, "itemid");
			}
		}
		return 0;
	}

	private static void RemoveEquipAt(DbStore db, int characterId, int position)
	{
		foreach (DbRow item in InventoryDao.ItemsByCharacter(db, characterId))
		{
			if (DbStore.Int(item, "inventorytype") == 1 && DbStore.Int(item, "position") == position && InventoryDao.IsBagPosition(position))
			{
				long id = DbStore.Long(item, "inventoryitemid", 0L);
				db.Table("inventoryitems").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
				db.Table("inventoryequipment").Delete((DbRow x) => DbStore.Long(x, "inventoryitemid", -1L) == id);
				break;
			}
		}
	}

	private static string Name(int itemId)
	{
		return BotItemCatalog.Shared.Name(itemId) ?? itemId.ToString();
	}

	private static MakerResponse Fail(string error)
	{
		return new MakerResponse
		{
			ok = false,
			error = error
		};
	}

	public static bool IsEquipment(int itemId)
	{
		if (itemId < 2000000)
		{
			return itemId != 0;
		}
		return false;
	}

	public static bool IsWeapon(int itemId)
	{
		if (itemId >= 1302000)
		{
			return itemId < 1493000;
		}
		return false;
	}

	public static bool IsAccessory(int itemId)
	{
		if (itemId >= 1110000)
		{
			return itemId < 1140000;
		}
		return false;
	}

	public static bool IsMakerReagent(int itemId)
	{
		return itemId / 10000 == 425;
	}

	public static bool CanDisassemble(int itemId)
	{
		if (DisassembledFee(itemId) > -1)
		{
			return DisassembledItems(itemId).Count > 0;
		}
		return false;
	}

	public static List<MakerReqItem> DisassemblyGains(int itemId)
	{
		return DisassembledItems(itemId);
	}

	public static int ReagentSlotsFor(int itemId)
	{
		return ReagentSlots(itemId);
	}

	public static int StimulantFor(int itemId)
	{
		return StimulantOf(itemId);
	}
}
