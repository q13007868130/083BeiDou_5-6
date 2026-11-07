using System;
using System.IO;
using System.Reflection;

public static class PlatformRes
{
	public static string LoadText(string resPath)
	{
		string text = TryLoadTextAsset(resPath);
		if (text != null)
		{
			return text;
		}
		string[] array = new string[7]
		{
			AppContext.BaseDirectory,
			".",
			"Assets/Resources",
			"../Assets/Resources",
			"../../Assets/Resources",
			"Assets/Scripts/Server/Db",
			"../Assets/Scripts/Server/Db"
		};
		foreach (string path in array)
		{
			string[] array2 = new string[3] { "", ".json", ".sql" };
			foreach (string text2 in array2)
			{
				string path2 = Path.Combine(path, resPath + text2);
				if (File.Exists(path2))
				{
					return File.ReadAllText(path2);
				}
			}
		}
		return null;
	}

	private static string TryLoadTextAsset(string resPath)
	{
		try
		{
			Type type = Type.GetType("UnityEngine.Resources, UnityEngine");
			if (type == null)
			{
				return null;
			}
			MethodInfo method = type.GetMethod("Load", new Type[2]
			{
				typeof(string),
				typeof(Type)
			});
			if (method == null)
			{
				return null;
			}
			object obj = method.Invoke(null, new object[2]
			{
				resPath,
				type.Assembly.GetType("UnityEngine.TextAsset")
			});
			if (obj == null)
			{
				return null;
			}
			PropertyInfo property = obj.GetType().GetProperty("text");
			return (property != null) ? ((string)property.GetValue(obj, null)) : null;
		}
		catch
		{
			return null;
		}
	}

	public static string PersistentDir()
	{
		try
		{
			Type type = Type.GetType("UnityEngine.Application, UnityEngine");
			if (type != null)
			{
				PropertyInfo property = type.GetProperty("persistentDataPath", BindingFlags.Static | BindingFlags.Public);
				if (property != null)
				{
					return (string)property.GetValue(null, null);
				}
			}
		}
		catch
		{
		}
		return ".";
	}
}
