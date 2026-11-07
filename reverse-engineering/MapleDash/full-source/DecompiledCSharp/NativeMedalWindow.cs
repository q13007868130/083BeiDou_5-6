using System;
using UnityEngine;

public static class NativeMedalWindow
{
	public static bool Visible;

	private static Vector2 scroll;

	private static int tab;

	public static void Toggle()
	{
		Visible = !Visible;
	}

	public static void Wear(int item, bool unequip)
	{
		if (item / 10000 != 114 || (Profile.ItemCount(item) <= 0 && Profile.EquipOfSlot(-49) != item))
		{
			NativeUtilPrompt.Notice("没有持有这枚勋章");
			return;
		}
		string text = EquipCatalog.WhyNot(item, EquipLoadout.PanelAttributes(), Profile.JobId);
		if (!unequip && text != null)
		{
			NativeUtilPrompt.Notice(text);
			return;
		}
		InventoryChangeResponse inventoryChangeResponse = GameSession.Net.ChangeInventory(new InventoryChangeRequest
		{
			characterId = GameSession.CharacterId,
			itemId = item,
			equip = true,
			equipSlot = -49,
			quantity = ((!unequip) ? 1 : 0)
		});
		if (!inventoryChangeResponse.ok)
		{
			NativeUtilPrompt.Notice(inventoryChangeResponse.error);
			return;
		}
		GameSession.Character.inventory = inventoryChangeResponse.inventory;
		GameSession.ApplyInventoryToProfile();
		Profile.Save();
	}

	public static void Draw()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		Rect val = new Rect(20f, 20f, Mathf.Min(600f, UiKit.W - 40f), Mathf.Min(470f, UiKit.H - BottomBar.Height - 40f));
		GUI.Box(val, "");
		Font font = GUI.skin.font;
		GUI.skin.font = UiKit.Font;
		GUILayout.BeginArea(new Rect(val.x + 12f, val.y + 10f, val.width - 24f, val.height - 20f));
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label("勋章", Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("关闭", new GUILayoutOption[1] { GUILayout.Width(60f) }))
		{
			Visible = false;
		}
		GUILayout.EndHorizontal();
		tab = GUILayout.Toolbar(tab, new string[4] { "全部", "挑战中", "已完成", "已持有" }, Array.Empty<GUILayoutOption>());
		scroll = GUILayout.BeginScrollView(scroll, Array.Empty<GUILayoutOption>());
		foreach (int id in QuestCatalog.Ids)
		{
			QuestDef questDef = QuestCatalog.Get(id);
			if (questDef.medalItem <= 0)
			{
				continue;
			}
			bool flag = Profile.IsQuestStarted(id);
			bool flag2 = Profile.IsQuestCompleted(id);
			bool flag3 = Profile.ItemCount(questDef.medalItem) > 0 || Profile.EquipOfSlot(-49) == questDef.medalItem;
			if ((tab == 1 && !flag) || (tab == 2 && !flag2) || (tab == 3 && !flag3))
			{
				continue;
			}
			string name = questDef.name;
			string text;
			if (flag2)
			{
				text = " · 已完成";
			}
			else
			{
				text = (flag ? " · 挑战中" : "");
			}
			GUILayout.Label(name + text, Array.Empty<GUILayoutOption>());
			GUILayout.Label(WzNames.Item(questDef.medalItem), Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			if (flag3)
			{
				bool flag4 = Profile.EquipOfSlot(-49) == questDef.medalItem;
				if (GUILayout.Button(flag4 ? "卸下勋章" : "装备勋章", Array.Empty<GUILayoutOption>()))
				{
					Wear(questDef.medalItem, flag4);
				}
			}
			if (!flag2 && GUILayout.Button(flag ? "检查并领取" : "开始挑战", Array.Empty<GUILayoutOption>()))
			{
				QuestRequirement questRequirement = (flag ? questDef.completeReq : questDef.startReq);
				if (!(flag ? QuestLogic.CanComplete(questDef, questRequirement.npc, out var reason) : QuestLogic.CanStart(questDef, questRequirement.npc, out reason)))
				{
					BottomBar.Toast(reason);
				}
				else
				{
					NpcField.OpenQuest(new NpcField.QuestChoice
					{
						quest = questDef,
						npcId = questRequirement.npc,
						complete = flag
					});
					Visible = false;
				}
			}
			if (flag && GUILayout.Button("放弃", Array.Empty<GUILayoutOption>()))
			{
				Profile.ResetQuest(id);
				NpcField.InvalidateMarkers();
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
		GUI.skin.font = font;
	}
}
