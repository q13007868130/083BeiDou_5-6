using System;
using System.Globalization;

public sealed class CharRecord
{
	public long id;

	public int accountid;

	public int world;

	public string name;

	public int level;

	public int gachaexp;

	public long exp;

	public int str;

	public int dex;

	public int luk;

	public int int_;

	public int hp;

	public int mp;

	public int maxhp;

	public int maxmp;

	public int hpMpUsed;

	public long meso;

	public int job;

	public int skincolor;

	public int gender;

	public int fame;

	public int fquest;

	public int hair;

	public int face;

	public int ap;

	public string sp = "0,0,0,0,0,0,0,0,0,0";

	public int map;

	public int spawnpoint;

	public int gm;

	public int party;

	public int buddyCapacity = 25;

	public string createdate;

	public int rank = 1;

	public int rankMove;

	public int jobRank = 1;

	public int jobRankMove;

	public int guildid;

	public int guildrank = 5;

	public int messengerid;

	public int messengerposition = 4;

	public int mountlevel = 1;

	public int mountexp;

	public int mounttiredness;

	public int omokwins;

	public int omoklosses;

	public int omokties;

	public int matchcardwins;

	public int matchcardlosses;

	public int matchcardties;

	public int MerchantMesos;

	public int HasMerchant;

	public int equipslots = 24;

	public int useslots = 24;

	public int setupslots = 24;

	public int etcslots = 24;

	public int familyId = -1;

	public int monsterbookcover;

	public int allianceRank = 5;

	public int vanquisherStage;

	public int ariantPoints;

	public int dojoPoints;

	public int lastDojoStage;

	public int finishedDojoTutorial;

	public int vanquisherKills;

	public int summonValue;

	public int partnerId;

	public int marriageItemId;

	public int reborns;

	public int PQPoints;

	public string dataString;

	public string lastLogoutTime = "2015-01-01 05:00:00";

	public string lastExpGainTime = "2015-01-01 05:00:00";

	public int partySearch = 1;

	public int jailexpire;

	public int fishLevel = 1;

	public int damageSkinId;

	public long damageCap = 19999999L;

	public static CharRecord FromRow(DbRow r)
	{
		return new CharRecord
		{
			id = L(r, "id"),
			accountid = I(r, "accountid"),
			world = I(r, "world"),
			name = S(r, "name"),
			level = I(r, "level"),
			exp = L(r, "exp"),
			gachaexp = I(r, "gachaexp"),
			str = I(r, "str"),
			dex = I(r, "dex"),
			luk = I(r, "luk"),
			int_ = I(r, "int"),
			hp = I(r, "hp"),
			mp = I(r, "mp"),
			maxhp = I(r, "maxhp"),
			maxmp = I(r, "maxmp"),
			meso = L(r, "meso"),
			hpMpUsed = I(r, "hpMpUsed"),
			job = I(r, "job"),
			skincolor = I(r, "skincolor"),
			gender = I(r, "gender"),
			fame = I(r, "fame"),
			fquest = I(r, "fquest"),
			hair = I(r, "hair"),
			face = I(r, "face"),
			ap = I(r, "ap"),
			sp = S(r, "sp"),
			map = I(r, "map"),
			spawnpoint = I(r, "spawnpoint"),
			gm = I(r, "gm"),
			party = I(r, "party"),
			buddyCapacity = I(r, "buddyCapacity"),
			createdate = S(r, "createdate"),
			rank = I(r, "rank"),
			rankMove = I(r, "rankMove"),
			jobRank = I(r, "jobRank"),
			jobRankMove = I(r, "jobRankMove"),
			guildid = I(r, "guildid"),
			guildrank = I(r, "guildrank"),
			messengerid = I(r, "messengerid"),
			messengerposition = I(r, "messengerposition"),
			mountlevel = I(r, "mountlevel"),
			mountexp = I(r, "mountexp"),
			mounttiredness = I(r, "mounttiredness"),
			omokwins = I(r, "omokwins"),
			omoklosses = I(r, "omoklosses"),
			omokties = I(r, "omokties"),
			matchcardwins = I(r, "matchcardwins"),
			matchcardlosses = I(r, "matchcardlosses"),
			matchcardties = I(r, "matchcardties"),
			MerchantMesos = I(r, "MerchantMesos"),
			HasMerchant = I(r, "HasMerchant"),
			equipslots = I(r, "equipslots"),
			useslots = I(r, "useslots"),
			setupslots = I(r, "setupslots"),
			etcslots = I(r, "etcslots"),
			familyId = I(r, "familyId"),
			monsterbookcover = I(r, "monsterbookcover"),
			allianceRank = I(r, "allianceRank"),
			vanquisherStage = I(r, "vanquisherStage"),
			ariantPoints = I(r, "ariantPoints"),
			dojoPoints = I(r, "dojoPoints"),
			lastDojoStage = I(r, "lastDojoStage"),
			finishedDojoTutorial = I(r, "finishedDojoTutorial"),
			vanquisherKills = I(r, "vanquisherKills"),
			summonValue = I(r, "summonValue"),
			partnerId = I(r, "partnerId"),
			marriageItemId = I(r, "marriageItemId"),
			reborns = I(r, "reborns"),
			PQPoints = I(r, "PQPoints"),
			dataString = S(r, "dataString"),
			lastLogoutTime = S(r, "lastLogoutTime"),
			lastExpGainTime = S(r, "lastExpGainTime"),
			partySearch = I(r, "partySearch"),
			jailexpire = I(r, "jailexpire"),
			fishLevel = I(r, "fishLevel"),
			damageSkinId = I(r, "damageSkinId"),
			damageCap = L(r, "damageCap")
		};
	}

	public DbRow ToInsertRow()
	{
		return new DbRow
		{
			["accountid"] = (long)accountid,
			["world"] = (long)world,
			["name"] = name,
			["level"] = (long)level,
			["exp"] = exp,
			["gachaexp"] = (long)gachaexp,
			["str"] = (long)str,
			["dex"] = (long)dex,
			["luk"] = (long)luk,
			["int"] = (long)int_,
			["hp"] = (long)hp,
			["mp"] = (long)mp,
			["maxhp"] = (long)maxhp,
			["maxmp"] = (long)maxmp,
			["meso"] = meso,
			["hpMpUsed"] = (long)hpMpUsed,
			["job"] = (long)job,
			["skincolor"] = (long)skincolor,
			["gender"] = (long)gender,
			["fame"] = (long)fame,
			["fquest"] = (long)fquest,
			["hair"] = (long)hair,
			["face"] = (long)face,
			["ap"] = (long)ap,
			["sp"] = sp,
			["map"] = (long)map,
			["spawnpoint"] = (long)spawnpoint,
			["gm"] = (long)gm,
			["party"] = (long)party,
			["buddyCapacity"] = (long)buddyCapacity,
			["createdate"] = createdate ?? DbStore.NowText(),
			["rank"] = (long)rank,
			["rankMove"] = (long)rankMove,
			["jobRank"] = (long)jobRank,
			["jobRankMove"] = (long)jobRankMove,
			["guildid"] = (long)guildid,
			["guildrank"] = (long)guildrank,
			["messengerid"] = (long)messengerid,
			["messengerposition"] = (long)messengerposition,
			["mountlevel"] = (long)mountlevel,
			["mountexp"] = (long)mountexp,
			["mounttiredness"] = (long)mounttiredness,
			["omokwins"] = (long)omokwins,
			["omoklosses"] = (long)omoklosses,
			["omokties"] = (long)omokties,
			["matchcardwins"] = (long)matchcardwins,
			["matchcardlosses"] = (long)matchcardlosses,
			["matchcardties"] = (long)matchcardties,
			["MerchantMesos"] = (long)MerchantMesos,
			["HasMerchant"] = (long)HasMerchant,
			["equipslots"] = (long)equipslots,
			["useslots"] = (long)useslots,
			["setupslots"] = (long)setupslots,
			["etcslots"] = (long)etcslots,
			["familyId"] = (long)familyId,
			["monsterbookcover"] = (long)monsterbookcover,
			["allianceRank"] = (long)allianceRank,
			["vanquisherStage"] = (long)vanquisherStage,
			["ariantPoints"] = (long)ariantPoints,
			["dojoPoints"] = (long)dojoPoints,
			["lastDojoStage"] = (long)lastDojoStage,
			["finishedDojoTutorial"] = (long)finishedDojoTutorial,
			["vanquisherKills"] = (long)vanquisherKills,
			["summonValue"] = (long)summonValue,
			["partnerId"] = (long)partnerId,
			["marriageItemId"] = (long)marriageItemId,
			["reborns"] = (long)reborns,
			["PQPoints"] = (long)PQPoints,
			["dataString"] = dataString ?? "",
			["lastLogoutTime"] = lastLogoutTime,
			["lastExpGainTime"] = lastExpGainTime,
			["partySearch"] = (long)partySearch,
			["jailexpire"] = (long)jailexpire,
			["fishLevel"] = (long)fishLevel,
			["damageSkinId"] = (long)damageSkinId,
			["damageCap"] = damageCap
		};
	}

	public DbRow ToUpdateRow()
	{
		DbRow dbRow = ToInsertRow();
		dbRow.Remove("createdate");
		dbRow.Remove("name");
		return dbRow;
	}

	public int GetSp(int bookIndex)
	{
		string[] array = (sp ?? "").Split(',');
		if (bookIndex < 0 || bookIndex >= array.Length)
		{
			return 0;
		}
		if (!int.TryParse(array[bookIndex], out var result))
		{
			return 0;
		}
		return result;
	}

	public void SetSp(int bookIndex, int value)
	{
		string[] array = (sp ?? "").Split(',');
		if (array.Length < 10)
		{
			array = "0,0,0,0,0,0,0,0,0,0".Split(',');
		}
		if (bookIndex >= 0 && bookIndex < array.Length)
		{
			array[bookIndex] = value.ToString(CultureInfo.InvariantCulture);
			sp = string.Join(",", array);
		}
	}

	private static int I(DbRow r, string k)
	{
		if (r == null || !r.TryGetValue(k, out var value) || value == null)
		{
			return 0;
		}
		try
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0;
		}
	}

	private static long L(DbRow r, string k)
	{
		if (r == null || !r.TryGetValue(k, out var value) || value == null)
		{
			return 0L;
		}
		try
		{
			return Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0L;
		}
	}

	private static string S(DbRow r, string k)
	{
		if (r == null || !r.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return (value as string) ?? value.ToString();
	}
}
