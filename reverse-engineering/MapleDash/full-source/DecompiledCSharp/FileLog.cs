using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

public static class FileLog
{
	public const string PrefKey = "log_to_file";

	private const string Folder = "logs";

	private const int MaxPending = 20000;

	private const int FlushMs = 300;

	private const int BatchLines = 400;

	private static readonly ConcurrentQueue<(DateTime date, string text)> pending = new ConcurrentQueue<(DateTime, string)>();

	private static readonly object gate = new object();

	private static readonly object writeGate = new object();

	private static readonly StringBuilder scratch = new StringBuilder(8192);

	private static Thread worker;

	private static volatile bool running;

	private static string dir;

	private static string path;

	private static int lost;

	private static bool booted;

	private static string device;

	private static int nextOpenTry;

	public static bool Enabled { get; private set; }

	public static string Dir => dir ?? (dir = Path.Combine(Application.persistentDataPath, "logs"));

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void AutoBoot()
	{
		Boot();
	}

	public static void Boot()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected Obj, but got Unknown
		if (booted)
		{
			return;
		}
		booted = true;
		try
		{
			Application.logMessageReceivedThreaded += OnLog;
			Application.quitting += FlushNow;
			Enabled = PlayerPrefs.GetInt("log_to_file", 1) != 0;
			if (Enabled)
			{
				Open();
			}
		}
		catch (Exception)
		{
			Enabled = false;
		}
	}

	public static void SetEnabled(bool on)
	{
		PlayerPrefs.SetInt("log_to_file", on ? 1 : 0);
		PlayerPrefs.Save();
		if (on != Enabled)
		{
			if (on)
			{
				Enabled = true;
				Open();
				Note("日志记录已开启");
			}
			else
			{
				Note("日志记录已关闭");
				FlushNow();
				Enabled = false;
			}
		}
	}

	public static bool Toggle()
	{
		SetEnabled(!Enabled);
		return Enabled;
	}

	private static void Open()
	{
		try
		{
			Directory.CreateDirectory(Dir);
			lock (gate)
			{
				path = DailyPath(DateTime.Now);
			}
			if (worker == null || !running)
			{
				running = true;
				worker = new Thread(Loop)
				{
					IsBackground = true,
					Name = "FileLog"
				};
				worker.Start();
			}
			Append(SessionHeader() + "\n", DateTime.Now.Date);
		}
		catch (Exception)
		{
			path = null;
		}
	}

	private static void EnsureOpen()
	{
		if (path == null)
		{
			int tickCount = Environment.TickCount;
			if (tickCount >= nextOpenTry)
			{
				nextOpenTry = tickCount + 5000;
				Open();
			}
		}
	}

	private static void OnLog(string condition, string stackTrace, LogType type)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Invalid comparison between Unknown and I4
		if (!Enabled)
		{
			return;
		}
		if (path == null)
		{
			EnsureOpen();
		}
		if (pending.Count >= 20000)
		{
			Interlocked.Increment(ref lost);
			return;
		}
		DateTime now = DateTime.Now;
		StringBuilder stringBuilder = new StringBuilder(condition?.Length ?? 96);
		stringBuilder.Append(now.ToString("HH:mm:ss.fff")).Append(' ');
		stringBuilder.Append(Level(type)).Append(' ');
		stringBuilder.Append(condition);
		if (((int)type == 0 || (int)type == 4 || (int)type == 1) && !string.IsNullOrEmpty(stackTrace))
		{
			stringBuilder.Append('\n');
			stringBuilder.Append((stackTrace.Length > 2000) ? (stackTrace.Substring(0, 2000) + "…") : stackTrace);
		}
		pending.Enqueue((now.Date, stringBuilder.ToString()));
	}

	private static char Level(LogType t)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		switch ((int)t)
		{
		case 2:
			return 'W';
		case 0:
		case 1:
		case 4:
			return 'E';
		default:
			return 'I';
		}
	}

	public static void Note(string text)
	{
		if (Enabled && !string.IsNullOrEmpty(text))
		{
			pending.Enqueue((DateTime.Now.Date, "\n===== " + text + " =====\n"));
		}
	}

	private static string DeviceStatic()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (device != null)
		{
			return device;
		}
		StringBuilder stringBuilder = new StringBuilder(320);
		stringBuilder.Append("MapleDash ").Append(Application.version).Append(" · ")
			.Append(Application.platform)
			.Append(" · ")
			.Append(Application.identifier)
			.Append('\n');
		try
		{
			stringBuilder.Append("设备 ").Append(SystemInfo.deviceModel).Append(" / ")
				.Append(SystemInfo.operatingSystem)
				.Append('\n');
			stringBuilder.Append("CPU ").Append(SystemInfo.processorType).Append(" ×")
				.Append(SystemInfo.processorCount)
				.Append(" / 内存 ")
				.Append(SystemInfo.systemMemorySize)
				.Append("MB\n");
			stringBuilder.Append("GPU ").Append(SystemInfo.graphicsDeviceName).Append(" / 显存 ")
				.Append(SystemInfo.graphicsMemorySize)
				.Append("MB\n");
		}
		catch (Exception)
		{
		}
		return device = stringBuilder.ToString();
	}

	public static string DeviceSummary()
	{
		StringBuilder stringBuilder = new StringBuilder(DeviceStatic(), 460);
		try
		{
			UiKit.RefreshMetrics();
			stringBuilder.Append("屏幕 ").Append(Screen.width).Append('×')
				.Append(Screen.height)
				.Append(" dpi=")
				.Append(Mathf.RoundToInt(Screen.dpi))
				.Append(" 虚拟 ")
				.Append(Mathf.RoundToInt(UiKit.W))
				.Append('×')
				.Append(Mathf.RoundToInt(UiKit.H))
				.Append('\n');
		}
		catch (Exception)
		{
		}
		return stringBuilder.ToString();
	}

	private static string SessionHeader()
	{
		return "会话开始 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + DeviceSummary();
	}

	private static void Loop()
	{
		while (running)
		{
			Thread.Sleep(300);
			Drain();
		}
	}

	private static void Drain()
	{
		if (!Enabled && pending.IsEmpty)
		{
			return;
		}
		lock (writeGate)
		{
			if (!pending.TryPeek(out (DateTime, string) result))
			{
				return;
			}
			scratch.Length = 0;
			for (int i = 0; i < 400; i++)
			{
				if (!pending.TryPeek(out (DateTime, string) result2))
				{
					break;
				}
				if (!(result2.Item1 == result.Item1))
				{
					break;
				}
				if (!pending.TryDequeue(out (DateTime, string) result3))
				{
					break;
				}
				scratch.Append(result3.Item2).Append('\n');
			}
			int num = Interlocked.Exchange(ref lost, 0);
			if (num > 0)
			{
				scratch.Append("（日志积压，丢弃 ").Append(num).Append(" 行）\n");
			}
			if (scratch.Length > 0)
			{
				Append(scratch.ToString(), result.Item1);
			}
		}
	}

	private static string DailyPath(DateTime date)
	{
		return Path.Combine(Dir, "client-" + date.ToString("yyyy-MM-dd") + ".txt");
	}

	private static void Append(string text, DateTime date)
	{
		try
		{
			lock (gate)
			{
				Directory.CreateDirectory(Dir);
				path = DailyPath(date);
				using FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				fileStream.Write(bytes, 0, bytes.Length);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void FlushNow()
	{
		try
		{
			lock (writeGate)
			{
				while (!pending.IsEmpty)
				{
					Drain();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static List<string> HistoryFiles()
	{
		List<string> list = new List<string>();
		try
		{
			if (!Directory.Exists(Dir))
			{
				return list;
			}
			list.AddRange(Directory.GetFiles(Dir, "client-????-??-??.txt"));
			list.AddRange(Directory.GetFiles(Dir, "client.*.txt"));
			string item = Path.Combine(Dir, "client.txt");
			if (File.Exists(item))
			{
				list.Add(item);
			}
			list.Sort((string a, string b) => File.GetLastWriteTimeUtc(a).CompareTo(File.GetLastWriteTimeUtc(b)));
		}
		catch (Exception)
		{
		}
		return list;
	}

	public static string ReadShared(string file, int maxChars)
	{
		try
		{
			using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			string text = streamReader.ReadToEnd();
			if (maxChars > 0 && text.Length > maxChars)
			{
				return text.Substring(text.Length - maxChars);
			}
			return text;
		}
		catch (Exception)
		{
			return "";
		}
	}
}
