namespace Server.Bot.Social;

public sealed class ChatMessage
{
	public readonly BotActor sender;

	public readonly string content;

	public readonly int mapId;

	public readonly long timestamp;

	public ChatMessage(BotActor sender, string content)
	{
		this.sender = sender;
		this.content = content;
		mapId = sender.mapId;
		timestamp = BotScheduler.Now();
	}
}
