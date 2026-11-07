using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class MobDefenseCatalog
{
	private static MobDefenseCatalog shared;

	private readonly Dictionary<string, object> monsters;

	public static MobDefenseCatalog Shared => shared ?? (shared = new MobDefenseCatalog(PlatformRes.LoadText("Enhancement/MobDefense")));

	public MobDefenseCatalog(string json)
	{
		monsters = ((MiniJsonDb.Parse(json) as Dictionary<string, object>)?["monsters"] as Dictionary<string, object>) ?? throw new InvalidOperationException("282怪物防御数据缺失");
	}

	public void Apply(int id, Dictionary<string, object> stats)
	{
		if (stats == null)
		{
			throw new ArgumentNullException("stats");
		}
		if (!monsters.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var value))
		{
			throw new InvalidOperationException("怪物尚未核实282防御：" + id);
		}
		Dictionary<string, object> dictionary = (Dictionary<string, object>)value;
		stats["PDRate"] = Convert.ToInt32(dictionary["PDRate"]);
		stats["MDRate"] = Convert.ToInt32(dictionary["MDRate"]);
	}
}
