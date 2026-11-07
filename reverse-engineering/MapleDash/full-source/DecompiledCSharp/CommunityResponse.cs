using System.Collections.Generic;

public sealed class CommunityResponse
{
	public bool ok = true;

	public string error = "";

	public string guildName = "";

	public string notice = "";

	public string precepts = "";

	public string board = "";

	public int guildId;

	public int capacity;

	public int gp;

	public int familyId;

	public int reputation;

	public int todayRep;

	public int totalRep;

	public int map;

	public long meso;

	public int guildInvite;

	public int familyInvite;

	public int guildConfirmation;

	public int summonFrom;

	public string[] titles = new string[5];

	public List<CommunityMember> guild = new List<CommunityMember>();

	public List<CommunityMember> family = new List<CommunityMember>();

	public List<int> used = new List<int>();

	public List<int> couponBuffs = new List<int>();

	public int treeTarget;

	public List<CommunityMember> familyTree = new List<CommunityMember>();

	public double expRate = 1.0;

	public double dropRate = 1.0;

	public double mesoRate = 1.0;

	public double familyDropRate = 1.0;

	public long buffExpires;
}
