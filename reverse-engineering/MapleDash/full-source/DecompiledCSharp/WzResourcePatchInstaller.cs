using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

public static class WzResourcePatchInstaller
{
	private const string Transaction = "ResourcePatchTransaction";

	private static string SafePath(string root, string relative)
	{
		string value = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
		string fullPath = Path.GetFullPath(Path.Combine(root, relative));
		if (!fullPath.StartsWith(value, StringComparison.Ordinal) || relative.Contains("\\") || relative.StartsWith("/", StringComparison.Ordinal))
		{
			throw new InvalidDataException("资源路径无效: " + relative);
		}
		return fullPath;
	}

	private static void DurableWrite(string path, string text)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		fileStream.Write(bytes, 0, bytes.Length);
		fileStream.Flush(flushToDisk: true);
	}

	private static Dictionary<string, string> Files(string manifest)
	{
		Dictionary<string, object> dictionary = (Dictionary<string, object>)MiniJson.Parse(manifest);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (Dictionary<string, object> item in (List<object>)dictionary["files"])
		{
			string text = (string)item["path"];
			if (text.StartsWith(".", StringComparison.Ordinal))
			{
				throw new InvalidDataException("资源路径保留");
			}
			dictionary2.Add(text, (string)item["sha256"]);
		}
		return dictionary2;
	}

	public static void Recover(string root)
	{
		string text = Path.Combine(root, "ResourcePatchTransaction");
		if (!Directory.Exists(text))
		{
			return;
		}
		string path = Path.Combine(text, "journal");
		if (!File.Exists(Path.Combine(text, "committed")) && File.Exists(path))
		{
			string[] array = File.ReadAllLines(path);
			for (int num = array.Length - 1; num >= 0; num--)
			{
				string[] array2 = array[num].Split('|');
				if (array2.Length != 2 || (array2[0] != "0" && array2[0] != "1"))
				{
					throw new InvalidDataException("补丁恢复日志损坏，请联系群主");
				}
				string relative = Encoding.UTF8.GetString(Convert.FromBase64String(array2[1]));
				string text2 = SafePath(root, relative);
				string text3 = SafePath(Path.Combine(text, "backup"), relative);
				if (File.Exists(text3))
				{
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
					Directory.CreateDirectory(Path.GetDirectoryName(text2));
					File.Move(text3, text2);
				}
				else if (array2[0] == "0" && File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
		}
		string retired = text + ".cleanup-" + Guid.NewGuid().ToString("N");
		Directory.Move(text, retired);
		Task.Run(() =>
		{
			try
			{
				Directory.Delete(retired, recursive: true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		});
	}

	public static void Install(WzResourceParts pack, string root, string manifest, string fallbackManifest, string versionJson, Action<int, int> progress, Action<string> phase)
	{
		Recover(root);
		string path = Path.Combine(root, "ResourcePatchTransaction");
		string text = Path.Combine(path, "new");
		string text2 = Path.Combine(root, "Data");
		string path2 = Path.Combine(text2, ".resource-manifest.json");
		Dictionary<string, string> dictionary = Files(File.Exists(path2) ? File.ReadAllText(path2) : fallbackManifest);
		Dictionary<string, string> dictionary2 = Files(manifest);
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		Directory.CreateDirectory(text);
		bool flag = false;
		try
		{
			if (pack.ManifestText != manifest)
			{
				throw new InvalidDataException("补丁目标清单不匹配");
			}
			long num = 0L;
			foreach (KeyValuePair<string, string> item in dictionary2)
			{
				SafePath(text2, item.Key);
				ZipArchiveEntry zipArchiveEntry = pack.FindEntry("Data/" + item.Key);
				if (zipArchiveEntry != null)
				{
					list.Add(item.Key);
					num = checked(num + zipArchiveEntry.Length);
				}
			}
			long num2 = 0L;
			byte[] array = new byte[131072];
			foreach (KeyValuePair<string, string> item2 in dictionary2)
			{
				ZipArchiveEntry zipArchiveEntry2 = pack.FindEntry("Data/" + item2.Key);
				if (zipArchiveEntry2 == null)
				{
					continue;
				}
				phase?.Invoke("正在解压补丁资源");
				string path3 = SafePath(text, "Data/" + item2.Key);
				if (zipArchiveEntry2 != null)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path3));
				}
				using Stream stream = ((zipArchiveEntry2 != null) ? zipArchiveEntry2.Open() : File.OpenRead(SafePath(text2, item2.Key)));
				using FileStream fileStream = File.Create(path3);
				int num3;
				while ((num3 = stream.Read(array, 0, array.Length)) > 0)
				{
					fileStream?.Write(array, 0, num3);
					num2 += num3;
					progress?.Invoke((int)((num > 0) ? (num2 * 9900 / num) : 9900), 10000);
				}
				fileStream?.Flush(flushToDisk: true);
			}
			foreach (KeyValuePair<string, string> item3 in dictionary)
			{
				if (!dictionary2.ContainsKey(item3.Key))
				{
					list2.Add("Data/" + item3.Key);
				}
			}
			List<string> list3 = new List<string>();
			foreach (string item4 in list)
			{
				list3.Add("Data/" + item4);
			}
			list3.AddRange(list2);
			DurableWrite(SafePath(text, "Data/.resource-manifest.json"), manifest);
			list3.Add("Data/.resource-manifest.json");
			if (versionJson != null)
			{
				DurableWrite(SafePath(text, "Data/.resUpdate.json"), versionJson);
				list3.Add("Data/.resUpdate.json");
			}
			DurableWrite(SafePath(text, "NativeAssetIndex.sha256"), WzResourceInstaller.Hash(Encoding.UTF8.GetBytes(manifest)) + "\n");
			list3.Add("NativeAssetIndex.sha256");
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string item5 in list3)
			{
				stringBuilder.Append(File.Exists(SafePath(root, item5)) ? "1|" : "0|").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(item5))).Append('\n');
			}
			DurableWrite(Path.Combine(path, "journal.pending"), stringBuilder.ToString());
			File.Move(Path.Combine(path, "journal.pending"), Path.Combine(path, "journal"));
			phase?.Invoke("正在应用补丁");
			progress?.Invoke(0, 10000);
			int num4 = 0;
			foreach (string item6 in list3)
			{
				string text3 = SafePath(root, item6);
				string text4 = SafePath(Path.Combine(path, "backup"), item6);
				if (File.Exists(text3))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text4));
					File.Move(text3, text4);
				}
				string text5 = SafePath(text, item6);
				if (File.Exists(text5))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text3));
					File.Move(text5, text3);
				}
				progress?.Invoke(++num4 * 9900 / list3.Count, 10000);
			}
			phase?.Invoke("正在完成资源安装");
			DurableWrite(Path.Combine(path, "committed.pending"), "ok");
			File.Move(Path.Combine(path, "committed.pending"), Path.Combine(path, "committed"));
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				Recover(root);
			}
		}
		try
		{
			Recover(root);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
