public sealed class SocialRequest
{
	public int characterId;

	public SocialOperation operation;

	public NativeChatChannel channel;

	public string target = "";

	public string text = "";

	public string group = "Default Group";

	public int minLevel;

	public int maxLevel;

	public int cardId;

	public int buddyCapacity;

	public int messengerRoom;

	public int partyId;

	public int memberId;

	public long afterMessengerMessage;
}
