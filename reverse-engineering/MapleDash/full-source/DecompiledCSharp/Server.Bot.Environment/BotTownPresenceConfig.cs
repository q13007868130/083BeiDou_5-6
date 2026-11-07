using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.Bot.Environment;

public sealed class BotTownPresenceConfig
{
	public sealed class MapShare
	{
		public int mapId;

		public int count;

		public BotTownOverrides overrides;
	}

	public sealed class TownEntry
	{
		public string name;

		public string dialogueOverride;

		public int levelLo;

		public int levelHi;

		public int wanderers;

		public readonly List<MapShare> maps = new List<MapShare>();

		public int MainMapId
		{
			get
			{
				if (maps.Count != 0)
				{
					return maps[0].mapId;
				}
				return -1;
			}
		}
	}

	private readonly BotResources resources;

	private List<TownEntry> cached;

	public IReadOnlyList<TownEntry> Towns => cached ?? (cached = Load());

	public BotTownPresenceConfig(BotResources resources)
	{
		this.resources = resources;
	}

	public IReadOnlyList<TownEntry> Reload()
	{
		return cached = Load();
	}

	public HashSet<int> AllTownMapIds()
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (TownEntry town in Towns)
		{
			foreach (MapShare map in town.maps)
			{
				hashSet.Add(map.mapId);
			}
		}
		return hashSet;
	}

	public BotTownOverrides OverridesFor(int mapId)
	{
		foreach (TownEntry town in Towns)
		{
			foreach (MapShare map in town.maps)
			{
				if (map.mapId == mapId)
				{
					return map.overrides;
				}
			}
		}
		return new BotTownOverrides();
	}

	private List<TownEntry> Load()
	{
		List<TownEntry> list = new List<TownEntry>();
		try
		{
			foreach (object item in List(Get(resources.ReadData("soloMapling/ArtificialPlayer/BotTownSystem/TownPresence.yaml") as Dictionary<string, object>, "towns")))
			{
				if (!(item is Dictionary<string, object> node))
				{
					continue;
				}
				int num = Int(Get(node, "level_lo"), 10);
				TownEntry townEntry = new TownEntry
				{
					name = Str(Get(node, "name"), "town"),
					levelLo = num,
					levelHi = Int(Get(node, "level_hi"), num),
					wanderers = Int(Get(node, "wanderers"), 0),
					dialogueOverride = Str(Get(node, "dialogue"), null)
				};
				foreach (object item2 in List(Get(node, "maps")))
				{
					if (!(item2 is Dictionary<string, object> node2))
					{
						continue;
					}
					int num2 = Int(Get(node2, "map"), -1);
					int num3 = Int(Get(node2, "count"), 0);
					if (num2 <= 0 || num3 <= 0)
					{
						continue;
					}
					BotTownOverrides botTownOverrides = new BotTownOverrides();
					Zones(Get(node2, "ban"), 1.0, botTownOverrides.ban);
					Zones(Get(node2, "boost"), 2.0, botTownOverrides.boost);
					foreach (object item3 in List(Get(node2, "pins")))
					{
						if (item3 is Dictionary<string, object> node3)
						{
							int num4 = Int(Get(node3, "x"), int.MinValue);
							int num5 = Int(Get(node3, "y"), int.MinValue);
							if (num4 != int.MinValue && num5 != int.MinValue)
							{
								botTownOverrides.pins.Add(new BotNavigationGraph.Point(num4, num5));
							}
						}
					}
					townEntry.maps.Add(new MapShare
					{
						mapId = num2,
						count = num3,
						overrides = botTownOverrides
					});
				}
				if (townEntry.maps.Count > 0)
				{
					list.Add(townEntry);
				}
			}
			return list;
		}
		catch (Exception)
		{
			return new List<TownEntry>();
		}
	}

	private static void Zones(object node, double multiplier, List<BotTownOverrides.Zone> result)
	{
		foreach (object item in List(node))
		{
			if (item is Dictionary<string, object> node2)
			{
				double multiplier2 = (double.TryParse(Str(Get(node2, "mult"), ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) ? result2 : multiplier);
				result.Add(new BotTownOverrides.Zone(Int(Get(node2, "x1"), int.MinValue), Int(Get(node2, "y1"), int.MinValue), Int(Get(node2, "x2"), int.MaxValue), Int(Get(node2, "y2"), int.MaxValue), multiplier2));
			}
		}
	}

	private static object Get(Dictionary<string, object> node, string key)
	{
		if (node == null || !node.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	private static IEnumerable<object> List(object node)
	{
		IEnumerable<object> enumerable = node as List<object>;
		return enumerable ?? Array.Empty<object>();
	}

	private static string Str(object value, string fallback)
	{
		if (value != null)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}
		return fallback;
	}

	private static int Int(object value, int fallback)
	{
		if (value is long num)
		{
			return (int)num;
		}
		if (value is double num2)
		{
			return (int)num2;
		}
		if (!int.TryParse(Str(value, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}
}
