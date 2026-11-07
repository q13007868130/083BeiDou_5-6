using System;
using System.Collections.Generic;
using System.Threading;
using Server.Bot;
using Server.Bot.Environment;
using UnityEngine;

public sealed class AdminHost : MonoBehaviour
{
	public const int DefaultPort = 8686;

	public const string ResourcePath = "AdminUi/adminui";

	private static AdminHost instance;

	private AdminHttpServer server;

	private AdminApi api;

	private string startError;

	private bool adminChecked;

	private Thread pumpThread;

	private volatile bool running = true;

	public static bool Running
	{
		get
		{
			if ((Object)(object)instance != (Object)null && instance.server != null)
			{
				return instance.server.Running;
			}
			return false;
		}
	}

	public static int Port
	{
		get
		{
			if (!((Object)(object)instance != (Object)null) || instance.server == null)
			{
				return 8686;
			}
			return instance.server.Port;
		}
	}

	public static string BaseUrl => "http://127.0.0.1:" + Port + "/";

	public static AdminHttpServer ServerForTest
	{
		get
		{
			if (!((Object)(object)instance != (Object)null))
			{
				return null;
			}
			return instance.server;
		}
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void Boot()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected Obj, but got Unknown
		if (!((Object)(object)instance != (Object)null))
		{
			GameObject val = new GameObject("AdminHost");
			Object.DontDestroyOnLoad((Object)val);
			instance = val.AddComponent<AdminHost>();
		}
	}

	private void Awake()
	{
		if ((Object)(object)instance != (Object)null && (Object)(object)instance != (Object)(object)this)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		instance = this;
		AdminMainThread.MainThreadId = Thread.CurrentThread.ManagedThreadId;
		AdminMainThread.Pumping = true;
		AdminMainThread.OnError = (Exception e) =>
		{
			Debug.LogError((object)("[Admin] 主线程任务异常: " + e));
		};
		AdminBackend.Broadcast = (string msg) =>
		{
			try
			{
				BottomBar.Toast(msg);
			}
			catch (Exception ex)
			{
				Debug.LogWarning((object)("[Admin] 广播失败: " + ex.Message));
			}
		};
		AdminBackend.CurrentCharacterId = () => GameSession.CharacterId;
		AdminNames.Mob = WzNames.Mob;
		AdminNames.Npc = WzNames.Npc;
		AdminNames.Item = WzNames.Item;
		AdminNames.Map = WzNames.Map;
		AdminNames.ItemDesc = ItemCatalog.Desc;
		AdminNames.Quest = (int id) =>
		{
			string text = WzAssets.FileRoot("Quest/QuestInfo.img").Get(id.ToString())?.Text("name");
			return (!string.IsNullOrEmpty(text)) ? text : null;
		};
		Application.runInBackground = true;
		pumpThread = new Thread(BackgroundPump)
		{
			IsBackground = true,
			Name = "AdminBackgroundPump"
		};
		pumpThread.Start();
	}

	private void BackgroundPump()
	{
		bool flag = false;
		while (running)
		{
			if (AdminMainThread.Foreground)
			{
				flag = false;
				Thread.Sleep(100);
			}
			else if (!flag)
			{
				flag = true;
				Thread.Sleep(300);
			}
			else if (AdminMainThread.Pump() == 0)
			{
				Thread.Sleep(25);
			}
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		SwitchForeground(hasFocus);
	}

	private void OnApplicationPause(bool paused)
	{
		SwitchForeground(!paused);
	}

	private void SwitchForeground(bool has)
	{
		if (!has)
		{
			AdminMainThread.Foreground = false;
			return;
		}
		AdminMainThread.Foreground = true;
		AdminMainThread.AwaitIdle(500);
	}

	private void Update()
	{
		AdminMainThread.Pump();
		if (adminChecked || GameServer.Local == null)
		{
			return;
		}
		try
		{
			string text = AdminBackend.EnsureWebAdmin();
			adminChecked = true;
			if (!string.IsNullOrEmpty(text))
			{
				Debug.Log((object)("[Admin] " + text));
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Admin] 初始化后台账号失败: " + ex.Message));
		}
	}

	private void OnDestroy()
	{
		if ((Object)(object)instance == (Object)(object)this)
		{
			instance = null;
		}
		running = false;
		pumpThread = null;
		StopServer();
	}

	private void OnApplicationQuit()
	{
		AdminWebView.Close();
		StopServer();
	}

	public static string EnsureStarted()
	{
		if ((Object)(object)instance == (Object)null)
		{
			Boot();
		}
		if ((Object)(object)instance == (Object)null)
		{
			throw new InvalidOperationException("后台宿主未初始化");
		}
		if (instance.server != null && instance.server.Running)
		{
			return BaseUrl;
		}
		if (!string.IsNullOrEmpty(instance.startError))
		{
			throw new InvalidOperationException(instance.startError);
		}
		instance.StartServer();
		return BaseUrl;
	}

	public static string OpenInBrowser()
	{
		string text = EnsureStarted();
		AdminWebView.Open(text);
		return text;
	}

	private void StartServer()
	{
		try
		{
			api = new AdminApi
			{
				Log = (string msg) =>
				{
					Debug.Log((object)("[Admin] " + msg));
				}
			};
			TextAsset val = Resources.Load<TextAsset>("AdminUi/adminui");
			if ((Object)(object)val != (Object)null && val.bytes != null && val.bytes.Length != 0)
			{
				api.Statics = AdminStaticBundle.Load(val.bytes);
				Debug.Log((object)("[Admin] 后台前端资源包已加载，共 " + api.Statics.Count + " 个文件"));
			}
			else
			{
				Debug.LogWarning((object)"[Admin] 找不到 Resources/AdminUi/adminui.bytes，后台页面将只显示构建提示。先跑 tools/build_admin_ui.sh 生成。");
			}
			AdminRoutes.Register(api);
			AdminRoutes.RegisterUnityOnly(api);
			AdminBots.ActiveBots = () => BotRegistry.Count();
			AdminBots.MarketStartupComplete = () => BotEnvironment.IsMarketStartupComplete;
			AdminBots.MarketStartupRunning = () => BotEnvironment.IsMarketStartupRunning;
			AdminBots.RequestMarketStartup = BotEnvironment.RequestMarketStartup;
			AdminBots.PartyTeams = () =>
			{
				List<AdminBots.PartyTeamInfo> list = new List<AdminBots.PartyTeamInfo>();
				BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
				BotRuntime botRuntime = ((GameSession.Server != null) ? GameSession.Server.Bots : null);
				if (partyGrind == null || botRuntime == null)
				{
					return list;
				}
				long num = BotScheduler.Now();
				foreach (int activeLeader in partyGrind.ActiveLeaders)
				{
					BotActor botActor = botRuntime.Player(activeLeader);
					list.Add(new AdminBots.PartyTeamInfo
					{
						characterId = activeLeader,
						name = ((botActor != null) ? botActor.name : ""),
						mapId = (botActor?.mapId ?? 0),
						companions = partyGrind.CompanionCountOf(activeLeader),
						remainingSec = Math.Max(0L, (partyGrind.Expiry(activeLeader) - num) / 1000)
					});
				}
				return list;
			};
			AdminBots.PartySummon = (long characterId) =>
			{
				BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
				BotRuntime botRuntime = ((GameSession.Server != null) ? GameSession.Server.Bots : null);
				if (partyGrind == null || botRuntime == null)
				{
					return "服务端未启动（请先进入游戏）";
				}
				BotActor botActor = botRuntime.Player((int)characterId);
				if (botActor == null)
				{
					return "角色不在线";
				}
				if (partyGrind.Start(botActor))
				{
					return (string)null;
				}
				return partyGrind.IsActive((int)characterId) ? "基友集合仍在生效中，先解散再召唤" : ("召唤失败：检查后台开关、队伍空位（需要 " + partyGrind.ConfiguredCount + " 个）");
			};
			AdminBots.PartyDismiss = (long characterId) =>
			{
				BotPartyGrindService partyGrind = BotTypeManager.PartyGrind;
				if (partyGrind == null)
				{
					return false;
				}
				partyGrind.Stop((int)characterId, "管理员已解散基友集合。");
				return true;
			};
			server = AdminHttpServer.StartOn(8686, api.Handle, (string msg) =>
			{
				Debug.Log((object)("[Admin] " + msg));
			});
			AdminCatalogData.Prewarm();
			Debug.Log((object)("[Admin] 后台管理已就绪: " + BaseUrl + "（仅本机可访问）"));
			startError = null;
		}
		catch (Exception ex)
		{
			startError = "后台服务启动失败：" + ex.Message;
			Debug.LogError((object)("[Admin] " + startError));
			throw;
		}
	}

	private void StopServer()
	{
		try
		{
			server?.Dispose();
		}
		catch (Exception)
		{
		}
		server = null;
	}
}
