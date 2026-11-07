using System;
using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeSkillVfx
{
	private static readonly Dictionary<string, Dictionary<string, NativeVfxClip>> skills = new Dictionary<string, Dictionary<string, NativeVfxClip>>();

	private static readonly Dictionary<string, Dictionary<string, object>> metadata = new Dictionary<string, Dictionary<string, object>>();

	private static readonly Dictionary<string, List<NativeVfxVideo>> videos = new Dictionary<string, List<NativeVfxVideo>>();

	public static string Key(int id, int level, string alias = "")
	{
		Ensure();
		string text = id + "@" + level;
		if (skills.ContainsKey(text))
		{
			return text;
		}
		if (string.IsNullOrEmpty(alias) || !skills.ContainsKey(alias))
		{
			foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)WzAssets.Index["skills"])
			{
				if (Convert.ToInt32(item.Value) == id)
				{
					alias = item.Key;
					break;
				}
			}
		}
		Dictionary<string, NativeVfxClip> dictionary = ((!string.IsNullOrEmpty(alias) && skills.TryGetValue(alias, out var value)) ? new Dictionary<string, NativeVfxClip>(value) : new Dictionary<string, NativeVfxClip>());
		Dictionary<string, object> value2 = ((!string.IsNullOrEmpty(alias) && metadata.TryGetValue(alias, out var value3)) ? new Dictionary<string, object>(value3) : new Dictionary<string, object>());
		List<NativeVfxVideo> list = ((!string.IsNullOrEmpty(alias) && videos.TryGetValue(alias, out var value4)) ? new List<NativeVfxVideo>(value4) : new List<NativeVfxVideo>());
		string text2 = "Skill/" + (NativeSoulWeapon.IsSkill(id) ? 800128 : (id / 10000)).ToString("D3") + ".img/skill/" + id.ToString("D7");
		ImgNode imgNode = WzAssets.Node(text2);
		HashSet<string> hashSet = new HashSet<string>();
		ImgNode imgNode2 = imgNode?.Get("level/" + level)?.TryResolve();
		if (imgNode != null)
		{
			foreach (ImgNode child in imgNode.Children)
			{
				hashSet.Add(child.Name);
			}
		}
		if (imgNode2 != null)
		{
			foreach (ImgNode child2 in imgNode2.Children)
			{
				hashSet.Add(child2.Name);
			}
		}
		foreach (string branch in hashSet)
		{
			if (branch == "level" || branch.StartsWith("icon", StringComparison.Ordinal) || branch == "action")
			{
				continue;
			}
			ImgNode imgNode3 = imgNode?.Get("level/" + level + "/" + branch)?.TryResolve();
			ImgNode imgNode4 = imgNode3 ?? imgNode?.Get(branch)?.TryResolve();
			if (imgNode4 == null)
			{
				continue;
			}
			foreach (string item2 in new List<string>(dictionary.Keys))
			{
				if (item2 == branch || item2.StartsWith(branch + "/", StringComparison.Ordinal))
				{
					dictionary.Remove(item2);
				}
			}
			list.RemoveAll((NativeVfxVideo v) => v.path == branch || v.path.StartsWith(branch + "/", StringComparison.Ordinal));
			string sourcePath = ((imgNode3 != null) ? (text2 + "/level/" + level + "/" + branch) : (text2 + "/" + branch));
			if (branch == "number" || branch == "number2")
			{
				ReadIcons(imgNode4, sourcePath, branch, dictionary);
			}
			else
			{
				Read(imgNode4, sourcePath, branch, dictionary, value2, list, new HashSet<ImgNode>());
			}
		}
		string text3 = id switch
		{
			11101005 => "SoulRush", 
			14101004 => "Flying1", 
			4111006 => "Flying", 
			_ => null, 
		};
		if (text3 != null)
		{
			string text4 = "Effect/BasicEff.img/" + text3;
			ImgNode imgNode5 = WzAssets.Node(text4);
			if (imgNode5 != null)
			{
				Read(imgNode5, text4, "movement", dictionary, value2, list, new HashSet<ImgNode>());
			}
		}
		skills.Add(text, dictionary);
		metadata.Add(text, value2);
		videos.Add(text, list);
		return text;
	}

	private static ImgNode MetaNode(ImgNode frame, ImgNode canvas)
	{
		if (frame == null || !(frame.Kind == "Canvas"))
		{
			return canvas;
		}
		return frame;
	}

	private static void Read(ImgNode source, string sourcePath, string path, Dictionary<string, NativeVfxClip> clips, Dictionary<string, object> metadata, List<NativeVfxVideo> layerVideos, HashSet<ImgNode> ancestors)
	{
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = source.TryResolve();
		if (imgNode == null || !ancestors.Add(imgNode))
		{
			return;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (ImgNode child in imgNode.Children)
		{
			ImgNode imgNode2 = child.TryResolve();
			if (imgNode2 != null && imgNode2.Value != null && imgNode2.Kind != "Canvas")
			{
				dictionary[child.Name] = imgNode2.Value;
			}
		}
		metadata[path] = dictionary;
		List<NativeVfxFrame> list = new List<NativeVfxFrame>();
		foreach (ImgNode item in WzAssets.Frames(imgNode))
		{
			ImgNode imgNode3 = item.TryResolve();
			if (imgNode3 != null && !(imgNode3.Kind != "Canvas"))
			{
				ImgNode imgNode4 = MetaNode(item, imgNode3);
				list.Add(new NativeVfxFrame
				{
					file = "wz://" + sourcePath + ((imgNode.Kind == "Canvas") ? "" : ("/" + item.Name)),
					origin = WzAssets.Origin(imgNode4),
					delay = Mathf.Max(0.001f, (float)Mathf.Abs(imgNode4.Int("delay", 100)) / 1000f),
					alpha0 = (float)imgNode4.Int("a0", 255) / 255f,
					alpha1 = (float)imgNode4.Int("a1", 255) / 255f,
					z = imgNode4.Int("z", imgNode.Int("z"))
				});
			}
		}
		if (list.Count > 0)
		{
			clips[path] = new NativeVfxClip
			{
				path = path,
				frames = list.ToArray(),
				meta = dictionary
			};
		}
		if (imgNode.Kind != "Canvas")
		{
			foreach (ImgNode child2 in imgNode.Children)
			{
				ImgNode imgNode5 = child2.TryResolve();
				if (imgNode5 != null)
				{
					if (imgNode5.Kind == "Canvas#Video")
					{
						layerVideos.Add(new NativeVfxVideo
						{
							path = path,
							node = imgNode5,
							z = imgNode.Int("z"),
							delay = (float)Mathf.Max(0, imgNode.Int("delay")) / 1000f,
							loop = path.EndsWith("/loop", StringComparison.Ordinal)
						});
					}
					else if (imgNode5.Kind != "Canvas" && imgNode5.Children.Count > 0)
					{
						Read(child2, sourcePath + "/" + child2.Name, path + "/" + child2.Name, clips, metadata, layerVideos, ancestors);
					}
				}
			}
		}
		ancestors.Remove(imgNode);
	}

	private static void ReadIcons(ImgNode source, string sourcePath, string branch, Dictionary<string, NativeVfxClip> clips)
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		ImgNode imgNode = source.TryResolve();
		if (imgNode == null)
		{
			return;
		}
		List<KeyValuePair<int, ImgNode>> list = new List<KeyValuePair<int, ImgNode>>();
		foreach (ImgNode child in imgNode.Children)
		{
			if (child.Name.StartsWith("icon", StringComparison.Ordinal) && int.TryParse(child.Name.Substring(4), out var result) && result > 0)
			{
				list.Add(new KeyValuePair<int, ImgNode>(result, child));
			}
		}
		list.Sort((KeyValuePair<int, ImgNode> a, KeyValuePair<int, ImgNode> b) => a.Key.CompareTo(b.Key));
		foreach (KeyValuePair<int, ImgNode> item in list)
		{
			ImgNode imgNode2 = item.Value.TryResolve();
			if (imgNode2 != null && !(imgNode2.Kind != "Canvas"))
			{
				ImgNode imgNode3 = MetaNode(item.Value, imgNode2);
				string text = branch + "/" + item.Value.Name;
				clips[text] = new NativeVfxClip
				{
					path = text,
					frames = new NativeVfxFrame[1]
					{
						new NativeVfxFrame
						{
							file = "wz://" + sourcePath + "/" + item.Value.Name,
							origin = WzAssets.Origin(imgNode3),
							delay = 1f,
							alpha0 = 1f,
							alpha1 = 1f,
							z = imgNode3.Int("z", imgNode.Int("z"))
						}
					},
					meta = new Dictionary<string, object> { { "index", item.Key } }
				};
			}
		}
	}

	private static void Ensure()
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		if (skills.Count > 0)
		{
			return;
		}
		foreach (Dictionary<string, object> item in (List<object>)((Dictionary<string, object>)MiniJson.Parse(Resources.Load<TextAsset>("GameplaySkillVfx").text))["skills"])
		{
			string key = (string)item["key"];
			Dictionary<string, NativeVfxClip> dictionary2 = new Dictionary<string, NativeVfxClip>();
			foreach (KeyValuePair<string, object> item2 in (Dictionary<string, object>)item["clips"])
			{
				Dictionary<string, object> dictionary3 = (Dictionary<string, object>)item2.Value;
				List<NativeVfxFrame> list = new List<NativeVfxFrame>();
				foreach (Dictionary<string, object> item3 in (List<object>)dictionary3["frames"])
				{
					string text = (string)item3["source"];
					ImgNode imgNode = WzAssets.Node(text);
					Vector2 origin = WzAssets.Origin(imgNode);
					list.Add(new NativeVfxFrame
					{
						file = "wz://" + text,
						origin = origin,
						delay = Mathf.Max(0.001f, (float)Mathf.Abs(imgNode.Int("delay", 100)) / 1000f),
						alpha0 = (float)imgNode.Int("a0", 255) / 255f,
						alpha1 = (float)imgNode.Int("a1", 255) / 255f,
						z = imgNode.Int("z", (int)Art.Num(item3, "z"))
					});
				}
				dictionary2.Add(item2.Key, new NativeVfxClip
				{
					path = item2.Key,
					frames = list.ToArray(),
					meta = (Dictionary<string, object>)dictionary3["meta"]
				});
			}
			skills.Add(key, dictionary2);
			metadata.Add(key, (Dictionary<string, object>)item["metadata"]);
		}
	}

	public static NativeVfxClip Get(string key, string path)
	{
		Ensure();
		if (!skills.TryGetValue(key, out var value))
		{
			return null;
		}
		if (!value.TryGetValue(path, out var value2))
		{
			return null;
		}
		return value2;
	}

	public static List<NativeVfxClip> Parts(string key, string branch)
	{
		Ensure();
		List<NativeVfxClip> list = new List<NativeVfxClip>();
		if (skills.TryGetValue(key, out var value))
		{
			foreach (KeyValuePair<string, NativeVfxClip> item in value)
			{
				if (item.Key == branch || item.Key.StartsWith(branch + "/", StringComparison.Ordinal))
				{
					list.Add(item.Value);
				}
			}
		}
		list.Sort((NativeVfxClip a, NativeVfxClip b) => string.CompareOrdinal(a.path, b.path));
		return list;
	}

	public static List<string> SubBranches(string key, string root)
	{
		Ensure();
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (skills.TryGetValue(key, out var value))
		{
			foreach (string key2 in value.Keys)
			{
				if (!key2.StartsWith(root + "/", StringComparison.Ordinal))
				{
					continue;
				}
				string text = key2.Substring(root.Length + 1);
				int num = text.IndexOf('/');
				if (num > 0)
				{
					string item = root + "/" + text.Substring(0, num);
					if (hashSet.Add(item))
					{
						list.Add(item);
					}
				}
			}
		}
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	public static List<NativeVfxClip> Icons(string key, string branch)
	{
		Ensure();
		List<NativeVfxClip> list = new List<NativeVfxClip>();
		if (skills.TryGetValue(key, out var value))
		{
			foreach (KeyValuePair<string, NativeVfxClip> item in value)
			{
				if (item.Key.StartsWith(branch + "/", StringComparison.Ordinal))
				{
					list.Add(item.Value);
				}
			}
		}
		list.Sort((NativeVfxClip a, NativeVfxClip b) => IconIndex(a).CompareTo(IconIndex(b)));
		return list;
	}

	private static int IconIndex(NativeVfxClip clip)
	{
		if (clip.meta == null || !clip.meta.TryGetValue("index", out var value))
		{
			return 0;
		}
		return Convert.ToInt32(value);
	}

	public static List<NativeVfxClip> Segment(string key, string branch, string stage)
	{
		return Parts(key, branch + "/" + stage);
	}

	public static Dictionary<string, object> Meta(string key, string path)
	{
		Ensure();
		if (!metadata.TryGetValue(key, out var value) || !value.TryGetValue(path, out var value2))
		{
			return null;
		}
		return (Dictionary<string, object>)value2;
	}

	public static float Number(string key, string path, string field, float fallback = 0f)
	{
		Dictionary<string, object> dictionary = Meta(key, path);
		if (dictionary != null)
		{
			return Art.Num(dictionary, field, fallback);
		}
		return fallback;
	}

	public static bool Has(string key, string branch)
	{
		return Parts(key, branch).Count > 0;
	}

	public static List<NativeVfxVideo> VideoLayers(string key)
	{
		Ensure();
		if (!videos.TryGetValue(key, out var value))
		{
			return new List<NativeVfxVideo>();
		}
		return value;
	}

	public static List<NativeVfxVideo> VideoParts(string key, string branch)
	{
		List<NativeVfxVideo> list = new List<NativeVfxVideo>();
		foreach (NativeVfxVideo item in VideoLayers(key))
		{
			if (item.path == branch || item.path.StartsWith(branch + "/", StringComparison.Ordinal))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool HasVideo(string key, string branch)
	{
		return VideoParts(key, branch).Count > 0;
	}

	public static List<string> Roots(string key)
	{
		Ensure();
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (skills.TryGetValue(key, out var value))
		{
			foreach (string key2 in value.Keys)
			{
				string item = key2.Split('/')[0];
				if (hashSet.Add(item))
				{
					list.Add(item);
				}
			}
		}
		foreach (NativeVfxVideo item3 in VideoLayers(key))
		{
			string item2 = item3.path.Split('/')[0];
			if (hashSet.Add(item2))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public static string Alias(string part)
	{
		if (part == "cast")
		{
			return "effect";
		}
		if (part == "cast2")
		{
			return "effect0";
		}
		return part;
	}

	public static bool Numbered(string name, string stem)
	{
		if (name == null || !name.StartsWith(stem, StringComparison.Ordinal))
		{
			return false;
		}
		for (int i = stem.Length; i < name.Length; i++)
		{
			if (name[i] < '0' || name[i] > '9')
			{
				return false;
			}
		}
		return true;
	}

	public static List<string> Layers(string key, string stem)
	{
		Ensure();
		List<string> list = new List<string>();
		foreach (string item in Roots(key))
		{
			if (Numbered(item, stem))
			{
				list.Add(item);
			}
		}
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	public static bool HasLayer(string key, string stem)
	{
		return Layers(key, stem).Count > 0;
	}

	public static bool RapidAttack(int id)
	{
		ImgNode imgNode = WzAssets.Node("Skill/" + (id / 10000).ToString("D3") + ".img/skill/" + id.ToString("D7"));
		if (imgNode == null)
		{
			return false;
		}
		if (imgNode.Int("info/rapidAttack") != 0)
		{
			return true;
		}
		if (imgNode.Get("keydown") == null || imgNode.Get("keydownend") == null)
		{
			return false;
		}
		if (SafeInt(imgNode, "common/attackDelay") > 0)
		{
			return SafeInt(imgNode, "common/attackCount") > 0;
		}
		return false;
	}

	private static int SafeInt(ImgNode node, string path)
	{
		object obj = node?.Get(path)?.TryResolve()?.Value;
		if (obj == null)
		{
			return 0;
		}
		try
		{
			return Convert.ToInt32(obj);
		}
		catch
		{
			return 0;
		}
	}
}
