using System;
using System.Collections.Generic;
using System.IO;
using MapleDash.Wz;
using UnityEngine;

public static class WzAssets
{
	private static Dictionary<string, object> index;

	private static HashSet<string> inventory;

	private static readonly HashSet<string> embeddedFallbacks = new HashSet<string>();

	private static readonly Dictionary<string, ImgNode> roots = new Dictionary<string, ImgNode>();

	private static readonly Dictionary<ImgNode, byte[]> pixels = new Dictionary<ImgNode, byte[]>();

	private static readonly Dictionary<ImgNode, Texture2D> textures = new Dictionary<ImgNode, Texture2D>();

	private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, Texture2D> aliases = new Dictionary<string, Texture2D>();

	private static readonly Dictionary<string, Art.Clip> mobClips = new Dictionary<string, Art.Clip>();

	private static readonly HashSet<string> missing = new HashSet<string>();

	public static string DataRoot
	{
		get
		{
			string environmentVariable = Environment.GetEnvironmentVariable("MAPLEDASH_IMG_ROOT");
			if (!string.IsNullOrEmpty(environmentVariable))
			{
				return environmentVariable;
			}
			return GamePlatform.DataRoot;
		}
	}

	public static int EmbeddedFallbackCount => embeddedFallbacks.Count;

	public static Dictionary<string, object> Index
	{
		get
		{
			if (index == null)
			{
				TextAsset val = Resources.Load<TextAsset>("NativeAssetIndex");
				if ((Object)(object)val == (Object)null)
				{
					throw new FileNotFoundException("NativeAssetIndex missing; run tools/package_native_assets.py");
				}
				string src = val.text;
				string path = Path.Combine(DataRoot, ".resource-manifest.json");
				if (File.Exists(path))
				{
					src = File.ReadAllText(path);
				}
				index = (Dictionary<string, object>)MiniJson.Parse(src);
			}
			return index;
		}
	}

	public static int MissingCount => missing.Count;

	public static int FileCount => roots.Count;

	public static int TextureCount => textures.Count;

	public static bool HasFile(string relative)
	{
		return File.Exists(Path.Combine(DataRoot, relative));
	}

	public static string MapPath(int id)
	{
		return "Map/Map/Map" + id / 100000000 + "/" + id.ToString("D9") + ".img";
	}

	public static ImgNode FileRoot(string relative)
	{
		if (roots.TryGetValue(relative, out var value))
		{
			return value;
		}
		if (relative.Contains("..") || Path.IsPathRooted(relative))
		{
			throw new InvalidDataException("Invalid IMG path " + relative);
		}
		using (ImgFile imgFile = new ImgFile(Path.Combine(DataRoot, relative)))
		{
			value = imgFile.Root;
		}
		roots.Add(relative, value);
		return value;
	}

	public static ImgNode Node(string source)
	{
		if (source.StartsWith("wz://", StringComparison.Ordinal))
		{
			source = source.Substring(5);
		}
		int num = source.IndexOf(".img/", StringComparison.Ordinal);
		if (num < 0)
		{
			for (int num2 = source.IndexOf('/'); num2 >= 0; num2 = source.IndexOf('/', num2 + 1))
			{
				string relative = source.Substring(0, num2) + ".img";
				if (HasFile(relative))
				{
					return FileRoot(relative).Get(source.Substring(num2 + 1))?.Resolve();
				}
			}
			return FileRoot(source);
		}
		return FileRoot(source.Substring(0, num + 4)).Get(source.Substring(num + 5))?.Resolve();
	}

	public static ImgNode Canvas(ImgNode frame)
	{
		ImgNode imgNode = frame.Resolve();
		for (int i = 0; i < 64; i++)
		{
			string text = imgNode.Text("_inlink");
			if (text != "")
			{
				ImgNode imgNode2 = imgNode;
				while (imgNode2.Parent != null)
				{
					imgNode2 = imgNode2.Parent;
				}
				imgNode = imgNode2.Get(text)?.Resolve();
			}
			else
			{
				if (!((text = imgNode.Text("_outlink")) != ""))
				{
					if (imgNode.Kind != "Canvas")
					{
						throw new InvalidDataException("Expected Canvas: " + imgNode.Name);
					}
					return imgNode;
				}
				int num = text.IndexOf(".img/", StringComparison.Ordinal);
				string text2 = ((num < 0) ? text : text.Substring(0, num + 4));
				if (!HasFile(text2) && imgNode.Kind == "Canvas")
				{
					if (!pixels.ContainsKey(imgNode))
					{
						pixels.Add(imgNode, imgNode.Rgba());
					}
					if (embeddedFallbacks.Add(text2))
					{
						Debug.LogWarning((object)("[WzAssets] Missing outlink file " + text2 + "; using verified embedded Canvas pixels"));
					}
					return imgNode;
				}
				imgNode = Node(text);
			}
			if (imgNode == null)
			{
				throw new InvalidDataException("Missing Canvas link " + text);
			}
		}
		throw new InvalidDataException("Canvas link cycle: " + frame.Name);
	}

	public static byte[] Pixels(ImgNode frame)
	{
		ImgNode imgNode = Canvas(frame);
		if (!pixels.TryGetValue(imgNode, out var value))
		{
			value = imgNode.Rgba();
			pixels.Add(imgNode, value);
		}
		return value;
	}

	public static Texture2D Texture(ImgNode frame)
	{
		ImgNode imgNode = Canvas(frame);
		if (textures.TryGetValue(imgNode, out var value))
		{
			return value;
		}
		value = Upload(Pixels(imgNode), imgNode.Width, imgNode.Height, frame.Name);
		textures.Add(imgNode, value);
		return value;
	}

	public static Texture2D Upload(byte[] rgba, int width, int height, string name)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected Obj, but got Unknown
		int num = checked(width * 4);
		byte[] array = new byte[rgba.Length];
		for (int i = 0; i < height; i++)
		{
			Buffer.BlockCopy(rgba, i * num, array, (height - 1 - i) * num, num);
		}
		Texture2D val = new Texture2D(width, height, (TextureFormat)4, false)
		{
			name = name,
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		val.LoadRawTextureData(array);
		val.Apply();
		return val;
	}

	public static Vector2 Origin(ImgNode frame)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = frame.Get("origin") ?? Canvas(frame).Get("origin");
		return new Vector2((float)(imgNode?.X ?? 0), (float)(imgNode?.Y ?? 0));
	}

	public static Sprite Sprite(ImgNode frame, string key = null)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		key = key ?? ("node:" + frame.GetHashCode());
		if (sprites.TryGetValue(key, out var value))
		{
			return value;
		}
		Texture2D val = Texture(frame);
		Vector2 val2 = Origin(frame);
		value = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(val2.x / (float)((Texture)val).width, 1f - val2.y / (float)((Texture)val).height), 40f, 0u, (SpriteMeshType)0);
		((Object)value).name = key;
		sprites.Add(key, value);
		return value;
	}

	public static string AliasKey(string relative)
	{
		int num = relative.LastIndexOf('.');
		int num2 = relative.LastIndexOf('/');
		if (num <= num2)
		{
			return relative;
		}
		return relative.Substring(0, num);
	}

	public static ImgNode AliasNode(string relative)
	{
		if (relative.StartsWith("wz://", StringComparison.Ordinal))
		{
			return Node(relative);
		}
		string text = AliasKey(relative);
		if (((Dictionary<string, object>)Index["aliases"]).TryGetValue(text, out var value))
		{
			return Node((string)((Dictionary<string, object>)value)["source"]);
		}
		if (text.StartsWith("skillicon/", StringComparison.Ordinal) && int.TryParse(text.Substring("skillicon/".Length), out var result))
		{
			ImgNode imgNode = Node("Skill/" + (result / 10000).ToString("D3") + ".img/skill/" + result.ToString("D7") + "/icon");
			if (imgNode != null)
			{
				return imgNode;
			}
		}
		if (missing.Add(text))
		{
			Debug.LogWarning((object)("[WzAssets] Unmapped resource: " + relative));
		}
		return null;
	}

	public static Texture2D Texture(string relative)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected Obj, but got Unknown
		if (aliases.TryGetValue(relative, out var value))
		{
			return value;
		}
		ImgNode imgNode = AliasNode(relative);
		if (imgNode == null)
		{
			return null;
		}
		value = Texture(imgNode);
		Dictionary<string, object> dictionary = (Dictionary<string, object>)Index["aliases"];
		if (!relative.StartsWith("wz://", StringComparison.Ordinal) && dictionary.TryGetValue(AliasKey(relative), out var value2) && ((Dictionary<string, object>)value2).TryGetValue("crop", out var value3))
		{
			List<object> list = (List<object>)value3;
			int num = Convert.ToInt32(list[0]);
			int num2 = Convert.ToInt32(list[1]);
			int num3 = Convert.ToInt32(list[2]);
			int num4 = Convert.ToInt32(list[3]);
			Texture2D val = new Texture2D(num3, num4, (TextureFormat)4, false);
			val.SetPixels(value.GetPixels(num, ((Texture)value).height - num2 - num4, num3, num4));
			val.Apply();
			((Texture)val).filterMode = (FilterMode)0;
			((Texture)val).wrapMode = (TextureWrapMode)1;
			((Object)val).name = relative;
			value = val;
		}
		aliases.Add(relative, value);
		return value;
	}

	public static Sprite Frame(string relative)
	{
		if (relative.StartsWith("avatar://", StringComparison.Ordinal))
		{
			return WzCharacter.Frame(relative);
		}
		ImgNode imgNode = AliasNode(relative);
		if (imgNode != null)
		{
			return Sprite(imgNode, relative);
		}
		return null;
	}

	public static List<ImgNode> Frames(ImgNode action)
	{
		List<ImgNode> list = new List<ImgNode>();
		if (action == null)
		{
			return list;
		}
		action = action.TryResolve();
		if (action == null)
		{
			return list;
		}
		if (action.Kind == "Canvas")
		{
			list.Add(action);
			return list;
		}
		foreach (ImgNode child in action.Children)
		{
			if (int.TryParse(child.Name, out var _))
			{
				list.Add(child);
			}
		}
		list.Sort((ImgNode a, ImgNode b) => int.Parse(a.Name).CompareTo(int.Parse(b.Name)));
		return list;
	}

	public static ImgNode MobArtRoot(int id)
	{
		string file;
		return MobArtRoot(id, out file);
	}

	private static ImgNode MobArtRoot(int id, out string file)
	{
		file = "Mob/" + id.ToString("D7") + ".img";
		ImgNode imgNode = FileRoot(file);
		HashSet<int> hashSet = new HashSet<int> { id };
		while (imgNode.Get("info/link") != null)
		{
			id = Convert.ToInt32(imgNode.Get("info/link").Value);
			if (!hashSet.Add(id))
			{
				throw new InvalidDataException("Mob link cycle");
			}
			file = "Mob/" + id.ToString("D7") + ".img";
			imgNode = FileRoot(file);
		}
		return imgNode;
	}

	public static Art.Clip MorphClip(string key, string action)
	{
		string key2 = "morph/" + key + "/" + action;
		if (mobClips.TryGetValue(key2, out var value))
		{
			return value;
		}
		if (!int.TryParse(key, out var result))
		{
			return null;
		}
		HashSet<int> hashSet = new HashSet<int>();
		while (hashSet.Add(result))
		{
			string text = "Morph/" + result.ToString("D4") + ".img";
			if (!HasFile(text))
			{
				return null;
			}
			ImgNode imgNode = FileRoot(text);
			List<ImgNode> list = Frames(imgNode.Get(action));
			if (list.Count > 0)
			{
				Art.Clip clip = new Art.Clip
				{
					frames = new string[list.Count],
					delays = new float[list.Count]
				};
				for (int i = 0; i < list.Count; i++)
				{
					clip.frames[i] = "wz://" + text + "/" + action + "/" + list[i].Name;
					clip.delays[i] = (float)Math.Max(1, Math.Abs(list[i].Resolve().Int("delay", 100))) / 1000f;
				}
				mobClips[key2] = clip;
				return clip;
			}
			ImgNode imgNode2 = imgNode.Get("info/link");
			if (imgNode2 == null)
			{
				return null;
			}
			result = Convert.ToInt32(imgNode2.Value);
		}
		return null;
	}

	public static Art.Clip MobClip(string key, string action)
	{
		string key2 = key + "/" + action;
		if (mobClips.TryGetValue(key2, out var value))
		{
			return value;
		}
		if (!((Dictionary<string, object>)Index["mobs"]).TryGetValue(key, out var value2))
		{
			if (!int.TryParse(key, out var result))
			{
				return null;
			}
			value2 = result;
		}
		List<ImgNode> list = Frames(MobArtRoot(Convert.ToInt32(value2), out var file).Get(action));
		if (list.Count == 0)
		{
			return null;
		}
		Art.Clip clip = new Art.Clip
		{
			frames = new string[list.Count],
			delays = new float[list.Count]
		};
		for (int i = 0; i < list.Count; i++)
		{
			clip.frames[i] = "wz://" + file + "/" + action + "/" + list[i].Name;
			clip.delays[i] = (float)Math.Max(1, Math.Abs(list[i].Resolve().Int("delay", 100))) / 1000f;
		}
		mobClips.Add(key2, clip);
		return clip;
	}
}
