using System;
using System.Collections.Generic;
using UnityEngine;

public static class AdminLive
{
	public static bool IsCurrentCharacter(long characterId)
	{
		if (GameSession.InGame && characterId > 0)
		{
			return GameSession.CharacterId == (int)characterId;
		}
		return false;
	}

	public static bool IsCurrentAccount(long accountId)
	{
		if (GameSession.InGame && accountId > 0)
		{
			return GameSession.AccountId == (int)accountId;
		}
		return false;
	}

	public static string WarpToHenesys(long characterId)
	{
		return WarpTo(characterId, 100000000L);
	}

	public static string WarpTo(long characterId, long mapId)
	{
		if (!IsCurrentCharacter(characterId))
		{
			return null;
		}
		GameLoop i = GameLoop.I;
		if ((Object)(object)i == (Object)null || (Object)(object)i.field == (Object)null)
		{
			return "已写入数据库；当前不在原版地图场景，重新进入地图后生效";
		}
		i.field.WarpTo((int)mapId);
		Profile.LastMapId = (int)mapId;
		if (GameSession.Character != null && GameSession.Character.record != null)
		{
			GameSession.Character.record.map = (int)mapId;
		}
		return "当前角色已传送到地图 " + mapId;
	}

	public static string Rename(long characterId, string newName)
	{
		if (!IsCurrentCharacter(characterId))
		{
			return null;
		}
		if (GameSession.Character != null && GameSession.Character.record != null)
		{
			GameSession.Character.record.name = newName;
		}
		Profile.Data.playerName = newName;
		return "当前角色已改名，界面立即生效";
	}

	public static string SyncInventory(DbStore db, long characterId, long itemRowId, bool deleted)
	{
		if (!IsCurrentCharacter(characterId))
		{
			return null;
		}
		List<InventoryEntry> list = ((GameSession.Character != null) ? GameSession.Character.inventory : null);
		if (list == null || itemRowId <= 0)
		{
			return null;
		}
		int num = list.FindIndex((InventoryEntry e) => e.itemRowId == itemRowId);
		if (deleted)
		{
			if (num >= 0)
			{
				ApplyProfileDelta(list[num].itemId, -list[num].quantity);
				list.RemoveAt(num);
			}
			return "已从当前角色背包移除该道具";
		}
		DbRow dbRow = db.Table("inventoryitems").Find(DbStore.Eq("inventoryitemid", itemRowId));
		if (dbRow == null)
		{
			if (num >= 0)
			{
				ApplyProfileDelta(list[num].itemId, -list[num].quantity);
				list.RemoveAt(num);
			}
			return null;
		}
		InventoryEntry inventoryEntry = CharService.ToInventoryEntry(db, dbRow);
		int num2 = ((num >= 0) ? list[num].quantity : 0);
		if (num >= 0)
		{
			list[num] = inventoryEntry;
		}
		else
		{
			list.Add(inventoryEntry);
		}
		ApplyProfileDelta(inventoryEntry.itemId, inventoryEntry.quantity - num2);
		return "当前角色背包已同步";
	}

	private static void ApplyProfileDelta(int itemId, int delta)
	{
		if (itemId > 0 && delta != 0 && itemId / 1000000 != 1)
		{
			if (delta > 0)
			{
				Profile.Data.AddItem(itemId, delta);
			}
			else
			{
				Profile.Data.RemoveItem(itemId, -delta);
			}
			Profile.Save();
		}
	}

	public static string KickAccount(long accountId, string reason)
	{
		if (!IsCurrentAccount(accountId))
		{
			return null;
		}
		string text = (string.IsNullOrEmpty(reason) ? "账号已被管理员封停" : ("账号已被封停：" + reason));
		try
		{
			GameLoop i = GameLoop.I;
			if ((Object)(object)i != (Object)null)
			{
				i.ExitToCharSelect();
			}
			GameSession.Logout();
			LoginFlow i2 = LoginFlow.I;
			if ((Object)(object)i2 != (Object)null)
			{
				i2.ForceLogout(text);
			}
			else
			{
				BottomBar.Toast(text);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Admin] 踢下线失败: " + ex.Message));
		}
		return text;
	}
}
