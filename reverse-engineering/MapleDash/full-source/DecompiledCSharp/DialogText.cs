using System.Text;
using UnityEngine;

public static class DialogText
{
	public static readonly Color Ink = Color.black;

	public static readonly Color Blue = Color.blue;

	public static readonly Color Red = Color.red;

	public static readonly Color Green = Color.green;

	public static readonly Color Orange = new Color(0.75f, 0.5f, 0.19f);

	public static readonly Color Purple = new Color(81f / 255f, 55f / 255f, 140f / 255f);

	public static readonly Color White = Color.white;

	public static string NormalizeBreaks(string raw)
	{
		if (string.IsNullOrEmpty(raw) || raw.IndexOf('\\') < 0)
		{
			return raw;
		}
		return raw.Replace("\\\\r\\\\n", "\n").Replace("\\\\n", "\n").Replace("\\r\\n", "\n")
			.Replace("\\r", "\n")
			.Replace("\\n", "\n")
			.Replace("\\t", "\t");
	}

	public static DialogContent Parse(string raw)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0613: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0698: Unknown result type (might be due to invalid IL or missing references)
		//IL_069d: Unknown result type (might be due to invalid IL or missing references)
		DialogContent content = new DialogContent();
		if (string.IsNullOrEmpty(raw))
		{
			return content;
		}
		raw = NormalizeBreaks(raw);
		StringBuilder buffer = new StringBuilder(raw.Length);
		Color color = Ink;
		bool bold = false;
		int link = -1;
		int num = 0;
		while (num < raw.Length)
		{
			char c = raw[num];
			switch (c)
			{
			case '\r':
				num++;
				continue;
			case '\t':
				buffer.Append("    ");
				num++;
				continue;
			default:
				buffer.Append(c);
				num++;
				continue;
			case '#':
				break;
			}
			if (num + 1 >= raw.Length)
			{
				buffer.Append('#');
				break;
			}
			char c2 = raw[num + 1];
			switch (c2)
			{
			case '#':
				buffer.Append('#');
				num += 2;
				continue;
			case 'n':
				if (bold)
				{
					Flush();
					bold = false;
				}
				num += 2;
				continue;
			case 'e':
				if (!bold)
				{
					Flush();
					bold = true;
				}
				num += 2;
				continue;
			case 'l':
				if (link >= 0)
				{
					Flush();
					link = -1;
				}
				num += 2;
				continue;
			case 'L':
			{
				int i;
				for (i = num + 2; i < raw.Length && char.IsDigit(raw[i]); i++)
				{
				}
				if (i > num + 2 && i < raw.Length && raw[i] == '#')
				{
					Flush();
					int.TryParse(raw.Substring(num + 2, i - num - 2), out link);
					content.Runs.Add(new DialogRun
					{
						Kind = DialogRunKind.Icon,
						Link = link,
						SelectionMarker = true,
						Art = "UI/UIWindow.img/UtilDlgEx/dot0"
					});
					num = i + 1;
					continue;
				}
				break;
			}
			}
			if (c2 == 'b' || c2 == 'r' || c2 == 'g' || c2 == 'k' || c2 == 'd' || c2 == 'w')
			{
				Flush();
				color = c2 switch
				{
					'w' => White, 
					'd' => Purple, 
					'g' => Green, 
					'r' => Red, 
					'b' => Blue, 
					_ => Ink, 
				};
				num += 2;
				continue;
			}
			if ("hocmp@tzyuqsWvif".IndexOf(c2) < 0)
			{
				buffer.Append('#');
				num++;
				continue;
			}
			int num2 = raw.IndexOf('#', num + 2);
			if (num2 < 0)
			{
				buffer.Append('#');
				num++;
				continue;
			}
			string text = raw.Substring(num + 2, num2 - num - 2);
			switch (c2)
			{
			case 'h':
				buffer.Append(Profile.playerName);
				break;
			case 'o':
				buffer.Append(WzNames.Mob(ToInt(text)));
				break;
			case 'c':
				buffer.Append(Profile.ItemCount(ToInt(text)));
				break;
			case 'm':
				buffer.Append(WzNames.Map(ToInt(text)));
				break;
			case '@':
			case 'p':
				buffer.Append(WzNames.Npc(ToInt(text)));
				break;
			case 't':
			case 'z':
				buffer.Append(WzNames.Item(ToInt(text)));
				break;
			case 'y':
				buffer.Append(QuestCatalog.Get(ToInt(text))?.name ?? "");
				break;
			case 'u':
			{
				int num4 = Profile.QuestStatus(ToInt(text));
				buffer.Append(num4 switch
				{
					1 => "进行中", 
					2 => "已结束", 
					_ => "未开始", 
				});
				break;
			}
			case 'q':
				buffer.Append(SkillName(ToInt(text)));
				break;
			case 's':
			{
				Flush();
				int num3 = ToInt(text);
				content.Runs.Add(new DialogRun
				{
					Kind = DialogRunKind.Icon,
					Link = link,
					Bold = bold,
					Color = color,
					Art = "Skill/" + (num3 / 10000).ToString("D3") + ".img/skill/" + num3.ToString("D7") + "/icon",
					Fallback = "[" + SkillName(num3) + "]"
				});
				break;
			}
			case 'W':
				Flush();
				content.Runs.Add(new DialogRun
				{
					Kind = DialogRunKind.Icon,
					Link = link,
					Bold = bold,
					Color = color,
					Art = "UI/UIWindow.img/Quest/" + text
				});
				break;
			case 'i':
			case 'v':
			{
				Flush();
				int id = ToInt(text);
				content.Runs.Add(new DialogRun
				{
					Kind = DialogRunKind.Icon,
					Link = link,
					Bold = bold,
					Color = color,
					Art = (ItemIconPath(id) ?? ""),
					Fallback = "[" + WzNames.Item(id) + "]"
				});
				break;
			}
			case 'f':
				Flush();
				if (text.Length > 0)
				{
					content.Runs.Add(new DialogRun
					{
						Kind = DialogRunKind.Icon,
						Link = link,
						Bold = bold,
						Color = color,
						Art = text
					});
				}
				break;
			}
			num = num2 + 1;
		}
		Flush();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (DialogRun run in content.Runs)
		{
			stringBuilder.Append((run.Kind == DialogRunKind.Icon) ? run.Fallback : run.Text);
		}
		content.Body = stringBuilder.ToString().Trim('\n');
		return content;
		void Flush()
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			if (buffer.Length != 0)
			{
				string text2 = buffer.ToString();
				buffer.Length = 0;
				content.Runs.Add(new DialogRun
				{
					Kind = ((link >= 0) ? DialogRunKind.Link : DialogRunKind.Text),
					Link = link,
					Text = text2,
					Bold = bold,
					Color = color
				});
			}
		}
	}

	private static int ToInt(string s)
	{
		int i = 0;
		int num = 0;
		for (; i < s.Length && char.IsWhiteSpace(s[i]); i++)
		{
		}
		bool flag = i < s.Length && s[i] == '-';
		if (i < s.Length && (s[i] == '+' || s[i] == '-'))
		{
			i++;
		}
		while (i < s.Length && s[i] >= '0' && s[i] <= '9')
		{
			num = num * 10 + s[i++] - 48;
		}
		if (!flag)
		{
			return num;
		}
		return -num;
	}

	private static string SkillName(int id)
	{
		return WzAssets.FileRoot("String/Skill.img")?.Get(id.ToString("D7"))?.Text("name") ?? "";
	}

	public static string ItemIconPath(int id)
	{
		if (id <= 0)
		{
			return null;
		}
		object obj;
		switch (id / 1000000)
		{
		case 1:
		{
			string text = EquipCatalog.DirOf(id);
			if (!string.IsNullOrEmpty(text))
			{
				return "Character/" + text + "/" + id.ToString("D8") + ".img/info/icon";
			}
			if (WeaponDefs.TryGet(id.ToString(), out var _))
			{
				return "Character/Weapon/" + id.ToString("D8") + ".img/info/icon";
			}
			return null;
		}
		default:
			obj = null;
			break;
		case 5:
			obj = "Cash";
			break;
		case 4:
			obj = "Etc";
			break;
		case 3:
			obj = "Install";
			break;
		case 2:
			obj = "Consume";
			break;
		}
		string text2 = (string)obj;
		if (text2 == null)
		{
			return null;
		}
		return "Item/" + text2 + "/" + (id / 10000).ToString("D4") + ".img/" + id.ToString("D8") + "/info/icon";
	}

	public static string StripMarkup(string raw)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (DialogRun run in Parse(raw).Runs)
		{
			stringBuilder.Append((run.Kind == DialogRunKind.Icon) ? run.Fallback : run.Text);
		}
		return stringBuilder.ToString();
	}

	static DialogText()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
	}
}
