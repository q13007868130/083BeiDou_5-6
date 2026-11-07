using System;
using System.IO;
using Server.Bot;

public sealed class GameServer
{
	private BotRuntime bots;

	public DbStore Db { get; private set; }

	public ITransport Transport { get; private set; }

	public IMapInfo MapInfo { get; set; }

	public BotRuntime Bots
	{
		get
		{
			if (bots == null)
			{
				bots = new BotRuntime(Db, Social.BotMessenger, Social.Parties);
				bots.trades.InventoryCommitted += (BotActor actor, long delta) =>
				{
					TradeInventoryCommitted?.Invoke(actor, delta);
				};
			}
			return bots;
		}
	}

	public NativeScriptValues ScriptValues { get; private set; }

	public NativeScriptService Scripts { get; private set; }

	public NativeSocialService Social { get; private set; }

	public NativeCommunityService Community { get; private set; }

	public NativeCashService Cash { get; private set; }

	public NativeNpcShopService NpcShops { get; private set; }

	public NativeBossService Bosses { get; private set; }

	public NativeWeatherService Weather { get; set; }

	public NativeSkillBackdropService Backdrops { get; } = new NativeSkillBackdropService();

	public static GameServer Local { get; private set; }

	public event Action<BotActor, long> TradeInventoryCommitted;

	public BotActor ActiveBotPlayer(int id)
	{
		return bots?.Player(id);
	}

	public static GameServer Start(string dbFileName = "mapstory.db")
	{
		if (Local != null)
		{
			return Local;
		}
		string dbPath = Path.Combine(PlatformRes.PersistentDir(), dbFileName);
		GameServer srv = new GameServer
		{
			Db = DbStore.Open(dbPath)
		};
		srv.ScriptValues = new NativeScriptValues(srv.Db);
		srv.Scripts = new NativeScriptService(srv.Db);
		srv.Social = new NativeSocialService(srv.Db);
		srv.Social.ChatAccepted = srv.RouteBotChat;
		srv.Community = new NativeCommunityService(srv.Db);
		srv.Community.SameChannel = (int a, int b) =>
		{
			BotActor botActor = srv.Social.Parties.Get(a);
			if (botActor != null)
			{
				BotActor botActor2 = srv.Social.Parties.Get(b);
				if (botActor2 != null)
				{
					return botActor.channel == botActor2.channel;
				}
			}
			return false;
		};
		srv.Cash = new NativeCashService(srv.Db);
		srv.Bosses = new NativeBossService(srv.Db);
		srv.NpcShops = new NativeNpcShopService(srv.Db)
		{
			Player = srv.ActiveBotPlayer
		};
		srv.Transport = new InProcessTransport(srv);
		BotWarmup.Start("Bot/Items");
		BotWarmup.Start("Bot/SoloMapling");
		BotWarmup.Start("Bot/ReactorTemplates");
		BotWarmup.Start("Bot/ReactorDrops");
		BotWarmup.Start("Bot/Maps");
		Local = srv;
		NativeMerchantService.MarketProvider = () => srv.Bots.shops;
		NativePlayerShopService.MarketProvider = () => srv.Bots.shops;
		NativeOwlService.MarketProvider = () => srv.Bots.shops;
		DropStore.DbProvider = () => Local?.Db;
		DropStore.Invalidate();
		return srv;
	}

	public static GameServer StartWith(DbStore db)
	{
		GameServer srv = new GameServer
		{
			Db = db
		};
		srv.ScriptValues = new NativeScriptValues(srv.Db);
		srv.Scripts = new NativeScriptService(srv.Db);
		srv.Social = new NativeSocialService(db);
		srv.Social.ChatAccepted = srv.RouteBotChat;
		srv.Community = new NativeCommunityService(db);
		srv.Community.SameChannel = (int a, int b) =>
		{
			BotActor botActor = srv.Social.Parties.Get(a);
			if (botActor != null)
			{
				BotActor botActor2 = srv.Social.Parties.Get(b);
				if (botActor2 != null)
				{
					return botActor.channel == botActor2.channel;
				}
			}
			return false;
		};
		srv.Cash = new NativeCashService(db);
		srv.Bosses = new NativeBossService(db);
		srv.NpcShops = new NativeNpcShopService(srv.Db)
		{
			Player = srv.ActiveBotPlayer
		};
		srv.Transport = new InProcessTransport(srv);
		Local = srv;
		NativeMerchantService.MarketProvider = () => srv.Bots.shops;
		NativePlayerShopService.MarketProvider = () => srv.Bots.shops;
		NativeOwlService.MarketProvider = () => srv.Bots.shops;
		DropStore.DbProvider = () => Local?.Db;
		DropStore.Invalidate();
		return srv;
	}

	public static void Stop()
	{
		if (Local != null)
		{
			Local.bots?.Dispose();
			Local.Db.Save();
			Local = null;
		}
	}

	public void Save()
	{
		Db.Save();
	}

	public void EndBotSession(int id)
	{
		bots?.RemovePlayer(id);
	}

	private void RouteBotChat(SocialRequest request)
	{
		if (request.channel == NativeChatChannel.All)
		{
			bots?.ReceiveChat(request.characterId, request.text);
		}
		else if (request.channel == NativeChatChannel.Whisper)
		{
			bots?.ReceiveWhisper(request.characterId, request.target, request.text);
		}
	}
}
