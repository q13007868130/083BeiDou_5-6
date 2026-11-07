using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class AdminFileTree
{
	private static string baseDir;

	public static void Register(AdminApi api)
	{
		api.Post("/file/v1/tree", Tree);
		api.Post("/file/v1/tree/read", Read);
		api.Post("/file/v1/tree/write", Write);
	}

	private static string BaseDir()
	{
		if (baseDir != null)
		{
			return baseDir;
		}
		string text = Path.Combine(WzAssets.DataRoot, "scripts");
		if (!Directory.Exists(text))
		{
			throw new AdminApiException("脚本目录不存在: " + text);
		}
		baseDir = Path.GetFullPath(text);
		return baseDir;
	}

	private static int CompareNodes(FileInfo a, FileInfo b)
	{
		string text = a.Name.Replace(".js", "");
		string text2 = b.Name.Replace(".js", "");
		bool flag = IsNum(text);
		bool flag2 = IsNum(text2);
		if (flag && !flag2)
		{
			return -1;
		}
		if (!flag & flag2)
		{
			return 1;
		}
		if (flag & flag2)
		{
			return long.Parse(text2).CompareTo(long.Parse(text));
		}
		return string.Compare(text, text2, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsNum(string s)
	{
		if (s.Length == 0)
		{
			return false;
		}
		foreach (char c in s)
		{
			if (c < '0' || c > '9')
			{
				return false;
			}
		}
		return true;
	}

	private static object Tree(AdminCtx ctx)
	{
		string currentKey = AdminJson.GetString(AdminJson.AsDict(ctx.Body), "currentKey");
		return AdminMainThread.Run(() =>
		{
			FileInfo fileInfo = Resolve(currentKey);
			if (!fileInfo.Exists || (fileInfo.Attributes & FileAttributes.Directory) == 0)
			{
				throw new AdminApiException("请输入文件夹");
			}
			List<FileInfo> list = new List<FileInfo>();
			List<FileInfo> list2 = new List<FileInfo>();
			string[] fileSystemEntries = Directory.GetFileSystemEntries(fileInfo.FullName);
			for (int i = 0; i < fileSystemEntries.Length; i++)
			{
				FileInfo fileInfo2 = new FileInfo(fileSystemEntries[i]);
				if ((fileInfo2.Attributes & FileAttributes.Directory) != 0)
				{
					list.Add(fileInfo2);
				}
				else
				{
					list2.Add(fileInfo2);
				}
			}
			list.Sort(CompareNodes);
			list2.Sort(CompareNodes);
			List<object> list3 = new List<object>();
			int num = 0;
			foreach (FileInfo item in list)
			{
				list3.Add(Node(item, currentKey, num++));
			}
			foreach (FileInfo item2 in list2)
			{
				list3.Add(Node(item2, currentKey, num++));
			}
			return list3;
		});
	}

	private static object Node(FileInfo info, string parentKey, int index)
	{
		string text = (string.IsNullOrEmpty(parentKey) ? index.ToString() : (parentKey + "-" + index));
		bool flag = (info.Attributes & FileAttributes.Directory) != 0;
		return AdminJson.Obj("title", info.Name, "key", text, "isLeaf", !flag, "children", flag ? new List<object>() : null);
	}

	private static FileInfo Resolve(string currentKey)
	{
		FileInfo fileInfo = new FileInfo(BaseDir());
		if (!string.IsNullOrEmpty(currentKey))
		{
			string[] array = currentKey.Split('-');
			for (int i = 0; i < array.Length; i++)
			{
				if (!int.TryParse(array[i], out var result))
				{
					throw new AdminApiException("文件/文件夹不存在");
				}
				string[] fileSystemEntries = Directory.GetFileSystemEntries(fileInfo.FullName);
				if (result < 0 || result >= fileSystemEntries.Length)
				{
					throw new AdminApiException("文件/文件夹不存在");
				}
				fileInfo = new FileInfo(fileSystemEntries[result]);
			}
		}
		if (!Path.GetFullPath(fileInfo.FullName).StartsWith(BaseDir(), StringComparison.Ordinal))
		{
			throw new AdminApiException("检测到路径逃逸尝试");
		}
		return fileInfo;
	}

	private static object Read(AdminCtx ctx)
	{
		Dictionary<string, object> o = AdminJson.AsDict(ctx.Body);
		string currentKey = AdminJson.GetString(o, "currentKey");
		string title = AdminJson.GetString(o, "title");
		return AdminMainThread.Run(() =>
		{
			FileInfo fileInfo = Resolve(currentKey);
			if (fileInfo.Name != title)
			{
				throw new AdminApiException("文件目录发生变动，请重新读取");
			}
			return AdminJson.Obj("content", File.ReadAllText(fileInfo.FullName, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)));
		});
	}

	private static object Write(AdminCtx ctx)
	{
		Dictionary<string, object> o = AdminJson.AsDict(ctx.Body);
		string currentKey = AdminJson.GetString(o, "currentKey");
		string title = AdminJson.GetString(o, "title");
		string content = AdminJson.GetString(o, "content");
		return AdminMainThread.Run(() =>
		{
			FileInfo fileInfo = Resolve(currentKey);
			if (fileInfo.Name != title)
			{
				throw new AdminApiException("文件目录发生变动，请重新写入");
			}
			if ((fileInfo.Attributes & FileAttributes.Directory) != 0)
			{
				throw new AdminApiException("请选择文件");
			}
			File.WriteAllText(fileInfo.FullName, content ?? "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			ScriptSource.ClearCache();
			return AdminJson.Ok();
		});
	}
}
