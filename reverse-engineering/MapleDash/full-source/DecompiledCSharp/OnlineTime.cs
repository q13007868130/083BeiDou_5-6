using System;
using System.Globalization;
using UnityEngine;

public static class OnlineTime
{
	private const string Key = "每日在线时间";

	private const float SaveInterval = 60f;

	private static bool loaded;

	private static int baseSeconds;

	private static float loadedAt;

	private static float lastSaveAt;

	public static int Seconds
	{
		get
		{
			if (!GameSession.InGame)
			{
				return 0;
			}
			if (!loaded)
			{
				Load();
			}
			return baseSeconds + Mathf.FloorToInt(Time.realtimeSinceStartup - loadedAt);
		}
	}

	public static void Tick()
	{
		if (GameSession.InGame)
		{
			if (!loaded)
			{
				Load();
			}
			if (!(Time.realtimeSinceStartup - lastSaveAt < 60f))
			{
				Flush();
			}
		}
	}

	public static void Flush()
	{
		if (loaded && GameSession.InGame)
		{
			int seconds = Seconds;
			Write(seconds);
			baseSeconds = seconds;
			loadedAt = Time.realtimeSinceStartup;
			lastSaveAt = loadedAt;
		}
	}

	public static void Reset()
	{
		loaded = false;
		baseSeconds = 0;
	}

	private static void Load()
	{
		loaded = true;
		baseSeconds = Read();
		loadedAt = (lastSaveAt = Time.realtimeSinceStartup);
	}

	private static ScriptValueRequest Request(bool write, string value)
	{
		return new ScriptValueRequest
		{
			characterId = GameSession.CharacterId,
			account = true,
			period = 1,
			name = "每日在线时间",
			write = write,
			value = value
		};
	}

	private static int Read()
	{
		try
		{
			int result;
			return int.TryParse(GameSession.Net?.ScriptValue(Request(write: false, null))?.value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result) ? Mathf.Max(0, result) : 0;
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[OnlineTime] 读取在线时长失败：" + ex.Message));
			return 0;
		}
	}

	private static void Write(int seconds)
	{
		try
		{
			GameSession.Net?.ScriptValue(Request(write: true, seconds.ToString(CultureInfo.InvariantCulture)));
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[OnlineTime] 保存在线时长失败：" + ex.Message));
		}
	}
}
