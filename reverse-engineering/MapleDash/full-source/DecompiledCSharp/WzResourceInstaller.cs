using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class WzResourceInstaller
{
	public static string Hash(byte[] bytes)
	{
		using SHA256 sHA = SHA256.Create();
		return BitConverter.ToString(sHA.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
	}

	public static void Verify(string root, string manifest, Action<int, int> progress)
	{
		List<object> list = (List<object>)((Dictionary<string, object>)MiniJson.Parse(manifest))["files"];
		int num = 0;
		foreach (Dictionary<string, object> item in list)
		{
			using (FileStream inputStream = File.OpenRead(Path.Combine(root, "Data", (string)item["path"])))
			{
				using SHA256 sHA = SHA256.Create();
				if (BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant() != (string)item["sha256"])
				{
					throw new InvalidDataException("资源校验失败: " + item["path"]);
				}
			}
			progress?.Invoke(++num, list.Count);
		}
	}

	public static void VerifySample(string root, string manifest, int sample, Action<int, int> progress = null)
	{
		List<object> list = (List<object>)((Dictionary<string, object>)MiniJson.Parse(manifest))["files"];
		if (list.Count != 0)
		{
			int num = Math.Max(1, list.Count / Math.Max(1, sample));
			int num2 = -1;
			int num3 = 0;
			int num4 = (list.Count - 1) / num + 1;
			if ((list.Count - 1) % num != 0)
			{
				num4++;
			}
			progress?.Invoke(0, num4);
			for (int i = 0; i < list.Count; i += num)
			{
				VerifyOne(root, (Dictionary<string, object>)list[i]);
				num2 = i;
				progress?.Invoke(++num3, num4);
			}
			if (num2 != list.Count - 1)
			{
				VerifyOne(root, (Dictionary<string, object>)list[list.Count - 1]);
				progress?.Invoke(++num3, num4);
			}
		}
	}

	private static void VerifyOne(string root, Dictionary<string, object> file)
	{
		string path = Path.Combine(root, "Data", (string)file["path"]);
		if (!File.Exists(path))
		{
			throw new FileNotFoundException("资源缺失: " + file["path"]);
		}
		using FileStream inputStream = File.OpenRead(path);
		using SHA256 sHA = SHA256.Create();
		if (BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant() != (string)file["sha256"])
		{
			throw new InvalidDataException("资源校验失败: " + file["path"]);
		}
	}

	private static void RetireDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}
		string retired = path + ".cleanup-" + Guid.NewGuid().ToString("N");
		Directory.Move(path, retired);
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

	public static void CleanupRetired(string root)
	{
		Task.Run(() =>
		{
			try
			{
				string[] array = new string[3] { "Installing.cleanup-", "PreviousData.cleanup-", "ResourcePatchTransaction.cleanup-" };
				foreach (string text in array)
				{
					string[] directories = Directory.GetDirectories(root, text + "*");
					foreach (string path in directories)
					{
						try
						{
							Directory.Delete(path, recursive: true);
						}
						catch (IOException)
						{
						}
						catch (UnauthorizedAccessException)
						{
						}
					}
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		});
	}

	public static void DeleteInstalledArchive(string baseZipPath)
	{
		string[] array = WzResourceParts.Discover(baseZipPath);
		for (int i = 0; i < array.Length; i++)
		{
			DeleteFile(array[i]);
		}
		DeleteFile(baseZipPath);
	}

	private static void DeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	public static string ExtractFull(string zipPath, string root, Action<int, int> progress = null)
	{
		string result;
		using (WzResourceParts wzResourceParts = OpenPack(zipPath))
		{
			if (wzResourceParts.IsPatch)
			{
				throw new InvalidDataException("这里需要全量资源包，不能使用增量补丁");
			}
			result = ExtractAll(wzResourceParts, root, progress);
		}
		DeleteInstalledArchive(zipPath);
		return result;
	}

	private static WzResourceParts OpenPack(string zipPath)
	{
		return WzResourceParts.Open(zipPath) ?? throw new FileNotFoundException("找不到资源包: " + zipPath);
	}

	private static string ExtractAll(WzResourceParts pack, string root, Action<int, int> progress)
	{
		string text = Path.Combine(root, "Data");
		Directory.CreateDirectory(text);
		string value = Path.GetFullPath(text) + Path.DirectorySeparatorChar;
		int dataEntryCount = pack.DataEntryCount;
		int num = 0;
		progress?.Invoke(0, dataEntryCount);
		for (int i = 0; i < pack.PartCount; i++)
		{
			string fileName = Path.GetFileName(pack.PartPath(i));
			foreach (ZipArchiveEntry entry in pack.PartArchive(i).Entries)
			{
				if (!entry.FullName.StartsWith("Data/", StringComparison.Ordinal) || entry.FullName.EndsWith("/", StringComparison.Ordinal))
				{
					continue;
				}
				string fullPath = Path.GetFullPath(Path.Combine(text, entry.FullName.Substring(5)));
				if (!fullPath.StartsWith(value, StringComparison.Ordinal))
				{
					throw new InvalidDataException("资源路径无效");
				}
				Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
				try
				{
					using Stream stream = entry.Open();
					using FileStream destination = File.Create(fullPath);
					stream.CopyTo(destination, 131072);
				}
				catch (InvalidDataException innerException)
				{
					throw new InvalidDataException("资源包 " + fileName + " 已损坏（" + entry.FullName + "），请重新传输该分包", innerException);
				}
				progress?.Invoke(++num, dataEntryCount);
			}
		}
		string manifestText = pack.ManifestText;
		File.WriteAllText(Path.Combine(text, ".resource-manifest.json"), manifestText);
		return manifestText;
	}

	public static string ReadArchiveManifest(string zipPath)
	{
		using WzResourceParts wzResourceParts = OpenPack(zipPath);
		return wzResourceParts.ManifestText;
	}

	private static Dictionary<string, string> VerifiedFiles(string root, string fallbackManifest)
	{
		if (fallbackManifest == null)
		{
			return null;
		}
		try
		{
			string path = Path.Combine(root, "Data/.resource-manifest.json");
			string text = (File.Exists(path) ? File.ReadAllText(path) : fallbackManifest);
			if (File.ReadAllText(Path.Combine(root, "NativeAssetIndex.sha256")).Trim() != Hash(Encoding.UTF8.GetBytes(text)))
			{
				return null;
			}
			Dictionary<string, object> dictionary = (Dictionary<string, object>)MiniJson.Parse(text);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (Dictionary<string, object> item in (List<object>)dictionary["files"])
			{
				dictionary2.Add((string)item["path"], (string)item["sha256"]);
			}
			return dictionary2;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		catch (ArgumentException)
		{
			return null;
		}
		catch (InvalidCastException)
		{
			return null;
		}
		catch (KeyNotFoundException)
		{
			return null;
		}
	}

	public static void Install(string zipPath, string root, string expectedManifest, Action<int, int> progress, Action<string> phase = null, string resourceVersionJson = null, string verifiedBaseManifest = null)
	{
		using (WzResourceParts pack = OpenPack(zipPath))
		{
			InstallPacked(pack, root, expectedManifest, progress, phase, resourceVersionJson, verifiedBaseManifest);
		}
		DeleteInstalledArchive(zipPath);
	}

	private static void InstallPacked(WzResourceParts pack, string root, string expectedManifest, Action<int, int> progress, Action<string> phase, string resourceVersionJson, string verifiedBaseManifest)
	{
		string text = Path.Combine(root, "Installing");
		string text2 = Path.Combine(root, "Data");
		string text3 = Path.Combine(root, "PreviousData");
		string text4 = Path.Combine(root, "NativeAssetIndex.sha256");
		Directory.CreateDirectory(root);
		WzResourcePatchInstaller.Recover(root);
		if (verifiedBaseManifest != null)
		{
			if (pack.IsPatch)
			{
				WzResourcePatchInstaller.Install(pack, root, expectedManifest, verifiedBaseManifest, resourceVersionJson, progress, phase);
				return;
			}
			phase?.Invoke("正在解压资源");
			string text5 = ExtractAll(pack, root, progress);
			if (resourceVersionJson != null)
			{
				File.WriteAllText(Path.Combine(text2, ".resUpdate.json"), resourceVersionJson);
			}
			if (text5 != null)
			{
				File.WriteAllText(text4, Hash(Encoding.UTF8.GetBytes(text5)) + "\n");
			}
			return;
		}
		if (!Directory.Exists(text2) && Directory.Exists(text3))
		{
			Directory.Move(text3, text2);
		}
		phase?.Invoke("正在准备资源安装");
		RetireDirectory(text);
		Directory.CreateDirectory(text);
		try
		{
			string manifestText = pack.ManifestText;
			if (Hash(Encoding.UTF8.GetBytes(manifestText)) != Hash(Encoding.UTF8.GetBytes(expectedManifest)))
			{
				throw new InvalidDataException("资源包版本与当前游戏不匹配");
			}
			bool isPatch = pack.IsPatch;
			List<object> list = (List<object>)((Dictionary<string, object>)MiniJson.Parse(manifestText))["files"];
			Dictionary<string, string> dictionary = (isPatch ? VerifiedFiles(root, verifiedBaseManifest) : null);
			int num = 0;
			byte[] array = new byte[131072];
			phase?.Invoke(isPatch ? "正在应用资源补丁并校验" : "首次安装：解压并校验资源");
			progress?.Invoke(0, list.Count);
			foreach (Dictionary<string, object> item in list)
			{
				string text6 = (string)item["path"];
				string fullPath = Path.GetFullPath(Path.Combine(text, text6));
				if (!fullPath.StartsWith(Path.GetFullPath(text) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				{
					throw new InvalidDataException("资源路径无效");
				}
				ZipArchiveEntry zipArchiveEntry = pack.FindEntry("Data/" + text6);
				if (zipArchiveEntry == null && !isPatch)
				{
					throw new InvalidDataException("资源包缺少文件: " + text6);
				}
				Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
				if (zipArchiveEntry == null && dictionary != null && dictionary.TryGetValue(text6, out var value) && value == (string)item["sha256"])
				{
					phase?.Invoke("正在复制未变化资源（无需重复校验）");
					File.Copy(Path.Combine(text2, text6), fullPath);
					progress?.Invoke(++num, list.Count);
					continue;
				}
				phase?.Invoke((dictionary != null) ? "正在安装并校验新增/修改资源" : "正在安装并完整校验资源");
				using (Stream stream = ((zipArchiveEntry != null) ? zipArchiveEntry.Open() : File.OpenRead(Path.Combine(text2, text6))))
				{
					using FileStream fileStream = File.Create(fullPath);
					using SHA256 sHA = SHA256.Create();
					int num2;
					while ((num2 = stream.Read(array, 0, array.Length)) > 0)
					{
						fileStream.Write(array, 0, num2);
						sHA.TransformBlock(array, 0, num2, array, 0);
					}
					sHA.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
					if (BitConverter.ToString(sHA.Hash).Replace("-", "").ToLowerInvariant() != (string)item["sha256"])
					{
						throw new InvalidDataException("资源校验失败: " + text6);
					}
				}
				progress?.Invoke(++num, list.Count);
			}
			File.WriteAllText(Path.Combine(text, ".resource-manifest.json"), expectedManifest);
			if (resourceVersionJson != null)
			{
				File.WriteAllText(Path.Combine(text, ".resUpdate.json"), resourceVersionJson);
			}
			phase?.Invoke("正在完成资源安装");
			RetireDirectory(text3);
			if (Directory.Exists(text2))
			{
				Directory.Move(text2, text3);
			}
			try
			{
				Directory.Move(text, text2);
				File.WriteAllText(text4 + ".pending", Hash(Encoding.UTF8.GetBytes(expectedManifest)) + "\n");
				if (File.Exists(text4))
				{
					File.Delete(text4);
				}
				File.Move(text4 + ".pending", text4);
			}
			catch
			{
				if (Directory.Exists(text2))
				{
					Directory.Delete(text2, recursive: true);
				}
				if (Directory.Exists(text3))
				{
					Directory.Move(text3, text2);
				}
				throw;
			}
			try
			{
				RetireDirectory(text3);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
		finally
		{
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
		}
	}
}
