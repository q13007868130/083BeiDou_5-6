namespace Server.Bot;

public sealed class BotClient
{
	public const string ClientIp = "127.0.0.1";

	public static long SessionId = 6969L;

	private static BotClient botClient;

	public DbStore db;

	public int world;

	public int channel = 1;

	public int accId;

	public int gmLevel;

	public BotCharacter player;

	public static BotClient EnsureStandaloneClient(DbStore db)
	{
		if (BotClient.botClient != null)
		{
			if (db != null)
			{
				BotClient.botClient.db = db;
			}
			return BotClient.botClient;
		}
		BotClient botClient = new BotClient
		{
			db = db,
			world = 0,
			channel = 1
		};
		BotCharacter botCharacter = new BotCharacter
		{
			id = 0,
			name = "Host",
			client = botClient
		};
		botClient.player = botCharacter;
		BotClient.botClient = botClient;
		return BotClient.botClient;
	}

	public static BotClient GetBotClient()
	{
		return botClient;
	}

	public static void Init(DbStore db)
	{
		EnsureStandaloneClient(db);
	}
}
