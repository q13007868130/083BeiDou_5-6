using System;
using System.Globalization;
using MapleDash.Wz;
using UnityEngine;

internal static class MatrixPresentation
{
	private static string text;

	private static SkillData skill;

	private static int level;

	public static bool Active { get; private set; }

	public static void Begin()
	{
		Active = true;
		text = null;
		skill = null;
	}

	public static void Hover(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			text = value;
		}
	}

	public static void Details(string name, string desc, int current, bool showLevel = true)
	{
		skill = null;
		string text;
		if (string.IsNullOrEmpty(name))
		{
			text = null;
		}
		else
		{
			text = (showLevel ? (name + "  Lv" + current) : name);
		}
		MatrixPresentation.text = (string.IsNullOrEmpty(text) ? desc : (text + "\n" + desc));
	}

	public static void Skill(int[] skills, int current, string name, string desc)
	{
		Details(name, desc, current);
		if (skills != null && skills.Length != 0)
		{
			SkillData skillData = SkillBook.Get(skills[0]);
			if (skillData != null)
			{
				skill = skillData.ForTooltip(name, desc);
				level = current;
			}
		}
	}

	public static void Draw()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected Obj, but got Unknown
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected Obj, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		Active = false;
		try
		{
			if (GrowthUi.Repaint && !NativeUtilPrompt.Visible)
			{
				if (skill != null)
				{
					SkillWindow.DrawSkillData(skill, Pointer.Pos, level);
				}
				else if (!string.IsNullOrEmpty(text))
				{
					float s = GrowthUi.S;
					float num = 300f * s;
					GUIStyle val = new GUIStyle
					{
						font = UiKit.Font,
						fontSize = Mathf.Max(9, Mathf.RoundToInt(12f * s)),
						wordWrap = true
					};
					val.normal.textColor = Color.white;
					text = text.Replace("\\n", "\n");
					float num2 = val.CalcHeight(new GUIContent(text), num - 20f * s) + 20f * s;
					Rect rect = new Rect(Mathf.Clamp(Pointer.Pos.x + 16f * s, 4f, Mathf.Max(4f, UiKit.W - num - 4f)), Mathf.Clamp(Pointer.Pos.y + 14f * s, 4f, Mathf.Max(4f, UiKit.H - num2 - 4f)), num, num2);
					ItemTooltip.DrawFrame(rect, s);
					GUI.Label(new Rect(rect.x + 10f * s, rect.y + 10f * s, num - 20f * s, num2 - 20f * s), text, val);
				}
			}
		}
		finally
		{
			skill = null;
			text = null;
		}
	}

	public static void Regions(string root)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = WzAssets.Node(root);
		if (imgNode == null)
		{
			return;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Name.StartsWith("tooltip:", StringComparison.OrdinalIgnoreCase) && child.TryVector("lt", out var x, out var y) && child.TryVector("rb", out var x2, out var y2))
			{
				Rect val = GrowthUi.At(x, y, x2 - x, y2 - y);
				if (val.Contains(Pointer.Pos))
				{
					Hover(child.Text("text"));
				}
			}
		}
	}

	public static void Animation(string path, float x, float y)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int i;
		for (i = 0; i < 128; i++)
		{
			ImgNode imgNode = WzAssets.Node(path + i);
			if (imgNode == null)
			{
				break;
			}
			num += Mathf.Max(1, imgNode.Int("delay", 100));
		}
		if (num == 0)
		{
			return;
		}
		int num2 = (int)(Time.realtimeSinceStartup * 1000f) % num;
		for (int j = 0; j < i; j++)
		{
			ImgNode imgNode2 = WzAssets.Node(path + j);
			num2 -= Mathf.Max(1, imgNode2.Int("delay", 100));
			if (num2 < 0)
			{
				Vector2 val = WzAssets.Origin(imgNode2);
				GrowthUi.Image(path, j.ToString(CultureInfo.InvariantCulture), x - val.x, y - val.y);
				break;
			}
		}
	}

	public static void Help(bool hexa)
	{
		NativeUtilPrompt.Pages(hexa ? "HEXA 矩阵帮助" : "V 矩阵帮助", (!hexa) ? new string[5] { "本项目沿用源端 V 点矩阵：技能核心4格、强化核心6格、共用核心19格。点击核心打开强化窗口，分配 V 点后才会提升对应技能。", "强化窗显示当前等级、目标等级和所需 V 点。可逐级增加、选择当前点数能负担的等级，或撤销本次选择。确认成功后显示等级变化。", "V 点转换会消耗 V 点兑换核心宝石。背包使用核心宝石可获得 V 点，部分宝石首次使用会直接开启指定核心。数量和收益均由服务端核实，V 点上限9999。", "特殊核心需先登记，再点击启用或停用。同一时间只能启用一个特殊核心；其触发条件、效果与冷却以核心悬浮说明为准。", "重置入口会先提示金币费用并要求确认，再返还已分配 V 点。五转技能不受冷却时间初始化、增益持续时间增加、召唤兽持续时间增加效果影响。" } : new string[7] { "启用 HEXA 核心可获得六转技能与能力值。强化核心可提升效果。技能页分为技能、精通、强化、共用四类核心。", "选中核心可查看等级与所需材料。激活需满足前置技能条件；气息与碎片由服务端检查，成功后同步技能。强化窗可逐级选择或选择当前材料能负担的等级。", "HEXA 技能不受冷却时间初始化、增益持续时间增加、召唤兽持续时间增加效果影响。除部分技能外，会套用被动超技能与五转强化核心效果。", "能力值页先激活核心，再选择三项不同能力。攻击力与魔法攻击力不能同时选择。强化随机提升一条能力，总等级上限20，每条上限10。", "保存槽会保存完整能力类型与等级。可切换套用强化槽或保存槽；保存与套用是不同操作。变更能力每次100万金币，保留各条等级。", "正常初始化要求总等级至少10，费用1000万金币。无法还原三条分配的旧记录可免费初始化。初始化后重新选择与强化。主属性效果按职业换算。", "资源区可查看索尔艾尔达气息与碎片；转换入口按原材料种类兑换，费用与数量以窗口显示和服务端校验为准。" });
	}
}
