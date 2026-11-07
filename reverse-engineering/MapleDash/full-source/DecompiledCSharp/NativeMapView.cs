using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using Server.Field;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class NativeMapView : MonoBehaviour
{
	private sealed class HiddenPortal
	{
		public int X;

		public int Y;

		public Animation Start;

		public Animation Continue;

		public Animation Exit;

		public bool Near;

		public float Changed;
	}

	private sealed class Animation
	{
		public SpriteRenderer Renderer;

		public Sprite[] Frames;

		public float[] Delays;

		public float Cycle;

		public void Tick(float time)
		{
			float num = time % Cycle;
			int num2 = 0;
			while (num2 < Delays.Length - 1 && num >= Delays[num2])
			{
				num -= Delays[num2++];
			}
			Renderer.sprite = Frames[num2];
		}
	}

	private sealed class Background
	{
		public ImgNode Definition;

		public Animation Art;

		public readonly List<SpriteRenderer> Cells = new List<SpriteRenderer>();
	}

	private sealed class MapDraw
	{
		public ImgNode Definition;

		public ImgNode Art;

		public long Z;

		public int Layer;

		public int Sequence;

		public bool Npc;
	}

	private static readonly (int mapId, int npcId, int x, int fh)[] ExtraNpcs = new (int, int, int, int)[11]
	{
		(910000000, 9000019, 700, 163),
		(980000203, 2042000, 0, 1),
		(980000204, 2042000, 0, 1),
		(980000303, 2042000, 0, 1),
		(980000304, 2042000, 0, 1),
		(980000403, 2042000, 0, 1),
		(980000404, 2042000, 0, 1),
		(980000503, 2042000, 0, 1),
		(980000504, 2042000, 0, 1),
		(980000603, 2042000, 0, 1),
		(980000604, 2042000, 0, 1)
	};

	private readonly HashSet<string> files = new HashSet<string>();

	private readonly HashSet<ImgNode> framesUsed = new HashSet<ImgNode>();

	private readonly List<Animation> animations = new List<Animation>();

	private readonly List<Background> backgrounds = new List<Background>();

	private readonly HashSet<SpriteRenderer> weatherSkyBacks = new HashSet<SpriteRenderer>();

	private readonly List<HiddenPortal> hiddenPortals = new List<HiddenPortal>();

	private NativeWeatherField weather;

	private readonly HashSet<string> reportedMissing = new HashSet<string>();

	private Transform mapBody;

	private ImgNode displayedMap;

	private readonly List<long> mapDepths = new List<long>();

	private readonly List<SpriteRenderer> mapRenderers = new List<SpriteRenderer>();

	private readonly Dictionary<MapNpcPosition, (FieldNpc model, GameObject view)> dynamicNpcs = new Dictionary<MapNpcPosition, (FieldNpc, GameObject)>();

	public int DrawCount { get; private set; }

	public int TextureCount => framesUsed.Count;

	public int FileCount => files.Count;

	public int MissingArt { get; private set; }

	private static string MapPath(int id)
	{
		return "Map/Map/Map" + id / 100000000 + "/" + id.ToString("D9") + ".img";
	}

	private static List<ImgNode> Children(ImgNode n)
	{
		if (n != null)
		{
			return n.Children;
		}
		return new List<ImgNode>();
	}

	public static ImgNode MapRoot(int id)
	{
		HashSet<int> hashSet = new HashSet<int>();
		while (hashSet.Add(id))
		{
			ImgNode imgNode = WzAssets.FileRoot(MapPath(id));
			ImgNode imgNode2 = imgNode.Get("info/link");
			if (imgNode2 == null)
			{
				return imgNode;
			}
			id = Convert.ToInt32(imgNode2.Value);
		}
		throw new InvalidDataException("Map link cycle: " + id);
	}

	public static float NpcGroundY(ImgNode map, ImgNode life)
	{
		return NpcGroundYAt(map, life.Int("x"), life.Int("fh"), life.Int("y"));
	}

	public static float NpcGroundYAt(ImgNode map, int x, int fh, float fallback)
	{
		if (fh > 0)
		{
			foreach (ImgNode item in Children(map.Get("foothold")))
			{
				foreach (ImgNode child in item.Children)
				{
					ImgNode imgNode = child.Get(fh.ToString());
					if (imgNode != null)
					{
						float num = imgNode.Int("x1");
						float num2 = imgNode.Int("x2");
						if (num != num2 && !((float)x < Math.Min(num, num2)) && !((float)x > Math.Max(num, num2)))
						{
							return (float)imgNode.Int("y1") + (float)(imgNode.Int("y2") - imgNode.Int("y1")) * ((float)x - num) / (num2 - num);
						}
					}
				}
			}
		}
		return fallback;
	}

	public static long NpcDepth(ImgNode map, int foothold)
	{
		foreach (ImgNode item in Children(map.Get("foothold")))
		{
			foreach (ImgNode child in item.Children)
			{
				if (child.Get(foothold.ToString()) != null)
				{
					return 10 * (3000L * (long)int.Parse(item.Name) - int.Parse(child.Name)) - 1073711829;
				}
			}
		}
		return -1073711829L;
	}

	public static FieldMap LoadMap(FieldMap previous)
	{
		ImgNode imgNode = MapRoot(previous.id);
		ImgNode imgNode2 = WzAssets.FileRoot(MapPath(previous.id)).Get("info");
		if (imgNode2 == null)
		{
			throw new InvalidDataException("Missing map info " + previous.id);
		}
		FieldMap fieldMap = new FieldMap
		{
			id = previous.id,
			name = previous.name,
			street = previous.street,
			town = (imgNode2.Int("town") != 0),
			returnMap = imgNode2.Int("returnMap"),
			fieldLimit = imgNode2.Int("fieldLimit"),
			fieldType = imgNode2.Int("fieldType"),
			bgm = imgNode2.Text("bgm"),
			createMobInterval = imgNode2.Int("createMobInterval", 5000),
			onUserEnter = imgNode2.Text("onUserEnter"),
			onFirstUserEnter = imgNode2.Text("onFirstUserEnter"),
			decHp = imgNode2.Int("decHP"),
			protectItem = imgNode2.Int("protectItem"),
			everlast = (imgNode2.Int("everlast") != 0),
			forcedReturn = imgNode2.Int("forcedReturn", 999999999),
			timeLimit = imgNode2.Int("timeLimit", -1),
			timeMobId = (imgNode2.Get("timeMob")?.Int("id", -1) ?? (-1)),
			timeMobMessage = imgNode2.Get("timeMob")?.Text("message"),
			mobRate = ((imgNode2.Get("mobRate")?.Value == null) ? 1f : Convert.ToSingle(imgNode2.Get("mobRate").Value))
		};
		foreach (ImgNode item in Children(imgNode.Get("foothold")))
		{
			foreach (ImgNode child in item.Children)
			{
				foreach (ImgNode child2 in child.Children)
				{
					fieldMap.footholds.Add(new FieldFoothold
					{
						id = int.Parse(child2.Name),
						layer = int.Parse(item.Name),
						group = int.Parse(child.Name),
						x1 = child2.Int("x1"),
						y1 = child2.Int("y1"),
						x2 = child2.Int("x2"),
						y2 = child2.Int("y2"),
						forbidFallDown = child2.Int("forbidFallDown"),
						force = child2.Int("force"),
						drag = child2.Int("drag")
					});
				}
			}
		}
		foreach (ImgNode item2 in Children(imgNode.Get("ladderRope")))
		{
			fieldMap.ropes.Add(new FieldRope
			{
				x = item2.Int("x"),
				y1 = item2.Int("y1"),
				y2 = item2.Int("y2"),
				ladder = (item2.Int("l") != 0),
				upperExit = (item2.Int("uf") != 0)
			});
		}
		foreach (ImgNode item3 in Children(imgNode.Get("portal")))
		{
			fieldMap.portals.Add(new FieldPortal
			{
				id = (int.TryParse(item3.Name, out var result) ? result : fieldMap.portals.Count),
				name = item3.Text("pn"),
				targetName = item3.Text("tn"),
				type = item3.Int("pt"),
				tm = item3.Int("tm", 999999999),
				x = item3.Int("x"),
				y = item3.Int("y"),
				script = item3.Text("script")
			});
		}
		foreach (ImgNode item4 in Children(imgNode.Get("life")))
		{
			if (item4.Text("type") == "m")
			{
				fieldMap.spawns.Add(new FieldSpawn
				{
					mob = int.Parse(item4.Text("id")),
					mobTime = item4.Int("mobTime"),
					fh = item4.Int("fh"),
					team = item4.Int("team", -1),
					dir = item4.Int("f"),
					x = item4.Int("x"),
					y = item4.Int("y"),
					cy = item4.Int("cy"),
					rx0 = item4.Int("rx0"),
					rx1 = item4.Int("rx1")
				});
			}
			else if (item4.Text("type") == "n")
			{
				fieldMap.npcs.Add(new FieldNpc
				{
					id = int.Parse(item4.Text("id")),
					hide = item4.Int("hide"),
					x = item4.Int("x"),
					y = NpcGroundY(imgNode, item4),
					head = NpcHeadHeight(int.Parse(item4.Text("id")))
				});
			}
		}
		(int, int, int, int)[] extraNpcs = ExtraNpcs;
		for (int i = 0; i < extraNpcs.Length; i++)
		{
			(int, int, int, int) tuple = extraNpcs[i];
			if (tuple.Item1 == previous.id)
			{
				fieldMap.npcs.Add(new FieldNpc
				{
					id = tuple.Item2,
					x = tuple.Item3,
					y = NpcGroundYAt(imgNode, tuple.Item3, tuple.Item4, 23f),
					head = NpcHeadHeight(tuple.Item2)
				});
			}
		}
		MapRuntime runtime = MapService.Get(previous.id);
		if (runtime != null && runtime.removedNpcs.Count > 0)
		{
			fieldMap.npcs.RemoveAll((FieldNpc npc) => runtime.removedNpcs.Contains(npc.id));
		}
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		foreach (FieldFoothold foothold in fieldMap.footholds)
		{
			num = Math.Min(num, (int)Math.Min(foothold.x1, foothold.x2));
			num3 = Math.Max(num3, (int)Math.Max(foothold.x1, foothold.x2));
			num2 = Math.Min(num2, (int)Math.Min(foothold.y1, foothold.y2));
			num4 = Math.Max(num4, (int)Math.Max(foothold.y1, foothold.y2));
		}
		if (fieldMap.footholds.Count == 0)
		{
			num = -400;
			num3 = 400;
			num2 = 0;
			num4 = 300;
		}
		ImgNode imgNode3 = imgNode.Get("info");
		fieldMap.vr = new int[4]
		{
			imgNode3.Int("VRLeft", num),
			imgNode3.Int("VRTop", num2 - 300),
			imgNode3.Int("VRRight", num3),
			imgNode3.Int("VRBottom", num4 + 100)
		};
		return fieldMap;
	}

	private ImgNode FileRoot(string relative)
	{
		files.Add(relative);
		return WzAssets.FileRoot(relative);
	}

	private Sprite Sprite(ImgNode frame)
	{
		framesUsed.Add(frame);
		return WzAssets.Sprite(frame);
	}

	private Animation Draw(ImgNode node, string name, float x, float y, int order, bool flip = false, int alpha = 255, int defaultDelay = 100)
	{
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		if (node == null)
		{
			throw new InvalidDataException("Missing IMG artwork " + name);
		}
		node = node.Resolve();
		List<ImgNode> list = new List<ImgNode>();
		if (node.Kind == "Canvas")
		{
			list.Add(node);
		}
		else
		{
			List<ImgNode> list2 = new List<ImgNode>();
			foreach (ImgNode child in node.Children)
			{
				if (int.TryParse(child.Name, out var _))
				{
					list2.Add(child);
				}
			}
			list2.Sort((ImgNode a, ImgNode b) => int.Parse(a.Name).CompareTo(int.Parse(b.Name)));
			foreach (ImgNode item in list2)
			{
				ImgNode imgNode = item.TryResolve();
				if (imgNode != null && imgNode.Kind == "Canvas")
				{
					list.Add(imgNode);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		GameObject val = new GameObject(name);
		val.transform.SetParent(((Component)this).transform, false);
		val.transform.localPosition = new Vector3(x / 40f, (0f - y) / 40f, 0f);
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		((Renderer)val2).sortingOrder = order;
		val2.flipX = flip;
		val2.color = new Color(1f, 1f, 1f, (float)alpha / 255f);
		Animation animation = new Animation
		{
			Renderer = val2,
			Frames = new Sprite[list.Count],
			Delays = new float[list.Count]
		};
		for (int num = 0; num < list.Count; num++)
		{
			animation.Frames[num] = Sprite(list[num]);
			animation.Delays[num] = (float)Math.Max(1, Math.Abs(list[num].Int("delay", defaultDelay))) / 1000f;
			animation.Cycle += animation.Delays[num];
		}
		val2.sprite = animation.Frames[0];
		if (list.Count > 1)
		{
			animations.Add(animation);
		}
		DrawCount++;
		return animation;
	}

	private static bool Drawable(ImgNode node)
	{
		ImgNode imgNode = node?.TryResolve();
		if (imgNode == null)
		{
			return false;
		}
		if (imgNode.Kind == "Canvas")
		{
			return true;
		}
		foreach (ImgNode child in imgNode.Children)
		{
			if (int.TryParse(child.Name, out var _) && child.TryResolve()?.Kind == "Canvas")
			{
				return true;
			}
		}
		return false;
	}

	private void Missing(ImgNode owner, int mapId, string kind, string path, string label)
	{
		MissingArt++;
		if (reportedMissing.Add(kind + "/" + path))
		{
			Debug.LogWarning((object)("[NativeMapView] 地图 " + mapId + " 缺少 " + kind + " 素材 " + path + "（节点 " + owner.Name + "/" + label + "），按原端行为跳过"));
		}
	}

	private static ImgNode NpcRoot(string npcId)
	{
		string relative = "Npc/" + npcId + ".img";
		for (int i = 0; i < 16; i++)
		{
			if (!WzAssets.HasFile(relative))
			{
				return null;
			}
			ImgNode imgNode = WzAssets.FileRoot(relative);
			ImgNode imgNode2 = imgNode.Get("info/link");
			if (imgNode2 == null)
			{
				return imgNode;
			}
			relative = "Npc/" + Convert.ToInt32(imgNode2.Value).ToString("D7") + ".img";
		}
		return null;
	}

	private static float NpcHeadHeight(int npcId)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ImgNode imgNode = NpcRoot(npcId.ToString("D7"));
			List<ImgNode> list = ((imgNode == null) ? null : WzAssets.Frames(imgNode.Get("stand")));
			if (list == null || list.Count == 0)
			{
				return 80f;
			}
			float y = WzAssets.Origin(list[0]).y;
			return (y > 4f) ? y : ((float)WzAssets.Canvas(list[0]).Height);
		}
		catch (Exception)
		{
			return 80f;
		}
	}

	private void AddMapRenderer(SpriteRenderer renderer, long depth)
	{
		int num = mapDepths.Count;
		while (num > 0 && mapDepths[num - 1] > depth)
		{
			num--;
		}
		mapDepths.Insert(num, depth);
		mapRenderers.Insert(num, renderer);
		((Component)renderer).transform.SetParent(mapBody, false);
		for (int i = num; i < mapRenderers.Count; i++)
		{
			((Renderer)mapRenderers[i]).sortingOrder = i;
		}
	}

	public void SyncDynamicNpcs(FieldMap field)
	{
		MapRuntime mapRuntime = MapService.Get(NativeEventScripts.ResolveMap(field.id)?.Scope ?? field.id);
		if (mapRuntime == null)
		{
			return;
		}
		List<MapNpcPosition> list = new List<MapNpcPosition>();
		foreach (KeyValuePair<MapNpcPosition, (FieldNpc, GameObject)> dynamicNpc in dynamicNpcs)
		{
			if (!mapRuntime.npcs.Contains(dynamicNpc.Key) || mapRuntime.removedNpcs.Contains(dynamicNpc.Key.id))
			{
				if ((Object)(object)dynamicNpc.Value.Item2 != (Object)null)
				{
					Object.Destroy((Object)(object)dynamicNpc.Value.Item2);
				}
				field.npcs.Remove(dynamicNpc.Value.Item1);
				list.Add(dynamicNpc.Key);
			}
		}
		foreach (MapNpcPosition item2 in list)
		{
			dynamicNpcs.Remove(item2);
		}
		foreach (MapNpcPosition npc in mapRuntime.npcs)
		{
			if (!npc.dynamic || dynamicNpcs.ContainsKey(npc))
			{
				continue;
			}
			string text = npc.id.ToString("D7");
			ImgNode imgNode = NpcRoot(text);
			if (!Drawable(imgNode?.Get("stand")))
			{
				continue;
			}
			int foothold = 0;
			float num = float.PositiveInfinity;
			foreach (FieldFoothold foothold2 in field.footholds)
			{
				if (foothold2.x1 != foothold2.x2 && !((float)npc.x < Math.Min(foothold2.x1, foothold2.x2)) && !((float)npc.x > Math.Max(foothold2.x1, foothold2.x2)))
				{
					float num2 = foothold2.y1 + (foothold2.y2 - foothold2.y1) * ((float)npc.x - foothold2.x1) / (foothold2.x2 - foothold2.x1);
					if (num2 >= (float)npc.y && num2 < num)
					{
						num = num2;
						foothold = foothold2.id;
					}
				}
			}
			Animation animation = Draw(imgNode.Get("stand"), "Npc_" + text, npc.x, npc.y, 0);
			if (animation != null)
			{
				AddMapRenderer(animation.Renderer, NpcDepth(displayedMap, foothold));
			}
			FieldNpc fieldNpc = new FieldNpc
			{
				id = npc.id,
				x = npc.x,
				y = npc.y,
				head = NpcHeadHeight(npc.id)
			};
			field.npcs.Add(fieldNpc);
			Dictionary<MapNpcPosition, (FieldNpc model, GameObject view)> dictionary = dynamicNpcs;
			object item;
			if (animation == null)
			{
				item = null;
			}
			else
			{
				SpriteRenderer renderer = animation.Renderer;
				item = ((renderer != null) ? ((Component)renderer).gameObject : null);
			}
			dictionary.Add(npc, (fieldNpc, (GameObject)item));
		}
	}

	public void RemoveNpcView(int npcId)
	{
		if (!((Object)(object)mapBody == (Object)null))
		{
			Transform val = mapBody.Find("Npc_" + npcId.ToString("D7"));
			if ((Object)(object)val != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)val).gameObject);
			}
		}
	}

	public void Show(int id)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected Obj, but got Unknown
		((Component)this).gameObject.AddComponent<NativeReactorView>().Bind(NativeEventScripts.ResolveMap(id)?.Scope ?? id);
		ImgNode imgNode = (displayedMap = MapRoot(id));
		GameObject val = new GameObject("MapLayers");
		val.transform.SetParent(((Component)this).transform, false);
		val.AddComponent<SortingGroup>().sortingOrder = -400;
		mapBody = val.transform;
		List<MapDraw> list = new List<MapDraw>();
		for (int i = 0; i < 9; i++)
		{
			ImgNode imgNode2 = imgNode.Get(i.ToString());
			if (imgNode2 == null)
			{
				continue;
			}
			string text = imgNode2.Text("info/tS");
			string relative = "Map/Tile/" + text + ".img";
			bool flag = !string.IsNullOrEmpty(text) && WzAssets.HasFile(relative);
			foreach (ImgNode item in Children(imgNode2.Get("tile")))
			{
				ImgNode imgNode3 = (flag ? FileRoot(relative).Get(item.Text("u") + "/" + item.Int("no")) : null);
				if (!Drawable(imgNode3))
				{
					Missing(imgNode2, id, "tile", text + "/" + item.Text("u") + "/" + item.Int("no"), item.Name);
				}
				else
				{
					list.Add(new MapDraw
					{
						Definition = item,
						Art = imgNode3,
						Layer = i,
						Z = imgNode3.Resolve().Int("z") + 10 * (3000L * (long)i - item.Int("zM")) - 1073721834,
						Sequence = list.Count
					});
				}
			}
			foreach (ImgNode item2 in Children(imgNode2.Get("obj")))
			{
				string text2 = item2.Text("oS");
				string relative2 = "Map/Obj/" + text2 + ".img";
				ImgNode imgNode4 = (WzAssets.HasFile(relative2) ? FileRoot(relative2).Get(item2.Text("l0") + "/" + item2.Text("l1") + "/" + item2.Text("l2")) : null);
				if (!Drawable(imgNode4))
				{
					Missing(imgNode2, id, "obj", text2 + "/" + item2.Text("l0") + "/" + item2.Text("l1") + "/" + item2.Text("l2"), item2.Name);
				}
				else
				{
					list.Add(new MapDraw
					{
						Definition = item2,
						Art = imgNode4,
						Layer = i,
						Z = item2.Int("z") + 30000L * (long)i - 1073739824,
						Sequence = list.Count
					});
				}
			}
		}
		foreach (ImgNode item3 in Children(imgNode.Get("life")))
		{
			if (!(item3.Text("type") != "n") && item3.Int("hide") == 0)
			{
				string text3 = int.Parse(item3.Text("id")).ToString("D7");
				ImgNode imgNode5 = NpcRoot(text3);
				if (!Drawable(imgNode5?.Get("stand")))
				{
					Missing(item3, id, "npc", text3 + "/stand", item3.Name);
					continue;
				}
				list.Add(new MapDraw
				{
					Definition = item3,
					Art = imgNode5.Get("stand"),
					Npc = true,
					Z = NpcDepth(imgNode, item3.Int("fh")),
					Sequence = list.Count
				});
			}
		}
		list.Sort((MapDraw a, MapDraw b) =>
		{
			int num3 = a.Z.CompareTo(b.Z);
			return (num3 != 0) ? num3 : a.Sequence.CompareTo(b.Sequence);
		});
		for (int num = 0; num < list.Count; num++)
		{
			MapDraw mapDraw = list[num];
			ImgNode definition = mapDraw.Definition;
			Animation animation = Draw(mapDraw.Art, mapDraw.Npc ? ("Npc_" + definition.Text("id")) : (definition.Parent.Name + "_" + mapDraw.Layer + "_" + definition.Name), definition.Int("x"), mapDraw.Npc ? NpcGroundY(imgNode, definition) : ((float)definition.Int("y")), num, definition.Int("f") != 0);
			if (animation != null)
			{
				AddMapRenderer(animation.Renderer, mapDraw.Z);
			}
		}
		foreach (ImgNode item4 in Children(imgNode.Get("back")))
		{
			if (item4.Text("bS") == "")
			{
				continue;
			}
			string relative3 = "Map/Back/" + item4.Text("bS") + ".img";
			string text4 = ((item4.Int("ani") != 0) ? "ani" : "back");
			ImgNode node = (WzAssets.HasFile(relative3) ? FileRoot(relative3) : null)?.Get(text4 + "/" + item4.Int("no"));
			if (!Drawable(node))
			{
				Missing(item4, id, "back", item4.Text("bS") + "/" + text4 + "/" + item4.Int("no"), item4.Name);
				continue;
			}
			Animation animation2 = Draw(node, "Back_" + item4.Name, 0f, 0f, ((item4.Int("front") != 0) ? 600 : (-800)) + int.Parse(item4.Name), item4.Int("f") != 0, item4.Int("a", 255));
			if (animation2 != null)
			{
				((Renderer)animation2.Renderer).enabled = false;
				backgrounds.Add(new Background
				{
					Definition = item4,
					Art = animation2
				});
			}
		}
		foreach (ImgNode item5 in Children(imgNode.Get("portal")))
		{
			if (item5.Int("pt") == 2 || item5.Int("pt") == 4 || item5.Int("pt") == 7)
			{
				Draw(FileRoot("Map/MapHelper.img").Get("portal/game/pv"), "Portal_" + item5.Name, item5.Int("x"), item5.Int("y"), 200, flip: false, 255, 120);
			}
		}
		foreach (ImgNode item6 in Children(imgNode.Get("portal")))
		{
			if (item6.Int("pt") == 10)
			{
				string text5 = item6.Text("image");
				ImgNode imgNode6 = FileRoot("Map/MapHelper.img").Get("portal/game/ph/" + ((text5 == "") ? "default" : text5));
				HiddenPortal hiddenPortal = new HiddenPortal
				{
					X = item6.Int("x"),
					Y = item6.Int("y"),
					Changed = float.NegativeInfinity
				};
				hiddenPortal.Start = Draw(imgNode6.Get("portalStart"), "Portal_" + item6.Name + "_start", hiddenPortal.X, hiddenPortal.Y, 200);
				hiddenPortal.Continue = Draw(imgNode6.Get("portalContinue"), "Portal_" + item6.Name, hiddenPortal.X, hiddenPortal.Y, 200);
				hiddenPortal.Exit = Draw(imgNode6.Get("portalExit"), "Portal_" + item6.Name + "_exit", hiddenPortal.X, hiddenPortal.Y, 200);
				Animation[] array = new Animation[3] { hiddenPortal.Start, hiddenPortal.Continue, hiddenPortal.Exit };
				foreach (Animation animation3 in array)
				{
					animations.Remove(animation3);
					((Renderer)animation3.Renderer).enabled = false;
				}
				hiddenPortals.Add(hiddenPortal);
			}
		}
		weather = ((Component)this).gameObject.AddComponent<NativeWeatherField>();
		weather.Bind(this, id, imgNode);
		Debug.Log((object)("[DirectIMG] Map=" + id + " files=" + FileCount + " draws=" + DrawCount + " textures=" + TextureCount + " animations=" + animations.Count + " missingArt=" + MissingArt));
	}

	private void LateUpdate()
	{
		foreach (Animation animation in animations)
		{
			animation.Tick(Time.timeSinceLevelLoad);
		}
		TickHiddenPortals();
		weather?.Tick();
		if ((Object)(object)Camera.main != (Object)null)
		{
			UpdateBackgrounds(Camera.main, Time.timeSinceLevelLoad);
		}
	}

	private void TickHiddenPortals()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		PlayerCtrl playerCtrl = GameLoop.I?.player;
		HiddenPortal hiddenPortal = null;
		if ((Object)(object)playerCtrl != (Object)null)
		{
			Vector2 feetPos = playerCtrl.FeetPos;
			for (int num = hiddenPortals.Count - 1; num >= 0; num--)
			{
				if (feetPos.x * 40f >= (float)(hiddenPortals[num].X - 100) && feetPos.x * 40f < (float)(hiddenPortals[num].X + 100) && (0f - feetPos.y) * 40f >= (float)(hiddenPortals[num].Y - 50) && (0f - feetPos.y) * 40f < (float)(hiddenPortals[num].Y + 50))
				{
					hiddenPortal = hiddenPortals[num];
					break;
				}
			}
		}
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		foreach (HiddenPortal hiddenPortal2 in hiddenPortals)
		{
			bool flag = hiddenPortal2 == hiddenPortal;
			if (flag != hiddenPortal2.Near)
			{
				hiddenPortal2.Near = flag;
				hiddenPortal2.Changed = timeSinceLevelLoad;
			}
			float num2 = timeSinceLevelLoad - hiddenPortal2.Changed;
			((Renderer)hiddenPortal2.Start.Renderer).enabled = flag && num2 < hiddenPortal2.Start.Cycle;
			((Renderer)hiddenPortal2.Continue.Renderer).enabled = flag && num2 >= hiddenPortal2.Start.Cycle;
			((Renderer)hiddenPortal2.Exit.Renderer).enabled = !flag && num2 < hiddenPortal2.Exit.Cycle;
			if (((Renderer)hiddenPortal2.Start.Renderer).enabled)
			{
				hiddenPortal2.Start.Tick(num2);
			}
			if (((Renderer)hiddenPortal2.Continue.Renderer).enabled)
			{
				hiddenPortal2.Continue.Tick(num2 - hiddenPortal2.Start.Cycle);
			}
			if (((Renderer)hiddenPortal2.Exit.Renderer).enabled)
			{
				hiddenPortal2.Exit.Tick(num2);
			}
		}
	}

	public IEnumerable<SpriteRenderer> WeatherTintBacks()
	{
		foreach (Background background in backgrounds)
		{
			if (background.Definition.Name != "weather" && background.Definition.Int("front") == 0)
			{
				yield return background.Art.Renderer;
			}
		}
	}

	public SpriteRenderer AddWeatherBack(Dictionary<string, object> row, int order)
	{
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		string text = "Map/Back/" + (string)row["bank"] + ".img";
		ImgNode imgNode = FileRoot(text).Get(((Convert.ToInt32(row["ani"]) != 0) ? "ani/" : "back/") + Convert.ToInt32(row["no"]));
		if (!Drawable(imgNode))
		{
			throw new InvalidDataException("Missing original weather art " + text);
		}
		ImgNode imgNode2 = new ImgNode
		{
			Name = "weather",
			Kind = "Property"
		};
		foreach (KeyValuePair<string, object> item in row)
		{
			imgNode2.Children.Add(new ImgNode
			{
				Name = item.Key,
				Kind = "Int",
				Value = item.Value,
				Parent = imgNode2
			});
		}
		if ((bool)row["sizePitch"])
		{
			ImgNode imgNode3 = WzAssets.Canvas((imgNode.Resolve().Kind == "Canvas") ? imgNode : imgNode.Get("0"));
			imgNode2.Get("cx").Value = imgNode3.Width;
			imgNode2.Get("cy").Value = imgNode3.Height;
		}
		Animation animation = Draw(imgNode, "Weather_" + row["group"], 0f, 0f, order);
		if (animation == null)
		{
			return null;
		}
		((Renderer)animation.Renderer).enabled = false;
		animation.Renderer.color = new Color(1f, 1f, 1f, 0f);
		backgrounds.Add(new Background
		{
			Definition = imgNode2,
			Art = animation
		});
		return animation.Renderer;
	}

	public void PromoteWeatherBacks(ImgNode map)
	{
		ImgNode imgNode = map.Get("back/0");
		foreach (Background background in backgrounds)
		{
			ImgNode definition = background.Definition;
			if (!(definition.Name == "weather") && definition.Int("front") == 0)
			{
				int num = int.Parse(definition.Name);
				bool flag = string.Equals(definition.Text("bS"), imgNode.Text("bS"), StringComparison.OrdinalIgnoreCase) && definition.Int("no") == imgNode.Int("no");
				if ((num < 2) | flag)
				{
					((Renderer)background.Art.Renderer).sortingOrder = -1200 + num;
					weatherSkyBacks.Add(background.Art.Renderer);
				}
			}
		}
	}

	public void SetWeatherSkyVisibility(bool hidden)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		foreach (SpriteRenderer weatherSkyBack in weatherSkyBacks)
		{
			if ((Object)(object)weatherSkyBack != (Object)null)
			{
				Color color = weatherSkyBack.color;
				color.a = ((!hidden) ? 1 : 0);
				weatherSkyBack.color = color;
			}
		}
	}

	public void UpdateBackgrounds(Camera camera, float elapsed)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Expected Obj, but got Unknown
		float num = (((Component)camera).transform.position.x - ((Component)this).transform.position.x) * 40f;
		float num2 = (0f - (((Component)camera).transform.position.y - ((Component)this).transform.position.y)) * 40f;
		float num3 = camera.orthographicSize * 40f;
		float num4 = num3 * camera.aspect;
		foreach (Background background in backgrounds)
		{
			ImgNode definition = background.Definition;
			SpriteRenderer renderer = background.Art.Renderer;
			int num5 = definition.Int("type");
			bool flag = num5 == 4 || num5 == 6;
			bool flag2 = num5 == 5 || num5 == 7;
			bool num6 = num5 == 1 || num5 == 3 || num5 == 4 || num5 == 6 || num5 == 7;
			bool flag3 = num5 == 2 || num5 == 3 || num5 == 5 || num5 == 6 || num5 == 7;
			Sprite sprite = renderer.sprite;
			Rect rect = sprite.rect;
			float width = rect.width;
			rect = sprite.rect;
			float height = rect.height;
			float num7 = (float)definition.Int("x") + num * (1f + (flag ? 0f : ((float)definition.Int("rx") / 100f)));
			float num8 = (float)definition.Int("y") + num2 * (1f + (flag2 ? 0f : ((float)definition.Int("ry") / 100f)));
			if (flag)
			{
				num7 -= elapsed * 5f * (float)definition.Int("rx");
			}
			if (flag2)
			{
				num8 -= elapsed * 5f * (float)definition.Int("ry");
			}
			float num9 = ((definition.Int("cx") > 0) ? ((float)definition.Int("cx")) : width);
			float num10 = ((definition.Int("cy") > 0) ? ((float)definition.Int("cy")) : height);
			float num11 = num7 - (renderer.flipX ? (width - sprite.pivot.x) : sprite.pivot.x);
			float num12 = num8 - (height - sprite.pivot.y);
			int num13 = (num6 ? (Mathf.FloorToInt((num - num4 - num11 - width) / num9) + 1) : 0);
			int num14 = (num6 ? (Mathf.CeilToInt((num + num4 - num11) / num9) - 1) : 0);
			int num15 = (flag3 ? (Mathf.FloorToInt((num2 - num3 - num12 - height) / num10) + 1) : 0);
			int num16 = (flag3 ? (Mathf.CeilToInt((num2 + num3 - num12) / num10) - 1) : 0);
			int num17 = 0;
			for (int i = num15; i <= num16; i++)
			{
				for (int j = num13; j <= num14; j++)
				{
					if (num17 == background.Cells.Count)
					{
						GameObject val = new GameObject("BackCell");
						val.transform.SetParent(((Component)this).transform, false);
						background.Cells.Add(val.AddComponent<SpriteRenderer>());
					}
					SpriteRenderer val2 = background.Cells[num17++];
					((Renderer)val2).enabled = true;
					val2.sprite = sprite;
					((Renderer)val2).sortingOrder = ((Renderer)renderer).sortingOrder;
					val2.flipX = renderer.flipX;
					val2.color = renderer.color;
					((Component)val2).transform.localPosition = new Vector3((num7 + (float)j * num9) / 40f, (0f - (num8 + (float)i * num10)) / 40f, 0f);
				}
			}
			for (int k = num17; k < background.Cells.Count; k++)
			{
				((Renderer)background.Cells[k]).enabled = false;
			}
		}
	}
}
