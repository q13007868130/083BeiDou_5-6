using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace MapleDash.Wz;

public sealed class ImgNode
{
	public string Name;

	public string Kind;

	public object Value;

	public ImgNode Parent;

	public readonly List<ImgNode> Children = new List<ImgNode>();

	public int Width;

	public int Height;

	public int Format;

	public int X;

	public int Y;

	public int SoundLengthMs;

	public byte[] SoundHeader;

	public byte[] SoundData;

	public int VideoType;

	public byte[] VideoData;

	internal byte[] Pixels;

	public ImgNode Get(string path)
	{
		ImgNode imgNode = this;
		string[] array = path.Split('/');
		foreach (string part in array)
		{
			if (!(part == "") && !(part == "."))
			{
				imgNode = ((!(part == "..")) ? imgNode?.TryResolve()?.Children.Find((ImgNode c) => c.Name == part) : imgNode?.Parent);
			}
		}
		return imgNode;
	}

	public int Int(string path, int fallback = 0)
	{
		object obj = Get(path)?.Value;
		if (obj != null)
		{
			return Convert.ToInt32(obj);
		}
		return fallback;
	}

	public bool TryVector(string path, out int x, out int y)
	{
		x = (y = 0);
		ImgNode imgNode = (string.IsNullOrEmpty(path) ? TryResolve() : Get(path)?.TryResolve());
		if (imgNode == null)
		{
			return false;
		}
		if (imgNode.Kind == "Shape2D#Vector2D")
		{
			x = imgNode.X;
			y = imgNode.Y;
			return true;
		}
		object obj = imgNode.Get("x")?.Value;
		object obj2 = imgNode.Get("y")?.Value;
		if (obj == null || obj2 == null)
		{
			return false;
		}
		x = Convert.ToInt32(obj);
		y = Convert.ToInt32(obj2);
		return true;
	}

	public string Text(string path, string fallback = "")
	{
		return (Get(path)?.Value as string) ?? fallback;
	}

	public ImgNode TryResolve()
	{
		ImgNode imgNode = this;
		for (int i = 0; i < 64; i++)
		{
			if (imgNode == null)
			{
				return null;
			}
			if (imgNode.Kind != "UOL")
			{
				return imgNode;
			}
			imgNode = imgNode.Parent?.Get((string)imgNode.Value);
		}
		return null;
	}

	public ImgNode Resolve()
	{
		return TryResolve() ?? throw new InvalidDataException("Missing UOL target: " + Name);
	}

	public byte[] Rgba()
	{
		if (Kind != "Canvas")
		{
			throw new InvalidDataException("Not a Canvas: " + Name);
		}
		byte[] array = Pixels;
		if (array.Length < 2)
		{
			throw new InvalidDataException("Empty Canvas");
		}
		if (array[0] != 120 || ((array[0] << 8) + array[1]) % 31 != 0)
		{
			using BinaryReader binaryReader = new BinaryReader(new MemoryStream(array));
			using MemoryStream memoryStream = new MemoryStream();
			while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
			{
				int num = binaryReader.ReadInt32();
				if (num <= 0 || num > binaryReader.BaseStream.Length - binaryReader.BaseStream.Position)
				{
					throw new InvalidDataException("Invalid listWz chunk");
				}
				byte[] array2 = binaryReader.ReadBytes(num);
				for (int i = 0; i < num; i++)
				{
					array2[i] ^= ImgFile.KeyByte(i);
				}
				memoryStream.Write(array2, 0, array2.Length);
			}
			array = memoryStream.ToArray();
		}
		byte[] array3;
		using (MemoryStream stream = new MemoryStream(array, 2, array.Length - 2))
		{
			using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
			using MemoryStream memoryStream2 = new MemoryStream();
			deflateStream.CopyTo(memoryStream2);
			array3 = memoryStream2.ToArray();
		}
		int num2;
		int num3;
		byte[] array4;
		checked
		{
			num2 = Width * Height;
			num3 = ((Format == 2) ? 4 : 2);
			if (Format != 1 && Format != 2 && Format != 513)
			{
				throw new NotSupportedException("Canvas format " + Format);
			}
			if (array3.Length < num2 * num3)
			{
				throw new InvalidDataException("Canvas pixel length mismatch " + Name + ": " + array3.Length + " != " + unchecked(num2 * num3));
			}
			array4 = new byte[num2 * 4];
		}
		for (int j = 0; j < num2; j++)
		{
			int num4 = j * num3;
			int num5 = j * 4;
			if (Format == 2)
			{
				array4[num5] = array3[num4 + 2];
				array4[num5 + 1] = array3[num4 + 1];
				array4[num5 + 2] = array3[num4];
				array4[num5 + 3] = array3[num4 + 3];
			}
			else if (Format == 1)
			{
				array4[num5] = (byte)((array3[num4 + 1] & 0xF) * 17);
				array4[num5 + 1] = (byte)((array3[num4] >> 4) * 17);
				array4[num5 + 2] = (byte)((array3[num4] & 0xF) * 17);
				array4[num5 + 3] = (byte)((array3[num4 + 1] >> 4) * 17);
			}
			else
			{
				int num6 = array3[num4] | (array3[num4 + 1] << 8);
				array4[num5] = (byte)(((num6 >> 11) & 0x1F) * 8);
				array4[num5 + 1] = (byte)(((num6 >> 5) & 0x3F) * 4);
				array4[num5 + 2] = (byte)((num6 & 0x1F) * 8);
				array4[num5 + 3] = byte.MaxValue;
			}
		}
		return array4;
	}
}
