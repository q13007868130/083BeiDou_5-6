using System;
using System.Collections.Generic;

namespace Server.Field;

public sealed class MapReactorTemplate
{
	public sealed class Event
	{
		public int type;

		public int next;

		public int item;

		public int quantity;
	}

	public readonly Dictionary<int, List<Event>> states = new Dictionary<int, List<Event>>();

	public int left;

	public int top;

	public int right;

	public int bottom;

	public List<Event> Events(int state)
	{
		if (!states.TryGetValue(state, out var value))
		{
			return null;
		}
		return value;
	}

	public int Next(int state, int index)
	{
		List<Event> list = Events(state);
		if (list == null || index >= list.Count)
		{
			return -1;
		}
		return list[index].next;
	}

	public static Dictionary<int, MapReactorTemplate> Parse(Dictionary<string, object> root)
	{
		Dictionary<int, MapReactorTemplate> dictionary = new Dictionary<int, MapReactorTemplate>();
		foreach (KeyValuePair<string, object> item in root)
		{
			Dictionary<string, object> dictionary2 = (Dictionary<string, object>)item.Value;
			MapReactorTemplate mapReactorTemplate = new MapReactorTemplate();
			if (dictionary2["area"] is List<object> list)
			{
				mapReactorTemplate.left = Convert.ToInt32(list[0]);
				mapReactorTemplate.top = Convert.ToInt32(list[1]);
				mapReactorTemplate.right = Convert.ToInt32(list[2]);
				mapReactorTemplate.bottom = Convert.ToInt32(list[3]);
			}
			foreach (KeyValuePair<string, object> item2 in (Dictionary<string, object>)dictionary2["states"])
			{
				List<Event> list2 = new List<Event>();
				foreach (Dictionary<string, object> item3 in (List<object>)item2.Value)
				{
					list2.Add(new Event
					{
						type = Convert.ToInt32(item3["type"]),
						next = Convert.ToInt32(item3["next"]),
						item = Convert.ToInt32(item3["item"]),
						quantity = Convert.ToInt32(item3["quantity"])
					});
				}
				mapReactorTemplate.states.Add(int.Parse(item2.Key), list2);
			}
			dictionary.Add(int.Parse(item.Key), mapReactorTemplate);
		}
		return dictionary;
	}
}
