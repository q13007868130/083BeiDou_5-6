using System.Collections.Generic;
using UnityEngine;

public static class QuestNotice
{
	private sealed class ReadyEntry
	{
		public int id;

		public float time;
	}

	private static readonly List<ReadyEntry> ready = new List<ReadyEntry>();

	private static GUIStyle readyStyle;

	private static float until;

	public static string Text { get; private set; }

	public static string Active
	{
		get
		{
			if (!(Time.unscaledTime < until))
			{
				return null;
			}
			return Text;
		}
	}

	public static bool HasReady(int id)
	{
		return ready.Exists((ReadyEntry entry) => entry.id == id && Time.unscaledTime - entry.time < 31f);
	}

	public static void Ready(int id)
	{
		RemoveReady(id);
		ready.Add(new ReadyEntry
		{
			id = id,
			time = Time.unscaledTime
		});
	}

	public static void RemoveReady(int id)
	{
		ready.RemoveAll((ReadyEntry entry) => entry.id == id);
	}

	public static void DrawReady()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected Obj, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		ready.RemoveAll((ReadyEntry entry) => Time.unscaledTime - entry.time >= 31f || !Profile.IsQuestStarted(entry.id));
		if (ready.Count == 0)
		{
			return;
		}
		float s = BottomBar.S;
		if (readyStyle == null)
		{
			readyStyle = new GUIStyle
			{
				font = UiKit.Font,
				clipping = (TextClipping)1
			};
		}
		readyStyle.fontSize = Mathf.RoundToInt(12f * s);
		readyStyle.normal.textColor = Color.white;
		for (int num = 0; num < ready.Count; num++)
		{
			ReadyEntry readyEntry = ready[num];
			float num2 = Time.unscaledTime - readyEntry.time;
			Color color = GUI.color;
			GUI.color = new Color(color.r, color.g, color.b, color.a * Mathf.Clamp01(num2) * Mathf.Clamp01(31f - num2));
			Rect val = BottomBar.NotifyBoxRect();
			QuestUi.Begin(new Vector2(val.x + -107f * s, val.y - (float)(43 + (ready.Count - num) * 5) * s), s);
			QuestUi.Image("FadeYesNo/backgrnd4", 0f, 0f);
			QuestUi.Image("FadeYesNo/icon7", 6f, 12f);
			GUI.Label(QuestUi.Rect(27f, 7f, 100f, 16f), "任务完成！", readyStyle);
			GUI.Label(QuestUi.Rect(27f, 20f, 100f, 16f), QuestUi.Fit(QuestCatalog.Get(readyEntry.id)?.name ?? "", 100f), readyStyle);
			bool flag = QuestUi.Button("UI/UIWindow.img/FadeYesNo/BtOK", 136f, 7f);
			bool flag2 = QuestUi.Button("UI/UIWindow.img/FadeYesNo/BtCancel", 136f, 20f);
			GUI.color = color;
			if (flag | flag2)
			{
				if (flag)
				{
					QuestWindow.Select(readyEntry.id);
				}
				ready.RemoveAt(num);
				break;
			}
		}
	}

	public static void Push(string questName)
	{
		Text = "完成任务：" + questName;
		until = Time.unscaledTime + 2.5f;
		ShowCompleteEffect();
	}

	public static void ShowCompleteEffect()
	{
		NativeImgAnimation.ShowInfo("Effect/BasicEff.img/QuestClear");
		AudioManager.Play("Game/QuestClear", 1f, pitchVary: false);
	}
}
