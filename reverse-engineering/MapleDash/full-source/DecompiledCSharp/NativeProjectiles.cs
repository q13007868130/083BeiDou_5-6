using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeProjectiles
{
	public struct Cost
	{
		public int Item;

		public int Position;

		public int Count;

		public bool Allowed;
	}

	private static readonly Dictionary<int, bool> projectileLayer = new Dictionary<int, bool>();

	public static bool NightWalkerReplay(int id)
	{
		if ((id < 14121003 || id > 14121008) && (id < 14121014 || id > 14121018) && (id < 14121027 || id > 14121028))
		{
			if (id >= 14121030)
			{
				return id <= 14121036;
			}
			return false;
		}
		return true;
	}

	public static bool WindArcherReplay(int id)
	{
		if ((id < 13121003 || id > 13121005) && (id < 13121009 || id > 13121018) && id != 13121019 && id != 13121020)
		{
			return id == 13121023;
		}
		return true;
	}

	public static bool MarksmanReplay(int id)
	{
		if (id != 3221009 && id != 3221013 && id != 3221030 && id != 3221031 && id != 3221032)
		{
			return id == 3221034;
		}
		return true;
	}

	public static bool IsRangedSkill(int id)
	{
		int num = id / 10000;
		if (id != 4001344 && id != 14001004 && id != 5001003)
		{
			if (((num >= 300 && num <= 324) || (num >= 410 && num <= 414) || (num >= 520 && num <= 524) || (num >= 1300 && num <= 1314) || (num >= 1410 && num <= 1414)) && id != 3101003)
			{
				return id != 3201003;
			}
			return false;
		}
		return true;
	}

	public static bool ConsumesAmmo(int id)
	{
		if (!IsRangedSkill(id))
		{
			return false;
		}
		if (Art.Int(SkillBook.Get(id)?.Level(SkillProgress.LevelOf(id)), "bulletCount") > 0)
		{
			return true;
		}
		return HasProjectileLayer(id);
	}

	private static bool HasProjectileLayer(int id)
	{
		if (projectileLayer.TryGetValue(id, out var value))
		{
			return value;
		}
		bool flag = true;
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7"))?.TryResolve();
		if (imgNode != null)
		{
			flag = imgNode.Get("ball") != null || imgNode.Get("shootobj") != null || imgNode.Get("flipBall") != null;
			if (flag && imgNode.Get("noBulletConsume") != null)
			{
				flag = false;
			}
		}
		projectileLayer[id] = flag;
		return flag;
	}

	public static bool Matches(int item, WeaponKind weapon, int weaponId, int level)
	{
		bool flag = item / 1000 == 2060;
		bool flag2 = item / 1000 == 2061;
		if (weaponId == 1472063)
		{
			return flag | flag2;
		}
		switch (weapon)
		{
		case WeaponKind.claw:
			if (item / 10000 == 207 && ((item != 2070007 && item != 2070018) || level >= 70))
			{
				if (item == 2070016)
				{
					return level >= 50;
				}
				return true;
			}
			return false;
		case WeaponKind.gun:
			if (item / 10000 == 233)
			{
				if (item != 2331000 && item != 2332000)
				{
					return level > item % 10 * 20 + 9;
				}
				return level >= 70;
			}
			return false;
		default:
			if (!((weapon == WeaponKind.bow) & flag))
			{
				return (weapon == WeaponKind.crossbow) & flag2;
			}
			return true;
		}
	}

	public static Cost Select(IEnumerable<InventoryEntry> inventory, WeaponKind weapon, int weaponId, int level, int skill, int bulletCount, int bulletConsume, bool partner, bool soulArrow, bool shadowClaw, int furySlotMax)
	{
		if (skill == 4121003 || skill == 4221003 || skill == 5121002 || skill == 15111006)
		{
			return new Cost
			{
				Allowed = true
			};
		}
		bool flag = NightWalkerReplay(skill);
		bool flag2 = WindArcherReplay(skill);
		bool flag3 = MarksmanReplay(skill);
		int num = ((skill != 4111004) ? ((short)bulletCount) : 0);
		if (partner && !flag && !flag2)
		{
			num *= 2;
		}
		List<InventoryEntry> list = new List<InventoryEntry>(inventory);
		list.Sort((InventoryEntry a, InventoryEntry b) => a.position.CompareTo(b.position));
		int num2 = 0;
		int num3 = 0;
		Cost result = default;
		foreach (InventoryEntry item in list)
		{
			if (item.inventoryType == 2 && item.position > 0)
			{
				if (item.itemId == 2070018 && item.quantity - num <= 10)
				{
					num2 = (short)(-furySlotMax);
				}
				if (item.quantity >= num && Matches(item.itemId, weapon, weaponId, level))
				{
					result.Item = item.itemId;
					result.Position = item.position;
					num3 = item.quantity;
					break;
				}
			}
		}
		result.Allowed = ((result.Item != 0) | soulArrow | flag3) || skill == 11101004 || skill == 15111007 || skill == 14101006 || skill == 4111004 || skill == 13101005;
		if (result.Item != 0 && !soulArrow && !shadowClaw && skill != 11101004 && skill != 15111007 && skill != 14101006)
		{
			result.Count = ((flag | flag2 | flag3) ? 1 : num);
			if (bulletConsume != 0)
			{
				result.Count = (sbyte)(bulletConsume * ((!partner) ? 1 : 2));
			}
			if (num2 < 0)
			{
				result.Count = num2;
			}
			result.Count = num3 - Math.Max(0, (int)(short)(num3 - result.Count));
		}
		return result;
	}

	public static Cost For(PlayerCtrl player, int skill, int level)
	{
		Dictionary<string, object> d = SkillBook.Get(skill)?.Level(level);
		NativeBuffs component = ((Component)player).GetComponent<NativeBuffs>();
		List<InventoryEntry> list = new List<InventoryEntry>();
		if (GameSession.InGame)
		{
			list.AddRange(GameSession.Character.inventory);
		}
		else
		{
			int num = 0;
			foreach (int item in Profile.InventoryItems(1))
			{
				InventoryEntry inventoryEntry = new InventoryEntry
				{
					itemId = item,
					inventoryType = 2
				};
				num = (inventoryEntry.position = num + 1);
				inventoryEntry.quantity = Profile.ItemCount(item);
				list.Add(inventoryEntry);
			}
		}
		int.TryParse(WeaponDefs.Canonical(Profile.equipWeapon), out var result);
		int num2 = SkillProgress.LevelOf((Profile.JobId >= 1400 && Profile.JobId <= 1412) ? 14100000 : 4100000);
		int num3 = WzAssets.Node("Item/Consume/0207.img/02070018/info")?.Int("slotMax", 100) ?? 100;
		int num4 = ((GameServer.Local != null) ? GameConfigDao.GetInt(GameServer.Local.Db, "item_slot_max") : 0);
		if (num3 != 1 && num4 > 0)
		{
			num3 = num4;
		}
		num3 = (short)(num3 + num2 * 10);
		return Select(list, WeaponDefs.Get(Profile.equipWeapon).kind, result, Profile.level, skill, Art.Int(d, "bulletCount", 1), Art.Int(d, "bulletConsume"), component?.HasStat("SHADOWPARTNER") ?? false, component?.HasStat("SOULARROW") ?? false, component?.HasStat("SHADOW_CLAW") ?? false, num3);
	}

	public static bool Spend(Cost cost)
	{
		if (cost.Allowed)
		{
			if (cost.Count != 0)
			{
				return Profile.ConsumeProjectile(cost.Item, cost.Count, cost.Position);
			}
			return true;
		}
		return false;
	}
}
