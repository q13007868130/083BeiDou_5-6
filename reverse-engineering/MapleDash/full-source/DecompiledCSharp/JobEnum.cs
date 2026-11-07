using System.Collections.Generic;
using MapleDash.Js;

public static class JobEnum
{
	public sealed class Const
	{
		public readonly string Name;

		public readonly int Id;

		public readonly string Display;

		internal JsHostObject Js;

		public Const(string name, int id, string display)
		{
			Name = name;
			Id = id;
			Display = display;
		}
	}

	private static readonly string[] table;

	private static readonly Dictionary<string, Const> byName;

	private static readonly Dictionary<int, Const> byId;

	public static IEnumerable<Const> All
	{
		get
		{
			string[] array = table;
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('|');
				yield return byName[array2[0]];
			}
		}
	}

	static JobEnum()
	{
		table = new string[82]
		{
			"BEGINNER|0|新手", "WARRIOR|100|战士", "FIGHTER|110|剑客", "CRUSADER|111|勇士", "HERO|112|英雄", "PAGE|120|准骑士", "WHITEKNIGHT|121|骑士", "PALADIN|122|圣骑士", "SPEARMAN|130|枪战士", "DRAGONKNIGHT|131|龙骑士",
			"DARKKNIGHT|132|黑骑士", "MAGICIAN|200|魔法师", "FP_WIZARD|210|法师（火毒）", "FP_MAGE|211|巫师（火毒）", "FP_ARCHMAGE|212|魔导师（火毒）", "IL_WIZARD|220|法师（冰雷）", "IL_MAGE|221|巫师（冰雷）", "IL_ARCHMAGE|222|魔导师（冰雷）", "CLERIC|230|牧师", "PRIEST|231|祭司",
			"BISHOP|232|主教", "BOWMAN|300|弓箭手", "HUNTER|310|猎人", "RANGER|311|射手", "BOWMASTER|312|神射手", "CROSSBOWMAN|320|弩弓手", "SNIPER|321|游侠", "MARKSMAN|322|箭神", "THIEF|400|飞侠", "ASSASSIN|410|刺客",
			"HERMIT|411|无影人", "NIGHTLORD|412|隐士", "BANDIT|420|侠客", "CHIEFBANDIT|421|独行客", "SHADOWER|422|侠盗", "PIRATE|500|海盗", "BRAWLER|510|拳手", "MARAUDER|511|斗士", "BUCCANEER|512|冲锋队长", "GUNSLINGER|520|火枪手",
			"OUTLAW|521|大副", "CORSAIR|522|船长", "MAPLELEAF_BRIGADIER|800|巡查员", "GM|900|管理员", "SUPERGM|910|超级管理员", "NOBLESSE|1000|初心者", "DAWNWARRIOR1|1100|魂骑士(1转)", "DAWNWARRIOR2|1110|魂骑士(2转)", "DAWNWARRIOR3|1111|魂骑士(3转)", "DAWNWARRIOR4|1112|魂骑士(4转)",
			"BLAZEWIZARD1|1200|炎术士(1转)", "BLAZEWIZARD2|1210|炎术士(2转)", "BLAZEWIZARD3|1211|炎术士(3转)", "BLAZEWIZARD4|1212|炎术士(4转)", "WINDARCHER1|1300|风灵使者(1转)", "WINDARCHER2|1310|风灵使者(2转)", "WINDARCHER3|1311|风灵使者(3转)", "WINDARCHER4|1312|风灵使者(4转)", "NIGHTWALKER1|1400|夜行者(1转)", "NIGHTWALKER2|1410|夜行者(2转)",
			"NIGHTWALKER3|1411|夜行者(3转)", "NIGHTWALKER4|1412|夜行者(4转)", "THUNDERBREAKER1|1500|奇袭者(1转)", "THUNDERBREAKER2|1510|奇袭者(2转)", "THUNDERBREAKER3|1511|奇袭者(3转)", "THUNDERBREAKER4|1512|奇袭者(4转)", "LEGEND|2000|战童", "EVAN|2001|龙初心", "ARAN1|2100|战神(1转)", "ARAN2|2110|战神(2转)",
			"ARAN3|2111|战神(3转)", "ARAN4|2112|战神(4转)", "EVAN1|2200|龙神1", "EVAN2|2210|龙神2", "EVAN3|2211|龙神3", "EVAN4|2212|龙神4", "EVAN5|2213|龙神5", "EVAN6|2214|龙神6", "EVAN7|2215|龙神7", "EVAN8|2216|龙神8",
			"EVAN9|2217|龙神9", "EVAN10|2218|龙神10"
		};
		byName = new Dictionary<string, Const>();
		byId = new Dictionary<int, Const>();
		string[] array = table;
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split('|');
			Const obj = new Const(array2[0], int.Parse(array2[1]), array2[2]);
			byName[obj.Name] = obj;
			byId[obj.Id] = obj;
		}
	}

	public static Const Of(string name)
	{
		if (!byName.TryGetValue(name ?? "", out var value))
		{
			return null;
		}
		return value;
	}

	public static Const Of(int id)
	{
		if (!byId.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static int StyleOf(int jobId, bool strOverDex)
	{
		switch (jobId / 100)
		{
		case 1:
		case 11:
		case 21:
			return 100;
		case 2:
		case 12:
		case 22:
			return 200;
		case 3:
		case 13:
			if (jobId / 10 != 32)
			{
				return 300;
			}
			return 320;
		case 4:
		case 14:
			return 400;
		case 5:
		case 15:
			if (!strOverDex)
			{
				return 520;
			}
			return 510;
		default:
			return 0;
		}
	}

	public static Const Style(int jobId, int str, int dex)
	{
		return Of(StyleOf(jobId, str > dex));
	}

	public static JsHostObject Js(Const c)
	{
		if (c == null)
		{
			return null;
		}
		if (c.Js == null)
		{
			JsHostObject jsHostObject = new JsHostObject(c);
			jsHostObject.Set("getId", JsFunction.FromNative("getId", (JsCall _) => new JsNumber(c.Id)));
			jsHostObject.Set("getName", JsFunction.FromNative("getName", (JsCall _) => new JsString(c.Display)));
			jsHostObject.Set("name", new JsString(c.Name));
			jsHostObject.Set("id", new JsNumber(c.Id));
			c.Js = jsHostObject;
		}
		return c.Js;
	}
}
