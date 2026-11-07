using System.Collections.Generic;

public sealed class LegacyProfileSnapshot
{
	public string name = "";

	public int level = 1;

	public long exp;

	public int str;

	public int dex;

	public int int_;

	public int luk;

	public int maxHp;

	public int maxMp;

	public int job;

	public int map;

	public int ap;

	public int sp;

	public int equipSlots = 24;

	public long meso;

	public List<LegacySkillEntry> skills = new List<LegacySkillEntry>();
}
