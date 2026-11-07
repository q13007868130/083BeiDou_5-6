using System.Collections.Generic;
using Server.Bot.Social;

public sealed class SocialResponse
{
	public NativeCharacterInfoData characterInfo;

	public bool ok = true;

	public bool party;

	public bool searching;

	public bool messenger;

	public NativeCommandResult command;

	public string error = "";

	public int capacity;

	public int bookLevel = 1;

	public int bookCover;

	public Dictionary<int, int> cards = new Dictionary<int, int>();

	public List<SocialMember> buddies = new List<SocialMember>();

	public int messengerInvite;

	public string messengerInviter;

	public List<SocialMember> messengerMembers = new List<SocialMember>();

	public List<BotMessenger.Entry> messengerMessages = new List<BotMessenger.Entry>();

	public int partyId;

	public int partyLeader;

	public int partyInvite;

	public List<SocialMember> partyMembers = new List<SocialMember>();
}
