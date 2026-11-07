using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Server.Bot.Environment;

public static class BotMovementRecording
{
	public sealed class Packet
	{
		public long timestamp;

		public byte[] data;
	}

	public readonly struct Command(sbyte command, short x, short y, short vx, short vy, short foothold, sbyte stance, short duration)
	{
		public readonly sbyte command = command;

		public readonly sbyte stance = stance;

		public readonly short x = x;

		public readonly short y = y;

		public readonly short vx = vx;

		public readonly short vy = vy;

		public readonly short foothold = foothold;

		public readonly short duration = duration;
	}

	public sealed class RawPacket
	{
		public long timestamp;

		public int declaredCommands;

		public List<Command> commands;
	}

	public static string ResourcePath(int mapId, string name, string extension)
	{
		if (name.StartsWith("/", StringComparison.Ordinal))
		{
			name = name.Substring(1);
		}
		return "soloMapling/ArtificialPlayer/BotMovementSystem/movementDataPackets/map" + mapId + "/" + name + "." + extension;
	}

	public static List<Packet> ReadBinary(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new FileNotFoundException("Movement recording missing");
		}
		List<Packet> list = new List<Packet>();
		int num = 0;
		while (bytes.Length - num >= 12)
		{
			long num2 = 0L;
			for (int i = 0; i < 8; i++)
			{
				num2 = (num2 << 8) | bytes[num++];
			}
			int num3 = 0;
			for (int j = 0; j < 4; j++)
			{
				num3 = (num3 << 8) | bytes[num++];
			}
			if (num3 < 0)
			{
				throw new InvalidDataException("Negative movement packet length");
			}
			if (num3 > bytes.Length - num)
			{
				break;
			}
			byte[] array = new byte[num3];
			Array.Copy(bytes, num, array, 0, num3);
			num += num3;
			list.Add(new Packet
			{
				timestamp = num2,
				data = array
			});
		}
		return list;
	}

	public static List<RawPacket> ReadCsv(string text)
	{
		if (text == null)
		{
			throw new FileNotFoundException("Movement recording missing");
		}
		using StringReader stringReader = new StringReader(text);
		List<RawPacket> list = new List<RawPacket>();
		string text2 = null;
		while (true)
		{
			string obj = text2 ?? stringReader.ReadLine();
			string text3 = obj;
			if (obj == null)
			{
				break;
			}
			text2 = null;
			long timestamp = long.Parse(text3.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
			text3 = stringReader.ReadLine();
			if (text3 == null)
			{
				break;
			}
			int num = int.Parse(text3.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
			List<Command> list2 = new List<Command>();
			for (int i = 0; i < num; i++)
			{
				text3 = stringReader.ReadLine();
				if (text3 == null)
				{
					break;
				}
				if (!text3.Contains(","))
				{
					text2 = text3;
					break;
				}
				string[] array = text3.Split(',');
				if (array.Length != 8)
				{
					throw new FormatException("Invalid movement packet: " + text3);
				}
				list2.Add(new Command(ParseByte(array[0]), ParseShort(array[1]), ParseShort(array[2]), ParseShort(array[3]), ParseShort(array[4]), ParseShort(array[5]), ParseByte(array[6]), ParseShort(array[7])));
			}
			list.Add(new RawPacket
			{
				timestamp = timestamp,
				declaredCommands = num,
				commands = list2
			});
		}
		return list;
	}

	private static sbyte ParseByte(string value)
	{
		return sbyte.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
	}

	private static short ParseShort(string value)
	{
		return short.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
	}
}
