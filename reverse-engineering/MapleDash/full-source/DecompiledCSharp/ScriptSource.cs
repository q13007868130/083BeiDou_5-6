using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class ScriptSource
{
	private const string PackedResource = "Scripts/npc_scripts";

	private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

	private static readonly HashSet<string> missing = new HashSet<string>();

	private static Dictionary<string, object> packed;

	private static bool packedLoaded;

	public static string Root => Path.Combine(WzAssets.DataRoot, "scripts");

	public static string Evidence => Root + " | Resources/Scripts/npc_scripts";

	private static Dictionary<string, object> Packed
	{
		get
		{
			if (packedLoaded)
			{
				return packed;
			}
			packedLoaded = true;
			TextAsset val = Resources.Load<TextAsset>("Scripts/npc_scripts");
			if ((Object)(object)val == (Object)null)
			{
				Debug.LogWarning((object)"[Script] 缺少 Resources/Scripts/npc_scripts，请先运行 tools/sync_npc_scripts.py");
				return null;
			}
			try
			{
				packed = MiniJson.Parse(val.text) as Dictionary<string, object>;
			}
			catch (Exception ex)
			{
				Debug.LogError((object)("[Script] 脚本包解析失败: " + ex.Message));
				packed = null;
			}
			return packed;
		}
	}

	public static string LoadNpc(int npcId, string scriptName, out string key)
	{
		string[] array = ((!string.IsNullOrEmpty(scriptName)) ? new string[3]
		{
			"npc/" + scriptName,
			"BeiDouSpecial/" + scriptName,
			"npc/" + npcId
		} : new string[1] { "npc/" + npcId });
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = LoadKey(text);
			if (text2 != null)
			{
				key = text;
				return text2;
			}
		}
		key = array[0];
		return null;
	}

	public static string LoadKey(string key)
	{
		if (string.IsNullOrEmpty(key) || key.StartsWith("/", StringComparison.Ordinal) || key.IndexOf('\\') >= 0)
		{
			throw new ArgumentException("Invalid script key", "key");
		}
		string[] array = key.Split('/');
		foreach (string text in array)
		{
			if (text.Length == 0 || text == "." || text == "..")
			{
				throw new ArgumentException("Invalid script key", "key");
			}
		}
		int num = key.IndexOf('/');
		if (num >= 0)
		{
			return Load(key.Substring(0, num), key.Substring(num + 1));
		}
		return Load("", key);
	}

	public static bool Has(string kind, int id)
	{
		return !string.IsNullOrEmpty(Load(kind, id));
	}

	public static string Load(string kind, int id)
	{
		return Load(kind, id.ToString());
	}

	public static string Load(string kind, string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		string text = (string.IsNullOrEmpty(kind) ? name : (kind + "/" + name));
		if (Path.IsPathRooted(text) || text.IndexOf('\\') >= 0)
		{
			throw new ArgumentException("Invalid script path");
		}
		string[] array = text.Split('/');
		foreach (string text2 in array)
		{
			if (text2.Length == 0 || text2 == "." || text2 == "..")
			{
				throw new ArgumentException("Invalid script path");
			}
		}
		if (cache.TryGetValue(text, out var value))
		{
			return value;
		}
		if (missing.Contains(text))
		{
			return null;
		}
		value = LoadFile(kind, name);
		if (string.IsNullOrEmpty(value))
		{
			value = LoadPacked(text);
		}
		if (string.IsNullOrEmpty(value))
		{
			missing.Add(text);
			return null;
		}
		cache[text] = value;
		return value;
	}

	private static string LoadFile(string kind, string name)
	{
		string text = Path.Combine(Root, kind, name + ".js");
		try
		{
			if (File.Exists(text))
			{
				return File.ReadAllText(text, Encoding.UTF8);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Script] 读取失败 " + text + ": " + ex.Message));
		}
		return null;
	}

	private static string LoadPacked(string key)
	{
		Dictionary<string, object> dictionary = Packed;
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(key, out var value))
		{
			return null;
		}
		return value as string;
	}

	public static List<string> Keys(string kind)
	{
		List<string> list = new List<string>();
		string text = Path.Combine(Root, kind);
		try
		{
			if (Directory.Exists(text))
			{
				string[] files = Directory.GetFiles(text, "*.js");
				for (int i = 0; i < files.Length; i++)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
					if (!list.Contains(fileNameWithoutExtension))
					{
						list.Add(fileNameWithoutExtension);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Script] 枚举目录失败 " + text + ": " + ex.Message));
		}
		Dictionary<string, object> dictionary = Packed;
		if (dictionary != null)
		{
			string text2 = kind + "/";
			foreach (string key in dictionary.Keys)
			{
				if (key.StartsWith(text2, StringComparison.Ordinal))
				{
					string text3 = key.Substring(text2.Length);
					if (text3.Length != 0 && text3.IndexOf('/') < 0 && !list.Contains(text3))
					{
						list.Add(text3);
					}
				}
			}
		}
		return list;
	}

	public static void ClearCache()
	{
		cache.Clear();
		missing.Clear();
		packed = null;
		packedLoaded = false;
	}
}
