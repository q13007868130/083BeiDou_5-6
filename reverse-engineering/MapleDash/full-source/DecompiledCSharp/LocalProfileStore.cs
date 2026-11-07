using System;
using System.Collections.Generic;
using UnityEngine;

public static class LocalProfileStore
{
	private const string Snapshot = "mxd.profile.v1";

	private const string Backup = "mxd.profile.v1.backup";

	private static PlayerProfileData Read(string key)
	{
		if (!PlayerPrefs.HasKey(key))
		{
			return null;
		}
		try
		{
			PlayerProfileData playerProfileData = JsonUtility.FromJson<PlayerProfileData>(PlayerPrefs.GetString(key));
			return (playerProfileData != null && playerProfileData.version == 1 && playerProfileData.owned != null) ? playerProfileData : null;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	public static PlayerProfileData Load()
	{
		PlayerProfileData playerProfileData = Read("mxd.profile.v1") ?? Read("mxd.profile.v1.backup");
		if (playerProfileData != null)
		{
			return playerProfileData;
		}
		return new PlayerProfileData
		{
			gold = PlayerPrefs.GetInt("mxd.gold", 0),
			cls = PlayerPrefs.GetString("mxd.cls", "beginner"),
			equipWeapon = PlayerPrefs.GetString("mxd.equip", "w_sword_basic"),
			hair = PlayerPrefs.GetString("mxd.hair", "h0"),
			outfit = PlayerPrefs.GetString("mxd.outfit", "none"),
			owned = new List<string>(PlayerPrefs.GetString("mxd.owned", "w_sword_basic").Split(','))
		};
	}

	public static bool Save(PlayerProfileData data)
	{
		try
		{
			if (Read("mxd.profile.v1") != null)
			{
				PlayerPrefs.SetString("mxd.profile.v1.backup", PlayerPrefs.GetString("mxd.profile.v1"));
			}
			PlayerPrefs.SetString("mxd.profile.v1", JsonUtility.ToJson((object)data));
			PlayerPrefs.Save();
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("[Profile] 本地存档保存失败：" + ex.Message));
			return false;
		}
	}
}
