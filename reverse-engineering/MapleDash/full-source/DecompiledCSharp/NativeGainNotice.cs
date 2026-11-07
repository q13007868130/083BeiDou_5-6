using System.Collections.Generic;
using UnityEngine;

public static class NativeGainNotice
{
	private sealed class Line
	{
		public string text;

		public float time;

		public Color color;
	}

	private static readonly List<Line> lines = new List<Line>();

	private static GUIStyle style;

	public static void Clear()
	{
		lines.Clear();
	}

	public static void Add(string text, bool yellow = false)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (lines.Count == 6)
		{
			lines.RemoveAt(0);
		}
		lines.Add(new Line
		{
			text = text,
			time = Time.unscaledTime,
			color = (yellow ? Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)32, byte.MaxValue)) : Color.white)
		});
	}

	public static void Experience(long amount)
	{
		if (amount > 0)
		{
			Add("获得了经验值（+" + amount + "）");
		}
	}

	public static void Pickup(int id, long amount)
	{
		if (amount > 0)
		{
			string text;
			if (id != 0)
			{
				text = ((FieldCatalog.Items.TryGetValue(id, out var value) && !string.IsNullOrEmpty(value.name)) ? value.name : id.ToString());
			}
			else
			{
				text = "金币";
			}
			Add("获得了" + text + "（+" + amount + "）");
		}
	}

	public static void Draw()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected Obj, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected Obj, but got Unknown
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		lines.RemoveAll((Line line2) => Time.unscaledTime - line2.time >= 6f);
		if (lines.Count != 0)
		{
			float s = BottomBar.S;
			if (style == null)
			{
				style = new GUIStyle
				{
					padding = new RectOffset(),
					alignment = (TextAnchor)2
				};
			}
			style.font = UiKit.Font;
			style.fontSize = Mathf.Max(1, Mathf.RoundToInt(12f * s));
			float num = UiKit.H - (float)(BottomBar.ChatExpanded ? 235 : 157) * s + (float)((6 - lines.Count) * 14) * s;
			for (int num2 = 0; num2 < lines.Count; num2++)
			{
				Line line = lines[num2];
				float num3 = Mathf.Clamp01(1f - (Time.unscaledTime - line.time) / 6f);
				Rect val = new Rect(UiKit.W - 296f * s, num + (float)(14 * num2) * s, 290f * s, 14f * s);
				style.normal.textColor = new Color(0f, 0f, 0f, num3);
				GUI.Label(new Rect(val.x + s, val.y + s, val.width, val.height), line.text, style);
				Color color = line.color;
				color.a = num3;
				style.normal.textColor = color;
				GUI.Label(val, line.text, style);
			}
		}
	}
}
