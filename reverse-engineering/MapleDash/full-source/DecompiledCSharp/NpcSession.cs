using System;
using System.Collections.Generic;
using System.Diagnostics;
using MapleDash.Js;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public sealed class NpcSession
{
	public readonly int NpcId;

	public readonly int QuestId;

	public readonly bool IsQuest;

	public readonly bool QuestEnd;

	public readonly NpcDialog Dialog = new NpcDialog();

	public bool Disposed;

	public bool Closed;

	public string LastError;

	public string SpeakerName;

	private int scriptSessionId;

	private int scriptCharacterId;

	private bool hasStart;

	private bool hasAction;

	private bool hasEnd;

	public BotCharacter Actor;

	public string LastSay;

	private ITransport scriptTransport;

	private NativeScriptService offlineScripts;

	public readonly NativeScriptRoute Route = new NativeScriptRoute();

	public FieldPortal Portal;

	private JsHostObject cm;

	private int pendingWarp = -1;

	private string pendingWarpPortal;

	private int pendingWarpPortalId = -1;

	private int pendingNpc = -1;

	private string pendingNpcScript;

	private Action<int, int> localResponse;

	private bool localKeepsSession;

	public static bool Silent;

	public static NpcSession Active { get; private set; }

	public string ScriptName { get; private set; }

	public int ItemId { get; private set; }

	public JsValue CmHost => cm;

	public static bool Blocking
	{
		get
		{
			if (Active != null && !Active.Closed)
			{
				return Active.Dialog.Visible;
			}
			return false;
		}
	}

	private ScriptResponse Dispatch(ScriptRequest request)
	{
		request.characterId = scriptCharacterId;
		request.sessionId = scriptSessionId;
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			using (Profile.DeferSaves())
			{
				using (GameServer.Local?.Db.DeferSaves())
				{
					return (scriptTransport != null) ? scriptTransport.Script(request) : offlineScripts.Handle(request);
				}
			}
		}
		finally
		{
			if (stopwatch.ElapsedMilliseconds >= 100)
			{
				Debug.LogWarning((object)("[NPC slow] npc=" + NpcId + " quest=" + QuestId + " script=" + ScriptName + " operation=" + request.operation.ToString() + " function=" + request.function + " elapsedMs=" + stopwatch.ElapsedMilliseconds));
			}
		}
	}

	private void CallScript(string function, params JsValue[] arguments)
	{
		Dispatch(new ScriptRequest
		{
			operation = ScriptOperation.Call,
			function = function,
			arguments = arguments
		});
	}

	private void ReleaseScript()
	{
		if (scriptSessionId != 0)
		{
			Dispatch(new ScriptRequest
			{
				operation = ScriptOperation.Close
			});
			scriptSessionId = 0;
		}
	}

	public static NpcSession Choose(int npcId, string text, Action<int> choose)
	{
		CloseActive();
		NpcSession npcSession = new NpcSession(npcId, 0, isQuest: false, questEnd: false);
		npcSession.localResponse = (int mode, int selection) =>
		{
			if (mode == 1)
			{
				choose(selection);
			}
		};
		npcSession.SetDialog(NpcDialogMode.Simple, text);
		npcSession.Dialog.IsMenu = true;
		Active = npcSession;
		return npcSession;
	}

	public static NpcSession Conversation(int npcId, string speakerName, Action<NpcSession, int> reply)
	{
		CloseActive();
		NpcSession session = new NpcSession(npcId, 0, isQuest: false, questEnd: false)
		{
			SpeakerName = speakerName
		};
		session.localKeepsSession = true;
		session.localResponse = (int mode, int selection) =>
		{
			reply(session, mode);
		};
		Active = session;
		return session;
	}

	internal static NpcSession MapContext()
	{
		return new NpcSession(0, 0, isQuest: false, questEnd: false);
	}

	private NpcSession(int npcId, int questId, bool isQuest, bool questEnd)
	{
		NpcId = npcId;
		QuestId = questId;
		IsQuest = isQuest;
		QuestEnd = questEnd;
	}

	public static NpcSession UseItem(int itemId)
	{
		if (!GameSession.InGame || Profile.ItemCount(itemId) <= 0 || Blocking)
		{
			return null;
		}
		ImgNode imgNode = WzAssets.Node(string.Concat("Item/Consume/" + (itemId / 10000).ToString("D4") + ".img/" + itemId.ToString("D8"), "/spec"));
		string text = imgNode?.Text("script");
		if (string.IsNullOrEmpty(text))
		{
			UseRestorative(itemId);
			return null;
		}
		string text2 = "item/" + text;
		string text3 = ScriptSource.LoadKey(text2);
		if (string.IsNullOrEmpty(text3))
		{
			return null;
		}
		CloseActive();
		NpcSession npcSession = new NpcSession(imgNode.Int("npc"), 0, isQuest: false, questEnd: false)
		{
			ItemId = itemId
		};
		Active = npcSession;
		npcSession.Begin(text3, text2);
		npcSession.FinishInvocation();
		return npcSession;
	}

	public static bool UseRestorative(int itemId)
	{
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		if (itemId / 1000000 != 2 || (Object)(object)playerCtrl == (Object)null || playerCtrl.hp <= 0f || Blocking || Profile.ItemCount(itemId) <= 0)
		{
			return false;
		}
		ImgNode imgNode = NativeItemSource.Node(itemId)?.Get("spec");
		if (imgNode == null || !string.IsNullOrEmpty(imgNode.Text("script")) || imgNode.Int("time") != 0)
		{
			return false;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Name != "hp" && child.Name != "mp" && child.Name != "hpR" && child.Name != "mpR")
			{
				return false;
			}
		}
		int num = (short)imgNode.Int("hp");
		int num2 = (short)imgNode.Int("mp");
		int num3 = imgNode.Int("hpR");
		int num4 = imgNode.Int("mpR");
		if (num == 0 && num2 == 0 && num3 == 0 && num4 == 0)
		{
			return false;
		}
		int jobId = Profile.JobId;
		int num5 = ((jobId / 10 == 41) ? 4110000 : ((jobId / 10 == 141) ? 14110003 : 0));
		int num6 = ((num5 != 0) ? SkillProgress.LevelOf(num5) : 0);
		if (num6 > 0)
		{
			ImgNode imgNode2 = WzAssets.Node("Skill/" + (num5 / 10000).ToString("D3") + ".img/skill/" + num5 + "/level/" + num6);
			if (imgNode2 != null)
			{
				num = (int)((double)num * ((double)imgNode2.Int("x") / 100.0));
				num2 = (int)((double)num2 * ((double)imgNode2.Int("x") / 100.0));
			}
		}
		int num7 = (int)((double)playerCtrl.hpMax * ((double)num3 / 100.0));
		NativeMobDebuffs component = ((Component)playerCtrl).GetComponent<NativeMobDebuffs>();
		if (component != null && component.Has(133))
		{
			num /= 2;
			num7 /= 2;
		}
		if (!Profile.RemoveItem(itemId, 1))
		{
			return false;
		}
		playerCtrl.hp = Mathf.Clamp(playerCtrl.hp + (float)num + (float)num7, 0f, playerCtrl.hpMax);
		playerCtrl.mp = Mathf.Clamp(playerCtrl.mp + (float)num2 + (float)(int)((double)playerCtrl.mpMax * ((double)num4 / 100.0)), 0f, playerCtrl.mpMax);
		GameSession.SaveToDb();
		return true;
	}

	public static NpcSession Talk(int npcId)
	{
		return Talk(npcId, null);
	}

	public static NpcSession Talk(int npcId, string scriptName)
	{
		CloseActive();
		if (npcId == 3003104)
		{
			WeeklyGemstoneQuest.Read(force: true);
		}
		switch (npcId)
		{
		case 9010009:
			NativeDeliveryWindow.Open(quick: false);
			return null;
		case 9030000:
			NativeMerchantWindow.Retrieve();
			return null;
		default:
		{
			string text = ScriptSource.LoadNpc(npcId, scriptName, out var key);
			if (string.IsNullOrEmpty(text))
			{
				if (NativeNpcShopWindow.HasNpc(npcId))
				{
					NativeNpcShopWindow.OpenNpc(npcId);
					return null;
				}
				BottomBar.Toast(WzNames.Npc(npcId) + " 没有对白脚本");
				return null;
			}
			NpcSession npcSession = (Active = new NpcSession(npcId, 0, isQuest: false, questEnd: false));
			npcSession.Begin(text, key);
			npcSession.FinishInvocation();
			return npcSession;
		}
		}
	}

	public static NpcSession TalkAs(int npcId, BotCharacter actor, string scriptName = null)
	{
		if (actor == null)
		{
			return null;
		}
		CloseActive();
		string text = ScriptSource.LoadNpc(npcId, scriptName, out var key);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		NpcSession npcSession = new NpcSession(npcId, 0, isQuest: false, questEnd: false)
		{
			Actor = actor
		};
		Active = npcSession;
		npcSession.Begin(text, key);
		npcSession.FinishInvocation();
		return npcSession;
	}

	public bool Advance()
	{
		if (Disposed || Closed)
		{
			return false;
		}
		Dialog.Mode = NpcDialogMode.None;
		CallScript("action", new JsNumber(1.0), new JsNumber(0.0), new JsNumber(-1.0));
		FinishInvocation();
		if (!Disposed)
		{
			return !Closed;
		}
		return false;
	}

	public static bool RunPortal(FieldPortal portal)
	{
		if (portal == null || string.IsNullOrEmpty(portal.script))
		{
			return false;
		}
		string text = ScriptSource.Load("portal", portal.script);
		if (string.IsNullOrEmpty(text) || !text.Contains("function enter"))
		{
			return false;
		}
		CloseActive();
		NpcSession npcSession = (Active = new NpcSession(0, 0, isQuest: false, questEnd: false)
		{
			Portal = portal
		});
		npcSession.Begin(text, "portal/" + portal.script, "enter");
		npcSession.FinishInvocation();
		return true;
	}

	public static NpcSession TalkQuest(QuestDef quest, int npcId, bool end)
	{
		CloseActive();
		string reason = null;
		bool num;
		if (!end)
		{
			num = QuestLogic.CanStart(quest, npcId, out reason);
		}
		else
		{
			if (QuestLogic.InProgressAt(quest, npcId))
			{
				goto IL_0034;
			}
			num = QuestLogic.CanComplete(quest, npcId, out reason);
		}
		if (!num)
		{
			BottomBar.Toast(reason);
			return null;
		}
		goto IL_0034;
		IL_0034:
		NpcSession npcSession = new NpcSession(npcId, quest.id, isQuest: true, end);
		string text = ScriptSource.Load("quest", quest.id);
		if (string.IsNullOrEmpty(text))
		{
			npcSession.BeginDataQuest(quest, npcId, end);
			Active = npcSession;
			return npcSession;
		}
		Active = npcSession;
		npcSession.Begin(text, "quest/" + quest.id);
		npcSession.FinishInvocation();
		return npcSession;
	}

	private static List<string> QuestPages(ImgNode node)
	{
		List<string> list = new List<string>();
		int num = 0;
		while (node != null && node.Get(num.ToString()) != null)
		{
			list.Add(node.Text(num.ToString()));
			num++;
		}
		return list;
	}

	private void BeginDataQuest(QuestDef quest, int npcId, bool end)
	{
		ImgNode say = WzAssets.FileRoot("Quest/Say.img").Get(quest.id + "/" + (end ? "1" : "0"));
		List<string> pages = QuestPages(say);
		int val = (end ? Math.Max(0, quest.info.Length - 1) : 0);
		if (pages.Count == 0)
		{
			pages.Add((quest.info.Length != 0) ? quest.info[Math.Min(val, quest.info.Length - 1)] : quest.name);
		}
		int page = 0;
		if (end)
		{
			QuestLogic.RestoreLostItems(quest);
		}
		bool branch = end && !QuestLogic.CanComplete(quest, npcId);
		if (branch)
		{
			List<string> list = QuestPages(say?.Get("stop/" + QuestLogic.IncompleteStop(quest)));
			pages = ((list.Count > 0) ? list : new List<string> { (quest.info.Length > 1) ? quest.info[1] : quest.name });
		}
		bool asking = say != null && say.Int("ask") != 0;
		Action show = () =>
		{
			NpcSession npcSession = this;
			NpcDialogMode mode = (((!branch & asking) && pages[page].Contains("#L")) ? NpcDialogMode.Simple : ((!branch) ? ((page == pages.Count - 1) ? NpcDialogMode.AcceptDecline : ((page == 0) ? NpcDialogMode.Next : NpcDialogMode.NextPrev)) : ((page != pages.Count - 1) ? NpcDialogMode.Next : NpcDialogMode.Ok)));
			npcSession.SetDialog(mode, pages[page]);
		};
		localKeepsSession = true;
		localResponse = (int mode, int selection) =>
		{
			if (mode < 0)
			{
				Close();
			}
			else if (branch)
			{
				if (mode == 1 && ++page < pages.Count)
				{
					show();
				}
				else
				{
					Close();
				}
			}
			else
			{
				if (asking && pages[page].Contains("#L"))
				{
					if (mode != 1)
					{
						Close();
						return;
					}
					if (!DialogFlow.Links(Dialog.Content).Contains(selection))
					{
						return;
					}
					string text = say?.Text("stop/" + page + "/" + selection);
					if (!string.IsNullOrEmpty(text))
					{
						pages = new List<string> { text };
						page = 0;
						branch = true;
						show();
						return;
					}
				}
				if (page < pages.Count - 1)
				{
					page = Math.Max(0, page + ((mode == 1) ? 1 : (-1)));
					show();
				}
				else
				{
					if (mode == 1)
					{
						if (!(end ? QuestLogic.CanComplete(quest, npcId, out var reason) : QuestLogic.CanStart(quest, npcId, out reason)))
						{
							BottomBar.Toast(reason);
							Close();
							return;
						}
						if (end)
						{
							QuestLogic.Complete(quest, npcId, applyActions: true);
						}
						else
						{
							QuestLogic.Start(quest, npcId, applyActions: true);
						}
						NpcField.InvalidateMarkers();
						QuestDef questDef = (end ? QuestLogic.ChainedNext(quest.completeAct, npcId) : null);
						if (questDef != null)
						{
							Close();
							TalkQuest(questDef, npcId, end: false);
							return;
						}
					}
					pages = QuestPages(say?.Get((mode == 1) ? "yes" : "no"));
					page = 0;
					branch = true;
					if (pages.Count > 0)
					{
						show();
					}
					else
					{
						Close();
					}
				}
			}
		};
		show();
	}

	private void Begin(string source, string name)
	{
		Begin(source, name, "start");
	}

	private void Begin(string source, string name, string entry)
	{
		try
		{
			int num = name.IndexOf('/');
			ScriptName = ((num < 0) ? name : name.Substring(num + 1));
			scriptTransport = GameSession.Net;
			scriptCharacterId = ((Actor != null) ? Actor.id : GameSession.CharacterId);
			if (scriptTransport == null)
			{
				offlineScripts = new NativeScriptService();
			}
			cm = ((Actor != null) ? BotNpcApi.Build(this, Actor) : NpcApi.Build(this, NpcId, QuestId, IsQuest));
			Dictionary<string, JsValue> bindings = new Dictionary<string, JsValue>
			{
				[IsQuest ? "qm" : "cm"] = cm,
				["pi"] = cm,
				["im"] = cm,
				["player"] = ((Actor != null) ? BotNpcApi.Player(this, Actor) : NpcApi.BuildPlayer(this)),
				["Java"] = NpcApi.BuildJava()
			};
			ScriptResponse scriptResponse = Dispatch(new ScriptRequest
			{
				operation = ScriptOperation.Open,
				name = name,
				source = source,
				bindings = bindings
			});
			scriptSessionId = scriptResponse.sessionId;
			hasStart = scriptResponse.hasStart;
			hasAction = scriptResponse.hasAction;
			hasEnd = scriptResponse.hasEnd;
			if (IsQuest)
			{
				if (QuestEnd ? hasEnd : hasStart)
				{
					CallScript(QuestEnd ? "end" : "start", new JsNumber(1.0), new JsNumber(0.0), new JsNumber(0.0));
				}
				else
				{
					Disposed = true;
				}
			}
			else if (entry != "start")
			{
				CallScript(entry, cm);
			}
			else if (!hasStart)
			{
				Disposed = true;
			}
			else
			{
				CallScript("start");
			}
		}
		catch (JsRuntimeException e)
		{
			Fail(e);
		}
		catch (Exception e2)
		{
			Fail(e2);
		}
	}

	public void Respond(int mode, int type, int selection)
	{
		if (Closed)
		{
			return;
		}
		if (Disposed)
		{
			Close();
			return;
		}
		try
		{
			if (localResponse != null)
			{
				Action<int, int> action = localResponse;
				if (localKeepsSession)
				{
					Dialog.Mode = NpcDialogMode.None;
				}
				else
				{
					Close();
				}
				action(mode, selection);
				return;
			}
			Dialog.Mode = NpcDialogMode.None;
			if (Route.type != NativeScriptRouteType.None)
			{
				if (!Route.Resolve(mode, selection, Dialog.Input, out var function, out var arguments))
				{
					Close();
					return;
				}
				CallScript(function, arguments);
			}
			else if (IsQuest)
			{
				CallScript(QuestEnd ? "end" : "start", new JsNumber(mode), new JsNumber(type), new JsNumber(selection));
			}
			else if (hasAction)
			{
				CallScript("action", new JsNumber(mode), new JsNumber(type), new JsNumber(selection));
			}
			else
			{
				Close();
			}
			FinishInvocation();
		}
		catch (JsRuntimeException e)
		{
			Fail(e);
		}
		catch (Exception e2)
		{
			Fail(e2);
		}
	}

	public void Reply(int mode, int selection = -1)
	{
		Respond(mode, Dialog.MessageType, selection);
	}

	public void Cancel()
	{
		if (Dialog.Mode == NpcDialogMode.GetText)
		{
			Close();
			return;
		}
		Reply(-1);
		Close();
	}

	private void FinishInvocation()
	{
		if (Disposed)
		{
			ReleaseScript();
		}
		if (!Closed && Disposed && !Dialog.Visible)
		{
			Close();
		}
	}

	public void SetDialog(NpcDialogMode mode, string text)
	{
		if (!Closed)
		{
			Dialog.Set(mode, text);
		}
	}

	public void DisposeFromScript()
	{
		Disposed = true;
	}

	public void RequestWarp(int mapId, string portalName = null, int portalId = -1)
	{
		pendingWarp = mapId;
		pendingWarpPortal = portalName;
		pendingWarpPortalId = portalId;
		CloseImmediate();
	}

	public void RequestOpenNpc(int npcId, string scriptName = null)
	{
		if (scriptName == "undefined" || scriptName == "null")
		{
			scriptName = null;
		}
		if (string.IsNullOrEmpty(scriptName))
		{
			scriptName = null;
		}
		pendingNpc = npcId;
		pendingNpcScript = scriptName;
		CloseImmediate();
	}

	public void Close()
	{
		if (!Closed)
		{
			CloseImmediate();
		}
	}

	private void CloseImmediate()
	{
		ReleaseScript();
		Closed = true;
		Disposed = true;
		Dialog.Hide();
		if (Active == this)
		{
			Active = null;
		}
		ApplyPending();
	}

	private void ApplyPending()
	{
		int num = pendingWarp;
		pendingWarp = -1;
		string portalName = pendingWarpPortal;
		pendingWarpPortal = null;
		int portalIndex = pendingWarpPortalId;
		pendingWarpPortalId = -1;
		int num2 = pendingNpc;
		pendingNpc = -1;
		string scriptName = pendingNpcScript;
		pendingNpcScript = null;
		if (num >= 0)
		{
			FieldDirector fieldDirector = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.field : null);
			if ((Object)(object)fieldDirector != (Object)null)
			{
				fieldDirector.WarpTo(num, portalName, portalIndex);
			}
			else
			{
				Profile.LastMapId = num;
			}
		}
		if (num2 > 0)
		{
			Talk(num2, scriptName);
		}
	}

	public static void Tick()
	{
		if (Active != null && (Active.pendingWarp >= 0 || Active.pendingNpc > 0))
		{
			Active.ApplyPending();
		}
	}

	public static void CloseActive()
	{
		if (Active != null)
		{
			Active.Close();
		}
		Active = null;
	}

	private void Fail(Exception e)
	{
		LastError = e.Message;
		Debug.LogError((object)("[Script] " + (IsQuest ? ("quest/" + QuestId) : ("npc/" + NpcId)) + " :: " + e.Message));
		string text = null;
		string text2 = null;
		if (e.Message != null && e.Message.StartsWith("Unmigrated server API: ", StringComparison.Ordinal))
		{
			text = e.Message.Substring("Unmigrated server API: ".Length);
		}
		else if (e.Message != null && e.Message.StartsWith("Java interop: class not migrated: ", StringComparison.Ordinal))
		{
			text2 = e.Message.Substring("Java interop: class not migrated: ".Length);
		}
		string text3;
		if (text != null)
		{
			text3 = "该功能尚未接入（缺少 " + text + "）";
		}
		else
		{
			text3 = ((text2 != null) ? ("该功能需要未接入的服务端类 " + text2) : "对话脚本出错（详见日志）");
		}
		BottomBar.Toast(text3);
		Dialog.Hide();
		CloseImmediate();
	}
}
