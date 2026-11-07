using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public static class AdminPng
{
	private static readonly uint[] crcTable = BuildCrcTable();

	public static byte[] Encode(int width, int height, byte[] rgba)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentException("PNG 尺寸非法: " + width + "x" + height);
		}
		if (rgba == null || rgba.Length != checked(width * height * 4))
		{
			throw new ArgumentException("RGBA 长度与尺寸不符");
		}
		List<byte> list = new List<byte>(rgba.Length / 2 + 256);
		list.AddRange(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
		byte[] array = new byte[13];
		WriteInt(array, 0, width);
		WriteInt(array, 4, height);
		array[8] = 8;
		array[9] = 6;
		array[10] = 0;
		array[11] = 0;
		array[12] = 0;
		Chunk(list, "IHDR", array);
		int num;
		byte[] array2;
		checked
		{
			num = width * 4;
			array2 = new byte[height * (num + 1)];
		}
		for (int i = 0; i < height; i++)
		{
			int num2 = i * (num + 1);
			array2[num2] = 0;
			Buffer.BlockCopy(rgba, i * num, array2, num2 + 1, num);
		}
		Chunk(list, "IDAT", Zlib(array2));
		Chunk(list, "IEND", Array.Empty<byte>());
		return list.ToArray();
	}

	private static byte[] Zlib(byte[] raw)
	{
		using MemoryStream memoryStream = new MemoryStream(raw.Length / 2 + 32);
		memoryStream.WriteByte(120);
		memoryStream.WriteByte(156);
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			deflateStream.Write(raw, 0, raw.Length);
		}
		uint num = Adler32(raw);
		memoryStream.WriteByte((byte)(num >> 24));
		memoryStream.WriteByte((byte)(num >> 16));
		memoryStream.WriteByte((byte)(num >> 8));
		memoryStream.WriteByte((byte)num);
		return memoryStream.ToArray();
	}

	private static void Chunk(List<byte> png, string type, byte[] data)
	{
		int num = data.Length;
		png.Add((byte)(num >> 24));
		png.Add((byte)(num >> 16));
		png.Add((byte)(num >> 8));
		png.Add((byte)num);
		byte[] array = new byte[4];
		for (int i = 0; i < 4; i++)
		{
			array[i] = (byte)type[i];
		}
		png.AddRange(array);
		png.AddRange(data);
		uint crc = Crc32Continue(uint.MaxValue, array, 0, 4);
		crc = Crc32Continue(crc, data, 0, data.Length) ^ 0xFFFFFFFFu;
		png.Add((byte)(crc >> 24));
		png.Add((byte)(crc >> 16));
		png.Add((byte)(crc >> 8));
		png.Add((byte)crc);
	}

	private static uint[] BuildCrcTable()
	{
		uint[] array = new uint[256];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num;
			for (int i = 0; i < 8; i++)
			{
				num2 = (((num2 & 1) != 0) ? (0xEDB88320u ^ (num2 >> 1)) : (num2 >> 1));
			}
			array[num] = num2;
		}
		return array;
	}

	public static uint Crc32(byte[] data, int offset, int length)
	{
		return Crc32Continue(uint.MaxValue, data, offset, length) ^ 0xFFFFFFFFu;
	}

	private static uint Crc32Continue(uint crc, byte[] data, int offset, int length)
	{
		for (int i = 0; i < length; i++)
		{
			crc = crcTable[(crc ^ data[offset + i]) & 0xFF] ^ (crc >> 8);
		}
		return crc;
	}

	public static uint Adler32(byte[] data)
	{
		uint num = 1u;
		uint num2 = 0u;
		for (int i = 0; i < data.Length; i++)
		{
			num = (num + data[i]) % 65521;
			num2 = (num2 + num) % 65521;
		}
		return (num2 << 16) | num;
	}

	private static void WriteInt(byte[] buf, int offset, int value)
	{
		buf[offset] = (byte)(value >> 24);
		buf[offset + 1] = (byte)(value >> 16);
		buf[offset + 2] = (byte)(value >> 8);
		buf[offset + 3] = (byte)value;
	}
}
