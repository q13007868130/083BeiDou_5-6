using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public sealed class McvPlayback : IDisposable
{
	private sealed class Slot
	{
		public byte[] pixels;

		public ulong start;
	}

	private readonly object gate = new object();

	private readonly Slot[] slots = new Slot[3];

	private readonly Thread worker;

	private readonly byte[] data;

	private readonly Stopwatch clock = new Stopwatch();

	private bool stopped;

	private bool complete;

	private Exception error;

	public McvVideo Video { get; }

	public Exception Error
	{
		get
		{
			lock (gate)
			{
				return error;
			}
		}
	}

	public bool Finished
	{
		get
		{
			lock (gate)
			{
				return complete && (error != null || Elapsed >= Video.DurationNanoseconds);
			}
		}
	}

	private ulong Elapsed => (ulong)(clock.Elapsed.TotalSeconds * 1000000000.0);

	[DllImport("mapledash_mcv", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr mcv_create(uint codec, uint width, uint height, int alpha);

	[DllImport("mapledash_mcv", CallingConvention = CallingConvention.Cdecl)]
	private static extern void mcv_destroy(IntPtr decoder);

	[DllImport("mapledash_mcv", CallingConvention = CallingConvention.Cdecl)]
	private static extern int mcv_decode(IntPtr decoder, [In] byte[] bytes, uint length, uint colorOffset, uint colorSize, uint alphaOffset, uint alphaSize, [Out] byte[] output, uint capacity);

	public McvPlayback(byte[] bytes)
	{
		Video = McvVideo.Parse(bytes);
		data = bytes;
		worker = new Thread(Decode)
		{
			IsBackground = true,
			Name = "MCV decoder"
		};
		worker.Start();
	}

	private void Decode()
	{
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = mcv_create(Video.FourCc, Video.Width, Video.Height, Video.Flags & 1);
			if (intPtr == IntPtr.Zero)
			{
				throw new InvalidDataException("MCV decoder initialization failed");
			}
			McvVideo.Frame[] frames = Video.Frames;
			foreach (McvVideo.Frame frame in frames)
			{
				lock (gate)
				{
					if (stopped)
					{
						break;
					}
				}
				byte[] array = new byte[checked(Video.Width * Video.Height * 4)];
				if (mcv_decode(intPtr, data, (uint)data.Length, frame.colorOffset, frame.colorSize, frame.alphaOffset, frame.alphaSize, array, (uint)array.Length) == 0)
				{
					throw new InvalidDataException("MCV VPX frame decode failed");
				}
				lock (gate)
				{
					int num;
					while ((num = Array.FindIndex(slots, (Slot s) => s == null)) < 0 && !stopped)
					{
						Monitor.Wait(gate);
					}
					if (stopped)
					{
						break;
					}
					slots[num] = new Slot
					{
						pixels = array,
						start = frame.startNanoseconds
					};
					if (!clock.IsRunning)
					{
						clock.Start();
					}
				}
			}
		}
		catch (Exception ex)
		{
			lock (gate)
			{
				error = ex;
			}
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				mcv_destroy(intPtr);
			}
			lock (gate)
			{
				complete = true;
				Monitor.PulseAll(gate);
			}
		}
	}

	public byte[] TakeFrame()
	{
		lock (gate)
		{
			if (stopped || (complete && Elapsed >= Video.DurationNanoseconds))
			{
				return null;
			}
			ulong elapsed = Elapsed;
			int num = -1;
			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i] != null && slots[i].start <= elapsed && (num < 0 || slots[i].start >= slots[num].start))
				{
					num = i;
				}
			}
			if (num < 0)
			{
				return null;
			}
			Slot slot = slots[num];
			for (int j = 0; j < slots.Length; j++)
			{
				if (slots[j] != null && slots[j].start < slot.start)
				{
					slots[j] = null;
				}
			}
			slots[num] = null;
			Monitor.PulseAll(gate);
			return slot.pixels;
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			stopped = true;
			Monitor.PulseAll(gate);
		}
		worker.Join();
		lock (gate)
		{
			Array.Clear(slots, 0, slots.Length);
			clock.Stop();
		}
	}
}
