using System;
using System.Collections.Generic;
using Server.Field;

namespace Server.Bot.Environment;

public sealed class BotAirCollision
{
	public enum Kind
	{
		None,
		Wall,
		Ceiling,
		Land
	}

	public readonly struct Hit(Kind kind, BotNavigationGraph.Point point, FootholdSegment foothold, double progress)
	{
		public readonly Kind kind = kind;

		public readonly BotNavigationGraph.Point point = point;

		public readonly FootholdSegment foothold = foothold;

		public readonly double progress = progress;

		public static Hit None => new Hit(Kind.None, default, null, double.PositiveInfinity);
	}

	private readonly MapRuntime map;

	private readonly BotGroundIndex ground;

	private readonly List<FootholdSegment> walls = new List<FootholdSegment>();

	private readonly List<FootholdSegment> ceilings = new List<FootholdSegment>();

	public BotAirCollision(MapRuntime map, BotGroundIndex ground)
	{
		this.map = map;
		this.ground = ground;
		HashSet<int> hashSet = BotFootholdCollision.FromBelow(map.footholds);
		foreach (FootholdSegment foothold in map.footholds)
		{
			if (foothold.Wall)
			{
				walls.Add(foothold);
			}
			if (hashSet.Contains(foothold.id))
			{
				ceilings.Add(foothold);
			}
		}
	}

	public Hit Resolve(BotNavigationGraph.Point from, BotNavigationGraph.Point to)
	{
		Hit result = Wall(from, to);
		Hit hit = Ceiling(from, to);
		Hit hit2 = Landing(from, to);
		if (hit.kind != Kind.None && hit.progress < result.progress)
		{
			result = hit;
		}
		if (hit2.kind != Kind.None && hit2.progress < result.progress)
		{
			result = hit2;
		}
		return result;
	}

	public Hit Wall(BotNavigationGraph.Point from, BotNavigationGraph.Point to, bool allowWalkableEndpoint = false)
	{
		if (from.x == to.x)
		{
			return Hit.None;
		}
		Hit result = Boundary(from, to);
		foreach (FootholdSegment wall in walls)
		{
			double num = ((int)wall.x1 - from.x) / (to.x - from.x);
			if (!(num <= 0.0) && !(num > 1.0))
			{
				double num2 = (double)from.y + (double)(to.y - from.y) * num;
				int num3 = (int)Math.Min(wall.y1, wall.y2);
				int num4 = (int)Math.Max(wall.y1, wall.y2);
				if (!(num2 < (double)num3) && !(num2 > (double)num4) && (!allowWalkableEndpoint || (!(Math.Abs(num2 - (double)num3) < 0.001) && (!(Math.Abs(num2 - (double)num4) < 0.001) || num4 - num3 > 26))) && num < result.progress)
				{
					result = new Hit(Kind.Wall, new BotNavigationGraph.Point((int)wall.x1 - Math.Sign(to.x - from.x), BotGroundPhysics.JavaRound(num2)), wall, num);
				}
			}
		}
		return result;
	}

	private Hit Boundary(BotNavigationGraph.Point from, BotNavigationGraph.Point to)
	{
		int num = map.right - map.left;
		int num2 = map.bottom - map.top;
		if (num <= 0 || num2 <= 0 || from.x == to.x)
		{
			return Hit.None;
		}
		int num3 = (int)map.minX;
		int num4 = (int)map.maxX;
		int num5 = Math.Sign(to.x - from.x);
		bool flag = num3 < num4;
		bool flag2 = num >= 262144 && num2 >= 262144;
		int num6;
		if (flag)
		{
			num6 = (flag2 ? num3 : Math.Min(map.left, num3));
		}
		else
		{
			num6 = map.left;
		}
		int num7;
		if (flag)
		{
			num7 = (flag2 ? num4 : Math.Max(map.right, num4));
		}
		else
		{
			num7 = map.right;
		}
		int num8 = ((num5 > 0) ? num7 : num6);
		if (num5 > 0 && (from.x > num8 || to.x <= num8))
		{
			return Hit.None;
		}
		if (num5 < 0 && (from.x < num8 || to.x >= num8))
		{
			return Hit.None;
		}
		double num9 = (num8 - from.x) / (to.x - from.x);
		if (num9 < 0.0 || num9 > 1.0)
		{
			return Hit.None;
		}
		return new Hit(Kind.Wall, new BotNavigationGraph.Point(num8, BotGroundPhysics.JavaRound((double)from.y + (double)(to.y - from.y) * num9)), null, num9);
	}

	public Hit Ceiling(BotNavigationGraph.Point from, BotNavigationGraph.Point to)
	{
		if (to.y >= from.y)
		{
			return Hit.None;
		}
		Hit result = Hit.None;
		foreach (FootholdSegment ceiling in ceilings)
		{
			int num = (int)ceiling.y1;
			if (ceiling.y1 != ceiling.y2 || num > from.y || num < to.y)
			{
				continue;
			}
			double num2 = (num - from.y) / (to.y - from.y);
			if (!(num2 <= 0.0) && !(num2 > 1.0))
			{
				double num3 = (double)from.x + (double)(to.x - from.x) * num2;
				if (!(num3 < (double)Math.Min(ceiling.x1, ceiling.x2)) && !(num3 > (double)Math.Max(ceiling.x1, ceiling.x2)) && num2 < result.progress)
				{
					result = new Hit(Kind.Ceiling, new BotNavigationGraph.Point(BotGroundPhysics.JavaRound(num3), num + 1), ceiling, num2);
				}
			}
		}
		return result;
	}

	public Hit Landing(BotNavigationGraph.Point from, BotNavigationGraph.Point to)
	{
		if (to.y < from.y)
		{
			return Hit.None;
		}
		int num = Math.Sign(to.x - from.x);
		if (num == 0)
		{
			return LandingAtX(from, to, to.x, 1.0);
		}
		int num2 = Math.Abs(to.x - from.x);
		for (int i = 0; i <= num2; i++)
		{
			double progress = i / num2;
			Hit result = LandingAtX(from, to, from.x + num * i, progress);
			if (result.kind == Kind.Land)
			{
				return result;
			}
		}
		return Hit.None;
	}

	private Hit LandingAtX(BotNavigationGraph.Point from, BotNavigationGraph.Point to, int x, double progress)
	{
		int y = BotGroundPhysics.JavaRound((double)from.y + (double)(to.y - from.y) * progress);
		Hit result = LandingAtProbe(from, x, y, progress, from.y + 1, tangent: false);
		if (result.kind == Kind.Land)
		{
			return result;
		}
		if (x == from.x)
		{
			return Hit.None;
		}
		return LandingAtProbe(from, x, y, progress, from.y, tangent: true);
	}

	private Hit LandingAtProbe(BotNavigationGraph.Point from, int x, int y, double progress, int probeY, bool tangent)
	{
		BotNavigationGraph.Point? point = ground.PointBelow(x, probeY);
		if (!point.HasValue || point.Value.y < Math.Min(from.y, y) || point.Value.y > Math.Max(from.y, y) || (tangent && point.Value.y != from.y))
		{
			return Hit.None;
		}
		FootholdSegment footholdSegment = ground.Below(x, probeY);
		if (footholdSegment != null)
		{
			return new Hit(Kind.Land, point.Value, footholdSegment, progress);
		}
		return Hit.None;
	}
}
