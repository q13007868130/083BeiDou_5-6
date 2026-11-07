using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MapleDash.Wz;
using UnityEngine;

public static class Art
{
	public class StageInfo
	{
		public string map;

		public Vector2Int px;

		public Vector2Int size;

		public Vector2 ground;

		public Vector2 walk;

		public Color sky;

		public Vector4 view;

		public bool hasView;

		public readonly List<StageBack> backgrounds = new List<StageBack>();

		public string fileBack;

		public string fileMid;

		public string fileFront;

		public int orderBack = -800;

		public int orderMid = -400;

		public int orderFront = 600;

		public readonly List<Vector4> Footholds = new List<Vector4>();

		public readonly List<bool> ForbidFallDown = new List<bool>();

		public readonly List<int> FootholdDrag = new List<int>();

		public readonly List<int> FootholdForces = new List<int>();

		public readonly List<ClimbLine> Climbs = new List<ClimbLine>();

		public bool HasFront => !string.IsNullOrEmpty(fileFront);

		public float WalkMin => WorldX(walk.x);

		public float WalkMax => WorldX(walk.y);

		public float LeftX => WorldX(px.x);

		public float RightX => WorldX(px.x + size.x);

		public float TopY => WorldY(px.y);

		public float BottomY => WorldY(px.y + size.y);

		public float WorldX(float mapX)
		{
			return (mapX - ground.x) / 40f;
		}

		public float WorldY(float mapY)
		{
			return -1.4f + (ground.y - mapY) / 40f;
		}
	}

	public class ClimbLine
	{
		public float x;

		public float top;

		public float bottom;

		public bool ladder;

		public bool upperExit;
	}

	public class StageBack
	{
		public string file;

		public Vector2 origin;

		public Vector2 size;

		public int x;

		public int y;

		public int rx;

		public int ry;

		public int cx;

		public int cy;

		public int type;

		public int front;

		public int alpha;

		public int no;
	}

	public class DmgGlyph
	{
		public string file;

		public Vector2 origin;

		public Vector2 size;
	}

	public class DmgSkin
	{
		public DmgGlyph[] baseDigit = new DmgGlyph[10];

		public DmgGlyph[] edgeDigit;

		public DmgGlyph miss;

		public DmgGlyph fx;
	}

	public class Clip
	{
		public string[] frames;

		public float[] delays;

		public float Duration
		{
			get
			{
				float num = 0f;
				for (int i = 0; i < delays.Length; i++)
				{
					num += delays[i];
				}
				return Mathf.Max(0.05f, num);
			}
		}
	}

	public const float PPU = 40f;

	private static bool tried;

	private static Dictionary<string, StageInfo> stages;

	private static Dictionary<string, DmgSkin> dmgSkins;

	public static bool Ready { get; private set; }

	public static StageInfo Stage(string theme)
	{
		Ensure();
		if (stages == null || string.IsNullOrEmpty(theme))
		{
			return null;
		}
		if (!stages.TryGetValue(theme, out var value))
		{
			return null;
		}
		return value;
	}

	private static void ReadStages(Dictionary<string, object> root)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0688: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b2: Unknown result type (might be due to invalid IL or missing references)
		if (!root.ContainsKey("stages") || !(root["stages"] is Dictionary<string, object> dictionary))
		{
			return;
		}
		stages = new Dictionary<string, StageInfo>();
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (!(item.Value is Dictionary<string, object> dictionary2))
			{
				continue;
			}
			StageInfo stageInfo = new StageInfo
			{
				map = Str(dictionary2, "map")
			};
			Vector2 val = ReadVec(dictionary2, "px");
			stageInfo.px = new Vector2Int(Mathf.RoundToInt(val.x), Mathf.RoundToInt(val.y));
			stageInfo.size = new Vector2Int(Mathf.RoundToInt(ReadNum(dictionary2, "px", 2)), Mathf.RoundToInt(ReadNum(dictionary2, "px", 3)));
			stageInfo.ground = ReadVec(dictionary2, "ground");
			stageInfo.walk = (Vector2)(dictionary2.ContainsKey("walk") ? ReadVec(dictionary2, "walk") : new Vector2((float)stageInfo.px.x, (float)(stageInfo.px.x + stageInfo.size.x)));
			stageInfo.sky = ToColor(Int(dictionary2, "sky", 7321576));
			stageInfo.hasView = dictionary2.ContainsKey("view");
			if (stageInfo.hasView)
			{
				stageInfo.view = new Vector4(ReadNum(dictionary2, "view", 0), ReadNum(dictionary2, "view", 1), ReadNum(dictionary2, "view", 2), ReadNum(dictionary2, "view", 3));
			}
			if (dictionary2.ContainsKey("backgrounds") && dictionary2["backgrounds"] is List<object> list)
			{
				foreach (object item2 in list)
				{
					if (item2 is Dictionary<string, object> dictionary3)
					{
						stageInfo.backgrounds.Add(new StageBack
						{
							file = Str(dictionary3, "file"),
							origin = ReadVec(dictionary3, "origin"),
							size = ReadVec(dictionary3, "size"),
							x = Int(dictionary3, "x"),
							y = Int(dictionary3, "y"),
							rx = Int(dictionary3, "rx"),
							ry = Int(dictionary3, "ry"),
							cx = Int(dictionary3, "cx"),
							cy = Int(dictionary3, "cy"),
							type = Int(dictionary3, "type"),
							front = Int(dictionary3, "front"),
							alpha = Int(dictionary3, "a", 255),
							no = Int(dictionary3, "no")
						});
					}
				}
			}
			if (dictionary2.ContainsKey("footholds") && dictionary2["footholds"] is List<object> list2)
			{
				foreach (object item3 in list2)
				{
					if (item3 is List<object> { Count: >=4 } list3)
					{
						float mapX = ToF(list3[0]);
						float mapY = ToF(list3[1]);
						float mapX2 = ToF(list3[2]);
						float mapY2 = ToF(list3[3]);
						stageInfo.Footholds.Add(new Vector4(stageInfo.WorldX(mapX), stageInfo.WorldY(mapY), stageInfo.WorldX(mapX2), stageInfo.WorldY(mapY2)));
					}
				}
			}
			if (dictionary2.ContainsKey("ladderRope") && dictionary2["ladderRope"] is List<object> list4)
			{
				foreach (object item4 in list4)
				{
					if (item4 is Dictionary<string, object> d)
					{
						stageInfo.Climbs.Add(new ClimbLine
						{
							x = stageInfo.WorldX(Num(d, "x")),
							top = stageInfo.WorldY(Num(d, "y1")),
							bottom = stageInfo.WorldY(Num(d, "y2")),
							ladder = (Int(d, "l") != 0),
							upperExit = (Int(d, "uf") != 0)
						});
					}
				}
			}
			Dictionary<string, object> dictionary4 = (dictionary2.ContainsKey("files") ? (dictionary2["files"] as Dictionary<string, object>) : null);
			if (dictionary4 != null)
			{
				stageInfo.fileBack = Str(dictionary4, "back");
				stageInfo.fileMid = Str(dictionary4, "mid");
				stageInfo.fileFront = Str(dictionary4, "front");
			}
			Dictionary<string, object> dictionary5 = (dictionary2.ContainsKey("orders") ? (dictionary2["orders"] as Dictionary<string, object>) : null);
			if (dictionary5 != null)
			{
				stageInfo.orderBack = Int(dictionary5, "back", -800);
				stageInfo.orderMid = Int(dictionary5, "mid", -400);
				stageInfo.orderFront = Int(dictionary5, "front", 600);
			}
			FieldMap fieldMap = NativeMapView.LoadMap(new FieldMap
			{
				id = int.Parse(stageInfo.map)
			});
			stageInfo.Footholds.Clear();
			stageInfo.ForbidFallDown.Clear();
			stageInfo.FootholdForces.Clear();
			stageInfo.FootholdDrag.Clear();
			stageInfo.Climbs.Clear();
			foreach (FieldFoothold foothold in fieldMap.footholds)
			{
				stageInfo.Footholds.Add(new Vector4(stageInfo.WorldX(foothold.x1), stageInfo.WorldY(foothold.y1), stageInfo.WorldX(foothold.x2), stageInfo.WorldY(foothold.y2)));
				stageInfo.ForbidFallDown.Add(foothold.forbidFallDown != 0);
				stageInfo.FootholdForces.Add(foothold.force);
				stageInfo.FootholdDrag.Add(foothold.drag);
			}
			foreach (FieldRope rope in fieldMap.ropes)
			{
				stageInfo.Climbs.Add(new ClimbLine
				{
					x = stageInfo.WorldX(rope.x),
					top = stageInfo.WorldY(rope.y1),
					bottom = stageInfo.WorldY(rope.y2),
					ladder = rope.ladder,
					upperExit = rope.upperExit
				});
			}
			stageInfo.view = new Vector4((float)fieldMap.vr[0], (float)fieldMap.vr[1], (float)fieldMap.vr[2], (float)fieldMap.vr[3]);
			stageInfo.hasView = true;
			stages[item.Key] = stageInfo;
		}
	}

	private static float ReadNum(Dictionary<string, object> node, string key, int idx)
	{
		List<object> list = (node.ContainsKey(key) ? (node[key] as List<object>) : null);
		if (list == null || idx >= list.Count)
		{
			return 0f;
		}
		return ToF(list[idx]);
	}

	private static Color ToColor(int rgb)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		return new Color((float)((rgb >> 16) & 0xFF) / 255f, (float)((rgb >> 8) & 0xFF) / 255f, (float)(rgb & 0xFF) / 255f);
	}

	public static DmgSkin Damage(string skin)
	{
		Ensure();
		if (dmgSkins == null || string.IsNullOrEmpty(skin))
		{
			return null;
		}
		if (!dmgSkins.TryGetValue(skin, out var value))
		{
			return null;
		}
		return value;
	}

	private static void ReadDamage(Dictionary<string, object> root)
	{
		if (!root.ContainsKey("damage") || !(root["damage"] is Dictionary<string, object> dictionary))
		{
			return;
		}
		dmgSkins = new Dictionary<string, DmgSkin>();
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			if (item.Value is Dictionary<string, object> node)
			{
				DmgSkin dmgSkin = new DmgSkin
				{
					baseDigit = ReadGlyphs(node, "base")
				};
				if (dmgSkin.baseDigit != null)
				{
					dmgSkin.edgeDigit = ReadGlyphs(node, "edge");
					dmgSkin.miss = ReadGlyph(node, "miss");
					dmgSkin.fx = ReadGlyph(node, "fx");
					dmgSkins[item.Key] = dmgSkin;
				}
			}
		}
	}

	private static DmgGlyph[] ReadGlyphs(Dictionary<string, object> node, string key)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		List<object> list = (node.ContainsKey(key) ? (node[key] as List<object>) : null);
		if (list == null)
		{
			return null;
		}
		DmgGlyph[] array = new DmgGlyph[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] is List<object> { Count: >=5 } list2)
			{
				array[i] = new DmgGlyph
				{
					file = list2[0].ToString(),
					origin = new Vector2(ToF(list2[1]), ToF(list2[2])),
					size = new Vector2(ToF(list2[3]), ToF(list2[4]))
				};
			}
		}
		return array;
	}

	private static DmgGlyph ReadGlyph(Dictionary<string, object> node, string key)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		List<object> list = (node.ContainsKey(key) ? (node[key] as List<object>) : null);
		if (list == null || list.Count < 5)
		{
			return null;
		}
		return new DmgGlyph
		{
			file = list[0].ToString(),
			origin = new Vector2(ToF(list[1]), ToF(list[2])),
			size = new Vector2(ToF(list[3]), ToF(list[4]))
		};
	}

	public static Sprite FrameAt(string rel, Vector2 originPx)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		return Frame(rel, originPx, fromTop: true);
	}

	public static void Ensure()
	{
		if (tried)
		{
			return;
		}
		tried = true;
		TextAsset val = Resources.Load<TextAsset>("NativePresentation");
		if ((Object)(object)val == (Object)null)
		{
			throw new InvalidDataException("Missing NativePresentation configuration");
		}
		if (!(MiniJson.Parse(val.text) is Dictionary<string, object> root))
		{
			Debug.LogWarning((object)"[Art] manifest 解析失败");
			return;
		}
		ReadStages(root);
		ReadDamage(root);
		Ready = WzAssets.Index != null;
		if (Ready)
		{
			Debug.Log((object)"[Art] Native IMG resource library ready");
		}
	}

	private static Vector2 ReadVec(Dictionary<string, object> node, string key)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (!node.ContainsKey(key))
		{
			return Vector2.zero;
		}
		if (!(node[key] is List<object> { Count: >=2 } list))
		{
			return Vector2.zero;
		}
		return new Vector2(ToF(list[0]), ToF(list[1]));
	}

	private static float ToF(object o)
	{
		if (o == null)
		{
			return 0f;
		}
		if (o is bool)
		{
			if (!(bool)o)
			{
				return 0f;
			}
			return 1f;
		}
		if (o is double num)
		{
			return (float)num;
		}
		if (o is long num2)
		{
			return num2;
		}
		if (o is int num3)
		{
			return num3;
		}
		if (o is float)
		{
			return (float)o;
		}
		if (o is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		return 0f;
	}

	public static Sprite Frame(string rel)
	{
		Ensure();
		if (!string.IsNullOrEmpty(rel))
		{
			return WzAssets.Frame(rel);
		}
		return null;
	}

	public static Sprite FrameBottom(string rel)
	{
		return Frame(rel);
	}

	private static Sprite Frame(string rel, Vector2 pivotPx, bool fromTop)
	{
		return Frame(rel);
	}

	public static Clip GetClip(string kind, string key, string anim)
	{
		Ensure();
		return kind switch
		{
			"chars" => WzCharacter.Clip(key, anim), 
			"morph" => WzAssets.MorphClip(key, anim), 
			"mobs" => WzAssets.MobClip(key, anim), 
			_ => null, 
		};
	}

	public static bool Has(string kind, string key)
	{
		Ensure();
		if (!(kind == "chars"))
		{
			if (kind == "mobs")
			{
				int result;
				if (!((Dictionary<string, object>)WzAssets.Index["mobs"]).ContainsKey(key))
				{
					return int.TryParse(key, out result);
				}
				return true;
			}
			return false;
		}
		return WzCharacter.Has(key);
	}

	public static string Resolve(string kind, string key, string anim, string fallback)
	{
		if (GetClip(kind, key, anim) != null)
		{
			return anim;
		}
		string text = anim;
		if (anim == "walk")
		{
			text = "move";
		}
		else
		{
			switch (anim)
			{
			case "stand":
				text = "move";
				break;
			case "atk":
				text = "attack1";
				break;
			case "hit":
				text = "hit1";
				break;
			case "die":
				text = "die1";
				break;
			}
		}
		if (GetClip(kind, key, text) != null)
		{
			return text;
		}
		return fallback;
	}

	public static SpriteAnim Attach(Transform parent, string kind, string key, string idle, float targetHeight, string name = "Visual")
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected Obj, but got Unknown
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		((Renderer)val2).sortingOrder = 300;
		SpriteAnim spriteAnim = null;
		if (Ready && Has(kind, key))
		{
			spriteAnim = val.AddComponent<SpriteAnim>();
			spriteAnim.Init(kind, key, idle, val2);
			Clip clip = GetClip(kind, key, Resolve(kind, key, idle, idle));
			if (clip != null)
			{
				Sprite val3 = Frame(clip.frames[0]);
				if ((Object)(object)val3 != (Object)null && targetHeight > 0.01f)
				{
					Bounds bounds = val3.bounds;
					if (bounds.size.y > 0.01f)
					{
						bounds = val3.bounds;
						float num = targetHeight / bounds.size.y;
						val.transform.localScale = new Vector3(num, num, 1f);
					}
				}
			}
		}
		else
		{
			val2.sprite = SpriteFactory.Bottom();
			val.transform.localScale = new Vector3(0.8f, 1.3f, 1f);
		}
		return spriteAnim;
	}

	public static Texture2D Tex(string rel)
	{
		Ensure();
		if (!string.IsNullOrEmpty(rel))
		{
			return WzAssets.Texture(rel);
		}
		return null;
	}

	public static Vector2 Origin(string rel)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		ImgNode imgNode = (string.IsNullOrEmpty(rel) ? null : WzAssets.Node(rel));
		if (imgNode != null)
		{
			return WzAssets.Origin(imgNode);
		}
		return Vector2.zero;
	}

	public static string Str(Dictionary<string, object> d, string key, string def = "")
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return def;
		}
		return d[key].ToString();
	}

	public static int Int(Dictionary<string, object> d, string key, int def = 0)
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return def;
		}
		return Mathf.RoundToInt(ToF(d[key]));
	}

	public static float Num(Dictionary<string, object> d, string key, float def = 0f)
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return def;
		}
		return ToF(d[key]);
	}

	public static long Long(object value)
	{
		if (value != null)
		{
			if (value is long)
			{
				return (long)value;
			}
			if (!(value is int num))
			{
				if (!(value is short num2))
				{
					if (!(value is byte b))
					{
						if (!(value is double a))
						{
							if (!(value is float num3))
							{
								if (value is string s)
								{
									if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
									{
										return 0L;
									}
									return result;
								}
								return 0L;
							}
							return (long)Math.Round(num3);
						}
						return (long)Math.Round(a);
					}
					return b;
				}
				return num2;
			}
			return num;
		}
		return 0L;
	}

	public static int Id(Dictionary<string, object> d, string key, int def = 0)
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return def;
		}
		return (int)Long(d[key]);
	}

	public static Vector2 Size(Dictionary<string, object> d)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (d == null || !d.ContainsKey("size"))
		{
			return Vector2.one;
		}
		if (!(d["size"] is List<object> { Count: >=2 } list))
		{
			return Vector2.one;
		}
		return new Vector2(ToF(list[0]), ToF(list[1]));
	}
}
