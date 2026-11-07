using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public static class LogShare
{
	private static string selectedFile;

	private const string Mime = "text/plain";

	public static void Tick()
	{
		string text = Interlocked.Exchange(ref selectedFile, null);
		if (text != null)
		{
			ShareFile(text);
		}
	}

	public static void Share()
	{
		FileLog.FlushNow();
		List<string> list = FileLog.HistoryFiles();
		list.Reverse();
		if (list.Count == 0)
		{
			BottomBar.Toast("还没有日志文件，先玩一会儿再分享");
		}
		else
		{
			BottomBar.Toast("当前平台暂不支持日志选择");
		}
	}

	public static void ShareFile(string selected)
	{
		string text;
		try
		{
			FileLog.FlushNow();
			text = Export(selected);
		}
		catch (Exception ex)
		{
			BottomBar.Toast("日志导出失败：" + ex.Message);
			return;
		}
		BottomBar.Toast("日志已导出：" + text);
	}

	public static string Export(string selected)
	{
		if (!FileLog.HistoryFiles().Contains(selected))
		{
			throw new IOException("日志文件已不存在");
		}
		string text = Path.Combine(FileLog.Dir, "share");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, "mapstory-log-" + Path.GetFileNameWithoutExtension(selected) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
		using (FileStream fileStream = new FileStream(selected, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			using FileStream fileStream2 = new FileStream(text2, FileMode.Create, FileAccess.Write);
			long num = fileStream.Length;
			byte[] array = new byte[65536];
			while (num > 0)
			{
				int num2 = fileStream.Read(array, 0, (int)Math.Min(array.Length, num));
				if (num2 == 0)
				{
					break;
				}
				fileStream2.Write(array, 0, num2);
				num -= num2;
			}
		}
		Cleanup(text);
		return text2;
	}

	private static void Cleanup(string dir)
	{
		try
		{
			List<string> list = new List<string>(Directory.GetFiles(dir, "mapstory-log-*.txt"));
			if (list.Count > 5)
			{
				list.Sort(StringComparer.Ordinal);
				for (int i = 0; i < list.Count - 5; i++)
				{
					File.Delete(list[i]);
				}
			}
		}
		catch (Exception)
		{
		}
	}
}
