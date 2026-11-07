using System.Collections.Generic;
using UnityEngine;

public static class QuestAlarm
{
	private static readonly List<int> tracked = new List<int>();

	private static readonly Dictionary<int, float> updated = new Dictionary<int, float>();

	private static bool automatic = true;

	private static bool visible = true;

	private static bool expanded;

	private static Vector2 position = new Vector2(float.NaN, 35f);

	private static Vector2 grab;

	private static bool dragging;

	public static IReadOnlyList<int> Tracked => tracked;

	public static bool CanTrack(QuestDef quest)
	{
		if (quest != null && tracked.Count < 5 && !tracked.Contains(quest.id))
		{
			if (quest.completeReq.mobs.Count <= 0 && quest.completeReq.items.Count <= 0 && quest.completeReq.quests.Count <= 0)
			{
				return quest.completeReq.meso > 0;
			}
			return true;
		}
		return false;
	}

	public static void Track(int id)
	{
		if (!Profile.IsQuestStarted(id))
		{
			return;
		}
		if (!tracked.Contains(id))
		{
			if (!CanTrack(QuestCatalog.Get(id)))
			{
				return;
			}
			tracked.Add(id);
		}
		updated[id] = Time.unscaledTime;
		visible = true;
		expanded = true;
	}

	public static void Progress(int id)
	{
		if (automatic || tracked.Contains(id))
		{
			Track(id);
		}
	}

	public static void Remove(int id)
	{
		tracked.Remove(id);
		updated.Remove(id);
	}

	public static void Draw()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		for (int num = tracked.Count - 1; num >= 0; num--)
		{
			int num2 = tracked[num];
			if (!Profile.IsQuestStarted(num2) || (automatic && Time.unscaledTime - updated[num2] >= 600f))
			{
				Remove(num2);
			}
		}
		if (!visible)
		{
			return;
		}
		float s = BottomBar.S;
		if (float.IsNaN(position.x))
		{
			position.x = UiKit.W / s - 233f;
		}
		position = NativeUi.ClampWindowPosition(position, new Vector2(223f, 25f), s);
		Rect val = new Rect((position.x + 25f) * s, position.y * s, 114f * s, 20f * s);
		if ((int)Event.current.type == 7 && Pointer.Pressed && val.Contains(Pointer.Pos))
		{
			dragging = true;
			grab = Pointer.Pos / s - position;
			Pointer.ConsumeClick();
		}
		if (dragging && Pointer.Down)
		{
			position = NativeUi.ClampWindowPosition(Pointer.Pos / s - grab, new Vector2(223f, 25f), s);
		}
		if (dragging && !Pointer.Down)
		{
			dragging = false;
			Pointer.ConsumeClick();
		}
		QuestUi.Begin(position * s, s);
		QuestUi.Image("QuestAlarm/" + (expanded ? "backgrndmax" : "backgrndmin"), 0f, 0f);
		QuestUi.Label(new Rect(25f, 2f, 114f, 18f), "任务告知(" + tracked.Count + "/5)", Color.black);
		if (QuestUi.Button("UI/UIWindow.img/QuestAlarm/BtQ", 5f, 4f))
		{
			QuestWindow.Toggle();
		}
		if (QuestUi.Button("UI/UIWindow.img/QuestAlarm/BtAuto", 140f, 4f))
		{
			automatic = !automatic;
		}
		if (QuestUi.Button("UI/Basic.img/" + (expanded ? "BtMin" : "BtMax"), 190f, 4f))
		{
			expanded = !expanded;
		}
		if (QuestUi.Button("UI/Basic.img/BtClose", 203f, 4f))
		{
			visible = false;
		}
		if (!expanded || tracked.Count == 0)
		{
			return;
		}
		List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
		foreach (int item in tracked)
		{
			QuestDef questDef = QuestCatalog.Get(item);
			if (questDef == null)
			{
				continue;
			}
			list.Add(new KeyValuePair<int, string>(item, questDef.name));
			foreach (QuestItemReq mob in questDef.completeReq.mobs)
			{
				list.Add(new KeyValuePair<int, string>(0, WzNames.Mob(mob.item) + " " + Profile.QuestProgressInt(item, mob.item.ToString()) + "/" + mob.count));
			}
			foreach (QuestItemReq item2 in questDef.completeReq.items)
			{
				list.Add(new KeyValuePair<int, string>(0, WzNames.Item(item2.item) + " " + Profile.ItemCount(item2.item) + "/" + item2.count));
			}
		}
		float num3 = 25f;
		foreach (KeyValuePair<int, string> item3 in list)
		{
			QuestUi.Image("QuestAlarm/backgrndcenter", 0f, num3);
			QuestUi.Label(new Rect(10f, num3, 185f, 18f), item3.Value, Color.black);
			if (item3.Key > 0 && QuestUi.Click(QuestUi.Rect(10f, num3, 185f, 18f)))
			{
				QuestWindow.Select(item3.Key);
			}
			if (item3.Key > 0 && QuestUi.Button("UI/Basic.img/BtClose2", 203f, num3 - 2f))
			{
				Remove(item3.Key);
				break;
			}
			num3 += 18f;
		}
		QuestUi.Image("QuestAlarm/backgrndbottom", 0f, num3);
	}

	static QuestAlarm()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
	}
}
