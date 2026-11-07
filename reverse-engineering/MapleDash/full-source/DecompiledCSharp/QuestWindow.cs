using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MapleDash.Wz;
using UnityEngine;

public static class QuestWindow
{
	private struct Entry
	{
		public int id;

		public int area;

		public int count;
	}

	public static bool Visible;

	private static int tab = 1;

	private static int selectedId;

	private static float listScroll;

	private static float bodyScroll;

	private static float summaryScroll;

	private static bool detailVisible = true;

	private static readonly HashSet<int> collapsed = new HashSet<int>();

	private static readonly List<Entry> rows = new List<Entry>();

	public static void Toggle()
	{
		if (Visible)
		{
			Visible = false;
			return;
		}
		Visible = true;
		BuildRows();
		if (tab == 1 && rows.Count == 0)
		{
			tab = 0;
		}
		EnsureSelection();
		detailVisible = selectedId > 0;
	}

	public static void Select(int id)
	{
		QuestDef questDef = QuestCatalog.Get(id);
		if (questDef != null)
		{
			selectedId = id;
			tab = Profile.QuestStatus(id);
			collapsed.Remove(questDef.area);
			BuildRows();
			RevealSelection();
			bodyScroll = (summaryScroll = 0f);
			detailVisible = (Visible = true);
		}
	}

	private static void EnsureSelection()
	{
		BuildRows();
		QuestDef questDef = QuestCatalog.Get(selectedId);
		if (selectedId > 0 && questDef != null && Profile.QuestStatus(selectedId) == tab)
		{
			collapsed.Remove(questDef.area);
			BuildRows();
			if (rows.Exists((Entry row) => row.id == selectedId))
			{
				RevealSelection();
				return;
			}
		}
		selectedId = 0;
		bodyScroll = (summaryScroll = (listScroll = 0f));
		if (rows.Count > 0 && collapsed.Remove(rows[0].area))
		{
			BuildRows();
		}
		foreach (Entry row in rows)
		{
			if (row.id > 0)
			{
				selectedId = row.id;
				break;
			}
		}
		RevealSelection();
	}

	private static void RevealSelection()
	{
		int num = rows.FindIndex((Entry row) => row.id == selectedId && row.id > 0);
		if (num >= 0)
		{
			if ((float)num < listScroll)
			{
				listScroll = num;
			}
			else if ((float)num >= listScroll + 14f)
			{
				listScroll = num - 13;
			}
			listScroll = Mathf.Clamp(listScroll, 0f, (float)Mathf.Max(0, rows.Count - 14));
		}
	}

	private static void SelectTab(int value)
	{
		tab = value;
		selectedId = 0;
		EnsureSelection();
		detailVisible = selectedId > 0;
	}

	public static void Draw()
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_0481: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		if (selectedId > 0 && Profile.QuestStatus(selectedId) != tab)
		{
			EnsureSelection();
		}
		bool flag = detailVisible && selectedId > 0;
		float num = Mathf.Min(new float[3]
		{
			BottomBar.S,
			(UiKit.W - 16f) / (flag ? 550f : 245f),
			(UiKit.H - BottomBar.Height - 16f) / 396f
		});
		Vector2 val = new Vector2((UiKit.W - (float)(flag ? 550 : 245) * num) / 2f, Mathf.Max(0f, (UiKit.H - BottomBar.Height - 396f * num) / 2f));
		QuestUi.Begin(val, num);
		NativeUi.RegisterWindow("QuestWindow", new Rect(val.x, val.y, (float)(flag ? 550 : 245) * num, 396f * num), () => Visible);
		QuestUi.Image("Quest/backgrnd", 0f, 0f);
		if (QuestUi.Button("UI/Basic.img/BtClose", 229f, 6f))
		{
			Visible = false;
			return;
		}
		DrawTabs();
		BuildRows();
		if (rows.Count == 0)
		{
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/Quest/notice" + tab);
			if ((Object)(object)val2 != (Object)null)
			{
				QuestUi.Image("Quest/notice" + tab, (210 - ((Texture)val2).width) / 2 - 2, (309 - ((Texture)val2).height) / 2 + 50);
			}
		}
		listScroll = QuestUi.Scroll(new Rect(225f, 50f, 15f, 309f), new Rect(10f, 50f, 215f, 309f), listScroll, Mathf.Max(0, rows.Count - 14), 1f);
		int num2 = (int)listScroll;
		for (int num3 = num2; num3 < rows.Count && num3 < num2 + 14; num3++)
		{
			Entry entry = rows[num3];
			float num4 = 50 + (num3 - num2) * 22;
			if (entry.id == 0)
			{
				QuestUi.Fill(new Rect(10f, num4 - 1f, 208f, 19f), Color32.op_Implicit(new Color32((byte)159, (byte)181, (byte)206, byte.MaxValue)));
				bool flag2 = QuestUi.Button("UI/Basic.img/" + (collapsed.Contains(entry.area) ? "BtMax" : "BtMin"), 13f, num4 + 2f);
				QuestUi.Label(text: (WzAssets.FileRoot("Etc/QuestCategory.img")?.Text(entry.area.ToString()) ?? "") + " (" + entry.count + ")", rect: new Rect(30f, num4 + 3f, 188f, 16f), color: Color.white);
				if ((flag2 || QuestUi.Click(QuestUi.Rect(10f, num4, 208f, 19f))) && !collapsed.Add(entry.area))
				{
					collapsed.Remove(entry.area);
				}
				continue;
			}
			bool flag3 = selectedId == entry.id;
			if (flag3)
			{
				QuestUi.Fill(new Rect(28f, num4, 192f, 19f), Color32.op_Implicit(new Color32((byte)57, (byte)96, (byte)147, byte.MaxValue)));
			}
			string text;
			if (tab != 0)
			{
				if (tab == 2)
				{
					text = "icon4";
				}
				else
				{
					text = (QuestLogic.CanComplete(QuestCatalog.Get(entry.id), 0) ? "icon2/0" : "icon3/0");
				}
			}
			else
			{
				text = "icon0";
			}
			QuestUi.Image("Quest/" + text, 11f, num4 + 2f);
			QuestUi.Label(new Rect(30f, num4 + 2f, 192f, 16f), QuestUi.Fit(QuestCatalog.Get(entry.id).name, 192f), flag3 ? Color.white : Color.black);
			if (QuestUi.Click(QuestUi.Rect(28f, num4, 192f, 19f)))
			{
				selectedId = entry.id;
				detailVisible = true;
				bodyScroll = (summaryScroll = 0f);
			}
		}
		if (detailVisible && selectedId > 0)
		{
			DrawDetail();
		}
	}

	private static void DrawTabs()
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		TabPiece("left" + ((tab == 0) ? 1 : 0), 3f, 23f);
		for (int i = 0; i < 3; i++)
		{
			float num = 7 + i * 65;
			float x = num + 57f;
			Texture2D val = Art.Tex("wz://UI/Basic.img/Tab2/fill" + ((tab == i) ? 1 : 0));
			if ((Object)(object)val != (Object)null)
			{
				GUI.DrawTexture(QuestUi.Rect(num, 23f, 57f, ((Texture)val).height), (Texture)(object)val);
			}
			TabPiece((i == 2) ? ("right" + ((tab == i) ? 1 : 0)) : ("middle" + ((tab == i) ? 1 : ((tab == i + 1) ? 2 : 0))), x, 23f);
			Texture2D val2 = Art.Tex("wz://UI/UIWindow.img/Quest/Tab/" + ((tab == i) ? "disabled/" : "enabled/") + i);
			if ((Object)(object)val2 != (Object)null)
			{
				GUI.DrawTexture(QuestUi.Rect(num + 28f - (float)(((Texture)val2).width / 2), 32 - ((Texture)val2).height / 2 + 2, ((Texture)val2).width, ((Texture)val2).height), (Texture)(object)val2);
			}
			if (QuestUi.Click(QuestUi.Rect(num - 4f, 23f, 65f, 19f)) && tab != i)
			{
				SelectTab(i);
			}
		}
	}

	private static void TabPiece(string piece, float x, float y)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = Art.Tex("wz://UI/Basic.img/Tab2/" + piece);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(QuestUi.Rect(x, y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}

	private static void BuildRows()
	{
		SortedDictionary<int, List<int>> sortedDictionary = new SortedDictionary<int, List<int>>();
		foreach (int id in QuestCatalog.Ids)
		{
			QuestDef questDef = QuestCatalog.Get(id);
			if (questDef != null && Profile.QuestStatus(id) == tab && (tab != 0 || (QuestLogic.CanStart(questDef, 0) && QuestLogic.NotificationDateAvailable(questDef.startReq, DateTime.Now))))
			{
				if (!sortedDictionary.TryGetValue(questDef.area, out var value))
				{
					value = (sortedDictionary[questDef.area] = new List<int>());
				}
				value.Add(id);
			}
		}
		rows.Clear();
		foreach (KeyValuePair<int, List<int>> item in sortedDictionary)
		{
			rows.Add(new Entry
			{
				area = item.Key,
				count = item.Value.Count
			});
			if (collapsed.Contains(item.Key))
			{
				continue;
			}
			item.Value.Sort((int a, int b) => string.CompareOrdinal(QuestCatalog.Get(a).name, QuestCatalog.Get(b).name));
			foreach (int item2 in item.Value)
			{
				rows.Add(new Entry
				{
					id = item2
				});
			}
		}
	}

	private static void DrawDetail()
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		QuestDef questDef = QuestCatalog.Get(selectedId);
		if (questDef == null)
		{
			return;
		}
		bool flag = !string.IsNullOrEmpty(questDef.demandSummary) || !string.IsNullOrEmpty(questDef.summary) || !string.IsNullOrEmpty(questDef.rewardSummary);
		QuestUi.Image("Quest/" + (flag ? "backgrnd4" : "backgrnd2"), 245f, 0f);
		if (QuestUi.Button("UI/Basic.img/BtHide", 530f, 6f))
		{
			detailVisible = false;
			return;
		}
		QuestUi.Label(new Rect(280f, 40f, 150f, 18f), QuestUi.Fit(questDef.name, 150f), Color.white);
		QuestRequirement startReq = questDef.startReq;
		string text;
		if (startReq.maxLevel > 0)
		{
			text = "等级 " + startReq.maxLevel + "以下";
		}
		else
		{
			text = ((startReq.level > 0) ? ("等级 " + startReq.level + "以上") : "");
		}
		QuestUi.Label(new Rect(268f, 60f, 150f, 18f), text, Color.white);
		DrawNpc(questDef.startReq.npc, 487f, 110f);
		string text2 = (questDef.stateInfo.TryGetValue(Profile.QuestStatus(questDef.id), out var value) ? value : "");
		DrawContent(ExpandProgress(questDef, text2), new Rect(262f, 122f, 265f, (float)(flag ? 120 : 243)), ref bodyScroll, 120f);
		if (flag)
		{
			List<string> list = new List<string> { "#fUI/UIWindow.img/Quest/summary#" };
			if (!string.IsNullOrEmpty(questDef.summary))
			{
				list.Add(questDef.summary);
			}
			if (!string.IsNullOrEmpty(questDef.demandSummary))
			{
				list.Add(questDef.demandSummary);
			}
			if (!string.IsNullOrEmpty(questDef.rewardSummary))
			{
				list.Add(" ");
				list.Add("#fUI/UIWindow.img/Quest/reward#");
				list.Add(questDef.rewardSummary);
			}
			for (int i = 0; i < list.Count; i++)
			{
				list[i] = ExpandProgress(questDef, list[i]);
			}
			DrawContent(list, new Rect(262f, 248f, 265f, 110f), ref summaryScroll);
		}
		int num = Profile.QuestStatus(questDef.id);
		if (num != 1 && num != 2)
		{
			return;
		}
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && num == 1 && (questDef.id < 1200 || questDef.id >= 1400);
		if (QuestUi.Button("UI/UIWindow.img/UtilDlgEx/BtQGiveup", 487f, 372f))
		{
			int id = questDef.id;
			string name = questDef.name;
			NativeUtilPrompt.Confirm("放弃任务「" + name + "」后进度会清空，确定要放弃吗？", (bool yes) =>
			{
				if (yes)
				{
					Profile.ResetQuest(id);
					selectedId = 0;
					NpcField.InvalidateMarkers();
				}
			});
		}
		GUI.enabled = enabled && num == 1 && (questDef.id < 1200 || questDef.id >= 1400) && QuestAlarm.CanTrack(questDef);
		if (QuestUi.Button("UI/UIWindow.img/Quest/BtAlert", 395f, 372f))
		{
			QuestAlarm.Track(questDef.id);
		}
		GUI.enabled = enabled;
	}

	public static string ExpandProgress(QuestDef q, string text)
	{
		return Regex.Replace(text ?? "", "#a(\\d+)#", (Match match) =>
		{
			string value = match.Groups[1].Value;
			string text2 = q.id.ToString();
			if (!value.StartsWith(text2) || !int.TryParse(value.Substring(text2.Length), out var result) || result < 1 || result > q.completeReq.mobs.Count)
			{
				return "";
			}
			QuestItemReq questItemReq = q.completeReq.mobs[result - 1];
			return Profile.QuestProgressInt(q.id, questItemReq.item.ToString()) + "/" + questItemReq.count;
		}).Replace("\\r\\n", "\n").Replace("\\n", "\n");
	}

	private static void DrawContent(string text, Rect view, ref float scroll, float rangeHeight)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		DrawContent(new string[1] { text }, view, ref scroll, rangeHeight);
	}

	private static void DrawContent(IList<string> blocks, Rect view, ref float scroll, float rangeHeight = 110f)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		List<DialogFlow.Layout> list = new List<DialogFlow.Layout>(blocks.Count);
		float num = 0f;
		foreach (string block in blocks)
		{
			DialogFlow.Layout layout = DialogFlow.Measure(DialogText.Parse(block), view.width, input: false, 1f);
			list.Add(layout);
			num += layout.Height;
		}
		float maximum = Mathf.Max(0f, Mathf.Floor((num + (float)(blocks.Count * 7) - rangeHeight) / 8f) * 8f);
		scroll = QuestUi.Scroll(new Rect(view.x + 266f, view.y, 15f, view.height), view, scroll, maximum, 8f);
		GUI.BeginClip(QuestUi.Rect(view.x + 1f, view.y + 1f, view.width - 2f, view.height - 2f));
		float num2 = 0f;
		foreach (DialogFlow.Layout item in list)
		{
			foreach (DialogFlow.Span span in item.Spans)
			{
				Rect val = new Rect((span.Rect.x - 1f) * QuestUi.Scale, (span.Rect.y + num2 - scroll - 1f) * QuestUi.Scale, span.Rect.width * QuestUi.Scale, span.Rect.height * QuestUi.Scale);
				if ((Object)(object)span.Tex != (Object)null)
				{
					GUI.DrawTexture(val, (Texture)(object)span.Tex);
				}
				else
				{
					QuestUi.LocalLabel(val, span.Text, span.Run.Color, span.Run.Bold);
				}
			}
			num2 += item.Height;
		}
		GUI.EndClip();
	}

	private static void DrawNpc(int id, float x, float y)
	{
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		if (id <= 0)
		{
			return;
		}
		ImgNode imgNode = WzAssets.FileRoot("Npc/" + id.ToString("D7") + ".img");
		HashSet<int> hashSet = new HashSet<int>();
		while (imgNode?.Get("info/link") != null && hashSet.Add(id))
		{
			id = imgNode.Int("info/link");
			imgNode = WzAssets.FileRoot("Npc/" + id.ToString("D7") + ".img");
		}
		List<ImgNode> list = WzAssets.Frames(imgNode?.Get("stand"));
		if (list.Count == 0)
		{
			return;
		}
		float num = 0f;
		foreach (ImgNode item in list)
		{
			num += (float)Mathf.Max(1, Mathf.Abs(item.Int("delay", 120)));
		}
		float num2 = Mathf.Repeat(Time.unscaledTime * 1000f, num);
		ImgNode frame = list[0];
		foreach (ImgNode item2 in list)
		{
			frame = item2;
			num2 -= (float)Mathf.Max(1, Mathf.Abs(item2.Int("delay", 120)));
			if (num2 < 0f)
			{
				break;
			}
		}
		Texture2D val = WzAssets.Texture(frame);
		Vector2 val2 = WzAssets.Origin(frame);
		if ((Object)(object)val != (Object)null)
		{
			GUI.DrawTexture(QuestUi.Rect(x - val2.x, y - val2.y, ((Texture)val).width, ((Texture)val).height), (Texture)(object)val);
		}
	}
}
