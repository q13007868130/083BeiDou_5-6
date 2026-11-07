using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MapleDash.Wz;

public sealed class ImgFile : IDisposable
{
	private static readonly List<byte> key = new List<byte>();

	private static readonly object keyLock = new object();

	private static readonly char[] Cp1252High = new char[32]
	{
		'€', '\u0081', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰',
		'Š', '‹', 'Œ', '\u008d', 'Ž', '\u008f', '\u0090', '‘', '’', '“',
		'”', '•', '–', '—', '\u02dc', '™', 'š', '›', 'œ', '\u009d',
		'ž', 'Ÿ'
	};

	private readonly BinaryReader reader;

	public readonly ImgNode Root;

	internal static byte KeyByte(int index)
	{
		lock (keyLock)
		{
			if (index >= key.Count)
			{
				byte[] array = new byte[32];
				byte[] array2 = new byte[8] { 19, 8, 6, 180, 27, 15, 51, 82 };
				for (int i = 0; i < 8; i++)
				{
					array[i * 4] = array2[i];
				}
				using Aes aes = Aes.Create();
				aes.Key = array;
				aes.Mode = CipherMode.ECB;
				aes.Padding = PaddingMode.None;
				using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
				byte[] array3 = new byte[16];
				byte[] array4 = new byte[4] { 77, 35, 199, 43 };
				for (int j = 0; j < 16; j++)
				{
					array3[j] = ((key.Count == 0) ? array4[j % 4] : key[key.Count - 16 + j]);
				}
				while (key.Count <= index)
				{
					array3 = cryptoTransform.TransformFinalBlock(array3, 0, 16);
					key.AddRange(array3);
				}
			}
			return key[index];
		}
	}

	private static string DecodeCp1252(byte[] bytes)
	{
		char[] array = new char[bytes.Length];
		for (int i = 0; i < bytes.Length; i++)
		{
			byte b = bytes[i];
			array[i] = ((b >= 128 && b <= 159) ? Cp1252High[b - 128] : ((char)b));
		}
		return new string(array);
	}

	public ImgFile(string path)
		: this(File.ReadAllBytes(path), path)
	{
	}

	public ImgFile(byte[] bytes, string path)
	{
		reader = new BinaryReader(new MemoryStream(bytes));
		try
		{
			if (reader.ReadByte() != 115 || ReadString() != "Property")
			{
				throw new InvalidDataException("Expected GMS standalone IMG: " + path);
			}
			reader.ReadUInt16();
			Root = new ImgNode
			{
				Name = Path.GetFileName(path),
				Kind = "Property"
			};
			Properties(Root);
		}
		catch
		{
			reader.Dispose();
			throw;
		}
	}

	private int Int()
	{
		int num = reader.ReadSByte();
		if (num != -128)
		{
			return num;
		}
		return reader.ReadInt32();
	}

	private string ReadString()
	{
		int num = reader.ReadSByte();
		if (num == 0)
		{
			return "";
		}
		bool flag = num > 0;
		int num2 = ((num == -128 || num == 127) ? reader.ReadInt32() : Math.Abs(num));
		if (num2 < 0 || num2 > reader.BaseStream.Length)
		{
			throw new InvalidDataException("IMG string length");
		}
		byte[] array = reader.ReadBytes(checked(num2 * ((!flag) ? 1 : 2)));
		if (array.Length != num2 * ((!flag) ? 1 : 2))
		{
			throw new EndOfStreamException();
		}
		for (int i = 0; i < num2; i++)
		{
			if (flag)
			{
				int num3 = (43690 + i) & 0xFFFF;
				array[2 * i] ^= (byte)((num3 & 0xFF) ^ KeyByte(2 * i));
				array[2 * i + 1] ^= (byte)((num3 >> 8) ^ KeyByte(2 * i + 1));
			}
			else
			{
				array[i] ^= (byte)(((170 + i) & 0xFF) ^ KeyByte(i));
			}
		}
		if (!flag)
		{
			return DecodeCp1252(array);
		}
		return Encoding.Unicode.GetString(array);
	}

	private string StringBlock()
	{
		byte b = reader.ReadByte();
		switch (b)
		{
		case 0:
		case 115:
			return ReadString();
		default:
			throw new InvalidDataException("IMG string marker " + b);
		case 1:
		case 27:
		{
			uint num = reader.ReadUInt32();
			long position = reader.BaseStream.Position;
			reader.BaseStream.Position = num;
			string result = ReadString();
			reader.BaseStream.Position = position;
			return result;
		}
		}
	}

	private void Properties(ImgNode parent)
	{
		int num = Int();
		if (num < 0 || num > reader.BaseStream.Length)
		{
			throw new InvalidDataException("IMG property count");
		}
		for (int i = 0; i < num; i++)
		{
			ImgNode imgNode = new ImgNode
			{
				Name = StringBlock(),
				Parent = parent
			};
			byte b = reader.ReadByte();
			switch (b)
			{
			case 2:
			case 11:
				imgNode.Value = reader.ReadInt16();
				break;
			case 3:
			case 19:
				imgNode.Value = Int();
				break;
			case 20:
			{
				int num4 = reader.ReadSByte();
				imgNode.Value = ((num4 == -128) ? reader.ReadInt64() : num4);
				break;
			}
			case 4:
				imgNode.Value = ((reader.ReadByte() == 128) ? reader.ReadSingle() : 0f);
				break;
			case 5:
				imgNode.Value = reader.ReadDouble();
				break;
			case 8:
				imgNode.Value = StringBlock();
				break;
			case 9:
			{
				uint num2 = reader.ReadUInt32();
				long num3 = reader.BaseStream.Position + num2;
				if (num3 > reader.BaseStream.Length)
				{
					throw new EndOfStreamException();
				}
				Extended(imgNode, num3);
				if (reader.BaseStream.Position > num3)
				{
					throw new InvalidDataException("IMG block overflow");
				}
				reader.BaseStream.Position = num3;
				break;
			}
			default:
				throw new NotSupportedException("IMG property tag " + b);
			case 0:
				break;
			}
			parent.Children.Add(imgNode);
		}
	}

	private void Extended(ImgNode n, long end)
	{
		n.Kind = StringBlock();
		switch (n.Kind)
		{
		case "Property":
			reader.ReadUInt16();
			Properties(n);
			break;
		case "Shape2D#Convex2D":
		{
			int num4 = Int();
			if (num4 < 0 || num4 > reader.BaseStream.Length)
			{
				throw new InvalidDataException("Convex point count");
			}
			for (int i = 0; i < num4; i++)
			{
				ImgNode imgNode = new ImgNode
				{
					Name = i.ToString(),
					Parent = n,
					Kind = StringBlock()
				};
				if (imgNode.Kind != "Shape2D#Vector2D")
				{
					throw new NotSupportedException("Convex child " + imgNode.Kind);
				}
				imgNode.X = Int();
				imgNode.Y = Int();
				n.Children.Add(imgNode);
			}
			break;
		}
		case "Shape2D#Vector2D":
			n.X = Int();
			n.Y = Int();
			break;
		case "UOL":
			reader.ReadByte();
			n.Value = StringBlock();
			break;
		case "Canvas":
			reader.ReadByte();
			if (reader.ReadByte() == 1)
			{
				reader.ReadUInt16();
				Properties(n);
			}
			n.Width = Int();
			n.Height = Int();
			n.Format = Int() + reader.ReadByte();
			reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadByte();
			n.Pixels = reader.ReadBytes(checked((int)(end - reader.BaseStream.Position)));
			break;
		case "Sound_DX8":
		case "Sound":
		{
			reader.ReadByte();
			int num2 = Int();
			n.SoundLengthMs = Int();
			long num3 = end - reader.BaseStream.Position - num2;
			if (num2 < 0 || n.SoundLengthMs < 0 || num3 < 0 || num3 > int.MaxValue)
			{
				throw new InvalidDataException("IMG sound length mismatch: " + n.Name);
			}
			n.SoundHeader = reader.ReadBytes((int)num3);
			n.SoundData = reader.ReadBytes(num2);
			if (n.SoundData.Length != num2)
			{
				throw new EndOfStreamException();
			}
			break;
		}
		case "Canvas#Video":
		{
			reader.ReadByte();
			if (reader.ReadByte() == 1)
			{
				reader.ReadUInt16();
				Properties(n);
			}
			n.VideoType = reader.ReadByte();
			int num = Int();
			if (num < 0 || num > end - reader.BaseStream.Position)
			{
				throw new InvalidDataException("IMG video length mismatch: " + n.Name);
			}
			n.VideoData = reader.ReadBytes(num);
			if (reader.BaseStream.Position != end)
			{
				reader.BaseStream.Position = end;
			}
			break;
		}
		case "RawData":
			n.Value = reader.ReadBytes(checked((int)(end - reader.BaseStream.Position)));
			break;
		default:
			throw new NotSupportedException("IMG extended type " + n.Kind);
		}
	}

	public void Dispose()
	{
		reader.Dispose();
	}
}
