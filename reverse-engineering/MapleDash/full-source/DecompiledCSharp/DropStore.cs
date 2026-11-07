using System;
using System.Collections.Generic;
using System.Linq;

public static class DropStore
{
	public const int ChanceDenominator = 999999;

	public static Func<DbStore> DbProvider;

	public static Func<Dictionary<int, List<DropEntry>>> JsonDrops;

	public static Func<List<DropEntry>> JsonGlobalDrops;

	private static readonly object gate = new object();

	private static Dictionary<int, List<DropEntry>> drops = new Dictionary<int, List<DropEntry>>();

	private static List<DropEntry> global = new List<DropEntry>();

	private static bool loaded;

	private static DbStore loadedDb;

	public static void Invalidate()
	{
		lock (gate)
		{
			loaded = false;
			loadedDb = null;
		}
	}

	public static void EnsureDatabase(DbStore db)
	{
	}

	public static List<DropEntry> ForMob(int mobId)
	{
		if (!Snapshot().TryGetValue(mobId, out var value))
		{
			return new List<DropEntry>();
		}
		return value;
	}

	public static List<DropEntry> Global()
	{
		Snapshot();
		lock (gate)
		{
			return new List<DropEntry>(global);
		}
	}

	public static List<DropEntry> BaselineForMob(int mobId)
	{
		Dictionary<int, List<DropEntry>> dictionary = ((JsonDrops != null) ? JsonDrops() : null);
		if (dictionary == null || !dictionary.TryGetValue(mobId, out var value))
		{
			return new List<DropEntry>();
		}
		return new List<DropEntry>(value);
	}

	public static List<DropEntry> BaselineGlobal()
	{
		if (JsonGlobalDrops == null)
		{
			return new List<DropEntry>();
		}
		return new List<DropEntry>(JsonGlobalDrops());
	}

	public static DropEntry BaselineEntry(bool global, int owner, int item)
	{
		if (global)
		{
			foreach (DropEntry item2 in BaselineGlobal())
			{
				if (item2.continent == owner && item2.item == item)
				{
					return item2;
				}
			}
			return null;
		}
		foreach (DropEntry item3 in BaselineForMob(owner))
		{
			if (item3.item == item)
			{
				return item3;
			}
		}
		return null;
	}

	public static Dictionary<int, List<DropEntry>> All()
	{
		Dictionary<int, List<DropEntry>> dictionary = Snapshot();
		Dictionary<int, List<DropEntry>> dictionary2 = new Dictionary<int, List<DropEntry>>();
		foreach (KeyValuePair<int, List<DropEntry>> item in dictionary)
		{
			dictionary2[item.Key] = new List<DropEntry>(item.Value);
		}
		return dictionary2;
	}

	private static Dictionary<int, List<DropEntry>> Snapshot()
	{
		DbStore dbStore = ((DbProvider != null) ? DbProvider() : null);
		lock (gate)
		{
			if (loaded && dbStore == loadedDb)
			{
				return drops;
			}
			loaded = true;
			loadedDb = dbStore;
			drops = new Dictionary<int, List<DropEntry>>();
			global = new List<DropEntry>();
			Dictionary<int, List<DropEntry>> dictionary = ((JsonDrops != null) ? JsonDrops() : null);
			if (dictionary != null)
			{
				foreach (KeyValuePair<int, List<DropEntry>> item in dictionary)
				{
					drops[item.Key] = new List<DropEntry>(item.Value);
				}
			}
			if (JsonGlobalDrops != null)
			{
				global = new List<DropEntry>(JsonGlobalDrops());
			}
			ExcludeUnsupportedDrops(drops, global);
			if (dbStore == null)
			{
				return drops;
			}
			if (dbStore.HasTable("drop_data"))
			{
				Dictionary<int, List<DropEntry>> dictionary2 = new Dictionary<int, List<DropEntry>>();
				foreach (DbRow row2 in dbStore.Table("drop_data").rows)
				{
					int key = (int)DbStore.Long(row2, "dropperid", 0L);
					if (!dictionary2.TryGetValue(key, out var value))
					{
						value = (dictionary2[key] = new List<DropEntry>());
					}
					value.Add(FromRow(row2, isGlobal: false));
				}
				foreach (KeyValuePair<int, List<DropEntry>> item2 in dictionary2)
				{
					if (!drops.TryGetValue(item2.Key, out var value2))
					{
						value2 = new List<DropEntry>();
						drops[item2.Key] = value2;
					}
					foreach (DropEntry row in item2.Value)
					{
						int num = value2.FindIndex((DropEntry e) => e.item == row.item);
						if (row.chance <= 0)
						{
							if (num >= 0)
							{
								value2.RemoveAt(num);
							}
						}
						else if (num >= 0)
						{
							value2[num] = row;
						}
						else
						{
							value2.Add(row);
						}
					}
				}
			}
			if (dbStore.HasTable("drop_data_global"))
			{
				foreach (DbRow row3 in dbStore.Table("drop_data_global").rows)
				{
					DropEntry entry = FromRow(row3, isGlobal: true);
					int num2 = global.FindIndex((DropEntry e) => e.continent == entry.continent && e.item == entry.item);
					if (entry.chance <= 0)
					{
						if (num2 >= 0)
						{
							global.RemoveAt(num2);
						}
					}
					else if (num2 >= 0)
					{
						global[num2] = entry;
					}
					else
					{
						global.Add(entry);
					}
				}
			}
			ExcludeUnsupportedDrops(drops, global);
			return drops;
		}
		static void ExcludeUnsupportedDrops(Dictionary<int, List<DropEntry>> mobs, List<DropEntry> globals)
		{
			foreach (KeyValuePair<int, List<DropEntry>> mob in mobs)
			{
				mob.Value.RemoveAll((DropEntry e) => IsUnsupportedDrop(e.item));
			}
			globals.RemoveAll((DropEntry e) => IsUnsupportedDrop(e.item));
			foreach (int item3 in mobs.Keys.Where((int k) => mobs[k].Count == 0).ToList())
			{
				mobs.Remove(item3);
			}
		}
		static bool IsUnsupportedDrop(int item)
		{
			if ((item < 4260000 || item >= 4270000) && item != 2435719 && item / 1000 != 2590)
			{
				return item / 1000 == 2591;
			}
			return true;
		}
	}

	private static DropEntry FromRow(DbRow row, bool isGlobal)
	{
		return new DropEntry
		{
			id = DbStore.Long(row, "id", 0L),
			item = (int)DbStore.Long(row, "itemid", 0L),
			min = (int)DbStore.Long(row, "minimum_quantity", 1L),
			max = (int)DbStore.Long(row, "maximum_quantity", 1L),
			quest = (int)DbStore.Long(row, "questid", 0L),
			chance = (int)DbStore.Long(row, "chance", 0L),
			continent = (isGlobal ? DbStore.Long(row, "continent", -1L) : (-1)),
			comments = DbStore.Str(row, "comments")
		};
	}
}
