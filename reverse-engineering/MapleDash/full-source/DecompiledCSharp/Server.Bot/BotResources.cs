using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Bot;

public sealed class BotResources
{
	private readonly Dictionary<string, object> resources;

	public BotResources(string json)
		: this((json == null) ? null : (MiniJsonDb.Parse(json) as Dictionary<string, object>))
	{
	}

	public BotResources(Dictionary<string, object> root)
	{
		resources = ((root != null && root.TryGetValue("resources", out var value)) ? (value as Dictionary<string, object>) : null);
		if (resources == null)
		{
			throw new ArgumentException("SoloMapling resource pack is missing or invalid");
		}
	}

	public static BotResources Load()
	{
		return new BotResources(BotWarmup.Root("Bot/SoloMapling"));
	}

	public byte[] ReadBytes(string path)
	{
		if (resources.TryGetValue(path, out var value) && value is Dictionary<string, object> dictionary && dictionary.TryGetValue("base64", out var value2) && value2 is string s)
		{
			return Convert.FromBase64String(s);
		}
		return null;
	}

	public string ReadText(string path)
	{
		if (resources.TryGetValue(path, out var value) && value is Dictionary<string, object> dictionary)
		{
			if (dictionary.TryGetValue("error", out var value2))
			{
				throw new InvalidDataException(path + ": " + value2);
			}
			if (dictionary.TryGetValue("text", out var value3))
			{
				return value3 as string;
			}
		}
		return null;
	}

	public object ReadData(string path)
	{
		if (resources.TryGetValue(path, out var value) && value is Dictionary<string, object> dictionary)
		{
			if (dictionary.TryGetValue("error", out var value2))
			{
				throw new InvalidDataException(path + ": " + value2);
			}
			if (dictionary.TryGetValue("data", out var value3))
			{
				return value3;
			}
		}
		return null;
	}

	public List<string> ListFileStems(string directory, string extension = ".csv")
	{
		string text = directory.TrimEnd('/') + "/";
		List<string> list = new List<string>();
		foreach (string key in resources.Keys)
		{
			if (key.StartsWith(text, StringComparison.Ordinal) && key.EndsWith(extension, StringComparison.Ordinal))
			{
				list.Add(key.Substring(text.Length, key.Length - text.Length - extension.Length));
			}
		}
		return list;
	}
}
