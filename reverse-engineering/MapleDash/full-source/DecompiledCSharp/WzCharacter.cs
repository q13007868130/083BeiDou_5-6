using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using Server.Bot;
using UnityEngine;

public static class WzCharacter
{
	private sealed class Part
	{
		public string Category;

		public string Name;

		public string Slot;

		public ImgNode Node;

		public Vector2Int Origin;

		public Vector2Int Top;

		public readonly Dictionary<string, Vector2Int> Anchors = new Dictionary<string, Vector2Int>();

		public int Sequence;

		public int Z;
	}

	private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, Art.Clip> clips = new Dictionary<string, Art.Clip>();

	private static readonly string[] anchorPriority = new string[5] { "navel", "neck", "hand", "brow", "handMove" };

	private static readonly Dictionary<string, Dictionary<string, object>> swappedRecipes = new Dictionary<string, Dictionary<string, object>>();

	private static string equippedIdFor;

	private static string equippedIdCache;

	private static readonly string[][] attackFallback = new string[10][]
	{
		null,
		new string[5] { "swingO1", "swingO2", "swingO3", "stabO1", "stabO2" },
		new string[4] { "stabT1", "stabT2", "swingT2", "swingP1" },
		new string[3] { "shoot1", "shoot2", "shootF" },
		new string[3] { "shoot2", "shoot1", "shootF" },
		new string[3] { "swingT1", "swingT2", "swingT3" },
		new string[3] { "swingO1", "swingO2", "stabO1" },
		new string[3] { "stabO1", "stabO2", "swingO1" },
		new string[3] { "stabO1", "stabO2", "swingO1" },
		new string[3] { "shoot1", "shoot2", "shootF" }
	};

	private static readonly HashSet<string> CapHideTop = new HashSet<string> { "hairOverHead", "backHair" };

	private static readonly HashSet<string> CapHidePartial = new HashSet<string> { "hairOverHead", "backHair", "hairBelowBody", "backHairBelowCap" };

	private static readonly Dictionary<string, string[]> OverCapRemap = new Dictionary<string, string[]>
	{
		{
			"accessoryEyeOverCap",
			new string[2] { "accessoryEye", "Ay" }
		},
		{
			"accessoryFaceOverCap",
			new string[2] { "accessoryFace", "Af" }
		},
		{
			"accessoryFaceUpperOverCap",
			new string[2] { "accessoryFace", "Af" }
		},
		{
			"cap",
			new string[2] { "capBelowHairOverHead", null }
		}
	};

	private static readonly Dictionary<string, Dictionary<string, object>> appearanceRecipes = new Dictionary<string, Dictionary<string, object>>();

	private static Dictionary<string, object> Recipe(string key)
	{
		if (appearanceRecipes.TryGetValue(key, out var value))
		{
			return value;
		}
		if (key.StartsWith("beginner_", StringComparison.Ordinal))
		{
			key = "knight_" + key.Substring(9);
		}
		if (!((Dictionary<string, object>)WzAssets.Index["characters"]).TryGetValue(key, out var value2))
		{
			return null;
		}
		Dictionary<string, object> dictionary = (Dictionary<string, object>)value2;
		string text = EquippedWeaponId();
		if (text == null)
		{
			return dictionary;
		}
		string key2 = key + "|" + text;
		if (swappedRecipes.TryGetValue(key2, out var value3))
		{
			return value3;
		}
		List<object> list = (List<object>)dictionary["ids"];
		if (list.Count == 0 || Category((string)list[list.Count - 1]) != "Weapon")
		{
			return dictionary;
		}
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>(dictionary);
		List<object> list2 = new List<object>(list);
		list2[list2.Count - 1] = text;
		dictionary2["ids"] = list2;
		swappedRecipes[key2] = dictionary2;
		return dictionary2;
	}

	private static string EquippedWeaponId()
	{
		string equipWeapon = Profile.equipWeapon;
		if (equipWeapon == equippedIdFor)
		{
			return equippedIdCache;
		}
		equippedIdFor = equipWeapon;
		equippedIdCache = null;
		if (string.IsNullOrEmpty(equipWeapon))
		{
			return null;
		}
		string text;
		try
		{
			text = WeaponDefs.Canonical(equipWeapon);
		}
		catch
		{
			return null;
		}
		if (text.Length == 7)
		{
			text = "0" + text;
		}
		if (text.Length != 8)
		{
			return null;
		}
		int num;
		try
		{
			num = int.Parse(text) / 10000;
		}
		catch
		{
			return null;
		}
		if (num < 121 || num > 160)
		{
			return null;
		}
		if (!WzAssets.HasFile("Character/Weapon/" + text + ".img"))
		{
			return null;
		}
		equippedIdCache = text;
		return text;
	}

	public static string[] CharacterIds(CharRecord rec, IEnumerable<InventoryEntry> inventory)
	{
		List<string> list = new List<string>
		{
			(2000 + rec.skincolor).ToString("D8"),
			(12000 + rec.skincolor).ToString("D8"),
			rec.face.ToString("D8"),
			rec.hair.ToString("D8")
		};
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (InventoryEntry item in inventory)
		{
			if (item.inventoryType == 1 && item.itemId > 0)
			{
				dictionary[item.position] = item.itemId;
			}
		}
		int[] appearance = EquipSlot.Appearance;
		foreach (int num in appearance)
		{
			if (num != -11 && (dictionary.TryGetValue(num - 100, out var value) || dictionary.TryGetValue(num, out value)))
			{
				list.Add(value.ToString("D8"));
			}
		}
		if (dictionary.TryGetValue(-111, out var value2))
		{
			list.Add(value2.ToString("D8"));
		}
		if (dictionary.TryGetValue(-11, out var value3))
		{
			list.Add(value3.ToString("D8"));
		}
		return list.ToArray();
	}

	public static string CurrentAppearanceKey()
	{
		if (!GameSession.InGame || GameSession.Character == null)
		{
			return null;
		}
		List<InventoryEntry> list = new List<InventoryEntry>();
		int[] worn = EquipSlot.Worn;
		foreach (int num in worn)
		{
			int num2 = Profile.Data.EquipOfSlot(num);
			if (num2 > 0)
			{
				list.Add(new InventoryEntry
				{
					inventoryType = 1,
					position = num,
					itemId = num2
				});
			}
		}
		string[] array = CharacterIds(GameSession.Character.record, list);
		if (NpcField.Map != null && NpcField.Map.fieldType == 82)
		{
			array = TutorialAppearance(array);
		}
		string text = "character_" + string.Join("_", array);
		if (!appearanceRecipes.ContainsKey(text))
		{
			appearanceRecipes.Add(text, new Dictionary<string, object>
			{
				{
					"ids",
					new List<object>(array)
				},
				{ "attack", "swingO1" }
			});
		}
		return text;
	}

	public static string[] TutorialAppearance(string[] original)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < original.Length; i++)
		{
			if (i < 4 || Category(original[i]) == "Weapon")
			{
				list.Add(original[i]);
			}
		}
		list.Insert(4, "01002562");
		list.Insert(5, "01052081");
		return list.ToArray();
	}

	public static bool Has(string key)
	{
		return Recipe(key) != null;
	}

	private static string Category(string id)
	{
		int num = int.Parse(id) / 10000;
		switch (num)
		{
		case 0:
			return "Body";
		case 1:
			return "Head";
		case 2:
		case 5:
			return "Face";
		case 3:
		case 4:
		case 6:
			return "Hair";
		case 104:
			return "Coat";
		case 100:
			return "Cap";
		case 105:
			return "Longcoat";
		case 101:
		case 102:
		case 103:
			return "Accessory";
		case 108:
			return "Glove";
		case 109:
			return "Shield";
		case 110:
			return "Cape";
		case 106:
			return "Pants";
		case 107:
			return "Shoes";
		default:
			if (num != 169 && num != 170)
			{
				throw new NotSupportedException("Character equipment category " + num);
			}
			goto case 121;
		case 121:
		case 122:
		case 123:
		case 124:
		case 125:
		case 126:
		case 127:
		case 128:
		case 129:
		case 130:
		case 131:
		case 132:
		case 133:
		case 134:
		case 135:
		case 136:
		case 137:
		case 138:
		case 139:
		case 140:
		case 141:
		case 142:
		case 143:
		case 144:
		case 145:
		case 146:
		case 147:
		case 148:
		case 149:
		case 150:
		case 151:
		case 152:
		case 153:
		case 154:
		case 155:
		case 156:
		case 157:
		case 158:
		case 159:
		case 160:
			return "Weapon";
		}
	}

	private static ImgNode Equipment(string id)
	{
		string text = Category(id);
		return WzAssets.FileRoot("Character/" + ((text == "Body" || text == "Head") ? "" : (text + "/")) + id + ".img");
	}

	private static string Pose(Dictionary<string, object> recipe, string action)
	{
		if (recipe.ContainsKey("mount"))
		{
			switch (action)
			{
			case "stand":
			case "alert":
			case "hit":
			case "sit":
				return "stand1";
			case "walk":
				return "walk1";
			}
		}
		List<object> list = (List<object>)recipe["ids"];
		ImgNode imgNode = Equipment((string)list[list.Count - 1]);
		switch (action)
		{
		case "stand":
			return "stand" + imgNode.Int("info/stand", 1);
		case "walk":
			return "walk" + imgNode.Int("info/walk", 1);
		case "atk":
			return AttackPose(recipe, imgNode);
		case "hit":
			return "alert";
		case "die":
			return "dead";
		default:
			if (action.StartsWith("skill_", StringComparison.Ordinal))
			{
				string text = action.Substring(6);
				bool flag = text.EndsWith("_prepare", StringComparison.Ordinal);
				if (flag)
				{
					text = text.Substring(0, text.Length - 8);
				}
				int result = 1;
				int num = text.IndexOf('@');
				if (num >= 0)
				{
					if (!int.TryParse(text.Substring(num + 1), out result))
					{
						return null;
					}
					text = text.Substring(0, num);
				}
				int result2;
				if (((Dictionary<string, object>)WzAssets.Index["skills"]).TryGetValue(text, out var value))
				{
					result2 = Convert.ToInt32(value);
				}
				else if (!int.TryParse(text, out result2))
				{
					return null;
				}
				ImgNode imgNode2 = WzAssets.Node("Skill/" + (result2 / 10000).ToString("D3") + ".img/skill/" + result2.ToString("D7"));
				if (imgNode2 == null)
				{
					return null;
				}
				ImgNode imgNode3 = imgNode2.Get("level/" + result);
				if (flag)
				{
					return (imgNode3?.Get("prepare/action") ?? imgNode2.Get("prepare/action"))?.Value as string;
				}
				ImgNode imgNode4 = imgNode3?.Get("action") ?? imgNode2.Get("action");
				if (imgNode4 == null)
				{
					return null;
				}
				ImgNode imgNode5 = Equipment((string)list[0]);
				if (imgNode4.Value is string text2 && imgNode5.Get(text2) != null)
				{
					return text2;
				}
				foreach (ImgNode child in imgNode4.Children)
				{
					if (child.Value is string text3 && imgNode5.Get(text3) != null)
					{
						return text3;
					}
				}
				return null;
			}
			return action;
		}
	}

	private static string AttackPose(Dictionary<string, object> recipe, ImgNode weapon)
	{
		ImgNode imgNode = Equipment((string)((List<object>)recipe["ids"])[0]);
		int num = weapon.Int("info/attack", 1);
		if (num >= 1 && num < attackFallback.Length && attackFallback[num] != null)
		{
			string[] array = attackFallback[num];
			foreach (string text in array)
			{
				if (imgNode.Get(text) != null)
				{
					return text;
				}
			}
		}
		string text2 = (recipe.ContainsKey("attack") ? (recipe["attack"] as string) : null);
		if (text2 != null && imgNode.Get(text2) != null)
		{
			return text2;
		}
		return "swingO1";
	}

	public static string AttackPoseName(string key)
	{
		Dictionary<string, object> dictionary = Recipe(key);
		if (dictionary != null)
		{
			return Pose(dictionary, "atk");
		}
		return null;
	}

	public static int WeaponAttackSpeed(string key)
	{
		Dictionary<string, object> dictionary = Recipe(key);
		if (dictionary == null)
		{
			return 6;
		}
		foreach (string item in (List<object>)dictionary["ids"])
		{
			if (Category(item) == "Weapon" && !IsCashWeapon(item))
			{
				return Equipment(item).Int("info/attackSpeed", 6);
			}
		}
		return 6;
	}

	public static float AttackFrameDelay(float delay, int attackSpeed)
	{
		return (float)(Mathf.RoundToInt(delay * 1000f) * (Mathf.Clamp(attackSpeed, 2, 10) + 10) / 16) / 1000f;
	}

	public static string AttackSoundKey(string key)
	{
		Dictionary<string, object> dictionary = Recipe(key);
		if (dictionary == null)
		{
			return null;
		}
		List<object> list = (List<object>)dictionary["ids"];
		if (list.Count == 0)
		{
			return null;
		}
		string id = (string)list[list.Count - 1];
		int num;
		try
		{
			num = Equipment(id).Int("info/attack", 1);
		}
		catch
		{
			return null;
		}
		return "Weapon/" + num switch
		{
			2 => "poleArm", 
			3 => "bow", 
			4 => "cBow", 
			5 => "swordS", 
			6 => "mace", 
			7 => "tGlove", 
			8 => "knuckle", 
			9 => "gun", 
			_ => "swordL", 
		} + "/Attack";
	}

	public static string Afterimage(string key)
	{
		Dictionary<string, object> dictionary = Recipe(key);
		if (dictionary == null)
		{
			return null;
		}
		string id = (string)((List<object>)dictionary["ids"])[((List<object>)dictionary["ids"]).Count - 1];
		string text = null;
		try
		{
			text = Equipment(id).Text("info/afterImage");
		}
		catch
		{
			return null;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = AfterimageFallback(id);
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (!WzAssets.HasFile("Character/Afterimage/" + text + ".img"))
		{
			return null;
		}
		return text;
	}

	private static string AfterimageFallback(string id)
	{
		int num;
		try
		{
			num = int.Parse(id) / 10000;
		}
		catch
		{
			return null;
		}
		switch (num)
		{
		case 130:
		case 133:
		case 137:
		case 147:
			return "swordOL";
		case 131:
			return "axe";
		case 132:
			return "mace";
		case 138:
			return "swordOL";
		case 140:
			return "swordTS";
		case 141:
			return "axe";
		case 142:
			return "mace";
		case 143:
			return "spear";
		case 144:
			return "poleArm";
		case 145:
			return "bow";
		case 146:
			return "crossBow";
		case 148:
			return "knuckle";
		case 149:
			return "gun";
		default:
			return null;
		}
	}

	public static Art.Clip Clip(string key, string action)
	{
		string key2 = key + "/" + action;
		if (clips.TryGetValue(key2, out var value))
		{
			return value;
		}
		Dictionary<string, object> dictionary = Recipe(key);
		if (dictionary == null)
		{
			return null;
		}
		string text = Pose(dictionary, action);
		if (text == null)
		{
			return null;
		}
		List<ImgNode> list = WzAssets.Frames((dictionary.TryGetValue("mount", out var value2) ? WzAssets.FileRoot("Character/TamingMob/" + Convert.ToInt32(value2).ToString("D8") + ".img") : Equipment((string)((List<object>)dictionary["ids"])[0])).Get(text));
		if (list.Count == 0 && action != "die")
		{
			return null;
		}
		int num = Math.Max(1, list.Count);
		value = new Art.Clip
		{
			frames = new string[num],
			delays = new float[num]
		};
		for (int i = 0; i < num; i++)
		{
			string text2 = ((list.Count > 0) ? list[i].Name : "0");
			value.frames[i] = "avatar://" + key + "/" + text + "/" + text2;
			object obj = ((list.Count > 0) ? list[i].TryResolve()?.Get("delay")?.TryResolve()?.Value : null);
			value.delays[i] = ((list.Count > 0) ? ((float)Math.Max(1, Math.Abs((obj == null) ? 120 : Convert.ToInt32(obj))) / 1000f) : 0.12f);
		}
		clips.Add(key2, value);
		return value;
	}

	private static Vector2Int Vector(ImgNode node)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2Int(node?.X ?? 0, node?.Y ?? 0);
	}

	private static int BaseZ(string slot)
	{
		List<object> list = (List<object>)WzAssets.Index["zOrder"];
		if (string.IsNullOrEmpty(slot))
		{
			return list.Count;
		}
		int num = list.IndexOf(slot);
		if (num >= 0)
		{
			return num;
		}
		string[] array;
		if (slot.StartsWith("back", StringComparison.Ordinal))
		{
			string item = ((slot.Length > 4) ? (char.ToLowerInvariant(slot[4]) + slot.Substring(5)) : slot);
			int num2 = list.IndexOf(item);
			if (num2 >= 0)
			{
				return Math.Max(1, num2 - 5);
			}
			array = new string[2] { "Below", "Over" };
			foreach (string text in array)
			{
				int num3 = slot.IndexOf(text, StringComparison.Ordinal);
				if (num3 < 0)
				{
					continue;
				}
				string text2 = slot.Substring(num3 + text.Length);
				if (!(text2 == ""))
				{
					int num4 = list.IndexOf("back" + char.ToUpperInvariant(text2[0]) + text2.Substring(1));
					if (num4 >= 0)
					{
						return Math.Max(1, num4 + ((!(text == "Below")) ? 1 : (-1)));
					}
				}
			}
			return Math.Max(1, list.IndexOf("body") - 1);
		}
		string text3 = slot.ToLowerInvariant();
		if (text3.StartsWith("cap") && text3.Contains("below"))
		{
			int num5 = list.IndexOf("cap");
			if (num5 >= 0)
			{
				return Math.Max(1, num5 - 1);
			}
		}
		array = new string[2] { "Below", "below" };
		foreach (string text4 in array)
		{
			int num6 = slot.IndexOf(text4, StringComparison.Ordinal);
			if (num6 < 0)
			{
				continue;
			}
			string text5 = slot.Substring(num6 + text4.Length);
			if (!(text5 == ""))
			{
				int num7 = list.IndexOf(char.ToLowerInvariant(text5[0]) + text5.Substring(1));
				if (num7 >= 0)
				{
					return Math.Max(1, num7 - 1);
				}
			}
		}
		array = new string[2] { "Over", "over" };
		foreach (string text6 in array)
		{
			int num8 = slot.IndexOf(text6, StringComparison.Ordinal);
			if (num8 < 0)
			{
				continue;
			}
			string text7 = slot.Substring(num8 + text6.Length);
			if (!(text7 == ""))
			{
				int num9 = list.IndexOf(char.ToLowerInvariant(text7[0]) + text7.Substring(1));
				if (num9 >= 0)
				{
					return Math.Min(list.Count - 1, num9 + 1);
				}
			}
		}
		return list.Count - 1;
	}

	private static int Z(Part part, string pose, bool capCoversHair, HashSet<string> capTokens)
	{
		string text = part.Slot;
		if (pose == "stand1" && part.Category == "Weapon" && text == "weaponOverArm")
		{
			text = "weapon";
		}
		else if (Array.IndexOf(new string[8] { "prone", "proneStab", "shoot1", "shoot2", "shootF", "swingT1", "stabO1", "stabO2" }, pose) >= 0)
		{
			if (part.Category == "Weapon" && (text == "weapon" || text == "weaponBelowArm"))
			{
				text = "weaponOverHand";
			}
			else if (part.Category == "Body" && (text == "arm" || text == "armOverHair"))
			{
				text = "handOverHair";
			}
		}
		else
		{
			if (Array.IndexOf(new string[3] { "swingT2", "stabT1", "stabT2" }, pose) < 0 || !(part.Category == "Body"))
			{
				goto IL_0169;
			}
			switch (text)
			{
			case "arm":
			case "armBelowHead":
			case "armBelowHeadOverMailChest":
				break;
			default:
				goto IL_0169;
			}
			text = "handOverHair";
		}
		goto IL_01dc;
		IL_0169:
		if (pose == "swingT3" && part.Category == "Body" && (text == "arm" || text == "armBelowHeadOverMailChest"))
		{
			text = "handOverHair";
		}
		else if (pose == "stand2" && part.Category == "Weapon" && text == "weaponBelowArm")
		{
			text = "weaponOverArm";
		}
		goto IL_01dc;
		IL_01dc:
		if (text != null && OverCapRemap.TryGetValue(text, out var value) && (!capCoversHair || (value[1] != null && capTokens.Contains(value[1]))))
		{
			text = value[0];
		}
		if (pose == "ladder" || pose == "rope")
		{
			Dictionary<string, object> dictionary = (Dictionary<string, object>)WzAssets.Index["backAbsoluteZ"];
			if (text != null && dictionary.TryGetValue(text, out var value2))
			{
				return Convert.ToInt32(value2);
			}
			Dictionary<string, object> dictionary2 = (Dictionary<string, object>)WzAssets.Index["backZ"];
			if (text != null && dictionary2.TryGetValue(text, out var value3))
			{
				return Convert.ToInt32(value3);
			}
			if (text == null || !text.StartsWith("back", StringComparison.Ordinal))
			{
				return -1;
			}
		}
		return BaseZ(text);
	}

	private static bool IsCashWeapon(string id)
	{
		if (int.TryParse(id, out var result))
		{
			return result / 10000 == 170;
		}
		return false;
	}

	private static ImgNode RenderRoot(ImgNode root, string id, string pose)
	{
		if (!IsCashWeapon(id))
		{
			return root;
		}
		List<ImgNode> list = root.Children.FindAll((ImgNode n) => int.TryParse(n.Name, out var _));
		list.Sort((ImgNode a, ImgNode b) => int.Parse(a.Name).CompareTo(int.Parse(b.Name)));
		foreach (ImgNode item in list)
		{
			if (item.Get(pose + "/0")?.TryResolve() != null)
			{
				return item;
			}
		}
		string[] array = new string[2] { "stand1", "stand2" };
		foreach (string text in array)
		{
			foreach (ImgNode item2 in list)
			{
				if (item2.Get(text + "/0")?.TryResolve() != null)
				{
					return item2;
				}
			}
		}
		if (list.Count <= 0)
		{
			return root;
		}
		return list[0];
	}

	private static List<Part> Placements(Dictionary<string, object> recipe, ref string pose, ref int frame)
	{
		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0644: Unknown result type (might be due to invalid IL or missing references)
		//IL_0649: Unknown result type (might be due to invalid IL or missing references)
		//IL_064e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0650: Unknown result type (might be due to invalid IL or missing references)
		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
		//IL_080b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0810: Unknown result type (might be due to invalid IL or missing references)
		//IL_0812: Unknown result type (might be due to invalid IL or missing references)
		//IL_0817: Unknown result type (might be due to invalid IL or missing references)
		//IL_0741: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_0721: Unknown result type (might be due to invalid IL or missing references)
		//IL_0725: Unknown result type (might be due to invalid IL or missing references)
		//IL_072a: Unknown result type (might be due to invalid IL or missing references)
		//IL_072f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0731: Unknown result type (might be due to invalid IL or missing references)
		//IL_0736: Unknown result type (might be due to invalid IL or missing references)
		//IL_0760: Unknown result type (might be due to invalid IL or missing references)
		//IL_0759: Unknown result type (might be due to invalid IL or missing references)
		//IL_0762: Unknown result type (might be due to invalid IL or missing references)
		//IL_0767: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		if (recipe.ContainsKey("mount"))
		{
			return MountedPlacements(recipe, pose, frame);
		}
		List<object> list = (List<object>)recipe["ids"];
		ImgNode imgNode = Equipment((string)list[0]);
		for (int i = 0; i < 64; i++)
		{
			ImgNode imgNode2 = imgNode.Get(pose + "/" + frame);
			if (!(imgNode2?.Get("action")?.Value is string text))
			{
				break;
			}
			pose = text;
			frame = imgNode2.Int("frame");
			if (i == 63)
			{
				throw new InvalidDataException("Character action cycle");
			}
		}
		ImgNode imgNode3 = Equipment((string)list[list.Count - 1]);
		if (imgNode3.Get(pose) == null && pose.StartsWith("swingO", StringComparison.Ordinal) && imgNode3.Get(pose.Replace("swingO", "swingT")) != null)
		{
			pose = pose.Replace("swingO", "swingT");
		}
		string text2 = null;
		foreach (string item in list)
		{
			if (Category(item) == "Cap")
			{
				text2 = item;
				break;
			}
		}
		HashSet<string> hashSet = new HashSet<string>();
		bool flag = false;
		HashSet<string> hashSet2 = new HashSet<string>();
		if (text2 != null)
		{
			string text4 = Equipment(text2).Text("info/vslot");
			for (int j = 0; j + 1 < text4.Length; j += 2)
			{
				hashSet.Add(text4.Substring(j, 2));
			}
			if (hashSet.Contains("H2"))
			{
				flag = true;
			}
			else if (hashSet.Contains("H1"))
			{
				foreach (string item2 in hashSet.Contains("H3") ? CapHidePartial : CapHideTop)
				{
					hashSet2.Add(item2);
				}
			}
		}
		bool capCoversHair = flag || hashSet2.Count > 0;
		bool flag2 = list.Exists((object obj) => IsCashWeapon((string)obj));
		List<Part> list2 = new List<Part>();
		foreach (string item3 in list)
		{
			string text5 = Category(item3);
			if (flag2 && text5 == "Weapon" && !IsCashWeapon(item3))
			{
				continue;
			}
			ImgNode imgNode4 = RenderRoot(Equipment(item3), item3, pose);
			List<string> list3 = new List<string> { pose + "/" + frame };
			switch (text5)
			{
			case "Face":
			{
				list3.Clear();
				if (recipe.TryGetValue("expression", out var value))
				{
					list3.Add((string)value + "/" + recipe["expressionFrame"]);
				}
				list3.Add("default");
				break;
			}
			case "Head":
				list3.Add("front");
				break;
			case "Hair":
				list3.Add("default");
				break;
			}
			HashSet<ImgNode> hashSet3 = new HashSet<ImgNode>();
			HashSet<string> hashSet4 = new HashSet<string>();
			foreach (string item4 in list3)
			{
				ImgNode imgNode5 = imgNode4.Get(item4)?.TryResolve();
				if (imgNode5 == null)
				{
					continue;
				}
				int count = list2.Count;
				foreach (ImgNode child in imgNode5.Children)
				{
					if ((text5 == "Head" && child.Name != "head" && child.Name != "humanEar") || (text5 == "Hair" && (flag || hashSet2.Contains(child.Name))))
					{
						continue;
					}
					ImgNode imgNode6 = child.TryResolve();
					if (imgNode6 == null || imgNode6.Kind != "Canvas" || !hashSet3.Add(imgNode6))
					{
						continue;
					}
					string text6 = imgNode6.Get("z")?.Value as string;
					if (text6 != null && !hashSet4.Add(text6))
					{
						continue;
					}
					Part part = new Part
					{
						Category = text5,
						Name = child.Name,
						Slot = text6,
						Node = imgNode6,
						Origin = Vector(imgNode6.Get("origin")),
						Sequence = list2.Count
					};
					ImgNode imgNode7 = imgNode6.Get("map");
					if (imgNode7 != null)
					{
						foreach (ImgNode child2 in imgNode7.Children)
						{
							part.Anchors[child2.Name] = Vector(child2);
						}
					}
					list2.Add(part);
				}
				if (list2.Count > count)
				{
					break;
				}
			}
		}
		list2.Sort((Part a, Part b) =>
		{
			int num2 = ((!(a.Category == "Body")) ? ((!(a.Category == "Head")) ? 4 : ((a.Name == "head") ? 2 : 3)) : ((!(a.Name == "body")) ? 1 : 0));
			int value5 = ((!(b.Category == "Body")) ? ((!(b.Category == "Head")) ? 4 : ((b.Name == "head") ? 2 : 3)) : ((!(b.Name == "body")) ? 1 : 0));
			int num3 = num2.CompareTo(value5);
			return (num3 == 0) ? a.Sequence.CompareTo(b.Sequence) : num3;
		});
		Dictionary<string, Vector2Int> dictionary = new Dictionary<string, Vector2Int>();
		Vector2Int value2 = Vector2Int.zero;
		bool flag3 = false;
		foreach (Part item5 in list2)
		{
			if (item5.Category == "Body" && item5.Name == "body" && !flag3)
			{
				item5.Anchors.TryGetValue("navel", out value2);
				item5.Top = -item5.Origin - value2;
				flag3 = true;
			}
			else
			{
				string text7;
				if (item5.Category == "Head")
				{
					text7 = "neck";
				}
				else if (item5.Category == "Face" || item5.Category == "Hair")
				{
					text7 = "brow";
				}
				else
				{
					text7 = ((item5.Category == "Weapon") ? "hand" : "navel");
				}
				string[] array = anchorPriority;
				foreach (string text8 in array)
				{
					if (item5.Anchors.ContainsKey(text8))
					{
						text7 = text8;
						break;
					}
				}
				item5.Anchors.TryGetValue(text7, out var value3);
				if (dictionary.TryGetValue(text7, out var value4))
				{
					item5.Top = value4 - item5.Origin - value3;
				}
				else
				{
					item5.Top = -item5.Origin - ((text7 == "handMove") ? value2 : Vector2Int.zero);
				}
			}
			foreach (KeyValuePair<string, Vector2Int> anchor in item5.Anchors)
			{
				if (!dictionary.ContainsKey(anchor.Key))
				{
					dictionary.Add(anchor.Key, item5.Top + item5.Origin + anchor.Value);
				}
			}
		}
		foreach (Part item6 in list2)
		{
			item6.Top += value2;
			item6.Z = Z(item6, pose, capCoversHair, hashSet);
		}
		list2.Sort((Part a, Part b) =>
		{
			int num2 = a.Z.CompareTo(b.Z);
			return (num2 == 0) ? a.Sequence.CompareTo(b.Sequence) : num2;
		});
		return list2;
	}

	private static List<Part> MountedPlacements(Dictionary<string, object> recipe, string pose, int frame)
	{
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, object> dictionary = new Dictionary<string, object>(recipe);
		dictionary.Remove("mount");
		string pose2 = ((pose == "ladder" || pose == "rope") ? "rope" : "sit");
		int frame2 = 0;
		List<Part> list = Placements(dictionary, ref pose2, ref frame2);
		ImgNode imgNode = WzAssets.FileRoot("Character/TamingMob/" + Convert.ToInt32(recipe["mount"]).ToString("D8") + ".img").Get(pose + "/" + frame)?.TryResolve();
		List<ImgNode> list2 = new List<ImgNode>();
		if (imgNode != null)
		{
			list2.Add(imgNode);
		}
		if (recipe.TryGetValue("saddle", out var value))
		{
			ImgNode imgNode2 = WzAssets.FileRoot("Character/TamingMob/" + Convert.ToInt32(value).ToString("D8") + ".img")?.Get(recipe["mount"]?.ToString() + "/" + pose + "/" + frame)?.TryResolve();
			if (imgNode2 != null)
			{
				list2.Add(imgNode2);
			}
		}
		List<Part> list3 = new List<Part>();
		Vector2Int value2 = Vector2Int.zero;
		Vector2Int val = Vector2Int.zero;
		bool flag = false;
		foreach (ImgNode item in list2)
		{
			foreach (ImgNode child in item.Children)
			{
				ImgNode imgNode3 = child.TryResolve();
				if (imgNode3 == null || imgNode3.Kind != "Canvas")
				{
					continue;
				}
				Part part = new Part
				{
					Category = "TamingMob",
					Name = child.Name,
					Node = imgNode3,
					Origin = Vector(imgNode3.Get("origin")),
					Slot = imgNode3.Text("z"),
					Sequence = list.Count + list3.Count
				};
				part.Top = -part.Origin;
				part.Z = BaseZ(part.Slot);
				ImgNode imgNode4 = imgNode3.Get("map");
				if (imgNode4 != null)
				{
					foreach (ImgNode child2 in imgNode4.Children)
					{
						part.Anchors[child2.Name] = Vector(child2);
					}
				}
				if (!flag && part.Anchors.TryGetValue("navel", out value2))
				{
					flag = true;
				}
				list3.Add(part);
			}
		}
		foreach (Part item2 in list)
		{
			if (item2.Category == "Body" && item2.Name == "body" && item2.Anchors.TryGetValue("navel", out var value3))
			{
				val = item2.Top + item2.Origin + value3;
				break;
			}
		}
		foreach (Part item3 in list)
		{
			item3.Top += value2 - val;
		}
		foreach (Part item4 in list3)
		{
			if (item4.Anchors.TryGetValue("navel", out var value4))
			{
				item4.Top = value2 - item4.Origin - value4;
			}
		}
		list.AddRange(list3);
		list.Sort((Part a, Part b) =>
		{
			int num = a.Z.CompareTo(b.Z);
			return (num == 0) ? a.Sequence.CompareTo(b.Sequence) : num;
		});
		return list;
	}

	public static Sprite ExpressionFrame(string uri, string expression, float elapsed)
	{
		string[] array = uri.Substring("avatar://".Length).Split('/');
		Dictionary<string, object> dictionary = Recipe(array[0]);
		if (dictionary == null)
		{
			return Frame(uri);
		}
		ImgNode imgNode = null;
		foreach (string item in (List<object>)dictionary["ids"])
		{
			if (Category(item) == "Face")
			{
				imgNode = Equipment(item);
				break;
			}
		}
		List<ImgNode> list = WzAssets.Frames(imgNode?.Get(expression));
		if (list.Count == 0)
		{
			return Frame(uri);
		}
		float num = 0f;
		foreach (ImgNode item2 in list)
		{
			num += (float)Math.Max(1, item2.Int("delay", 100));
		}
		float num2 = Mathf.Repeat(elapsed * 1000f, num);
		ImgNode imgNode2 = list[0];
		foreach (ImgNode item3 in list)
		{
			imgNode2 = item3;
			num2 -= (float)Math.Max(1, item3.Int("delay", 100));
			if (num2 < 0f)
			{
				break;
			}
		}
		string text = uri + "|" + expression + "/" + imgNode2.Name;
		if (cache.TryGetValue(text, out var value))
		{
			return value;
		}
		Dictionary<string, object> recipe = new Dictionary<string, object>(dictionary)
		{
			["expression"] = expression,
			["expressionFrame"] = imgNode2.Name
		};
		using (BotPerf.Timer("WzCharacter.ExprCompose"))
		{
			return Compose(text, recipe, array[1], int.Parse(array[2]));
		}
	}

	public static Sprite Frame(string uri)
	{
		if (cache.TryGetValue(uri, out var value))
		{
			return value;
		}
		string[] array = uri.Substring("avatar://".Length).Split('/');
		Dictionary<string, object> dictionary = Recipe(array[0]);
		if (dictionary == null)
		{
			throw new InvalidDataException(uri);
		}
		string pose = array[1];
		int frame = int.Parse(array[2]);
		return Compose(uri, dictionary, pose, frame);
	}

	public static Vector2Int Attachment(string uri, string name)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(uri))
		{
			return Vector2Int.zero;
		}
		string[] array = uri.Substring("avatar://".Length).Split('/');
		Dictionary<string, object> dictionary = Recipe(array[0]);
		if (dictionary == null)
		{
			return Vector2Int.zero;
		}
		string pose = array[1];
		int frame = int.Parse(array[2]);
		foreach (Part item in Placements(dictionary, ref pose, ref frame))
		{
			if (item.Anchors.TryGetValue(name, out var value))
			{
				return item.Top + item.Origin + value;
			}
		}
		return Vector2Int.zero;
	}

	public static string MountedAppearance(string key, int itemId, int saddleId = 0)
	{
		string text = key + "_mount_" + itemId + "_" + saddleId;
		if (!appearanceRecipes.ContainsKey(text))
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>(Recipe(key));
			dictionary["mount"] = itemId;
			if (saddleId > 0)
			{
				dictionary["saddle"] = saddleId;
			}
			appearanceRecipes.Add(text, dictionary);
		}
		return text;
	}

	public static int AppearanceFrame(string[] ids, float elapsed, bool walk = false)
	{
		ImgNode imgNode = Equipment(ids[^1]);
		string path = (walk ? ("walk" + imgNode.Int("info/walk", 1)) : ("stand" + imgNode.Int("info/stand", 1)));
		ImgNode imgNode2 = Equipment(ids[0]).Get(path);
		if (imgNode2 == null)
		{
			return 0;
		}
		float num = 0f;
		int num2 = 0;
		while (imgNode2.Get(num2.ToString()) != null)
		{
			num += (float)Math.Max(1, imgNode2.Get(num2.ToString()).Int("delay", 100)) / 1000f;
			num2++;
		}
		if (num2 == 0)
		{
			return 0;
		}
		float num3 = Mathf.Repeat(Mathf.Max(0f, elapsed), num);
		for (int i = 0; i < num2; i++)
		{
			num3 -= (float)Math.Max(1, imgNode2.Get(i.ToString()).Int("delay", 100)) / 1000f;
			if (num3 < 0f)
			{
				return i;
			}
		}
		return num2 - 1;
	}

	public static Sprite Appearance(string key, string[] ids, int frame = 0, bool walk = false)
	{
		string text = (walk ? "walk" : "stand");
		string text2 = "appearance://" + key + "/" + text + "/" + frame;
		if (cache.TryGetValue(text2, out var value))
		{
			return value;
		}
		Register(key, ids);
		Dictionary<string, object> recipe = appearanceRecipes[key];
		string text3 = Pose(recipe, text);
		if (text3 == null)
		{
			return null;
		}
		return Compose(text2, recipe, text3, frame);
	}

	public static void Register(string key, string[] ids)
	{
		if (!string.IsNullOrEmpty(key) && ids != null && ids.Length != 0 && !appearanceRecipes.ContainsKey(key))
		{
			List<object> list = new List<object>(ids.Length);
			foreach (string item in ids)
			{
				list.Add(item);
			}
			appearanceRecipes.Add(key, new Dictionary<string, object>
			{
				{ "ids", list },
				{ "attack", "swingO1" }
			});
		}
	}

	private static Sprite Compose(string uri, Dictionary<string, object> recipe, string pose, int frame)
	{
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		using (BotPerf.Timer("WzCharacter.Compose"))
		{
			List<Part> list = Placements(recipe, ref pose, ref frame);
			int num = 0;
			int num2 = 0;
			int num3 = 1;
			int num4 = 1;
			foreach (Part item in list)
			{
				ImgNode imgNode = WzAssets.Canvas(item.Node);
				num = Math.Min(num, item.Top.x);
				num2 = Math.Min(num2, item.Top.y);
				num3 = Math.Max(num3, item.Top.x + imgNode.Width);
				num4 = Math.Max(num4, item.Top.y + imgNode.Height);
			}
			int num5 = num3 - num;
			int num6 = num4 - num2;
			byte[] array = new byte[checked(num5 * num6 * 4)];
			foreach (Part item2 in list)
			{
				ImgNode imgNode2 = WzAssets.Canvas(item2.Node);
				byte[] array2 = WzAssets.Pixels(item2.Node);
				for (int i = 0; i < imgNode2.Height; i++)
				{
					for (int j = 0; j < imgNode2.Width; j++)
					{
						int num7 = (i * imgNode2.Width + j) * 4;
						int num8 = ((item2.Top.y - num2 + i) * num5 + item2.Top.x - num + j) * 4;
						int num9 = array2[num7 + 3];
						if (num9 == 0)
						{
							continue;
						}
						if (num9 == 255 || array[num8 + 3] == 0)
						{
							Buffer.BlockCopy(array2, num7, array, num8, 4);
							continue;
						}
						int num10 = array[num8 + 3] * (255 - num9);
						int num11 = num9 * 255 + num10;
						for (int k = 0; k < 3; k++)
						{
							array[num8 + k] = (byte)((array2[num7 + k] * num9 * 255 + array[num8 + k] * num10 + num11 / 2) / num11);
						}
						array[num8 + 3] = (byte)((num11 + 127) / 255);
					}
				}
			}
			Sprite val = Sprite.Create(WzAssets.Upload(array, num5, num6, uri), new Rect(0f, 0f, (float)num5, (float)num6), new Vector2((float)(-num) / (float)num5, 1f + (float)num2 / (float)num6), 40f, 0u, (SpriteMeshType)0);
			((Object)val).name = uri;
			cache.Add(uri, val);
			return val;
		}
	}

	public static int PrecomposeFrameCount(string key, string anim)
	{
		return Clip(key, anim)?.frames.Length ?? 0;
	}

	public static bool PrecomposeFrame(string key, string anim, int frame)
	{
		Art.Clip clip = Clip(key, anim);
		if (clip == null || frame < 0 || frame >= clip.frames.Length)
		{
			return false;
		}
		WzAssets.Frame(clip.frames[frame]);
		return true;
	}
}
