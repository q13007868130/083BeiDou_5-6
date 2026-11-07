using System;

public static class AdminNames
{
	public static Func<int, string> Mob;

	public static Func<int, string> Npc;

	public static Func<int, string> Item;

	public static Func<int, string> ItemDesc;

	public static Func<int, string> Quest;

	public static Func<int, string> Map;

	public static Func<int, string> Skill;

	public static string MobName(int id)
	{
		return Resolve(Mob, id);
	}

	public static string NpcName(int id)
	{
		return Resolve(Npc, id);
	}

	public static string ItemName(int id)
	{
		return Resolve(Item, id);
	}

	public static string QuestName(int id)
	{
		return Resolve(Quest, id);
	}

	public static string MapName(int id)
	{
		return Resolve(Map, id);
	}

	public static string SkillName(int id)
	{
		return Resolve(Skill, id);
	}

	public static string ItemDescription(int id)
	{
		if (id <= 0)
		{
			return "";
		}
		try
		{
			return (ItemDesc != null) ? (ItemDesc(id) ?? "") : "";
		}
		catch
		{
			return "";
		}
	}

	private static string Resolve(Func<int, string> fn, int id)
	{
		if (id <= 0)
		{
			return "";
		}
		try
		{
			string text = fn?.Invoke(id);
			return string.IsNullOrEmpty(text) ? id.ToString() : text;
		}
		catch
		{
			return id.ToString();
		}
	}

	public static string ItemCategory(int id)
	{
		if (id <= 0)
		{
			return "金币";
		}
		return (id / 1000000) switch
		{
			1 => "装备", 
			2 => "消耗", 
			3 => "设置", 
			4 => "其他", 
			5 => "特殊", 
			_ => "未知", 
		};
	}
}
