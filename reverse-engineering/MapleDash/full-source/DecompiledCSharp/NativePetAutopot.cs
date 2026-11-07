using System;
using MapleDash.Wz;
using UnityEngine;

public static class NativePetAutopot
{
	public static void OnLoss(PlayerCtrl player, float previousHp, float previousMp)
	{
		if (player.hp >= previousHp && player.mp >= previousMp)
		{
			return;
		}
		bool summoned = false;
		if (GameSession.Character?.inventory != null)
		{
			foreach (InventoryEntry item in GameSession.Character.inventory)
			{
				if (item.petData != null && Convert.ToInt32(item.petData["summoned"]) == 1)
				{
					summoned = true;
					break;
				}
			}
		}
		Tick(player, previousHp, previousMp, summoned);
		GameLoop i = GameLoop.I;
		if (i != null)
		{
			FieldDirector field = i.field;
			if (field != null)
			{
				((Component)field).GetComponent<NativePetField>()?.RecordVitals(player);
			}
		}
	}

	internal static bool BelowAlarm(float current, float maximum, double ratio, bool server)
	{
		if (ratio > 0.0)
		{
			if (!server)
			{
				return Math.Floor(current * 100f / Math.Max(1f, maximum)) < ratio * 100.0;
			}
			return (double)current <= (double)maximum * ratio;
		}
		return false;
	}

	public static void Tick(PlayerCtrl player, float previousHp, float previousMp, bool summoned)
	{
		if (player.hp <= 0f || !GameSession.InGame)
		{
			return;
		}
		DbStore dbStore = GameServer.Local?.Db;
		int num;
		if (dbStore != null)
		{
			num = (GameConfigDao.GetBool(dbStore, "use_server_auto_pot") ? 1 : 0);
			if (num != 0)
			{
				goto IL_004d;
			}
		}
		else
		{
			num = 0;
		}
		if (dbStore == null)
		{
			goto IL_004d;
		}
		int num2 = (GameConfigDao.GetBool(dbStore, "use_compulsory_auto_pot") ? 1 : 0);
		goto IL_004e;
		IL_004d:
		num2 = 0;
		goto IL_004e;
		IL_004e:
		bool flag = (byte)num2 != 0;
		double num3 = (flag ? GameConfigDao.GetFloat(dbStore, "pet_auto_hp_ratio", 0.99) : ((double)ClientSettings.HpAlarm / 100.0));
		double num4 = (flag ? GameConfigDao.GetFloat(dbStore, "pet_auto_mp_ratio", 0.99) : ((double)ClientSettings.MpAlarm / 100.0));
		bool flag2 = (byte)((uint)num | (flag ? 1u : 0u)) != 0;
		if (previousHp >= 0f && player.hp < previousHp && BelowAlarm(player.hp, player.hpMax, num3, flag2) && (flag2 || (summoned && Profile.EquipOfSlot(-24) > 0)))
		{
			Use(player, KeyMap.PetPotion(hp: true), num3, num4, flag2);
		}
		if (previousMp >= 0f && player.mp < previousMp && BelowAlarm(player.mp, player.mpMax, num4, flag2) && (flag2 || (summoned && Profile.EquipOfSlot(-25) > 0)))
		{
			Use(player, KeyMap.PetPotion(hp: false), num3, num4, flag2);
		}
	}

	public static bool Use(PlayerCtrl player, int id, double hpRatio, double mpRatio, bool multi)
	{
		if (id / 1000000 != 2 || player.hp <= 0f || (player.hp >= player.hpMax && player.mp >= player.mpMax))
		{
			return false;
		}
		ImgNode imgNode = NativeItemSource.Node(id)?.Get("spec");
		if (imgNode == null)
		{
			return false;
		}
		int num = (short)imgNode.Int("hp");
		int num2 = (short)imgNode.Int("mp");
		if (num <= 0 && imgNode.Int("hpR") > 0)
		{
			num = (int)Math.Ceiling((double)(player.hpMax * (float)imgNode.Int("hpR")) / 100.0);
		}
		if (num2 <= 0 && imgNode.Int("mpR") > 0)
		{
			num2 = (int)Math.Ceiling((double)(player.mpMax * (float)imgNode.Int("mpR")) / 100.0);
		}
		if (num <= 0 && num2 <= 0)
		{
			return false;
		}
		int val = 1;
		if (multi)
		{
			val = 0;
			if (num > 0)
			{
				val = Math.Max(val, (int)Math.Ceiling(((double)player.hpMax * hpRatio - (double)player.hp) / (double)num));
			}
			if (num2 > 0)
			{
				val = Math.Max(val, (int)Math.Ceiling(((double)player.mpMax * mpRatio - (double)player.mp) / (double)num2));
			}
		}
		val = Math.Min(val, Profile.ItemCount(id));
		if (val > 0)
		{
			return NativeInventoryActions.UseStatPotion(id, player, val);
		}
		return false;
	}
}
