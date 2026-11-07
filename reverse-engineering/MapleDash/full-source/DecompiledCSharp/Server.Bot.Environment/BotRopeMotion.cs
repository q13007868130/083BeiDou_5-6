using System;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotRopeMotion
{
	public enum Result
	{
		Hold,
		Climb,
		Land,
		Fall
	}

	public sealed class State
	{
		public MapRope rope;

		public BotNavigationGraph.Point position;

		public int direction;

		public bool climbing;
	}

	private readonly BotGroundIndex ground;

	public const int GrabRange = 10;

	public BotRopeMotion(BotGroundIndex ground)
	{
		this.ground = ground;
	}

	public static int FirstClimbableY(MapRope rope)
	{
		return Math.Min(rope.Bottom, rope.Top + 1);
	}

	public static bool CanGrab(BotNavigationGraph.Point position, MapRope rope)
	{
		if (rope != null && Math.Abs(position.x - rope.x) <= 10 && position.y >= FirstClimbableY(rope))
		{
			return position.y <= rope.Bottom;
		}
		return false;
	}

	public static State Attach(MapRope rope, int y)
	{
		return new State
		{
			rope = rope,
			climbing = true,
			position = new BotNavigationGraph.Point(rope.x, Math.Max(FirstClimbableY(rope), Math.Min(rope.Bottom, y)))
		};
	}

	public BotNavigationGraph.Point? TopExitLanding(MapRope rope)
	{
		if (rope == null)
		{
			return null;
		}
		BotNavigationGraph.Point? result = null;
		int num = int.MaxValue;
		int[] array = new int[3]
		{
			rope.x,
			rope.x - 8,
			rope.x + 8
		};
		foreach (int x in array)
		{
			BotNavigationGraph.Point? point = ground.PointBelow(x, rope.Top - 25);
			if (point.HasValue && point.Value.y >= rope.Top - 24 && point.Value.y <= rope.Top + 20)
			{
				int num2 = Math.Abs(point.Value.y - rope.Top);
				if (num2 < num)
				{
					result = point;
					num = num2;
				}
			}
		}
		return result;
	}

	public Result Step(State state)
	{
		if (state.rope == null)
		{
			state.climbing = false;
			return Result.Fall;
		}
		int num = Math.Sign(state.direction);
		int num2 = state.position.y + num * 5;
		if (num2 <= state.rope.Top)
		{
			BotNavigationGraph.Point? point = TopExitLanding(state.rope);
			if (point.HasValue)
			{
				state.position = point.Value;
				state.climbing = false;
				state.rope = null;
				return Result.Land;
			}
			state.position = new BotNavigationGraph.Point(state.rope.x, FirstClimbableY(state.rope));
			return Result.Hold;
		}
		if (num2 > state.rope.Bottom)
		{
			state.climbing = false;
			state.rope = null;
			return Result.Fall;
		}
		if (num == 0)
		{
			return Result.Hold;
		}
		state.position = new BotNavigationGraph.Point(state.rope.x, num2);
		return Result.Climb;
	}
}
