using System;
using System.Text;

public static class NativeSkillBackdropPacket
{
	public const int MaxDuration = 86400000;

	public static bool ValidPath(string path)
	{
		if (string.IsNullOrEmpty(path) || path.Length > 192 || path.Contains(".."))
		{
			return false;
		}
		foreach (char c in path)
		{
			if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_' && c != '.' && c != '/' && c != '-')
			{
				return false;
			}
		}
		if (path.EndsWith(".mcv", StringComparison.Ordinal))
		{
			int num = path.LastIndexOf('/');
			if (num >= 0)
			{
				if (path.StartsWith("Video/", StringComparison.Ordinal) && num == 5)
				{
					return path.Length > 10;
				}
				return false;
			}
			return path.Length > 4;
		}
		if (path.Contains(".img/"))
		{
			return path.Contains("/");
		}
		return false;
	}

	public static string Normalize(string path)
	{
		path = path?.Trim();
		if (!ValidPath(path))
		{
			return null;
		}
		if (!path.EndsWith(".mcv", StringComparison.Ordinal) || path.Contains("/"))
		{
			return path;
		}
		return "Video/" + path;
	}

	public static bool TryRead(byte[] packet, int offset, out string path, out int duration)
	{
		path = null;
		duration = 0;
		if (packet == null || offset < 0 || offset > packet.Length - 3)
		{
			return false;
		}
		int num = offset + 2;
		if (packet[num++] == 0)
		{
			return true;
		}
		if (packet.Length - num < 2)
		{
			return false;
		}
		int num2 = packet[num] | (packet[num + 1] << 8);
		num += 2;
		if (num2 > 192 || packet.Length - num < num2 + 4)
		{
			return false;
		}
		path = Encoding.ASCII.GetString(packet, num, num2);
		num += num2;
		if (!ValidPath(path))
		{
			path = null;
			return false;
		}
		duration = Math.Max(0, packet[num] | (packet[num + 1] << 8) | (packet[num + 2] << 16) | (packet[num + 3] << 24));
		return true;
	}
}
