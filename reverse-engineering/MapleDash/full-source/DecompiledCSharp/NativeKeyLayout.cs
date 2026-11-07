using System.Collections.Generic;
using UnityEngine;

public static class NativeKeyLayout
{
	private static readonly Dictionary<int, KeyCode> scanCodes = new Dictionary<int, KeyCode>
	{
		{
			1,
			(KeyCode)27
		},
		{
			2,
			(KeyCode)49
		},
		{
			3,
			(KeyCode)50
		},
		{
			4,
			(KeyCode)51
		},
		{
			5,
			(KeyCode)52
		},
		{
			6,
			(KeyCode)53
		},
		{
			7,
			(KeyCode)54
		},
		{
			8,
			(KeyCode)55
		},
		{
			9,
			(KeyCode)56
		},
		{
			10,
			(KeyCode)57
		},
		{
			11,
			(KeyCode)48
		},
		{
			12,
			(KeyCode)45
		},
		{
			13,
			(KeyCode)61
		},
		{
			14,
			(KeyCode)8
		},
		{
			15,
			(KeyCode)9
		},
		{
			16,
			(KeyCode)113
		},
		{
			17,
			(KeyCode)119
		},
		{
			18,
			(KeyCode)101
		},
		{
			19,
			(KeyCode)114
		},
		{
			20,
			(KeyCode)116
		},
		{
			21,
			(KeyCode)121
		},
		{
			22,
			(KeyCode)117
		},
		{
			23,
			(KeyCode)105
		},
		{
			24,
			(KeyCode)111
		},
		{
			25,
			(KeyCode)112
		},
		{
			26,
			(KeyCode)91
		},
		{
			27,
			(KeyCode)93
		},
		{
			28,
			(KeyCode)13
		},
		{
			29,
			(KeyCode)306
		},
		{
			30,
			(KeyCode)97
		},
		{
			31,
			(KeyCode)115
		},
		{
			32,
			(KeyCode)100
		},
		{
			33,
			(KeyCode)102
		},
		{
			34,
			(KeyCode)103
		},
		{
			35,
			(KeyCode)104
		},
		{
			36,
			(KeyCode)106
		},
		{
			37,
			(KeyCode)107
		},
		{
			38,
			(KeyCode)108
		},
		{
			39,
			(KeyCode)59
		},
		{
			40,
			(KeyCode)39
		},
		{
			41,
			(KeyCode)96
		},
		{
			42,
			(KeyCode)304
		},
		{
			43,
			(KeyCode)92
		},
		{
			44,
			(KeyCode)122
		},
		{
			45,
			(KeyCode)120
		},
		{
			46,
			(KeyCode)99
		},
		{
			47,
			(KeyCode)118
		},
		{
			48,
			(KeyCode)98
		},
		{
			49,
			(KeyCode)110
		},
		{
			50,
			(KeyCode)109
		},
		{
			51,
			(KeyCode)44
		},
		{
			52,
			(KeyCode)46
		},
		{
			53,
			(KeyCode)47
		},
		{
			54,
			(KeyCode)303
		},
		{
			56,
			(KeyCode)308
		},
		{
			57,
			(KeyCode)32
		},
		{
			58,
			(KeyCode)301
		},
		{
			59,
			(KeyCode)282
		},
		{
			60,
			(KeyCode)283
		},
		{
			61,
			(KeyCode)284
		},
		{
			62,
			(KeyCode)285
		},
		{
			63,
			(KeyCode)286
		},
		{
			64,
			(KeyCode)287
		},
		{
			65,
			(KeyCode)288
		},
		{
			66,
			(KeyCode)289
		},
		{
			67,
			(KeyCode)290
		},
		{
			68,
			(KeyCode)291
		},
		{
			71,
			(KeyCode)278
		},
		{
			73,
			(KeyCode)280
		},
		{
			79,
			(KeyCode)279
		},
		{
			81,
			(KeyCode)281
		},
		{
			82,
			(KeyCode)277
		},
		{
			83,
			(KeyCode)127
		},
		{
			87,
			(KeyCode)292
		},
		{
			88,
			(KeyCode)293
		},
		{
			89,
			(KeyCode)305
		},
		{
			90,
			(KeyCode)307
		},
		{
			157,
			(KeyCode)305
		},
		{
			184,
			(KeyCode)307
		},
		{
			199,
			(KeyCode)278
		},
		{
			200,
			(KeyCode)273
		},
		{
			201,
			(KeyCode)280
		},
		{
			203,
			(KeyCode)276
		},
		{
			205,
			(KeyCode)275
		},
		{
			207,
			(KeyCode)279
		},
		{
			208,
			(KeyCode)274
		},
		{
			209,
			(KeyCode)281
		},
		{
			210,
			(KeyCode)277
		},
		{
			211,
			(KeyCode)127
		}
	};

	private static readonly Dictionary<KeyCode, Texture2D> nameTex = new Dictionary<KeyCode, Texture2D>();

	public static readonly Dictionary<KeyCode, Vector2> Positions = new Dictionary<KeyCode, Vector2>
	{
		{
			(KeyCode)49,
			new Vector2(48f, 66f)
		},
		{
			(KeyCode)50,
			new Vector2(82f, 66f)
		},
		{
			(KeyCode)51,
			new Vector2(116f, 66f)
		},
		{
			(KeyCode)52,
			new Vector2(150f, 66f)
		},
		{
			(KeyCode)53,
			new Vector2(184f, 66f)
		},
		{
			(KeyCode)54,
			new Vector2(218f, 66f)
		},
		{
			(KeyCode)55,
			new Vector2(252f, 66f)
		},
		{
			(KeyCode)56,
			new Vector2(286f, 66f)
		},
		{
			(KeyCode)57,
			new Vector2(320f, 66f)
		},
		{
			(KeyCode)48,
			new Vector2(354f, 66f)
		},
		{
			(KeyCode)45,
			new Vector2(388f, 66f)
		},
		{
			(KeyCode)61,
			new Vector2(422f, 66f)
		},
		{
			(KeyCode)113,
			new Vector2(64f, 99f)
		},
		{
			(KeyCode)119,
			new Vector2(98f, 99f)
		},
		{
			(KeyCode)101,
			new Vector2(132f, 99f)
		},
		{
			(KeyCode)114,
			new Vector2(166f, 99f)
		},
		{
			(KeyCode)116,
			new Vector2(200f, 99f)
		},
		{
			(KeyCode)121,
			new Vector2(234f, 99f)
		},
		{
			(KeyCode)117,
			new Vector2(268f, 99f)
		},
		{
			(KeyCode)105,
			new Vector2(302f, 99f)
		},
		{
			(KeyCode)111,
			new Vector2(336f, 99f)
		},
		{
			(KeyCode)112,
			new Vector2(370f, 99f)
		},
		{
			(KeyCode)91,
			new Vector2(404f, 99f)
		},
		{
			(KeyCode)93,
			new Vector2(438f, 99f)
		},
		{
			(KeyCode)306,
			new Vector2(22f, 198f)
		},
		{
			(KeyCode)97,
			new Vector2(81f, 132f)
		},
		{
			(KeyCode)115,
			new Vector2(115f, 132f)
		},
		{
			(KeyCode)100,
			new Vector2(149f, 132f)
		},
		{
			(KeyCode)102,
			new Vector2(183f, 132f)
		},
		{
			(KeyCode)103,
			new Vector2(217f, 132f)
		},
		{
			(KeyCode)104,
			new Vector2(251f, 132f)
		},
		{
			(KeyCode)106,
			new Vector2(285f, 132f)
		},
		{
			(KeyCode)107,
			new Vector2(319f, 132f)
		},
		{
			(KeyCode)108,
			new Vector2(353f, 132f)
		},
		{
			(KeyCode)59,
			new Vector2(387f, 132f)
		},
		{
			(KeyCode)39,
			new Vector2(421f, 132f)
		},
		{
			(KeyCode)96,
			new Vector2(14f, 66f)
		},
		{
			(KeyCode)304,
			new Vector2(38f, 165f)
		},
		{
			(KeyCode)92,
			new Vector2(472f, 99f)
		},
		{
			(KeyCode)122,
			new Vector2(98f, 165f)
		},
		{
			(KeyCode)120,
			new Vector2(132f, 165f)
		},
		{
			(KeyCode)99,
			new Vector2(166f, 165f)
		},
		{
			(KeyCode)118,
			new Vector2(200f, 165f)
		},
		{
			(KeyCode)98,
			new Vector2(234f, 165f)
		},
		{
			(KeyCode)110,
			new Vector2(268f, 165f)
		},
		{
			(KeyCode)109,
			new Vector2(302f, 165f)
		},
		{
			(KeyCode)44,
			new Vector2(336f, 165f)
		},
		{
			(KeyCode)46,
			new Vector2(370f, 165f)
		},
		{
			(KeyCode)303,
			new Vector2(457f, 165f)
		},
		{
			(KeyCode)308,
			new Vector2(122f, 198f)
		},
		{
			(KeyCode)32,
			new Vector2(233f, 198f)
		},
		{
			(KeyCode)282,
			new Vector2(82f, 27f)
		},
		{
			(KeyCode)283,
			new Vector2(116f, 27f)
		},
		{
			(KeyCode)284,
			new Vector2(150f, 27f)
		},
		{
			(KeyCode)285,
			new Vector2(184f, 27f)
		},
		{
			(KeyCode)286,
			new Vector2(226f, 27f)
		},
		{
			(KeyCode)287,
			new Vector2(260f, 27f)
		},
		{
			(KeyCode)288,
			new Vector2(294f, 27f)
		},
		{
			(KeyCode)289,
			new Vector2(328f, 27f)
		},
		{
			(KeyCode)290,
			new Vector2(370f, 27f)
		},
		{
			(KeyCode)291,
			new Vector2(404f, 27f)
		},
		{
			(KeyCode)278,
			new Vector2(548f, 66f)
		},
		{
			(KeyCode)280,
			new Vector2(582f, 66f)
		},
		{
			(KeyCode)279,
			new Vector2(548f, 99f)
		},
		{
			(KeyCode)281,
			new Vector2(582f, 99f)
		},
		{
			(KeyCode)277,
			new Vector2(514f, 66f)
		},
		{
			(KeyCode)127,
			new Vector2(514f, 99f)
		},
		{
			(KeyCode)292,
			new Vector2(438f, 27f)
		},
		{
			(KeyCode)293,
			new Vector2(472f, 27f)
		},
		{
			(KeyCode)305,
			new Vector2(461f, 198f)
		},
		{
			(KeyCode)307,
			new Vector2(348f, 198f)
		}
	};

	public static IEnumerable<KeyCode> PhysicalKeys => scanCodes.Values;

	public static KeyCode FunctionKey(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)key == 303)
		{
			return (KeyCode)304;
		}
		if ((int)key == 305)
		{
			return (KeyCode)306;
		}
		if ((int)key == 307)
		{
			return (KeyCode)308;
		}
		return key;
	}

	public static KeyCode OtherModifier(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		if ((int)key != 304)
		{
			if ((int)key != 306)
			{
				if ((int)key != 308)
				{
					return (KeyCode)0;
				}
				return (KeyCode)307;
			}
			return (KeyCode)305;
		}
		return (KeyCode)303;
	}

	public static int VirtualKey(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected I4, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected I4, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Invalid comparison between Unknown and I4
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Invalid comparison between Unknown and I4
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Invalid comparison between Unknown and I4
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Invalid comparison between Unknown and I4
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Invalid comparison between Unknown and I4
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected I4, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected I4, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected I4, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected I4, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		key = FunctionKey(key);
		if ((int)key >= 97 && (int)key <= 122)
		{
			return key - 97 + 65;
		}
		if ((int)key >= 48 && (int)key <= 57)
		{
			return (int)key;
		}
		if ((int)key >= 282 && (int)key <= 293)
		{
			return key - 282 + 112;
		}
		if ((int)key <= 47)
		{
			if ((int)key <= 13)
			{
				if ((int)key == 8)
				{
					return 8;
				}
				if ((int)key == 9)
				{
					return 9;
				}
				if ((int)key == 13)
				{
					return 13;
				}
			}
			else if ((int)key <= 27)
			{
				if ((int)key == 19)
				{
					return 19;
				}
				if ((int)key == 27)
				{
					return 27;
				}
			}
			else
			{
				if ((int)key == 32)
				{
					return 32;
				}
				switch (key - 39)
				{
				case 5:
					return 188;
				case 6:
					return 189;
				case 7:
					return 190;
				case 8:
					return 191;
				case 0:
					return 222;
				}
			}
		}
		else if ((int)key <= 96)
		{
			if ((int)key == 59)
			{
				return 186;
			}
			if ((int)key == 61)
			{
				return 187;
			}
			switch (key - 91)
			{
			case 5:
				return 192;
			case 0:
				return 219;
			case 1:
				return 220;
			case 2:
				return 221;
			}
		}
		else if ((int)key <= 281)
		{
			if ((int)key == 127)
			{
				return 46;
			}
			switch (key - 273)
			{
			case 7:
				return 33;
			case 8:
				return 34;
			case 6:
				return 35;
			case 5:
				return 36;
			case 3:
				return 37;
			case 0:
				return 38;
			case 2:
				return 39;
			case 1:
				return 40;
			case 4:
				return 45;
			}
		}
		else
		{
			if ((int)key == 301)
			{
				return 20;
			}
			switch (key - 304)
			{
			case 0:
				return 16;
			case 2:
				return 17;
			case 4:
				return 18;
			}
		}
		return 0;
	}

	public static Vector2 TrayPosition(GameAction action)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		NativeKeyActions.Entry entry = NativeKeyActions.Of(action);
		if (entry == null)
		{
			return Vector2.zero;
		}
		int num;
		if (entry.id >= 100)
		{
			num = entry.id - 100 + 33;
		}
		else
		{
			num = ((entry.id >= 50) ? (entry.id - 50 + 28) : entry.id);
		}
		return new Vector2((float)(9 + num % 18 * 34), (float)(267 + num / 18 * 34));
	}

	public static KeyCode FromScanCode(int scan)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (!scanCodes.TryGetValue(scan, out var value))
		{
			return (KeyCode)0;
		}
		return FunctionKey(value);
	}

	public static int ToScanCode(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		key = FunctionKey(key);
		foreach (KeyValuePair<int, KeyCode> scanCode in scanCodes)
		{
			if (scanCode.Value == key)
			{
				return scanCode.Key;
			}
		}
		return -1;
	}

	public static string NamePath(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		int num = ToScanCode(key);
		if (num >= 0)
		{
			return "wz://UI/UIWindow.img/KeyConfig/key/" + num;
		}
		return null;
	}

	public static Texture2D NameTexture(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)key == 0)
		{
			return null;
		}
		if (nameTex.TryGetValue(key, out var value))
		{
			return value;
		}
		string text = NamePath(key);
		value = ((text == null) ? null : Art.Tex(text));
		nameTex[key] = value;
		return value;
	}

	static NativeKeyLayout()
	{
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_050a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Unknown result type (might be due to invalid IL or missing references)
		//IL_0538: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Unknown result type (might be due to invalid IL or missing references)
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
		//IL_064f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0707: Unknown result type (might be due to invalid IL or missing references)
		//IL_071e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0735: Unknown result type (might be due to invalid IL or missing references)
		//IL_074c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0763: Unknown result type (might be due to invalid IL or missing references)
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0794: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0807: Unknown result type (might be due to invalid IL or missing references)
		//IL_081e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0835: Unknown result type (might be due to invalid IL or missing references)
		//IL_084c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0863: Unknown result type (might be due to invalid IL or missing references)
		//IL_087d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0897: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0916: Unknown result type (might be due to invalid IL or missing references)
		//IL_0930: Unknown result type (might be due to invalid IL or missing references)
		//IL_094a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0964: Unknown result type (might be due to invalid IL or missing references)
		//IL_097e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0998: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a65: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a99: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab3: Unknown result type (might be due to invalid IL or missing references)
	}
}
