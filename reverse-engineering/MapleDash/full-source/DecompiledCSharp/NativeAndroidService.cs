using System;
using System.Collections.Generic;
using Server.Bot;

public static class NativeAndroidService
{
	public const int RobotSlot = -32;

	public const int HeartSlot = -54;

	public const int ApHat = 1200;

	public const int ApCape = 1201;

	public const int ApFaceAccessory = 1202;

	public const int ApTop = 1203;

	public const int ApBottom = 1204;

	public const int ApShoes = 1205;

	public const int ApGloves = 1206;

	public const int ApWeapon = 1207;

	public const int ApRing = 1210;

	public const int ApRing2 = 1211;

	public const int ApRing3 = 1212;

	public const int ApRing4 = 1213;

	public const int ApFirst = 1200;

	public const int ApLast = 1213;

	public static readonly int[] ApSlots = new int[12]
	{
		1200, 1201, 1202, 1203, 1204, 1205, 1206, 1207, 1210, 1211,
		1212, 1213
	};

	public const int RobotIdFirst = 1662000;

	public const int RobotIdLast = 1669999;

	public const int HeartIdFirst = 1672000;

	public const int HeartIdLast = 1679999;

	public const int FallbackSkin = 2000;

	private static Dictionary<string, object> templates;

	private static readonly object gate = new object();

	private static Dictionary<string, object> Templates
	{
		get
		{
			lock (gate)
			{
				if (templates != null)
				{
					return templates;
				}
				string text = PlatformRes.LoadText("Bot/AndroidTemplates");
				Dictionary<string, object> dictionary = ((text == null) ? null : (MiniJsonDb.Parse(text) as Dictionary<string, object>));
				templates = ((dictionary != null && dictionary.TryGetValue("templates", out var value)) ? ((value as Dictionary<string, object>) ?? new Dictionary<string, object>()) : new Dictionary<string, object>());
				return templates;
			}
		}
	}

	public static int TemplateCount => Templates.Count;

	public static int FollowDirection(bool modeFacingLeft, bool facesLeft, int ownerX, int androidX, int previousAndroidX, int direction)
	{
		int num = ownerX;
		if (!modeFacingLeft & facesLeft)
		{
			num -= 145;
		}
		if (modeFacingLeft && !facesLeft)
		{
			num += 145;
		}
		int num2;
		if (modeFacingLeft)
		{
			num2 = (facesLeft ? 65 : 105);
		}
		else
		{
			num2 = (facesLeft ? 105 : 65);
		}
		if (facesLeft ? (ownerX > androidX) : (androidX > ownerX))
		{
			num2 += 45;
		}
		int num3 = Math.Abs(num - androidX);
		if (num3 < num2)
		{
			return 0;
		}
		if ((androidX == previousAndroidX && num3 > num2) || num3 > 80)
		{
			if (num <= androidX)
			{
				return -1;
			}
			return 1;
		}
		return direction;
	}

	public static bool IsRobot(int itemId)
	{
		if (itemId >= 1662000)
		{
			return itemId <= 1669999;
		}
		return false;
	}

	public static bool IsHeart(int itemId)
	{
		if (itemId >= 1672000)
		{
			return itemId <= 1679999;
		}
		return false;
	}

	public static Dictionary<string, object> Template(int templateId)
	{
		if (!Templates.TryGetValue(templateId.ToString(), out var value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	private static Dictionary<string, object> ItemInfo(int itemId)
	{
		return BotItemCatalog.Shared.Info(itemId);
	}

	private static int ItemInt(int itemId, string key, int fallback = 0)
	{
		return BotItemCatalog.Int(ItemInfo(itemId), key, fallback);
	}

	public static int GradeOf(int itemId)
	{
		return ItemInt(itemId, "grade");
	}

	public static int TemplateOf(int itemId)
	{
		return ItemInt(itemId, "android");
	}

	public static AndroidState Resolve(int characterId, Func<int, int> slotOf)
	{
		AndroidState androidState = new AndroidState();
		if (slotOf == null)
		{
			androidState.reason = "无效的装备栏";
			return androidState;
		}
		int num = slotOf(-32);
		int num2 = slotOf(-54);
		if (num <= 0)
		{
			return androidState;
		}
		if (!IsRobot(num))
		{
			androidState.reason = "机器人道具无效";
			return androidState;
		}
		if (num2 <= 0 || !IsHeart(num2))
		{
			androidState.reason = "請安裝心臟";
			return androidState;
		}
		int num3 = GradeOf(num);
		if (GradeOf(num2) > num3 + 3)
		{
			androidState.reason = "心臟等級過高（需不超過機器人等級 +3）";
			return androidState;
		}
		int templateId = TemplateOf(num);
		Dictionary<string, object> dictionary = Template(templateId);
		if (dictionary == null)
		{
			androidState.reason = "缺少外观模板 " + templateId;
			return androidState;
		}
		androidState.templateId = templateId;
		androidState.gender = TemplateInt(dictionary, "info", "gender");
		List<int> list = TemplateList(dictionary, "skin");
		androidState.skin = ((list != null && list.Count > 0) ? list[0] : 2000);
		androidState.hair = PickFrom(dictionary, "hair", characterId, templateId, 1);
		androidState.face = PickFrom(dictionary, "face", characterId, templateId, 7);
		ApplyWardrobe(androidState, dictionary, slotOf);
		androidState.visible = true;
		return androidState;
	}

	private static void ApplyWardrobe(AndroidState state, Dictionary<string, object> template, Func<int, int> slotOf)
	{
		Dictionary<string, object> basic = (template.TryGetValue("basic", out var value) ? (value as Dictionary<string, object>) : null);
		state.hat = BasicInt(basic, "cap");
		state.cape = BasicInt(basic, "cape");
		state.faceAccessory = BasicInt(basic, "faceAccessory", "accessory");
		state.top = BasicInt(basic, "longcoat");
		state.shoes = BasicInt(basic, "shoes");
		state.gloves = BasicInt(basic, "gloves", "glove");
		if (slotOf(1200) > 0)
		{
			state.hat = slotOf(1200);
		}
		if (slotOf(1201) > 0)
		{
			state.cape = slotOf(1201);
		}
		if (slotOf(1202) > 0)
		{
			state.faceAccessory = slotOf(1202);
		}
		if (slotOf(1203) > 0)
		{
			state.top = slotOf(1203);
		}
		if (slotOf(1204) > 0)
		{
			state.bottom = slotOf(1204);
		}
		if (slotOf(1205) > 0)
		{
			state.shoes = slotOf(1205);
		}
		if (slotOf(1206) > 0)
		{
			state.gloves = slotOf(1206);
		}
		if (slotOf(1207) > 0)
		{
			state.weapon = slotOf(1207);
		}
	}

	public static bool IsApSlot(int slot)
	{
		if (slot < 1200 || slot > 1207)
		{
			if (slot >= 1210)
			{
				return slot <= 1213;
			}
			return false;
		}
		return true;
	}

	public static bool ApAccepts(int apSlot, int itemSlot)
	{
		if (itemSlot <= -100)
		{
			itemSlot += 100;
		}
		switch (apSlot)
		{
		case 1200:
			return itemSlot == -1;
		case 1201:
			return itemSlot == -9;
		case 1202:
			return itemSlot == -2;
		case 1203:
			return itemSlot == -5;
		case 1204:
			return itemSlot == -6;
		case 1205:
			return itemSlot == -7;
		case 1206:
			return itemSlot == -8;
		case 1207:
			return itemSlot == -11;
		case 1210:
		case 1211:
		case 1212:
		case 1213:
			if (itemSlot != -12 && itemSlot != -13 && itemSlot != -15)
			{
				return itemSlot == -16;
			}
			return true;
		default:
			return false;
		}
	}

	public static List<int> Pool(int templateId, string key)
	{
		Dictionary<string, object> dictionary = Template(templateId);
		if (dictionary != null)
		{
			return TemplateList(dictionary, key);
		}
		return null;
	}

	public static List<Dictionary<string, object>> ActionEntries(int templateId, string action)
	{
		Dictionary<string, object> dictionary = Template(templateId);
		if (dictionary == null || !dictionary.TryGetValue("actions", out var value))
		{
			return null;
		}
		if (!(value is Dictionary<string, object> dictionary2))
		{
			return null;
		}
		if (!dictionary2.TryGetValue(action, out var value2) || !(value2 is List<object> list))
		{
			return null;
		}
		List<Dictionary<string, object>> list2 = new List<Dictionary<string, object>>();
		foreach (object item2 in list)
		{
			if (item2 is Dictionary<string, object> item)
			{
				list2.Add(item);
			}
		}
		if (list2.Count != 0)
		{
			return list2;
		}
		return null;
	}

	public static Dictionary<string, object> RollAction(int templateId, string action, Random random)
	{
		List<Dictionary<string, object>> list = ActionEntries(templateId, action);
		if (list == null)
		{
			return null;
		}
		int num = 0;
		foreach (Dictionary<string, object> item in list)
		{
			num += Math.Max(1, Convert.ToInt32(item.TryGetValue("prob", out var value) ? value : ((object)1)));
		}
		int num2 = (random ?? new Random()).Next(num);
		foreach (Dictionary<string, object> item2 in list)
		{
			num2 -= Math.Max(1, Convert.ToInt32(item2.TryGetValue("prob", out var value2) ? value2 : ((object)1)));
			if (num2 < 0)
			{
				return item2;
			}
		}
		return list[list.Count - 1];
	}

	private static int TemplateInt(Dictionary<string, object> template, string section, string key)
	{
		if (!template.TryGetValue(section, out var value) || !(value is Dictionary<string, object> dictionary))
		{
			return 0;
		}
		if (!dictionary.TryGetValue(key, out var value2))
		{
			return 0;
		}
		return Convert.ToInt32(value2);
	}

	private static List<int> TemplateList(Dictionary<string, object> template, string key)
	{
		if (!template.TryGetValue(key, out var value) || !(value is List<object> list))
		{
			return null;
		}
		List<int> list2 = new List<int>(list.Count);
		foreach (object item in list)
		{
			list2.Add(Convert.ToInt32(item));
		}
		return list2;
	}

	private static int PickFrom(Dictionary<string, object> template, string key, int characterId, int templateId, int stride)
	{
		List<int> list = TemplateList(template, key);
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		uint num = 2166136261u;
		num = (num ^ (uint)characterId) * 16777619;
		num = (num ^ (uint)templateId) * 16777619;
		num = (num ^ (uint)stride) * 16777619;
		num ^= num >> 15;
		num *= 2246822519u;
		num ^= num >> 13;
		return list[(int)(num % (uint)list.Count)];
	}

	private static int BasicInt(Dictionary<string, object> basic, params string[] keys)
	{
		if (basic == null)
		{
			return 0;
		}
		foreach (string key in keys)
		{
			if (basic.TryGetValue(key, out var value) && Convert.ToInt32(value) > 0)
			{
				return Convert.ToInt32(value);
			}
		}
		return 0;
	}
}
