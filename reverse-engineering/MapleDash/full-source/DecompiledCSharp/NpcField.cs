using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NpcField
{
	public struct Marker
	{
		public bool start;

		public bool complete;

		public bool progress;
	}

	public sealed class QuestChoice
	{
		public int npcId;

		public QuestDef quest;

		public bool complete;

		public bool progress;

		public bool welcome;

		public string Title
		{
			get
			{
				if (!welcome)
				{
					return quest.name;
				}
				return "#r[北斗冒险岛]#k来自北斗开发者的问候。" + (complete ? " (已完成)" : "");
			}
		}

		public int AlertId
		{
			get
			{
				if (!welcome)
				{
					return quest.id;
				}
				return int.MinValue;
			}
		}
	}

	private static FieldMap map;

	private static readonly Dictionary<int, Marker> markers = new Dictionary<int, Marker>();

	private static float markerRefresh;

	private static readonly List<QuestChoice> alerts = new List<QuestChoice>();

	private static float alertsRefresh;

	public const string AlertAppear = "Effect/BasicEff.img/QuestAlert/Appear";

	public const string AlertDefault = "Effect/BasicEff.img/QuestAlert/Default";

	private static readonly HashSet<int> alertSeen = new HashSet<int>();

	private static readonly HashSet<int> alertLive = new HashSet<int>();

	private static readonly List<int> alertGone = new List<int>();

	private static float alertAppearAt = float.NegativeInfinity;

	private static float alertAppearLength = -1f;

	public static FieldMap Map => map;

	private static float Px => 40f;

	public static float AlertAppearStartedAt => alertAppearAt;

	public static void Bind(FieldMap m)
	{
		map = m;
		markers.Clear();
		markerRefresh = 0f;
		alertsRefresh = 0f;
		alertSeen.Clear();
		alertAppearAt = float.NegativeInfinity;
	}

	public static void Clear()
	{
		map = null;
		markers.Clear();
	}

	public static FieldNpc Nearest(Vector2 feetWorld, float maxPx = 110f)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (map == null)
		{
			return null;
		}
		FieldNpc result = null;
		float num = maxPx / Px;
		for (int i = 0; i < map.npcs.Count; i++)
		{
			FieldNpc fieldNpc = map.npcs[i];
			if (fieldNpc.hide == 0)
			{
				float num2 = map.WorldX(fieldNpc.x);
				float num3 = map.WorldY(fieldNpc.y);
				float num4 = Mathf.Abs(feetWorld.x - num2);
				float num5 = Mathf.Abs(feetWorld.y - num3);
				float num6 = Mathf.Max(num4, num5 * 0.6f);
				if (num6 < num)
				{
					num = num6;
					result = fieldNpc;
				}
			}
		}
		return result;
	}

	public static FieldNpc Pick(Vector2 guiPos)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (map == null || (Object)(object)Camera.main == (Object)null)
		{
			return null;
		}
		float scale = UiKit.Scale;
		Vector3 val = new Vector3(guiPos.x * scale, (float)Screen.height - guiPos.y * scale, 0f);
		Vector3 val2 = Camera.main.ScreenToWorldPoint(val);
		FieldNpc result = null;
		float num = float.MaxValue;
		for (int i = 0; i < map.npcs.Count; i++)
		{
			FieldNpc fieldNpc = map.npcs[i];
			if (fieldNpc.hide != 0)
			{
				continue;
			}
			float num2 = map.WorldX(fieldNpc.x);
			float num3 = map.WorldY(fieldNpc.y);
			float num4 = val2.x - num2;
			float num5 = val2.y - num3;
			if (!(Mathf.Abs(num4) > 34f / Px) && !(num5 < -12f / Px) && !(num5 > 120f / Px))
			{
				float num6 = Mathf.Abs(num4);
				if (num6 < num)
				{
					num = num6;
					result = fieldNpc;
				}
			}
		}
		return result;
	}

	public static Marker MarkerOf(int npcId)
	{
		if (Time.unscaledTime >= markerRefresh)
		{
			markerRefresh = Time.unscaledTime + 0.4f;
			markers.Clear();
		}
		if (markers.TryGetValue(npcId, out var value))
		{
			return value;
		}
		value = new Marker
		{
			complete = (npcId == 2104 && WelcomeTutorial.CanComplete)
		};
		List<int> list = QuestCatalog.ForNpc(npcId);
		for (int i = 0; i < list.Count; i++)
		{
			QuestDef questDef = QuestCatalog.Get(list[i]);
			if (questDef != null && (questDef.id < 1200 || questDef.id >= 1400))
			{
				if (QuestLogic.CanComplete(questDef, npcId))
				{
					value.complete = true;
					break;
				}
				if (QuestLogic.InProgressAt(questDef, npcId))
				{
					value.progress = true;
				}
				if (!value.start && QuestLogic.CanStart(questDef, npcId))
				{
					value.start = true;
				}
			}
		}
		markers[npcId] = value;
		return value;
	}

	public static void InvalidateMarkers()
	{
		markers.Clear();
		alertsRefresh = 0f;
	}

	public static List<QuestChoice> ChoicesFor(int npcId)
	{
		foreach (int item in QuestCatalog.ForNpc(npcId))
		{
			if (item == 34151 || NativeWeeklyGemstoneService.IsDaily(item))
			{
				WeeklyGemstoneQuest.Read(force: true);
				break;
			}
		}
		List<QuestChoice> list = new List<QuestChoice>();
		if (npcId == 2104 && WelcomeTutorial.CanComplete)
		{
			list.Add(new QuestChoice
			{
				npcId = npcId,
				welcome = true,
				complete = true
			});
		}
		foreach (int item2 in QuestCatalog.ForNpc(npcId))
		{
			QuestDef questDef = QuestCatalog.Get(item2);
			if (questDef != null && (questDef.id < 1200 || questDef.id >= 1400))
			{
				if (QuestLogic.CanComplete(questDef, npcId))
				{
					list.Add(new QuestChoice
					{
						npcId = npcId,
						quest = questDef,
						complete = true
					});
				}
				else if (QuestLogic.CanStart(questDef, npcId))
				{
					list.Add(new QuestChoice
					{
						npcId = npcId,
						quest = questDef
					});
				}
				else if (QuestLogic.InProgressAt(questDef, npcId))
				{
					list.Add(new QuestChoice
					{
						npcId = npcId,
						quest = questDef,
						progress = true
					});
				}
			}
		}
		list.Sort((QuestChoice a, QuestChoice b) => (a.complete != b.complete) ? ((!a.complete) ? 1 : (-1)) : a.AlertId.CompareTo(b.AlertId));
		return list;
	}

	public static string DefaultTalk(int npcId)
	{
		return WzAssets.FileRoot("String/Npc.img").Get(npcId.ToString())?.Text("d0") ?? "";
	}

	private static bool IsSilentNpc(int npcId)
	{
		ImgNode imgNode = WzAssets.FileRoot("Npc/" + npcId.ToString("D7") + ".img").Get("info");
		if (imgNode == null)
		{
			return false;
		}
		if (imgNode.Int("MapleTV") == 1)
		{
			return true;
		}
		ImgNode imgNode2 = imgNode.Get("speak");
		if (imgNode2 != null && imgNode2.Children.Count > 0)
		{
			return !ScriptSource.Has("npc", npcId);
		}
		return false;
	}

	public static void Interact(int npcId)
	{
		if (NpcSession.Blocking)
		{
			return;
		}
		List<QuestChoice> choices = ChoicesFor(npcId);
		List<string> scripts = new List<string>();
		ImgNode imgNode = WzAssets.FileRoot("Npc/" + npcId.ToString("D7") + ".img").Get("info/script");
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				string text = child.Text("script");
				if (!string.IsNullOrEmpty(text))
				{
					scripts.Add(text);
				}
			}
		}
		bool flag = NativeNpcShopWindow.HasNpc(npcId);
		if ((choices.Count > 0 && scripts.Count == 0) & flag)
		{
			scripts.Add(null);
		}
		if (choices.Count == 0 && scripts.Count == 0 && IsSilentNpc(npcId) && !flag)
		{
			return;
		}
		if (choices.Count == 0)
		{
			NpcSession.Talk(npcId, (scripts.Count > 0) ? scripts[0] : null);
			return;
		}
		NpcSession npcSession = NpcSession.Choose(npcId, DefaultTalk(npcId), (int index) =>
		{
			if (index >= 0 && index < choices.Count)
			{
				OpenQuest(choices[index]);
			}
			else if (index >= choices.Count && index < choices.Count + scripts.Count)
			{
				NpcSession.Talk(npcId, scripts[index - choices.Count]);
			}
		});
		FillQuestMenu(npcSession.Dialog.Content, choices);
		if (scripts.Count > 0)
		{
			npcSession.Dialog.Content.Selections.Add(new DialogSelection
			{
				Index = -1,
				HeaderArt = "list2"
			});
			for (int num = 0; num < scripts.Count; num++)
			{
				npcSession.Dialog.Content.Selections.Add(new DialogSelection
				{
					Index = choices.Count + num,
					Text = (string.IsNullOrEmpty(scripts[num]) ? ("与 " + WzNames.Npc(npcId) + " 对话") : scripts[num])
				});
			}
		}
		npcSession.Dialog.SelectFirst();
	}

	public static IReadOnlyList<QuestChoice> QuestAlerts()
	{
		if (Time.unscaledTime < alertsRefresh)
		{
			return alerts;
		}
		alertsRefresh = Time.unscaledTime + 0.4f;
		alerts.Clear();
		if (WelcomeTutorial.Available)
		{
			alerts.Add(new QuestChoice
			{
				npcId = 9010000,
				welcome = true
			});
		}
		foreach (int id in QuestCatalog.Ids)
		{
			QuestDef questDef = QuestCatalog.Get(id);
			if (questDef != null)
			{
				if (QuestLogic.CanComplete(questDef, questDef.completeReq.npc) && questDef.autoStart && questDef.autoPreComplete)
				{
					alerts.Add(new QuestChoice
					{
						npcId = questDef.completeReq.npc,
						quest = questDef,
						complete = true
					});
				}
				else if (questDef.autoStart && !questDef.autoAccept && QuestLogic.NotificationDateAvailable(questDef.startReq, DateTime.Now) && QuestLogic.CanStart(questDef, questDef.startReq.npc))
				{
					alerts.Add(new QuestChoice
					{
						npcId = questDef.startReq.npc,
						quest = questDef
					});
				}
			}
		}
		return alerts;
	}

	public static float AlertAppearSeconds()
	{
		if (alertAppearLength < 0f)
		{
			float num = 0f;
			foreach (ImgNode item in WzAssets.Frames(WzAssets.Node("Effect/BasicEff.img/QuestAlert/Appear")))
			{
				num += (float)Mathf.Max(1, item.Int("delay", 120)) / 1000f;
			}
			alertAppearLength = num;
		}
		return alertAppearLength;
	}

	public static bool AlertAnimAt(float now, out string path, out float elapsedMs)
	{
		path = null;
		elapsedMs = 0f;
		IReadOnlyList<QuestChoice> readOnlyList = QuestAlerts();
		if (readOnlyList.Count == 0)
		{
			alertSeen.Clear();
			alertAppearAt = float.NegativeInfinity;
			return false;
		}
		alertLive.Clear();
		bool flag = false;
		foreach (QuestChoice item in readOnlyList)
		{
			alertLive.Add(item.AlertId);
			if (alertSeen.Add(item.AlertId))
			{
				flag = true;
			}
		}
		alertGone.Clear();
		foreach (int item2 in alertSeen)
		{
			if (!alertLive.Contains(item2))
			{
				alertGone.Add(item2);
			}
		}
		foreach (int item3 in alertGone)
		{
			alertSeen.Remove(item3);
		}
		if (flag || now < alertAppearAt)
		{
			alertAppearAt = now;
		}
		float num = now - alertAppearAt;
		float num2 = AlertAppearSeconds();
		if (num < num2)
		{
			path = "Effect/BasicEff.img/QuestAlert/Appear";
			elapsedMs = num * 1000f;
			return true;
		}
		path = "Effect/BasicEff.img/QuestAlert/Default";
		elapsedMs = (num - num2) * 1000f;
		return true;
	}

	public static bool AlertAnim(out string path, out float elapsedMs)
	{
		return AlertAnimAt(Time.unscaledTime, out path, out elapsedMs);
	}

	private static void FillQuestMenu(DialogContent content, IReadOnlyList<QuestChoice> choices)
	{
		int[] array = new int[3] { 0, 1, 3 };
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i];
			bool flag = false;
			for (int j = 0; j < choices.Count; j++)
			{
				if (((!choices[j].complete) ? ((!choices[j].progress) ? 1 : 3) : 0) == num)
				{
					if (!flag)
					{
						content.Selections.Add(new DialogSelection
						{
							Index = -1,
							HeaderArt = "list" + num
						});
						flag = true;
					}
					content.Selections.Add(new DialogSelection
					{
						Index = j,
						Text = choices[j].Title
					});
				}
			}
		}
	}

	public static void ShowQuestAlerts()
	{
		if (NpcSession.Blocking)
		{
			return;
		}
		List<QuestChoice> choices = new List<QuestChoice>(QuestAlerts());
		if (choices.Count == 0)
		{
			return;
		}
		NpcSession npcSession = NpcSession.Choose(9010000, "冒险岛上有人要给你传话。", (int index) =>
		{
			if (index >= 0 && index < choices.Count)
			{
				OpenQuest(choices[index]);
			}
		});
		npcSession.SpeakerName = "冒险岛运营员";
		FillQuestMenu(npcSession.Dialog.Content, choices);
		npcSession.Dialog.SelectFirst();
	}

	public static void OpenQuest(QuestChoice choice)
	{
		if (choice.welcome)
		{
			if (choice.complete)
			{
				WelcomeTutorial.Complete();
			}
			else
			{
				WelcomeTutorial.Open();
			}
			return;
		}
		PlayerCtrl player = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if (map != null && (Object)(object)player != (Object)null && !choice.quest.autoStart && !choice.quest.autoComplete && !map.npcs.Exists((FieldNpc n) =>
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			return n.id == choice.npcId && n.hide == 0 && Mathf.Abs(player.FeetPos.x - map.WorldX(n.x)) <= 1200f / Px && Mathf.Abs(player.FeetPos.y - map.WorldY(n.y)) <= 800f / Px;
		}))
		{
			BottomBar.Toast("目标 NPC 距离过远");
		}
		else
		{
			NpcSession.TalkQuest(choice.quest, choice.npcId, choice.complete || choice.progress);
		}
	}

	public static bool TryInteractNearest()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (NpcSession.Blocking || GameWindows.AnyVisible || KeyConfigWindow.Visible || VirtualControlsEditor.Visible)
		{
			return false;
		}
		PlayerCtrl playerCtrl = (((Object)(object)GameLoop.I != (Object)null) ? GameLoop.I.player : null);
		if ((Object)(object)playerCtrl == (Object)null)
		{
			return false;
		}
		FieldNpc fieldNpc = Nearest(playerCtrl.FeetPos);
		if (fieldNpc == null)
		{
			return false;
		}
		Interact(fieldNpc.id);
		return true;
	}
}
