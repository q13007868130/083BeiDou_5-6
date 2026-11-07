using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotNavigationMapLoader
{
	private const int MaxCachedShards = 2;

	private readonly Dictionary<string, object> maps;

	private readonly Func<string, string> readResource;

	private readonly Dictionary<string, Dictionary<string, object>> shardCache = new Dictionary<string, Dictionary<string, object>>();

	private readonly List<string> shardOrder = new List<string>();

	private readonly HashSet<string> pinnedShards = new HashSet<string>();

	public int PinnedCount => pinnedShards.Count;

	public int Count => maps.Count;

	public void PinShard(string resource)
	{
		if (!string.IsNullOrEmpty(resource))
		{
			pinnedShards.Add(resource);
		}
	}

	public bool IsPinned(string resource)
	{
		return pinnedShards.Contains(resource);
	}

	public BotNavigationMapLoader(string json, Func<string, string> readResource = null)
	{
		this.readResource = readResource ?? new Func<string, string>(PlatformRes.LoadText);
		maps = ((MiniJsonDb.Parse(json) as Dictionary<string, object>)?["maps"] as Dictionary<string, object>) ?? throw new ArgumentException("Original map geometry is missing");
	}

	public BotNavigationMapLoader(Dictionary<string, object> root, Func<string, string> readResource = null)
	{
		this.readResource = readResource ?? new Func<string, string>(PlatformRes.LoadText);
		maps = (root?["maps"] as Dictionary<string, object>) ?? throw new ArgumentException("Original map geometry is missing");
	}

	public static BotNavigationMapLoader Load()
	{
		return new BotNavigationMapLoader(BotWarmup.Root("Bot/Maps"));
	}

	public string ShardPath(int id)
	{
		if (!maps.TryGetValue(Key(id), out var value))
		{
			return null;
		}
		return value as string;
	}

	public MapRuntime Geometry(int id)
	{
		return Build(id, ReadMap(id));
	}

	public bool TryGeometry(int id, out MapRuntime map)
	{
		map = null;
		Dictionary<string, object> dictionary = TryReadMap(id);
		if (dictionary == null)
		{
			return false;
		}
		map = Build(id, dictionary);
		return true;
	}

	private MapRuntime Build(int id, Dictionary<string, object> map)
	{
		Dictionary<string, object> dictionary = (Dictionary<string, object>)map["info"];
		Dictionary<string, object> d = dictionary;
		string text = (dictionary.TryGetValue("link", out var value) ? Convert.ToString(value) : "");
		if (!string.IsNullOrEmpty(text))
		{
			map = ReadMap(int.Parse(text));
			dictionary = (Dictionary<string, object>)map["info"];
		}
		MapRuntime mapRuntime = new MapRuntime
		{
			mapId = id,
			returnMap = Int(d, "returnMap", id),
			fieldLimit = Int(d, "fieldLimit"),
			everlast = (Int(d, "everlast") != 0),
			swim = (Int(dictionary, "swim") != 0),
			top = Int(dictionary, "VRTop"),
			bottom = Int(dictionary, "VRBottom"),
			left = Int(dictionary, "VRLeft"),
			right = Int(dictionary, "VRRight"),
			minX = 0f,
			maxX = 0f,
			minY = 0f,
			maxY = 0f
		};
		if (mapRuntime.top == mapRuntime.bottom)
		{
			Dictionary<string, object> dictionary2 = (Dictionary<string, object>)map["miniMap"];
			if (dictionary2.Count > 0)
			{
				mapRuntime.left = -Int(dictionary2, "centerX");
				mapRuntime.top = -Int(dictionary2, "centerY");
				mapRuntime.right = mapRuntime.left + Int(dictionary2, "width");
				mapRuntime.bottom = mapRuntime.top + Int(dictionary2, "height");
			}
			else
			{
				mapRuntime.left = (mapRuntime.top = -131072);
				mapRuntime.right = (mapRuntime.bottom = 131072);
			}
		}
		if (dictionary.TryGetValue("fs", out var value2))
		{
			mapRuntime.footholdSpeed = Convert.ToSingle(value2);
		}
		foreach (Dictionary<string, object> item in (List<object>)map["footholds"])
		{
			mapRuntime.AddFoothold(Int(item, "x1"), Int(item, "y1"), Int(item, "x2"), Int(item, "y2"));
			FootholdSegment footholdSegment = mapRuntime.footholds[mapRuntime.footholds.Count - 1];
			footholdSegment.id = Int(item, "id");
			footholdSegment.previous = Int(item, "prev");
			footholdSegment.next = Int(item, "next");
			footholdSegment.forbidFallDown = Int(item, "forbidFallDown") != 0;
			footholdSegment.layer = Int(item, "layer");
			footholdSegment.group = Int(item, "group");
		}
		foreach (Dictionary<string, object> item2 in (List<object>)map["ropes"])
		{
			mapRuntime.ropes.Add(new MapRope
			{
				id = Int(item2, "id"),
				x = Int(item2, "x"),
				y1 = Int(item2, "y1"),
				y2 = Int(item2, "y2"),
				ladder = (Int(item2, "l") == 1),
				upperExit = (Int(item2, "uf") != 0)
			});
		}
		foreach (Dictionary<string, object> item3 in (List<object>)map["portals"])
		{
			mapRuntime.portals.Add(new MapPortal
			{
				id = Int(item3, "id"),
				x = Int(item3, "x"),
				y = Int(item3, "y"),
				type = Int(item3, "pt"),
				targetMap = Int(item3, "tm"),
				name = Str(item3, "pn"),
				targetName = Str(item3, "tn"),
				script = Str(item3, "script")
			});
		}
		if (map.TryGetValue("npcs", out var value3) && value3 is List<object> list)
		{
			foreach (Dictionary<string, object> item4 in list)
			{
				mapRuntime.npcs.Add(new MapNpcPosition
				{
					id = Int(item4, "id"),
					x = Int(item4, "x"),
					y = Int(item4, "y")
				});
			}
		}
		if (map.TryGetValue("reactors", out var value4) && value4 is List<object> list2)
		{
			foreach (Dictionary<string, object> item5 in list2)
			{
				mapRuntime.reactors.Add(new MapReactorSpawn
				{
					id = Int(item5, "id"),
					x = Int(item5, "x"),
					y = Int(item5, "y"),
					reactorTime = Int(item5, "reactorTime", -1),
					name = Str(item5, "name")
				});
			}
		}
		return mapRuntime;
	}

	private static string Key(int id)
	{
		return id.ToString(CultureInfo.InvariantCulture);
	}

	private Dictionary<string, object> TryReadMap(int id)
	{
		string key = Key(id);
		if (!maps.TryGetValue(key, out var value))
		{
			return null;
		}
		if (value is Dictionary<string, object> result)
		{
			return result;
		}
		if (!(value is string resource))
		{
			return null;
		}
		Dictionary<string, object> dictionary = Shard(resource, allowBlocking: false);
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(key, out var value2))
		{
			return null;
		}
		return value2 as Dictionary<string, object>;
	}

	private Dictionary<string, object> ReadMap(int id)
	{
		string key = Key(id);
		if (!maps.TryGetValue(key, out var value))
		{
			throw new ArgumentException("Map data not found for " + id);
		}
		if (value is Dictionary<string, object> result)
		{
			return result;
		}
		string text = (value as string) ?? throw new ArgumentException("Invalid map resource index for " + id);
		if (!(Shard(text, allowBlocking: true) ?? throw new ArgumentException("Map geometry group is missing: " + text)).TryGetValue(key, out var value2) || !(value2 is Dictionary<string, object> result2))
		{
			throw new ArgumentException("Map geometry is missing: " + id);
		}
		return result2;
	}

	private Dictionary<string, object> Shard(string resource, bool allowBlocking)
	{
		if (shardCache.TryGetValue(resource, out var value))
		{
			TouchShard(resource);
			return value;
		}
		if (!allowBlocking)
		{
			if (!BotWarmup.TryRoot(resource, out var root))
			{
				if (BotWarmup.Ready(resource))
				{
					throw new ArgumentException("Map geometry group is missing: " + resource);
				}
				return null;
			}
			Adopt(resource, root);
			return root;
		}
		if (BotWarmup.TryRoot(resource, out var root2))
		{
			Adopt(resource, root2);
			return root2;
		}
		Dictionary<string, object> dictionary = null;
		try
		{
			dictionary = BotWarmup.Root(resource);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
		Adopt(resource, dictionary);
		return dictionary;
	}

	private void Adopt(string resource, Dictionary<string, object> root)
	{
		if (root == null)
		{
			return;
		}
		shardCache[resource] = root;
		TouchShard(resource);
		BotWarmup.Release(resource);
		while (shardOrder.Count > 2)
		{
			int num = -1;
			for (int i = 0; i < shardOrder.Count; i++)
			{
				if (!pinnedShards.Contains(shardOrder[i]))
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				string key = shardOrder[num];
				shardOrder.RemoveAt(num);
				shardCache.Remove(key);
				continue;
			}
			break;
		}
	}

	private void TouchShard(string resource)
	{
		shardOrder.Remove(resource);
		shardOrder.Add(resource);
	}

	private static int Int(Dictionary<string, object> d, string k, int fallback = 0)
	{
		return BotItemCatalog.Int(d, k, fallback);
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value))
		{
			return "";
		}
		return (value as string) ?? "";
	}
}
