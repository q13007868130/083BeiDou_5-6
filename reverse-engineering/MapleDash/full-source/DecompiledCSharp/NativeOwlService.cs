using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeOwlService
{
	public const int MaxResults = 200;

	public const int TopSearched = 10;

	public const int MinSearchedKinds = 5;

	public const byte ErrorNoShop = 1;

	public const byte ErrorFull = 2;

	public const byte ErrorMismatch = 3;

	public const byte ErrorNotAllowed = 17;

	public const byte ErrorMaintenance = 18;

	public static readonly int[] DefaultSuggestions = new int[11]
	{
		1082002, 2070005, 2070006, 1022047, 1102041, 2044705, 2340000, 2340001, 2040017, 1092030,
		2040804
	};

	public static Func<MarketShopService> MarketProvider;

	private static readonly Dictionary<int, int> searched = new Dictionary<int, int>();

	public static Dictionary<int, int> Searched => new Dictionary<int, int>(searched);

	public static void Reset()
	{
		searched.Clear();
	}

	public static OwlResponse Handle(DbStore db, OwlRequest r)
	{
		CharRecord charRecord = CharactersDao.FindById(db, r.characterId);
		if (charRecord == null)
		{
			return new OwlResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		return r.operation switch
		{
			OwlOperation.Open => Open(db), 
			OwlOperation.Search => Search(db, r.itemId), 
			OwlOperation.Warp => Warp(charRecord, r.ownerId, r.mapId, r.itemId), 
			_ => new OwlResponse
			{
				ok = false,
				error = "未知操作"
			}, 
		};
	}

	private static OwlResponse Open(DbStore db)
	{
		List<int> list = new List<int>();
		if (!Enforced(db))
		{
			if (searched.Count < 5)
			{
				int[] defaultSuggestions = DefaultSuggestions;
				foreach (int item in defaultSuggestions)
				{
					list.Add(item);
				}
			}
			else
			{
				List<KeyValuePair<int, int>> list2 = new List<KeyValuePair<int, int>>(searched);
				list2.Sort((KeyValuePair<int, int> a, KeyValuePair<int, int> b) => (a.Value == b.Value) ? a.Key.CompareTo(b.Key) : b.Value.CompareTo(a.Value));
				for (int num = 0; num < Math.Min(list2.Count, 10); num++)
				{
					list.Add(list2[num].Key);
				}
			}
		}
		return new OwlResponse
		{
			suggestions = list,
			hits = new List<OwlHit>()
		};
	}

	private static OwlResponse Search(DbStore db, int itemId)
	{
		if (itemId <= 0)
		{
			return new OwlResponse
			{
				ok = false,
				error = "请先选择要搜索的道具"
			};
		}
		if (!Enforced(db))
		{
			searched.TryGetValue(itemId, out var value);
			searched[itemId] = value + 1;
		}
		List<OwlHit> list = Scan(itemId);
		return new OwlResponse
		{
			hits = list,
			charged = (list.Count > 0)
		};
	}

	public static List<OwlHit> Scan(int itemId)
	{
		List<OwlHit> list = new List<OwlHit>();
		MarketShopService marketShopService = MarketProvider?.Invoke();
		if (marketShopService == null)
		{
			return list;
		}
		foreach (MarketShop item in marketShopService.AllOpen())
		{
			if (item.items == null)
			{
				continue;
			}
			foreach (MarketShopStock item2 in item.items)
			{
				if (item2 != null && item2.item != null && item2.exists && item2.bundles > 0 && item2.item.itemId == itemId)
				{
					list.Add(new OwlHit
					{
						ownerId = item.ownerId,
						ownerName = item.ownerName,
						shopName = item.description,
						mapId = item.mapId,
						bundles = item2.bundles,
						quantity = item2.item.quantity,
						price = item2.price,
						channel = (byte)Math.Max(0, item.channel - 1),
						equip = item2.item.isEquip,
						item = item2.item.Copy()
					});
				}
			}
		}
		list.Sort((OwlHit a, OwlHit b) => (a.price == b.price) ? a.ownerId.CompareTo(b.ownerId) : a.price.CompareTo(b.price));
		if (list.Count > 200)
		{
			list.RemoveRange(200, list.Count - 200);
		}
		return list;
	}

	private static OwlResponse Warp(CharRecord c, int ownerId, int mapId, int itemId)
	{
		if (ownerId == c.id)
		{
			return new OwlResponse
			{
				ok = false,
				code = 1,
				error = "不能访问自己的店铺"
			};
		}
		MarketShop marketShop = MarketProvider?.Invoke()?.Get(ownerId);
		if (marketShop == null)
		{
			return new OwlResponse
			{
				ok = false,
				code = 1,
				error = "找不到这家店铺了"
			};
		}
		if (!marketShop.open)
		{
			return new OwlResponse
			{
				ok = false,
				code = 18,
				error = "店主正在整理店铺，请稍后再试"
			};
		}
		bool flag = false;
		if (marketShop.items != null)
		{
			foreach (MarketShopStock item in marketShop.items)
			{
				if (item != null && item.item != null && item.exists && item.bundles > 0 && item.item.itemId == itemId)
				{
					flag = true;
					break;
				}
			}
		}
		if (marketShop.mapId != mapId || !flag)
		{
			return new OwlResponse
			{
				ok = false,
				code = 3,
				error = "这家店铺已经不再出售该道具"
			};
		}
		return new OwlResponse
		{
			map = marketShop.mapId
		};
	}

	private static bool Enforced(DbStore db)
	{
		return GameConfigDao.GetBool(db, "use_enforce_item_suggestion");
	}
}
