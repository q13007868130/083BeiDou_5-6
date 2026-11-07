using System.Collections.Generic;
using MapleDash.Wz;
using UnityEngine;

public static class NativeBossVideoEffect
{
	private static readonly Dictionary<string, int> markers = new Dictionary<string, int>();

	private static Dictionary<string, object> videos;

	public static int Marker(string path)
	{
		if (markers.TryGetValue(path, out var value))
		{
			return value;
		}
		ImgNode imgNode = WzAssets.FileRoot("Map/Effect.img").Get(path + "/0");
		if (imgNode == null)
		{
			value = -1;
		}
		else
		{
			ImgNode imgNode2 = WzAssets.Canvas(imgNode);
			value = NativeVideoMarker.Decode(WzAssets.Pixels(imgNode), imgNode2.Width, imgNode2.Height);
		}
		markers[path] = value;
		return value;
	}

	public static bool Handle(string path)
	{
		int num = Marker(path);
		if (num == 33)
		{
			NativeSkillVideo.Stop(NativeSkillVideo.Channel.BossScene);
			return true;
		}
		if (num < 1)
		{
			return false;
		}
		if (videos == null)
		{
			TextAsset val = Resources.Load<TextAsset>("NativeBossVideos");
			if ((Object)(object)val == (Object)null)
			{
				return false;
			}
			videos = (Dictionary<string, object>)MiniJson.Parse(val.text);
		}
		if (videos.TryGetValue(num.ToString(), out var value))
		{
			return NativeSkillVideo.QueueBoss((string)value);
		}
		return false;
	}
}
