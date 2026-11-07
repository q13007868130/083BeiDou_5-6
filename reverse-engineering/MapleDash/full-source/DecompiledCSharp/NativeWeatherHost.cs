using System;
using System.Collections.Generic;

public static class NativeWeatherHost
{
	public static NativeWeatherService Shared;

	public static Func<NativeWeatherService> FromGame;

	public static NativeWeatherService Service
	{
		get
		{
			NativeWeatherService nativeWeatherService = ((FromGame != null) ? FromGame() : null);
			if (nativeWeatherService != null)
			{
				Shared = nativeWeatherService;
				return nativeWeatherService;
			}
			if (Shared == null)
			{
				Shared = Create(PlatformRes.LoadText("NativeWeather"), NowMs());
			}
			return Shared;
		}
	}

	public static long NowMs()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	public static NativeWeatherService Create(string json, long now)
	{
		if (!(MiniJsonDb.Parse(json) is Dictionary<string, object> dictionary) || !dictionary.TryGetValue("regions", out var value) || !(value is List<object> list))
		{
			throw new InvalidOperationException("NativeWeather.json 缺少 regions");
		}
		NativeWeatherRegion[] array = new NativeWeatherRegion[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			Dictionary<string, object> dictionary2 = (Dictionary<string, object>)list[i];
			List<object> list2 = (List<object>)dictionary2["weights"];
			double[] array2 = new double[list2.Count];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = Convert.ToDouble(list2[j]);
			}
			array[i] = new NativeWeatherRegion
			{
				Name = (string)dictionary2["name"],
				Forced = Convert.ToInt32(dictionary2["forced"]),
				Tint = Convert.ToInt32(dictionary2["tint"]),
				Palette = i,
				Weights = array2
			};
		}
		Random random = new Random();
		return new NativeWeatherService(array, now, random.NextDouble);
	}
}
