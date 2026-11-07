using System;
using System.IO;

public sealed class McvVideo
{
	public sealed class Frame
	{
		public uint colorOffset;

		public uint colorSize;

		public uint alphaOffset;

		public uint alphaSize;

		public ulong startNanoseconds;

		public ulong delayNanoseconds;
	}

	public uint FourCc { get; private set; }

	public ushort Width { get; private set; }

	public ushort Height { get; private set; }

	public byte Flags { get; private set; }

	public ulong DurationNanoseconds { get; private set; }

	public Frame[] Frames { get; private set; }

	private static InvalidDataException Invalid(string detail)
	{
		return new InvalidDataException("MCV: " + detail);
	}

	public static McvVideo Parse(byte[] data)
	{
		if (data == null || data.Length < 36)
		{
			throw Invalid("shorter than fixed header");
		}
		using MemoryStream memoryStream = new MemoryStream(data, writable: false);
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		if (binaryReader.ReadUInt32() != 810959693)
		{
			throw Invalid("signature");
		}
		binaryReader.ReadUInt16();
		ushort num = binaryReader.ReadUInt16();
		McvVideo mcvVideo = new McvVideo
		{
			FourCc = (binaryReader.ReadUInt32() ^ 0xA5A5A5A5u),
			Width = binaryReader.ReadUInt16(),
			Height = binaryReader.ReadUInt16()
		};
		uint num2 = binaryReader.ReadUInt32();
		mcvVideo.Flags = binaryReader.ReadByte();
		memoryStream.Position = 24L;
		ulong num3 = binaryReader.ReadUInt64();
		uint num4 = binaryReader.ReadUInt32();
		if (num < 36 || num > data.Length)
		{
			throw Invalid("header length");
		}
		if (mcvVideo.FourCc != 808996950 && mcvVideo.FourCc != 809062486)
		{
			throw Invalid("codec");
		}
		if (mcvVideo.Width == 0 || mcvVideo.Height == 0 || mcvVideo.Width > 8192 || mcvVideo.Height > 8192)
		{
			throw Invalid("dimensions");
		}
		if (num2 == 0 || num2 > 100000)
		{
			throw Invalid("frame count");
		}
		if ((mcvVideo.Flags & -8) != 0)
		{
			throw Invalid("flags");
		}
		if (num2 * (8 + (((mcvVideo.Flags & 1) != 0) ? 8 : 0) + (((mcvVideo.Flags & 2) != 0) ? 4 : 0) + (((mcvVideo.Flags & 4) != 0) ? 8 : 0)) > data.Length - num)
		{
			throw Invalid("truncated tables");
		}
		mcvVideo.Frames = new Frame[num2];
		memoryStream.Position = num;
		for (int i = 0; i < mcvVideo.Frames.Length; i++)
		{
			mcvVideo.Frames[i] = new Frame
			{
				colorOffset = binaryReader.ReadUInt32(),
				colorSize = binaryReader.ReadUInt32()
			};
		}
		if ((mcvVideo.Flags & 1) != 0)
		{
			Frame[] frames = mcvVideo.Frames;
			foreach (Frame obj in frames)
			{
				obj.alphaOffset = binaryReader.ReadUInt32();
				obj.alphaSize = binaryReader.ReadUInt32();
			}
		}
		checked
		{
			try
			{
				Frame[] frames = mcvVideo.Frames;
				for (int j = 0; j < frames.Length; j = unchecked(j + 1))
				{
					frames[j].delayNanoseconds = (((mcvVideo.Flags & 2) != 0) ? binaryReader.ReadUInt32() : num4) * num3;
				}
				if ((mcvVideo.Flags & 4) != 0)
				{
					frames = mcvVideo.Frames;
					for (int j = 0; j < frames.Length; j = unchecked(j + 1))
					{
						frames[j].startNanoseconds = binaryReader.ReadUInt64() * num3;
					}
				}
				else
				{
					ulong num5 = 0uL;
					frames = mcvVideo.Frames;
					foreach (Frame frame in frames)
					{
						frame.startNanoseconds = num5;
						num5 += frame.delayNanoseconds;
					}
				}
				uint dataStart = (uint)memoryStream.Position;
				frames = mcvVideo.Frames;
				foreach (Frame frame2 in frames)
				{
					frame2.colorOffset = Payload(dataStart, frame2.colorOffset, frame2.colorSize, data.Length);
					if (frame2.alphaSize != 0)
					{
						frame2.alphaOffset = Payload(dataStart, frame2.alphaOffset, frame2.alphaSize, data.Length);
					}
					mcvVideo.DurationNanoseconds = Math.Max(mcvVideo.DurationNanoseconds, frame2.startNanoseconds + frame2.delayNanoseconds);
				}
			}
			catch (OverflowException)
			{
				throw Invalid("integer overflow");
			}
			return mcvVideo;
		}
	}

	private static uint Payload(uint dataStart, uint offset, uint size, int length)
	{
		uint num = checked(dataStart + offset);
		if (num > length || size > length - num)
		{
			throw Invalid("payload out of bounds");
		}
		return num;
	}
}
