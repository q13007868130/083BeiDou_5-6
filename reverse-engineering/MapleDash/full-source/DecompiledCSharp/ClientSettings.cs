using UnityEngine;

public static class ClientSettings
{
	private const string PrefInvite = "client.invite";

	private const string PrefHpAlarm = "client.hpalarm";

	private const string PrefMpAlarm = "client.mpalarm";

	private const string PrefScreenFx = "client.screenfx";

	public const int InviteCount = 12;

	public static readonly string[] InviteNames = new string[12]
	{
		"悄悄话", "好友聊天", "聊天邀请", "交易申请", "组队邀请", "寻找组队", "游戏邀请", "家族聊天", "家族邀请", "联盟聊天",
		"联盟邀请", "学院招待"
	};

	private static int InviteMask => PlayerPrefs.GetInt("client.invite", 4095);

	public static int HpAlarm
	{
		get
		{
			return Mathf.Clamp(PlayerPrefs.GetInt("client.hpalarm", 0), 0, 100);
		}
		set
		{
			PlayerPrefs.SetInt("client.hpalarm", Mathf.Clamp(value, 0, 100));
		}
	}

	public static int MpAlarm
	{
		get
		{
			return Mathf.Clamp(PlayerPrefs.GetInt("client.mpalarm", 0), 0, 100);
		}
		set
		{
			PlayerPrefs.SetInt("client.mpalarm", Mathf.Clamp(value, 0, 100));
		}
	}

	public static bool ScreenEffects
	{
		get
		{
			return PlayerPrefs.GetInt("client.screenfx", 1) != 0;
		}
		set
		{
			PlayerPrefs.SetInt("client.screenfx", value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	public static bool Invite(int index)
	{
		if (index >= 0 && index < 12)
		{
			return (InviteMask & (1 << index)) != 0;
		}
		return false;
	}

	public static void SetInvite(int index, bool on)
	{
		if (index >= 0 && index < 12)
		{
			int inviteMask = InviteMask;
			inviteMask = (on ? (inviteMask | (1 << index)) : (inviteMask & ~(1 << index)));
			PlayerPrefs.SetInt("client.invite", inviteMask);
			PlayerPrefs.Save();
		}
	}
}
