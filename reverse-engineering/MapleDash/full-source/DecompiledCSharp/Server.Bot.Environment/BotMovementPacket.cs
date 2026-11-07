using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Bot.Environment;

public static class BotMovementPacket
{
	private static BinaryReader Open(byte[] bytes)
	{
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes, writable: false));
		Skip(binaryReader, 9);
		return binaryReader;
	}

	private static void Skip(BinaryReader r, int n)
	{
		if (r.ReadBytes(n).Length != n)
		{
			throw new EndOfStreamException();
		}
	}

	public static List<BotMovementRecording.Command> Deconstruct(byte[] bytes)
	{
		using BinaryReader binaryReader = Open(bytes);
		List<BotMovementRecording.Command> list = new List<BotMovementRecording.Command>();
		int num = binaryReader.ReadSByte();
		for (int i = 0; i < num; i++)
		{
			sbyte b = binaryReader.ReadSByte();
			sbyte stance = 0;
			short x = 0;
			short y = 0;
			short vx = 0;
			short vy = 0;
			short foothold = 0;
			short duration = 0;
			switch (b)
			{
			case 0:
			case 5:
			case 17:
				x = binaryReader.ReadInt16();
				y = binaryReader.ReadInt16();
				vx = binaryReader.ReadInt16();
				vy = binaryReader.ReadInt16();
				foothold = binaryReader.ReadInt16();
				stance = binaryReader.ReadSByte();
				duration = binaryReader.ReadInt16();
				break;
			case 1:
			case 2:
			case 6:
			case 12:
			case 13:
			case 16:
			case 18:
			case 19:
			case 20:
			case 22:
				Skip(binaryReader, 4);
				stance = binaryReader.ReadSByte();
				duration = binaryReader.ReadInt16();
				break;
			case 3:
			case 4:
			case 7:
			case 8:
			case 9:
			case 11:
				Skip(binaryReader, 8);
				stance = binaryReader.ReadSByte();
				break;
			case 14:
				Skip(binaryReader, 9);
				break;
			case 10:
				binaryReader.ReadByte();
				continue;
			case 15:
				Skip(binaryReader, 12);
				stance = binaryReader.ReadSByte();
				binaryReader.ReadInt16();
				break;
			case 21:
				Skip(binaryReader, 3);
				break;
			default:
				continue;
			}
			list.Add(new BotMovementRecording.Command(b, x, y, vx, vy, foothold, stance, duration));
		}
		return list;
	}

	public static byte[] Reconstruct(IReadOnlyList<BotMovementRecording.Command> commands)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(new byte[9]);
		binaryWriter.Write((byte)commands.Count);
		foreach (BotMovementRecording.Command command in commands)
		{
			binaryWriter.Write(command.command);
			binaryWriter.Write(command.x);
			binaryWriter.Write(command.y);
			binaryWriter.Write(command.vx);
			binaryWriter.Write(command.vy);
			binaryWriter.Write(command.foothold);
			binaryWriter.Write(command.stance);
			binaryWriter.Write(command.duration);
		}
		return memoryStream.ToArray();
	}

	public static byte[] Offset(byte[] bytes, int x, int y, int foothold)
	{
		List<BotMovementRecording.Command> list = Deconstruct(bytes);
		if (list.Count == 0)
		{
			throw new InvalidDataException("Empty movement offset");
		}
		int num = list[0].x - x;
		int num2 = list[0].y - y;
		for (int i = 0; i < list.Count; i++)
		{
			BotMovementRecording.Command command = list[i];
			list[i] = new BotMovementRecording.Command(command.command, (short)(command.x - num), (short)(command.y - num2), command.vx, command.vy, (short)foothold, command.stance, command.duration);
		}
		return Reconstruct(list);
	}

	public static byte[] Apply(byte[] bytes, BotCharacter actor)
	{
		using BinaryReader binaryReader = Open(bytes);
		int num = binaryReader.ReadSByte();
		if (num < 1)
		{
			throw new InvalidDataException("Empty movement");
		}
		for (int i = 0; i < num; i++)
		{
			int num2 = binaryReader.ReadSByte();
			switch (num2)
			{
			case 0:
			case 5:
			case 17:
				actor.x = binaryReader.ReadInt16();
				actor.y = binaryReader.ReadInt16();
				Skip(binaryReader, 6);
				actor.stance = binaryReader.ReadSByte();
				binaryReader.ReadInt16();
				break;
			case 1:
			case 2:
			case 6:
			case 12:
			case 13:
			case 16:
			case 18:
			case 19:
			case 20:
			case 22:
				actor.x += binaryReader.ReadInt16();
				actor.y += binaryReader.ReadInt16();
				actor.stance = binaryReader.ReadSByte();
				binaryReader.ReadInt16();
				break;
			case 3:
			case 4:
			case 7:
			case 8:
			case 9:
				actor.x = binaryReader.ReadInt16();
				actor.y = binaryReader.ReadInt16();
				Skip(binaryReader, 4);
				actor.stance = binaryReader.ReadSByte();
				break;
			case 11:
				Skip(binaryReader, 8);
				actor.stance = binaryReader.ReadSByte();
				break;
			case 14:
				Skip(binaryReader, 9);
				break;
			case 10:
				binaryReader.ReadByte();
				break;
			case 15:
				actor.x = binaryReader.ReadInt16();
				actor.y = binaryReader.ReadInt16();
				Skip(binaryReader, 8);
				actor.stance = binaryReader.ReadSByte();
				binaryReader.ReadInt16();
				break;
			case 21:
				Skip(binaryReader, 3);
				break;
			default:
				throw new InvalidDataException("Unknown movement command: " + num2);
			}
		}
		actor.facing = (((actor.stance & 1) == 0) ? 1 : (-1));
		int num3 = (int)binaryReader.BaseStream.Position - 9;
		byte[] array = new byte[num3];
		Array.Copy(bytes, 9, array, 0, num3);
		return array;
	}
}
