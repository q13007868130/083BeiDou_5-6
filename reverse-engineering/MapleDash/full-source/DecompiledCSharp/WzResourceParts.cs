using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public sealed class WzResourceParts : IDisposable
{
	private sealed class Member
	{
		public string Path;

		public int Number;

		public ZipArchive Archive;
	}

	public const string InfoEntry = "resource-pack.json";

	public const string ManifestEntry = "resource-manifest.json";

	public const string PatchEntry = "resource-patch.json";

	private const int ManifestLimit = 67108864;

	private readonly List<Member> members = new List<Member>();

	private Dictionary<string, ZipArchiveEntry> lookup;

	private string manifest;

	private int dataEntries = -1;

	public int PartCount => members.Count;

	public bool IsSplit => members.Count > 1;

	public string ManifestText
	{
		get
		{
			if (manifest != null)
			{
				return manifest;
			}
			ZipArchiveEntry entry = members[0].Archive.GetEntry("resource-manifest.json");
			if (entry == null || entry.Length > 67108864)
			{
				throw new InvalidDataException("资源包缺少有效清单");
			}
			using (StreamReader streamReader = new StreamReader(entry.Open(), Encoding.UTF8))
			{
				manifest = streamReader.ReadToEnd();
			}
			if (!(MiniJson.Parse(manifest) is Dictionary<string, object> dictionary) || !(dictionary["files"] is List<object> { Count: not 0 }))
			{
				throw new InvalidDataException("资源清单没有文件");
			}
			return manifest;
		}
	}

	public bool IsPatch => members[0].Archive.GetEntry("resource-patch.json") != null;

	public int DataEntryCount
	{
		get
		{
			if (dataEntries >= 0)
			{
				return dataEntries;
			}
			int num = 0;
			foreach (Member member in members)
			{
				foreach (ZipArchiveEntry entry in member.Archive.Entries)
				{
					if (entry.FullName.StartsWith("Data/", StringComparison.Ordinal) && !entry.FullName.EndsWith("/", StringComparison.Ordinal))
					{
						num++;
					}
				}
			}
			dataEntries = num;
			return num;
		}
	}

	private WzResourceParts()
	{
	}

	public string PartPath(int index)
	{
		return members[index].Path;
	}

	public int PartNumber(int index)
	{
		return members[index].Number;
	}

	public ZipArchive PartArchive(int index)
	{
		return members[index].Archive;
	}

	public static bool Exists(string baseZipPath)
	{
		return Discover(baseZipPath).Length != 0;
	}

	public static string[] Discover(string baseZipPath)
	{
		string text = Path.GetDirectoryName(baseZipPath);
		if (string.IsNullOrEmpty(text))
		{
			text = ".";
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseZipPath);
		List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
		if (Directory.Exists(text))
		{
			string[] files = Directory.GetFiles(text, fileNameWithoutExtension + ".part*.zip");
			foreach (string text2 in files)
			{
				if (TryPartNumber(Path.GetFileName(text2), fileNameWithoutExtension, out var number))
				{
					list.Add(new KeyValuePair<int, string>(number, text2));
				}
			}
		}
		bool flag = File.Exists(baseZipPath);
		if (list.Count == 0)
		{
			if (!flag)
			{
				return Array.Empty<string>();
			}
			return new string[1] { baseZipPath };
		}
		list.Sort((KeyValuePair<int, string> a, KeyValuePair<int, string> b) => a.Key.CompareTo(b.Key));
		if (flag)
		{
			DateTime dateTime = DateTime.MinValue;
			foreach (KeyValuePair<int, string> item in list)
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(item.Value);
				if (lastWriteTimeUtc > dateTime)
				{
					dateTime = lastWriteTimeUtc;
				}
			}
			if (File.GetLastWriteTimeUtc(baseZipPath) > dateTime)
			{
				return new string[1] { baseZipPath };
			}
		}
		string[] array = new string[list.Count];
		for (int num = 0; num < list.Count; num++)
		{
			array[num] = list[num].Value;
		}
		return array;
	}

	private static bool TryPartNumber(string name, string stem, out int number)
	{
		number = 0;
		string text = stem + ".part";
		string text2 = ".zip";
		if (name.Length <= text.Length + text2.Length)
		{
			return false;
		}
		if (!name.StartsWith(text, StringComparison.Ordinal) || !name.EndsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text3 = name.Substring(text.Length, name.Length - text.Length - text2.Length);
		string text4 = text3;
		foreach (char c in text4)
		{
			if (c < '0' || c > '9')
			{
				return false;
			}
		}
		if (int.TryParse(text3, out number))
		{
			return number > 0;
		}
		return false;
	}

	public static WzResourceParts Open(string baseZipPath)
	{
		string[] array = Discover(baseZipPath);
		if (array.Length != 0)
		{
			return Open(array);
		}
		return null;
	}

	public static WzResourceParts Open(string[] paths)
	{
		WzResourceParts wzResourceParts = new WzResourceParts();
		try
		{
			List<int> list = new List<int>();
			foreach (string text in paths)
			{
				Member member = new Member
				{
					Path = text,
					Number = 1
				};
				try
				{
					member.Archive = ZipFile.OpenRead(text);
				}
				catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
				{
					throw new InvalidDataException("资源包 " + Path.GetFileName(text) + " 打不开（分包没传完整或已损坏），请重新传输该包", ex);
				}
				wzResourceParts.members.Add(member);
				int count = 1;
				ZipArchiveEntry entry = member.Archive.GetEntry("resource-pack.json");
				if (entry != null)
				{
					ReadInfo(entry, member.Path, out member.Number, out count);
				}
				list.Add(count);
			}
			wzResourceParts.Validate(paths.Length, list);
			return wzResourceParts;
		}
		catch
		{
			wzResourceParts.Dispose();
			throw;
		}
	}

	private static void ReadInfo(ZipArchiveEntry entry, string path, out int index, out int count)
	{
		if (entry.Length > 1048576)
		{
			throw new InvalidDataException("分包描述文件异常: " + Path.GetFileName(path));
		}
		string src;
		using (StreamReader streamReader = new StreamReader(entry.Open(), Encoding.UTF8))
		{
			src = streamReader.ReadToEnd();
		}
		index = (count = 0);
		try
		{
			Dictionary<string, object> obj = (MiniJson.Parse(src) as Dictionary<string, object>) ?? throw new FormatException();
			if (obj.TryGetValue("index", out var value))
			{
				index = Convert.ToInt32(value);
			}
			if (obj.TryGetValue("count", out var value2))
			{
				count = Convert.ToInt32(value2);
			}
		}
		catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException || ex is ArgumentException)
		{
			throw new InvalidDataException("分包描述文件损坏: " + Path.GetFileName(path), ex);
		}
		if (index < 1 || count < 1 || index > count)
		{
			throw new InvalidDataException("分包描述包号非法: " + Path.GetFileName(path));
		}
	}

	private void Validate(int found, List<int> counts)
	{
		foreach (int count in counts)
		{
			if (count != found)
			{
				throw new InvalidDataException("资源包不完整：分包声明共 " + count + " 包，实际只找到 " + found + " 包（" + MissingNames(count, found) + "），请把所有分包都传到同一目录");
			}
		}
		members.Sort((Member a, Member b) => a.Number.CompareTo(b.Number));
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Member member in members)
		{
			if (!hashSet.Add(member.Number))
			{
				throw new InvalidDataException("分包包号重复：" + member.Number + "（同名不同号的旧包残留？）");
			}
		}
		for (int num = 0; num < members.Count; num++)
		{
			if (members[num].Number != num + 1)
			{
				throw new InvalidDataException("分包包号不连续：缺少第 " + (num + 1) + " 包");
			}
		}
		if (members[0].Archive.GetEntry("resource-manifest.json") == null)
		{
			throw new InvalidDataException("资源包缺少清单（第 1 包里没有 resource-manifest.json）：请用 tools/pack_resources.sh 重新打包");
		}
	}

	private static string MissingNames(int declared, int found)
	{
		List<string> list = new List<string>();
		for (int i = found + 1; i <= declared; i++)
		{
			if (list.Count >= 3)
			{
				break;
			}
			list.Add("part" + i.ToString("00"));
		}
		if (list.Count != 0)
		{
			return "缺 " + string.Join("、", list);
		}
		return "检查是否被覆盖";
	}

	public ZipArchiveEntry FindEntry(string name)
	{
		if (lookup == null)
		{
			lookup = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
			foreach (Member member in members)
			{
				foreach (ZipArchiveEntry entry in member.Archive.Entries)
				{
					if (!entry.FullName.EndsWith("/", StringComparison.Ordinal) && !lookup.ContainsKey(entry.FullName))
					{
						lookup[entry.FullName] = entry;
					}
				}
			}
		}
		if (!lookup.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	public void Dispose()
	{
		foreach (Member member in members)
		{
			try
			{
				member.Archive?.Dispose();
			}
			catch (IOException)
			{
			}
		}
		members.Clear();
		lookup = null;
	}
}
