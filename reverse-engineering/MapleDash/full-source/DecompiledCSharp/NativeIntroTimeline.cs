using System;
using System.Collections.Generic;
using MapleDash.Wz;

public sealed class NativeIntroTimeline
{
	public sealed class Command
	{
		public int Type;

		public int Start;

		public int X;

		public int Y;

		public int MoveX;

		public int MoveY;

		public int Duration;

		public int Z;

		public int Field;

		public string Visual;

		public string Sound;

		public int Displacement(int distance, long elapsed)
		{
			if (Duration <= 0 || elapsed >= Duration)
			{
				return distance;
			}
			if (elapsed <= 0)
			{
				return 0;
			}
			return distance * (int)elapsed / Duration;
		}
	}

	private readonly List<Command> pending;

	private readonly long started;

	public IReadOnlyList<Command> Commands { get; }

	public NativeIntroTimeline(ImgNode root, long now, int defaultX = 0, int defaultY = 0)
	{
		if (root == null)
		{
			throw new InvalidOperationException("Missing original intro scene");
		}
		List<Command> list = new List<Command>();
		int num = 0;
		while (true)
		{
			ImgNode imgNode = root.Get(num.ToString());
			if (imgNode == null)
			{
				break;
			}
			int num2 = imgNode.Int("type");
			if (num2 != 0 && num2 != 2 && num2 != 5)
			{
				throw new NotSupportedException("Unmigrated intro command type " + num2 + " at " + root.Name + "/" + num);
			}
			list.Add(new Command
			{
				Type = num2,
				Start = imgNode.Int("start"),
				Visual = imgNode.Text("visual"),
				Sound = imgNode.Text("sound"),
				X = imgNode.Int("x", defaultX) + imgNode.Int("dx"),
				Y = imgNode.Int("y", defaultY) + imgNode.Int("dy"),
				MoveX = imgNode.Int("x1"),
				MoveY = imgNode.Int("y1"),
				Duration = imgNode.Int("duration"),
				Z = imgNode.Int("z"),
				Field = imgNode.Int("field")
			});
			num++;
		}
		if (list.Count == 0)
		{
			throw new InvalidOperationException("Empty original intro scene: " + root.Name);
		}
		Commands = list.AsReadOnly();
		pending = new List<Command>(list);
		started = now;
	}

	public void Tick(long now, Action<Command> execute)
	{
		foreach (Command item in new List<Command>(pending))
		{
			if (now - started > item.Start)
			{
				pending.Remove(item);
				execute(item);
			}
		}
	}
}
