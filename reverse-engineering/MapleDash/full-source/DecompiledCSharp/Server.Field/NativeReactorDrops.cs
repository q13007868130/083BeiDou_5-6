using System;
using System.Collections.Generic;
using Server.Bot;

namespace Server.Field;

public static class NativeReactorDrops
{
	public sealed class Entry
	{
		public int item;

		public int chance;

		public int quest;
	}

	private sealed class Spray
	{
		public MapReactor reactor;

		public int owner;

		public int x;

		public int minMeso;

		public int maxMeso;

		public int index;

		public double mesoRate;

		public long at;

		public List<Entry> items;
	}

	public static readonly Dictionary<int, List<Entry>> Tables = new Dictionary<int, List<Entry>>();

	public static readonly HashSet<int> MissingTables = new HashSet<int>();

	public static Random Random = new Random();

	public static Func<int, (double drop, double meso)> Rates;

	public static Func<int, int, int, bool> NeedsQuestItem;

	private static readonly List<Spray> sprays = new List<Spray>();

	public static void Load(Dictionary<string, object> root)
	{
		Tables.Clear();
		MissingTables.Clear();
		foreach (KeyValuePair<string, object> item in root)
		{
			List<Entry> list = new List<Entry>();
			foreach (Dictionary<string, object> item2 in (List<object>)item.Value)
			{
				list.Add(new Entry
				{
					item = Convert.ToInt32(item2["item"]),
					chance = Convert.ToInt32(item2["chance"]),
					quest = Convert.ToInt32(item2["quest"])
				});
			}
			Tables.Add(int.Parse(item.Key), list);
		}
	}

	public static void Drop(MapReactor reactor, int owner, bool meso, int mesoChance, int minMeso, int maxMeso, int minItems)
	{
		if (!Tables.TryGetValue(reactor.id, out var value))
		{
			MissingTables.Add(reactor.id);
			value = null;
		}
		(double, double) tuple = Rates?.Invoke(owner) ?? (1.0, 1.0);
		List<Entry> list = new List<Entry>();
		if (meso && Random.NextDouble() < 1.0 / (double)mesoChance)
		{
			list.Add(new Entry
			{
				quest = -1
			});
		}
		if (value != null)
		{
			foreach (Entry item in value)
			{
				if (Random.NextDouble() < tuple.Item1 / (double)item.chance)
				{
					list.Add(item);
				}
			}
		}
		while (list.Count < minItems)
		{
			list.Add(new Entry
			{
				quest = -1
			});
		}
		List<Entry> list2 = new List<Entry>();
		List<Entry> list3 = new List<Entry>();
		List<Entry> list4 = new List<Entry>();
		foreach (Entry item2 in list)
		{
			if (item2.quest <= 0)
			{
				list2.Add(item2);
				continue;
			}
			Func<int, int, int, bool> needsQuestItem = NeedsQuestItem;
			if (needsQuestItem == null || needsQuestItem(owner, item2.quest, item2.item))
			{
				list3.Add(item2);
			}
			else
			{
				list4.Add(item2);
			}
		}
		Shuffle(list2);
		Shuffle(list3);
		Shuffle(list4);
		list.Clear();
		list.AddRange(list2);
		list.AddRange(list3);
		list.AddRange(list4);
		List<Entry> list5 = new List<Entry>();
		List<Entry> list6 = new List<Entry>();
		for (int i = 0; i < list.Count; i++)
		{
			if (i % 2 == 0)
			{
				list5.Add(list[i]);
			}
			else
			{
				list6.Add(list[i]);
			}
		}
		list5.Reverse();
		list5.AddRange(list6);
		sprays.Add(new Spray
		{
			reactor = reactor,
			owner = owner,
			items = list5,
			x = reactor.x - ((list.Count % 2 == 0) ? 12 : 0) - 12 * list.Count,
			minMeso = minMeso,
			maxMeso = maxMeso,
			mesoRate = tuple.Item2,
			at = MapReactorService.Now()
		});
	}

	private static void Shuffle(List<Entry> rows)
	{
		for (int num = rows.Count - 1; num > 0; num--)
		{
			int index = Random.Next(num + 1);
			Entry value = rows[num];
			rows[num] = rows[index];
			rows[index] = value;
		}
	}

	public static void Tick(long now)
	{
		foreach (Spray item in new List<Spray>(sprays))
		{
			while (item.index < item.items.Count && now >= item.at)
			{
				MapReactor reactor = item.reactor;
				Entry entry = item.items[item.index++];
				int x = item.x;
				item.x += 25;
				item.at += 200L;
				if (entry.item == 0)
				{
					int meso = (int)Math.Min(2147483647.0, (Random.NextDouble() * (double)(item.maxMeso - item.minMeso) + (double)item.minMeso) * item.mesoRate);
					MapItemService.CalcDropPos(reactor.mapId, x, reactor.y, reactor.x, reactor.y, out var outX, out var outY);
					MapItemService.SpawnMesoDrop(reactor.mapId, meso, outX, outY, reactor.objectId, item.owner, MapItemService.PartyOf?.Invoke(item.owner) ?? (-1), playerDrop: false, 2);
				}
				else
				{
					MapItemService.DropFromReactor(reactor.mapId, reactor.objectId, item.owner, BotLogic.GenerateCleanItem(entry.item), x, reactor.y, reactor.x, reactor.y, entry.quest);
				}
			}
			if (item.index == item.items.Count)
			{
				sprays.Remove(item);
			}
		}
	}

	public static void Clear(int? mapId = null)
	{
		if (mapId.HasValue)
		{
			sprays.RemoveAll((Spray s) => s.reactor.mapId == mapId.Value);
		}
		else
		{
			sprays.Clear();
		}
	}
}
