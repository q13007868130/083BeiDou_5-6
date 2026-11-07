using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class EquipCatalog
{
	private static readonly Dictionary<int, ImgNode> cache = new Dictionary<int, ImgNode>();

	public static string DirOf(int itemId)
	{
		switch (itemId / 10000)
		{
		case 100:
			return "Cap";
		case 104:
			return "Coat";
		case 105:
			return "Longcoat";
		case 106:
			return "Pants";
		case 107:
			return "Shoes";
		case 108:
			return "Glove";
		case 109:
			return "Shield";
		case 110:
			return "Cape";
		case 111:
			return "Ring";
		case 101:
		case 102:
		case 103:
			return "Accessory";
		case 167:
			if (itemId / 1000 != 1672)
			{
				return "Accessory";
			}
			return "Android";
		case 166:
			return "Android";
		case 171:
			switch (itemId / 1000)
			{
			default:
				return null;
			case 1713:
			case 1714:
				return "AuthenticForce";
			case 1712:
				return "ArcaneForce";
			}
		default:
		{
			int num = itemId / 10000;
			if (num >= 112 && num <= 119)
			{
				return "Accessory";
			}
			if (num >= 180 && num <= 183)
			{
				return "PetEquip";
			}
			switch (num)
			{
			case 190:
			case 191:
			case 193:
			case 198:
			case 199:
				return "TamingMob";
			case 169:
			case 170:
				return "Weapon";
			default:
				return null;
			case 121:
			case 122:
			case 123:
			case 124:
			case 125:
			case 126:
			case 127:
			case 128:
			case 129:
			case 130:
			case 131:
			case 132:
			case 133:
			case 134:
			case 135:
			case 136:
			case 137:
			case 138:
			case 139:
			case 140:
			case 141:
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
			case 150:
			case 151:
			case 152:
			case 153:
			case 154:
			case 155:
			case 156:
			case 157:
			case 158:
			case 159:
			case 160:
				return "Weapon";
			}
		}
		}
	}

	public static ImgNode Info(int itemId)
	{
		if (cache.TryGetValue(itemId, out var value))
		{
			return value;
		}
		string text = DirOf(itemId);
		ImgNode imgNode = ((text == null) ? null : WzAssets.Node("Character/" + text + "/" + itemId.ToString("D8") + ".img/info"));
		cache[itemId] = imgNode;
		return imgNode;
	}

	public static bool Exists(int itemId)
	{
		return Info(itemId) != null;
	}

	public static bool CanEquip(int itemId, CharacterAttributes a, int jobId)
	{
		ImgNode imgNode = Info(itemId);
		if (imgNode == null)
		{
			return false;
		}
		int num = imgNode.Int("reqJob");
		if (num != 0 && (num & JobDefs.JobBit(jobId)) == 0)
		{
			return false;
		}
		if (a.level >= imgNode.Int("reqLevel") && a.str >= imgNode.Int("reqSTR") && a.dex >= imgNode.Int("reqDEX") && a.intelligence >= imgNode.Int("reqINT"))
		{
			return a.luk >= imgNode.Int("reqLUK");
		}
		return false;
	}

	public static string WhyNot(int itemId, CharacterAttributes a, int jobId)
	{
		ImgNode imgNode = Info(itemId);
		if (imgNode == null)
		{
			return "没有这件装备的数据";
		}
		int num = imgNode.Int("reqJob");
		if (num != 0 && (num & JobDefs.JobBit(jobId)) == 0)
		{
			return "职业不符（" + JobDefs.NameOf(jobId) + "）";
		}
		if (a.level < imgNode.Int("reqLevel"))
		{
			return "等级不足 Lv." + imgNode.Int("reqLevel");
		}
		if (a.str < imgNode.Int("reqSTR"))
		{
			return "力量不足 " + imgNode.Int("reqSTR");
		}
		if (a.dex < imgNode.Int("reqDEX"))
		{
			return "敏捷不足 " + imgNode.Int("reqDEX");
		}
		if (a.intelligence < imgNode.Int("reqINT"))
		{
			return "智力不足 " + imgNode.Int("reqINT");
		}
		if (a.luk < imgNode.Int("reqLUK"))
		{
			return "运气不足 " + imgNode.Int("reqLUK");
		}
		return null;
	}

	public static void ApplyAddition(int itemId, CharacterAttributes result, ref int bossDamage)
	{
		ImgNode imgNode = Info(itemId)?.Get("addition");
		if (imgNode != null)
		{
			int level = result.level;
			int jobId = Profile.JobId;
			ImgNode imgNode2 = imgNode.Get("statinc");
			if (imgNode2 != null && ConditionMet(imgNode2.Get("con"), level, jobId))
			{
				result.str += imgNode2.Int("incSTR");
				result.dex += imgNode2.Int("incDEX");
				result.intelligence += imgNode2.Int("incINT");
				result.luk += imgNode2.Int("incLUK");
				result.maxHp += imgNode2.Int("incMHP");
				result.maxMp += imgNode2.Int("incMMP");
				result.weaponDefense += imgNode2.Int("incPDD");
				result.magicDefense += imgNode2.Int("incMDD");
				result.accuracy += imgNode2.Int("incACC");
				result.avoidability += imgNode2.Int("incEVA");
				result.equipmentSpeed += imgNode2.Int("incSpeed");
				result.equipmentJump += imgNode2.Int("incJump");
			}
			ImgNode imgNode3 = imgNode.Get("critical");
			if (imgNode3 != null && ConditionMet(imgNode3.Get("con"), level, jobId))
			{
				result.criticalChance += (float)Mathf.Max(0, imgNode3.Int("prob")) / 100f;
				result.criticalBonus += (float)Mathf.Max(0, imgNode3.Int("damage")) / 100f;
			}
			ImgNode imgNode4 = imgNode.Get("boss");
			if (imgNode4 != null && ConditionMet(imgNode4.Get("con"), level, jobId) && imgNode4.Int("prob", 100) >= 100)
			{
				bossDamage += imgNode4.Int("damage");
			}
		}
	}

	private static bool ConditionMet(ImgNode con, int level, int job)
	{
		if (con == null)
		{
			return true;
		}
		int num = con.Int("lv", con.Int("level"));
		if (num > 0 && level < num)
		{
			return false;
		}
		ImgNode imgNode = con.Get("job");
		if (imgNode != null)
		{
			bool flag = false;
			foreach (ImgNode child in imgNode.Children)
			{
				if (child.Value != null && Convert.ToInt32(child.Value) == job)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		ImgNode imgNode2 = con.Get("weekDay");
		if (imgNode2 != null)
		{
			string b = DateTime.Now.DayOfWeek.ToString().ToLowerInvariant();
			bool flag2 = false;
			foreach (ImgNode child2 in imgNode2.Children)
			{
				if (string.Equals(child2.Value as string, b, StringComparison.OrdinalIgnoreCase))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
		}
		return true;
	}

	public static void Apply(int itemId, CharacterAttributes result, ref int watk, ref int matk, int slot = 0)
	{
		int bossDamage = 0;
		Apply(itemId, result, ref watk, ref matk, slot, ref bossDamage);
	}

	public static void Apply(int itemId, CharacterAttributes result, ref int watk, ref int matk, int slot, ref int bossDamage, InventoryEntry overrideInstance = null)
	{
		ImgNode info = Info(itemId);
		if (info == null)
		{
			return;
		}
		InventoryEntry instance = overrideInstance ?? GameSession.Character?.inventory.Find((InventoryEntry i) => i.itemId == itemId && i.position < 0 && (slot == 0 || i.position == slot));
		if (instance?.equipmentData != null && instance.equipmentData.TryGetValue("starForceVestige", out var value) && Convert.ToInt32(value) != 0)
		{
			return;
		}
		bool native = NativeScrollService.HasStats(instance);
		result.str += Value("incSTR");
		result.dex += Value("incDEX");
		result.intelligence += Value("incINT");
		result.luk += Value("incLUK");
		result.maxHp += Value("incMHP");
		result.maxMp += Value("incMMP");
		result.weaponDefense += Value("incPDD");
		result.magicDefense += Value("incMDD");
		result.accuracy += Value("incACC");
		result.avoidability += Value("incEVA");
		watk += Value("incPAD");
		matk += Value("incMAD");
		result.equipmentSpeed += Value("incSpeed");
		result.equipmentJump += Value("incJump");
		Dictionary<string, int> star;
		if (instance?.equipmentData != null && instance.equipmentData.TryGetValue("enhancementVersion", out var value2) && Convert.ToInt32(value2) == 282)
		{
			Dictionary<string, int> baseStats = new Dictionary<string, int>
			{
				{
					"str",
					Value("incSTR")
				},
				{
					"dex",
					Value("incDEX")
				},
				{
					"int",
					Value("incINT")
				},
				{
					"luk",
					Value("incLUK")
				},
				{
					"hp",
					Value("incMHP")
				},
				{
					"mp",
					Value("incMMP")
				},
				{
					"watk",
					Value("incPAD")
				},
				{
					"matk",
					Value("incMAD")
				},
				{
					"wdef",
					Value("incPDD")
				},
				{
					"mdef",
					Value("incMDD")
				},
				{
					"speed",
					Value("incSpeed")
				},
				{
					"jump",
					Value("incJump")
				}
			};
			int num = info.Int("reqLevel");
			if (instance.equipmentData.TryGetValue("iIncReq", out var value3))
			{
				num += Convert.ToInt32(value3);
			}
			star = StarForceStats.Calculate(itemId, num, instance.starLevel, info.Int("superiorEqp") != 0, baseStats);
			result.str += Delta("str");
			result.dex += Delta("dex");
			result.intelligence += Delta("int");
			result.luk += Delta("luk");
			result.maxHp += Delta("hp");
			result.maxMp += Delta("mp");
			result.weaponDefense += Delta("wdef");
			result.magicDefense += Delta("mdef");
			result.equipmentSpeed += Delta("speed");
			result.equipmentJump += Delta("jump");
			watk += Delta("watk");
			matk += Delta("matk");
		}
		ApplyAddition(itemId, result, ref bossDamage);
		int Delta(string key)
		{
			if (!star.TryGetValue(key, out var value4))
			{
				return 0;
			}
			return value4;
		}
		int Value(string key)
		{
			if (!native)
			{
				return info.Int(key);
			}
			return key switch
			{
				"incSTR" => instance.str, 
				"incDEX" => instance.dex, 
				"incINT" => instance.int_, 
				"incLUK" => instance.luk, 
				"incMHP" => instance.hp, 
				"incMMP" => instance.mp, 
				"incPDD" => instance.wdef, 
				"incMDD" => instance.mdef, 
				"incACC" => instance.acc, 
				"incEVA" => instance.avoid, 
				"incPAD" => instance.watk, 
				"incMAD" => instance.matk, 
				"incSpeed" => instance.speed, 
				"incJump" => instance.jump, 
				_ => info.Int(key), 
			};
		}
	}
}
